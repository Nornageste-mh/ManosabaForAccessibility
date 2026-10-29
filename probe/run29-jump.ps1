# run29-jump.ps1 —— 用 JUMP 直接加载指定剧本，验证「对白/说话人」朗读链（不点任何按钮）
#
# 为什么需要它：正常流程要人工点几十句旁白才轮到对白，验收成本太高；
# MANOSABA_A11Y_JUMP=<脚本路径> 会跳过游戏流程直接 LoadAndPlay 一个剧本，
# 用来快速看 Reader 是否念出「说话人：台词」。
# ★ 它跳过游戏状态机，所以只能验证朗读链，不能当玩家路径的验收。
param(
    [Parameter(Mandatory = $true)][string]$Script,
    [int]$WaitSeconds = 90,
    [string]$Tag = 'jump'
)
$ErrorActionPreference = 'Continue'
$GameDir   = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath   = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir   = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc    = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir    = Join-Path $RepoDir 'probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
Start-Sleep -Seconds 2
Get-ChildItem (Join-Path $GameDir 'BepInEx\plugins') -Recurse -Filter 'ManosabaA11y.dll' -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -ne $PluginDir } | ForEach-Object { Remove-Item $_.FullName -Force }
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

$env:MANOSABA_A11Y_DIAG   = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
$env:MANOSABA_A11Y_JUMP   = $Script
Remove-Item Env:MANOSABA_A11Y_TEST -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_INJECT -ErrorAction SilentlyContinue

Write-Output ("=== {0} JUMP={1} ===" -f $Tag, $Script)
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null
Start-Sleep -Seconds $WaitSeconds
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }

$l = Get-Content $LogPath -Encoding UTF8 -ErrorAction SilentlyContinue
"say=$(($l | Select-String -Pattern 'Reader\.say\[').Count)  exception=$(($l | Select-String -Pattern 'MethodAccessException').Count)  advAuthorName=$(($l | Select-String -Pattern 'HOOK advAuthorName').Count)  advAuthor=$(($l | Select-String -Pattern 'HOOK advAuthor id=').Count)"
"--- authorName samples ---"
$l | Select-String -Pattern 'HOOK advAuthorName' | Select-Object -First 6 | ForEach-Object { "  " + ($_.Line -replace '^.*HOOK','HOOK') }
"--- say samples ---"
$l | Select-String -Pattern 'Reader\.say\[' | Select-Object -First 12 | ForEach-Object { "  " + ($_.Line -replace '^.*Reader\.say','Reader.say') }
$sum = Join-Path $OutDir ("{0}-jump-summary.txt" -f $Tag)
$l | Select-String -Pattern 'JUMP|Reader\.say\[|HOOK adv|MethodAccessException|patched ' | ForEach-Object { $_.Line } | Set-Content $sum -Encoding UTF8
"summary -> $sum"
