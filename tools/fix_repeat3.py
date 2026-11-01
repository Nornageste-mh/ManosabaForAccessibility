import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = '''            CfgRepeatKey = Config.Bind("朗读", "重读按键", "=" "
                "重读上一句剧情/选项。本作只启用新 Input System，键名照 Unity KeyCode 写。");'''
new = '''            CfgRepeatKey = Config.Bind("朗读", "重读按键", "=",
                "重读上一句剧情/选项。本作只启用新 Input System，键名照 Unity KeyCode 写；\n"
                + "`=` 直接写等号，也可以写 `Equals`。默认不是 Backspace 的原因：\n"
                + "它会和本作自带的“返回上一句”冲突（玩家实测）。");'''
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("repeat key bind fixed")
