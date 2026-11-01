import io
pc = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/Choices.cs"
s = io.open(pc, encoding="utf-8-sig").read()
old = "        internal static void OnButtonAdded(Naninovel.UI.ChoiceHandlerPanel panel)"
new = """        /// <summary>当前画面上是否有正在等待选择的选项（剧情跳转等开发者功能据此提醒）。</summary>
        internal static bool HasChoice => _buttons.Count > 0;

        internal static void OnButtonAdded(Naninovel.UI.ChoiceHandlerPanel panel)"""
assert s.count(old) == 1
s = s.replace(old, new)
io.open(pc, "w", encoding="utf-8-sig", newline="\n").write(s)

pj = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/ScriptJump.cs"
t = io.open(pj, encoding="utf-8-sig").read()
old2 = """                _active = false;
                _verifyAt = Time.realtimeSinceStartup + 3f;"""
new2 = """                _active = false;
                // 跳转时若画面正停在选项上，游戏的选择状态还挂着，跳过去可能不立刻打印台词。
                // 这是开发功能，不替玩家做选择，只提醒一句。
                if (Choices.HasChoice)
                    Repeat.Say("注意：画面刚才停在选项上，跳转后如果没声音，先按数字键选一项。", true);
                _verifyAt = Time.realtimeSinceStartup + 3f;"""
assert t.count(old2) == 1
t = t.replace(old2, new2)
io.open(pj, "w", encoding="utf-8-sig", newline="\n").write(t)
print("patched")
