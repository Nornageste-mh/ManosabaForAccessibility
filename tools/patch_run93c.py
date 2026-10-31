import io

p = r'D:\DSHWorkSpace\manosaba_a11y\probe\run93-mapreal.ps1'
s = io.open(p, encoding='utf-8-sig').read()

s = s.replace("""    [int]$WaitSeconds = 180""",
              """    [int]$WaitSeconds = 180,
    [int]$MapPick = 1""")

s = s.replace("$env:MANOSABA_A11Y_TEST_MAPCHOICE = '1'",
              "$env:MANOSABA_A11Y_TEST_MAPCHOICE = \"$MapPick\"")

s = s.replace("if (-not $pickOk -and (Text-Has 'MAPCHOICE 数字键 1 ->'))",
              "if (-not $pickOk -and (Text-Has 'MAPCHOICE 数字键 \\d+ ->'))")

s = s.replace('Write-Host "[info] pid=$($proc.Id) jump=$Script 等 34s"',
              'Write-Host "[info] pid=$($proc.Id) jump=$Script pick=$MapPick 等 34s"')

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('run93 patched for MapPick')
