import io
base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

# 1) AuthorFromLineId: \u53ea\u8fd4\u56de\u80fd\u67e5\u6210\u4e2d\u6587\u540d\u7684\u4f5c\u8005
p = base + r"\Reader.cs"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("""                System.Text.RegularExpressions.Match m = LineAuthorRe.Match(lineId);
                return m.Success ? m.Groups[1].Value : "";""",
"""                System.Text.RegularExpressions.Match m = LineAuthorRe.Match(lineId);
                if (!m.Success) return "";
                string id = m.Groups[1].Value;
                // \u2605 \u8fd8\u8981\u786e\u8ba4\u8fd9\u4e2a\u5019\u9009\u80fd\u67e5\u6210\u4e2d\u6587\u540d\uff1a\u5426\u5219 `..._Narrative001`
                //   \u4f1a\u88ab\u53cd\u63a8\u6210\u4f5c\u8005 `Narrative`\uff08\u65c1\u767d\u5c31\u88ab\u5ff5\u6210\u4e00\u4e2a\u82f1\u6587\u540d\uff09\u3002
                string mapName;
                return SpeakerNames.TryMap(id, out mapName) ? id : "";""", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("AuthorFromLineId tightened")

# 2) \u914d\u7f6e\u8fc1\u79fb\uff08\u65e7 cfg \u7684 0 / TODO\uff09
p2 = base + r"\Plugin.cs"
s2 = io.open(p2, encoding="utf-8-sig").read()
anchor = """            // \u65e0\u4eba\u503c\u5b88\u5b9e\u673a\u6d4b\u8bd5\u5f00\u5173"""
assert anchor in s2
mig = """            // \u65e7 cfg \u8fc1\u79fb\uff1a\u53ea\u505a\u201c\u4e00\u5b9a\u77e5\u9053\u7528\u6237\u610f\u56fe\u201d\u7684\u4e24\u4ef6\u4e8b\u3002
            // 1) \u6c89\u9ed8\u6309\u952e\u65e7\u9ed8\u8ba4\u503c "0" \u4e0d\u662f\u5408\u6cd5\u952e\u540d\uff08Hotkey.cs \u62d2\u7edd\u7eaf\u6570\u5b57\uff09\uff0c
            //    \u5b83\u672c\u6765\u5c31\u4ece\u6765\u6ca1\u751f\u6548\u8fc7 \u2192 \u89c6\u4e3a\u201c\u672a\u8bbe\u7f6e\u201d\u5199\u56de\u7a7a\u4e32\u3002
            // 2) \u9000\u51fa\u786e\u8ba4\u5bf9\u8c61\u540d\u65e7\u9ed8\u8ba4\u5199\u7684\u662f TODO \u5360\u4f4d\u7b26\uff08run41 \u4e4b\u524d\u7684\u6a21\u677f\uff09\u3002
            try
            {
                if (CfgSilenceKey != null && (CfgSilenceKey.Value ?? "").Trim() == "0")
                {
                    CfgSilenceKey.Value = "";
                    L.LogInfo("[a11y] cfg \u8fc1\u79fb\uff1a\u6c89\u9ed8\u6309\u952e\\"0\\" -> \\"\uff08\u7559\u7a7a = \u505c\u7528\uff09");
                }
                if (CfgQuitNames != null && (CfgQuitNames.Value ?? "").IndexOf("TODO", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    CfgQuitNames.Value = "Button_Exit";
                    L.LogInfo("[a11y] cfg \u8fc1\u79fb\uff1a\u9000\u51fa\u786e\u8ba4\u5bf9\u8c61\u540d TODO -> Button_Exit\uff08\u5b9e\u673a\u53d6\u8bc1\uff09");
                }
            }
            catch (Exception e) { A11yHost.Diag("cfg \u8fc1\u79fb: " + e.Message); }

"""
s2 = s2.replace(anchor, mig + anchor, 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(s2)
print("config migration added")
