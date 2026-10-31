import io

p = r'D:\DSHWorkSpace\manosaba_a11y\probe\run93-mapreal.ps1'
s = io.open(p, encoding='utf-8-sig').read()

a = "$env:MANOSABA_A11Y_TEST_MAPCHOICE = '1'"
if 'MANOSABA_A11Y_KEYDIAG = ' not in s:
    s = s.replace(a, a + "\n$env:MANOSABA_A11Y_KEYDIAG = '1'")

old = """while ((Get-Date) -lt $deadline) {
    Start-Sleep 3"""
new = """while ((Get-Date) -lt $deadline) {
    Start-Sleep 3
    # 玩家自己会点地图/推进剧情：这里也点屏幕中心，既推进对话也可能点到地图地点
    $null = Focus-Game $h
    Click-Rel $h 0.5 0.5"""
if 'Click-Rel $h 0.5 0.5' not in s:
    s = s.replace(old, new)

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('patched')
