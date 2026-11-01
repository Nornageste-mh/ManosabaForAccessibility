import io
p2 = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Reader.cs"
s2 = io.open(p2, encoding="utf-8-sig").read()
s2 = s2.replace("LineSpeakers.AuthorFromLineId(lineKey)", "AuthorFromLineId(lineKey)", 1)
marker = "        private static string Speaker" if "private static string Speaker" in s2 else "        private static void SpeakNow("
assert marker in s2
snippet = """        /// <summary>
        /// \u884c id \u2192 \u4f5c\u8005 id \u7684\u56de\u9000\u5339\u914d\uff08\u672b\u5c3e `_\u5b57\u6bcd+\u4e09\u4f4d\u6570\u5b57`\uff09\u3002
        /// \u672c\u4f5c 1,988 \u4e2a `@printDebate` \u884c\u5728\u672c\u5730\u5316\u6587\u6863\u91cc\u6ca1\u6709\u4f5c\u8005\u6ce8\u91ca\uff0c\u4f5c\u8005\u53ea\u5728 id \u91cc\uff1b
        /// \u5728\u5df2\u77e5 24,492 \u6761\u4f5c\u8005\u4e0a\u56de\u653e\u4e00\u81f4\u7387 99.84%\u3002\u653e\u5728\u8fd9\u91cc\u800c\u4e0d\u653e LineSpeakers\uff0c
        /// \u662f\u56e0\u4e3a\u540e\u8005\u662f\u751f\u6210\u6587\u4ef6\uff0c\u91cd\u65b0\u751f\u6210\u4f1a\u628a\u624b\u5199\u4ee3\u7801\u8986\u76d6\u6389\u3002
        /// </summary>
        private static string AuthorFromLineId(string lineId)
        {
            if (string.IsNullOrEmpty(lineId)) return "";
            try
            {
                System.Text.RegularExpressions.Match m = LineAuthorRe.Match(lineId);
                return m.Success ? m.Groups[1].Value : "";
            }
            catch { return ""; }
        }

        private static readonly System.Text.RegularExpressions.Regex LineAuthorRe =
            new System.Text.RegularExpressions.Regex(@"_([A-Za-z][A-Za-z0-9]*?)\\d{3}$",
                System.Text.RegularExpressions.RegexOptions.Compiled);

"""
s2 = s2.replace(marker, snippet + marker, 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(s2)
print("inserted helper")
