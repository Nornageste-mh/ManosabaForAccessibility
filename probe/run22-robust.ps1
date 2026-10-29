# run22-robust.ps1 —— 标题点击重试版：6 FPS 下标题动画/按钮可用时机不稳定，
# 单次点击经常落空。本轮反复点 New Game 直到日志出现 Reader.say（说明真的进正片），
# 然后再验证「长按点击 / 长按 Enter」能否推进下一句。
param([int]$Tag = 22)
$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir = Join-Path $RepoDir 'probe\logs'

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W32R {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool c);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@

Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
Start-Sleep -Seconds 2
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
Remove-Item Env:\MANOSABA_A11Y_TEST -ErrorAction SilentlyContinue
Remove-Item Env:\MANOSABA_A11Y_TEST_PLAY -ErrorAction SilentlyContinue
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null

function GetHwnd { $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1; if ($p) { return $p.MainWindowHandle } else { return [IntPtr]::Zero } }
function FocusGame {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return $false }
    [W32R]::ShowWindow($h, 9) | Out-Null
    $fg = [W32R]::GetForegroundWindow(); $tid = [W32R]::GetWindowThreadProcessId($fg, [ref]([uint32]0)); $my = [W32R]::GetCurrentThreadId()
    $att = $false
    if ($tid -ne 0 -and $tid -ne $my) { $att = [W32R]::AttachThreadInput($my, $tid, $true) }
    [W32R]::SetForegroundWindow($h) | Out-Null
    if ($att) { [W32R]::AttachThreadInput($my, $tid, $false) | Out-Null }
    Start-Sleep -Milliseconds 150
    return ([W32R]::GetForegroundWindow() -eq $h)
}
function ClickHold([int]$cx, [int]$cy, [int]$holdMs = 450) {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return }
    $pt = New-Object W32R+POINT; $pt.X = $cx; $pt.Y = $cy
    [W32R]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32R]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 100
    [W32R]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $holdMs
    [W32R]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}
function KeyHold([byte]$vk, [int]$holdMs = 350) {
    FocusGame | Out-Null
    [W32R]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $holdMs
    [W32R]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
}
function Shot([string]$name) {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return }
    FocusGame | Out-Null
    $r = New-Object W32R+RECT; [W32R]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T))
    $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [W32R]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $p = Join-Path $OutDir ("run{0}-{1}.png" -f $Tag, $name)
    $bmp.Save($p, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Output "shot -> $p"
}
function SayCount { @(Select-String -Path $LogPath -Pattern 'Reader.say' -ErrorAction SilentlyContinue).Count }

Write-Output "等标题 22s"; Start-Sleep -Seconds 22
$inStory = $false
for ($try = 1; $try -le 8; $try++) {
    FocusGame | Out-Null
    ClickHold 330 770 450
    Start-Sleep -Seconds 10
    $n = SayCount
    Write-Output ("try {0}: say={1}" -f $try, $n)
    if ($n -gt 0) { $inStory = $true; break }
    # 再给标题一点时间（有些轮次要等入场动画播完）
    if ($try -eq 3) { Start-Sleep -Seconds 15 }
}
Shot 'story-0'
Write-Output ("进入正片={0} say={1}" -f $inStory, (SayCount))
if (-not $inStory) {
    Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
    Write-Output "FAILED-TO-START"
    exit 2
}

Write-Output "A) 长按点击中心 x4"
for ($i = 1; $i -le 4; $i++) { FocusGame | Out-Null; ClickHold 960 540 450; Start-Sleep -Milliseconds 1200 }
Start-Sleep -Seconds 2
$afterClick = SayCount
Write-Output ("   say={0}" -f $afterClick)
Shot 'after-click'

Write-Output "B) 长按 Enter x4"
for ($i = 1; $i -le 4; $i++) { KeyHold 13 350; Start-Sleep -Milliseconds 1200 }
Start-Sleep -Seconds 2
$afterEnter = SayCount
Write-Output ("   say={0}" -f $afterEnter)
Shot 'after-enter'

Write-Output "C) 长按空格 x4"
for ($i = 1; $i -le 4; $i++) { KeyHold 32 350; Start-Sleep -Milliseconds 1200 }
Start-Sleep -Seconds 2
$afterSpace = SayCount
Write-Output ("   say={0}" -f $afterSpace)
Shot 'after-space'

$summary = Join-Path $OutDir 'run22-robust-summary.txt'
@(
    "===== run$Tag robust summary $(Get-Date -Format HH:mm:ss) =====",
    ("say total={0} afterClick={1} afterEnter={2} afterSpace={3}" -f (SayCount), $afterClick, $afterEnter, $afterSpace),
    "----- Reader 行 -----",
    (Select-String -Path $LogPath -Pattern 'Reader\.' -ErrorAction SilentlyContinue | ForEach-Object { $_.Line })
) | Set-Content -Path $summary -Encoding UTF8
Write-Output "summary -> $summary"
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Write-Output "DONE"
