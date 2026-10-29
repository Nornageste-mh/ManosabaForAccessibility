# run18-input.ps1 —— 定位「哪种输入能推进剧情」
# 背景：run17 真实点击已经进了正片（截图显示第一句「——这里是囚牢。」），
#       但点击屏幕中心 (960,540) 60 次都没推进第二句。
# 本轮只做一件事：进入正片后依次尝试不同输入手段，每步之后看 Reader.say 有没有增长，
#       从而确定 TestDriver 该用哪种输入驱动实机验收。
param(
    [int]$Tag = 18
)
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
public class W32I {
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
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null

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
function ClickClient([int]$cx, [int]$cy) {
    $h = GetHwnd
    if ($h -eq [IntPtr]::Zero) { return }
    $pt = New-Object W32I+POINT
    $pt.X = $cx; $pt.Y = $cy
    [W32I]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32I]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 60
    [W32I]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [W32I]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}
function SendKey([byte]$vk, [string]$name) {
    FocusGame | Out-Null
    [W32I]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [W32I]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
    Write-Output "  key $name (vk=$vk)"
}
function Shot([string]$name) {
    $h = GetHwnd
    if ($h -eq [IntPtr]::Zero) { return }
    FocusGame | Out-Null
    $r = New-Object W32I+RECT
    [W32I]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $ht = $r.B - $r.T
    $bmp = New-Object System.Drawing.Bitmap($w, $ht)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [W32I]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $p = Join-Path $OutDir ("run{0}-{1}.png" -f $Tag, $name)
    $bmp.Save($p, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "shot -> $p"
}
function SayCount { @(Select-String -Path $LogPath -Pattern 'Reader.say' -ErrorAction SilentlyContinue).Count }
function HookCount { @(Select-String -Path $LogPath -Pattern 'Reader\.say\[print' -ErrorAction SilentlyContinue).Count }

Write-Output "等标题 30s"; Start-Sleep -Seconds 30
FocusGame | Out-Null
ClickClient 330 770
Write-Output "已点 New Game"
Start-Sleep -Seconds 45
Shot 'story-0'
Write-Output ("进入后 say={0} print-say={1}" -f (SayCount), (HookCount))

Write-Output "A) 点击画面中心 (960,540) x3"
for ($i = 1; $i -le 3; $i++) { FocusGame | Out-Null; ClickClient 960 540; Start-Sleep -Milliseconds 700 }
Start-Sleep -Seconds 2
Write-Output ("   say={0}" -f (SayCount))

Write-Output "B) 点击底部文本框 (960,930) x3"
for ($i = 1; $i -le 3; $i++) { FocusGame | Out-Null; ClickClient 960 930; Start-Sleep -Milliseconds 700 }
Start-Sleep -Seconds 2
Write-Output ("   say={0}" -f (SayCount))

Write-Output "C) 回车 Enter x3"
for ($i = 1; $i -le 3; $i++) { SendKey 13 'Enter'; Start-Sleep -Milliseconds 700 }
Start-Sleep -Seconds 2
Write-Output ("   say={0}" -f (SayCount))
Shot 'after-enter'

Write-Output "D) 空格 Space x3"
for ($i = 1; $i -le 3; $i++) { SendKey 32 'Space'; Start-Sleep -Milliseconds 700 }
Start-Sleep -Seconds 2
Write-Output ("   say={0}" -f (SayCount))
Shot 'after-space'

Write-Output "E) 鼠标中键/右键? 先试 Z 键 x3"
for ($i = 1; $i -le 3; $i++) { SendKey 90 'Z'; Start-Sleep -Milliseconds 700 }
Start-Sleep -Seconds 2
Write-Output ("   say={0}" -f (SayCount))
Shot 'final'

$summary = Join-Path $OutDir ("run{0}-input-summary.txt" -f $Tag)
@(
    "===== run$Tag input summary $(Get-Date -Format HH:mm:ss) =====",
    ("say={0} printSay={1}" -f (SayCount), (HookCount)),
    "----- Reader.say 全部 -----",
    (Select-String -Path $LogPath -Pattern 'Reader\.' -ErrorAction SilentlyContinue | Select-Object -First 60 | ForEach-Object { $_.Line })
) | Set-Content -Path $summary -Encoding UTF8
Write-Output "summary -> $summary"

Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Write-Output "DONE"
