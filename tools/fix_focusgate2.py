import io
base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

# Choices: \u5b57\u952e\u5faa\u73af\u5916\u5305\u7126\u70b9\u95e8
p = base + r"\Choices.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """                    if (KeyEdge.Pressed(KeyCode.Alpha0 + i) || KeyEdge.Pressed(KeyCode.Keypad0 + i))"""
new = """                    if (Plugin.FocusGate.CanReadChoiceKeys()
                        && (KeyEdge.Pressed(KeyCode.Alpha0 + i) || KeyEdge.Pressed(KeyCode.Keypad0 + i)))"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Choices focus gate")

p = base + r"\MapChoice.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """                    if (KeyEdge.Pressed(KeyCode.Alpha0 + i) || KeyEdge.Pressed(KeyCode.Keypad0 + i))"""
new = """                    if (Plugin.FocusGate.CanReadChoiceKeys()
                        && (KeyEdge.Pressed(KeyCode.Alpha0 + i) || KeyEdge.Pressed(KeyCode.Keypad0 + i)))"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("MapChoice focus gate")

p = base + r"\Evidence.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """                if (!Tapped(KeyCode.Alpha0 + i) && !Tapped(KeyCode.Keypad0 + i)) continue;"""
new = """                if (!Plugin.FocusGate.CanReadChoiceKeys()) continue;
                if (!Tapped(KeyCode.Alpha0 + i) && !Tapped(KeyCode.Keypad0 + i)) continue;"""
assert old in s
s = s.replace(old, new, 1)
s = s.replace("""                        if (!Tapped(KeyCode.Alpha0 + k) && !Tapped(KeyCode.Keypad0 + k)) continue;""",
              """                        if (!Plugin.FocusGate.CanReadChoiceKeys()) continue;
                        if (!Tapped(KeyCode.Alpha0 + k) && !Tapped(KeyCode.Keypad0 + k)) continue;""", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Evidence focus gate")
