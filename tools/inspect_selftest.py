import io
# SelfTest\uff1a\u52a0\u4e24\u6761\u65ad\u8a00\uff08`=` \u53ef\u89e3\u6790\u3001Equals \u4e5f\u53ef\u89e3\u6790\uff09
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\SelfTest.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """            Hotkey.Spec sp; string why;"""
print("anchor found:", old in s)
print(s[s.index("Hotkey.parse.F7") - 400 : s.index("Hotkey.parse.F7") + 200] if "Hotkey.parse.F7" in s else "no F7 label")
