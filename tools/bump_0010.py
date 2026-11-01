import io
for p, old, new in [
  (r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\ManosabaA11y.csproj", "<Version>0.0.0.9</Version>", "<Version>0.0.0.10</Version>"),
  (r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs", 'public const string Version = "0.0.0.9";', 'public const string Version = "0.0.0.10";'),
]:
    s = io.open(p, encoding="utf-8-sig").read()
    assert old in s, p
    io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s.replace(old, new, 1))
    print("bumped", p.split("\\")[-1])
