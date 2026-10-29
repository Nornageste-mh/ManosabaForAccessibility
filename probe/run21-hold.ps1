# run21-hold.ps1 —— 关键假设：游戏 6 FPS，Naninovel 输入是「每帧轮询 InputSystem」，
# 之前 down→up 只隔 60~80ms，按下与抬起都落在同一帧内 → 轮询看不到「按住」状态，点击被吞。
# 本轮把每次点击改成「按下 → 按住 500ms → 抬起」，键盘同理，验证剧情能否推进。
param([int]$Tag = 21)
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
public class W32H {
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
    [W32H]::ShowWindow($h, 9) | Out-Null
    $fg = [W32H]::GetForegroundWindow(); $tid = [W32H]::GetWindowThreadProcessId($fg, [ref]([uint32]0)); $my = [W32H]::GetCurrentThreadId()
    $att = $false
    if ($tid -ne 0 -and $tid -ne $my) { $att = [W32H]::AttachThreadInput($my, $tid, $true) }
    [W32H]::SetForegroundWindow($h) | Out-Null
    if ($att) { [W32H]::AttachThreadInput($my, $tid, $false) | Out-Null }
    Start-Sleep -Milliseconds 150
    return ([W32H]::GetForegroundWindow() -eq $h)
}
function ClickHold([int]$cx, [int]$cy, [int]$holdMs = 500) {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return }
    $pt = New-Object W32H+POINT; $pt.X = $cx; $pt.Y = $cy
    [W32H]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32H]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 100
    [W32H]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $holdMs
    [W32H]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}
function KeyHold([byte]$vk, [int]$holdMs = 400) {
    FocusGame | Out-Null
    [W32H]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $holdMs
    [W32H]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
}
function Shot([string]$name) {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return }
    FocusGame | Out-Null
    $r = New-Object W32H+RECT; [W32H]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T))
    $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [W32H]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $p = Join-Path $OutDir ("run{0}-{1}.png" -f $Tag, $name)
    $bmp.Save($p, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Output "shot -> $p"
}
function SayCount { @(Select-String -Path $LogPath -Pattern 'Reader.say' -ErrorAction SilentlyContinue).Count }

Write-Output "等标题 30s"; Start-Sleep -Seconds 30
FocusGame | Out-Null
ClickHold 330 770 500
Write-Output "已点 New Game（按住 500ms）"
Start-Sleep -Seconds 45
Shot 'story-0'
Write-Output ("进入后 say={0}" -f (SayCount))

Write-Output "A) 长按点击中心 x5"
for ($i = 1; $i -le 5; $i++) { FocusGame | Out-Null; ClickHold 960 540 500; Start-Sleep -Milliseconds 900 }
Start-Sleep -Seconds 2
Write-Output ("   say={0}" -f (SayCount))
Shot 'after-click'

Write-Output "B) 长按 Enter x5"
for ($i = 1; $i -le 5; $i++) { KeyHold 13 400; Start-Sleep -Milliseconds 900 }
Start-Sleep -Seconds 2
Write-Output ("   say={0}" -f (SayCount))
Shot 'after-enter'

Write-Output "C) 长按空格 x3"
for ($i = 1; $i -le 3; $i++) { KeyHold 32 400; Start-Sleep -Milliseconds 900 }
Start-Sleep -Seconds 2
Write-Output ("   say={0}" -f (SayCount))
Shot 'after-space'

$summary = Join-Path $OutDir 'run21-hold-summary.txt'
@(
    "===== run$Tag hold summary $(Get-Date -Format HH:mm:ss) =====",
    ("say={0}" -f (SayCount)),
    (Select-String -Path $LogPath -Pattern 'Reader\.|a11y\] DIAG' -ErrorAction SilentlyContinue | ForEach-Object { $_.Line })
) | Set-Content -Path $summary -Encoding UTF8
Write-Output "summary -> $summary"
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Write-Output "DONE"
