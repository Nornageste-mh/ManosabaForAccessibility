import io
src = io.open("D:/DSHWorkSpace/manosaba_a11y/probe/run125-scriptjump.ps1", encoding="utf-8-sig").read()
s = src
s = s.replace("$env:MANOSABA_A11Y_TEST_SCRIPTJUMP = '1'", "$env:MANOSABA_A11Y_TEST_SCRIPTJUMP = ''\n$env:MANOSABA_A11Y_PERFDIAG = '1'")
s = s.replace("if ((Log-Matches 'SCRIPTJUMP 跳转 path=').Count -gt 0) { $modeSeen = $true; break }",
              "if ($i -ge 55) { $modeSeen = $true; break }")
s = s.replace("""$lines += "---- SCRIPTJUMP ----"
$lines += (Log-Matches 'SCRIPTJUMP[^\\r\\n]*' | Select-Object -Last 25)""",
"""$lines += "---- PERF static ----"
$lines += (Log-Matches 'PERF (static|device|camera|urp)[^\\r\\n]*' | Select-Object -Last 10)
$lines += "---- PERF 10s ----"
$lines += (Log-Matches 'PERF 10s[^\\r\\n]*' | Select-Object -Last 8)
$lines += "---- PERF counts ----"
$lines += (Log-Matches 'PERF counts[^\\r\\n]*' | Select-Object -Last 6)""")
s = s.replace('$Tag = ', '$Tag = ')
s = s.replace('run125-scriptjump.ps1', 'run126-perf.ps1')
io.open("D:/DSHWorkSpace/manosaba_a11y/probe/run126-perf.ps1", "w", encoding="utf-8-sig", newline="\n").write(s)
print("written run126-perf.ps1")
