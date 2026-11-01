import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()

def rep(old, new, cnt=1):
    global s
    assert s.count(old) == cnt, "count %d for %r" % (s.count(old), old[:60])
    s = s.replace(old, new)

# 1) 配置字段（放在 CfgAllowScriptJump 之后）
rep("        internal static ConfigEntry<bool> CfgAllowScriptJump;",
    """        internal static ConfigEntry<bool> CfgAllowScriptJump;

        /// <summary>性能取证日志（仅 MANOSABA_A11Y_PERFDIAG=1 或配置打开）：不做任何优化，只测量。</summary>
        internal static bool CfgPerfDiag;""")

# 2) 环境变量读取（放在 TEST_SCRIPTJUMP 之前）
rep("""            // 自动化：从插件内部驱动「剧情跳转」菜单（仅 MANOSABA_A11Y_TEST_SCRIPTJUMP=1）""",
    """            // 性能取证（仅 MANOSABA_A11Y_PERFDIAG=1）：量帧率、渲染设置、场景昂贵对象与自己的开销。
            // 只测量、不改任何设置 —— 「低性能模式」要不要做、做什么，先看这份数据。
            if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_PERFDIAG") == "1")
            {
                CfgPerfDiag = true;
                A11yHost.Diag("PERFDIAG=1（只测量，不改任何设置）");
            }

            // 自动化：从插件内部驱动「剧情跳转」菜单（仅 MANOSABA_A11Y_TEST_SCRIPTJUMP=1）""")

# 3) 更新循环里插计时 + 末尾 Tick
rep("""            try { if (A11yHost.TickUiNav != null) A11yHost.TickUiNav(); }
            catch (Exception e) { A11yHost.Diag("Tick: " + e.Message); }""",
    """            PerfProbe.BeginCost("uinav");
            try { if (A11yHost.TickUiNav != null) A11yHost.TickUiNav(); }
            catch (Exception e) { A11yHost.Diag("Tick: " + e.Message); }
            PerfProbe.EndCost("uinav");""")

rep("""            // 剧情文本朗读队列（消息级钩子只登记，这里到点统一念，避免一句念两遍）
            try { Reader.Tick(); }
            catch (Exception e) { A11yHost.Diag("Reader.Tick: " + e.Message); }""",
    """            // 剧情文本朗读队列（消息级钩子只登记，这里到点统一念，避免一句念两遍）
            PerfProbe.BeginCost("reader");
            try { Reader.Tick(); }
            catch (Exception e) { A11yHost.Diag("Reader.Tick: " + e.Message); }
            PerfProbe.EndCost("reader");""")

rep("""            try { Evidence.Tick(); } catch (Exception e) { A11yHost.Diag("Evidence: " + e.Message); }""",
    """            PerfProbe.BeginCost("ev");
            try { Evidence.Tick(); } catch (Exception e) { A11yHost.Diag("Evidence: " + e.Message); }
            PerfProbe.EndCost("ev");""")

rep("""            try { ScreenAssist.Update(); }
            catch (Exception e) { A11yHost.Diag("ScreenAssist: " + e.Message); }""",
    """            PerfProbe.BeginCost("screen");
            try { ScreenAssist.Update(); }
            catch (Exception e) { A11yHost.Diag("ScreenAssist: " + e.Message); }
            PerfProbe.EndCost("screen");

            // 性能取证探针（仅 MANOSABA_A11Y_PERFDIAG=1）：只测量不改设置
            try { PerfProbe.Tick(); }
            catch (Exception e) { A11yHost.Diag("PerfProbe: " + e.Message); }""")

io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Plugin.cs perf wiring done")
