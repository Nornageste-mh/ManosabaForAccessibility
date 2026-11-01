import io
# \u7248\u672c\u53f7\u63d0\u5230 0.0.0.11\uff08csproj + Plugin\uff09
for p, old, new in [
  (r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\ManosabaA11y.csproj", "<Version>0.0.0.10</Version>", "<Version>0.0.0.11</Version>"),
  (r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs", 'public const string Version = "0.0.0.10";', 'public const string Version = "0.0.0.11";'),
]:
    s = io.open(p, encoding="utf-8-sig").read()
    assert old in s, p
    io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s.replace(old, new, 1))
    print("bumped", p.split("\\")[-1])

# \u6587\u6863\u7248\u672c\u53f7\u4e0e\u5feb\u63a5\u952e\u8868
docs = {
 r"D:\DSHWorkSpace\manosaba_a11y\README.md": [
   ("v0.0.0.10", "v0.0.0.11"),
   ("| `Backspace` |", "| `=` |"),
 ],
 r"D:\DSHWorkSpace\manosaba_a11y\mod\package\\u5b89\u88c5\u8bf4\u660e.md": [
   ("v0.0.0.10", "v0.0.0.11"),
   ("`Backspace`", "`=`"),
 ],
 r"D:\DSHWorkSpace\manosaba_a11y\mod\package\\u5e38\u89c1\u95ee\u9898.md": [
   ("v0.0.0.10", "v0.0.0.11"),
   ("`Backspace`", "`=`"),
 ],
 r"D:\DSHWorkSpace\manosaba_a11y\\u6d4b\u8bd5\u95ee\u5377.md": [("0.0.0.10", "0.0.0.11")],
 r"D:\DSHWorkSpace\manosaba_a11y\\u65e0\u969c\u788d\u53ef\u884c\u6027\u9a8c\u8bc1.md": [("0.0.0.10", "0.0.0.11")],
}
for p, pairs in docs.items():
    s = io.open(p, encoding="utf-8-sig").read()
    for a, b in pairs:
        s = s.replace(a, b)
    io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
    print("docs updated:", p.split("\\")[-1])
