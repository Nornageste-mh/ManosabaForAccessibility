# run47-navkey.ps1 —— 验证「新的导航开关键 Alt+Shift+F1」和「F7 切换语音后端」
#
# 背景（用户 m02715 实机反馈）：标题画面出来之前按 Tab 有反应，标题出来之后 Tab 没反应
# —— 游戏自己的 UI 把 Tab 吃掉了。所以把主键换成组合键 Alt+Shift+F1（Tab 仍作备用键）。
# 本脚本用真实 Win32 按键（keybd_event）验证：
#   1) Alt+Shift+F1 能否进/出导航模式（日志出现「导航开关键按下: Alt+Shift+F1」+「导航模式」）
#   2) F7 能否切换语音后端（日志出现「语音后端切换 ->」）
param(
    [int]$WaitSeconds = 45,
    [string]$Tag = 'run47'
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
public class W32N {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool c);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@

function Focus-Window([IntPtr]$h) {
    [W32N]::ShowWindow($h, 9) | Out-Null
    [W32N]::SetForegroundWindow($h) | Out-Null
    $fg = [W32N]::GetForegroundWindow()
    if ($fg -ne $h) {
        $pidFg = 0; $tidFg = [W32N]::GetWindowThreadProcessId($fg, [ref]$pidFg)
        [W32N]::AttachThreadInput([W32N]::GetCurrentThreadId(), $tidFg, $true) | Out-Null
        [W32N]::SetForegroundWindow($h) | Out-Null
        [W32N]::AttachThreadInput([W32N]::GetCurrentThreadId(), $tidFg, $false) | Out-Null
    }
    Start-Sleep -Milliseconds 200
}

# 组合键：三个键同时按下 600ms 再依次抬起。必须按住够久 —— 本作实测 6 FPS（delta≈0.165s），
# 太短的组合键会整个落在两帧之间，Hotkey.Down 采样看不到。
function Send-Chord([byte[]]$vks) {
    foreach ($vk in $vks) { [W32N]::keybd_event($vk, 0, 0, [UIntPtr]::Zero); Start-Sleep -Milliseconds 40 }
    Start-Sleep -Milliseconds 600
    [array]::Reverse($vks)
    foreach ($vk in $vks) { [W32N]::keybd_event($vk, 0, 2, [UIntPtr]::Zero); Start-Sleep -Milliseconds 40 }
    Start-Sleep -Milliseconds 200
}

function Send-Key([byte]$vk, [int]$holdMs = 420) {
    [W32N]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $holdMs
    [W32N]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 250
}

function Shot([IntPtr]$h, [string]$name) {
    try {
        $r = New-Object W32N+RECT
        [W32N]::GetWindowRect($h, [ref]$r) | Out-Null
        $w = $r.R - $r.L; $ht = $r.B - $r.T
        if ($w -le 0 -or $ht -le 0) { return }
        $bmp = New-Object System.Drawing.Bitmap $w, $ht
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        [W32N]::PrintWindow($h, $hdc, 2) | Out-Null
        $g.ReleaseHdc($hdc); $g.Dispose()
        $bmp.Save((Join-Path $OutDir "$Tag-$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
    } catch { Write-Host "  截图失败：$($_.Exception.Message)" }
}

function Log-Text { if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() } }

# ---------- 准备 ----------
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 3
foreach ($junk in @('ManosabaProbe.dll','NaninovelReProbe.dll')) {
    $p = Join-Path $GameDir "BepInEx\plugins\$junk"
    if (Test-Path $p) { Move-Item $p (Join-Path $RepoDir "probe\plugin\$junk.bak") -Force }
}
$stash = Join-Path $GameDir 'BepInEx\plugins\_stash_a11y'
if (Test-Path $stash) { Move-Item $stash (Join-Path $RepoDir 'probe\_stash_out\_stash-run47') -Force -ErrorAction SilentlyContinue }

New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (-not (Test-Path (Join-Path $PluginDir 'nvdaControllerClient.dll'))) {
    Copy-Item (Join-Path $RepoDir 'mod\nvda_dl\x64\nvdaControllerClient.dll') (Join-Path $PluginDir 'nvdaControllerClient.dll') -Force
}
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

$env:MANOSABA_A11Y_DIAG   = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
Remove-Item Env:MANOSABA_A11Y_TEST -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_TEST_PLAY -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_TEST_NAV -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_JUMP -ErrorAction SilentlyContinue

Write-Host "启动（$Tag）…"
$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Start-Sleep -Seconds $WaitSeconds

$h = $proc.MainWindowHandle
if ($h -eq [IntPtr]::Zero) {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) { $h = $p.MainWindowHandle }
}
if ($h -eq [IntPtr]::Zero) { Write-Host '拿不到窗口句柄'; exit 1 }
Focus-Window $h
Shot $h 'title'

# 1) Alt+Shift+F1 进导航（最多试 3 次，标题出现时机不定）
Write-Host '发 Alt+Shift+F1（进导航）…'
for ($try = 1; $try -le 3; $try++) {
    Focus-Window $h
    Send-Chord @([byte]0x12, [byte]0x10, [byte]0x70)   # Menu, Shift, F1
    Start-Sleep -Seconds 3
    $t = Log-Text
    if ($t | Where-Object { $_ -match '导航开关键按下: Alt\+Shift\+F1|导航模式' }) { Write-Host "  第 $try 次命中"; break }
    Write-Host "  第 $try 次没命中，再试"
}
Start-Sleep -Seconds 2
Shot $h 'nav-on'

# 2) 方向键下 3 次，看是否逐项朗读
Write-Host '发方向键下 3 次…'
foreach ($i in 1..3) { Send-Key 0x28 300; Start-Sleep -Milliseconds 800 }
Start-Sleep -Seconds 2
Shot $h 'nav-moved'

# 3) F7 切换语音后端
Write-Host '发 F7（切换语音后端）…'
Send-Key 0x76 300
Start-Sleep -Seconds 2

# 4) Alt+Shift+F1 退出导航
Write-Host '再发 Alt+Shift+F1（退出导航）…'
Send-Chord @([byte]0x12, [byte]0x10, [byte]0x70)
Start-Sleep -Seconds 2
Shot $h 'nav-off'

$text = Log-Text
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force

$interesting = $text | Where-Object {
    $_ -match 'UiNav|导航|朗读|Repeat|Speech|语音后端|\[a11y\] loaded|patchedMethods|Exception'
}
$out = @()
$out += "# $Tag 导航开关键 Alt+Shift+F1 + F7 后端切换验证"
$out += "# 采样 $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')；log 总行数=$($text.Count)；命中 $(@($interesting).Count) 行"
$out += ''
$out += $interesting
$out | Set-Content (Join-Path $OutDir "$Tag-summary.txt") -Encoding UTF8
Write-Host "--- 汇总写 $OutDir\$Tag-summary.txt ---"
$out | Select-Object -First 80
