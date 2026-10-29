# run49-keydiag.ps1 —— 键盘链路诊断：平台层到底读不读得到方向键？
#
# 背景（run48 实测）：Alt+Shift+F1 能进导航、能播报 5 项，但随后发方向键没有任何播报。
# 本作只启用新 Input System，平台层的方向键读取要靠 GlobalKeys（GetAsyncKeyState）兜底，
# 所以必须先确认「按键期间 GlobalKeys.Held(DownArrow) 是不是 true」，再谈导航逻辑。
param(
    [int]$BootSeconds = 50,
    [string]$Tag = 'run49'
)
$ErrorActionPreference = 'Continue'
$GameDir   = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath   = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir   = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc    = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir    = Join-Path $RepoDir 'probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W32K {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool c);
}
"@

function Focus-Window([IntPtr]$h) {
    [W32K]::ShowWindow($h, 9) | Out-Null
    [W32K]::SetForegroundWindow($h) | Out-Null
    $fg = [W32K]::GetForegroundWindow()
    if ($fg -ne $h) {
        $pidFg = 0; $tidFg = [W32K]::GetWindowThreadProcessId($fg, [ref]$pidFg)
        [W32K]::AttachThreadInput([W32K]::GetCurrentThreadId(), $tidFg, $true) | Out-Null
        [W32K]::SetForegroundWindow($h) | Out-Null
        [W32K]::AttachThreadInput([W32K]::GetCurrentThreadId(), $tidFg, $false) | Out-Null
    }
    Start-Sleep -Milliseconds 150
}

function Hold-Key([byte]$vk, [int]$holdMs) {
    # ★ 方向键 / Home / End 等是「扩展键」，keybd_event 必须带 KEYEVENTF_EXTENDEDKEY(0x0001)，
    #   否则系统把它当小键盘键处理，GetAsyncKeyState(VK_DOWN) 读不到（实测 0x0000）。
    [W32K]::keybd_event($vk, 0, 0x1, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $holdMs
    [W32K]::keybd_event($vk, 0, 0x3, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 200
}

function Log-Text { if (Test-Path $LogPath) { Get-Content $LogPath -Encoding UTF8 } else { @() } }

# ---------- 准备 ----------
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 3
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

$env:MANOSABA_A11Y_DIAG    = '1'
$env:MANOSABA_A11Y_KEYDIAG = '1'
$env:MANOSABA_A11Y_LOCALE  = 'zh-Hans'
foreach ($v in 'MANOSABA_A11Y_TEST','MANOSABA_A11Y_TEST_PLAY','MANOSABA_A11Y_TEST_NAV','MANOSABA_A11Y_JUMP','MANOSABA_A11Y_INJECT') {
    Remove-Item "Env:$v" -ErrorAction SilentlyContinue
}

Write-Host "启动（$Tag）…"
$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Start-Sleep -Seconds $BootSeconds

$h = $proc.MainWindowHandle
if ($h -eq [IntPtr]::Zero) {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($p) { $h = $p.MainWindowHandle }
}
if ($h -eq [IntPtr]::Zero) { Write-Host '拿不到窗口句柄'; exit 1 }

# 1) 等标题稳定：直到日志出现标题画面的导航扫描证据（最多再等 60s）
Write-Host '等标题画面…'
for ($i = 0; $i -lt 30; $i++) {
    $t = Log-TEXT
    if ($t | Where-Object { $_ -match '扫描到 1 组：Wrapper\(5\)|patchedMethods' }) { break }
    Start-Sleep -Seconds 2
}
Start-Sleep -Seconds 2

# 2) 先看「不按键」时 KEYDIAG 是什么样
Focus-Window $h
Start-Sleep -Seconds 2

# 3) 进导航
Write-Host '发 Alt+Shift+F1…'
Focus-Window $h
[W32K]::keybd_event(0x12,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 60
[W32K]::keybd_event(0x10,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 60
[W32K]::keybd_event(0x70,0,0,[UIntPtr]::Zero); Start-Sleep -Milliseconds 700
[W32K]::keybd_event(0x70,0,2,[UIntPtr]::Zero); Start-Sleep -Milliseconds 60
[W32K]::keybd_event(0x10,0,2,[UIntPtr]::Zero); Start-Sleep -Milliseconds 60
[W32K]::keybd_event(0x12,0,2,[UIntPtr]::Zero)
Start-Sleep -Seconds 3

# 4) 按住下箭头 1.2 秒（KEYDIAG 每 0.5s 一行，按住的这段时间里必须看到 down=True）
Write-Host '按住下箭头 1.2s…'
Focus-Window $h
[W32K]::keybd_event(0x28,0,0x1,[UIntPtr]::Zero)
Start-Sleep -Milliseconds 1200
[W32K]::keybd_event(0x28,0,0x3,[UIntPtr]::Zero)
Start-Sleep -Seconds 2

Write-Host '按住下箭头 1.2s（第二次）…'
Focus-Window $h
[W32K]::keybd_event(0x28,0,0x1,[UIntPtr]::Zero)
Start-Sleep -Milliseconds 1200
[W32K]::keybd_event(0x28,0,0x3,[UIntPtr]::Zero)
Start-Sleep -Seconds 2

Write-Host '发 Home…'
Focus-Window $h
Hold-Key 0x24 600
Start-Sleep -Seconds 2

$text = Log-Text
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force

$keydiag = $text | Where-Object { $_ -match 'KEYDIAG' }
$other   = $text | Where-Object { $_ -match '导航开关键按下|导航模式|\[朗读\]|主键|UiNav\] 导航开关|已退出导航' }
$out = @()
$out += "# $Tag 键盘链路诊断（KEYDIAG）"
$out += "# 采样 $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')；log 总行数=$($text.Count)；KEYDIAG $(@($keydiag).Count) 行"
$out += ''
$out += '## KEYDIAG（每 0.5s 一行；按住下箭头期间应为 down=True）'
$out += $keydiag
$out += ''
$out += '## 导航相关'
$out += $other
$out | Set-Content (Join-Path $OutDir "$Tag-summary.txt") -Encoding UTF8
Write-Host "--- 汇总：$OutDir\$Tag-summary.txt ---"
$keydiag | Select-Object -First 12
'' 
$other | Select-Object -First 25
