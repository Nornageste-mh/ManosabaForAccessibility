# run41-nav.ps1 —— 标题菜单键盘导航验证（Tab 进导航、方向键移动、看是否播报）
#
# 为什么这么做：UiNav 是平台层通用实现，但本作标题菜单不是 Naninovel TitleMenu、
# 且只有 6 FPS + 只启用新 Input System（裸 Input 不可用），所以「Tab 能不能进导航、
# 方向键能不能读到按钮名字」必须实机确认。
param(
    [int]$WaitSeconds = 120,
    [string]$Tag = 'run41',
    [string]$Locale = 'zh-Hans',
    [string]$Jump = ''
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
public class W32C {
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

function Get-GameWindow {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $p) { return $null }
    if ($p.MainWindowHandle -ne [IntPtr]::Zero) { return $p.MainWindowHandle }
    return $p.MainWindowHandle
}

function Focus-Window([IntPtr]$h) {
    [W32C]::ShowWindow($h, 9) | Out-Null
    [W32C]::SetForegroundWindow($h) | Out-Null
    $fg = [W32C]::GetForegroundWindow()
    if ($fg -ne $h) {
        $pidFg = 0; $tidFg = [W32C]::GetWindowThreadProcessId($fg, [ref]$pidFg)
        $tidCur = [W32C]::GetCurrentThreadId()
        [W32C]::AttachThreadInput($tidCur, $tidFg, $true) | Out-Null
        [W32C]::SetForegroundWindow($h) | Out-Null
        [W32C]::AttachThreadInput($tidCur, $tidFg, $false) | Out-Null
    }
    Start-Sleep -Milliseconds 200
}

function Click-Client([IntPtr]$h, [int]$x, [int]$y) {
    $pt = New-Object W32C+POINT; $pt.X = $x; $pt.Y = $y
    [W32C]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32C]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 120
    [W32C]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 160
    [W32C]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 140
}

function Send-Key([byte]$vk) {
    # 真实按键：GetAsyncKeyState 能读到（我们的全局读取就是它）。
    # 必须按住 ≥400ms —— 本作实测只有 6 FPS（delta≈0.165s），
    # 90ms 的按键会整个落在两帧之间，采样时永远看不到「按住」。
    [W32C]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 420
    [W32C]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 250
}

function Shot([IntPtr]$h, [string]$name) {
    try {
        $r = New-Object W32C+RECT
        [W32C]::GetWindowRect($h, [ref]$r) | Out-Null
        $w = $r.R - $r.L; $ht = $r.B - $r.T
        if ($w -le 0 -or $ht -le 0) { return }
        $bmp = New-Object System.Drawing.Bitmap $w, $ht
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        [W32C]::PrintWindow($h, $hdc, 2) | Out-Null
        $g.ReleaseHdc($hdc); $g.Dispose()
        $path = Join-Path $OutDir "$Tag-$name.png"
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "  截图 $path"
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
if (Test-Path $stash) { Move-Item $stash (Join-Path $RepoDir 'probe\_stash_out\_stash-run32') -Force -ErrorAction SilentlyContinue }

New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (-not (Test-Path (Join-Path $PluginDir 'nvdaControllerClient.dll'))) {
    Copy-Item (Join-Path $RepoDir 'mod\nvda_dl\x64\nvdaControllerClient.dll') (Join-Path $PluginDir 'nvdaControllerClient.dll') -Force
}
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

$env:MANOSABA_A11Y_DIAG   = '1'
$env:MANOSABA_A11Y_LOCALE = $Locale
$env:MANOSABA_A11Y_JUMP   = $Jump
Remove-Item Env:MANOSABA_A11Y_TEST -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_TEST_PLAY -ErrorAction SilentlyContinue

Write-Host "启动（$Tag）…"
$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Start-Sleep -Seconds 45

$h = Get-GameWindow
if (-not $h) { Write-Host '拿不到窗口句柄'; exit 1 }
Focus-Window $h
Shot $h 'title'

# 标题出现时机不定（30~50s），且 UiNav 只有在标题真的出来之后才扫得到组；
# 所以 Tab 要发到「日志里出现扫描结果」为止（最多 4 次）。
Write-Host '发 Tab（进导航模式），直到 UiNav 真的扫到组…'
for ($try = 1; $try -le 4; $try++) {
    Focus-Window $h
    Send-Key 0x09
    Start-Sleep -Seconds 3
    $t = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
    if ($t | Where-Object { $_ -match '扫描到 \d 组|导航模式' }) { Write-Host "  第 $try 次 Tab 后扫到组"; break }
    Write-Host "  第 $try 次没扫到，再试"
}
Start-Sleep -Seconds 2
Shot $h 'nav-on'

Write-Host '发方向键下 4 次…'
foreach ($i in 1..4) { Send-Key 0x28; Start-Sleep -Milliseconds 900 }
Start-Sleep -Seconds 3
Shot $h 'nav-moved'

Write-Host '发 Tab（退出导航模式）…'
Send-Key 0x09
Start-Sleep -Seconds 2
Shot $h 'nav-off'

Start-Sleep -Seconds 3
$text = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force

$interesting = $text | Where-Object {
    $_ -match 'UiNav|导航|朗读|TEST NAV|Repeat|Speech|语音后端|\[a11y\] loaded|patchedMethods|NOPATCH|Exception'
}
$lines = @()
$lines += "# $Tag 标题菜单键盘导航验证"
$lines += "# 采样 $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')；log 总行数=$($text.Count)"
$lines += ''
$lines += $interesting
$lines | Set-Content (Join-Path $OutDir "$Tag-summary.txt") -Encoding UTF8
Write-Host "--- 过滤后 $(@($interesting).Count) 行 ---"
$lines | Select-Object -First 60