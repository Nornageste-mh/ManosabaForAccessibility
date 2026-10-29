# run28-bisect.ps1 —— 补丁二分定位：哪条 Harmony 补丁会让剧情卡在第一句
#
# 背景（2026-10-29 实机事实）：打了补丁 → 第一句念完就停；纯净模式（plugins 空）→ 人工点能正常推进。
# 且纯净日志里没有 `MethodAccessException: IAwaiter.get_IsCompleted`，带补丁才有。
# 所以用 MANOSABA_A11Y_NOPATCH 逐组关掉补丁，看「能不能推进 + 异常还在不在」。
#
# 判据：`Reader.say` 计数 > 1 且日志无 MethodAccessException = 该配置可用。
param(
    [string]$NoPatch = '',
    [string]$Tag = 'bisect',
    [int]$StartDelay = 30,
    [int]$Clicks = 8
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
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool c);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@
function GetHwnd {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) { return $p.MainWindowHandle } else { return [IntPtr]::Zero }
}
function FocusGame {
    $h = GetHwnd
    if ($h -eq [IntPtr]::Zero) { return $false }
    [W32B]::ShowWindow($h, 9) | Out-Null
    $fg = [W32B]::GetForegroundWindow()
    $fgPid = [uint32]0
    $tid = [W32B]::GetWindowThreadProcessId($fg, [ref]$fgPid)
    $my  = [W32B]::GetCurrentThreadId()
    $att = $false
    if ($tid -ne 0 -and $tid -ne $my) { $att = [W32B]::AttachThreadInput($my, $tid, $true) }
    [W32B]::SetForegroundWindow($h) | Out-Null
    if ($att) { [W32B]::AttachThreadInput($my, $tid, $false) | Out-Null }
    Start-Sleep -Milliseconds 180
    return ([W32B]::GetForegroundWindow() -eq $h)
}
function ClickClient([int]$cx, [int]$cy, [string]$label) {
    $h = GetHwnd
    if ($h -eq [IntPtr]::Zero) { return }
    $pt = New-Object W32B+POINT
    $pt.X = $cx; $pt.Y = $cy
    [W32B]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32B]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 90
    [W32B]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 140
    [W32B]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 160
}
function Shot([string]$name) {
    $h = GetHwnd
    if ($h -eq [IntPtr]::Zero) { return }
    $r = New-Object W32B+RECT
    [W32B]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $ht = $r.B - $r.T
    if ($w -le 0 -or $ht -le 0) { return }
    $bmp = New-Object System.Drawing.Bitmap($w, $ht)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [W32B]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $p = Join-Path $OutDir ("{0}-{1}.png" -f $Tag, $name)
    $bmp.Save($p, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}
function LogStats {
    $l = Get-Content $LogPath -Encoding UTF8 -ErrorAction SilentlyContinue
    if (-not $l) { $l = @() }
    $say = ($l | Select-String -Pattern 'Reader\.say\[').Count
    $exc = ($l | Select-String -Pattern 'MethodAccessException').Count
    return @{ say = $say; exc = $exc }
}

Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
Start-Sleep -Seconds 2
# 清掉任何 plugins 子目录里的残留（BepInEx 递归加载，之前就吃过亏）
Get-ChildItem (Join-Path $GameDir 'BepInEx\plugins') -Recurse -Filter 'ManosabaA11y.dll' -ErrorAction SilentlyContinue |
    Where-Object { $_.DirectoryName -ne $PluginDir } | ForEach-Object { Remove-Item $_.FullName -Force }
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

$env:MANOSABA_A11Y_DIAG   = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
Remove-Item Env:MANOSABA_A11Y_TEST -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_INJECT -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_TEST_PLAY -ErrorAction SilentlyContinue
if ($NoPatch) { $env:MANOSABA_A11Y_NOPATCH = $NoPatch } else { Remove-Item Env:MANOSABA_A11Y_NOPATCH -ErrorAction SilentlyContinue }

Write-Output ("=== {0} NOPATCH='{1}' ===" -f $Tag, $NoPatch)
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null

$t0 = Get-Date
Start-Sleep -Seconds $StartDelay
Shot 'title' | Out-Null

# ---- 进正片：点击标题的 New Game，失败就换坐标重试 ----
# 历史教训：固定 (330,770) 有时能进、有时点不中（窗口边框/焦点/6FPS 都有影响），
# 所以这里用「候选坐标 × 重试」而不是赌一次。判据是日志里出现 Reader.say。
$candidates = @(@(438,871), @(436,880), @(420,862), @(455,872), @(390,872), @(470,885))
$entered = $false
foreach ($c in $candidates) {
    $focused = FocusGame
    ClickClient $c[0] $c[1] ("NewGame@{0},{1}" -f $c[0], $c[1])
    Start-Sleep -Seconds 7
    $s = LogStats
    Write-Output ("  try ({0},{1}) focused={2} say={3} exc={4}" -f $c[0], $c[1], $focused, $s.say, $s.exc)
    if ($s.say -gt 0) { $entered = $true; break }
}
Shot 'entered' | Out-Null
Write-Output ("  entered={0}" -f $entered)
Start-Sleep -Seconds 6

# ---- 推进剧情：点屏幕中央，「第一句之后能不能继续」就是本脚本要找的答案 ----
for ($i = 1; $i -le $Clicks; $i++) {
    FocusGame | Out-Null
    ClickClient 960 540 ("adv{0}" -f $i)
    Start-Sleep -Milliseconds 1400
}
Shot 'final' | Out-Null
Start-Sleep -Seconds 2
$s = LogStats
$l = Get-Content $LogPath -Encoding UTF8 -ErrorAction SilentlyContinue
Write-Output ("RESULT {0}: say={1} exception={2} elapsed={3:N0}s" -f $Tag, $s.say, $s.exc, ((Get-Date) - $t0).TotalSeconds)
($l | Select-String -Pattern 'NOPATCH=|Reader\.say\[|Reader\.say\b' -ErrorAction SilentlyContinue |
    Select-Object -First 6 | ForEach-Object { "  " + $_.Line })
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
Start-Sleep -Seconds 1
Write-Output ("summary -> {0}\{1}-bisect.txt" -f $OutDir, $Tag)
