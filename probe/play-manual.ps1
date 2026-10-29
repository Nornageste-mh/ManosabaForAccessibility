# play-manual.ps1 —— 人工实机验证用启动器
#
# 用途：让**人**用正常方式玩（鼠标点击 / 回车推进），插件把每一句朗读决策写进
# `D:\Steam\steamapps\common\manosaba_game\BepInEx\LogOutput.log`（`[a11y] Reader.say ...`），
# 用来回答「真实玩家的操作能不能让读屏逐句出声」——这是自动化注入长期卡住时唯一可靠的验证通道。
#
# 为什么需要环境变量：插件默认不开诊断日志（普通玩家的日志应保持干净），
#   MANOSABA_A11Y_DIAG=1 只打开「诊断日志」，不启用任何自动点击/自动播放。
param(
    [switch]$KillOnly
)
$ErrorActionPreference = 'Continue'
$GameDir   = 'D:\Steam\steamapps\common\manosaba_game'
$RepoDir   = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc    = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'

Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Output "kill 旧实例 pid=$($_.Id)"; Stop-Process -Id $_.Id -Force
}
if ($KillOnly) { Write-Output 'DONE (kill only)'; exit 0 }
Start-Sleep -Seconds 2

# 清掉 run19 对照组可能留下的副本（BepInEx 会递归加载 plugins 下的 dll，两份会导致整个插件被跳过）
$pluginsRoot = Split-Path $PluginDir -Parent
foreach ($p in @((Join-Path $pluginsRoot '_stash_a11y'), (Join-Path $pluginsRoot 'ManosabaProbe.dll'), (Join-Path $pluginsRoot 'NaninovelReProbe.dll'))) {
    if (Test-Path $p) { Remove-Item $p -Recurse -Force; Write-Output "删除残留: $p" }
}

New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
$nvda = Join-Path $RepoDir 'mod\nvda_dl\x64\nvdaControllerClient.dll'
if (Test-Path $nvda) { Copy-Item $nvda (Join-Path $PluginDir 'nvdaControllerClient.dll') -Force }
$d = Get-Item (Join-Path $PluginDir 'ManosabaA11y.dll')
Write-Output ("deploy {0} B {1}" -f $d.Length, $d.LastWriteTime.ToString('HH:mm:ss'))

$log = Join-Path $GameDir 'BepInEx\LogOutput.log'
if (Test-Path $log) { Remove-Item $log -Force }

$env:MANOSABA_A11Y_DIAG   = '1'     # 只开诊断日志，不开任何测试驱动
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
Remove-Item Env:MANOSABA_A11Y_TEST   -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_INJECT -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_TEST_PLAY -ErrorAction SilentlyContinue

Write-Output '启动 manosaba.exe ...'
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null
Start-Sleep -Seconds 3
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "pid=$($_.Id) hwnd=$($_.MainWindowHandle)" }
Write-Output '现在请人工操作：点 New Game → 用你平时的方式推进约 10 句台词。'
Write-Output "日志: $log"
