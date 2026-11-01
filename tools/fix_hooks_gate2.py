import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("""                TestNavMode = false; TestChoiceOnce = false; TestMapChoicePick = 0;
                TestMapPickKind = null; TestMapFloorCount = 0; TestEvModeOnce = false;
                TestEvForce = false; TestEvYes = false; TestEvPick = false; TestEvCmdMode = 0;
                TestRepeatOn = false; TestEscOn = false;""",
"""                TestNavMode = false; TestChoiceOnce = false; TestMapChoicePick = 0;
                TestMapPickKind = null; TestMapFloorCount = 0; TestEvModeOnce = false;
                TestEvForce = false; TestEvYes = false; TestEvPick = false; TestEvCmdMode = 0;
                _testRepeatOn = false; _testEscOn = false;""", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("hook gate names fixed")
