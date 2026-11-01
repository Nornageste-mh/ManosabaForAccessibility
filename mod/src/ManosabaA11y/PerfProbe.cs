using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ManosabaA11y
{
    /// <summary>
    /// 性能取证探针（仅 <c>MANOSABA_A11Y_PERFDIAG=1</c> 时工作）。
    ///
    /// 用途：在做「低性能模式」之前，先量清楚三件事，避免凭想象优化：
    ///   1. 游戏本身的帧率与关键设置（URP 渲染管线、MSAA、HDR、renderScale、阴影等）；
    ///   2. 场景里有多少「已知昂贵」的东西（TrueShadow 柔边阴影、Volume 后处理、
    ///      粒子、Canvas、骨骼蒙皮网格）；
    ///   3. **我们自己的开销**：每个子系统每帧花了多少毫秒
    ///      （`ScreenAssist` 每 0.5 秒扫一次全场景 Transform，这在 6 FPS 的机器上不便宜）。
    ///
    /// 输出格式（便于 grep）：
    ///   PERF static ...        —— 一次性环境信息
    ///   PERF 10s fps=… frameMs=… uinav=…ms screen=…ms reader=…ms choices=…ms map=…ms ev=…ms misc=…ms
    ///   PERF counts shadow=… volume=… particles=… canvases=… skinned=… renderers=…
    /// </summary>
    internal static class PerfProbe
    {
        private static bool _staticDumped;
        private static float _windowStart = -1f;
        private static int _frames;
        private static float _frameTimeSum;
        // 卡顿（spike）统计：平均帧率看不出 CG/立绘出现时的顿挫，必须单独数「长帧」。
        private static float _maxFrameMs;
        private static int _framesOver100, _framesOver200, _gc0, _gc1, _gc2;

        // 各子系统累计耗时（毫秒）与本窗口样本数
        private static readonly Dictionary<string, double> _costMs = new Dictionary<string, double>();
        private static readonly Dictionary<string, int> _costN = new Dictionary<string, int>();
        private static readonly Stopwatch _sw = new Stopwatch();

        internal static void BeginCost(string name)
        {
            if (!Plugin.CfgPerfDiag) return;
            try { _sw.Restart(); } catch { }
        }

        internal static void EndCost(string name)
        {
            if (!Plugin.CfgPerfDiag) return;
            try
            {
                _sw.Stop();
                double ms = _sw.Elapsed.TotalMilliseconds;
                double cur;
                _costMs.TryGetValue(name, out cur);
                _costMs[name] = cur + ms;
                int n;
                _costN.TryGetValue(name, out n);
                _costN[name] = n + 1;
            }
            catch { }
        }

        internal static void Tick()
        {
            if (!Plugin.CfgPerfDiag) return;
            try
            {
                if (!_staticDumped) { _staticDumped = true; DumpStatic(); }

                float now = Time.realtimeSinceStartup;
                if (_windowStart < 0f) _windowStart = now;
                _frames++;
                float dtMs = Time.unscaledDeltaTime * 1000f;
                _frameTimeSum += Time.unscaledDeltaTime;
                if (dtMs > _maxFrameMs) _maxFrameMs = dtMs;
                if (dtMs > 100f) _framesOver100++;
                if (dtMs > 200f) _framesOver200++;

                if (now - _windowStart < 10f) return;

                float span = now - _windowStart;
                float fps = span > 0f ? _frames / span : 0f;
                float frameMs = _frames > 0 ? (_frameTimeSum / _frames) * 1000f : 0f;

                int g0 = 0, g1 = 0, g2 = 0;
                try { g0 = GC.CollectionCount(0); g1 = GC.CollectionCount(1); g2 = GC.CollectionCount(2); } catch { }
                A11yHost.Diag("PERF 10s fps=" + fps.ToString("F1")
                              + " frameMs=" + frameMs.ToString("F1")
                              + " maxMs=" + _maxFrameMs.ToString("F0")
                              + " slow>100=" + _framesOver100
                              + " slow>200=" + _framesOver200
                              + " gc0=+" + (g0 - _gc0) + " gc1=+" + (g1 - _gc1) + " gc2=+" + (g2 - _gc2)
                              + " costs=" + CostSummary()
                              + " focused=" + Application.isFocused);
                _gc0 = g0; _gc1 = g1; _gc2 = g2;
                _maxFrameMs = 0f; _framesOver100 = 0; _framesOver200 = 0;

                DumpCounts();
                DumpUrp();

                _windowStart = now;
                _frames = 0;
                _frameTimeSum = 0f;
                _costMs.Clear();
                _costN.Clear();
            }
            catch (Exception e) { A11yHost.Diag("PerfProbe: " + e.Message); }
        }

        private static string CostSummary()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var kv in _costMs)
            {
                int n;
                _costN.TryGetValue(kv.Key, out n);
                if (n <= 0) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(kv.Key).Append('=').Append((kv.Value / n).ToString("F2")).Append("ms");
            }
            return sb.Length == 0 ? "<none>" : sb.ToString();
        }

        // ================================================================ 实验（只用于取证）
        // 仅 MANOSABA_A11Y_PERFTEST=1：依次施加「低性能模式」候选设置，用前后帧率对比来判断
        // 哪些真的有用。**这个开关不进正式功能**，正式实现等数据出来再和用户确认。
        private static bool _testApplied1;
        private static bool _testApplied2;
        private static float _testStart = -1f;

        internal static void TestTick()
        {
            if (!Plugin.CfgPerfTest) return;
            try
            {
                float now = Time.realtimeSinceStartup;
                if (_testStart < 0f) _testStart = now;

                // 第 70 秒：URP 降级（MSAA/阴影/renderScale）
                if (!_testApplied1 && now - _testStart > 70f)
                {
                    _testApplied1 = true;
                    A11yHost.Diag("PERF TEST 阶段1：URP 降级（renderScale 0.8 / MSAA 关 / 主光阴影关 / 软阴影关 / shadowDist 0 / cascade 1 / addLights 0）");
                    var u = Urp();
                    if (u != null)
                    {
                        try { u.renderScale = 0.8f; } catch (Exception e) { A11yHost.Diag("PERF TEST renderScale: " + e.Message); }
                        try { u.msaaSampleCount = 1; } catch (Exception e) { A11yHost.Diag("PERF TEST msaa: " + e.Message); }
                        try { u.supportsMainLightShadows = false; } catch (Exception e) { A11yHost.Diag("PERF TEST mainShadow: " + e.Message); }
                        try { u.supportsSoftShadows = false; } catch (Exception e) { A11yHost.Diag("PERF TEST softShadow: " + e.Message); }
                        try { u.shadowDistance = 0f; } catch (Exception e) { A11yHost.Diag("PERF TEST shadowDist: " + e.Message); }
                        try { u.shadowCascadeCount = 1; } catch (Exception e) { A11yHost.Diag("PERF TEST cascade: " + e.Message); }
                        try { u.maxAdditionalLightsCount = 0; } catch (Exception e) { A11yHost.Diag("PERF TEST addLights: " + e.Message); }
                    }
                    else A11yHost.Diag("PERF TEST 阶段1：拿不到 URP 资产");
                }

                // 第 140 秒：关掉全部 TrueShadow（柔边阴影）
                if (!_testApplied2 && now - _testStart > 140f)
                {
                    _testApplied2 = true;
                    int n = DisableTrueShadows();
                    A11yHost.Diag("PERF TEST 阶段2：关闭 TrueShadow 组件 " + n + " 个");
                }
            }
            catch (Exception e) { A11yHost.Diag("PerfProbe test: " + e.Message); }
        }

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

        private static int DisableTrueShadows()
        {
            int n = 0;
            try
            {
                var all = Resources.FindObjectsOfTypeAll<LeTai.TrueShadow.TrueShadow>();
                for (int i = 0; i < all.Length; i++)
                {
                    try
                    {
                        if (all[i] != null && all[i].enabled) { all[i].enabled = false; n++; }
                    }
                    catch { }
                }
            }
            catch (Exception e) { A11yHost.Diag("PERF TEST TrueShadow: " + e.Message); }
            return n;
        }

        private static void DumpStatic()
        {
            try
            {
                A11yHost.Diag("PERF static unity=" + Application.unityVersion
                              + " platform=" + Application.platform
                              + " screen=" + Screen.width + "x" + Screen.height
                              + " fullscreen=" + Screen.fullScreen
                              + " mode=" + Screen.fullScreenMode
                              + " targetFrameRate=" + Application.targetFrameRate
                              + " vSync=" + QualitySettings.vSyncCount
                              + " qualityLevel=" + QualitySettings.GetQualityLevel()
                              + " qualityNames=[" + string.Join(", ", QualitySettings.names) + "]"
                              + " aa=" + QualitySettings.antiAliasing
                              + " texLimit=" + QualitySettings.masterTextureLimit
                              + " shadowDist=" + QualitySettings.shadowDistance.ToString("F0")
                              + " realtimeShadow=" + QualitySettings.realtimeReflectionProbes);

                A11yHost.Diag("PERF device cpu=" + SystemInfo.processorType + " cores=" + SystemInfo.processorCount
                              + " ramMB=" + SystemInfo.systemMemorySize
                              + " gpu=" + SystemInfo.graphicsDeviceName
                              + " vramMB=" + SystemInfo.graphicsMemorySize
                              + " shaderLevel=" + SystemInfo.graphicsShaderLevel
                              + " tier=" + Graphics.activeTier);

                var cam = Camera.main;
                if (cam == null) { A11yHost.Diag("PERF camera=<none>"); }
                else
                {
                    A11yHost.Diag("PERF camera name=" + cam.name
                                  + " hdr=" + cam.allowHDR + " msaa=" + cam.allowMSAA
                                  + " dynRes=" + cam.allowDynamicResolution
                                  + " depth=" + cam.depthTextureMode
                                  + " px=" + cam.pixelWidth + "x" + cam.pixelHeight);
                }
            }
            catch (Exception e) { A11yHost.Diag("PerfProbe static: " + e.Message); }
        }

        private static void DumpUrp()
        {
            try
            {
                var asset = GraphicsSettings.currentRenderPipeline;
                if (asset == null) { A11yHost.Diag("PERF urp=<none>（可能是内置管线）"); return; }
                var u = asset.TryCast<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>();
                if (u == null)
                {
                    A11yHost.Diag("PERF urp type=" + asset.GetType().Name + "（不是 URP 资产）");
                    return;
                }
                A11yHost.Diag("PERF urp renderScale=" + u.renderScale.ToString("F2")
                              + " msaa=" + u.msaaSampleCount
                              + " hdr=" + u.supportsHDR
                              + " mainShadow=" + u.supportsMainLightShadows
                              + " softShadow=" + u.supportsSoftShadows
                              + " shadowDist=" + u.shadowDistance.ToString("F0")
                              + " cascade=" + u.shadowCascadeCount
                              + " shadowRes=" + u.mainLightShadowmapResolution
                              + " addLights=" + u.maxAdditionalLightsCount
                              + " srpBatcher=" + u.useSRPBatcher);
            }
            catch (Exception e) { A11yHost.Diag("PerfProbe urp: " + e.Message); }
        }

        private static void DumpCounts()
        {
            try
            {
                int shadow = 0, volume = 0, particles = 0, skinned = 0, renderers = 0, canvases = 0;
                // TrueShadow：本作立绘/UI 的柔边阴影，每个都会多几次模糊 pass
                try { shadow = Resources.FindObjectsOfTypeAll<LeTai.TrueShadow.TrueShadow>().Length; } catch { }
                try { volume = Resources.FindObjectsOfTypeAll<UnityEngine.Rendering.Volume>().Length; } catch { }
                try
                {
                    var ps = Resources.FindObjectsOfTypeAll<ParticleSystem>();
                    for (int i = 0; i < ps.Length; i++)
                    {
                        var p = ps[i];
                        if (p != null && p.gameObject != null && p.gameObject.activeInHierarchy) particles++;
                    }
                }
                catch { }
                try
                {
                    var sk = Resources.FindObjectsOfTypeAll<SkinnedMeshRenderer>();
                    for (int i = 0; i < sk.Length; i++)
                    {
                        var s = sk[i];
                        if (s != null && s.gameObject != null && s.gameObject.activeInHierarchy) skinned++;
                    }
                }
                catch { }
                try
                {
                    var rs = Resources.FindObjectsOfTypeAll<Renderer>();
                    for (int i = 0; i < rs.Length; i++)
                    {
                        var r = rs[i];
                        if (r != null && r.gameObject != null && r.gameObject.activeInHierarchy) renderers++;
                    }
                }
                catch { }
                try
                {
                    var cs = Resources.FindObjectsOfTypeAll<Canvas>();
                    for (int i = 0; i < cs.Length; i++)
                    {
                        var c = cs[i];
                        if (c != null && c.gameObject != null && c.gameObject.activeInHierarchy) canvases++;
                    }
                }
                catch { }
                A11yHost.Diag("PERF counts shadow=" + shadow + " volume=" + volume + " particles=" + particles
                              + " skinned=" + skinned + " renderers=" + renderers + " canvases=" + canvases);
            }
            catch (Exception e) { A11yHost.Diag("PerfProbe counts: " + e.Message); }
        }
    }
}
