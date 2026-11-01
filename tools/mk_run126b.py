import io
ps = r'''# run126-lowperf.ps1 —— 低性能模式实机取证（0.0.0.13）
#
# 目的：验证「低性能模式」真的能开、能还原，并量一下开启前后的帧率。
# 不需要点击：插件内部按 MANOSABA_A11Y_TEST_LOWPERF=1 在 40s 自动开启、在 100s 自动关闭，
# 我们只读日志里的 LOWPERF / PERF 行（本机自动化键盘注入进不了游戏，组合键链路靠单元断言覆盖）。
param(
    [int]$WaitSeconds = 150,
    [string]$Tag = 'run126'
)
$ErrorActionPreference = 'Continue'
$GameDir   = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath   = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir   = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc    = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir    = Join-Path $RepoDir 'probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Get-Process -Name manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

# 环境变量：只开诊断 + 低性能测试钩子，其他全部清空
$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
$env:MANOSABA_A11Y_PERFDIAG = '1'
$env:MANOSABA_A11Y_TEST_LOWPERF = '1'
foreach ($n in @('MANOSABA_A11Y_TEST','MANOSABA_A11Y_JUMP','MANOSABA_A11Y_JUMP_LINE','MANOSABA_A11Y_TEST_NAV',
                 'MANOSABA_A11Y_TEST_REPEAT','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_EVDIAG',
                 'MANOSABA_A11Y_TEST_EVCMD','MANOSABA_A11Y_TEST_EVFORCE','MANOSABA_A11Y_TEST_EVPICK',
                 'MANOSABA_A11Y_TEST_EVYES','MANOSABA_A11Y_TEST_EVMODE','MANOSABA_A11Y_TEST_MAPCHOICE',
                 'MANOSABA_A11Y_TEST_MAPFLOOR','MANOSABA_A11Y_TEST_MAPPICK_KIND','MANOSABA_A11Y_MAPDIAG',
                 'MANOSABA_A11Y_TEST_ESC','MANOSABA_A11Y_TEST_KEYS','MANOSABA_A11Y_TEST_SCRIPTJUMP',
                 'MANOSABA_A11Y_TEST_CHOICE','MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_SELFTEST',
                 'MANOSABA_A11Y_PERFTEST')) {
    Set-Item -Path ("env:" + $n) -Value '' -ErrorAction SilentlyContinue
}

$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
"S=启动 pid=$($proc.Id)"

Start-Sleep -Seconds $WaitSeconds

function Log-Text {
    if (-not (Test-Path $LogPath)) { return '' }
    $fs = [IO.File]::Open($LogPath, [IO.FileMode]::Open, [IO.FileAccess]::Read,
                          ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8)
    $t = $sr.ReadToEnd(); $sr.Dispose(); $fs.Dispose(); return $t
}
function Log-Matches([string]$Pattern) {
    $t = Log-Text
    if (-not $t) { return @() }
    return @(($t -split "`r?`n") | Select-String -Pattern $Pattern | ForEach-Object { $_.Line })
}

$lines = @()
$lines += "Tag=$Tag 时刻=$((Get-Date).ToString('HH:mm:ss')) 等待=${WaitSeconds}s"
$lines += "---- 版本/启动 ----"
$lines += (Log-Matches 'loaded v[0-9]|harmony patched|PERF static|PERF device|PERF urp|PERF counts')
$lines += "---- LOWPERF ----"
$lines += (Log-Matches 'LOWPERF[^\r\n]*|TEST LOWPERF[^\r\n]*')
$lines += "---- PERF 帧率与耗时 ----"
$lines += (Log-Matches 'PERF [0-9]+s[^\r\n]*')
$lines += "---- 错误 ----"
$lines += (Log-Matches 'Exception|error CS|Failed to patch|HarmonyException')

$enabled = @($lines | Select-String -Pattern 'LOWPERF 已开启').Count
$disabled = @($lines | Select-String -Pattern 'LOWPERF 已关闭').Count
$restored = @($lines | Select-String -Pattern '已还原画质设置|还原 \d+ 个柔边阴影').Count
$lines += "---- 结论 ----"
$lines += "开启=$enabled 关闭=$disabled 还原证据=$restored"

$out = Join-Path $OutDir "$Tag-summary.txt"
$lines | Set-Content -Path $out -Encoding UTF8
"汇总: $out"

Get-Process -Name manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
'''
io.open(r'D:\DSHWorkSpace\manosaba_a11y\probe\run126-lowperf.ps1', 'w', encoding='utf-8-sig', newline='\r\n').write(ps)
print('written bytes=', len(ps))
