import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/PerfProbe.cs"
s = io.open(p, encoding="utf-8-sig").read()

anchor = "        private static void DumpStatic()"
add = """        // ================================================================ 实验（只用于取证）
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

        private static void DumpStatic()"""
assert s.count(anchor) == 1
s = s.replace(anchor, add)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)

# Plugin: CfgPerfTest 字段 + env + 调用
p2 = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/Plugin.cs"
t = io.open(p2, encoding="utf-8-sig").read()
t = t.replace("""        internal static bool CfgPerfDiag;""",
"""        internal static bool CfgPerfDiag;

        /// <summary>性能实验（仅 MANOSABA_A11Y_PERFTEST=1）：依次施加候选优化并看帧率变化。只用于取证。</summary>
        internal static bool CfgPerfTest;""", 1)
t = t.replace("""                CfgPerfDiag = true;
                A11yHost.Diag("PERFDIAG=1（只测量，不改任何设置）");""",
"""                CfgPerfDiag = true;
                A11yHost.Diag("PERFDIAG=1（只测量，不改任何设置）");
                if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_PERFTEST") == "1")
                {
                    CfgPerfTest = true;
                    A11yHost.Diag("PERFTEST=1（会改动画面设置做对照实验，仅取证用）");
                }""", 1)
t = t.replace("""            try { PerfProbe.Tick(); }
            catch (Exception e) { A11yHost.Diag("PerfProbe: " + e.Message); }""",
"""            try { PerfProbe.Tick(); }
            catch (Exception e) { A11yHost.Diag("PerfProbe: " + e.Message); }
            try { PerfProbe.TestTick(); }
            catch (Exception e) { A11yHost.Diag("PerfProbe.Test: " + e.Message); }""", 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(t)
print("perf test hook added")
