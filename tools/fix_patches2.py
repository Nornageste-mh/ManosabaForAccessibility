import io
base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

# --- Plugin.cs: \u53bb\u6389 P0 \u5f15\u7528\uff0c\u5e76\u628a\u8bca\u65ad\u5199\u5165\u6539\u6210\u7eaf\u5185\u5b58 ---
p = base + r"\Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("                if (!skipPanel) _harmony.PatchAll(typeof(Patches.Patch_PanelAddMessage));\n", "", 1)
s = s.replace("""                // printer \u7ea7 AddMessage / SetMessages\uff1a\u672c\u4f5c ADV \u771f\u5165\u53e3\uff08\u9762\u677f\u7ea7\u4e00\u6761\u90fd\u4e0d\u54cd\uff09\uff0c""",
              """                // printer \u7ea7 AddMessage / SetMessages\uff1a\u672c\u4f5c ADV \u771f\u5165\u53e3\uff08\u9762\u677f\u7ea7\u4e00\u6761\u90fd\u4e0d\u54cd\uff0c\u5df2\u5220\u6389\u90a3\u6761\u8865\u4e01\uff09\uff0c""", 1)
# cfg \u6c61\u67d3\uff1aConfigEntry.Value = true \u4f1a\u843d\u76d8\u5230\u73a9\u5bb6 cfg\uff08\u5b9e\u6d4b\u7b2c157\u884c\u5df2\u88ab\u6c38\u4e45\u6539\u6210 true\uff09
s = s.replace("                    CfgDiagLog.Value = true;", "                    A11yHost.DiagOn = true; // \u7eaf\u5185\u5b58\uff1a\u4e0d\u80fd\u628a\u73a9\u5bb6\u7684 cfg \u6c38\u4e45\u6539\u6210\u5f00\u542f", 1)
s = s.replace("                    CfgDiagLog.Value = true;", "                    A11yHost.DiagOn = true; // \u540c\u4e0a", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Plugin.cs patched (panel patch removed, diag in memory)")

# --- Choices.cs: \u63d2\u4ef6\u56de\u8c03\u5305 try/catch\uff08\u5f02\u5e38\u4f1a\u6cbf AddChoiceButton \u7684\u8c03\u7528\u94fe\u5916\u6cc4\uff09---
p2 = base + r"\Choices.cs"
s2 = io.open(p2, encoding="utf-8-sig").read()
s2 = s2.replace("""        internal static void OnButtonAdded(Naninovel.UI.ChoiceHandlerPanel panel)
        {
            if (panel != null) _panel = panel;
        }""",
"""        internal static void OnButtonAdded(Naninovel.UI.ChoiceHandlerPanel panel)
        {
            // \u4e3a\u4ec0\u4e48\u8981 try/catch\uff1a\u8fd9\u4e2a\u56de\u8c03\u6302\u5728 AddChoiceButton \u7684 Postfix \u4e0a\uff0c
            // \u5f02\u5e38\u4f1a\u6cbf\u6e38\u620f\u7684 async \u8c03\u7528\u94fe\u5916\u6cc4\uff082022-10-29 \u5c31\u662f\u8fd9\u6837\u5361\u6b7b\u5267\u60c5\u7684\uff09\u3002
            try { if (panel != null) _panel = panel; }
            catch (Exception e) { A11yHost.Diag("Choices.OnButtonAdded: " + e.Message); }
        }""", 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(s2)
print("Choices.cs patched")
