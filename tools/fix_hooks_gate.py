import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()

# 1) \u5728\u8bfb\u6240\u6709\u6d4b\u8bd5\u94a9\u5b50\u4e4b\u524d\u52a0\u4e00\u9053\u603b\u95f8\uff1a\u6ca1\u6709 TEST/DIAG \u73af\u5883\u53d8\u91cf\u65f6\uff0c
#    \u624b\u4e0a\u6b8b\u7559\u7684 MANOSABA_A11Y_TEST_* \u4e00\u5f8b\u4e0d\u751f\u6548\uff08\u907f\u514d\u73a9\u5bb6 shell \u91cc\u6b8b\u7559\u53d8\u91cf\u88ab\u5f53\u6210\u771f\u73a9\u5bb6\u8d70\uff09
anchor = """            // \u9009\u9879\u9a8c\u6536\uff1a\u64ad\u62a5\u540e\u7531\u63d2\u4ef6\u81ea\u5df1\u9009\u4e00\u6b21\u7b2c 1 \u9879\uff08\u4ec5 MANOSABA_A11Y_TEST_CHOICE=1\uff09"""
assert anchor in s
guard = """            // \u2605 \u6d4b\u8bd5\u94a9\u5b50\u603b\u95f8\uff1a\u6ca1\u6709 TEST/DIAG \u73af\u5883\u53d8\u91cf\u65f6\u4e00\u5f8b\u4e0d\u751f\u6548\u3002
            //   \u4e3a\u4ec0\u4e48\uff1a\u8fd9\u4e9b\u94a9\u5b50\u4f1a\u201c\u66ff\u73a9\u5bb6\u70b9\u9009\u9879 / \u8d70\u5730\u70b9 / \u6309\u5bf9\u8bdd\u6846\u6309\u94ae\u201d\uff0c
            //   \u4e00\u65e6\u73a9\u5bb6 shell \u91cc\u6b8b\u7559\u4e86\u67d0\u4e2a TEST_* \u53d8\u91cf\uff0c\u6b63\u5e38\u6e38\u620f\u5c31\u4f1a\u88ab\u63d2\u624b\u3002
            bool hooksAllowed = TestMode || A11yHost.DiagOn;
            if (!hooksAllowed)
            {
                TestNavMode = false; TestChoiceOnce = false; TestMapChoicePick = 0;
                TestMapPickKind = null; TestMapFloorCount = 0; TestEvModeOnce = false;
                TestEvForce = false; TestEvYes = false; TestEvPick = false; TestEvCmdMode = 0;
                TestRepeatOn = false; TestEscOn = false;
            }
"""
s = s.replace(anchor, guard + anchor, 1)

# 2) \u5bfc\u822a\u5f00\u5173\u952e\u89e3\u6790\u65e5\u5fd7
old = """                if (Hotkey.Parse(CfgSilenceKey.Value, out sp, out why))"""
assert old in s
nav = """                // \u5bfc\u822a\u5f00\u5173\u952e\u4e5f\u8981\u6253\u89e3\u6790\u7ed3\u679c\uff1a\u5199\u9519\u4e86\u5c31\u662f\u201c\u6309\u952e\u6ca1\u53cd\u5e94\u201d\uff0c\u800c\u73a9\u5bb6\u4e0d\u53ef\u80fd\u53bb\u770b\u914d\u7f6e\u6587\u4ef6\u3002
                if (Hotkey.Parse(CfgNavKey.Value, out sp, out why))
                    L.LogInfo("[a11y] \u5bfc\u822a\u5f00\u5173\u952e=" + CfgNavKey.Value + " -> KeyCode." + sp.Key
                              + (sp.Alt ? " +Alt" : "") + (sp.Ctrl ? " +Ctrl" : "") + (sp.Shift ? " +Shift" : ""));
                else
                    L.LogWarning("[a11y] \u5bfc\u822a\u5f00\u5173\u952e\u8ba4\u4e0d\u51fa\u6765\uff1a" + CfgNavKey.Value + " \u2014\u2014 " + why);
"""
s = s.replace(old, nav + old, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Plugin hooks gate + nav key log")
