# run94-evdiag.ps1 —— 「出示证据 / 选证物」这一步的界面取证
#
# 目的：用户要求补上「出示证据」，但我们不知道那一步界面长什么样（有几张线索卡、
# 「出示证据」按钮叫什么、按下去有没有确认对话框）。本脚本只取证，不改游戏。
#
# 做法：
#   1) 读第 1 格存档进正片（比点「新游戏」稳，存档在第二章附近）；
#   2) 点左上角书本图标打开魔女手册（阅读态）；
#   3) 插件侧 MANOSABA_A11Y_TEST_EVMODE=1 会对当前页调游戏自己的
#      WitchBookPageBase.Show(asPresentEvidenceMode: true)，把「出示证据」按钮点亮；
#   4) MANOSABA_A11Y_EVDIAG=1 每 6s 转储：手册 UI 的 PresentationMode / 允许类别 /
#      页类型 / 带 Present|Evidence 字样的按钮 / 线索卡数量 / RebuildableDialog。
#   5) 截图 + 汇总写 probe\logs\<Tag>-summary.txt。
#
param(
    [int]$WaitSeconds = 260,
    [string]$Tag = 'run94',
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
    # PrintWindow 对 DXGI 翻转模型返回白图（本作实测），改用屏幕拷贝
    try {
        [W32A]::SetWindowPos($h, [IntPtr](-1), 0, 0, 0, 0, 0x0013) | Out-Null
        [W32A]::SetForegroundWindow($h) | Out-Null
        Start-Sleep -Milliseconds 400
        $r = New-Object W32A+RECT
        [W32A]::GetWindowRect($h, [ref]$r) | Out-Null
        $w = $r.R - $r.L; $hh = $r.B - $r.T
        if ($w -le 0 -or $hh -le 0) { return }
        $bmp = New-Object System.Drawing.Bitmap $w, $hh
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size $w, $hh))
        $g.Dispose()
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
$env:MANOSABA_A11Y_EVDIAG = '1'
$env:MANOSABA_A11Y_TEST_EVCMD = '2'
$env:MANOSABA_A11Y_TEST_EVFORCE = '1'
$env:MANOSABA_A11Y_TEST_EVPICK = '1'
$env:MANOSABA_A11Y_TEST_MAPCHOICE = ''
$env:MANOSABA_A11Y_MAPDIAG = ''
$env:MANOSABA_A11Y_TEST_ESC = ''

$p = Start-Process (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Write-Host "[info] 启动 pid=$($p.Id)，等引擎就绪 34s…"
Start-Sleep -Seconds 34
# 按进程枚举顶层窗口，排除 ConsoleWindowClass / IME（run94 首跑 $p.MainWindowHandle
# 拿到的是 BepInEx 控制台窗口，导致点击全打在控制台上、没进正片）
$h = [IntPtr]::Zero
for ($i = 0; $i -lt 40; $i++) {
    $h = [W32A]::FindGameWindow($p.Id)
    if ($h -ne [IntPtr]::Zero) { break }
    Start-Sleep -Seconds 1
}
if ($h -eq [IntPtr]::Zero) { Write-Host '[error] 没找到游戏窗口'; Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force; exit 3 }
Write-Host "[info] 窗口 hwnd=$h"

# ---------------- 进正片（读第 1 格存档；比点新游戏稳）
$entered = $false
for ($round = 1; $round -le 3; $round++) {
    Focus-Game $h
    Click-Rel $h 0.0875 0.832     # 标题「读取」
    Start-Sleep -Seconds 4
    Focus-Game $h
    Click-Rel $h 0.179 0.233      # 第 1 格存档
    for ($i = 0; $i -lt 12; $i++) {
        Start-Sleep -Seconds 2
        if ((Log-Matches 'Reader\.say\[').Count -gt 0) { $entered = $true; break }
    }
    if ($entered) { Write-Host "[ok] 第 $round 轮读取存档进入正片"; break }
    Write-Host "[warn] 第 $round 轮没进去，重试"
    Send-Key $h 0x1B 200
    Start-Sleep -Seconds 2
}

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
Write-Host '[info] 等 40s，让 EVMODE 点亮按钮 + EVDIAG 多转储几轮…'
Start-Sleep -Seconds 40
Shot $h (Join-Path $OutDir "$Tag-book-present.png")

# ---------------- Esc 关闭
Start-Sleep -Seconds 2
Write-Host '[info] 发 Esc'
Send-Key $h 0x1B 320
Start-Sleep -Seconds 3
Shot $h (Join-Path $OutDir "$Tag-after-esc.png")

$lines = @()
$lines += "Tag=$Tag 时刻=$(Get-Date -Format 'HH:mm:ss')"
$lines += "窗口 hwnd=$h"
$lines += "进入正片=$entered"
$lines += "EVIDENCE 转储数=$((Log-Matches 'EVIDENCE dump').Count)"
$lines += "EVMODE 调用数=$((Log-Matches 'EVIDENCE TEST: 已对').Count)"
$lines += "剧情朗读数=$((Log-Matches 'Reader\.say\[').Count)"
$lines += "---- EVIDENCE ----"
$lines += (Log-Matches 'EVIDENCE[^\r\n]*' | Select-Object -Last 14)
$lines += "---- 手册/整页界面 ----"
$lines += (Log-Matches 'ScreenAssist: [^\r\n]*' | Select-Object -Last 6)
$out = Join-Path $OutDir "$Tag-summary.txt"
$lines | Set-Content -Path $out -Encoding UTF8
Write-Host "[done] 汇总: $out"
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force

