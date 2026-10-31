import io

mp = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(mp, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:70])
    s = s.replace(old, new, 1)


rep("""        private static float _choiceDiagAt;""",
    """        private static float _choiceDiagAt;
        private static bool _kindTestDone;""")

rep("""        private static bool _kindTestDone;""",
    """        private static bool _kindTestDone;
        /// <summary>测试钩子：直接按 AreaKind 点某个地点（不受「当前楼层」过滤影响）。</summary>
        internal static void TestPickKind(string kind)
        {
            if (_kindTestDone || _scope == null || string.IsNullOrEmpty(kind)) return;
            try
            {
                WitchTrials.Views.MapArea[] areas = _scope.GetComponentsInChildren<WitchTrials.Views.MapArea>(true);
                if (areas == null) return;
                for (int i = 0; i < areas.Length; i++)
                {
                    WitchTrials.Views.MapArea a = areas[i];
                    if (a == null || a._areaKind.ToString() != kind) continue;
                    _kindTestDone = true;
                    A11yHost.Diag("MAPCHOICE 测试：按 kind 点击 " + kind);
                    _choiceDiagAt = Time.unscaledTime + 2f;
                    WitchTrials.Views.MapAreaFocusButton fb = a._focusButton;
                    if (fb != null) { Repeat.Say(SafeKind(a)); fb.onClick.Invoke(); }
                    return;
                }
            }
            catch (Exception e) { A11yHost.Diag("MapChoice.TestPickKind: " + e.Message); }
        }""")

rep("""                if (pick > 0) { Activate(pick); return; }""",
    """                if (!string.IsNullOrEmpty(Plugin.TestMapPickKind) && !_kindTestDone)
                {
                    TestPickKind(Plugin.TestMapPickKind);
                    return;
                }

                if (pick > 0) { Activate(pick); return; }""")

rep("""            _choiceDiagAt = 0f;""",
    """            _choiceDiagAt = 0f;""")

rep("""            _floorSwitchCount = 0;""",
    """            _floorSwitchCount = 0;
            _kindTestDone = false;""")

io.open(mp, 'w', encoding='utf-8-sig', newline='\n').write(s)

pp = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs'
t = io.open(pp, encoding='utf-8-sig').read()
anchor = '        internal static int TestMapFloorCount;'
if anchor not in t:
    raise SystemExit('plugin anchor missing')
t = t.replace(anchor, anchor + '\n        /// <summary>MANOSABA_A11Y_TEST_MAPPICK_KIND：按 AreaKind 直接点某个地点（不受楼层过滤）。</summary>\n        internal static string TestMapPickKind;', 1)

anchor2 = '            // 地图扫描诊断（仅 MANOSABA_A11Y_MAPDIAG=1）'
if anchor2 not in t:
    raise SystemExit('plugin env anchor missing')
t = t.replace(anchor2, '            TestMapPickKind = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_MAPPICK_KIND");\n' + anchor2, 1)
io.open(pp, 'w', encoding='utf-8-sig', newline='\n').write(t)
print('kind-pick test hook added')
