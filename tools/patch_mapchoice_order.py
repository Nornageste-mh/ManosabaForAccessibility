import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()

floor_block = """                // 测试钩子：MANOSABA_A11Y_TEST_MAPFLOOR=1 时，播报 2 秒后自动切一次楼层，
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

"""
if floor_block not in s:
    raise SystemExit('floor block not found')
s = s.replace(floor_block, '', 1)

anchor = """                if (pick == 0 && Plugin.TestMapChoiceOnce && !_testDone && _announcedKey.Length > 0)"""
if anchor not in s:
    raise SystemExit('pick hook anchor not found')
s = s.replace(anchor, floor_block + anchor, 1)

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('moved floor test hook before pick hook')
