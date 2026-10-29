# run17-click.ps1 —— 真实鼠标点击版实机验收
# 目标：从标题画面用真正的鼠标点击进入「New Game」，再定时点击推进剧情，
#       观察 [a11y] Reader.say 是否逐句增长（这是玩家实际走的路径）。
param(
    [int]$WaitSeconds = 240,
    [string]$Tag = 'run17'
)
$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir = Join-Path $RepoDir 'probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W32C {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
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
Write-Output ("deploy {0} B" -f (Get-Item (Join-Path $PluginDir 'ManosabaA11y.dll')).Length)

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
    [W32C]::ShowWindow($h, 9) | Out-Null
    $fg = [W32C]::GetForegroundWindow()
    $tid = [W32C]::GetWindowThreadProcessId($fg, [ref]([uint32]0))
    $my = [W32C]::GetCurrentThreadId()
    $att = $false
    if ($tid -ne 0 -and $tid -ne $my) { $att = [W32C]::AttachThreadInput($my, $tid, $true) }
    [W32C]::SetForegroundWindow($h) | Out-Null
    if ($att) { [W32C]::AttachThreadInput($my, $tid, $false) | Out-Null }
    Start-Sleep -Milliseconds 150
    return ([W32C]::GetForegroundWindow() -eq $h)
}
function ClickClient([int]$cx, [int]$cy, [string]$label) {
    $h = GetHwnd
    if ($h -eq [IntPtr]::Zero) { Write-Output ("click {0}: no window" -f $label); return }
    $pt = New-Object W32C+POINT
    $pt.X = $cx; $pt.Y = $cy
    [W32C]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32C]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 80
    [W32C]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [W32C]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Write-Output ("click {0} client=({1},{2}) screen=({3},{4}) fg={5}" -f $label, $cx, $cy, $pt.X, $pt.Y, (FocusGame))
}
function Shot([string]$name) {
    $h = GetHwnd
    if ($h -eq [IntPtr]::Zero) { return }
    FocusGame | Out-Null
    $r = New-Object W32C+RECT
    [W32C]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $ht = $r.B - $r.T
    $bmp = New-Object System.Drawing.Bitmap($w, $ht)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [W32C]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $path = Join-Path $OutDir ("{0}-{1}.png" -f $Tag, $name)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "shot -> $path"
}

Write-Output "等标题画面 30s"
Start-Sleep -Seconds 30
FocusGame | Out-Null
Shot 'title'
# New Game 文字大概在 (330,770)，另备 (450,950)（env-probe run3 成功过）
ClickClient 330 770 'newgame-a'
Start-Sleep -Seconds 4
ClickClient 330 770 'newgame-b'
Start-Sleep -Seconds 6
Shot 'after-newgame'

$deadline = (Get-Date).AddSeconds($WaitSeconds)
$i = 0
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 3
    $i++
    FocusGame | Out-Null
    ClickClient 960 540 ("advance-{0}" -f $i)
    $say = @(Select-String -Path $LogPath -Pattern 'Reader.say' -ErrorAction SilentlyContinue).Count
    if ($i % 10 -eq 0) {
        Write-Output ("T={0}s say={1}" -f $i * 3, $say)
        Shot ("adv{0}" -f $i)
    }
    if ($say -ge 15) { Write-Output "抓到 $say 句，提前结束"; break }
}

$summary = Join-Path $OutDir ($Tag + '-a11y-summary.txt')
@(
    "===== $Tag summary $(Get-Date -Format HH:mm:ss) =====",
    ("HOOK print={0} say={1} TESTDRIVER={2}" -f @(Select-String -Path $LogPath -Pattern 'HOOK print' -ErrorAction SilentlyContinue).Count, @(Select-String -Path $LogPath -Pattern 'Reader.say' -ErrorAction SilentlyContinue).Count, @(Select-String -Path $LogPath -Pattern 'TESTDRIVER' -ErrorAction SilentlyContinue).Count),
    "----- scriptPlayer / panels / say -----",
    (Select-String -Path $LogPath -Pattern 'scriptPlayer |panel |Reader.say|HOOK print|Exception' -ErrorAction SilentlyContinue | Select-Object -First 120 | ForEach-Object { $_.Line })
) | Set-Content -Path $summary -Encoding UTF8
Write-Output "summary -> $summary"

Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Write-Output "DONE"
