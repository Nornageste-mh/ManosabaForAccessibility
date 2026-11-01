import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs"
lines = io.open(p, encoding="utf-8-sig").read().split("\n")
# \u627e\u5230\u88ab\u5199\u574f\u7684\u533a\u6bb5\uff1a\u4ece CfgRepeatKey \u5f00\u59cb\u5230 CfgSpeechBackend \u4e4b\u524d
start = next(i for i, l in enumerate(lines) if "CfgRepeatKey = Config.Bind" in l)
end = next(i for i, l in enumerate(lines) if "CfgSpeechBackend = Config.Bind" in l)
fixed = [
 '            CfgRepeatKey = Config.Bind("\u6717\u8bfb", "\u91cd\u8bfb\u6309\u952e", "=",',
 '                "\u91cd\u8bfb\u4e0a\u4e00\u53e5\u5267\u60c5/\u9009\u9879\u3002\u672c\u4f5c\u53ea\u542f\u7528\u65b0 Input System\uff0c\u952e\u540d\u7167 Unity KeyCode \u5199\uff1b\\n"',
 '                + "`=` \u76f4\u63a5\u5199\u7b49\u53f7\uff0c\u4e5f\u53ef\u4ee5\u5199 `Equals`\u3002\u9ed8\u8ba4\u4e0d\u662f Backspace \u7684\u539f\u56e0\uff1a\\n"',
 '                + "\u5b83\u4f1a\u548c\u672c\u4f5c\u81ea\u5e26\u7684\u201c\u8fd4\u56de\u4e0a\u4e00\u53e5\u201d\u51b2\u7a81\uff08\u73a9\u5bb6\u5b9e\u6d4b\uff09\u3002");',
 '            CfgReadDebate = Config.Bind("\u6717\u8bfb", "\u6717\u8bfb\u5ba1\u5224\u8bae\u8bba", false,',
 '                "\u5ba1\u5224\u65f6\u7684\u8bba\u6218\u53f0\u8bcd\u51e0\u4e4e\u5168\u90e8\u5e26\u8bed\u97f3\uff0c\u9ed8\u8ba4\u4e0d\u81ea\u52a8\u6717\u8bfb\uff08\u907f\u514d\u4e0e\u8bed\u97f3\u53e0\u5728\u4e00\u8d77\uff09\uff1b\\n"',
 '                + "\u65e0\u8bba\u8fd9\u4e00\u9879\u5f00\u8fd8\u662f\u5173\uff0c**\u90fd\u4f1a\u8fdb\u91cd\u8bfb\u7f13\u51b2**\uff0c\u6309\u91cd\u8bfb\u952e\u53ef\u4ee5\u518d\u542c\u3002");',
]
out = lines[:start] + fixed + lines[end:]
io.open(p, "w", encoding="utf-8-sig", newline="\n").write("\n".join(out))
print("lines repaired:", start, "..", end)
