import io
s = io.open("D:/DSHWorkSpace/manosaba_a11y/probe/run126-perf.ps1", encoding="utf-8-sig").read()
s = s.replace("$env:MANOSABA_A11Y_PERFDIAG = '1'", "$env:MANOSABA_A11Y_PERFDIAG = '1'\n$env:MANOSABA_A11Y_PERFTEST = '1'")
s = s.replace("if ($i -ge 55) { $modeSeen = $true; break }", "if ($i -ge 130) { $modeSeen = $true; break }")
s = s.replace('run126-perf.ps1', 'run127-perftest.ps1')
io.open("D:/DSHWorkSpace/manosaba_a11y/probe/run127-perftest.ps1", "w", encoding="utf-8-sig", newline="\n").write(s)
print("written run127-perftest.ps1")
