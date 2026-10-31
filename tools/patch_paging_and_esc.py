import io

# --- 1) MapChoice: 把 pick 换算成全局序号（分页后必须）
p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()
old = '                if (pick > 0) { Activate(pick); return; }'
new = '                if (pick > 0) { Activate(firstOnPage + pick); return; }'
if old not in s:
    raise SystemExit('activate call not found')
s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)

# --- 2) ScreenAssist.SimulateEscape：地图选择态不许关（否则 await 挂断＝软锁）
p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\ScreenAssist.cs'
s = io.open(p, encoding='utf-8-sig').read()
old = '''        internal static bool SimulateEscape()
        {
            if (_active == null) return false;
            CloseScreen(_active, _activeTitle);
            return true;
        }'''
new = '''        internal static bool SimulateEscape()
        {
            if (_active == null) return false;
            // 和真实 Esc 一样要有「正在选地点就不许关」的保护：
            // MapPage.OnChoiceMapAreaAsync 正在 await 点击，关掉手册会让剧情永远等不到结果。
            if (MapChoice.HasChoice)
            {
                A11yHost.Diag("ScreenAssist: 模拟 Esc 被拦住（正在选择地点）");
                Repeat.Say("请先选择一个地点。");
                return false;
            }
            CloseScreen(_active, _activeTitle);
            return true;
        }'''
if old not in s:
    raise SystemExit('simulate escape not found')
s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)

# --- 3) Suite: map.choice 加 TEST_ESC（剧情会自己打开线索页挡路，玩家是 Esc 关掉继续）
p = r'D:\DSHWorkSpace\manosaba_a11y\tests\Suite.ps1'
s = io.open(p, encoding='utf-8-sig').read()
old = "Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv25'; MANOSABA_A11Y_TEST_MAPCHOICE = '7' }"
new = ("Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv25';\n"
       "                 MANOSABA_A11Y_TEST_MAPCHOICE = '7'; MANOSABA_A11Y_TEST_ESC = '1' }")
if old not in s:
    raise SystemExit('suite env not found')
s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('paging fix + simulate-escape guard + suite env')
