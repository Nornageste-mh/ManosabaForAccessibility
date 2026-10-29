# run31-probe.ps1 —— 界面取证：标题菜单控件对象名 / 旧输入 API 是否可用
#
# 只做两件事：
#   1) 开 MANOSABA_A11Y_PROBE_UI=1，插件在启动 12 秒后把当前界面所有 Selectable
#      的 对象名 / 父级链 / 显示文字 / 可见性 写进日志（供别名表取证）
#   2) 打一行 legacyInput=OK/FAIL，判断平台层热键（Input.GetKeyDown）在本作是否可用
#
# 采集完自动杀进程；结果写到 probe\logs\run31-ui-probe.txt
param(
    [int]$WaitSeconds = 70,
    [string]$Tag = 'run31'
)
$ErrorActionPreference = 'Continue'
$GameDir   = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath   = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir   = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc    = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir    = Join-Path $RepoDir 'probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$existing = Get-Process manosaba -ErrorAction SilentlyContinue
if ($existing) { Write-Host "已有 manosaba 实例，先退出：$($existing.Id -join ',')"; $existing | Stop-Process -Force; Start-Sleep -Seconds 3 }

# 清掉任何会干扰的探针插件（BepInEx 会递归扫子目录，必须整目录移出游戏目录）
foreach ($junk in @('ManosabaProbe.dll','NaninovelReProbe.dll')) {
    $p = Join-Path $GameDir "BepInEx\plugins\$junk"
    if (Test-Path $p) { Move-Item $p (Join-Path $RepoDir "probe\plugin\$junk.$(Get-Date -Format 'HHmmss').bak") -Force }
}
$stash = Join-Path $GameDir 'BepInEx\plugins\_stash_a11y'
if (Test-Path $stash) { Move-Item $stash (Join-Path $RepoDir "probe\_stash_out\_stash-$(Get-Date -Format 'HHmmss')") -Force }

New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (-not (Test-Path (Join-Path $PluginDir 'nvdaControllerClient.dll'))) {
    Copy-Item (Join-Path $RepoDir 'mod\nvda_dl\x64\nvdaControllerClient.dll') (Join-Path $PluginDir 'nvdaControllerClient.dll') -Force
}

if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

$env:MANOSABA_A11Y_DIAG      = '1'
$env:MANOSABA_A11Y_PROBE_UI  = '1'
$env:MANOSABA_A11Y_LOCALE    = 'zh-Hans'
Remove-Item Env:MANOSABA_A11Y_TEST -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_TEST_PLAY -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_JUMP -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_INJECT -ErrorAction SilentlyContinue

Write-Host "启动游戏（$Tag）…"
$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Start-Sleep -Seconds $WaitSeconds

$text = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force

$lines = @()
$lines += "# $Tag UI 取证（等待 ${WaitSeconds}s）"
$lines += "# 采样时间 $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$lines += ''
foreach ($l in $text) {
    if ($l -match 'UIPROBE|legacyInput|\[a11y\] loaded|patchedMethods|NOPATCH|语音后端') { $lines += $l }
}
$lines | Set-Content (Join-Path $OutDir "$Tag-ui-probe.txt") -Encoding UTF8
$lines | Select-Object -First 200
