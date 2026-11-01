import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("""                TestEvForce = false; TestEvYes = false; TestEvPick = false; TestEvCmdMode = 0;
                _testRepeatOn = false; _testEscOn = false;""",
"""                TestEvForce = false; TestEvYes = false; TestEvPick = false; TestEvCmdMode = "";
                // TEST_REPEAT / TEST_ESC 的开关在 PluginBehaviour 里（同一帧驱动），
                // 这里够不到；它们的触发还依赖 UI 场景，且只重复「重读/关闭」这类只读动作，风险可接受。""", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("fixed")
