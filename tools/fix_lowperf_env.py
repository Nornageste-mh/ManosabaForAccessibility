import io
p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs'
s = io.open(p, encoding='utf-8').read()
bad = '            TestLowPerf = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_LOWPERF") == "1";\n'
assert bad in s
s = s.replace(bad, '', 1)
# 正确落点：Load() 里读环境变量的那块（找 MANOSABA_A11Y_TEST_SCRIPTJUMP 的 GetEnvironmentVariable 行）
key = 'Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_SCRIPTJUMP")'
i = s.index(key)
lineEnd = s.index('\n', i) + 1
s = s[:lineEnd] + '            TestLowPerf = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_LOWPERF") == "1";\n' + s[lineEnd:]
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('fixed; envreads=', s.count('MANOSABA_A11Y_TEST_LOWPERF'))
