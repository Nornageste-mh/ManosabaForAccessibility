import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\SpeakerNames.cs"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("        /// <summary>*}\n", "        /// <summary>\n", 1)
s = s.replace("    /// <summary>AuthorId \u2192 \u6717\u8bfb\u7528\u4e2d\u6587\u540d\u3002\u67e5\u4e0d\u5230\u65f6\u8fd4\u56de\u539f id\uff08\u4e0d\u541e\u540d\u5b57\uff09\u3002</summary>",
              "    /// <summary>AuthorId \u2192 \u6717\u8bfb\u7528\u4e2d\u6587\u540d\u3002\u67e5\u4e0d\u5230\u65f6\u7531\u8c03\u7528\u65b9\u51b3\u5b9a\u56de\u843d\uff08\u89c1 TryMap\uff09\u3002</summary>", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("fixed")
