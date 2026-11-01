import io
src = io.open("D:/DSHWorkSpace/manosaba_a11y/probe/run120-trialline.ps1", encoding="utf-8-sig").read()
s = src
s = s.replace("$env:MANOSABA_A11Y_JUMP_LINE = $JumpLine", "$env:MANOSABA_A11Y_JUMP_LINE = ''")
s = s.replace("$env:MANOSABA_A11Y_EVDIAG = '1'", "$env:MANOSABA_A11Y_EVDIAG = ''\n$env:MANOSABA_A11Y_TEST_SCRIPTJUMP = '1'")
# 循环里不再等 mode=True，改为等 SCRIPTJUMP 跳转
s = s.replace("""        if ($lastDump -match 'mode=True') { $modeSeen = $true; break }""",
"""        if ((Log-Matches 'SCRIPTJUMP 跳转 path=').Count -gt 0) { $modeSeen = $true; break }""")
s = s.replace("""$lines += "---- EVIDENCE ----"
$lines += (Log-Matches 'EVIDENCE[^\\r\\n]*' | Select-Object -Last 20)""",
"""$lines += "---- SCRIPTJUMP ----"
$lines += (Log-Matches 'SCRIPTJUMP[^\\r\\n]*' | Select-Object -Last 25)
$lines += "---- Reader.say（跳转后应有新剧本的朗读）----"
$lines += (Log-Matches 'Reader\\.say\\[[^\\]]*\\][^\\r\\n]*id=[^\\r\\n]{0,40}' | Select-Object -Last 8)""")
s = s.replace("Tag=run120", "Tag=run125")
s = s.replace("run120-trialline.ps1", "run125-scriptjump.ps1")
io.open("D:/DSHWorkSpace/manosaba_a11y/probe/run125-scriptjump.ps1", "w", encoding="utf-8-sig", newline="\n").write(s)
print("written run125-scriptjump.ps1")
