import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:70])
    s = s.replace(old, new, 1)


rep("""        private static float _floorTestAt;""",
    """        private static float _floorTestAt;
        private static float _choiceDiagAt;""")

rep("""                if (pick > 0) { Activate(pick); return; }""",
    """                // 选完 1.5 秒后把游戏自己的 chosenAreaId / chosenAreaFlag 打出来：
                // 这是判断「我们的点击到底被游戏接受成了哪个地点」的唯一权威信号。
                if (_choiceDiagAt > 0f && Time.unscaledTime >= _choiceDiagAt)
                {
                    _choiceDiagAt = 0f;
                    DumpChosenVariables();
                }

                if (pick > 0) { Activate(pick); return; }""")

rep("""            _floorTestAt = 0f;""",
    """            _floorTestAt = 0f;
            _choiceDiagAt = 0f;""")

rep("""            A11yHost.Diag("MAPCHOICE 数字键 " + pick + " -> " + e.Name);""",
    """            A11yHost.Diag("MAPCHOICE 数字键 " + pick + " -> " + e.Name + "（kind="
                          + (e.Area == null ? "?" : SafeKind(e.Area)) + "）");
            _choiceDiagAt = Time.unscaledTime + 1.5f;""")

rep("""        // ------------------------------------------------------------------ 测试钩子""",
    """        // ------------------------------------------------------------------ 诊断
        private static string SafeKind(WitchTrials.Views.MapArea a)
        {
            try { return a._areaKind.ToString(); } catch { return "?"; }
        }

        private static void DumpChosenVariables()
        {
            try
            {
                Naninovel.ICustomVariableManager vars =
                    Naninovel.Engine.GetService<Naninovel.ICustomVariableManager>();
                if (vars == null) { A11yHost.Diag("MAPCHOICE 变量: 没有 ICustomVariableManager"); return; }
                string id = "-";
                string flag = "-";
                try { if (vars.VariableExists("chosenAreaId")) id = vars.GetVariableValue("chosenAreaId").ToString(); }
                catch (Exception e) { id = "err:" + e.Message; }
                try { if (vars.VariableExists("chosenAreaFlag")) flag = vars.GetVariableValue("chosenAreaFlag").ToString(); }
                catch (Exception e) { flag = "err:" + e.Message; }
                A11yHost.Diag("MAPCHOICE 变量: chosenAreaId=[" + id + "] chosenAreaFlag=[" + flag + "]");
            }
            catch (Exception e) { A11yHost.Diag("MAPCHOICE 变量: " + e.Message); }
        }

        // ------------------------------------------------------------------ 测试钩子""")

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('added chosenArea diagnostics')
