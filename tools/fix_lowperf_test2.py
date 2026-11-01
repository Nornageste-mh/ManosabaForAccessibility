import io, re
p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs'
s = io.open(p, encoding='utf-8').read()

# 字段改成 internal（PluginBehaviour 里要写）
s = s.replace('        private static int _testLowPerfStage;   // MANOSABA_A11Y_TEST_LOWPERF=1：自动开关一次，供自动化取证',
              '        internal static int _testLowPerfStage;   // MANOSABA_A11Y_TEST_LOWPERF=1：自动开关一次，供自动化取证', 1)

# TestLowPerfTick 里的引用加 Plugin. 前缀
start = s.index('        private static void TestLowPerfTick()')
end = s.index('        private static void TestKeyInjectTick()')
body = s[start:end]
body2 = (body.replace('if (!TestLowPerf) return;', 'if (!Plugin.TestLowPerf) return;')
             .replace('_testLowPerfStage', 'Plugin._testLowPerfStage')
             .replace('L.LogInfo(', 'Plugin.L.LogInfo('))
s = s[:start] + body2 + s[end:]
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('patched', body2.count('Plugin.'))
