# run83-assist.ps1 —— 验证 ScreenAssist：魔女手册自动播报 + Esc 关闭
#
# 背景：用户 2026-10-31 反馈「记录页会困死玩家」「地图页导航不到地点」。
#   ScreenAssist.cs 的做法：界面可见就自动播报「界面名，页签。内容：…」，Esc 直接
#   调用界面自己的 CloseButton.onClick（绕开平台层被遮挡层骗掉的射线判断）。
#
# 本脚本只做三件事（不依赖 Steam、不依赖物理按键注入的可靠性）：
#   1) 等 34s 引擎就绪 → 循环点标题「新游戏」直到日志出现剧情朗读；
#   2) 点若干次推进，然后点审判画面左上角书本图标 (84,62) 打开魔女手册；
#   3) 收集日志里的 `ScreenAssist: 播报 …`，再发一次 Esc 看是否出现 `Esc 关闭`；
#   4) 截图 + 汇总写 probe\logs\<Tag>-summary.txt。
param(
    [int]$WaitSeconds = 200,
    [string]$Tag = 'run83',
    [string]$Locale = 'zh-Hans'
)
$ErrorActionPreference = 'Continue'
$GameDir   = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath   = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir   = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc    = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir    = Join-Path $RepoDir 'probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
public class W32A {
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder sb, int n);
  public delegate bool EnumProc(IntPtr h, IntPtr p);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
  public static IntPtr FindGameWindow(int pid) {
    IntPtr found = IntPtr.Zero;
    EnumWindows(delegate(IntPtr h, IntPtr p) {
      uint wp; GetWindowThreadProcessId(h, out wp);
      if (wp != (uint)pid) return true;
      var c = new StringBuilder(256); GetClassName(h, c, 256);
      if (c.ToString() == "ConsoleWindowClass") return true;
      if (c.ToString() == "IME" || c.ToString() == "MSCTFIME UI") return true;
      found = h; return false;
    }, IntPtr.Zero);
    return found;
  }
}
"@

function Get-GameWindow([int]$pid) { return [W32A]::FindGameWindow($pid) }
function Focus-Game([IntPtr]$h) {
    [W32A]::ShowWindow($h, 9) | Out-Null
    [W32A]::SetWindowPos($h, [IntPtr](-1), 0, 0, 0, 0, 0x0013) | Out-Null
    [W32A]::BringWindowToTop($h) | Out-Null
    [W32A]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 200
}
function Click-Client([IntPtr]$h, [int]$x, [int]$y) {
    $pt = New-Object W32A+POINT; $pt.X = $x; $pt.Y = $y
    [W32A]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32A]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 120
    [W32A]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 150
    [W32A]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200
}
function Click-Rel([IntPtr]$h, [double]$rx, [double]$ry) {
    $r = New-Object W32A+RECT
    [W32A]::GetClientRect($h, [ref]$r) | Out-Null
    $cw = $r.R - $r.L; $ch = $r.B - $r.T
    if ($cw -le 0 -or $ch -le 0) { return }
    Click-Client $h ([int]($cw * $rx)) ([int]($ch * $ry))
}
function Send-Key([IntPtr]$h, [int]$vk, [int]$holdMs) {
    Focus-Game $h | Out-Null
    [W32A]::keybd_event([byte]$vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $holdMs
    [W32A]::keybd_event([byte]$vk, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200
}
function Shot([IntPtr]$h, [string]$path) {
    try {
        $r = New-Object W32A+RECT
        [W32A]::GetWindowRect($h, [ref]$r) | Out-Null
        $w = $r.R - $r.L; $hh = $r.B - $r.T
        if ($w -le 0 -or $hh -le 0) { return }
        $bmp = New-Object System.Drawing.Bitmap $w, $hh
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        [W32A]::PrintWindow($h, $hdc, 0) | Out-Null
        $g.ReleaseHdc($hdc); $g.Dispose()
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    } catch { Write-Host "[warn] 截图失败: $($_.Exception.Message)" }
}
function Log-Matches([string]$pattern) {
    if (-not (Test-Path $LogPath)) { return @() }
    try {
        $fs = [IO.File]::Open($LogPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8)
        $text = $sr.ReadToEnd(); $sr.Dispose(); $fs.Dispose()
        return @([regex]::Matches($text, $pattern) | ForEach-Object { $_.Value })
    } catch { return @() }
}

# ---------------- 准备
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
Get-ChildItem (Join-Path $GameDir 'BepInEx\plugins') -Filter '*.dll' -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -notlike '*manosaba_a11y*' } | ForEach-Object { Move-Item $_.FullName ($_.FullName + '.bak') -Force }
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
Remove-Item $LogPath -ErrorAction SilentlyContinue

$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_LOCALE = $Locale
$env:MANOSABA_A11Y_TEST = ''
$env:MANOSABA_A11Y_JUMP = ''
$env:MANOSABA_A11Y_TEST_NAV = ''
$env:MANOSABA_A11Y_TEST_REPEAT = ''
$env:MANOSABA_A11Y_PROBE_UI = ''

$p = Start-Process (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Write-Host "[info] 启动 pid=$($p.Id)，等引擎就绪 34s…"
Start-Sleep -Seconds 34
# 用 Process.MainWindowHandle（实测就是 UnityWndClass 主窗口）轮询，比 EnumWindows 稳
$h = [IntPtr]::Zero
for ($i = 0; $i -lt 40; $i++) {
    $p.Refresh()
    if ($p.MainWindowHandle -ne [IntPtr]::Zero) { $h = $p.MainWindowHandle; break }
    Start-Sleep -Seconds 1
}
if ($h -eq [IntPtr]::Zero) { Write-Host '[error] 没找到游戏窗口'; Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force; exit 3 }
Write-Host "[info] 窗口 hwnd=$h"

# ---------------- 进正片
$entered = $false
for ($i = 1; $i -le 10; $i++) {
    Focus-Game $h
    Click-Rel $h 0.228 0.806
    Start-Sleep -Seconds 5
    if ((Log-Matches 'Reader\.say\[').Count -gt 0 -or (Log-Matches '加载播报（').Count -gt 0) { $entered = $true; Write-Host "[ok] 第 $i 次点击进入正片/加载"; break }
}
if (-not $entered) { Write-Host '[warn] 10 次点击仍未进正片，继续观察' }

# ---------------- 推进几句，然后打开魔女手册
for ($i = 1; $i -le 12; $i++) {
    Focus-Game $h
    Click-Rel $h 0.5 0.5
    Start-Sleep -Milliseconds 1500
}
Start-Sleep -Seconds 2
Shot $h (Join-Path $OutDir "$Tag-story.png")
Write-Host '[info] 点左上角书本图标 (84,62) 打开魔女手册'
for ($i = 0; $i -lt 3; $i++) {
    Focus-Game $h
    Click-Rel $h 0.0437 0.0574
    Start-Sleep -Seconds 4
    if ((Log-Matches 'ScreenAssist: 播报').Count -gt 0) { break }
}
Shot $h (Join-Path $OutDir "$Tag-book.png")

# ---------------- Esc 关闭
Start-Sleep -Seconds 2
Write-Host '[info] 发 Esc'
Send-Key $h 0x1B 320
Start-Sleep -Seconds 3
Shot $h (Join-Path $OutDir "$Tag-after-esc.png")

$lines = @()
$lines += "Tag=$Tag 时刻=$(Get-Date -Format 'HH:mm:ss')"
$lines += "窗口 hwnd=$h"
$lines += "ScreenAssist 播报数=$((Log-Matches 'ScreenAssist: 播报').Count)"
$lines += "Esc 关闭数=$((Log-Matches 'ScreenAssist: Esc 关闭').Count)"
$lines += "剧情朗读数=$((Log-Matches 'Reader\.say\[').Count)"
$lines += "---- ScreenAssist 播报 ----"
$lines += (Log-Matches 'ScreenAssist: 播报[^\r\n]*' | Select-Object -Last 10)
$lines += "---- Esc / 错误 ----"
$lines += (Log-Matches 'ScreenAssist: (Esc 关闭|没找到关闭按钮)[^\r\n]*' | Select-Object -Last 5)
$out = Join-Path $OutDir "$Tag-summary.txt"
$lines | Set-Content -Path $out -Encoding UTF8
Write-Host "[done] 汇总: $out"
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
