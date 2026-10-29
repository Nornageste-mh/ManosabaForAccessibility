# run32-choice.ps1 —— 选项链实机验证（真实点击进正片 + 诊断跳转到带 @choice 的剧本）
#
# 为什么这么做：第一个选项在 Act01_Chapter01_Adv05，前面隔着 1000+ 行，
# 靠连点要 20 分钟且不可靠；而「在标题画面直接 LoadAndPlay」只会推进脚本状态机、
# UI 还停在标题（run16 实机）。所以：先用真实点击进正片，再由插件跳到 Adv05，
# 让选项面板真的出现在屏幕上，然后验证：
#   1) Choices 是否扫到面板、是否播报（日志 `[a11y] CHOICE 播报`）
#   2) 数字键是否被 Win32 全局读取接到（日志 `[a11y] CHOICE 数字键 1`）
#      —— 用 keybd_event 发真实按键（GetAsyncKeyState 能读到，注入的事件队列读不到）
param(
    [int]$WaitSeconds = 170,
    [string]$Tag = 'run32',
    [string]$Jump = 'Act01_Chapter01/Act01_Chapter01_Adv05',
    [switch]$PressKey
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
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
$env:MANOSABA_A11Y_JUMP   = $Jump
Remove-Item Env:MANOSABA_A11Y_TEST -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_TEST_PLAY -ErrorAction SilentlyContinue
Remove-Item Env:MANOSABA_A11Y_INJECT -ErrorAction SilentlyContinue

Write-Host "启动（$Tag，JUMP=$Jump，PressKey=$PressKey）…"
$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Start-Sleep -Seconds 26

$h = Get-GameWindow
if ($h -eq [IntPtr]::Zero) { Write-Host '拿不到窗口句柄'; }
Focus-Window $h
Shot $h 'title'

# 标题画面出现时机不定（实测 20~40s），单次点击常常点空 —— 循环点到「真的进正片」为止。
# 判据用日志（插件在进正片时会写 JUMP/Reader.say），比再截一张图可靠。
Write-Host '循环点「新游戏」(438,871)，直到剧情开始…'
$storySeen = $false
for ($i = 1; $i -le 20 -and -not $storySeen; $i++) {
    Focus-Window $h
    Click-Client $h 438 871
    Start-Sleep -Seconds 4
    $log = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
    if ($log | Where-Object { $_ -match 'JUMP 已进正片|Reader\.say|path=Act' }) {
        $storySeen = $true
        Write-Host "  第 $i 次点击后剧情开始"
    }
}
if ($storySeen) { Start-Sleep -Seconds 6; Shot $h 'story' } else { Write-Host '  20 次点击仍没进剧情' }

$choiceSeen = $false
$pressed = $false
$deadline = (Get-Date).AddSeconds($WaitSeconds - 56)
$tick = 0
while ((Get-Date) -lt $deadline) {
    $tick++
    # 进正片后要靠点击一句句推进（本作没有 autoplay 连播），否则永远只停在第 1 句。
    if ($storySeen -and $tick % 2 -eq 1) { Focus-Window $h; Click-Client $h 960 540 }
    $log = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
    if (-not $choiceSeen -and ($log | Where-Object { $_ -match 'CHOICE 播报|CHOICE 面板|AddChoiceButton' })) {
        $choiceSeen = $true
        Write-Host '  看到选项面板相关日志'
        Shot $h 'choices'
        # 选项出现后**停止点击**：否则点击可能把选项点掉，就分不清是「数字键选中」还是「鼠标选中」。
        $storySeen = $false
        if ($PressKey -and -not $pressed) {
            Start-Sleep -Milliseconds 600
            Write-Host '  发数字键 1（keybd_event，全局读取能接到）'
            Send-Key 0x31
            Start-Sleep -Milliseconds 900
            Send-Key 0x31
            $pressed = $true
            Start-Sleep -Seconds 5
            Shot $h 'after-key1'
        }
    }
    Start-Sleep -Seconds 1
}

$text = if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() }
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force

$interesting = $text | Where-Object {
    $_ -match 'CHOICE|Reader\.say|加载播报|JUMP|legacyInput|\[a11y\] loaded|patchedMethods|NOPATCH|语音后端|Exception|error'
}
$lines = @()
$lines += "# $Tag 选项链验证（JUMP=$Jump，PressKey=$PressKey）"
$lines += "# 采样 $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')；say=$(($interesting | Where-Object { $_ -match 'Reader\.say' }).Count) choice=$(($interesting | Where-Object { $_ -match 'CHOICE' }).Count)"
$lines += ''
$lines += $interesting
$lines | Set-Content (Join-Path $OutDir "$Tag-summary.txt") -Encoding UTF8
$lines | Select-Object -First 80
