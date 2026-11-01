import io
base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

# ---------- 1) \u91cd\u8bfb\u952e\u9ed8\u8ba4\u6539 "=" + cfg \u8fc1\u79fb Backspace -> "=" ----------
p = base + r"\Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """            CfgRepeatKey = Config.Bind("朗读", "重读按键", "Backspace","""
new = """            // \u9ed8\u8ba4 `=`\uff1aBackspace \u4f1a\u548c\u672c\u4f5c\u81ea\u5e26\u7684\u201c\u8fd4\u56de\u4e0a\u4e00\u53e5\u201d\u952e\u51b2\u7a81\uff08\u73a9\u5bb6\u5b9e\u6d4b\uff09\uff0c
            //   `=` \u5728\u8fd9\u4f5c\u91cc\u6ca1\u6709\u539f\u751f\u7528\u9014\uff0c\u4e5f\u5c31\u4e0d\u4f1a\u88ab\u6e38\u620f\u62a2\u3002
            CfgRepeatKey = Config.Bind("朗读", "重读按键", "=" """
new = new.rstrip('"') + '"'
assert old in s
s = s.replace(old, new, 1)

# \u8fc1\u79fb\uff1a\u65e7\u9ed8\u8ba4 Backspace -> "="
anchor = """                if (CfgQuitNames != null && (CfgQuitNames.Value ?? "").IndexOf("TODO", StringComparison.OrdinalIgnoreCase) >= 0)"""
mig = """                // 3) \u91cd\u8bfb\u952e\u65e7\u9ed8\u8ba4 Backspace \u4e0e\u6e38\u620f\u81ea\u5e26\u952e\u51b2\u7a81\uff080.0.0.11 \u6539\u4e3a `=`\uff09\u3002
                //    \u53ea\u8fc1\u79fb\u201c\u5b8c\u5168\u7b49\u4e8e\u65e7\u9ed8\u8ba4\u503c\u201d\u7684\u60c5\u5f62\uff0c\u4e0d\u78b0\u73a9\u5bb6\u81ea\u5df1\u6539\u8fc7\u7684\u952e\u3002
                if (CfgRepeatKey != null && string.Equals((CfgRepeatKey.Value ?? "").Trim(), "Backspace", StringComparison.OrdinalIgnoreCase))
                {
                    CfgRepeatKey.Value = "=";
                    L.LogInfo("[a11y] cfg \u8fc1\u79fb\uff1a\u91cd\u8bfb\u6309\u952e Backspace -> = \uff08\u907f\u8ba9\u6e38\u620f\u81ea\u5e26\u952e\uff09");
                }
"""
assert anchor in s
s = s.replace(anchor, mig + anchor, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("default repeat key '=' + migration")
