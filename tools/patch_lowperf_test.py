import io
base = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y'

# ---------- 1) Plugin.cs: TEST_LOWPERF 钩子 ----------
p = base + r'\Plugin.cs'
s = io.open(p, encoding='utf-8').read()

# 字段
anchor = '        // 低性能模式（0.0.0.13）：默认关闭；开关组合键运行中可切；四档各自可以单独关。'
assert anchor in s
s = s.replace(anchor, '        internal static bool TestLowPerf;   // MANOSABA_A11Y_TEST_LOWPERF=1：自动开关一次，供自动化取证\n' + anchor, 1)

# 读环境变量：放在 TestScriptJump 读取处之后
m = s.find('TestScriptJump')
assert m > 0
# 找 TestScriptJump 赋值语句行尾
lineStart = s.rfind('\n', 0, m) + 1
lineEnd = s.find('\n', m) + 1
line = s[lineStart:lineEnd]
s = s[:lineEnd] + '            TestLowPerf = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_LOWPERF") == "1";\n' + s[lineEnd:]

# Update 里调用 TestTick
anchor2 = '            try { LowPerf.Update(); } catch (Exception e) { A11yHost.Diag("LowPerf: " + e.Message); }'
assert anchor2 in s
s = s.replace(anchor2, anchor2 + '\n            try { TestLowPerfTick(); } catch (Exception e) { A11yHost.Diag("LowPerf.Test: " + e.Message); }', 1)

# 新增 TestLowPerfTick 方法（放在 TestKeyInjectTick 之前）
anchor3 = '        private static void TestKeyInjectTick()'
assert anchor3 in s
method = '''        /// <summary>
        /// 自动化取证：40 秒开启低性能模式、75 秒关闭（MANOSABA_A11Y_TEST_LOWPERF=1）。
        /// 为什么需要：本机自动化环境的键盘注入进不了游戏（keybd_event/SendInput 都被挡），
        /// 组合键的真实链路只能靠单元断言（Hotkey.Parse）覆盖，实机行为用这个钩子取证。
        /// </summary>
        private static void TestLowPerfTick()
        {
            if (!TestLowPerf) return;
            float now = Time.realtimeSinceStartup;
            if (_testLowPerfStage == 0 && now >= 40f)
            {
                _testLowPerfStage = 1;
                L.LogInfo("[a11y] TEST LOWPERF: 开启 (t=" + now.ToString("F1") + ")");
                LowPerf.Enable("自动化测试");
            }
            else if (_testLowPerfStage == 1 && now >= 75f)
            {
                _testLowPerfStage = 2;
                L.LogInfo("[a11y] TEST LOWPERF: 关闭 (t=" + now.ToString("F1") + ")");
                LowPerf.Disable("自动化测试");
            }
        }

'''
s = s.replace(anchor3, method + anchor3, 1)

# 静态字段 _testLowPerfStage（挂在 PluginBehaviour 旁边的静态区）：用 Plugin 类里的字段
anchor4 = '        internal static bool TestLowPerf;'
s = s.replace(anchor4, anchor4 + '\n        private static int _testLowPerfStage;', 1)

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('plugin ok:', s.count('TestLowPerf'), s.count('_testLowPerfStage'))

# ---------- 2) SelfTest.cs: 两条断言 ----------
p2 = base + r'\SelfTest.cs'
t = io.open(p2, encoding='utf-8').read()
anchor5 = '        private static void CheckScriptIndex()'
assert anchor5 in t
add = '''        private static void CheckLowPerf()
        {
            Check("lowperf.默认关闭",
                Plugin.CfgLowPerf != null && !Plugin.CfgLowPerf.Value && !LowPerf.Active,
                "CfgLowPerf=" + (Plugin.CfgLowPerf != null ? Plugin.CfgLowPerf.Value.ToString() : "null")
                + " Active=" + LowPerf.Active);

            bool parsed = false, ok = false;
            try
            {
                Hotkey.Spec spec;
                string why;
                parsed = Hotkey.Parse(Plugin.CfgLowPerfToggleKey.Value, out spec, out why);
                ok = parsed && spec.Key == KeyCode.F2 && spec.Alt && spec.Shift;
            }
            catch (Exception e) { ok = false; A11yHost.Diag("SelfTest lowperf: " + e.Message); }
            Check("lowperf.开关键可解析",
                ok,
                Plugin.CfgLowPerfToggleKey.Value + " parsed=" + parsed);
        }

'''
t = t.replace(anchor5, add + anchor5, 1)
# Run() 里调用
t = t.replace('            CheckScriptIndex();', '            CheckScriptIndex();\n            CheckLowPerf();', 1)
io.open(p2, 'w', encoding='utf-8', newline='\n').write(t)
print('selftest ok:', t.count('CheckLowPerf'))
