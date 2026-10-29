# run23-inject.ps1 —— InputSystem 注入版实机验收（无人值守）
#
# 背景（为什么换注入）：
#   * run17/18/19 用 Win32 mouse_event 能点进 New Game（`Reader.say` 拿到「——这里是囚牢。」），
#     但之后无论点中心/底部/Enter/Space/Z 都不推进（run18 输入矩阵全失败）。
#   * run19 做过**无补丁对照组**：不装任何 a11y 插件同样卡在第一句 → 不是补丁的锅。
#   * run20 实测 `timeScale=1`、`delta≈0.164`（≈6 FPS）、`focused=True`、
#     有 1 个 `ContinueInputUI visible=True active=True triggerActive=True`。
#   * 本作用新输入系统，Naninovel 的输入是**每帧轮询设备状态**；6 FPS 下 Win32 的
#     down→up 只隔 60ms，常常整段落在一帧内被轮询吞掉 → 点击「按了等于没按」。
# 所以本脚本不再自己点，而是让插件（MANOSABA_A11Y_INJECT=1）用
# `InputSystem.QueueStateEvent(new MouseState{buttons=1})` 把状态事件塞进引擎队列，
# 保证下一帧轮询一定看到「按住」，再在 0.28s 后补 `buttons=0`。
#
# 用法：pwsh -File probe\run23-inject.ps1 -WaitSeconds 200 -Tag run23
param(
    [int]$WaitSeconds = 200,
    [string]$Tag = 'run23',
    [string]$Locale = 'zh-Hans'
)

$ErrorActionPreference = 'Continue'
$GameDir   = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath   = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir   = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc    = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir    = Join-Path $RepoDir 'probe\logs'
$Summary   = Join-Path $OutDir ($Tag + '-a11y-summary.txt')
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W32I {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool c);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@

$t0 = Get-Date
function Now { (Get-Date).ToString('HH:mm:ss') }
function Elapsed { [int]((Get-Date) - $t0).TotalSeconds }
function Say([string]$s) { Write-Output ("[{0} T={1,3}s] {2}" -f (Now), (Elapsed), $s) }
function LogCount([string]$Pattern) {
    try { return @(Select-String -Path $LogPath -Pattern $Pattern -ErrorAction SilentlyContinue).Count } catch { return 0 }
}
function GetHwnd {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) { return $p.MainWindowHandle } else { return [IntPtr]::Zero }
}
function FocusGame {
    $h = GetHwnd
    if ($h -eq [IntPtr]::Zero) { return $false }
    [W32I]::ShowWindow($h, 9) | Out-Null
    $fg = [W32I]::GetForegroundWindow()
    $tid = [W32I]::GetWindowThreadProcessId($fg, [ref]([uint32]0))
    $my = [W32I]::GetCurrentThreadId()
    $att = $false
    if ($tid -ne 0 -and $tid -ne $my) { $att = [W32I]::AttachThreadInput($my, $tid, $true) }
    [W32I]::SetForegroundWindow($h) | Out-Null
    if ($att) { [W32I]::AttachThreadInput($my, $tid, $false) | Out-Null }
    Start-Sleep -Milliseconds 120
    return ([W32I]::GetForegroundWindow() -eq $h)
}
function Shot([string]$name) {
    $h = GetHwnd
    if ($h -eq [IntPtr]::Zero) { return }
    $r = New-Object W32I+RECT
    [W32I]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $ht = $r.B - $r.T
    if ($w -le 0 -or $ht -le 0) { return }
    $bmp = New-Object System.Drawing.Bitmap($w, $ht)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [W32I]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $path = Join-Path $OutDir ("{0}-{1}.png" -f $Tag, $name)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Say "shot -> $path"
}

Say "STEP1 清理残留进程与旧插件副本"
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Say "kill 残留 pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Start-Sleep -Seconds 2
# run19 对照组把插件目录移到了 _stash_a11y；BepInEx 会递归加载 plugins 下所有 dll，
# 留着会出现「同一插件两份」→ 必须清掉，否则测的不是新 dll。
$stash = Join-Path (Split-Path $PluginDir -Parent) '_stash_a11y'
if (Test-Path $stash) { Remove-Item $stash -Recurse -Force; Say "已删除旧副本 $stash" }
$stray = Join-Path (Split-Path $PluginDir -Parent) 'ManosabaProbe.dll'
if (Test-Path $stray) { Remove-Item $stray -Force; Say "已删除探针 dll" }

Say "STEP2 部署"
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
$deployed = Get-Item (Join-Path $PluginDir 'ManosabaA11y.dll')
Say ("deploy {0} B {1}" -f $deployed.Length, $deployed.LastWriteTime.ToString('HH:mm:ss'))
$nvda = Join-Path $RepoDir 'mod\nvda_dl\x64\nvdaControllerClient.dll'
if (Test-Path $nvda) { Copy-Item $nvda (Join-Path $PluginDir 'nvdaControllerClient.dll') -Force }

Say "STEP3 启动（TEST=1 + INJECT=1 + LOCALE=$Locale）"
$env:MANOSABA_A11Y_TEST   = '1'
$env:MANOSABA_A11Y_INJECT = '1'
$env:MANOSABA_A11Y_LOCALE = $Locale
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null

$deadline = (Get-Date).AddSeconds($WaitSeconds)
$last = -99
$shotAt = 0
$bestSay = 0
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    FocusGame | Out-Null
    $say = LogCount 'Reader\.say'
    if ($say -gt $bestSay) { $bestSay = $say }
    $t = Elapsed
    if ($t - $shotAt -ge 40) { $shotAt = $t; Shot ("t{0}" -f $t) }
    if ($t - $last -ge 15) {
        $last = $t
        $size = 0
        try { $size = [Math]::Round((Get-Item $LogPath -ErrorAction SilentlyContinue).Length / 1024) } catch { }
        Say ("say={0} injectClicks={1} TESTDRIVER={2} INJECT={3} log={4}KB" -f $say, (LogCount 'INJECT click'), (LogCount 'TESTDRIVER'), (LogCount 'INJECT'), $size)
    }
    if ($bestSay -ge 8) { Say "已抓到 $bestSay 句，提前结束"; break }
}

Say "STEP4 汇总"
$lines = @()
$lines += "===== $Tag summary $(Now)  wait=${WaitSeconds}s ====="
$lines += ("Reader.say={0} INJECT.click={1} TESTDRIVER={2} HOOK print={3} dedupe={4} Locale={5}" -f `
    (LogCount 'Reader\.say'), (LogCount 'INJECT click'), (LogCount 'TESTDRIVER'), (LogCount 'HOOK print'), (LogCount 'Reader.dedupe'), (LogCount 'Locale'))
$lines += "----- INJECT / TESTDRIVER 状态行 -----"
$lines += (Select-String -Path $LogPath -Pattern 'INJECT|TESTDRIVER' -ErrorAction SilentlyContinue |
           Where-Object { $_.Line -notmatch ' button ' } | Select-Object -First 150 | ForEach-Object { $_.Line })
$lines += "----- Reader / HOOK / Locale（前 120）-----"
$lines += (Select-String -Path $LogPath -Pattern 'Reader\.|HOOK print|Locale|patched|TEST MODE' -ErrorAction SilentlyContinue |
           Select-Object -First 120 | ForEach-Object { $_.Line })
$lines += "----- 错误 -----"
$lines += (Select-String -Path $LogPath -Pattern 'Exception|failed|未找到|error' -ErrorAction SilentlyContinue |
           Select-Object -Last 25 | ForEach-Object { $_.Line })
$lines | Set-Content -Path $Summary -Encoding UTF8
Say "summary -> $Summary"
$lines | ForEach-Object { Write-Output $_ }

Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Say "kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Say "DONE 实例已释放 $((Now))"
