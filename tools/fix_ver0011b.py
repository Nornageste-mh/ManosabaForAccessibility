import io
docs = {
 "D:/DSHWorkSpace/manosaba_a11y/mod/package/安装说明.md": [("v0.0.0.10", "v0.0.0.11"), ("`Backspace`", "`=`")],
 "D:/DSHWorkSpace/manosaba_a11y/mod/package/常见问题.md": [("v0.0.0.10", "v0.0.0.11"), ("`Backspace`", "`=`")],
 "D:/DSHWorkSpace/manosaba_a11y/测试问卷.md": [("0.0.0.10", "0.0.0.11")],
 "D:/DSHWorkSpace/manosaba_a11y/无障碍可行性验证.md": [("0.0.0.10", "0.0.0.11")],
}
for p, pairs in docs.items():
    s = io.open(p, encoding="utf-8-sig").read()
    for a, b in pairs:
        s = s.replace(a, b)
    io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
    print("docs updated:", p.split("/")[-1])
