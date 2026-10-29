<#
  run_re_probe.ps1 — naninovel-re 的探针运行脚本（无人值守）
  ============================================================
  作用：单实例守卫 → 部署 NaninovelReProbe.dll → 启动游戏 → 实时抓 RE_* 日志 → 到点杀进程树 → 收集证据。
  与 env-probe 的 run_probe.ps1 互不干扰（本脚本只额外做插件部署与 RE_ 过滤；不调用鼠标自动化，
  进入剧本由插件自己程序化点击 Naninovel.UI.TitleNewGameButton.OnButtonClick() 完成）。

  用法：
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_re_probe.ps1
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_re_probe.ps1 -TimeoutSec 300 -Tag struct
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_re_probe.ps1 -DisableStructPatches   # 只跑 string/bool 补丁
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_re_probe.ps1 -NoAutoLoad               # 只挂探针，不自动点击/autoplay/LoadAndPlay
    pwsh -NoProfile -ExecutionPolicy Bypass -File run_re_probe.ps1 -KeepPlugin               # 跑完不卸载插件（默认卸载，避免污染别人的 run）
  退出码：0=跑完；1=起不来；2=已有实例在跑（未启动）。
#>
[CmdletBinding()]
param(
  [int]$TimeoutSec = 240,
  [string]$GameDir = 'D:\Steam\steamapps\common\manosaba_game',
  [string]$ReDir = 'D:\DSHWorkSpace\manosaba_a11y\re',
  [string]$Tag = 're',
  [switch]$DisableStructPatches,
  [switch]$NoAutoLoad,
  [switch]$KeepPlugin,
  [switch]$NoKill
)

$ErrorActionPreference = 'Continue'
$exe = Join-Path $GameDir 'manosaba.exe'
$be = Join-Path $GameDir 'BepInEx'
$logPath = Join-Path $be 'LogOutput.log'
$errPath = Join-Path $be 'ErrorLog.log'
$pluginSrc = Join-Path $ReDir 'probe\NaninovelReProbe\bin\Release\NaninovelReProbe.dll'
$pluginsDir = Join-Path $be 'plugins'
$logOut = Join-Path $ReDir 'logs'
$sentinel = Join-Path $ReDir 'probe\DISABLE_STRUCT_PATCHES'
$noAutoSentinel = Join-Path $ReDir 'probe\NO_AUTO_LOAD'

function Say($m) { Write-Output ("[{0:HH:mm:ss}] {1}" -f (Get-Date), $m) }

if (-not (Test-Path $exe)) { Say "FATAL: exe 不存在: $exe"; exit 1 }
New-Item -ItemType Directory -Force -Path $logOut, $pluginsDir | Out-Null

# --- 0. struct 补丁开关 / 自动推进开关 ---
if ($DisableStructPatches) { Set-Content -Path $sentinel -Value 'skip struct-arg patches' -Encoding ascii; Say '哨兵文件已写入：跳过 struct 参数补丁' }
elseif (Test-Path $sentinel) { Remove-Item $sentinel -Force; Say '哨兵文件已移除：启用 struct 参数补丁' }
if ($NoAutoLoad) { Set-Content -Path $noAutoSentinel -Value 'no auto UI/autoplay/LoadAndPlay' -Encoding ascii; Say '哨兵文件已写入：NO_AUTO_LOAD（只挂探针，不自动进正片/autoplay）' }
elseif (Test-Path $noAutoSentinel) { Remove-Item $noAutoSentinel -Force; Say '哨兵文件已移除：允许自动进正片/autoplay' }

# --- 1. 部署插件 ---
if (-not (Test-Path $pluginSrc)) { Say "FATAL: 插件未编译: $pluginSrc"; exit 1 }
$target = Join-Path $pluginsDir 'NaninovelReProbe.dll'
Copy-Item $pluginSrc $target -Force
Say ("插件已部署: {0} ({1} B)" -f $target, (Get-Item $target).Length)
$installed = Get-ChildItem $pluginsDir -Filter *.dll -Recurse | ForEach-Object { $_.Name }
Say ("plugins 目录内容: " + ($installed -join ', '))

# --- 2. 单实例守卫 ---
$running = Get-Process -Name 'manosaba' -ErrorAction SilentlyContinue
if ($running) { Say ("ABORT: 已有 manosaba 在跑: " + (($running | ForEach-Object { $_.Id }) -join ',')); exit 2 }

# --- 3. 清日志（先备份上一份，避免覆盖别人的证据）---
if (Test-Path $logPath) { Copy-Item $logPath (Join-Path $logOut ("LogOutput-prev-{0}.log" -f (Get-Date -Format 'HHmmss'))) -Force }
Remove-Item $logPath, $errPath -ErrorAction SilentlyContinue
$myLog = Join-Path $logOut ("re-probe-run-{0}-{1}.log" -f $Tag, (Get-Date -Format 'HHmmss'))
Say ("本次证据文件: {0}" -f $myLog)

# --- 4. 启动 ---
Say '启动 manosaba.exe'
Start-Process -FilePath $exe -WorkingDirectory $GameDir

# --- 5. 实时抓 RE_* ---
$t0 = Get-Date
$deadline = $t0.AddSeconds($TimeoutSec)
$lastLen = 0
$proc = $null
$reLines = New-Object System.Collections.Generic.List[string]
while ((Get-Date) -lt $deadline) {
  Start-Sleep -Seconds 2
  if (-not $proc) {
    $proc = Get-Process -Name 'manosaba' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($proc) { Say ("进程 PID={0}" -f $proc.Id) }
  }
  if ($proc) { $proc.Refresh(); if ($proc.HasExited) { Say ("进程已退出 exitcode={0}" -f $proc.ExitCode); break } }
  if (Test-Path $logPath) {
    try {
      $lines = Get-Content $logPath -ErrorAction SilentlyContinue
      if ($lines -and $lines.Count -gt $lastLen) {
        $new = $lines[$lastLen..($lines.Count - 1)]
        $lastLen = $lines.Count
        foreach ($ln in $new) {
          if ($ln -match 'RE_|NaninovelReProbe|Naninovel RE Probe') {
            $reLines.Add($ln)
            $out = $ln -replace '^\[[^\]]+\]\s*\[NaninovelReProbe\]\s*', ''
            Write-Output ("  RE> " + $out)
          }
        }
      }
    } catch { }
  }
}

# --- 6. 收集 ---
if (Test-Path $logPath) { Copy-Item $logPath (Join-Path $logOut ("LogOutput-{0}.log" -f $Tag)) -Force }
$reLines | Set-Content -Path $myLog -Encoding UTF8
$reProbeFile = Join-Path $logOut 're-probe.log'
if (Test-Path $reProbeFile) { Copy-Item $reProbeFile (Join-Path $logOut ("re-probe-journal-{0}.log" -f $Tag)) -Force }
Say ("RE 行数: {0}; 已写 {1}" -f $reLines.Count, $myLog)
$err = $null
if (Test-Path $errPath) { $err = Get-Content $errPath -ErrorAction SilentlyContinue }
if ($err -and $err.Count -gt 0) { Say '--- ErrorLog ---'; $err | Select-Object -First 40 | ForEach-Object { Write-Output ("  ! " + $_) } }

# --- 7. 收尾 ---
if (-not $NoKill) {
  $left = Get-Process -Name 'manosaba' -ErrorAction SilentlyContinue
  if ($left) {
    Say ("清理进程: " + (($left | ForEach-Object { $_.Id }) -join ','))
    $left | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    if (Get-Process -Name 'manosaba' -ErrorAction SilentlyContinue) { taskkill /IM manosaba.exe /F | Out-Null }
    else { Say '已全部结束' }
  }
}
# --- 8. 卸载插件（默认）：避免残留 DLL 在别人的 run 里自动 LoadAndPlay/autoplay ---
$deployed = Join-Path $pluginsDir 'NaninovelReProbe.dll'
if (-not $KeepPlugin) {
  if (Test-Path $deployed) {
    Remove-Item $deployed -Force -ErrorAction SilentlyContinue
    if (Test-Path $deployed) { Say "WARN: 插件卸载失败（可能仍被进程占用）: $deployed" } else { Say "插件已卸载: $deployed" }
  }
}
else { Say "插件保留（-KeepPlugin）: $deployed" }

if ($proc) { exit 0 } else { Say 'FATAL: 进程从未出现'; exit 1 }
