import io
base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"
for f in ["Choices.cs", "MapChoice.cs", "Evidence.cs"]:
    p = base + "\\" + f
    s = io.open(p, encoding="utf-8-sig").read()
    s = s.replace("Plugin.FocusGate.CanReadChoiceKeys()", "FocusGate.CanReadChoiceKeys()")
    io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("refs fixed")
