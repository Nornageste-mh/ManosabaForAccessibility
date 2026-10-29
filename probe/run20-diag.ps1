# run20-diag.ps1 —— 实机诊断：游戏停在第一句时，Unity 时间/焦点、Naninovel 等待状态、ContinueInputUI 可见性
# 用 TestMode + PlayMode：TestDriver 会自己隔 4 秒 dump 一次状态，本脚本只负责真实点击（New Game + 中心点击）。
param([int]$Tag = 20)
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
public class W32D {
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
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
$env:MANOSABA_A11Y_TEST = '1'
$env:MANOSABA_A11Y_TEST_PLAY = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null

function GetHwnd { $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1; if ($p) { return $p.MainWindowHandle } else { return [IntPtr]::Zero } }
function FocusGame {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return $false }
    [W32D]::ShowWindow($h, 9) | Out-Null
    $fg = [W32D]::GetForegroundWindow(); $tid = [W32D]::GetWindowThreadProcessId($fg, [ref]([uint32]0)); $my = [W32D]::GetCurrentThreadId()
    $att = $false
    if ($tid -ne 0 -and $tid -ne $my) { $att = [W32D]::AttachThreadInput($my, $tid, $true) }
    [W32D]::SetForegroundWindow($h) | Out-Null
    if ($att) { [W32D]::AttachThreadInput($my, $tid, $false) | Out-Null }
    Start-Sleep -Milliseconds 120
    return ([W32D]::GetForegroundWindow() -eq $h)
}
function ClickClient([int]$cx, [int]$cy) {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return }
    $pt = New-Object W32D+POINT; $pt.X = $cx; $pt.Y = $cy
    [W32D]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32D]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 80
    [W32D]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 80
    [W32D]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
}
function Shot([string]$name) {
    $h = GetHwnd; if ($h -eq [IntPtr]::Zero) { return }
    FocusGame | Out-Null
    $r = New-Object W32D+RECT; [W32D]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.R - $r.L), ($r.B - $r.T))
    $g = [System.Drawing.Graphics]::FromImage($bmp); $hdc = $g.GetHdc()
    [W32D]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $p = Join-Path $OutDir ("run{0}-{1}.png" -f $Tag, $name)
    $bmp.Save($p, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Output "shot -> $p"
}
function SayCount { @(Select-String -Path $LogPath -Pattern 'Reader.say' -ErrorAction SilentlyContinue).Count }

Write-Output "等标题 30s"; Start-Sleep -Seconds 30
Write-Output ("focus={0}" -f (FocusGame))
ClickClient 330 770
Write-Output "已点 New Game"
Start-Sleep -Seconds 40
Write-Output ("进入后 say={0}" -f (SayCount))
Shot 'story-0'

Write-Output "持续点击中心 60s（让 TestDriver dump 出等待状态）"
$deadline = (Get-Date).AddSeconds(60)
while ((Get-Date) -lt $deadline) {
    FocusGame | Out-Null
    ClickClient 960 540
    Start-Sleep -Milliseconds 1200
}
Shot 'story-1'
Write-Output ("点击后 say={0}" -f (SayCount))

$summary = Join-Path $OutDir 'run20-diag-summary.txt'
@(
    "===== run$Tag diag summary $(Get-Date -Format HH:mm:ss) =====",
    ("say={0}" -f (SayCount)),
    "----- TESTDRIVER 与 Reader 行 -----",
    (Select-String -Path $LogPath -Pattern 'TESTDRIVER\[|Reader\.|a11y\] TEST' -ErrorAction SilentlyContinue | ForEach-Object { $_.Line })
) | Set-Content -Path $summary -Encoding UTF8
Write-Output "summary -> $summary"
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Write-Output "DONE"
