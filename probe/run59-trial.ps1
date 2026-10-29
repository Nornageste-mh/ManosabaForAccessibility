# run59-trial.ps1 —— 魔女审判（审判/辩论）实机验证
#
# 目的（用户 2026-10-29：「"魔女审判"暂时还没有测试」）：
#   1) 审判剧本里的辩论台词（@printDebate）能不能被朗读
#   2) 审判画面里的 HUD（HP / 倒计时 / 跳过按钮）控件叫什么、能不能被命名
#   3) 证据出示（ChoiceEvidence）与异议链接（<link>）在运行时的表现
#
# 做法：真实点击进正片 → 插件 JUMP 到 Trial01（LoadAndPlay）→ 连点推进 → 定时截图 + UIPROBE 转储。
param(
    [int]$WaitSeconds = 300,
    [string]$Tag = 'run59',
    [string]$Jump = 'Act01_Chapter01/Act01_Chapter01_Trial01',
    [string]$Locale = 'zh-Hans',
    [int]$ShotEvery = 25
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
public class W32T {
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
    [W32T]::ShowWindow($h, 9) | Out-Null
    [W32T]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 150
}

function Click-Client([IntPtr]$h, [int]$x, [int]$y) {
    $pt = New-Object W32T+POINT; $pt.X = $x; $pt.Y = $y
    [W32T]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32T]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 120
    [W32T]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 150
    [W32T]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 130
}

function Send-Key([byte]$vk) {
    [W32T]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 420
    [W32T]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 220
}

function Shot([IntPtr]$h, [string]$name) {
    try {
        $r = New-Object W32T+RECT
        [W32T]::GetWindowRect($h, [ref]$r) | Out-Null
        $w = $r.R - $r.L; $ht = $r.B - $r.T
        if ($w -le 0 -or $ht -le 0) { return }
        $bmp = New-Object System.Drawing.Bitmap $w, $ht
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        [W32T]::PrintWindow($h, $hdc, 2) | Out-Null
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
foreach ($e in 'MANOSABA_A11Y_TEST','MANOSABA_A11Y_TEST_PLAY','MANOSABA_A11Y_INJECT','MANOSABA_A11Y_TEST_NAV','MANOSABA_A11Y_NOPATCH') {
    Remove-Item "Env:$e" -ErrorAction SilentlyContinue
}

Write-Host "启动（$Tag，JUMP=$Jump，PROBE_UI=1）…"
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null
Start-Sleep -Seconds 26

$h = Get-GameWindow
if ($h -eq [IntPtr]::Zero) { Write-Host '拿不到窗口句柄' }
Focus-Window $h
Shot $h 'title'

Write-Host '循环点「新游戏」(438,871)，直到剧情开始…'
$storySeen = $false
for ($i = 1; $i -le 20 -and -not $storySeen; $i++) {
    Focus-Window $h
    Click-Client $h 438 871
    Start-Sleep -Seconds 4
    $log = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
    if ($log | Where-Object { $_ -match 'JUMP 已进正片|Reader\.say|path=(Act|Trial)' }) {
        $storySeen = $true
        Write-Host "  第 $i 次点击后剧情开始"
    }
}

$deadline = (Get-Date).AddSeconds($WaitSeconds - 56)
$started  = Get-Date
$tick = 0
$shotN = 0
while ((Get-Date) -lt $deadline) {
    $tick++
    if ($tick % 2 -eq 1) { Focus-Window $h; Click-Client $h 960 540 }
    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    if ($elapsed -ge ($shotN + 1) * $ShotEvery) {
        $shotN++
        Shot $h ("t$elapsed")
    }
    Start-Sleep -Seconds 1
}

$text = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force

$interesting = $text | Where-Object {
    $_ -match 'CHOICE|Reader\.say|Reader\.voiced|加载播报|JUMP|patchedMethods|NOPATCH|语音后端|Exception|error|HOOK'
}
$ui = $text | Where-Object { $_ -match 'UIPROBE' }

$lines = @()
$lines += "# $Tag 魔女审判验证（JUMP=$Jump）"
$lines += "# 采样 $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')；say=$(($text | Where-Object { $_ -match 'Reader\.say' }).Count) voiced=$(($text | Where-Object { $_ -match 'Reader\.voiced' }).Count) choice=$(($text | Where-Object { $_ -match 'CHOICE' }).Count) debate=$(($text | Where-Object { $_ -match 'debate' }).Count)"
$lines += ''
$lines += '## 朗读/补丁日志'
$lines += ($interesting | Select-Object -First 200)
$lines += ''
$lines += '## UIPROBE（审判画面控件命名取证）'
$lines += ($ui | Select-Object -First 300)
$lines | Set-Content (Join-Path $OutDir "$Tag-summary.txt") -Encoding UTF8

Write-Host ''
Write-Host "say=$(($text | Where-Object { $_ -match 'Reader\.say' }).Count) voiced=$(($text | Where-Object { $_ -match 'Reader\.voiced' }).Count) choice=$(($text | Where-Object { $_ -match 'CHOICE' }).Count) debate=$(($text | Where-Object { $_ -match 'debate' }).Count)"
Write-Host "汇总：$OutDir\$Tag-summary.txt"
