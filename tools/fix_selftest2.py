import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\SelfTest.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """            TextProcTests();
            SpeakerTests();
            VoiceTests();
            HotkeyTests();
            Summary();"""
new = """            TextProcTests();
            SpeakerTests();
            VoiceTests();
            HotkeyTests();
            // 2026-11-01 \u5ba1\u8ba1\u65b0\u589e\u7684\u4e24\u7ec4\uff1a\u6587\u672c\u8fb9\u754c\u7528\u4f8b + L2 \u63a5\u7ebf/\u914d\u7f6e\u9ed8\u8ba4\u503c
            CheckText();
            CheckWiring();
            Summary();"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Run() wired")
