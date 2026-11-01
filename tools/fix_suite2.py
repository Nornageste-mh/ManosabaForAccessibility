import io
p = r"D:\DSHWorkSpace\manosaba_a11y\tests\Suite.ps1"
s = io.open(p, encoding="utf-8-sig").read()
# P0-2: \u771f\u6b63\u751f\u6548\u7684\u7528\u4f8b\u770b\u95e8\u72d7\uff08\u5382\u65f6\u5230\u70b9\u5c31\u6253\u65ad\u7528\u4f8b\uff09
old = """    $sw = [Diagnostics.Stopwatch]::StartNew()
    $game = $null"""
new = """    $sw = [Diagnostics.Stopwatch]::StartNew()
    # \u7528\u4f8b\u5382\u65f6\u770b\u95e8\u72d7\uff1a\u65e7\u7248\u672c\u7684 $case.Timeout \u4ece\u6765\u6ca1\u88ab\u8bfb\u8fc7\uff0c
    # \u4e00\u4e2a\u5361\u4f4f\u7684\u7528\u4f8b\u4f1a\u62d6\u5230\u81ea\u5df1\u7684\u5185\u90e8\u8d85\u65f6\uff08\u5b9e\u6d4b menu.title 305s > \u58f0\u660e 300s\uff09\u3002
    $script:CaseDeadline = if ($case.ContainsKey('Timeout') -and $case.Timeout -gt 0) {
        (Get-Date).AddSeconds([double]$case.Timeout * $TimeoutScale)
    } else { (Get-Date).AddHours(1) }
    $game = $null"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Suite.ps1: case deadline set")

# Harness: \u4e09\u5904\u7b49\u5f85\u5faa\u73af\u91cc\u68c0\u67e5\u5382\u65f6
p2 = r"D:\DSHWorkSpace\manosaba_a11y\tests\lib\Harness.ps1"
h = io.open(p2, encoding="utf-8-sig").read()
guard = """        if ($script:CaseDeadline -and (Get-Date) -gt $script:CaseDeadline) {
            throw "CASE_TIMEOUT: 用例超过声明时长，主动中止（$Pattern）"
        }
"""
count = 0
import re
# \u5728 Wait-Log / Advance-Until / Enter-Story \u7684 while \u5faa\u73af\u4f53\u5f00\u5934\u63d2\u5165
for marker in ["    while ((Get-Date) -lt $deadline) {", "        while ((Get-Date) -lt $deadline) {"]:
    pass
lines = h.split("\n")
out = []
for i, ln in enumerate(lines):
    out.append(ln)
    if "while ((Get-Date) -lt $deadline)" in ln:
        indent = ln[:len(ln) - len(ln.lstrip())] + "    "
        out.append(indent + "if ($script:CaseDeadline -and (Get-Date) -gt $script:CaseDeadline) { throw \"CASE_TIMEOUT: 用例超过声明时长，主动中止\" }")
        count += 1
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write("\n".join(out))
print("Harness waits guarded:", count)
