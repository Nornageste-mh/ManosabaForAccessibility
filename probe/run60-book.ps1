# run60-book.ps1 —— 魔女手册（图鉴）实机验证：命名 + 导航朗读
#
# 背景（用户 2026-10-29 截图 + 「有很多未命名控件」）：
#   玩家在魔女手册里按导航听到一串「未命名开关」。run59 的 UIPROBE 已经证实地图页的
#   地点按钮叫 Focus、父物体是英文地点 id，页签叫 Tab_*；本次要验证：
#   1) UiNav.Game.cs 的新别名表是否真的让这些控件念出中文名（看 resolved= 与 [朗读] 行）
#   2) 手册打开后按 Alt+Shift+F1 进导航、方向键移动，实际念出来的句子
#
# 手册入口：审判画面左上角的书本图标（run59 t151 截图中位于客户区约 (84,62)）。
param(
    [int]$WaitSeconds = 240,
    [string]$Tag = 'run60',
    [string]$Jump = 'Act01_Chapter01/Act01_Chapter01_Trial01',
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
public class W32B {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@

function Get-GameWindow {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $p) { return [IntPtr]::Zero }
    return $p.MainWindowHandle
}
function Focus-Window([IntPtr]$h) {
    [W32B]::ShowWindow($h, 9) | Out-Null
    [W32B]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 150
}
function Click-Client([IntPtr]$h, [int]$x, [int]$y) {
    $pt = New-Object W32B+POINT; $pt.X = $x; $pt.Y = $y
    [W32B]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32B]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 120
    [W32B]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 150
    [W32B]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 130
}
function Send-Key([byte]$vk, [bool]$extended = $false) {
    # 方向键 / PageUp / PageDown / Home / End 是「扩展键」：keybd_event 必须带
    # KEYEVENTF_EXTENDEDKEY(0x0001)，否则 GetAsyncKeyState 读不到（run50 实测
    # vk=0x28 一直是 0x0000，而 0x41/0x09/0x70 是 0x8001）。
    [uint32]$down = if ($extended) { 0x0001 } else { 0 }
    [uint32]$up   = if ($extended) { 0x0003 } else { 0x0002 }
    [W32B]::keybd_event($vk, 0, $down, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 420
    [W32B]::keybd_event($vk, 0, $up, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 220
}
# Alt+Shift+F1：按住两个修饰键、点按 F1、再放开修饰键（平台层是 GetAsyncKeyState 边沿判定）
function Send-NavChord {
    [W32B]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero)   # Alt down
    [W32B]::keybd_event(0x10, 0, 0, [UIntPtr]::Zero)   # Shift down
    Start-Sleep -Milliseconds 260
    [W32B]::keybd_event(0x70, 0, 0, [UIntPtr]::Zero)   # F1 down
    Start-Sleep -Milliseconds 300
    [W32B]::keybd_event(0x70, 0, 2, [UIntPtr]::Zero)   # F1 up
    Start-Sleep -Milliseconds 160
    [W32B]::keybd_event(0x10, 0, 2, [UIntPtr]::Zero)
    [W32B]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 400
}
function Shot([IntPtr]$h, [string]$name) {
    try {
        $r = New-Object W32B+RECT
        [W32B]::GetWindowRect($h, [ref]$r) | Out-Null
        $w = $r.R - $r.L; $ht = $r.B - $r.T
        if ($w -le 0 -or $ht -le 0) { return }
        $bmp = New-Object System.Drawing.Bitmap $w, $ht
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        [W32B]::PrintWindow($h, $hdc, 2) | Out-Null
        $g.ReleaseHdc($hdc); $g.Dispose()
        $bmp.Save((Join-Path $OutDir "$Tag-$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "  截图 $Tag-$name.png"
    } catch { Write-Host "  截图失败：$($_.Exception.Message)" }
}

# ---------- 准备 ----------
$existing = Get-Process manosaba -ErrorAction SilentlyContinue
if ($existing) { Write-Host "已有实例，先退：$($existing.Id -join ',')"; $existing | Stop-Process -Force; Start-Sleep -Seconds 3 }
foreach ($junk in @('ManosabaProbe.dll','NaninovelReProbe.dll')) {
    $p = Join-Path $GameDir "BepInEx\plugins\$junk"
    if (Test-Path $p) { Move-Item $p (Join-Path $RepoDir "probe\plugin\$junk.bak") -Force }
}
$stash = Join-Path $GameDir 'BepInEx\plugins\_stash_a11y'
if (Test-Path $stash) { Move-Item $stash (Join-Path $RepoDir "probe\_stash_out\_stash-$Tag") -Force -ErrorAction SilentlyContinue }

New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (-not (Test-Path (Join-Path $PluginDir 'nvdaControllerClient.dll'))) {
    Copy-Item (Join-Path $RepoDir 'mod\nvda_dl\x64\nvdaControllerClient.dll') (Join-Path $PluginDir 'nvdaControllerClient.dll') -Force
}
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

$env:MANOSABA_A11Y_DIAG     = '1'
$env:MANOSABA_A11Y_LOCALE   = $Locale
$env:MANOSABA_A11Y_JUMP     = $Jump
$env:MANOSABA_A11Y_PROBE_UI = '1'
$env:MANOSABA_A11Y_KEYDIAG   = '1'   # 键盘链路诊断：确认方向键是否被 GetAsyncKeyState 读到
foreach ($e in 'MANOSABA_A11Y_TEST','MANOSABA_A11Y_TEST_PLAY','MANOSABA_A11Y_INJECT','MANOSABA_A11Y_TEST_NAV','MANOSABA_A11Y_NOPATCH') {
    Remove-Item "Env:$e" -ErrorAction SilentlyContinue
}

Write-Host "启动（$Tag，JUMP=$Jump）…"
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null
Start-Sleep -Seconds 26
$h = Get-GameWindow
Focus-Window $h
Shot $h 'title'

Write-Host '循环点「新游戏」(438,871)…'
$storySeen = $false
for ($i = 1; $i -le 20 -and -not $storySeen; $i++) {
    Focus-Window $h; Click-Client $h 438 871; Start-Sleep -Seconds 4
    $log = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
    if ($log | Where-Object { $_ -match 'JUMP 已进正片|Reader\.say' }) { $storySeen = $true; Write-Host "  第 $i 次点击后剧情开始" }
}

# 先让辩论跑几句（证明 JUMP 到了 Trial），再点左上角书本图标打开魔女手册
Start-Sleep -Seconds 8
for ($k = 0; $k -lt 6; $k++) { Focus-Window $h; Click-Client $h 960 540; Start-Sleep -Milliseconds 900 }
Shot $h 'trial'

Write-Host '点左上角书本图标 (84,62) 打开魔女手册…'
Focus-Window $h
Click-Client $h 84 62
Start-Sleep -Seconds 4
Shot $h 'book'

Write-Host '按 Alt+Shift+F1 进导航…'
Focus-Window $h
Send-NavChord
Start-Sleep -Seconds 3
Shot $h 'booknav1'
# 组内移动 + 换组：魔女手册每个页签各自成组（run60 首次实测 10 组），
# 用 PageDown 逐组走一遍，看每组念什么。
for ($k = 0; $k -lt 4; $k++) { Send-Key 0x28 $true }   # Down ×4（组内移动）
Start-Sleep -Seconds 1
Shot $h 'booknav2'
for ($k = 0; $k -lt 3; $k++) {
    Send-Key 0x22 $true                            # PageDown（换组）
    Start-Sleep -Milliseconds 700
    for ($j = 0; $j -lt 3; $j++) { Send-Key 0x28 $true }   # Down ×3
}
Start-Sleep -Seconds 2
Shot $h 'booknav3'

$deadline = (Get-Date).AddSeconds([Math]::Max(20, $WaitSeconds - 90))
while ((Get-Date) -lt $deadline) { Start-Sleep -Seconds 2 }

$text = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force

$say   = $text | Where-Object { $_ -match '\[朗读\]|Reader\.say|Reader\.voiced' }
$nav   = $text | Where-Object { $_ -match '\[UiNav\]|导航模式|导航开关键' }
$ui    = $text | Where-Object { $_ -match 'UIPROBE' }

$lines = @()
$lines += "# $Tag 魔女手册验证（JUMP=$Jump）"
$lines += "# 采样 $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')；朗读=$(($say).Count) 导航=$(($nav).Count) UIPROBE=$(($ui).Count)"
$lines += ''
$lines += '## 朗读（含导航播报）'
$lines += ($say | Select-Object -First 120)
$lines += ''
$lines += '## 导航'
$lines += ($nav | Select-Object -First 120)
$lines += ''
$lines += '## UIPROBE：魔女手册相关控件（含 resolved 命名结果）'
$lines += ($ui | Where-Object { $_ -match 'WitchBook|Page_Maps|Focus|Tab_|Book|Pagination' } | Select-Object -First 200)
$lines | Set-Content (Join-Path $OutDir "$Tag-summary.txt") -Encoding UTF8

Write-Host ''
Write-Host "朗读=$(($say).Count) 导航=$(($nav).Count)"
Write-Host "汇总：$OutDir\$Tag-summary.txt"
