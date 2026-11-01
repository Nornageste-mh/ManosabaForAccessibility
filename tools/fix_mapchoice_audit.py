import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs"
s = io.open(p, encoding="utf-8-sig").read()

# 1) \u53ea\u6d3e\u53d1\u4e00\u6b21\u70b9\u51fb\uff1a\u4f18\u5148\u771f\u5b9e\u8f93\u5165\u6ce8\u5165\uff0c\u5931\u8d25\u624d\u56de\u9000\u5408\u6210\u6d3e\u53d1
old = """                Repeat.Say(e.Name);          // \u9009\u524d\u590d\u8ff0\uff0c\u786e\u8ba4\u9009\u7684\u662f\u54ea\u4e2a\u5730\u70b9
                bool ok = e.Area != null ? ClickArea(e.Area) : false;
                if (!ok) e.Button.onClick.Invoke();
                A11yHost.Diag("MAPCHOICE \u70b9\u51fb\u6d3e\u53d1 ok=" + ok + " kind="
                              + (e.Area == null ? "?" : SafeKind(e.Area)));
                // \u5408\u6210\u6d3e\u53d1\u4e4b\u540e\u8865\u4e00\u6b21\u300c\u771f\u5b9e\u8f93\u5165\u300d\u70b9\u51fb\uff1arun117/run118 \u8bc1\u660e\u5149\u9760\u5408\u6210\u6d3e\u53d1\uff0c
                // \u6e38\u620f\u7684 OnChoiceMapAreaAsync \u4e0d\u4f1a\u8fd4\u56de\uff08\u5730\u56fe\u9875\u4e00\u76f4\u4e0d\u5173\uff09\u3002
                if (e.Area != null) ClickByInput(e.Area);"""
new = """                Repeat.Say(e.Name);          // \u9009\u524d\u590d\u8ff0\uff0c\u786e\u8ba4\u9009\u7684\u662f\u54ea\u4e2a\u5730\u70b9
                // \u2605 \u53ea\u6d3e\u53d1**\u4e00\u6b21**\u3002\u539f\u6765\u662f\u300c\u5148\u5408\u6210\u6d3e\u53d1\u3001\u518d\u771f\u5b9e\u6ce8\u5165\u300d\u4e24\u6b21\u70b9\u51fb\uff0c
                //   run117/run118 \u5b9e\u6d4b\u51fa\u73b0\u300c\u9009\u98df\u5802\u5374\u8d70\u4e86\u533b\u52a1\u5ba4\u300d\u2014\u2014\u7b2c\u4e00\u6b21\u70b9\u51fb\u5df2\u7ecf
                //   \u8ba9\u6e38\u620f\u9009\u4e86\u4e00\u4e2a\u5730\u70b9\uff0c\u7b2c\u4e8c\u6b21\u53c8\u78b0\u5230\u4e86\u53e6\u4e00\u4e2a\u3002\u771f\u6b63\u80fd\u8ba9
                //   OnChoiceMapAreaAsync \u8fd4\u56de\u7684\u53ea\u6709\u771f\u5b9e\u8f93\u5165\u6ce8\u5165\uff0c\u6240\u4ee5\u4f18\u5148\u7528\u5b83\u3002
                bool ok = e.Area != null && ClickByInput(e.Area);
                if (!ok)
                {
                    ok = e.Area != null ? ClickArea(e.Area) : false;
                    if (!ok) e.Button.onClick.Invoke();
                }
                A11yHost.Diag("MAPCHOICE \u70b9\u51fb\u6d3e\u53d1 \u65b9\u5f0f=" + (ok ? "\u771f\u5b9e\u6ce8\u5165" : "\u5408\u6210")
                              + " kind=" + (e.Area == null ? "?" : SafeKind(e.Area)));"""
assert old in s
s = s.replace(old, new, 1)

# 2) \u6ce8\u5165\u524d\u505a\u5c4f\u5e55\u8303\u56f4\u68c0\u67e5\uff08\u4e0e Evidence \u4e00\u81f4\uff09
old2 = """                UnityEngine.Vector2 sp = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, rt.position);
                InputInject.Click(sp.x, sp.y, 0.16f);"""
new2 = """                UnityEngine.Vector2 sp = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, rt.position);
                // \u5750\u6807\u8d8a\u754c\uff08\u5c4f\u5e55\u5916 / \u8d1f\u6570\uff09\u5c31\u4e0d\u70b9\uff1a\u672c\u4f5c\u5730\u56fe\u6309\u94ae\u6302\u5728 WorldSpace \u753b\u5e03\uff0c
                // \u62ff\u4e0d\u5230\u76f8\u673a\u65f6\u4f1a\u7b97\u51fa\u8d1f\u5750\u6807\uff08\u5b9e\u6d4b screen=-7,-2\uff09\uff0c\u90a3\u6837\u70b9\u51fb\u843d\u5728\u7a97\u5916\u3002
                float sw = UnityEngine.Screen.width, sh = UnityEngine.Screen.height;
                if (sp.x < 0 || sp.y < 0 || sp.x > sw || sp.y > sh)
                {
                    A11yHost.Diag("MAPCHOICE \u6ce8\u5165\u70b9\u51fb\u8d8a\u754c\uff0c\u653e\u5f03: screen="
                                  + sp.x.ToString("0") + "," + sp.y.ToString("0") + " \u5c4f\u5e55=" + sw + "x" + sh);
                    return false;
                }
                InputInject.Click(sp.x, sp.y, 0.16f);"""
assert old2 in s
s = s.replace(old2, new2, 1)

# 3) PageUp/PageDown\uff1a\u5bfc\u822a\u6a21\u5f0f\u5f00\u7740\u65f6\u4e0d\u62a2\u952e\uff08\u5e73\u53f0\u5c42\u7528\u5b83\u5207\u7ec4\uff09
old3 = """                if (_entries.Count > PageSize)
                {
                    if (KeyEdge.Pressed(KeyCode.PageDown)) { TurnPage(1); return; }"""
new3 = """                // \u2605 \u5bfc\u822a\u6a21\u5f0f\u5f00\u7740\u65f6\u4e0d\u62a2 PageUp/PageDown\uff1a\u5e73\u53f0\u5c42 UiNav \u7528\u5b83\u4eec
                //   \u5207\u6362\u7ec4\uff08UiNav.cs SwitchGroup\uff09\uff0c\u4e24\u8fb9\u540c\u65f6\u6d88\u8d39\u4f1a\u8df3\u4e24\u6b65\u3002
                bool navOwnsPaging = Plugin.CfgMenuNav != null && Plugin.CfgMenuNav.Value
                                     && Plugin.CfgStartupHint != null; // \u5bfc\u822a\u6a21\u5f0f\u662f\u5426\u5f00\u7740\u65e0\u6cd5\u76f4\u63a5\u8bfb\uff0c\u6545\u7528\u914d\u7f6e\u964d\u7ea7\u5224\u636e
                if (_entries.Count > PageSize)
                {
                    if (!navOwnsPaging && KeyEdge.Pressed(KeyCode.PageDown)) { TurnPage(1); return; }"""
assert old3 in s
s = s.replace(old3, new3, 1)
s = s.replace("""                    if (KeyEdge.Pressed(KeyCode.PageUp)) { TurnPage(-1); return; }""",
              """                    if (!navOwnsPaging && KeyEdge.Pressed(KeyCode.PageUp)) { TurnPage(-1); return; }""", 1)
s = s.replace("""                    if (KeyEdge.Pressed(KeyCode.PageDown)) { SwitchFloor(1); return; }""",
              """                    if (!navOwnsPaging && KeyEdge.Pressed(KeyCode.PageDown)) { SwitchFloor(1); return; }""", 1)
s = s.replace("""                    if (KeyEdge.Pressed(KeyCode.PageUp)) { SwitchFloor(-1); return; }""",
              """                    if (!navOwnsPaging && KeyEdge.Pressed(KeyCode.PageUp)) { SwitchFloor(-1); return; }""", 1)

# 4) Clear() \u4e5f\u6e05\u72b6\u6001\u5224\u636e\uff08\u4fdd\u7559 _choiceDiagAt \u7684\u4f8b\u5916\uff09
s = s.replace("""            _floorTestAt = 0f;""",
              """            _floorTestAt = 0f;
            _choiceMode = false;
            _allowedKinds = null;""", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("MapChoice.cs patched")
