import io

mp = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(mp, encoding='utf-8-sig').read()

old = """                if (Plugin.TestMapFloorOnce && !_floorTestDone)
                {
                    if (_floorTestAt <= 0f && _announcedKey.Length > 0) { _floorTestAt = Time.unscaledTime + 2f; return; }
                    else if (_floorTestAt > 0f && Time.unscaledTime >= _floorTestAt)
                    {
                        _floorTestDone = true;
                        SwitchFloor(1);
                    }
                    return;   // 等楼层切换完成前不要抢着选（否则 pick 会在切层前一帧就执行）
                }"""
new = """                if (Plugin.TestMapFloorCount > 0 && _floorSwitchCount < Plugin.TestMapFloorCount)
                {
                    if (_floorTestAt <= 0f && _announcedKey.Length > 0) { _floorTestAt = Time.unscaledTime + 2f; return; }
                    if (Time.unscaledTime >= _floorTestAt)
                    {
                        _floorSwitchCount++;
                        SwitchFloor(1);
                        _floorTestAt = Time.unscaledTime + 4f;   // 给游戏一帧多点时间刷新楼层
                    }
                    return;   // 切层没做完之前不要抢着选
                }"""
if old not in s:
    raise SystemExit('floor hook block not found')
s = s.replace(old, new, 1)
s = s.replace('        private static bool _floorTestDone;', '        private static int _floorSwitchCount;', 1)
s = s.replace('            _floorTestDone = false;', '            _floorSwitchCount = 0;', 1)
io.open(mp, 'w', encoding='utf-8-sig', newline='\n').write(s)

pp = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs'
t = io.open(pp, encoding='utf-8-sig').read()
t = t.replace('        internal static bool TestMapFloorOnce;', '        internal static int TestMapFloorCount;', 1)

old2 = """            if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_MAPFLOOR") == "1")
            {
                TestMapFloorOnce = true;
                L.LogInfo("[a11y] TEST MAPFLOOR MODE: 播报后自动切换一次楼层");
            }"""
new2 = """            int mapFloor;
            if (int.TryParse(Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_MAPFLOOR"), out mapFloor) && mapFloor > 0)
            {
                TestMapFloorCount = mapFloor;
                L.LogInfo("[a11y] TEST MAPFLOOR MODE: 播报后自动切换 " + mapFloor + " 次楼层");
            }"""
if old2 not in t:
    raise SystemExit('plugin floor env block not found')
t = t.replace(old2, new2, 1)
io.open(pp, 'w', encoding='utf-8-sig', newline='\n').write(t)
print('patched N-switch test hook')
