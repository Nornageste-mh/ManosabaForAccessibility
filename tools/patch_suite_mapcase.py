import io

p = r'D:\DSHWorkSpace\manosaba_a11y\tests\Suite.ps1'
s = io.open(p, encoding='utf-8-sig').read()

old = """            # JUMP 落到 Adv25 → 剧情自己打开魔女手册并要求在地图上选地点
            $ann = Wait-Log -Pattern 'MAPCHOICE 播报 \\d+ 个地点' -TimeoutSec 150"""
new = """            # JUMP 落到 Adv25 → 剧情自己打开魔女手册并要求在地图上选地点。
            # 注意：不能只干等——Adv19 还有几行要玩家点过才到 Adv25；
            # 也不能点屏幕中央/底部（中央会抢先点中地图地点、底部是楼层卡会挂断 await），
            # 所以固定点左上角「图鉴」标题那块空白来推进。
            $ann = @{ Ok = $false; Line = '' }
            for ($i = 1; $i -le 20; $i++) {
                $ann = Wait-Log -Pattern 'MAPCHOICE 播报 \\d+ 个地点' -TimeoutSec 12
                if ($ann.Ok) { break }
                $null = Click-Rel $Game.Hwnd 0.06 0.06
            }"""
if old not in s:
    raise SystemExit('announce wait block not found')
s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('map.choice: advance with corner clicks')
