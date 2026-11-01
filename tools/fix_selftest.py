import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Reader.cs"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("        private static string AuthorFromLineId(string lineId)",
              "        internal static string AuthorFromLineId(string lineId)", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)

p2 = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\SelfTest.cs"
s2 = io.open(p2, encoding="utf-8-sig").read()
# \u627e Summary() \u524d\u9762\u63d2\u65b0\u65ad\u8a00
anchor = "        private static void Summary()"
assert anchor in s2
new = '''        private static void CheckText()
        {
            // ---- 2026-11-01 \u5ba1\u8ba1\u56de\u586b\uff1a\u8fd9\u516d\u6761\u5728\u4fee\u590d\u524d\u662f\u7ea2\u7684 ----
            Check("textproc.br\u5de6\u4fa7\u52a9\u8bcd\u4e0d\u8865\u9017\u53f7",
                  !TextProc.Clean("\u6302\u5728\u5899\u4e0a\u7684<br>\u5168\u65b0\u7684\u5236\u670d").Contains("\u7684\uff0c"),
                  TextProc.Clean("\u6302\u5728\u5899\u4e0a\u7684<br>\u5168\u65b0\u7684\u5236\u670d"));
            Check("textproc.br\u540e\u5168\u89d2\u7f29\u8fdb\u4e0d\u7559\u7a7a\u683c",
                  !TextProc.Clean("\u5899\u4e0a\u7684<br>\u3000\u5168\u65b0").Contains(" "),
                  TextProc.Clean("\u5899\u4e0a\u7684<br>\u3000\u5168\u65b0"));
            Check("textproc.ruby\u62c9\u4e01\u65c1\u6ce8\u4e0d\u5ff5",
                  TextProc.Clean("<ruby=\\"Balloon\\">\u6c14\u7403</ruby>") == "\u6c14\u7403",
                  TextProc.Clean("<ruby=\\"Balloon\\">\u6c14\u7403</ruby>"));
            Check("textproc.\u5168\u89d2\u7a7a\u683c\u4e0d\u7559\u534a\u89d2\u7a7a\u683c",
                  TextProc.Clean("\u7532\u3000\u4e59") == "\u7532\u4e59",
                  TextProc.Clean("\u7532\u3000\u4e59"));
            Check("textproc.\u7834\u6298\u53f7\u540e\u4e0d\u53e0\u9017\u53f7",
                  !TextProc.Clean("\u7532\u2014\u2014\uff0c\u4e59").Contains("\u2014\uff0c"),
                  TextProc.Clean("\u7532\u2014\u2014\uff0c\u4e59"));
            Check("speaker.\u672a\u6620\u5c04\u82f1\u6587id\u4e0d\u5ff5",
                  TextProc.Speaker("CreatureFreshName") == "",
                  TextProc.Speaker("CreatureFreshName"));

            // ---- \u884c id \u53cd\u63a8\u4f5c\u8005\uff08\u5ba1\u8ba1 P1-2\uff1a1,988 \u4e2a @printDebate \u884c\u53ea\u80fd\u4ece id \u63a8\uff09----
            Check("speaker.printDebate\u884c\u53ef\u53cd\u63a8",
                  Reader.AuthorFromLineId("0101Trial01_Sherry001") == "Sherry",
                  Reader.AuthorFromLineId("0101Trial01_Sherry001"));
            Check("speaker.\u65c1\u767did\u4e0d\u4ea7\u751f\u4f5c\u8005",
                  Reader.AuthorFromLineId("0101Adv01_Narrative001") == "",
                  Reader.AuthorFromLineId("0101Adv01_Narrative001"));
            Check("speaker.\u53cd\u63a8\u7ed3\u679c\u80fd\u67e5\u6210\u4e2d\u6587\u540d",
                  TextProc.Speaker(Reader.AuthorFromLineId("0101Trial01_Sherry001")) == "\u6a58\u96ea\u8389",
                  TextProc.Speaker(Reader.AuthorFromLineId("0101Trial01_Sherry001")));
        }

        private static void CheckWiring()
        {
            // ---- L2 \u5951\u7ea6\u63a5\u7ebf\uff08\u5ba1\u8ba1 E\uff1a\u5c11\u586b\u4e00\u4e2a\u94a9\u5b50\u5c31\u662f\u9759\u9ed8\u5931\u6548\uff09----
            int missing = -1;
            try { missing = A11yHost.Missing(); } catch (Exception) { }
            Check("wiring.\u5951\u7ea6\u5fc5\u586b\u9879\u9f50\u5168", missing == 0, "missing=" + missing);
            Check("wiring.groupPriority", A11yHost.GameGroupPriority != null);
            Check("wiring.groupName", A11yHost.GameGroupName != null);
            Check("wiring.labelOf", A11yHost.GameLabelOf != null);
            Check("wiring.textIsNoise", A11yHost.GameTextIsNoise != null);
            Check("wiring.muteSubmit", A11yHost.ShouldMuteUnitySubmit != null);
            Check("wiring.blockAdvance", A11yHost.BlockGameAdvance != null);
            Check("wiring.navToggle", A11yHost.NavTogglePressed != null);

            // ---- \u914d\u7f6e\u9ed8\u8ba4\u503c\u5fc5\u987b\u5408\u6cd5\uff08\u5ba1\u8ba1 F\uff1a\u6c89\u9ed8\u6309\u952e\u539f\u9ed8\u8ba4 "0" \u662f\u4e0d\u5408\u6cd5\u952e\u540d\uff09----
            Check("config.\u6c89\u9ed8\u6309\u952e\u7559\u7a7a\u6216\u5408\u6cd5",
                  string.IsNullOrEmpty(Plugin.CfgSilenceKey == null ? "" : Plugin.CfgSilenceKey.Value));
            string quitNames = Plugin.CfgQuitNames == null ? "" : (Plugin.CfgQuitNames.Value ?? "");
            Check("config.\u9000\u51fa\u786e\u8ba4\u9ed8\u8ba4\u5df2\u53d6\u8bc1\u5bf9\u8c61\u540d",
                  quitNames.IndexOf("Button_Exit", StringComparison.OrdinalIgnoreCase) >= 0, quitNames);
        }

'''
s2 = s2.replace(anchor, new + anchor, 1)
# \u5728 Run() \u672b\u5c3e\u8c03\u7528\uff08\u627e Run \u5185\u7684 Summary \u524d\u4e00\u884c\uff09
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(s2)
print("SelfTest: new blocks inserted")
