# run7-a11y.ps1 —— 不靠鼠标的实机验收（TestDriver 版）
# 依赖 ManosabaA11y 的 MANOSABA_A11Y_TEST=1 测试模式：插件自己在引擎就绪后
#   Resources.FindObjectsOfTypeAll<Button>() 找「开始游戏」按钮并 onClick.Invoke()，
#   然后 IScriptPlayer.SetAutoPlayEnabled(true) 自动推进台词。本脚本只负责部署/启动/等/收日志/杀进程。
param(
    [int]$WaitSeconds = 180,
    [string]$Tag = 'run7',
    [switch]$KeepGame
)

$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc  = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir = Join-Path $RepoDir 'probe\logs'
$Summary = Join-Path $OutDir ($Tag + '-a11y-summary.txt')

$t0 = Get-Date
function Now { (Get-Date).ToString('HH:mm:ss') }
function Elapsed { [int]((Get-Date) - $t0).TotalSeconds }
function Say([string]$s) { Write-Output ("[{0} T={1,3}s] {2}" -f (Now), (Elapsed), $s) }
function LogCount([string]$Pattern) {
    try { return @(Select-String -Path $LogPath -Pattern $Pattern -ErrorAction SilentlyContinue).Count } catch { return 0 }
}

Say "STEP1 部署"
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Say "kill 残留 pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Start-Sleep -Seconds 2
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
Say ("deploy {0} B" -f (Get-Item (Join-Path $PluginDir 'ManosabaA11y.dll')).Length)

Say "STEP2 启动（TEST=1 + LOCALE=zh-Hans）"
$env:MANOSABA_A11Y_TEST = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null

$deadline = (Get-Date).AddSeconds($WaitSeconds)
$last = 0
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 6
    $hp = LogCount 'harmony patched'
    $hook = LogCount 'HOOK print'
    $say = LogCount 'Reader.say'
    $td = LogCount 'TESTDRIVER'
    $t = Elapsed
    if ($t - $last -ge 18) {
        $last = $t
        Say ("plugin=$hp HOOKprint=$hook say=$say testdriver=$td lines=$([Math]::Round((Get-Item $LogPath -ErrorAction SilentlyContinue).Length/1024))KB")
    }
    if ($say -ge 8) { Say "已抓到 $say 句，提前结束"; break }
}

Say "STEP3 汇总"
$lines = @()
$lines += "===== $Tag summary $(Now) ====="
$lines += ("HOOK print={0} Reader.say={1} dedupe={2} TESTDRIVER={3} Locale={4}" -f (LogCount 'HOOK print'), (LogCount 'Reader.say'), (LogCount 'Reader.dedupe'), (LogCount 'TESTDRIVER'), (LogCount 'Locale'))
$lines += "----- TESTDRIVER 状态行（去掉按钮刷屏）-----"
$lines += (Select-String -Path $LogPath -Pattern 'TESTDRIVER' | Where-Object { $_.Line -notmatch ' button ' } | Select-Object -First 120 | ForEach-Object { $_.Line })
$lines += "----- HOOK / Reader / Locale / patched 行（前 120）-----"
$lines += (Select-String -Path $LogPath -Pattern 'HOOK print|Reader\.|Locale|patched|切换文本语言' | Select-Object -First 120 | ForEach-Object { $_.Line })
$lines += "----- 错误 -----"
$lines += (Select-String -Path $LogPath -Pattern 'Exception|failed|未找到|error' | Select-Object -Last 25 | ForEach-Object { $_.Line })
$lines | Set-Content -Path $Summary -Encoding UTF8
Say "summary -> $Summary"
$lines | ForEach-Object { Write-Output $_ }

if (-not $KeepGame) {
    Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Say "kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
    Say "DONE 实例已释放 $((Now))"
} else {
    Say "KEEP pid=$((Get-Process manosaba).Id)"
}

