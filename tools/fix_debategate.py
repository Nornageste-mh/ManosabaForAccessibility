import io
base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

# ---------- Reader: \u8bae\u8bba\u53f0\u8bcd\u4e0d\u81ea\u52a8\u6717\u8bfb\uff0c\u53ea\u5165\u53ef\u91cd\u8bfb\u7f13\u51b2 ----------
p = base + r"\Reader.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """            // \u2014\u2014 \u914d\u97f3\u95f8\u95e8\uff1a\u8fd9\u4e00\u884c\u6709\u6e38\u620f\u914d\u97f3\u65f6\u4e0d\u81ea\u52a8\u6717\u8bfb\uff08\u5426\u5219\u8bed\u97f3\u548c TTS \u4f1a\u53e0\u5728\u4e00\u8d77\u4e92\u76f8\u76d6\u4f4f\uff09"""
new = """            // \u2014\u2014 \u5ba1\u5224\u8bba\u8f69\u95f8\u95e8\uff08\u73a9\u5bb6\u6307\u5b9a\uff0c0.0.0.11\uff09\uff1a
            //    \u8bba\u6218\u53f0\u8bcd\u51e0\u4e4e\u5168\u90e8\u5e26\u8bed\u97f3\uff08\u8bed\u6599 2,003 \u6761\u91cc 99.4%\uff09\uff0c\u81ea\u52a8 TTS \u4f1a\u548c\u8bed\u97f3
            //    \u53e0\u5728\u4e00\u8d77\u4e92\u76f8\u76d6\uff1b\u4f46**\u5fc5\u987b\u8fdb\u53ef\u91cd\u8bfb\u7f13\u51b2**\uff08\u4e0a\u9762 Remember \u5df2\u7ecf\u505a\u4e86\uff09\uff0c
            //    \u73a9\u5bb6\u6309\u91cd\u8bfb\u952e\u4ecd\u80fd\u542c\u5230\u4e2d\u6587\u53f0\u8bcd\u3002
            //    \u4e0d\u80fd\u7528\u914d\u97f3\u95f8\u95e8\u4ee3\u66ff\uff1a\u8bae\u8bba\u8def\u5f84\u62ff\u4e0d\u5230\u884c id\uff08`debate:add` \u4e0d\u8d70 PrintedMessage\uff09\u3002
            if (src.StartsWith("debate", StringComparison.Ordinal)
                && (Plugin.CfgReadDebate == null || !Plugin.CfgReadDebate.Value))
            {
                try { Speech.Stop(); } catch { }
                if (Plugin.CfgDiagLog != null && Plugin.CfgDiagLog.Value)
                    A11yHost.Diag("Reader.skipped[debate] \u53ea\u8fdb\u91cd\u8bfb\u7f13\u51b2: " + line);
                return;
            }

            // \u2014\u2014 \u914d\u97f3\u95f8\u95e8\uff1a\u8fd9\u4e00\u884c\u6709\u6e38\u620f\u914d\u97f3\u65f6\u4e0d\u81ea\u52a8\u6717\u8bfb\uff08\u5426\u5219\u8bed\u97f3\u548c TTS \u4f1a\u53e0\u5728\u4e00\u8d77\u4e92\u76f8\u76d6\u4f4f\uff09"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Reader: debate gate added")

# ---------- Plugin: CfgReadDebate ----------
p2 = base + r"\Plugin.cs"
t = io.open(p2, encoding="utf-8-sig").read()
t = t.replace("        internal static ConfigEntry<string> CfgRepeatKey;",
              "        internal static ConfigEntry<string> CfgRepeatKey;\n        internal static ConfigEntry<bool> CfgReadDebate;", 1)
old2 = """            CfgSpeechBackend = Config.Bind("\u6717\u8bfb", "\u8bed\u97f3\u540e\u7aef", "\u81ea\u52a8","""
new2 = """            CfgReadDebate = Config.Bind("\u6717\u8bfb", "\u6717\u8bfb\u5ba1\u5224\u8bae\u8bba", false,
                "\u5ba1\u5224\u65f6\u7684\u8bba\u6218\u53f0\u8bcd\u51e0\u4e4e\u5168\u90e8\u5e26\u8bed\u97f3\uff0c\u9ed8\u8ba4\u4e0d\u81ea\u52a8\u6717\u8bfb\uff08\u907f\u514d\u4e0e\u8bed\u97f3\u53e0\u5728\u4e00\u8d77\uff09\uff1b\n"
                + "\u65e0\u8bba\u8fd9\u4e00\u9879\u5f00\u8fd8\u662f\u5173\uff0c**\u90fd\u4f1a\u8fdb\u91cd\u8bfb\u7f13\u51b2**\uff0c\u6309\u91cd\u8bfb\u952e\u53ef\u4ee5\u518d\u542c\u3002");
            CfgSpeechBackend = Config.Bind("\u6717\u8bfb", "\u8bed\u97f3\u540e\u7aef", "\u81ea\u52a8","""
assert old2 in t
t = t.replace(old2, new2, 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(t)
print("Plugin: CfgReadDebate added")
