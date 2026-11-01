import io
p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs'
s = io.open(p, encoding='utf-8').read()

# 1) 首帧：按配置应用低性能模式（放在启动播报之后）
anchor = '''                if (Plugin.CfgStartupHint != null && Plugin.CfgStartupHint.Value)'''
i = s.index(anchor)
# 找到该 if 块结束：以 "\n                }\n            }\n" 之后插
j = s.index('            }\n', s.index('try { Repeat.Say("魔法少女ノ魔女裁判', i))
insertAt = s.index('\n', j) + 1
add = '''
            // 低性能模式：配置里开启了就在开局应用一次（默认关闭）。
            // 画面类设置（URP/TrueShadow/分辨率）在这里落地；之后每 3 秒补扫新出现的柔边阴影。
            if (!_lowPerfApplied)
            {
                _lowPerfApplied = true;
                try { LowPerf.ApplyFromConfig(); } catch (Exception e) { A11yHost.Diag("LowPerf: " + e.Message); }
            }
'''
s = s[:insertAt] + add + s[insertAt:]

# 2) 字段 _lowPerfApplied
anchor2 = '            _ticks++;\n'
assert anchor2 in s
s = s.replace(anchor2, '            _ticks++;\n', 1)

# 在 PluginBehaviour 类里找私有字段区（_ticks 定义处）
import re
m = re.search(r'(\n\s*private int _ticks;)', s)
assert m, 'ticks field not found'
s = s[:m.end()] + '\n        private bool _lowPerfApplied;' + s[m.end():]

# 3) OnDestroy：还原画面设置
k = s.index('        public void OnDestroy()')
brace = s.index('{', k)
s = s[:brace+1] + '''
            // 卸载时把低性能模式改过的画面设置还原，别把玩家的画质留在降级状态。
            try { LowPerf.Shutdown(); } catch { }
''' + s[brace+1:]

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('ok lowPerfApplied=', s.count('_lowPerfApplied'), ' shutdown=', s.count('LowPerf.Shutdown()'))
