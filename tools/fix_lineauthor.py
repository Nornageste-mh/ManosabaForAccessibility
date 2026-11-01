import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\LineSpeakers.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """        /// <summary>\u884c id \u2192 \u4f5c\u8005 id\uff1b\u8868\u672a\u547d\u4e2d\u8fd4\u56de\u7a7a\u4e32\u3002</summary>
        internal static string AuthorOf(string lineId)"""
new = """        /// <summary>
        /// \u884c id \u91cc\u53cd\u63a8\u4f5c\u8005 id\uff08\u515c\u5e95\uff09\u3002
        /// \u4e3a\u4ec0\u4e48\u9700\u8981\uff1a\u300a\u672c\u4f5c\u300b\u6709 2,050 \u4e2a\u6709\u914d\u97f3\u7684\u884c\u5728\u672c\u5730\u5316\u6587\u6863\u91cc**\u6ca1\u6709
        /// `; &gt; Ema: |#\u2026|` \u5f62\u5f0f\u7684\u4f5c\u8005\u6ce8\u91ca**\uff081,988 \u4e2a\u662f `@printDebate`\uff09\uff0c\u4f5c\u8005\u53ea\u5b58\u5728\u4e8e\u884c id \u91cc\uff0c
        /// \u5982 `0101Trial01_Sherry001` \u2192 `Sherry`\u3002\u89c4\u5219\uff1a\u672b\u5c3e\u4e09\u4f4d\u6570\u5b57\u524d\u7684\u90a3\u6bb5\u300c_\u5b57\u6bcd\u300d\u3002
        /// \u5728\u5df2\u77e5 24,492 \u6761\u4f5c\u8005\u4e0a\u56de\u653e\u4e00\u81f4\u7387 99.84%\uff08\u4f8b\u5916\u662f\u300c\u8868\u5199 Unknown\u3001id \u5199 Yuki\u300d\u7684\u4f2a\u88c5\u884c\uff09\u3002
        /// </summary>
        internal static string AuthorFromLineId(string lineId)
        {
            if (string.IsNullOrEmpty(lineId)) return "";
            try
            {
                Match m = LineAuthorRe.Match(lineId);
                return m.Success ? m.Groups[1].Value : "";
            }
            catch { return ""; }
        }

        private static readonly System.Text.RegularExpressions.Regex LineAuthorRe =
            new System.Text.RegularExpressions.Regex(@"_([A-Za-z][A-Za-z0-9]*?)\\d{3}$",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>\u884c id \u2192 \u4f5c\u8005 id\uff1b\u8868\u672a\u547d\u4e2d\u8fd4\u56de\u7a7a\u4e32\u3002</summary>
        internal static string AuthorOf(string lineId)"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("LineSpeakers patched")

# Reader: line-id \u515c\u5e95
p2 = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Reader.cs"
s2 = io.open(p2, encoding="utf-8-sig").read()
old2 = """                    string fromLine = LineSpeakers.AuthorOf(lineKey);
                    if (!string.IsNullOrEmpty(fromLine))
                    {"""
new2 = """                    string fromLine = LineSpeakers.AuthorOf(lineKey);
                    // \u8868\u672a\u547d\u4e2d\uff08\u672c\u4f5c 1,988 \u4e2a @printDebate \u884c\u90fd\u6ca1\u6709\u4f5c\u8005\u6ce8\u91ca\uff09
                    // \u65f6\u4ece\u884c id \u672b\u5c3e\u53cd\u63a8\uff08`0101Trial01_Sherry001` \u2192 `Sherry`\uff09\u3002
                    if (string.IsNullOrEmpty(fromLine)) fromLine = LineSpeakers.AuthorFromLineId(lineKey);
                    if (!string.IsNullOrEmpty(fromLine))
                    {"""
assert old2 in s2
s2 = s2.replace(old2, new2, 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(s2)
print("Reader patched")
