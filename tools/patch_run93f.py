import io

p = r'D:\DSHWorkSpace\manosaba_a11y\probe\run93-mapreal.ps1'
s = io.open(p, encoding='utf-8-sig').read()

s = s.replace("    [int]$MapFloor = 0", "    [int]$MapFloor = 0,\n    [string]$PickKind = ''", 1)
s = s.replace("if ($MapFloor -gt 0) { $env:MANOSABA_A11Y_TEST_MAPFLOOR = \"$MapFloor\" }",
              "if ($MapFloor -gt 0) { $env:MANOSABA_A11Y_TEST_MAPFLOOR = \"$MapFloor\" }\nif ($PickKind) { $env:MANOSABA_A11Y_TEST_MAPPICK_KIND = $PickKind }", 1)
s = s.replace("'MANOSABA_A11Y_TEST_MAPFLOOR') {",
              "'MANOSABA_A11Y_TEST_MAPFLOOR','MANOSABA_A11Y_TEST_MAPPICK_KIND') {", 1)
s = s.replace("if (-not $pickOk -and (Text-Has 'MAPCHOICE 数字键 \\d+ ->'))",
              "if (-not $pickOk -and (Text-Has 'MAPCHOICE 测试：按 kind 点击')) { $pickOk = $true; $pickAt = Get-Date; Write-Host '[step5] kind 点击已触发' }\nif (-not $pickOk -and (Text-Has 'MAPCHOICE 数字键 \\d+ ->'))", 1)
s = s.replace("""$lines += @([regex]::Matches($t, 'MAPCHOICE (?:切换楼层|测试钩子|数字键)[^\\r\\n]*')""",
              """$lines += @([regex]::Matches($t, 'MAPCHOICE (?:切换楼层|测试钩子|数字键|变量|测试：按 kind)[^\\r\\n]*')""", 1)

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('run93: -PickKind added')
