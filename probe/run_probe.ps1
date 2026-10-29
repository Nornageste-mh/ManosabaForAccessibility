<#
  run_probe.ps1 — 魔法少女ノ魔女裁判 (Steam AppID 3101040) 无人值守启动探针
  =====================================================================
  作用：确保单实例 → 启动 manosaba → 实时 tail BepInEx 日志 → 到点杀进程 →
        收集 LogOutput.log / ErrorLog.log / interop 清单到日志目录。
  用法：
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_probe.ps1                 # 默认 300 秒，直接跑 exe
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_probe.ps1 -TimeoutSec 600
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_probe.ps1 -Launch steam  # 用 steam://rungameid/
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_probe.ps1 -Tag before-fix
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_probe.ps1 -NoKill        # 调试用：到点不杀
  退出码：0 = 进程正常出现过；1 = 起不来；2 = 已有 manosaba 在跑（未启动）。
  设计约束：全团队共用一台机器，游戏实例唯一。启动前必查进程，结束必须 kill 整个进程树。
#>
[CmdletBinding()]
param(
  [int]$TimeoutSec = 300,
  [string]$GameDir = 'D:\Steam\steamapps\common\manosaba_game',
  [string]$LogOutDir = 'D:\DSHWorkSpace\manosaba_a11y\probe\logs',
  [ValidateSet('exe','steam')][string]$Launch = 'exe',
  [string]$Tag = 'run',
  [int]$PollSec = 5,
  [switch]$NoKill
)

$ErrorActionPreference = 'Continue'
$exe      = Join-Path $GameDir 'manosaba.exe'
$procName = 'manosaba'
$be       = Join-Path $GameDir 'BepInEx'
$logPath  = Join-Path $be 'LogOutput.log'
$errPath  = Join-Path $be 'ErrorLog.log'

function Say($m) { Write-Output ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $m) }

if (-not (Test-Path $exe)) { Say "FATAL: exe 不存在: $exe"; exit 1 }
New-Item -ItemType Directory -Force -Path $LogOutDir | Out-Null

# --- 1. 单实例守卫 ---
$running = Get-Process -Name $procName -ErrorAction SilentlyContinue
if ($running) {
  Say ("ABORT: 已有 manosaba 进程在跑: " + (($running | ForEach-Object { $_.Id }) -join ','))
  Say '先执行: Stop-Process -Name manosaba -Force  （或等前一个 run 收尾）'
  exit 2
}

# --- 2. 清掉旧日志，确保本次日志可归因 ---
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
Remove-Item $logPath, $errPath -ErrorAction SilentlyContinue
$interopDir = Join-Path $be 'interop'
$interopBefore = if (Test-Path $interopDir) { (Get-ChildItem $interopDir -Filter *.dll).Count } else { -1 }
Say ("启动前 interop dll 数 = {0}" -f $interopBefore)

# --- 3. 启动 ---
if ($Launch -eq 'steam') {
  Say 'Launch=steam: steam://rungameid/3101040'
  Start-Process 'steam://rungameid/3101040'
} else {
  Say ("Launch=exe: {0}" -f $exe)
  Start-Process -FilePath $exe -WorkingDirectory $GameDir
}

# --- 4. 等进程 + 实时 tail 日志 ---
$appeared = $false
$lastLen  = 0
$deadline = (Get-Date).AddSeconds($TimeoutSec)
$proc = $null
while ((Get-Date) -lt $deadline) {
  Start-Sleep -Seconds $PollSec
  if (-not $proc) {
    $proc = Get-Process -Name $procName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($proc) { $appeared = $true; Say ("进程已起 PID={0} 窗口={1}" -f $proc.Id, $proc.MainWindowHandle) }
  }
  if ($proc -and $proc.HasExited) { Say ("进程已退出 PID={0} exitcode={1}" -f $proc.Id, $proc.ExitCode); $proc = $null; break }
  if (Test-Path $logPath) {
    $lines = @(Get-Content $logPath -ErrorAction SilentlyContinue)
    if ($lines.Count -gt $lastLen) {
      $lines[$lastLen..($lines.Count-1)] | ForEach-Object { Write-Output ("  | " + $_) }
      $lastLen = $lines.Count
    }
  }
}

# --- 5. 收集 ---
$interopAfter = if (Test-Path $interopDir) { (Get-ChildItem $interopDir -Filter *.dll).Count } else { -1 }
Say ("结束时 interop dll 数 = {0}" -f $interopAfter)
foreach ($pair in @(@($logPath, "LogOutput-$Tag-$stamp.log"), @($errPath, "ErrorLog-$Tag-$stamp.log"))) {
  if (Test-Path $pair[0]) { Copy-Item $pair[0] (Join-Path $LogOutDir $pair[1]) -Force; Say ("已收集 {0} -> {1}" -f $pair[0], $pair[1]) }
}
$latest = Join-Path $LogOutDir "LogOutput-$Tag.log"
if (Test-Path $logPath) { Copy-Item $logPath $latest -Force }
# interop 清单
if (Test-Path $interopDir) {
  Get-ChildItem $interopDir -Filter *.dll | Sort-Object Name | ForEach-Object { "{0}`t{1}" -f $_.Name, $_.Length } |
    Set-Content -Encoding UTF8 (Join-Path $LogOutDir "interop-list-$Tag.txt")
}

# --- 6. 收尾：必须杀干净 ---
if (-not $NoKill) {
  $left = Get-Process -Name $procName -ErrorAction SilentlyContinue
  if ($left) {
    Say ("清理进程: " + (($left | ForEach-Object { $_.Id }) -join ','))
    $left | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    $still = Get-Process -Name $procName -ErrorAction SilentlyContinue
    if ($still) { Say "警告：仍有残留进程"; taskkill /IM manosaba.exe /F | Out-Null }
    else { Say '已全部结束' }
  } else { Say '无残留进程' }
}

if ($appeared) { exit 0 } else { Say 'FATAL: 进程从未出现'; exit 1 }
