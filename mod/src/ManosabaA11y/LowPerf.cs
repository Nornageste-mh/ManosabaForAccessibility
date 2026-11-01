using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;

namespace ManosabaA11y
{
    /// <summary>
    /// 低性能模式（默认关闭）：给弱机器 / 小内存机器让画面轻一点、插件自己少花一点。
    ///
    /// 为什么要有「档位」而不是一键全开：
    ///   0.0.0.12 的实测（`PerfProbe`，本机 ADV 场景）说明三件事：
    ///   1. 这台机器是**主线程**瓶颈，不是显卡瓶颈——把 renderScale 降到 0.8、关 MSAA、
    ///      关阴影、关掉 581 个 TrueShadow，帧率都在同一噪声区间（均值≈8 FPS）；
    ///   2. 插件自己每帧原来要花 10~13ms（`ev≈5.7ms` + `screen≈3~10ms`），
    ///      限频后降到 2.5~5ms —— 这部分**没有任何视觉代价**，所以它在低性能模式里是
    ///      「必做且默认开」的；
    ///   3. 画面类开关（URP 画质 / TrueShadow / 分辨率）在**别的机器**上可能有用，
    ///      在这台机器上没测出收益，所以它们做成可以单独关掉的档位，默认全开但可退。
    ///
    /// 无障碍纪律：
    ///   · 不自动改画面：只有玩家打开「低性能模式」或按下组合键才动设置；
    ///   · 低帧率只**提醒**（播报「按 Alt+Shift+F2 可开启低性能模式」），绝不擅自切换；
    ///   · 关闭模式 / 插件卸载时把改过的设置**还原**（保存原始值）。
    /// </summary>
    internal static class LowPerf
    {
        private const string HotkeyName = "Alt+Shift+F2";

        internal static bool Active { get; private set; }

        // ---- 保存的原始值（关闭/卸载时还原）----
        private static bool _saved;
        private static float _savedRenderScale = 1f;
        private static int _savedMsaa = 4;
        private static bool _savedMainShadows = true;
        private static bool _savedSoftShadows = true;
        private static float _savedShadowDistance = 150f;
        private static int _savedCascade = 4;
        private static int _savedAdditionalLights = 8;
        private static int _savedWidth, _savedHeight;
        private static FullScreenMode _savedMode;
        private static bool _savedResolution;

        // ---- 被我们关掉的柔边阴影组件（还原时逐个打开）----
        private static readonly List<LeTai.TrueShadow.TrueShadow> _disabledShadows =
            new List<LeTai.TrueShadow.TrueShadow>();

        // ---- 低帧率提醒 ----
        private static float _fpsWindowStart = -1f;
        private static int _fpsFrames;
        private static float _lastRemindAt = -600f;
        private static bool _remindedThisSession;

        private static float _nextShadowSweepAt;
        private static bool _warnedUrp;

        // ================================================================== 主循环

        internal static void Update()
        {
            try
            {
                // 组合键：开启 / 关闭（只在配置允许时响应；改画面属于玩家决策）
                if (Plugin.CfgLowPerfToggleKey != null
                    && Plugin.SpecDown(Plugin.CfgLowPerfToggleKey.Value))
                {
                    if (Active) Disable("玩家按键"); else Enable("玩家按键");
                    return;
                }

                // 模式开着：新出现的柔边阴影要跟着降级（场景切换会重建 UI/立绘）
                if (Active && Plugin.CfgLowPerfNoShadow != null && Plugin.CfgLowPerfNoShadow.Value
                    && Time.unscaledTime >= _nextShadowSweepAt)
                {
                    _nextShadowSweepAt = Time.unscaledTime + 3f;
                    DisableTrueShadows(false);
                }

                MeasureFps();
            }
            catch (Exception e) { A11yHost.Diag("LowPerf: " + e.Message); }
        }

        /// <summary>开局按配置应用一次（配置默认 false，所以正常玩家不会走到）。</summary>
        internal static void ApplyFromConfig()
        {
            try
            {
                if (Plugin.CfgLowPerf != null && Plugin.CfgLowPerf.Value) Enable("配置项");
            }
            catch (Exception e) { A11yHost.Diag("LowPerf: 配置应用失败 " + e.Message); }
        }

        // ================================================================== 开关

        internal static void Enable(string why)
        {
            if (Active) return;
            Active = true;
            try
            {
                if (Plugin.CfgLowPerfDegradeUrp != null && Plugin.CfgLowPerfDegradeUrp.Value) DegradeUrp();
                if (Plugin.CfgLowPerfNoShadow != null && Plugin.CfgLowPerfNoShadow.Value) DisableTrueShadows(true);
                if (Plugin.CfgLowPerfLowerResolution != null && Plugin.CfgLowPerfLowerResolution.Value) LowerResolution();
                A11yHost.Diag("LOWPERF 已开启（" + why + "）：urp=" + Bool(Plugin.CfgLowPerfDegradeUrp)
                              + " noShadow=" + Bool(Plugin.CfgLowPerfNoShadow)
                              + " lowRes=" + Bool(Plugin.CfgLowPerfLowerResolution));
                Repeat.Say("低性能模式已开启。", true);
            }
            catch (Exception e)
            {
                A11yHost.Diag("LOWPERF 开启失败: " + e.Message);
                Repeat.Say("低性能模式开启时出错，请查看日志。", true);
            }
        }

        internal static void Disable(string why)
        {
            if (!Active) return;
            Active = false;
            try
            {
                Restore();
                A11yHost.Diag("LOWPERF 已关闭（" + why + "）：设置已还原");
                Repeat.Say("低性能模式已关闭。", true);
            }
            catch (Exception e)
            {
                A11yHost.Diag("LOWPERF 关闭失败: " + e.Message);
                Repeat.Say("低性能模式关闭时出错，请查看日志。", true);
            }
        }

        /// <summary>插件卸载时兜底还原（别把玩家的画面设置留在降级状态）。</summary>
        internal static void Shutdown()
        {
            try { if (Active) { Active = false; Restore(); } }
            catch { }
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

        private static void DegradeUrp()
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

            if (!_saved)
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
                    _saved = true;
                }
                catch (Exception e) { A11yHost.Diag("LOWPERF: 保存原始画质失败 " + e.Message); }
            }

            Try(() => u.renderScale = 0.8f, "renderScale");
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
            if (!_saved) return;
            var u = Urp();
            if (u == null) return;
            Try(() => u.renderScale = _savedRenderScale, "renderScale");
            Try(() => u.msaaSampleCount = _savedMsaa, "msaa");
            Try(() => u.supportsMainLightShadows = _savedMainShadows, "mainShadows");
            Try(() => u.supportsSoftShadows = _savedSoftShadows, "softShadows");
            Try(() => u.shadowDistance = _savedShadowDistance, "shadowDistance");
            Try(() => u.shadowCascadeCount = _savedCascade, "cascade");
            Try(() => u.maxAdditionalLightsCount = _savedAdditionalLights, "additionalLights");
            _saved = false;
            A11yHost.Diag("LOWPERF URP: 已还原画质设置");
        }

        // ================================================================== TrueShadow

        /// <summary>
        /// 关掉柔边阴影（LeTai.TrueShadow）。本作实测有 805 个组件——它是 UI/立绘的
        /// 多趟模糊，属于 GPU 侧的固定成本。
        /// 注意：视觉上立绘与文字的柔边阴影会消失，对比度会变，所以在配置里可以单独关掉这项。
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

        // ================================================================== 分辨率

        /// <summary>
        /// 降低渲染分辨率（默认关闭！）。它会改变窗口大小/清晰度，是最"看得出来"的一档，
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
            RestoreResolution();
        }

        // ================================================================== 低帧率提醒

        /// <summary>
        /// 低帧率只**提醒**，绝不自动切换（玩家可能正在别的窗口、也可能在乎画面）。
        /// 规则：每 30 秒算一次平均帧率；低于 15 且模式没开，则每 10 分钟最多提醒一次。
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
            A11yHost.Diag("LOWPERF 低帧率提醒: fps=" + fps.ToString("F1") + " 未开启低性能模式");
            Repeat.Say("当前帧率较低，约 " + Mathf.RoundToInt(fps)
                       + " 帧。按 " + (Plugin.CfgLowPerfToggleKey != null ? Plugin.CfgLowPerfToggleKey.Value : HotkeyName)
                       + " 可以开启低性能模式。", true);
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

        private static string Bool(ConfigEntry<bool> c)
        {
            try { return c != null ? c.Value.ToString() : "?"; } catch { return "?"; }
        }
    }
}
