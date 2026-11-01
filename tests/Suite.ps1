# Suite.ps1 —— 《魔法少女ノ魔女裁判》A11Y 模组自动化测试套件
#
# 为什么不是「跑一遍完整剧情」：剧本 34,836 个文本块 / 79 万字，人工与全流程回放都不现实。
# 所以我们只测「模块/界面/剧情阶段」是否符合预期：
#   * 单元层：纯逻辑（TextProc 清洗、行 id、说话人表、配音表、快捷键解析）——进程内自测，秒级；
#   * 集成层：每个用例独立启动游戏，走「真点击进正片 → 跳转到目标剧本 → 断言日志证据」。
#
# 运行：
#   pwsh -File tests\Suite.ps1                      # 跑全部用例（约 15–20 分钟，逐个启动游戏）
#   pwsh -File tests\Suite.ps1 -Only unit.selftest   # 只跑单元自测（约 40 秒）
#   pwsh -File tests\Suite.ps1 -Only story,menu      # 按 id 前缀过滤
#   pwsh -File tests\Suite.ps1 -Tag nightly          # 报告文件名前缀
#
# 输出：tests\reports\<Tag>-report.md（人读）+ <Tag>-result.json（机读）+ shots\*.png
# 退出码：失败用例数（0 = 全绿；CI 可直接据此判红绿）。
param(
    [string[]]$Only = @(),
    [string]$Tag = ('suite-' + (Get-Date -Format 'MMdd-HHmm')),
    [switch]$KeepGame,
    [int]$TimeoutScale = 100
)

$ErrorActionPreference = 'Continue'
# `pwsh -File tests\Suite.ps1 -Only story,keys` 会把 "story,keys" 作为**一个**字符串传进来，
# 所以这里要自己按逗号拆开（否则「没有匹配的用例」）。
$Only = @($Only | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
. (Join-Path $PSScriptRoot 'lib\Harness.ps1')

$Version = (Select-String -Path (Join-Path $RepoDir 'mod\src\ManosabaA11y\Plugin.cs') -Pattern 'public const string Version = "([^"]+)"' -Encoding UTF8 |
            Select-Object -First 1).Matches[0].Groups[1].Value

function Clear-A11yEnv {
    foreach ($k in @('MANOSABA_A11Y_DIAG','MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_SELFTEST','MANOSABA_A11Y_JUMP',
                     'MANOSABA_A11Y_JUMP_LINE','MANOSABA_A11Y_LOCALE','MANOSABA_A11Y_TEST','MANOSABA_A11Y_TEST_NAV','MANOSABA_A11Y_TEST_PLAY',
                     'MANOSABA_A11Y_TEST_KEYS','MANOSABA_A11Y_TEST_REPEAT','MANOSABA_A11Y_TEST_ESC','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_DEBUGUI','MANOSABA_A11Y_INJECT',
                     'MANOSABA_A11Y_TEST_CHOICE','MANOSABA_A11Y_TEST_MAPCHOICE','MANOSABA_A11Y_TEST_MAPFLOOR','MANOSABA_A11Y_TEST_MAPPICK_KIND',
                     'MANOSABA_A11Y_TEST_EVMODE','MANOSABA_A11Y_TEST_EVCMD','MANOSABA_A11Y_TEST_EVFORCE','MANOSABA_A11Y_TEST_EVPICK','MANOSABA_A11Y_TEST_EVYES',
                     'MANOSABA_A11Y_EVDIAG','MANOSABA_A11Y_MAPDIAG','MANOSABA_A11Y_NOPATCH')) {
        Remove-Item -Path "Env:$k" -ErrorAction SilentlyContinue
    }
}

# ====================================================================================
# 用例表
#   每个用例：Id / Title / Group / Env（启动环境变量）/ Timeout（秒）/ Body（拿到 $Game 与 $Result）
# ====================================================================================
$Cases = @(
    # ---------------------------------------------------------------- 单元层
    @{
        Id = 'unit.selftest'; Title = '纯逻辑单元自测（TextProc/说话人/配音表/快捷键）'; Group = '单元'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_SELFTEST = '1' }; Timeout = 120
        Body = {
            param($Game, $Result)
            $r = Wait-Log -Pattern 'SELFTEST SUMMARY total=(\d+) pass=(\d+) fail=(\d+)' -TimeoutSec 100
            Add-Check $Result '自测跑完并给出汇总' $r.Ok ($r.Line)
            if ($r.Ok) {
                $m = [regex]::Match($r.Line, 'total=(\d+) pass=(\d+) fail=(\d+)')
                $total = [int]$m.Groups[1].Value; $pass = [int]$m.Groups[2].Value; $fail = [int]$m.Groups[3].Value
                Add-Check $Result '断言数 >= 15' ($total -ge 15) "total=$total"
                Add-Check $Result '全部通过' ($fail -eq 0) "fail=$fail"
                Add-Check $Result '汇总自洽' ($pass -eq $total) "pass=$pass total=$total"
                $Result.Evidence += $r.Line.Trim()
                foreach ($line in (Get-LogMatches 'SELFTEST (PASS|FAIL) [^\r\n]+') | Select-Object -First 6) { $Result.Evidence += $line.Trim() }
                foreach ($line in (Get-LogMatches 'SELFTEST FAIL [^\r\n]+')) { $Result.Evidence += $line.Trim() }
            }
        }
    },

    # ---------------------------------------------------------------- 界面层
    @{
        Id = 'menu.title'; Title = '标题菜单导航（Alt+Shift+F1 进入/退出、5 项、组名正确）'; Group = '界面/标题'
        # 键盘注入在本机被系统挡掉（SendInput 返回 0），所以用插件内的合成导航钩子
        # MANOSABA_A11Y_TEST_NAV=1（Load+45s 起每 25s 触发一次，等价于按了一次 Tab）。
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_TEST_NAV = '1' }; Timeout = 300
        Body = {
            param($Game, $Result)
            $ready = Wait-Log -Pattern 'loaded v[0-9.]+ \(IL2CPP' -TimeoutSec 60
            Add-Check $Result '插件加载' $ready.Ok $ready.Line
            Start-Sleep -Seconds 40    # 等标题画面（实测引擎初始化 + 标题脚本约 30–40 秒，6 FPS）
            $nav = Wait-Log -Pattern '导航模式，标题画面' -TimeoutSec 90
            Add-Check $Result '进入标题导航' $nav.Ok $nav.Line
            if ($nav.Ok) {
                $groupLine = Wait-Log -Pattern '导航模式，标题画面，共 \d+ 项' -TimeoutSec 10
                Add-Check $Result '播报组名与项数' $groupLine.Ok $groupLine.Line
                Add-Check $Result '共 5 项' ($groupLine.Line -match '共 5 项') $groupLine.Line
                $label = Wait-Log -Pattern '(新游戏|退出游戏|系统设置)，按钮' -TimeoutSec 15
                Add-Check $Result '按钮有中文名' $label.Ok $label.Line
                Add-Check $Result '不念 Ver. 版本号' (-not ($label.Line -match 'Ver\.')) $label.Line
                $Result.Evidence += $groupLine.Line.Trim()
                $Result.Evidence += $label.Line.Trim()
            }
            $exit = Wait-Log -Pattern '已退出导航模式' -TimeoutSec 90
            Add-Check $Result '再按一次退出导航' $exit.Ok $exit.Line
            $Result.Shots += (Shot $Game.Hwnd 'menu-title')
        }
    },

    # ---------------------------------------------------------------- 剧情层
    @{
        Id = 'story.adv'; Title = '普通对话（逐句朗读 + 行 id + 文本清洗）'; Group = '剧情/ADV'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv01' }; Timeout = 200
        Body = {
            param($Game, $Result)
            $e = Enter-Story $Game
            Add-Check $Result '进入正片' $e.Ok "尝试 $($e.Attempts) 次"
            if ($e.Ok) {
                $adv = Advance-Until $Game -Pattern 'Reader\.say\[reveal\][^\r\n]*' -MinCount 5 -TimeoutSec 120
                $say = @($adv.Matches)
                Add-Check $Result '至少朗读 5 句' $adv.Ok "句数 $($say.Count)"
                $withId = @($say | Where-Object { $_ -match 'id=0101Adv01_' })
                Add-Check $Result '带行 id（配音闸门依赖）' ($withId.Count -ge 1) ("命中 " + $withId.Count)
                $noTag = @($say | Where-Object { $_ -match '=> .*<[a-zA-Z]' })
                Add-Check $Result '朗读文本无残留标签' ($noTag.Count -eq 0) ($noTag -join ' | ')
                $Result.Evidence += ($say | Select-Object -First 4 | ForEach-Object { $_.Trim() })
                $Result.Shots += (Shot $Game.Hwnd 'story-adv')
            }
        }
    },

    @{
        Id = 'story.speaker'; Title = '对白说话人（读成「角色名：台词」）'; Group = '剧情/ADV'
        # 新默认「有配音时也报说话人 = false」，本用例专测说话人链，所以临时打开这一项。
        Cfg = @{ '有配音时也报说话人' = 'true' }
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv02'; MANOSABA_A11Y_TEST_REPEAT = '1' }; Timeout = 280
        Body = {
            param($Game, $Result)
            $e = Enter-Story $Game
            Add-Check $Result '进入正片' $e.Ok "尝试 $($e.Attempts) 次"
            if ($e.Ok) {
                # 说话人 id 来自消息级钩子（UITextPrinter.AddMessage 的 PrintedMessage.author）。
                # 注意：开头是旁白（author 为空），所以必须边推进边等，不能只 Wait-Log。
                $nl = Advance-Until $Game -Pattern 'HOOK print author id=\[[A-Za-z]' -MinCount 1 -TimeoutSec 150
                $auth = if ($nl.Ok) { $nl.Matches[0] } else { $null }
                Add-Check $Result '说话人 id 来自消息级钩子' ($null -ne $auth) "$auth"

                # 名字要么拼进「角色名：台词」，要么在配音行只报名（cfg 打开时）
                $sp = Advance-Until $Game -Pattern '(Reader\.voiced\[[^\]]+\][^\r\n]*只报名字|Reader\.say\[[^\]]+\][^\r\n]*=> [^\r\n：]{1,10}：)' -MinCount 1 -TimeoutSec 150
                $found = if ($sp.Ok) { $sp.Matches[0] } else { $null }
                Add-Check $Result '名字进入朗读路径' ($null -ne $found) "$found"

                # 退格重读的缓冲里必须带「角色名：」——这是「记住的是合成句」的证据。
                # 自动化环境注入按键无效（KeyBD/InputSystem 两条都实测无效），
                # 改由插件的 TEST_REPEAT 钩子每 25s 触发同一段代码路径（LastLine → Repeat.Say）。
                $rep = Wait-Log -Pattern '重读: [^\r\n]{2,14}：' -TimeoutSec 90
                Add-Check $Result '重读内容是「角色名：台词」' $rep.Ok $rep.Line

                if ($found) { $Result.Evidence += $found.Trim() }
                if ($auth) { $Result.Evidence += $auth.Trim() }
                if ($rep.Ok) { $Result.Evidence += $rep.Line.Trim() }
                $Result.Shots += (Shot $Game.Hwnd 'story-speaker')
            }
        }
    },

    @{
        Id = 'story.voice_gate'; Title = '配音闸门（有配音的行不念正文）'; Group = '剧情/配音'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv02' }; Timeout = 240
        Body = {
            param($Game, $Result)
            $e = Enter-Story $Game
            Add-Check $Result '进入正片' $e.Ok "尝试 $($e.Attempts) 次"
            if ($e.Ok) {
                $vg = Advance-Until $Game -Pattern 'Reader\.voiced\[[^\]]+\][^\r\n]*' -MinCount 3 -TimeoutSec 150
                $voiced = @($vg.Matches)
                Add-Check $Result '至少 3 句命中配音表' $vg.Ok "命中 $($voiced.Count)"
                $withId = @($voiced | Where-Object { $_ -match 'id=[0-9]{4}' })
                Add-Check $Result '配音判定带行 id' ($withId.Count -ge 1) ("命中 " + $withId.Count)
                $Result.Evidence += ($voiced | Select-Object -First 4 | ForEach-Object { $_.Trim() })
                $Result.Shots += (Shot $Game.Hwnd 'story-voice')
            }
        }
    },

    @{
        Id = 'keys.backspace'; Title = '退格重读上一句（含配音行）'; Group = '按键/退格'
        # 用插件自带的测试钩子（MANOSABA_A11Y_TEST_REPEAT）：自动化环境的 keybd_event
        # 进不了本作（只启用新 Input System），物理按键由 KEYDIAG 的 reading=true 另行覆盖。
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv02'; MANOSABA_A11Y_TEST_REPEAT = '1' }; Timeout = 300
        Body = {
            param($Game, $Result)
            $e = Enter-Story $Game
            Add-Check $Result '进入正片' $e.Ok "尝试 $($e.Attempts) 次"
            if ($e.Ok) {
                $null = Advance-Until $Game -Pattern 'Reader\.say\[[^\]]+\][^\r\n]*' -MinCount 3 -TimeoutSec 90
                $r = Wait-Log -Pattern '重读: ' -TimeoutSec 120
                Add-Check $Result '退格触发重读' $r.Ok $r.Line
                if ($r.Ok) {
                    $rep = @(Get-LogMatches '重读: [^\r\n]+')
                    Add-Check $Result '重读内容是剧情文本' (@($rep | Where-Object { $_.Length -gt 8 }).Count -ge 1) ($rep -join ' | ')
                    $Result.Evidence += ($rep | Select-Object -First 3 | ForEach-Object { $_.Trim() })
                }
                $Result.Shots += (Shot $Game.Hwnd 'keys-backspace')
            }
        }
    },

    # ---------------------------------------------------------------- 选项层
    @{
        Id = 'story.choice'; Title = '选项组播报 + 数字键选择'; Group = '剧情/选项'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv05'; MANOSABA_A11Y_TEST_CHOICE = '1' }; Timeout = 300
        Body = {
            param($Game, $Result)
            $e = Enter-Story $Game
            Add-Check $Result '进入正片' $e.Ok "尝试 $($e.Attempts) 次"
            if ($e.Ok) {
                $ch = Advance-Until $Game -Pattern 'CHOICE 播报[^\r\n]*=> [^\r\n]*' -MinCount 1 -TimeoutSec 200
                $hit = if ($ch.Ok) { $ch.Matches[0] } else { $null }
                Add-Check $Result '播报选项组' ($null -ne $hit) "$hit"
                if ($hit) {
                    # 诊断行形如：CHOICE 播报 2 项: A|B => 共 2 个选项。选项 1：A。选项 2：B。按数字键选择。
                    Add-Check $Result '整组文案含「共 N 个选项」' ($hit -match '=> .*共 \d+ 个选项') $hit
                    Add-Check $Result '逐项编号 + 按键提示' (($hit -match '选项 1：') -and ($hit -match '按数字键选择')) $hit
                    $Result.Evidence += $hit.Trim()
                    # 本机键盘注入被系统挡掉（SendInput 返回 0），改由插件测试钩子走同一条选择路径
                    $sel = Wait-Log -Pattern 'CHOICE (数字键 (按下 |测试钩子：自动选择第 )?1)' -TimeoutSec 60
                    Add-Check $Result '数字键 1 的选择路径走通' $sel.Ok $sel.Line
                    if ($sel.Ok) { $Result.Evidence += $sel.Line.Trim() }
                }
                $Result.Shots += (Shot $Game.Hwnd 'story-choice')
            }
        }
    },

    # ---------------------------------------------------------------- 审判层
    @{
        Id = 'trial.debate'; Title = '审判辩论朗读（debate 通道）'; Group = '剧情/审判'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Trial01' }; Timeout = 300
        Body = {
            param($Game, $Result)
            $e = Enter-Story $Game
            Add-Check $Result '进入正片' $e.Ok "尝试 $($e.Attempts) 次"
            if ($e.Ok) {
                $db = Advance-Until $Game -Pattern 'Reader\.say\[debate:add\][^\r\n]*' -MinCount 3 -TimeoutSec 240
                $hit = if ($db.Ok) { @($db.Matches) } else { $null }
                Add-Check $Result '辩论台词走 debate 通道 ≥3 句' ($null -ne $hit) ("句数 " + ($hit | Measure-Object).Count)
                if ($hit) {
                    $clean = @($hit | Where-Object { $_ -match '=> ' })
                    Add-Check $Result '辩论文本已清洗（有 => 输出）' ($clean.Count -ge 1) ("清洗 " + $clean.Count)
                    $Result.Evidence += ($hit | Select-Object -First 3 | ForEach-Object { $_.Trim() })
                }
                $Result.Shots += (Shot $Game.Hwnd 'trial-debate')
            }
        }
    },

    # ---------------------------------------------------------------- 加载层
    @{
        Id = 'screen.loading'; Title = '加载界面播报（LoadingUiExtended 钩子）'; Group = '界面/加载'
        Env = @{ MANOSABA_A11Y_DIAG = '1' }; Timeout = 180
        Body = {
            param($Game, $Result)
            $ready = Wait-Log -Pattern 'loaded v[0-9.]+ \(IL2CPP' -TimeoutSec 60
            Add-Check $Result '插件加载' $ready.Ok $ready.Line
            Start-Sleep -Seconds 30
            $null = Click-Rel $Game.Hwnd 0.220 0.832   # 标题「新游戏」（相对坐标）
            $r = Wait-Log -Pattern '加载播报（' -TimeoutSec 90
            Add-Check $Result '新游戏触发加载播报' $r.Ok $r.Line
            $hook = @(Get-LogMatches 'HOOK loadingUi visible=True')
            Add-Check $Result '可见性钩子被调用' ($hook.Count -ge 1) ("命中 " + $hook.Count)
            if ($r.Ok) { $Result.Evidence += $r.Line.Trim() }
            if ($hook.Count -gt 0) { $Result.Evidence += $hook[0].Trim() }
            $Result.Shots += (Shot $Game.Hwnd 'screen-loading')
        }
    },

    # ---------------------------------------------------------------- 整页界面层
    @{
        # 为什么用「读取存档」而不是「新游戏」：存档槽 1 直接落在第 1 章 Adv18，
        # 手册里已有线索，打开就能取证；新游戏要推进到有内容的位置太慢（用户提示）。
        Id = 'book.assist'; Title = '魔女手册整页界面：自动播报 + Esc 关闭'; Group = '界面/整页'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_TEST_ESC = '1' }; Timeout = 300
        Body = {
            param($Game, $Result)
            $ready = Wait-Log -Pattern 'loaded v[0-9.]+ \(IL2CPP' -TimeoutSec 60
            Add-Check $Result '插件加载' $ready.Ok $ready.Line
            Start-Sleep -Seconds 20

            # 标题 → 读取界面 → 第 1 格存档（相对坐标，客户区尺寸会变）
            # 实测点击偶发「点空」（全屏游戏被别的窗口盖住时第一下会丢），所以重试。
            $load = $null
            for ($i = 1; $i -le 4; $i++) {
                $null = Click-Rel $Game.Hwnd 0.0875 0.832
                Start-Sleep -Seconds 3
                $null = Click-Rel $Game.Hwnd 0.179 0.233
                $load = Wait-Log -Pattern 'Reader\.say\[' -TimeoutSec 25
                if ($load.Ok) { break }
                Write-Host "    （第 $i 次读取没进剧情，重试）"
                $null = Click-Rel $Game.Hwnd 0.5 0.5   # 关掉可能残留的界面
                Start-Sleep -Seconds 2
            }
            Add-Check $Result '读取存档后开始朗读' $load.Ok $load.Line
            if (-not $load.Ok) { return }

            # 点左上角书本（ADV 界面固定位置）→ 打开魔女手册；同一次也可能点空，重试
            $ann = $null
            for ($i = 1; $i -le 3; $i++) {
                $null = Click-Rel $Game.Hwnd 0.055 0.054
                $ann = Wait-Log -Pattern 'ScreenAssist: 播报 魔女手册[^\r\n]*' -TimeoutSec 20
                if ($ann.Ok) { break }
                Write-Host "    （第 $i 次点书本没打开，重试）"
            }
            Add-Check $Result '打开手册自动播报' $ann.Ok $ann.Line
            if ($ann.Ok) {
                Add-Check $Result '播报含页签名' ($ann.Line -match '页签。内容：') $ann.Line
                $Result.Evidence += $ann.Line.Trim()
            }
            $Result.Shots += (Shot $Game.Hwnd 'book-assist')

            # TEST_ESC 钩子（本机键盘注入被系统挡掉，只能用进程内钩子验证关闭路径）
            $esc = Wait-Log -Pattern 'ScreenAssist: Esc 关闭 [^\r\n]*' -TimeoutSec 90
            Add-Check $Result 'Esc 关闭整页界面' $esc.Ok $esc.Line
            if ($esc.Ok) { $Result.Evidence += $esc.Line.Trim() }
        }
    },

    # ---------------------------------------------------------------- 本地化层
    @{
        Id = 'locale.ja'; Title = '文本语言切换（日文台词入库）'; Group = '模块/本地化'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_LOCALE = 'ja'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv01' }; Timeout = 220
        Body = {
            param($Game, $Result)
            $e = Enter-Story $Game
            Add-Check $Result '进入正片' $e.Ok "尝试 $($e.Attempts) 次"
            if ($e.Ok) {
                $jp = Advance-Until $Game -Pattern 'Reader\.say\[[^\]]+\][^\r\n]*raw=[^\r\n]*[\u3041-\u30ff]' -MinCount 1 -TimeoutSec 150
                $ja = if ($jp.Ok) { $jp.Matches[0] } else { $null }
                Add-Check $Result '台词是日文（假名）' ($null -ne $ja) "$ja"
                if ($ja) { $Result.Evidence += $ja.Trim() }
                $Result.Shots += (Shot $Game.Hwnd 'locale-ja')
            }
        }
    },

    # ---------------------------------------------------------------- 审判层
    @{
        # 为什么默认跳过：要跨「读档 → 进正片」才轮到 JUMP_LINE 跳行（Plugin.JumpTick 只在
        # sp.Playing && path.StartsWith("Act") 之后跳），而这段在本机 6 FPS 下鼠标点击不稳；
        # 证物出示链路本身已由 probe\run124-evdialog.ps1 实机验收（ev14：idx=191 -> 197）。
        # 显式跑：pwsh -File Suite.ps1 -Only evidence.present -Tag ev-suite
        Id = 'evidence.present'; Title = '审判出示证据（跳行直达 + 证物名枚举 + 确认框）'; Group = '审判/出示证据'
        Env = @{ MANOSABA_A11Y_DIAG = '1'
                 MANOSABA_A11Y_JUMP_LINE = 'Act01_Chapter01/Act01_Chapter01_Trial04|327'
                 MANOSABA_A11Y_TEST_EVPICK = '1'; MANOSABA_A11Y_TEST_EVYES = '1' }
        Timeout = 300
        Skip = $true
        Body = {
            param($Game, $Result)
            $ready = Wait-Log -Pattern 'loaded v[0-9.]+ \(IL2CPP' -TimeoutSec 60
            Add-Check $Result '插件加载' $ready.Ok $ready.Line
            Start-Sleep -Seconds 20

            # 标题 → 读取界面 → 第 1 格存档（进正片后 JumpTick 才能跳行）
            $null = Focus-Game $Game.Hwnd
            $entered = $false
            for ($i = 1; $i -le 10 -and -not $entered; $i++) {
                Click-Rel $Game.Hwnd 0.0875 0.832
                Start-Sleep -Milliseconds 2500
                Click-Rel $Game.Hwnd 0.179 0.233
                $w = Wait-Log -Pattern 'Reader\.say\[|JUMP LoadAndPlayAtLine' -TimeoutSec 22
                $entered = $w.Ok
            }
            Add-Check $Result '读档并跳行' $entered '读第 1 格存档后由 JUMP_LINE 直达 @choiceEvidence 行'

            # 证物名枚举（证物卡是纯图标，靠「点一张 → 读右侧标题」得到名字）
            $enum = Wait-Log -Pattern 'EVIDENCE 名字枚举完成: 1=' -TimeoutSec 120
            Add-Check $Result '枚举出证物名字' $enum.Ok $enum.Line

            $ann = Wait-Log -Pattern 'EVIDENCE 播报: 请出示证据。可选的证物共 \d+ 件' -TimeoutSec 60
            Add-Check $Result '播报可选证物' $ann.Ok $ann.Line

            # 数字键选中 → 回车出示 → 游戏确认框 → 选「是」→ 剧本越过 @choiceEvidence
            $dlg = Wait-Log -Pattern 'EVIDENCE 对话框: ' -TimeoutSec 90
            Add-Check $Result '念出确认对话框' $dlg.Ok $dlg.Line

            $passed = Wait-Log -Pattern 'EVIDENCE dump .* idx=(19[8-9]|2\d\d) ' -TimeoutSec 60
            Add-Check $Result '剧本越过 choiceEvidence 行' $passed.Ok $passed.Line
            $Result.Shots += (Shot $Game.Hwnd 'evidence-present')
        }
    },

    # ---------------------------------------------------------------- 地图层
    @{
        # 为什么用「读取存档 + JUMP」：`@choiceMapArea` 出现在 Act01_Chapter01_Adv25 这类
        # 中段脚本里，从新游戏一路推进太慢；读第 1 格存档后 JUMP 直接落到 Adv25。
        # 自动选择用进程内测试钩子（MANOSABA_A11Y_TEST_MAPCHOICE=7）：本机自动化没法按数字键
        # （键盘注入被系统挡掉），钩子走的是同一条 `MapChoice.Activate` 代码路径。
        Id = 'map.choice'; Title = '地图页选择地点（限制名单 + 数字键 + 分支跳转）'; Group = '界面/地图'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv25';
                 MANOSABA_A11Y_TEST_MAPCHOICE = '7'; MANOSABA_A11Y_TEST_ESC = '1' }
        Timeout = 340
        # 默认批次不跑：从存档槽 1 一路点到 @choiceMapArea 要跨过「3 选 1 选项 → 剧情自己连开
        # 3 次魔女手册线索页」这段，鼠标点击在 6 FPS 下不稳定（run map3–map7 实测都卡在线索页）。
        # 地图选择本身已由 probe\run93-mapreal.ps1（run122）实机验收；等以后能用剧本跳转直达
        # 地图选择态时再把它加回默认批次。
        Skip = $true
        Body = {
            param($Game, $Result)
            $ready = Wait-Log -Pattern 'loaded v[0-9.]+ \(IL2CPP' -TimeoutSec 60
            Add-Check $Result '插件加载' $ready.Ok $ready.Line
            Start-Sleep -Seconds 20

            # 标题 → 读取界面 → 第 1 格存档（和 book.assist 同一条路，点空要重试）
            $load = $null
            for ($i = 1; $i -le 4; $i++) {
                $null = Click-Rel $Game.Hwnd 0.0875 0.832
                Start-Sleep -Seconds 3
                $null = Click-Rel $Game.Hwnd 0.179 0.233
                $load = Wait-Log -Pattern 'Reader\.say\[' -TimeoutSec 25
                if ($load.Ok) { break }
                Write-Host "    （第 $i 次读取没进剧情，重试）"
                $null = Click-Rel $Game.Hwnd 0.5 0.5
                Start-Sleep -Seconds 2
            }
            Add-Check $Result '读取存档后开始朗读' $load.Ok $load.Line
            if (-not $load.Ok) { return }

            # JUMP 落到 Adv25 → 剧情自己打开魔女手册并要求在地图上选地点。
            # 注意：不能只干等——Adv19 还有几行要玩家点过才到 Adv25；
            # 也不能点屏幕中央/底部（中央会抢先点中地图地点、底部是楼层卡会挂断 await），
            # 所以固定点左上角「图鉴」标题那块空白来推进。
            $ann = @{ Ok = $false; Line = '' }
            $answered = $false
            for ($i = 1; $i -le 26; $i++) {
                $ann = Wait-Log -Pattern 'MAPCHOICE 播报 \d+ 个地点' -TimeoutSec 12
                if ($ann.Ok) { break }
                # Adv18 有一个 3 选 1 的选项挡在路上（截图 map-choice.png 实证：三张纸卷
                # 「回答：逃往招待所/逃往监牢/总之先逃吧」）。左上角点击推不动选项，
                # 必须点选项按钮本身；第 1 个选项按钮中心 ≈ 相对 (0.5, 0.365)。
                if (-not $answered -and @(Get-LogMatches '共 3 个选项').Count -gt 0) {
                    $null = Click-Rel $Game.Hwnd 0.5 0.365
                    $answered = $true
                    Start-Sleep -Seconds 2
                }
                $null = Click-Rel $Game.Hwnd 0.5 0.06   # 顶部中间：不是按钮（(0.06,0.06) 是「图鉴」按钮，会把手册反复点开成死循环）
            }
            Add-Check $Result '识别出地图选择态并播报' $ann.Ok $ann.Line
            if ($ann.Ok) {
                Add-Check $Result '播报按楼层分组' ($ann.Line -match '在2F：') $ann.Line
                $Result.Evidence += $ann.Line.Trim()
            }
            $pick = Wait-Log -Pattern 'MAPCHOICE 变量: chosenAreaId=\[String:RecreationRoom\]' -TimeoutSec 90
            Add-Check $Result '选中娱乐室（游戏变量已写入）' $pick.Ok $pick.Line
            if ($pick.Ok) { $Result.Evidence += $pick.Line.Trim() }
            $story = Wait-Log -Pattern 'Reader\.say\[[^\]]+\][^\r\n]*娱乐室' -TimeoutSec 90
            Add-Check $Result '剧情进入娱乐室分支' $story.Ok $story.Line
            if ($story.Ok) { $Result.Evidence += $story.Line.Trim() }
            $Result.Shots += (Shot $Game.Hwnd 'map-choice')
        }
    }
)

# ====================================================================================
# 运行器
# ====================================================================================
$selected = $Cases
# Skip = $true 的用例不进默认批次（显式 -Only 指定时仍然会跑，方便排查）。
# ★ 旧写法把这行放在 `if ($Only.Count -gt 0)` 里面，而该条件又要求 $Only 为空
#   —— 永远不成立，于是 Skip 完全失效：默认批次把 evidence.present / map.choice
#   这种依赖真人点击的用例也拉起来跑了。
if ($Only.Count -eq 0) {
    $selected = @($Cases | Where-Object { -not ($_.ContainsKey('Skip') -and $_.Skip) })
} else {
    $selected = @($Cases | Where-Object { $c = $_; @($Only | Where-Object { $c.Id -like "$_*" -or $c.Group -like "*$_*" }).Count -gt 0 })
    if ($selected.Count -eq 0) { Write-Host "没有匹配的用例：$($Only -join ', ')"; exit 2 }
}

Write-Host "=== 自动化测试套件 版本 $Version 用例 $($selected.Count) 个 标签 $Tag ===" -ForegroundColor Cyan
$results = New-Object System.Collections.Generic.List[object]

foreach ($case in $selected) {
    Clear-A11yEnv
    $r = New-CaseResult $case.Id $case.Title $case.Group
    $r.Pass = $true
    $sw = [Diagnostics.Stopwatch]::StartNew()
    # 用例厂时看门狗：旧版本的 $case.Timeout 从来没被读过，
    # 一个卡住的用例会拖到自己的内部超时（实测 menu.title 305s > 声明 300s）。
    $script:CaseDeadline = if ($case.ContainsKey('Timeout') -and $case.Timeout -gt 0) {
        (Get-Date).AddSeconds([double]$case.Timeout * $TimeoutScale)
    } else { (Get-Date).AddHours(1) }
    $game = $null
    $cfgSaved = $null
    Write-Host ("--- {0} [{1}] …" -f $case.Id, $case.Group) -ForegroundColor Yellow
    try {
        if ($case.ContainsKey('Cfg') -and $case.Cfg) { $cfgSaved = Apply-CfgOverrides $case.Cfg }
        $game = Start-Game -Env $case.Env
        if ($game.Hwnd -eq [IntPtr]::Zero) { throw '找不到游戏主窗口（可能启动失败或还在初始化）' }
        & $case.Body $game $r
        if ($r.Checks.Count -eq 0) { $r.Pass = $false; $r.Error = '用例没有产出任何断言' }
    } catch {
        $r.Pass = $false
        $r.Error = $_.Exception.Message
    } finally {
        $r.Duration = [int]$sw.Elapsed.TotalSeconds
        if (-not $KeepGame) { Stop-Game $game }
        if ($cfgSaved) { Restore-CfgOverrides $cfgSaved }
        Clear-A11yEnv
    }
    $r.Pass = ($r.Pass -and (@($r.Checks | Where-Object { -not $_.Ok }).Count -eq 0) -and -not $r.Error)
    $results.Add($r) | Out-Null
    $badge = if ($r.Pass) { 'PASS' } else { 'FAIL' }
    $color = if ($r.Pass) { 'Green' } else { 'Red' }
    Write-Host ("    {0}  {1} s  ({2} 条断言)" -f $badge, $r.Duration, $r.Checks.Count) -ForegroundColor $color
}

$rep = Write-Report -Results $results -Tag $Tag -Version $Version
Write-Host ''
Write-Host ("=== 通过 {0} / 共 {1}，失败 {2} ===" -f $rep.Passed, $rep.Total, $rep.Failed) -ForegroundColor Cyan
Write-Host ("报告：{0}" -f $rep.Md)
Write-Host ("JSON：{0}" -f $rep.Json)
exit $rep.Failed
