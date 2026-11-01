import io
p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs'
s = io.open(p, encoding='utf-8').read()
bad = '''            if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_SCRIPTJUMP") == "1")
            TestLowPerf = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_LOWPERF") == "1";
            {'''
assert bad in s, 'anchor not found'
good = '''            if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_SCRIPTJUMP") == "1")
            {'''
s = s.replace(bad, good, 1)

# 独立语句：插在 SCRIPTJUMP 块结束后（找该块的收尾 “\n            }\n” 后）
i = s.index('L.LogInfo("[a11y] TEST SCRIPTJUMP: 45s')
j = s.index('\n            }\n', i) + len('\n            }\n')
s = s[:j] + '''            // 低性能模式取证（仅 MANOSABA_A11Y_TEST_LOWPERF=1）：40s 自动开启、75s 自动关闭。
            TestLowPerf = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_LOWPERF") == "1";
            if (TestLowPerf) L.LogInfo("[a11y] TEST LOWPERF MODE: 40s 开启、75s 关闭低性能模式");
''' + s[j:]
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('fixed')
