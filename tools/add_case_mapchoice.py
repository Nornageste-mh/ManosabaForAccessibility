import io

p = r'D:\DSHWorkSpace\manosaba_a11y\tests\Suite.ps1'
s = io.open(p, encoding='utf-8-sig').read()

anchor = """    }
)

# ====================================================================================
# 运行器"""
if anchor not in s:
    raise SystemExit('anchor not found')

case = """    },

    # ---------------------------------------------------------------- 地图层
    @{
        # 为什么用「读取存档 + JUMP」：`@choiceMapArea` 出现在 Act01_Chapter01_Adv25 这类
        # 中段脚本里，从新游戏一路推进太慢；读第 1 格存档后 JUMP 直接落到 Adv25。
        # 自动选择用进程内测试钩子（MANOSABA_A11Y_TEST_MAPCHOICE=7）：本机自动化没法按数字键
        # （键盘注入被系统挡掉），钩子走的是同一条 `MapChoice.Activate` 代码路径。
        Id = 'map.choice'; Title = '地图页选择地点（限制名单 + 数字键 + 分支跳转）'; Group = '界面/地图'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv25'; MANOSABA_A11Y_TEST_MAPCHOICE = '7' }
        Timeout = 340
        Body = {
            param($Game, $Result)
            $ready = Wait-Log -Pattern 'loaded v[0-9.]+ \\(IL2CPP' -TimeoutSec 60
            Add-Check $Result '插件加载' $ready.Ok $ready.Line
            Start-Sleep -Seconds 20

            # 标题 → 读取界面 → 第 1 格存档（和 book.assist 同一条路，点空要重试）
            $load = $null
            for ($i = 1; $i -le 4; $i++) {
                $null = Click-Rel $Game.Hwnd 0.0875 0.832
                Start-Sleep -Seconds 3
                $null = Click-Rel $Game.Hwnd 0.179 0.233
                $load = Wait-Log -Pattern 'Reader\\.say\\[' -TimeoutSec 25
                if ($load.Ok) { break }
                Write-Host "    （第 $i 次读取没进剧情，重试）"
                $null = Click-Rel $Game.Hwnd 0.5 0.5
                Start-Sleep -Seconds 2
            }
            Add-Check $Result '读取存档后开始朗读' $load.Ok $load.Line
            if (-not $load.Ok) { return }

            # JUMP 落到 Adv25 → 剧情自己打开魔女手册并要求在地图上选地点
            $ann = Wait-Log -Pattern 'MAPCHOICE 播报 \\d+ 个地点' -TimeoutSec 150
            Add-Check $Result '识别出地图选择态并播报' $ann.Ok $ann.Line
            if ($ann.Ok) {
                Add-Check $Result '播报按楼层分组' ($ann.Line -match '在2F：') $ann.Line
                $Result.Evidence += $ann.Line.Trim()
            }
            $pick = Wait-Log -Pattern 'MAPCHOICE 变量: chosenAreaId=\\[String:RecreationRoom\\]' -TimeoutSec 90
            Add-Check $Result '选中娱乐室（游戏变量已写入）' $pick.Ok $pick.Line
            if ($pick.Ok) { $Result.Evidence += $pick.Line.Trim() }
            $story = Wait-Log -Pattern 'Reader\\.say\\[[^\\]]+\\][^\\r\\n]*娱乐室' -TimeoutSec 90
            Add-Check $Result '剧情进入娱乐室分支' $story.Ok $story.Line
            if ($story.Ok) { $Result.Evidence += $story.Line.Trim() }
            $Result.Shots += (Shot $Game.Hwnd 'map-choice')
        }
    }
)

# ====================================================================================
# 运行器"""

s = s.replace(anchor, case, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('suite: map.choice added')
