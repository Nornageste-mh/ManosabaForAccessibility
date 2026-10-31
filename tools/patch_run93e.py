import io

p = r'D:\DSHWorkSpace\manosaba_a11y\probe\run93-mapreal.ps1'
s = io.open(p, encoding='utf-8-sig').read()

s = s.replace("    [switch]$MapFloor", "    [int]$MapFloor = 0", 1)
s = s.replace("if ($MapFloor) { $env:MANOSABA_A11Y_TEST_MAPFLOOR = '1' }",
              "if ($MapFloor -gt 0) { $env:MANOSABA_A11Y_TEST_MAPFLOOR = \"$MapFloor\" }", 1)
s = s.replace("if (-not $pickOk -and $MapFloor -and (Text-Has 'MAPCHOICE 切换楼层'))",
              "if (-not $pickOk -and $MapFloor -gt 0 -and (Text-Has 'MAPCHOICE 切换楼层'))", 1)

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('run93 floor param -> int')
