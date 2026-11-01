import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/ManosabaA11y.csproj"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("<Version>0.0.0.11</Version>", "<Version>0.0.0.12</Version>")
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
p2 = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/Plugin.cs"
t = io.open(p2, encoding="utf-8-sig").read()
assert 'public const string Version = "0.0.0.11";' in t
t = t.replace('public const string Version = "0.0.0.11";', 'public const string Version = "0.0.0.12";')
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(t)
print("bumped to 0.0.0.12")
