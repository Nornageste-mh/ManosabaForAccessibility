# run86-book-assist.ps1 —— ScreenAssist 验收：存档槽 1 → 魔女手册（线索页/地图页）→ Esc 关闭
#
# 用户 2026-10-31 提示：「如果你想到达那个位置，直接读取存档槽第一页的存档。」
# 所以本脚本不再从「新游戏」开始，而是：标题 → 读取 → 第 1 格存档 → 手册图标。
#
# 关键：每一步都用「LOOP 直到日志出现目标」的方式，而不是固定等待/固定点击数
# （本机 6 FPS，固定次数经常点空）。
param(
    [string]$Tag = 'run86',
    [string]$Locale = 'zh-Hans'
)
$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath = Join-Path $GameDir 'BepInEx\LogOutput.log'
$OutDir  = 'D:\DSHWorkSpace\manosaba_a11y\probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W86 {
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@
function Focus-Game([IntPtr]$h) {
    [W86]::ShowWindow($h, 9) | Out-Null
    [W86]::SetWindowPos($h, [IntPtr](-1), 0, 0, 0, 0, 0x0013) | Out-Null
    [W86]::BringWindowToTop($h) | Out-Null
    [W86]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 180
}
function Click-Rel([IntPtr]$h, [double]$rx, [double]$ry) {
    $cr = New-Object W86+RECT; [W86]::GetClientRect($h, [ref]$cr) | Out-Null
    $cw = $cr.R - $cr.L; $ch = $cr.B - $cr.T
    if ($cw -le 0 -or $ch -le 0) { return }
    $pt = New-Object W86+POINT; $pt.X = [int]($cw * $rx); $pt.Y = [int]($ch * $ry)
    [W86]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W86]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 100
    [W86]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 140
    [W86]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 900
}
function Get-LogText {
    if (-not (Test-Path $LogPath)) { return '' }
    try {
        $fs = [IO.File]::Open($LogPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8)
        $t = $sr.ReadToEnd(); $sr.Dispose(); $fs.Dispose(); return $t
    } catch { return '' }
}
function Text-Has([string]$pattern) { return [regex]::IsMatch((Get-LogText), $pattern) }
function Shot([IntPtr]$h, [string]$name) {
    $r = New-Object W86+RECT; [W86]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $hh = $r.B - $r.T
    if ($w -le 0 -or $hh -le 0) { return }
    $bmp = New-Object System.Drawing.Bitmap $w, $hh
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size($w, $hh)))
    $g.Dispose()
    $bmp.Save((Join-Path $OutDir "$Tag-$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_LOCALE = $Locale
$env:MANOSABA_A11Y_TEST_ESC = ''
$env:MANOSABA_A11Y_TEST_REPEAT = ''
$env:MANOSABA_A11Y_TEST_NAV = ''
$env:MANOSABA_A11Y_JUMP = ''
Remove-Item $LogPath -ErrorAction SilentlyContinue
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
$proc = Start-Process (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Write-Host "[info] pid=$($proc.Id) 等 34s"
Start-Sleep -Seconds 34
$h = [IntPtr]::Zero
for ($i = 0; $i -lt 30; $i++) { $proc.Refresh(); if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { $h = $proc.MainWindowHandle; break }; Start-Sleep 1 }
if ($h -eq [IntPtr]::Zero) { Write-Host '[error] 没窗口'; exit 3 }
Write-Host "[info] hwnd=$h"

# 1) 标题 → 读取
$ok = $false
for ($i = 1; $i -le 6; $i++) {
    Focus-Game $h; Click-Rel $h 0.0875 0.832
    Start-Sleep 2
    if (Text-Has '读取') { $ok = $true; break }
    if (Text-Has 'nav.*读取存档') { $ok = $true; break }
}
Write-Host "[step1] 读取界面 ok=$ok"
Shot $h 'loadlist'

# 2) 第 1 格存档
$ok2 = $false
for ($i = 1; $i -le 6; $i++) {
    Focus-Game $h; Click-Rel $h 0.179 0.233
    Start-Sleep 4
    if (Text-Has 'Reader\.say\[|加载播报（') { $ok2 = $true; break }
}
Write-Host "[step2] 存档读取 ok=$ok2"
Shot $h 'story'

# 3) 手册图标（左上）→ 等到 ScreenAssist 播报
$ok3 = $false
for ($i = 1; $i -le 8; $i++) {
    Focus-Game $h; Click-Rel $h 0.057 0.055
    Start-Sleep 2
    if (Text-Has 'ScreenAssist: 播报') { $ok3 = $true; break }
}
Write-Host "[step3] 手册打开 ok=$ok3"
Shot $h 'book-clues'

# 4) 切到「地图」页签（右侧第 3 个页签）
$ok4 = $false
for ($i = 1; $i -le 6; $i++) {
    Focus-Game $h; Click-Rel $h 0.939 0.414
    Start-Sleep 2
    if (Text-Has 'ScreenAssist: 播报 魔女手册，地图页签') { $ok4 = $true; break }
}
Write-Host "[step4] 地图页 ok=$ok4"
Shot $h 'book-maps'

# 5) 关闭（TEST_ESC 钩子不可用，这里只用截图确认界面还在；Esc 实机由用户验证）
$announce = @([regex]::Matches((Get-LogText), 'ScreenAssist: 播报[^\r\n]*') | ForEach-Object { $_.Value })
$lines = @()
$lines += "Tag=$Tag $(Get-Date -Format 'HH:mm:ss')"
$lines += "step1 读取界面=$ok  step2 存档读取=$ok2  step3 手册打开=$ok3  step4 地图页=$ok4"
$lines += "---- ScreenAssist 播报（全部）----"
$lines += ($announce | Select-Object -Last 8)
$lines | Set-Content (Join-Path $OutDir "$Tag-summary.txt") -Encoding UTF8
Write-Host "[done] $(Join-Path $OutDir "$Tag-summary.txt")"
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
