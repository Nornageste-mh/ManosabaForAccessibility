using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;

namespace ManosabaA11y
{
    /// <summary>
    /// 低性能模式，四档循环：关闭 → 保守 → 普通 → 激进 → 关闭。
    ///
    /// 为什么是「档位」而不是一堆复选框（0.0.0.14）：
    ///   0.0.0.13 只做了一档（= 现在的「保守」），用户反馈「优化有限、能不能更激进」。
    ///   一台机器上到底该降多少，玩家自己说不清，也没耐心逐项试；所以给一条阶梯：
    ///   每按一次键往前走一格，听到档位名就知道自己在哪一级，回到「关闭」即全还原。
    ///
    /// 每档加什么（实测见 docs/framework-notes.md §24）：
    ///   保守：URP 画质降级（renderScale 0.8 / MSAA 关 / 主光与软阴影关 / shadowDist 0 /
    ///         cascade 1 / addLights 0）+ 关掉柔边阴影；插件侧限频恒开（无视觉代价）。
    ///   普通：再降 renderScale 到 0.65、质量等级压到最低、纹理半分辨率、抗锯齿关、
    ///         阴影整体关、LOD 偏移降到 0.7。
    ///   激进：再降 renderScale 到 0.5、纹理四分之一、关各向异性过滤、关实时反射探针、
    ///         像素灯 0、关 VSync 并把帧率上限设到 30（省 GPU、让输入更跟手）、
    ///         停掉场景里所有粒子系统。
    ///   ★ 降低「窗口分辨率」不在档位里：它会改变窗口大小，读屏用户依赖固定窗口位置，
    ///     所以它单独一个默认关闭的开关，只有玩家自己开才会跟着生效。
    ///
    /// 无障碍纪律：
    ///   · 不自动改画面：只有玩家按键或配置里指定档位才动设置；
    ///   · 低帧率只提醒，绝不擅自切档；
    ///   · 每次换档都先 Restore() 再 Apply()，所以「降级」也干净（不会残留上一档的设置）；
    ///   · 关闭（档位 0）与插件卸载时把改过的设置逐项还原。
    /// </summary>
    internal static class LowPerf
    {
        /// <summary>档位。数值用于「下一档 = (+1) % 4」的循环。</summary>
        internal enum Mode { Off = 0, Conservative = 1, Normal = 2, Aggressive = 3 }

        internal const int ModeCount = 4;

        internal static Mode Current { get; private set; } = Mode.Off;

        /// <summary>兼容旧调用点：任何非「关闭」都算 Active。</summary>
        internal static bool Active { get { return Current != Mode.Off; } }

        internal static string Name(Mode m)
        {
            switch (m)
            {
                case Mode.Conservative: return "保守";
                case Mode.Normal: return "普通";
                case Mode.Aggressive: return "激进";
                default: return "关闭";
            }
        }

        internal static bool TryParseMode(string text, out Mode mode)
        {
            mode = Mode.Off;
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.Trim().ToLowerInvariant();
            if (t == "关闭" || t == "off" || t == "0" || t == "无") { mode = Mode.Off; return true; }
            if (t == "保守" || t == "conservative" || t == "1" || t == "低") { mode = Mode.Conservative; return true; }
            if (t == "普通" || t == "normal" || t == "2" || t == "中") { mode = Mode.Normal; return true; }
            if (t == "激进" || t == "aggressive" || t == "3" || t == "高") { mode = Mode.Aggressive; return true; }
            return false;
        }

        // ---- 保存的原始值（关闭/卸载时还原）----
        private static bool _savedUrp;
        private static float _savedRenderScale = 1f;
        private static int _savedMsaa = 4;
        private static bool _savedMainShadows = true;
        private static bool _savedSoftShadows = true;
        private static float _savedShadowDistance = 150f;
        private static int _savedCascade = 4;
        private static int _savedAdditionalLights = 8;

        private static bool _savedQuality;
        private static int _savedQualityLevel;
        private static int _savedTextureLimit;
        private static int _savedAntiAliasing;
        private static ShadowQuality _savedShadows;
        private static float _savedLodBias;
        private static int _savedPixelLightCount;
        private static float _savedShadowDistanceQuality;
        private static int _savedShadowCascades;
        private static bool _savedRealtimeReflection;
        private static AnisotropicFiltering _savedAniso;
        private static int _savedVSync;
        private static int _savedTargetFrameRate;

        private static bool _savedResolution;
        private static int _savedWidth, _savedHeight;
        private static FullScreenMode _savedMode;

        // ---- 被我们关掉的柔边阴影 / 粒子（还原时逐个打开/恢复）----
        private static readonly List<LeTai.TrueShadow.TrueShadow> _disabledShadows =
            new List<LeTai.TrueShadow.TrueShadow>();
        private static readonly List<ParticleSystem> _stoppedParticles = new List<ParticleSystem>();

        // ---- 低帧率提醒 ----
        private static float _fpsWindowStart = -1f;
        private static int _fpsFrames;
        private static float _lastRemindAt = -600f;
        private static bool _remindedThisSession;

        private static float _nextShadowSweepAt;
        private static bool _warnedUrp;

        // 当前档位实际应用的「手段」标记，供诊断与还原参考
        private static bool _appliedUrp, _appliedShadow, _appliedQuality, _appliedParticles;

        // ================================================================== 主循环

        internal static void Update()
        {
            try
            {
                // 组合键：四档循环（关闭 → 保守 → 普通 → 激进 → 关闭）
                if (Plugin.CfgLowPerfToggleKey != null
                    && Plugin.SpecDown(Plugin.CfgLowPerfToggleKey.Value))
                {
                    Mode next = (Mode)(((int)Current + 1) % ModeCount);
                    SetMode(next, "玩家按键");
                    return;
                }

                // 档位开着：新出现的柔边阴影/粒子要跟着降级（场景切换会重建 UI/立绘）
                if (Active && Time.unscaledTime >= _nextShadowSweepAt)
                {
                    _nextShadowSweepAt = Time.unscaledTime + SweepInterval();
                    if (_appliedShadow) DisableTrueShadows(false);
                    if (_appliedParticles) StopParticles(false);
                }

                MeasureFps();
            }
            catch (Exception e) { A11yHost.Diag("LowPerf: " + e.Message); }
        }

        /// <summary>档位越高，补扫周期越长（激进档自己也要省）。</summary>
        private static float SweepInterval()
        {
            switch (Current)
            {
                case Mode.Aggressive: return 6f;
                case Mode.Normal: return 4f;
                default: return 3f;
            }
        }

        /// <summary>开局按配置应用一次（配置默认「关闭」，所以正常玩家不会走到）。</summary>
        internal static void ApplyFromConfig()
        {
            try
            {
                if (Plugin.CfgLowPerfMode == null) return;
                Mode m;
                if (TryParseMode(Plugin.CfgLowPerfMode.Value, out m) && m != Mode.Off)
                    SetMode(m, "配置项");
            }
            catch (Exception e) { A11yHost.Diag("LowPerf: 配置应用失败 " + e.Message); }
        }

        // ================================================================== 换档

        /// <summary>
        /// 换档 = 先全部还原，再按新档位重新加码。
        /// 为什么要「先还原」：档位可以从激进往回转（激进→关闭），逐项撤销容易漏；
        /// 全量还原再应用虽然会有一点 hitch，但保证「关掉就是干净的原样」。
        /// </summary>
        internal static void SetMode(Mode mode, string why)
        {
            Mode old = Current;
            try
            {
                Restore();
                Current = mode;
                if (mode != Mode.Off) Apply(mode);

                A11yHost.Diag("LOWPERF 档位: " + Name(old) + " -> " + Name(mode) + "（" + why + "）"
                              + " urp=" + _appliedUrp + " shadow=" + _appliedShadow
                              + " quality=" + _appliedQuality + " particles=" + _appliedParticles);

                if (mode == Mode.Off) Repeat.Say("低性能模式已关闭。画面设置已还原。", true);
                else Repeat.Say("低性能模式：" + Name(mode) + "。", true);
            }
            catch (Exception e)
            {
                A11yHost.Diag("LOWPERF 换档失败: " + e.Message);
                try { Repeat.Say("低性能模式切换时出错，请查看日志。", true); } catch { }
            }
        }

        /// <summary>兼容旧调用点（历史代码/测试钩子）：按档位名切换。</summary>
        internal static void Enable(string why) { SetMode(Mode.Conservative, why); }
        internal static void Disable(string why) { SetMode(Mode.Off, why); }

        /// <summary>插件卸载时兜底还原（别把玩家的画面设置留在降级状态）。</summary>
        internal static void Shutdown()
        {
            try { if (Active) { Current = Mode.Off; Restore(); } }
            catch { }
        }

        // ================================================================== 按档位加码

        private static void Apply(Mode mode)
        {
            int level = (int)mode;

            // ---- 保守档就有：URP 画质 + 柔边阴影 ----
            if (Allowed(Plugin.CfgLowPerfDegradeUrp))
            {
                float scale = level >= 3 ? 0.5f : (level >= 2 ? 0.65f : 0.8f);
                DegradeUrp(scale);
                _appliedUrp = true;
            }
            if (Allowed(Plugin.CfgLowPerfNoShadow))
            {
                DisableTrueShadows(true);
                _appliedShadow = true;
            }

            // ---- 普通档起：质量等级 / 纹理 / 抗锯齿 / 阴影总开关 / LOD ----
            if (level >= 2 && Allowed(Plugin.CfgLowPerfQuality))
            {
                ApplyQuality(level >= 3);
                _appliedQuality = true;
            }

            // ---- 激进档起：各向异性/帧率上限/粒子 ----
            if (level >= 3 && Allowed(Plugin.CfgLowPerfAggressive))
            {
                ApplyAggressiveExtras();
                _appliedParticles = true;
            }

            // ---- 窗口分辨率：不属于档位，只有玩家自己开了这个开关才跟着生效 ----
            if (Allowed(Plugin.CfgLowPerfLowerResolution)) LowerResolution();
        }

        private static bool Allowed(ConfigEntry<bool> c)
        {
            try { return c == null || c.Value; } catch { return false; }
        }

        // ================================================================== URP

        private static UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset Urp()
        {
            try
            {
                var asset = GraphicsSettings.currentRenderPipeline;
                if (asset == null) return null;
                return asset.TryCast<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>();
            }
            catch { return null; }
        }

        private static void DegradeUrp(float renderScale)
        {
            var u = Urp();
            if (u == null)
            {
                if (!_warnedUrp)
                {
                    _warnedUrp = true;
                    A11yHost.Diag("LOWPERF: 当前不是 URP 管线，跳过画质降级");
                }
                return;
            }

            if (!_savedUrp)
            {
                try
                {
                    _savedRenderScale = u.renderScale;
                    _savedMsaa = u.msaaSampleCount;
                    _savedMainShadows = u.supportsMainLightShadows;
                    _savedSoftShadows = u.supportsSoftShadows;
                    _savedShadowDistance = u.shadowDistance;
                    _savedCascade = u.shadowCascadeCount;
                    _savedAdditionalLights = u.maxAdditionalLightsCount;
                    _savedUrp = true;
                }
                catch (Exception e) { A11yHost.Diag("LOWPERF: 保存原始画质失败 " + e.Message); }
            }

            Try(() => u.renderScale = renderScale, "renderScale");
            Try(() => u.msaaSampleCount = 1, "msaa");
            Try(() => u.supportsMainLightShadows = false, "mainShadows");
            Try(() => u.supportsSoftShadows = false, "softShadows");
            Try(() => u.shadowDistance = 0f, "shadowDistance");
            Try(() => u.shadowCascadeCount = 1, "cascade");
            Try(() => u.maxAdditionalLightsCount = 0, "additionalLights");

            A11yHost.Diag("LOWPERF URP: renderScale=" + SafeGet(() => u.renderScale.ToString("F2"))
                          + " msaa=" + SafeGet(() => u.msaaSampleCount.ToString())
                          + " mainShadow=" + SafeGet(() => u.supportsMainLightShadows.ToString())
                          + " shadowDist=" + SafeGet(() => u.shadowDistance.ToString("F0")));
        }

        private static void RestoreUrp()
        {
            if (!_savedUrp) return;
            var u = Urp();
            if (u == null) return;
            Try(() => u.renderScale = _savedRenderScale, "renderScale");
            Try(() => u.msaaSampleCount = _savedMsaa, "msaa");
            Try(() => u.supportsMainLightShadows = _savedMainShadows, "mainShadows");
            Try(() => u.supportsSoftShadows = _savedSoftShadows, "softShadows");
            Try(() => u.shadowDistance = _savedShadowDistance, "shadowDistance");
            Try(() => u.shadowCascadeCount = _savedCascade, "cascade");
            Try(() => u.maxAdditionalLightsCount = _savedAdditionalLights, "additionalLights");
            _savedUrp = false;
            A11yHost.Diag("LOWPERF URP: 已还原画质设置");
        }

        // ================================================================== 质量等级 / 纹理 / 阴影 / LOD

        private static void ApplyQuality(bool aggressive)
        {
            if (!_savedQuality)
            {
                try
                {
                    _savedQualityLevel = QualitySettings.GetQualityLevel();
                    _savedTextureLimit = QualitySettings.masterTextureLimit;
                    _savedAntiAliasing = QualitySettings.antiAliasing;
                    _savedShadows = QualitySettings.shadows;
                    _savedLodBias = QualitySettings.lodBias;
                    _savedPixelLightCount = QualitySettings.pixelLightCount;
                    _savedShadowDistanceQuality = QualitySettings.shadowDistance;
                    _savedShadowCascades = QualitySettings.shadowCascades;
                    _savedRealtimeReflection = QualitySettings.realtimeReflectionProbes;
                    _savedAniso = QualitySettings.anisotropicFiltering;
                    _savedVSync = QualitySettings.vSyncCount;
                    _savedTargetFrameRate = Application.targetFrameRate;
                    _savedQuality = true;
                }
                catch (Exception e) { A11yHost.Diag("LOWPERF: 保存质量设置失败 " + e.Message); }
            }

            Try(() => QualitySettings.SetQualityLevel(0, true), "qualityLevel");
            Try(() => QualitySettings.masterTextureLimit = aggressive ? 2 : 1, "textureLimit");
            Try(() => QualitySettings.antiAliasing = 0, "antiAliasing");
            Try(() => QualitySettings.shadows = ShadowQuality.Disable, "shadows");
            Try(() => QualitySettings.lodBias = aggressive ? 0.5f : 0.7f, "lodBias");
            Try(() => QualitySettings.pixelLightCount = 0, "pixelLightCount");
            Try(() => QualitySettings.shadowDistance = 0f, "qualityShadowDist");
            Try(() => QualitySettings.shadowCascades = 0, "shadowCascades");
            Try(() => QualitySettings.realtimeReflectionProbes = false, "realtimeReflection");

            A11yHost.Diag("LOWPERF 质量: level=" + SafeGet(() => QualitySettings.GetQualityLevel().ToString())
                          + " texLimit=" + SafeGet(() => QualitySettings.masterTextureLimit.ToString())
                          + " aa=" + SafeGet(() => QualitySettings.antiAliasing.ToString())
                          + " shadows=" + SafeGet(() => QualitySettings.shadows.ToString())
                          + " lod=" + SafeGet(() => QualitySettings.lodBias.ToString("F2")));
        }

        private static void RestoreQuality()
        {
            if (!_savedQuality) return;
            Try(() => QualitySettings.SetQualityLevel(_savedQualityLevel, true), "qualityLevel");
            Try(() => QualitySettings.masterTextureLimit = _savedTextureLimit, "textureLimit");
            Try(() => QualitySettings.antiAliasing = _savedAntiAliasing, "antiAliasing");
            Try(() => QualitySettings.shadows = _savedShadows, "shadows");
            Try(() => QualitySettings.lodBias = _savedLodBias, "lodBias");
            Try(() => QualitySettings.pixelLightCount = _savedPixelLightCount, "pixelLightCount");
            Try(() => QualitySettings.shadowDistance = _savedShadowDistanceQuality, "qualityShadowDist");
            Try(() => QualitySettings.shadowCascades = _savedShadowCascades, "shadowCascades");
            Try(() => QualitySettings.realtimeReflectionProbes = _savedRealtimeReflection, "realtimeReflection");
            Try(() => QualitySettings.anisotropicFiltering = _savedAniso, "aniso");
            Try(() => QualitySettings.vSyncCount = _savedVSync, "vsync");
            Try(() => Application.targetFrameRate = _savedTargetFrameRate, "targetFps");
            _savedQuality = false;
            A11yHost.Diag("LOWPERF 质量: 已还原质量设置");
        }

        // ================================================================== 激进档专属

        private static void ApplyAggressiveExtras()
        {
            // 各向异性过滤对读屏用户几乎只影响地面眩光，关掉省一点带宽
            Try(() => QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable, "aniso");
            // VSync 关掉 + 帧率上限 30：帧时间更稳、输入更跟手（也不让 GPU 空转画 60 帧）
            Try(() => QualitySettings.vSyncCount = 0, "vsync");
            Try(() => Application.targetFrameRate = 30, "targetFps");
            StopParticles(true);

            A11yHost.Diag("LOWPERF 激进: vSync=" + SafeGet(() => QualitySettings.vSyncCount.ToString())
                          + " targetFps=" + SafeGet(() => Application.targetFrameRate.ToString())
                          + " aniso=" + SafeGet(() => QualitySettings.anisotropicFiltering.ToString())
                          + " 粒子停用=" + _stoppedParticles.Count);
        }

        /// <summary>
        /// 停掉场景里所有粒子系统。本作实测 ADV 场景 `particles=0`，
        /// 所以这条主要是给标题/演出场景兜底——有就省，没有也不报错。
        /// </summary>
        private static void StopParticles(bool record)
        {
            int n = 0;
            try
            {
                var all = Resources.FindObjectsOfTypeAll<ParticleSystem>();
                for (int i = 0; i < all.Length; i++)
                {
                    var ps = all[i];
                    if (ps == null) continue;
                    try
                    {
                        if (!ps.isPlaying) continue;
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                        n++;
                        if (record) _stoppedParticles.Add(ps);
                    }
                    catch { }
                }
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 粒子: " + e.Message); }
            if (n > 0) A11yHost.Diag("LOWPERF 粒子: 停用 " + n + " 个粒子系统");
        }

        private static void RestoreParticles()
        {
            int n = 0;
            for (int i = 0; i < _stoppedParticles.Count; i++)
            {
                try
                {
                    var ps = _stoppedParticles[i];
                    if (ps == null) continue;
                    ps.Play();
                    n++;
                }
                catch { }
            }
            _stoppedParticles.Clear();
            if (n > 0) A11yHost.Diag("LOWPERF 粒子: 恢复 " + n + " 个粒子系统");
        }

        // ================================================================== TrueShadow

        /// <summary>
        /// 关掉柔边阴影（LeTai.TrueShadow）。本作实测 803 个组件——UI/立绘的多趟模糊，
        /// 属于 GPU 侧的固定成本。注意：文字与立绘的阴影会消失、对比度会变，
        /// 所以在配置里可以单独关掉这一项。
        /// </summary>
        private static void DisableTrueShadows(bool record)
        {
            int n = 0;
            try
            {
                var all = Resources.FindObjectsOfTypeAll<LeTai.TrueShadow.TrueShadow>();
                for (int i = 0; i < all.Length; i++)
                {
                    var ts = all[i];
                    if (ts == null) continue;
                    try
                    {
                        if (!ts.enabled) continue;
                        ts.enabled = false;
                        n++;
                        if (record) _disabledShadows.Add(ts);
                    }
                    catch { }
                }
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF TrueShadow: " + e.Message); }
            if (n > 0) A11yHost.Diag("LOWPERF TrueShadow: 关闭 " + n + " 个柔边阴影组件");
        }

        private static void RestoreTrueShadows()
        {
            int n = 0;
            for (int i = 0; i < _disabledShadows.Count; i++)
            {
                try
                {
                    var ts = _disabledShadows[i];
                    if (ts != null) { ts.enabled = true; n++; }
                }
                catch { }
            }
            _disabledShadows.Clear();
            if (n > 0) A11yHost.Diag("LOWPERF TrueShadow: 还原 " + n + " 个柔边阴影组件");
        }

        // ================================================================== 窗口分辨率

        /// <summary>
        /// 降低窗口分辨率（默认关闭！）。它会改变窗口大小/清晰度，是最"看得出来"的一档，
        /// 所以配置里默认 false，只有玩家自己打开才会生效。
        /// </summary>
        private static void LowerResolution()
        {
            try
            {
                _savedWidth = Screen.width;
                _savedHeight = Screen.height;
                _savedMode = Screen.fullScreenMode;
                _savedResolution = true;
                int w = Mathf.Max(960, Mathf.RoundToInt(_savedWidth * 0.75f));
                int h = Mathf.Max(540, Mathf.RoundToInt(_savedHeight * 0.75f));
                Screen.SetResolution(w, h, _savedMode);
                A11yHost.Diag("LOWPERF 分辨率: " + _savedWidth + "x" + _savedHeight + " -> " + w + "x" + h
                              + " mode=" + _savedMode);
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 分辨率: " + e.Message); }
        }

        private static void RestoreResolution()
        {
            if (!_savedResolution) return;
            try
            {
                Screen.SetResolution(_savedWidth, _savedHeight, _savedMode);
                A11yHost.Diag("LOWPERF 分辨率: 还原 " + _savedWidth + "x" + _savedHeight);
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 分辨率还原: " + e.Message); }
            _savedResolution = false;
        }

        private static void Restore()
        {
            RestoreUrp();
            RestoreTrueShadows();
            RestoreParticles();
            RestoreQuality();
            RestoreResolution();
            _appliedUrp = _appliedShadow = _appliedQuality = _appliedParticles = false;
        }

        // ================================================================== 低帧率提醒

        /// <summary>
        /// 低帧率只**提醒**，绝不自动切档（玩家可能正在别的窗口、也可能在乎画面）。
        /// 规则：每 30 秒算一次平均帧率；低于 15 且档位为关闭，则 10 分钟内最多提醒一次。
        /// </summary>
        private static void MeasureFps()
        {
            float now = Time.realtimeSinceStartup;
            if (_fpsWindowStart < 0f) { _fpsWindowStart = now; _fpsFrames = 0; return; }
            _fpsFrames++;
            float span = now - _fpsWindowStart;
            if (span < 30f) return;

            float fps = span > 0f ? _fpsFrames / span : 0f;
            _fpsWindowStart = now;
            _fpsFrames = 0;

            if (Active) return;
            if (Plugin.CfgLowPerfHint == null || !Plugin.CfgLowPerfHint.Value) return;
            if (fps >= 15f) return;
            if (_remindedThisSession && now - _lastRemindAt < 600f) return;

            _remindedThisSession = true;
            _lastRemindAt = now;
            A11yHost.Diag("LOWPERF 低帧率提醒: fps=" + fps.ToString("F1") + " 当前档位=关闭");
            Repeat.Say("当前帧率较低，约 " + Mathf.RoundToInt(fps)
                       + " 帧。按 " + (Plugin.CfgLowPerfToggleKey != null ? Plugin.CfgLowPerfToggleKey.Value : "Alt+Shift+F2")
                       + " 可以切换低性能模式的档位。", true);
        }

        // ================================================================== 工具

        private static void Try(Action a, string name)
        {
            try { a(); }
            catch (Exception e) { A11yHost.Diag("LOWPERF " + name + ": " + e.Message); }
        }

        private static string SafeGet(Func<string> f)
        {
            try { return f(); } catch { return "?"; }
        }
    }
}
