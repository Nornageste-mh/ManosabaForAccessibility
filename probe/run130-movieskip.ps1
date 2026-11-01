# run130-movieskip.ps1 —— 验证「跳过过场影片」（0.0.17）
# 只启动、不点击：开场影片如果有，插件应该在它开始播的 0.3 秒内 Stop 掉并播报。
param([int]$WaitSeconds = 120, [string]$Tag = 'run130')
$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir = Join-Path $RepoDir 'probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Get-Process -Name manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }
$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
foreach ($n in @('MANOSABA_A11Y_TEST','MANOSABA_A11Y_JUMP','MANOSABA_A11Y_JUMP_LINE','MANOSABA_A11Y_TEST_NAV',
                 'MANOSABA_A11Y_TEST_REPEAT','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_EVDIAG','MANOSABA_A11Y_PERFDIAG',
                 'MANOSABA_A11Y_TEST_EVCMD','MANOSABA_A11Y_TEST_EVFORCE','MANOSABA_A11Y_TEST_EVPICK',
                 'MANOSABA_A11Y_TEST_EVYES','MANOSABA_A11Y_TEST_EVMODE','MANOSABA_A11Y_TEST_MAPCHOICE',
                 'MANOSABA_A11Y_TEST_MAPFLOOR','MANOSABA_A11Y_TEST_MAPPICK_KIND','MANOSABA_A11Y_MAPDIAG',
                 'MANOSABA_A11Y_TEST_ESC','MANOSABA_A11Y_TEST_KEYS','MANOSABA_A11Y_TEST_SCRIPTJUMP',
                 'MANOSABA_A11Y_TEST_CHOICE','MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_SELFTEST',
                 'MANOSABA_A11Y_PERFTEST','MANOSABA_A11Y_TEST_LOWPERF','MANOSABA_A11Y_TEST_EXTREME',
                 'MANOSABA_A11Y_TEST_STOPANIM','MANOSABA_A11Y_SCAN_CMD')) {
    Set-Item -Path ("env:" + $n) -Value '' -ErrorAction SilentlyContinue
}
$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
"S=启动 pid=$($proc.Id)"
Start-Sleep -Seconds $WaitSeconds
function Log-Text {
    if (-not (Test-Path $LogPath)) { return '' }
    try {
        $fs = [IO.File]::Open($LogPath, [IO.FileMode]::Open, [IO.FileAccess]::Read,
                              ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8)
        $t = $sr.ReadToEnd(); $sr.Dispose(); $fs.Dispose(); return $t
    } catch { return '' }
}
$t = Log-Text
$lines = @()
$lines += "Tag=$Tag 时刻=$((Get-Date).ToString('HH:mm:ss')) 等待=${WaitSeconds}s"
$lines += "---- 运行/版本 ----"
$lines += @(($t -split "`r?`n") | Select-String -Pattern 'loaded v|跳过过场|Cinematic' | ForEach-Object { $_.Line })
$lines += "---- 过场相关（电影/影片/UI） ----"
$lines += @(($t -split "`r?`n") | Select-String -Pattern 'Movie|movie|Cinematic|开场|影片' | ForEach-Object { $_.Line } | Select-Object -First 20)
$lines += "---- 错误 ----"
$lines += @(($t -split "`r?`n") | Select-String -Pattern 'Exception|Failed to patch' | ForEach-Object { $_.Line } | Select-Object -First 8)
$skip = @(($t -split "`r?`n") | Select-String -Pattern '已跳过过场影片').Count
$lines += "---- 结论 ----"
$lines += "跳过次数=$skip"
$out = Join-Path $OutDir "$Tag-summary.txt"
$lines | Set-Content -Path $out -Encoding UTF8
"汇总: $out"
"S=跳过次数=$skip"
Get-Process -Name manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
