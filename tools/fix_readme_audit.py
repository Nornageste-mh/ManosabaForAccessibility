import io
p = r"D:\DSHWorkSpace\manosaba_a11y\README.md"
s = io.open(p, encoding="utf-8-sig").read()

# 1) \u8865\u4e00\u884c\u5ba1\u5224 HUD \u4e0e\u65b0\u4fee\u590d\u9879\uff08\u542b\u672a\u5b9e\u6d4b\u6807\u6ce8\uff09
old = "| \u5ba1\u5224\u753b\u9762 HUD\uff08\u8df3\u8fc7\u6309\u94ae\uff09 | \u2705 `DebateUI/Wrapper/SkipButton/Button \u2192 \u8df3\u8fc7\u8fa9\u8bba`\uff1b\u5012\u8ba1\u65f6\u4e0e HP \u6761**\u4e0d\u662f\u53ef\u4ea4\u4e92\u63a7\u4ef6\uff0c\u8bfb\u4e0d\u5230** |"
new = """| \u5ba1\u5224\u753b\u9762 HUD\uff08\u8df3\u8fc7\u6309\u94ae\uff09 | \u2705 `DebateUI/Wrapper/SkipButton/Button \u2192 \u8df3\u8fc7\u8fa9\u8bba` |
| \u5ba1\u5224\u5012\u8ba1\u65f6 / HP\uff080.0.0.10 \u65b0\u589e\uff09 | \u26a0\ufe0f \u5df2\u6302\u4e09\u4e2a void \u8865\u4e01\uff08`Timer.SetTime` / `Timer.ForceRemainingTimeToNearlyZero` / `DebateUi.OnCurrentHpChanged`\uff09\uff0c\u53ea\u5728\u8de8\u8fc7 5\u5206/2\u5206/1\u5206/30\u79d2/10\u79d2\u9608\u503c\u65f6\u62a5\u4e00\u53e5\uff1b**\u672a\u5b9e\u673a\u590d\u6d4b**\uff08\u9700\u6253\u5230\u5ba1\u5224\u6218\u6597\u6bb5\uff09 |"""
assert old in s
s = s.replace(old, new, 1)

# 2) \u5c55\u89c8\u4fee\u6b63\uff1a\u7528\u4f8b\u6570\u4e0e\u65ad\u8a00\u6570
s = s.replace("pwsh -File tests\\Suite.ps1 -Only unit.selftest   # \u5355\u5143\u5c42\uff1a\u7eaf\u903b\u8f91 26 \u6761\u65ad\u8a00\uff08\u7ea6 14 \u79d2\uff09",
              "pwsh -File tests\\Suite.ps1 -Only unit.selftest   # \u5355\u5143\u5c42\uff1a\u7eaf\u903b\u8f91 45 \u6761\u65ad\u8a00\uff08\u7ea6 40 \u79d2\uff09", 1)
s = s.replace("pwsh -File tests\\Suite.ps1                       # \u5168\u90e8 11 \u4e2a\u7528\u4f8b\uff1a\u6807\u9898/\u5bf9\u8bdd/\u8bf4\u8bdd\u4eba/\u914d\u97f3/\u9000\u683c/\u9009\u9879/\u8bae\u8bba/\u52a0\u8f7d/\u6574\u9875\u754c\u9762/\u8bed\u8a00",
              "pwsh -File tests\\Suite.ps1                       # \u9ed8\u8ba4\u6279\u6b21 11 \u4e2a\u7528\u4f8b\uff08\u53e6 2 \u4e2a\u9700\u771f\u4eba\u70b9\u51fb\u7684\u7528\u4f8b\u6807\u4e86 Skip\uff09", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("README truth fixes")
