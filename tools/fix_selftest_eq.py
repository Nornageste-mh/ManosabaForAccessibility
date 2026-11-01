import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\SelfTest.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """            ok = Hotkey.Parse("Backspace", out sp, out why);
            Check("hotkey.\u9000\u683c", ok && sp.Key == KeyCode.Backspace, "ok=" + ok + " why=" + why);"""
new = """            ok = Hotkey.Parse("Backspace", out sp, out why);
            Check("hotkey.\u9000\u683c", ok && sp.Key == KeyCode.Backspace, "ok=" + ok + " why=" + why);

            // 0.0.0.11\uff1a\u91cd\u8bfb\u952e\u9ed8\u8ba4\u6539\u6210 `=`\u3002Enum.TryParse \u4e0d\u8ba4\u8fd9\u4e2a\u7b26\u53f7\uff0c
            //   \u6240\u4ee5\u5fc5\u987b\u6709\u8fd9\u6761\u65ad\u8a00\u5b88\u7740\u2014\u2014\u5426\u5219\u73a9\u5bb6\u4f1a\u5f97\u5230\u4e00\u4e2a\u201c\u6c38\u8fdc\u4e0d\u89e6\u53d1\u201d\u7684\u91cd\u8bfb\u952e\u3002
            ok = Hotkey.Parse("=", out sp, out why);
            Check("hotkey.\u7b49\u53f7", ok && sp.Key == KeyCode.Equals && !sp.Alt && !sp.Ctrl && !sp.Shift,
                  "ok=" + ok + " why=" + why);

            ok = Hotkey.Parse("Equals", out sp, out why);
            Check("hotkey.Equals\u522b\u540d", ok && sp.Key == KeyCode.Equals, "ok=" + ok + " why=" + why);"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("SelfTest: '=' assertions added")
