import io, re
p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs'
s = io.open(p, encoding='utf-8').read()
orig = s

# 1) 版本号
s = s.replace('public const string Version = "0.0.0.12";',
              'public const string Version = "0.0.0.13";', 1)

# 2) 字段：放在 CfgAllowScriptJump 之后
anchor = '        internal static ConfigEntry<bool> CfgAllowScriptJump;\n'
assert anchor in s
fields = anchor + '''
        // 低性能模式（0.0.0.13）：默认关闭；开关组合键运行中可切；四档各自可以单独关。
        internal static ConfigEntry<bool> CfgLowPerf;
        internal static ConfigEntry<string> CfgLowPerfToggleKey;
        internal static ConfigEntry<bool> CfgLowPerfDegradeUrp;
        internal static ConfigEntry<bool> CfgLowPerfNoShadow;
        internal static ConfigEntry<bool> CfgLowPerfLowerResolution;
        internal static ConfigEntry<bool> CfgLowPerfHint;
'''
s = s.replace(anchor, fields, 1)

# 3) 配置项：接在 CfgAllowScriptJump 的 Bind 之后（以迁移注释为锚）
anchor2 = '            // 旧 cfg 迁移：只做“一定知道用户意图”的两件事。'
assert anchor2 in s
cfg = '''            CfgLowPerf = Config.Bind("性能", "低性能模式", false,
                "默认关闭。开启后：降低插件自身的每帧开销（无视觉代价）+ 降低画面设置。\\n" +
                "★ 实测（0.0.0.12 的性能探针，详见 docs/framework-notes.md §22）：\\n" +
                "  · 插件自己原来每帧要花 10~13ms，限频后 2.5~5ms，这部分是纯收益；\\n" +
                "  · 画面降级（URP 画质/柔边阴影/分辨率）在测试机上**没有测出帧率提升**——\\n" +
                "    那台机器是主线程瓶颈，不是显卡瓶颈。所以画面三档做成了可以单独关掉。\\n" +
                "★ 什么时候值得开：显存/内存小、显卡弱、或者画面卡顿明显时。");
            CfgLowPerfToggleKey = Config.Bind("性能", "低性能模式开关键", "Alt+Shift+F2",
                "运行中开关「低性能模式」的组合键。改成空串 = 只能用配置项开启。\\n" +
                "★ 和本作自带按键不冲突（游戏用的是回车/空格/左键/Esc/Ctrl）。");
            CfgLowPerfDegradeUrp = Config.Bind("性能", "降低 URP 画质", true,
                "低性能模式开启时生效：渲染缩放 0.8、关 MSAA、关主光与软阴影、关额外灯光。\\n" +
                "★ 画面会变糊、立体感变弱，但能省显卡。关闭模式时会还原原始设置。");
            CfgLowPerfNoShadow = Config.Bind("性能", "关闭柔边阴影（TrueShadow）", true,
                "低性能模式开启时生效：把本作 800 多个柔边阴影组件全部临时关掉。\\n" +
                "★ 文字与立绘的阴影会消失、对比度会变，看不清时请关掉这一项。");
            CfgLowPerfLowerResolution = Config.Bind("性能", "降低渲染分辨率", false,
                "低性能模式开启时生效：把窗口分辨率降到 75%（例如 1920x1080 → 1440x810）。\\n" +
                "★ 默认关闭：这一档最容易被看出来（窗口变小/画面变糊），而且它会改变窗口大小，\\n" +
                "  读屏用户如果依赖固定窗口位置，请不要开。");
            CfgLowPerfHint = Config.Bind("性能", "低帧率时提示开启", true,
                "长时间低于 15 帧时，念一句「按 Alt+Shift+F2 可以开启低性能模式」。\\n" +
                "★ 只提醒、绝不自动切换：开不开由玩家决定（自动改画面属于替玩家做决策）。");

'''
s = s.replace(anchor2, cfg + anchor2, 1)

# 4) Update 里接 LowPerf.Update（放在 ScreenAssist.Update 之后）
anchor3 = '            try { ScreenAssist.Update(); }'
assert anchor3 in s
s = s.replace(anchor3, anchor3 + '\n            try { LowPerf.Update(); } catch (Exception e) { A11yHost.Diag("LowPerf: " + e.Message); }', 1)

# 5) Load 末尾打一行低性能模式诊断（放在剧情跳转诊断之后）
anchor4 = '            L.LogInfo("[a11y] 剧情跳转（开发者）: allow="'
i = s.index(anchor4)
j = s.index(';', s.index('.Value)', i))  # 该语句的结尾分号（跨行 LogInfo 结束）
# 直接找该语句后的换行，插在其后
j2 = s.index('\n', j) + 1
diag = ''
s = s[:j2] + diag + s[j2:]

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('patched; version ok=', 'Version = "0.0.0.13"' in s, ' cfg ok=', s.count('CfgLowPerf = Config.Bind'), ' update ok=', s.count('LowPerf.Update()'))
