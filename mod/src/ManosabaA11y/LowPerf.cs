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
        /// <summary>档位。数值用于「下一档 = (+1) % ModeCount」的循环。</summary>
        /// <remarks>
        /// 0.0.0.15 起改成五档并重命名（用户要求「调整挡位名称、再加更多挡位、极限档只剩黑窗口」）：
        ///   关闭 / 轻度 / 均衡 / 强力 / 极限。
        /// 旧名字（保守/普通/激进）在 TryParseMode 里仍然认，只为了让老 cfg 不至于变成「关闭」——
        /// 迁移时会写回新名字（见 Plugin 的 cfg 迁移块）。
        /// </remarks>
        internal enum Mode { Off = 0, Light = 1, Balanced = 2, Strong = 3, Extreme = 4 }

        internal const int ModeCount = 5;

        internal static Mode Current { get; private set; } = Mode.Off;

        /// <summary>兼容旧调用点：任何非「关闭」都算 Active。</summary>
        internal static bool Active { get { return Current != Mode.Off; } }

        internal static string Name(Mode m)
        {
            switch (m)
            {
                case Mode.Light: return "轻度";
                case Mode.Balanced: return "均衡";
                case Mode.Strong: return "强力";
                case Mode.Extreme: return "极限";
                default: return "关闭";
            }
        }

        internal static bool TryParseMode(string text, out Mode mode)
        {
            mode = Mode.Off;
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.Trim().ToLowerInvariant();
            if (t == "关闭" || t == "off" || t == "0" || t == "无") { mode = Mode.Off; return true; }
            // 新名字
            if (t == "轻度" || t == "light" || t == "1" || t == "低") { mode = Mode.Light; return true; }
            if (t == "均衡" || t == "balanced" || t == "2" || t == "中") { mode = Mode.Balanced; return true; }
            if (t == "强力" || t == "strong" || t == "3" || t == "高") { mode = Mode.Strong; return true; }
            if (t == "极限" || t == "extreme" || t == "4" || t == "黑屏" || t == "全黑") { mode = Mode.Extreme; return true; }
            // 旧名字（0.0.0.14 及更早的 cfg 里可能就是这些词）
            if (t == "保守" || t == "conservative") { mode = Mode.Light; return true; }
            if (t == "普通" || t == "normal") { mode = Mode.Balanced; return true; }
            if (t == "激进" || t == "aggressive") { mode = Mode.Strong; return true; }
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
        private static int _savedTargetFrameRate = -1;

        private static bool _savedResolution;
        private static int _nativeWidth, _nativeHeight;
        private static float _appliedResolutionScale = 1f;
        private static int _savedWidth, _savedHeight;
        private static FullScreenMode _savedMode;

        // ---- 被我们关掉的柔边阴影 / 粒子（还原时逐个打开/恢复）----
        private static readonly List<LeTai.TrueShadow.TrueShadow> _disabledShadows =
            new List<LeTai.TrueShadow.TrueShadow>();
        private static readonly List<ParticleSystem> _stoppedParticles = new List<ParticleSystem>();

        // ---- 极限档（画面全黑）改过的东西 ----
        private static bool _appliedBlackout;
        private static readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
        // il2cpp 里每次 FindObjectsOfTypeAll 返回的都是**新的托管包装对象**，
        // 用 ReferenceEquals 判重会全部判成“新的”，于是每轮补扫都重复追加（实测 341 涨到 2355）。
        // 只认 GetInstanceID()。
        private static readonly HashSet<int> _hiddenRendererIds = new HashSet<int>();
        private static readonly HashSet<int> _blackenedGraphicIds = new HashSet<int>();
        private static readonly HashSet<int> _disabledLightIds = new HashSet<int>();
        private static readonly HashSet<int> _savedCameraIds = new HashSet<int>();
        private static readonly List<Light> _disabledLights = new List<Light>();
        // UI 不靠 CanvasRenderer.cull 来隐藏：UGUI 的 GraphicRaycaster 会把 cull 的图元跳过，
        // 那样「黑屏」的同时鼠标点击也全废了（游戏就彻底推不动）。改成把每个 Graphic 的颜色刷黑——
        // 颜色不影响射线判定，所以画面黑了、输入还在。
        private static readonly List<UnityEngine.UI.Graphic> _blackenedGraphics =
            new List<UnityEngine.UI.Graphic>();
        private static readonly List<Color> _blackenedColors = new List<Color>();
        // 相机：保留实例（ScreenSpaceCamera 的 UI 还要用 worldCamera），只把 cullingMask 清 0 + 纯黑清屏
        private static readonly List<Camera> _savedCameras = new List<Camera>();
        private static readonly List<int> _savedCullMask = new List<int>();
        private static readonly List<CameraClearFlags> _savedClearFlags = new List<CameraClearFlags>();
        private static readonly List<Color> _savedCamBg = new List<Color>();

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
                    if (_appliedBlackout) BlackoutNewObjects();
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
                case Mode.Extreme: return 8f;
                case Mode.Strong: return 6f;
                case Mode.Balanced: return 4f;
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
        internal static void Enable(string why) { SetMode(Mode.Light, why); }
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

            // ---- 轻度档就有：URP 画质 + 柔边阴影 ----
            if (Allowed(Plugin.CfgLowPerfDegradeUrp))
            {
                float scale = level >= 4 ? 0.3f : (level >= 3 ? 0.5f : (level >= 2 ? 0.65f : 0.8f));
                DegradeUrp(scale);
                _appliedUrp = true;
            }
            if (Allowed(Plugin.CfgLowPerfNoShadow))
            {
                DisableTrueShadows(true);
                _appliedShadow = true;
            }

            // ---- 均衡档起：质量等级 / 纹理 / 抗锯齿 / 阴影总开关 / LOD ----
            if (level >= 2 && Allowed(Plugin.CfgLowPerfQuality))
            {
                ApplyQuality(level >= 3);
                _appliedQuality = true;
            }

            // ---- 强力档起：各向异性/帧率上限/粒子 ----
            if (level >= 3 && Allowed(Plugin.CfgLowPerfAggressive))
            {
                ApplyAggressiveExtras();
                _appliedParticles = true;
            }

            // ---- 极限档：把画面整个扔掉，只留黑窗口 ----
            // 为什么单独一个开关：这是「屏幕上一个字都看不见」的档位，误触会很吓人，
            // 所以配置里可以禁用（关掉后循环到极限档时只播报、不真的动手）。
            if (level >= 4 && Allowed(Plugin.CfgLowPerfExtreme))
            {
                CullAllRendering();
                _appliedBlackout = true;
            }

            // ---- 窗口分辨率：跟着档位一起降（玩家可以单独关掉这一项） ----
            // 0.0.16 起按用户要求把「降窗口分辨率」算进档位：越往上窗口越小、越省。
            if (Allowed(Plugin.CfgLowPerfLowerResolution))
            {
                float resScale = ResolutionScaleFor(mode);
                if (resScale < 0.999f) SetResolutionScale(resScale);
            }

            // ---- 极限档专属：真的极限（有代价，玩家已确认接受） ----
            if (level >= 4)
            {
                ApplyExtremeExtras();
            }
        }

        /// <summary>
        /// 各档位对应的窗口分辨率比例（1.0 = 不动窗口）。抽成纯函数是为了能被单元断言覆盖：
        /// 「档位越高窗口越小」这件事不该只靠实机看起来对不对。
        /// </summary>
        internal static float ResolutionScaleFor(Mode mode)
        {
            switch (mode)
            {
                case Mode.Balanced: return 0.85f;
                case Mode.Strong: return 0.70f;
                case Mode.Extreme: return 0.50f;
                default: return 1.0f;
            }
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
                    if (_savedTargetFrameRate < 0) _savedTargetFrameRate = Application.targetFrameRate;
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

        // ================================================================== 极限档：丢掉所有画面

        // ---- 极限档专属：真的极限（用户明确说「有代价本来就是正常的」） ----
        private static bool _appliedExtreme;
        private static readonly List<UnityEngine.Rendering.Volume> _disabledVolumes =
            new List<UnityEngine.Rendering.Volume>();
        private static readonly List<Animator> _disabledAnimators = new List<Animator>();
        private static readonly HashSet<int> _disabledAnimatorIds = new HashSet<int>();
        private static bool _savedLodLevel, _savedSkinWeights, _savedMaxLod;
        private static int _savedMaxLodLevel = -1;
        private static int _savedSkinWeightsValue = -1;

        /// <summary>
        /// 极限档的「再往下压」：这一档追求的是最高帧数，代价（画质/动画）玩家已确认接受。
        ///   · 强制最低 LOD、单骨骼蒙皮（省 CPU/GPU 的蒙皮与网格切换）
        ///   · 关掉所有 URP Volume（后处理/调色/泛光这一整类的每帧开销）
        ///   · 每台相机的后处理/阴影/抗锯齿全关
        ///   · 停掉所有 Animator（立绘抖动的状态机每帧求值也占 CPU）
        ///   · 锁 30 帧 + 关 VSync（把省下来的算力留给输入响应）
        /// </summary>
        private static void ApplyExtremeExtras()
        {
            if (_appliedExtreme) return;
            _appliedExtreme = true;
            int volumes = 0, anims = 0, cams = 0;
            try
            {
                // 质量：最低 LOD、单骨骼
                try
                {
                    if (!_savedLodLevel)
                    {
                        _savedMaxLodLevel = QualitySettings.maximumLODLevel;
                        _savedSkinWeightsValue = (int)QualitySettings.skinWeights;
                        _savedLodLevel = true;
                    }
                    QualitySettings.maximumLODLevel = 1;
                    QualitySettings.skinWeights = SkinWeights.OneBone;
                }
                catch (Exception e) { A11yHost.Diag("LOWPERF 极限/质量: " + e.Message); }

                // URP Volume（后处理整条链）
                try
                {
                    var vols = Resources.FindObjectsOfTypeAll<UnityEngine.Rendering.Volume>();
                    for (int i = 0; i < vols.Length; i++)
                    {
                        var v = vols[i];
                        if (v == null || !v.enabled) continue;
                        v.enabled = false;
                        _disabledVolumes.Add(v);
                        volumes++;
                    }
                }
                catch (Exception e) { A11yHost.Diag("LOWPERF 极限/Volume: " + e.Message); }

                // 相机：关后处理/阴影/抗锯齿
                var camsAll = Resources.FindObjectsOfTypeAll<Camera>();
                for (int i = 0; i < camsAll.Length; i++)
                {
                    var c = camsAll[i];
                    if (c == null) continue;
                    try
                    {
                        var data = c.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                        if (data != null)
                        {
                            data.renderPostProcessing = false;
                            data.renderShadows = false;
                            data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.None;
                            data.dithering = false;
                            cams++;
                        }
                    }
                    catch { }
                }

                // Animator
                try
                {
                    var animsAll = Resources.FindObjectsOfTypeAll<Animator>();
                    for (int i = 0; i < animsAll.Length; i++)
                    {
                        var a = animsAll[i];
                        if (a == null) continue;
                        int id = a.GetInstanceID();
                        if (!_disabledAnimatorIds.Add(id)) continue;
                        if (!a.enabled) continue;
                        a.enabled = false;
                        _disabledAnimators.Add(a);
                        anims++;
                    }
                }
                catch (Exception e) { A11yHost.Diag("LOWPERF 极限/Animator: " + e.Message); }

                // 帧率上限
                try
                {
                    if (_savedTargetFrameRate < 0) _savedTargetFrameRate = Application.targetFrameRate;
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = 30;
                }
                catch (Exception e) { A11yHost.Diag("LOWPERF 极限/帧率: " + e.Message); }
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 极限: " + e.Message); }

            A11yHost.Diag("LOWPERF 极限加强: Volume=" + volumes + " Animator=" + anims + " 相机后处理=" + cams
                          + " LOD=1 单骨骼 目标帧率=30");
        }

        private static void RestoreExtremeExtras()
        {
            if (!_appliedExtreme) return;
            int volumes = 0, anims = 0;
            for (int i = 0; i < _disabledVolumes.Count; i++)
            {
                try { var v = _disabledVolumes[i]; if (v != null) { v.enabled = true; volumes++; } } catch { }
            }
            _disabledVolumes.Clear();

            for (int i = 0; i < _disabledAnimators.Count; i++)
            {
                try { var a = _disabledAnimators[i]; if (a != null) { a.enabled = true; anims++; } } catch { }
            }
            _disabledAnimators.Clear();
            _disabledAnimatorIds.Clear();

            try
            {
                if (_savedLodLevel)
                {
                    QualitySettings.maximumLODLevel = _savedMaxLodLevel;
                    try { QualitySettings.skinWeights = (SkinWeights)_savedSkinWeightsValue; } catch { }
                }
                if (_savedTargetFrameRate >= 0) Application.targetFrameRate = _savedTargetFrameRate;
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 极限还原: " + e.Message); }

            _appliedExtreme = false;
            if (volumes + anims > 0)
                A11yHost.Diag("LOWPERF 极限加强: 还原 Volume=" + volumes + " Animator=" + anims);
        }

        /// <summary>
        /// 极限档：把画面整个扔掉，只剩一个黑窗口。三件事：
        ///   1. 所有相机 cullingMask 清 0、清屏色改纯黑 → 3D 场景一张都不画；
        ///   2. 所有 Renderer 停用（立绘/背景/特效的绘制入口）；
        ///   3. 所有 UI Graphic 的颜色刷成黑色 → 对话框、按钮、标题全部变成黑块，屏幕整体是黑的。
        ///
        /// 为什么不干脆把 Canvas / CanvasRenderer 关掉：UGUI 的 GraphicRaycaster 只对
        /// 「已注册到 GraphicRegistry 且未被 cull」的图元做射线检测。一旦关掉 Canvas 或
        /// 设 CanvasRenderer.cull，**鼠标点击也一起失效** —— 游戏就彻底推不动了（软锁）。
        /// 刷颜色不影响射线判定，所以这是唯一能「全黑但还能玩」的做法。
        /// </summary>
        private static void CullAllRendering()
        {
            int cams = 0, rends = 0, lights = 0, graphics = 0;

            // 1) 相机：保留实例（ScreenSpaceCamera 的 UI 还要用 worldCamera），只清 cullingMask
            try
            {
                var all = Resources.FindObjectsOfTypeAll<Camera>();
                for (int i = 0; i < all.Length; i++)
                {
                    var cam = all[i];
                    if (cam == null) continue;
                    try
                    {
                        _savedCameras.Add(cam); _savedCameraIds.Add(cam.GetInstanceID());
                        _savedCullMask.Add(cam.cullingMask);
                        _savedClearFlags.Add(cam.clearFlags);
                        _savedCamBg.Add(cam.backgroundColor);
                        cam.cullingMask = 0;
                        cam.clearFlags = CameraClearFlags.SolidColor;
                        cam.backgroundColor = Color.black;
                        cam.allowHDR = false;
                        cam.allowMSAA = false;
                        cams++;
                    }
                    catch { }
                }
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 极限/相机: " + e.Message); }

            // 2) 所有 Renderer（Mesh/Skinned/Sprite/Particle/Line/Trail 都是 Renderer 子类）
            try
            {
                var all = Resources.FindObjectsOfTypeAll<Renderer>();
                for (int i = 0; i < all.Length; i++)
                {
                    var r = all[i];
                    if (r == null) continue;
                    try
                    {
                        if (!r.enabled) continue;
                        r.enabled = false;
                        _hiddenRenderers.Add(r); _hiddenRendererIds.Add(r.GetInstanceID());
                        rends++;
                    }
                    catch { }
                }
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 极限/Renderer: " + e.Message); }

            // 3) 灯光（黑屏下不再需要，省掉光照计算）
            try
            {
                var all = Resources.FindObjectsOfTypeAll<Light>();
                for (int i = 0; i < all.Length; i++)
                {
                    var li = all[i];
                    if (li == null) continue;
                    try
                    {
                        if (!li.enabled) continue;
                        li.enabled = false;
                        _disabledLights.Add(li);
                        lights++;
                    }
                    catch { }
                }
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 极限/灯光: " + e.Message); }

            // 4) UI：颜色刷黑（不是 cull！见方法注释）
            try
            {
                var all = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Graphic>();
                for (int i = 0; i < all.Length; i++)
                {
                    var g = all[i];
                    if (g == null) continue;
                    try
                    {
                        Color c = g.color;
                        if (c.r == 0f && c.g == 0f && c.b == 0f) continue;   // 已经是黑的，不用记
                        _blackenedGraphics.Add(g); _blackenedGraphicIds.Add(g.GetInstanceID());
                        _blackenedColors.Add(c);
                        g.color = new Color(0f, 0f, 0f, c.a);
                        graphics++;
                    }
                    catch { }
                }
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 极限/UI: " + e.Message); }

            A11yHost.Diag("LOWPERF 极限: 相机=" + cams + " renderer=" + rends
                          + " 灯光=" + lights + " UI图元刷黑=" + graphics + "（点击仍然可用）");
        }

        /// <summary>
        /// 极限档的增量补扫：剧情推进/切换场景会**新建** Renderer、UI 图元与相机，
        /// 只做一次全量会把新出现的对象漏掉（实测 run127b：黑屏期间画面重新亮起、还原计数也对不上）。
        /// 开档时全量，之后每轮补扫把新出现的对象也按住。
        /// </summary>
        private static void BlackoutNewObjects()
        {
            int addedR = 0, addedG = 0, addedC = 0;
            try
            {
                var cam = Resources.FindObjectsOfTypeAll<Camera>();
                for (int i = 0; i < cam.Length; i++)
                {
                    var c = cam[i];
                    if (c == null) continue;
                    if (!_savedCameraIds.Add(c.GetInstanceID())) continue;
                    _savedCameras.Add(c); _savedCullMask.Add(c.cullingMask);
                    _savedClearFlags.Add(c.clearFlags); _savedCamBg.Add(c.backgroundColor);
                    c.cullingMask = 0;
                    c.clearFlags = CameraClearFlags.SolidColor;
                    c.backgroundColor = Color.black;
                    addedC++;
                }

                var rend = Resources.FindObjectsOfTypeAll<Renderer>();
                for (int i = 0; i < rend.Length; i++)
                {
                    var r = rend[i];
                    if (r == null) continue;
                    int rid = r.GetInstanceID();
                    if (_hiddenRendererIds.Contains(rid)) continue;
                    _hiddenRendererIds.Add(rid);
                    // 只登记「我们真的关掉」的那些：FindObjectsOfTypeAll<Renderer> 还会返回
                    // 大量未激活预制体里的 Renderer（实测约 2500 个），把它们记进还原名单会让
                    // 「还原 N 个 Renderer」虚高、也白占内存。
                    if (!r.enabled) continue;
                    _hiddenRenderers.Add(r); r.enabled = false; addedR++;
                }

                var gs = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Graphic>();
                for (int i = 0; i < gs.Length; i++)
                {
                    var g = gs[i];
                    if (g == null) continue;
                    int gid = g.GetInstanceID();
                    if (_blackenedGraphicIds.Contains(gid)) continue;
                    _blackenedGraphicIds.Add(gid);
                    Color oc = g.color;
                    if (oc.r <= 0f && oc.g <= 0f && oc.b <= 0f) continue;   // 本来就是黑的，不用还原
                    _blackenedGraphics.Add(g); _blackenedColors.Add(oc);
                    g.color = Color.black;
                    addedG++;
                }
            }
            catch (Exception e) { A11yHost.Diag("LOWPERF 极限补扫: " + e.Message); }

            if (addedC + addedR + addedG > 0)
                A11yHost.Diag("LOWPERF 极限补扫: 新增相机=" + addedC + " renderer=" + addedR + " UI图元=" + addedG);
        }

        /// <summary>还原极限档改过的一切（顺序：UI 颜色 → Renderer → 灯光 → 相机）。</summary>
        private static void RestoreBlackout()
        {
            if (_blackenedGraphics.Count > 0)
            {
                int n = 0;
                for (int i = 0; i < _blackenedGraphics.Count; i++)
                {
                    try
                    {
                        var g = _blackenedGraphics[i];
                        if (g != null) { g.color = _blackenedColors[i]; n++; }
                    }
                    catch { }
                }
                _blackenedGraphics.Clear();
                _blackenedColors.Clear();
                if (n > 0) A11yHost.Diag("LOWPERF 极限: 还原 " + n + " 个 UI 图元颜色");
            }

            if (_hiddenRenderers.Count > 0)
            {
                int n = 0;
                for (int i = 0; i < _hiddenRenderers.Count; i++)
                {
                    try
                    {
                        var r = _hiddenRenderers[i];
                        if (r != null) { r.enabled = true; n++; }
                    }
                    catch { }
                }
                _hiddenRenderers.Clear();
                if (n > 0) A11yHost.Diag("LOWPERF 极限: 还原 " + n + " 个 Renderer");
            }

            if (_disabledLights.Count > 0)
            {
                int n = 0;
                for (int i = 0; i < _disabledLights.Count; i++)
                {
                    try
                    {
                        var li = _disabledLights[i];
                        if (li != null) { li.enabled = true; n++; }
                    }
                    catch { }
                }
                _disabledLights.Clear();
                if (n > 0) A11yHost.Diag("LOWPERF 极限: 还原 " + n + " 个灯光");
            }

            if (_savedCameras.Count > 0)
            {
                int n = 0;
                for (int i = 0; i < _savedCameras.Count; i++)
                {
                    try
                    {
                        var cam = _savedCameras[i];
                        if (cam != null)
                        {
                            cam.cullingMask = _savedCullMask[i];
                            cam.clearFlags = _savedClearFlags[i];
                            cam.backgroundColor = _savedCamBg[i];
                            n++;
                        }
                    }
                    catch { }
                }
                _savedCameras.Clear();
                _savedCullMask.Clear();
                _savedClearFlags.Clear();
                _savedCamBg.Clear();
                if (n > 0) A11yHost.Diag("LOWPERF 极限: 还原 " + n + " 个相机");
            }
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
        /// 按比例降窗口分辨率（0.0.16 起属于档位：均衡 0.85 / 强力 0.7 / 极限 0.5）。
        /// 一律以**原生分辨率**为基准算，所以反复切档不会越缩越小；原值只在第一次记下来，还原用。
        /// </summary>
        private static void SetResolutionScale(float scale)
        {
            try
            {
                if (!_savedResolution)
                {
                    _savedWidth = Screen.width;
                    _savedHeight = Screen.height;
                    _savedMode = Screen.fullScreenMode;
                    _nativeWidth = _savedWidth;
                    _nativeHeight = _savedHeight;
                    _savedResolution = true;
                }
                int baseW = _nativeWidth > 0 ? _nativeWidth : _savedWidth;
                int baseH = _nativeHeight > 0 ? _nativeHeight : _savedHeight;
                int w = Mathf.Max(640, Mathf.RoundToInt(baseW * scale));
                int h = Mathf.Max(360, Mathf.RoundToInt(baseH * scale));
                if (w == Screen.width && h == Screen.height) { _appliedResolutionScale = scale; return; }
                Screen.SetResolution(w, h, _savedMode);
                _appliedResolutionScale = scale;
                A11yHost.Diag("LOWPERF 分辨率: 原生 " + baseW + "x" + baseH + " × " + scale.ToString("F2")
                              + " -> " + w + "x" + h + " mode=" + _savedMode);
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
            RestoreExtremeExtras();
            RestoreBlackout();
            RestoreUrp();
            RestoreTrueShadows();
            RestoreParticles();
            RestoreQuality();
            RestoreResolution();
            _appliedUrp = _appliedShadow = _appliedQuality = _appliedParticles = false;
            _appliedBlackout = false;
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
