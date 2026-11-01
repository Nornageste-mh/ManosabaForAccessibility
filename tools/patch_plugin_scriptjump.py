import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()
orig = s
def rep(old, new, cnt=1):
    global s
    assert s.count(old) == cnt, "anchor count %d != %d for %r" % (s.count(old), cnt, old[:60])
    s = s.replace(old, new)

# 1) 配置字段改名
rep("        internal static ConfigEntry<bool> CfgAllowDevPanel;",
    "        internal static ConfigEntry<bool> CfgAllowScriptJump;")

# 2) 启动诊断
rep('''                L.LogInfo("[a11y] DevPanel 配置: allow=" + (CfgAllowDevPanel != null && CfgAllowDevPanel.Value)
                          + " isys=" + InputSysKeys.Available);
            }
            catch (Exception e) { L.LogWarning("[a11y] DevPanel 配置读取失败: " + e.Message); }''',
    '''                L.LogInfo("[a11y] 剧情跳转（开发者）: allow=" + (CfgAllowScriptJump != null && CfgAllowScriptJump.Value)
                          + " isys=" + InputSysKeys.Available + " 剧本数=" + ScriptIndex.Count);
            }
            catch (Exception e) { L.LogWarning("[a11y] 剧情跳转配置读取失败: " + e.Message); }''')

# 3) Config.Bind：整段替换（含旧说明）
old_bind_start = '            CfgAllowDevPanel = Config.Bind("其它", "允许激活开发者选项", false,'
i = s.index(old_bind_start)
j = s.index(");", s.index("开发构建（Editor/Dev Build）才有完整面板。保留开关是为了开发构建可用。", i)) + 2
new_bind = '''            CfgAllowScriptJump = Config.Bind("其它", "允许剧情跳转（开发者）", false,
                "开启后按 Ctrl+Alt+Shift+F3 打开「剧情跳转」菜单：上下键换剧本、翻页键跳十个、\\n" +
                "数字键输入起始行、回车跳到该剧本指定行、Esc 退出。\\n" +
                "★ 这是**开发者/测试功能**：会直接跳到没读过的剧情、破坏存档进度、可能剧透。\\n" +
                "  正式游玩请保持关闭（默认关闭）。\\n" +
                "★ 为什么不是「打开游戏内置开发者面板」（v0.0.0.6 的旧承诺）：实测正式发行版里\\n" +
                "  游戏自己没创建调试管理器（DebugOptionsBase.s_debugManager 为 null），\\n" +
                "  DebugPanel 只是内存里从未实例化的预制体，那条路在发行版里走不通。\\n" +
                "★ 跳转能力来自 Naninovel 的 ScriptPlayerExtensions.LoadAndPlayAtLine，\\n" +
                "  0.0.0.9 起已在实机验证过（当时用环境变量 MANOSABA_A11Y_JUMP_LINE 直达 Trial04 第 327 行）。");'''
s = s[:i] + new_bind + s[j:]

# 4) Update 调用
rep('''            // 开发者面板开关（Ctrl+Alt+Shift+F3；配置「允许激活开发者选项」默认关闭）
            try { DevPanel.Update(); }
            catch (Exception e) { A11yHost.Diag("DevPanel: " + e.Message); }''',
    '''            // 开发者功能：剧情跳转菜单（Ctrl+Alt+Shift+F3；配置「允许剧情跳转（开发者）」默认关闭）
            try { ScriptJump.Update(); }
            catch (Exception e) { A11yHost.Diag("ScriptJump: " + e.Message); }
            try { ScriptJump.TestTick(); }
            catch (Exception e) { A11yHost.Diag("ScriptJump.Test: " + e.Message); }''')

# 5) hook 字段 + 读取
rep("        internal static string TestEvCmdMode;",
    '''        internal static string TestEvCmdMode;

        /// <summary>自动化用（仅 MANOSABA_A11Y_TEST_SCRIPTJUMP=1）：从插件内部驱动剧情跳转菜单。</summary>
        internal static bool TestScriptJump;''')

rep("            TestEvForce = false; TestEvYes = false; TestEvPick = false; TestEvCmdMode = \"\";",
    "            TestEvForce = false; TestEvYes = false; TestEvPick = false; TestEvCmdMode = \"\";\n            TestScriptJump = false;")

io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Plugin.cs patched, delta bytes:", len(s) - len(orig))
