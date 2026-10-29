# run19-baseline.ps1 —— 对照组：完全不装 A11y 补丁，看游戏自己能否被点击推进
# 目的：排除「我方补丁把 Print/Reveal 链打断导致卡在第一句」。
param([int]$Tag = 19)
$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir = 'D:\DSHWorkSpace\manosaba_a11y'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir = Join-Path $RepoDir 'probe\logs'

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W32B {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
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

# 把 A11y 插件整体移走（对照组）
$Stash = Join-Path $GameDir 'BepInEx\plugins\_stash_a11y'
New-Item -ItemType Directory -Force -Path $Stash | Out-Null
if (Test-Path $PluginDir) { Move-Item $PluginDir (Join-Path $Stash ("manosaba_a11y-" + (Get-Date -Format HHmmss))) -Force }

$env:MANOSABA_A11Y_DIAG = $null
$env:MANOSABA_A11Y_TEST = $null
$env:MANOSABA_A11Y_LOCALE = $null
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null

function GetHwnd { $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1; if ($p) { return $p.MainWindowHandle } else { return [IntPtr]::Zero } }
function FocusGame {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return $false }
    [W32B]::ShowWindow($h, 9) | Out-Null
    $fg = [W32B]::GetForegroundWindow(); $tid = [W32B]::GetWindowThreadProcessId($fg, [ref]([uint32]0)); $my = [W32B]::GetCurrentThreadId()
    $att = $false
    if ($tid -ne 0 -and $tid -ne $my) { $att = [W32B]::AttachThreadInput($my, $tid, $true) }
    [W32B]::SetForegroundWindow($h) | Out-Null
    if ($att) { [W32B]::AttachThreadInput($my, $tid, $false) | Out-Null }
    Start-Sleep -Milliseconds 120
    return ([W32B]::GetForegroundWindow() -eq $h)
}
function ClickClient([int]$cx, [int]$cy) {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return }
    $pt = New-Object W32B+POINT; $pt.X = $cx; $pt.Y = $cy
    [W32B]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32B]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 60
    [W32B]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 50
    [W32B]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}
function Shot([string]$name) {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return }
    FocusGame | Out-Null
    $r = New-Object W32B+RECT; [W32B]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T))
    $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [W32B]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $p = Join-Path $OutDir ("run{0}-{1}.png" -f $Tag, $name)
    $bmp.Save($p, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Output "shot -> $p"
}

Write-Output "开始等标题 30s"
Start-Sleep -Seconds 30
Write-Output ("focus={0}" -f (FocusGame))
Shot 'title'
ClickClient 330 770
Write-Output "已点 New Game"
Start-Sleep -Seconds 40
Shot 'story-0'
Write-Output "连续点击中心 12 次"
for ($i = 1; $i -le 12; $i++) { FocusGame | Out-Null; ClickClient 960 540; Start-Sleep -Milliseconds 900 }
Start-Sleep -Seconds 3
Shot 'story-1'
Write-Output "再点底部文本框 8 次"
for ($i = 1; $i -le 8; $i++) { FocusGame | Out-Null; ClickClient 960 930; Start-Sleep -Milliseconds 900 }
Start-Sleep -Seconds 3
Shot 'story-2'
Write-Output "再点中心 8 次"
for ($i = 1; $i -le 8; $i++) { FocusGame | Out-Null; ClickClient 960 540; Start-Sleep -Milliseconds 900 }
Start-Sleep -Seconds 3
Shot 'story-3'
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Write-Output "DONE (插件已移到 $Stash，记得恢复)"
