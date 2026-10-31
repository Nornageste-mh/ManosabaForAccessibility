import io

p = r'D:\DSHWorkSpace\manosaba_a11y\probe\run93-mapreal.ps1'
s = io.open(p, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:70])
    s = s.replace(old, new, 1)


rep("""    [int]$MapPick = 1""",
    """    [int]$MapPick = 1,
    [switch]$MapFloor""")

rep("""    [int]$MapPick = 1,
    [switch]$MapFloor""",
    """    [int]$MapPick = 1,
    [switch]$MapFloor""")

rep("""    Set-Item -Path "Env:$v" -Value ''""",
    """    Set-Item -Path "Env:$v" -Value ''""")

rep("""               'MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_DEBUGUI','MANOSABA_A11Y_TEST') {""",
    """               'MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_DEBUGUI','MANOSABA_A11Y_TEST',
               'MANOSABA_A11Y_TEST_MAPFLOOR') {""")

rep("""$env:MANOSABA_A11Y_TEST_MAPCHOICE = "$MapPick\"""",
    """$env:MANOSABA_A11Y_TEST_MAPCHOICE = "$MapPick"
if ($MapFloor) { $env:MANOSABA_A11Y_TEST_MAPFLOOR = '1' }""")

# 收尾：除 pick 外也接受「切换楼层」作为通过条件
rep("""if (-not $pickOk -and (Text-Has 'MAPCHOICE 数字键 \\d+ ->'))""",
    """if (-not $pickOk -and $MapFloor -and (Text-Has 'MAPCHOICE 切换楼层')) { $pickOk = $true; Write-Host '[step5] 楼层切换已触发' }
if (-not $pickOk -and (Text-Has 'MAPCHOICE 数字键 \\d+ ->'))""")

# 摘要里附上楼层切换与第二次播报
rep("""---- MAPCHOICE 选择 ----""",
    """---- MAPCHOICE 楼层 ----""")

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('run93: -MapFloor added')
