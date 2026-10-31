import io

mp = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(mp, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:70])
    s = s.replace(old, new, 1)


rep("""        private static bool _testDone;""",
    """        private static bool _testDone;
        private static bool _floorTestDone;
        private static float _floorTestAt;""")

rep("""                if (pick > 0) { Activate(pick); return; }""",
    """                // 测试钩子：MANOSABA_A11Y_TEST_MAPFLOOR=1 时，播报 2 秒后自动切一次楼层，
                // 用来验证「切换后判据变准」（本机键盘注入不可用，见 README 踩坑 #12）。
                if (Plugin.TestMapFloorOnce && !_floorTestDone)
                {
                    if (_floorTestAt <= 0f && _announcedKey.Length > 0) _floorTestAt = Time.unscaledTime + 2f;
                    else if (_floorTestAt > 0f && Time.unscaledTime >= _floorTestAt)
                    {
                        _floorTestDone = true;
                        SwitchFloor(1);
                        return;
                    }
                }

                if (pick > 0) { Activate(pick); return; }""")

rep("""            _testDone = false;""",
    """            _testDone = false;
            _floorTestDone = false;
            _floorTestAt = 0f;""")

io.open(mp, 'w', encoding='utf-8-sig', newline='\n').write(s)

pp = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs'
p = io.open(pp, encoding='utf-8-sig').read()

old = "        internal static bool TestMapChoiceOnce;"
if old not in p:
    raise SystemExit('plugin field not found')
p = p.replace(old, old + "\n        internal static bool TestMapFloorOnce;", 1)

old2 = 'TestMapChoiceOnce = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_MAPCHOICE") != null;'
if old2 in p:
    p = p.replace(old2, old2 + '\n                TestMapFloorOnce = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_MAPFLOOR") == "1";', 1)
else:
    # 找出 Load() 里设置 TestMapChoiceOnce 的那一行
    import re
    m = re.search(r'\n[^\n]*TestMapChoiceOnce[^\n]*\n', p)
    if not m:
        raise SystemExit('TestMapChoiceOnce assignment not found')
    line = m.group(0)
    p = p.replace(line, line.rstrip('\n') + '\n                TestMapFloorOnce = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_MAPFLOOR") == "1";\n', 1)

io.open(pp, 'w', encoding='utf-8-sig', newline='\n').write(p)
print('test floor hook added')
