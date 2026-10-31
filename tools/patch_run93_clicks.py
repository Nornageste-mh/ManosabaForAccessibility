import io

p = r'D:\DSHWorkSpace\manosaba_a11y\probe\run93-mapreal.ps1'
s = io.open(p, encoding='utf-8-sig').read()

old = """    $null = Focus-Game $h
    Click-Rel $h 0.5 0.5"""
new = """    $null = Focus-Game $h
    # 地图选择状态下绝对不能再点屏幕：本机点中心会真的点中地图上的地点，
    # 游戏会先以那个地点完成选择（run115 实测：数字键 7 选娱乐室，结果走的是玄关大厅分支）。
    if (-not $choiceOk) { Click-Rel $h 0.5 0.5 }"""
if old not in s:
    raise SystemExit('click block not found')
s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('run93: stop clicking once map choice detected')
