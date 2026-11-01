import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\release.ps1"
lines = io.open(p, encoding="utf-8-sig").read().split("\n")
out = []
i = 0
while i < len(lines):
    ln = lines[i]
    if ln.startswith('if ($md.Count -lt 2)'):
        out.append('# 光数量不够：以前只要求“≥ 2 个 md”，改个名字也能通过。现在要求名字必须对得上。')
        out.append('$needMd = @("安装说明.md", "常见问题.md", "更新日志.md")')
        out.append('$missMd = @($needMd | Where-Object { -not (Test-Path -LiteralPath (Join-Path $pkg $_)) })')
        out.append('if ($missMd.Count -gt 0) { Fail "包根缺说明文档：$($missMd -join ' + "'、'" + ')" }')
        i += 1
        continue
    out.append(ln)
    i += 1
io.open(p, "w", encoding="utf-8-sig", newline="\n").write("\n".join(out))
print("md check replaced")
