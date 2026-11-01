import io
p = r"D:\DSHWorkSpace\manosaba_a11y\tests\lib\Harness.ps1"
lines = io.open(p, encoding="utf-8-sig").read().split("\n")
out, n = [], 0
for i, ln in enumerate(lines):
    out.append(ln)
    if "while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec)" in ln or "while ($attempts -lt $MaxTries)" in ln:
        indent = "    "
        out.append(indent + "if ($script:CaseDeadline -and (Get-Date) -gt $script:CaseDeadline) { throw \"CASE_TIMEOUT: 用例超过声明时长，主动中止\" }")
        n += 1
io.open(p, "w", encoding="utf-8-sig", newline="\n").write("\n".join(out))
print("guarded loops:", n)
