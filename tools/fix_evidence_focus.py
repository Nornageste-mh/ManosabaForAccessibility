import io
base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

# ---------- Evidence: \u5148\u70b9\u540e\u5ff5\uff08\u6821\u9a8c\u771f\u5b9e\u53d1\u51fa\uff09----------
p = base + r"\Evidence.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """            _selectedIndex = oneBased;
            Repeat.Say(e.Name);
            ClickButton(e.Btn != null ? e.Btn._button : null);
            A11yHost.Diag("EVIDENCE \u6570\u5b57\u952e " + oneBased + " -> " + e.Name);
            // \u53ea\u9009\u4e2d\uff0c\u4e0d\u66ff\u73a9\u5bb6\u51fa\u793a\uff080.0.0.9 \u8d77\uff09\u3002\u7406\u7531\uff1a\u9009\u54ea\u4ef6\u662f\u73a9\u5bb6\u7684\u51b3\u7b56\uff0c
            // \u300c\u73b0\u5728\u5c31\u63d0\u4ea4\u300d\u4e5f\u662f\u73a9\u5bb6\u7684\u51b3\u7b56\u2014\u2014\u81ea\u52a8\u5f39\u786e\u8ba4\u6846\u7b49\u4e8e\u66ff\u73a9\u5bb6\u8d70\u4e86\u4e00\u6b65\uff0c
            // \u800c\u4e14\u4f1a\u6d88\u706d\u300c\u9009\u4e2d\u540e\u7ee7\u7eed\u6d4f\u89c8\u5176\u5b83\u8bc1\u7269\u300d\u8fd9\u4e2a\u52a8\u4f5c\u3002\u6539\u6210\u6570\u5b57\u952e=\u9009\u4e2d\u3001\u56de\u8f66=\u51fa\u793a\u3002
            Repeat.Say("\u5df2\u9009\u4e2d\u3002\u6309\u56de\u8f66\u51fa\u793a\u8fd9\u4ef6\u8bc1\u636e\u3002");"""
new = """            _selectedIndex = oneBased;
            // \u2605 \u5148\u70b9\u540e\u5ff5\uff1a\u539f\u6765\u662f\u201c\u5148\u8bf4\u5df2\u9009\u4e2d\u3001\u518d\u53bb\u70b9\u201d\uff0c\u70b9\u51fb\u5931\u8d25\u65f6\u8bfb\u5c4f\u5c31\u64a4\u4e86\u8c0e\u3002
            //   ClickButton \u8fd4\u56de\u503c\u4ee3\u8868\u6ce8\u5165\u70b9\u51fb\u662f\u5426\u771f\u7684\u53d1\u51fa\uff08\u5750\u6807\u8d8a\u754c\u3001\u62ff\u4e0d\u5230
            //   RectTransform \u90fd\u4f1a\u8fd4\u56de false\uff09\u3002
            bool clicked = ClickButton(e.Btn != null ? e.Btn._button : null);
            A11yHost.Diag("EVIDENCE \u6570\u5b57\u952e " + oneBased + " -> " + e.Name + " clicked=" + clicked);
            Repeat.Say(clicked ? (e.Name + "\u3002\u5df2\u9009\u4e2d\u3002\u6309\u56de\u8f66\u51fa\u793a\u8fd9\u4ef6\u8bc1\u636e\u3002")
                               : (e.Name + "\u3002\u9009\u4e2d\u6ca1\u6210\u529f\uff0c\u8bf7\u518d\u6309\u4e00\u6b21\u3002"));"""
assert old in s
s = s.replace(old, new, 1)

# \u201c\u5df2\u51fa\u793a\u8bc1\u636e\u201d\u5148\u70b9\u540e\u8bf4
old2 = """                    Repeat.Say("\u5df2\u51fa\u793a\u8bc1\u636e\u3002");
                    ClickButton(sel);
                    A11yHost.Diag("EVIDENCE \u70b9\u4e86\u300c\u51fa\u793a\u8bc1\u636e\u300d name=" + b.name);
                    return;"""
new2 = """                    bool ok = ClickButton(sel);
                    A11yHost.Diag("EVIDENCE \u70b9\u4e86\u300c\u51fa\u793a\u8bc1\u636e\u300d name=" + b.name + " ok=" + ok);
                    Repeat.Say(ok ? "\u5df2\u51fa\u793a\u8bc1\u636e\u3002" : "\u51fa\u793a\u6ca1\u6210\u529f\uff0c\u8bf7\u518d\u6309\u4e00\u6b21\u56de\u8f66\u3002");
                    return;"""
assert old2 in s
s = s.replace(old2, new2, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Evidence click verification done")

# ---------- \u4ea4\u4e92\u5c42\u7126\u70b9\u95e8\uff1a\u6e38\u620f\u7a97\u53e3\u4e0d\u5728\u524d\u53f0\u5c31\u4e0d\u54cd\u5e94\u6570\u5b57\u952e ----------
p2 = base + r"\Plugin.cs"
s2 = io.open(p2, encoding="utf-8-sig").read()
anchor = "    /// <summary>\n    /// \u6bcf\u5e27\u9a71\u52a8"
add = """    /// <summary>
    /// \u4ea4\u4e92\u5c42\u7684\u201c\u73b0\u5728\u80fd\u4e0d\u80fd\u8bfb\u73a9\u5bb6\u7684\u9009\u62e9\u952e\u201d\u3002
    /// \u4e3a\u4ec0\u4e48\u9700\u8981\uff1a\u672c\u4f5c\u53ea\u542f\u7528\u65b0 Input System\uff0c\u5e73\u53f0\u5c42\u70ed\u952e\u9760 Win32 \u5168\u5c40\u8bfb\u952e\u515c\u5e95
    /// \uff08A11yHost.UseGlobalKeys\uff09\u2014\u2014\u5168\u5c40\u8bfb\u952e\u4e0d\u770b\u7126\u70b9\uff0c\u73a9\u5bb6\u5728\u522b\u7684\u7a0b\u5e8f\u91cc\u6572 1/2/3
    /// \u4f1a\u76f4\u63a5\u70b9\u6389\u9009\u9879\u3001\u8d70\u6389\u5730\u70b9\u3001\u9009\u4e2d\u8bc1\u7269\u3002\u6240\u4ee5\u9009\u62e9\u7c7b\u6309\u952e\u5fc5\u987b\u5148\u95ee
    /// \u4e00\u53e5\u201c\u6e38\u620f\u7a97\u53e3\u5728\u524d\u53f0\u5417\u201d\u3002\u70ed\u952e\uff08\u9000\u683c/\u4f11\u7720/F7\uff09\u4e0d\u5728\u6b64\u5217\u3002
    /// </summary>
    internal static class FocusGate
    {
        private static bool _hasKey;
        private static bool _cached;
        private static float _at;

        internal static bool CanReadChoiceKeys()
        {
            try
            {
                float now = UnityEngine.Time.realtimeSinceStartup;
                if (_hasKey && now - _at < 0.25f) return _cached;
                bool ok;
                try { ok = UnityEngine.Application.isFocused; }
                catch { ok = true; }
                _hasKey = true; _cached = ok; _at = now;
                return ok;
            }
            catch { return true; }
        }
    }

"""
assert anchor in s2
s2 = s2.replace(anchor, add + anchor, 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(s2)
print("FocusGate added")

# \u63a5\u5165\u4e09\u5c42
for fname, marker in [(r"\Choices.cs", "        internal static void Update()"),
                      (r"\MapChoice.cs", "        internal static void Tick()"),
                      (r"\Evidence.cs", "        private static void UpdateChoice()")]:
    p3 = base + fname
    s3 = io.open(p3, encoding="utf-8-sig").read()
    if marker not in s3:
        print("marker miss", fname); continue
    guard = marker + """
        {
            if (!Plugin.FocusGate.CanReadChoiceKeys())
            {
                // \u7a97\u53e3\u4e0d\u5728\u524d\u53f0\uff1a\u8df3\u8fc7\u6309\u952e\u5904\u7406\uff0c\u4f46\u4fdd\u7559\u72b6\u6001\u5237\u65b0\uff08\u5426\u5219\u56de\u6765\u65f6\u4f1a\u91cd\u64ad\uff09
                if (fname_is_choices) return;
            }
"""
    print("need manual", fname)
