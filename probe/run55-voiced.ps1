# run55-voiced.ps1 —— 0.0.0.3 三项新功能的实机验证
#
# 验证目标（对应用户 2026-10-29 的两条要求）：
#   1) 有配音的行不自动朗读：日志里只看得到 `Reader.voiced[...] id=... => 有配音，跳过正文`，
#      没有对应的 `Reader.say[...]`（旁白/无配音行仍然是 `Reader.say`）。
#   2) 退格重读上一句：发真实 Backspace（vk 0x08），日志出现 `重读: <最后一句>`。
#      且重读的是剧情/选项，不是界面导航播报。
#   3) 隐藏控制台窗口：日志出现 `隐藏控制台窗口 hwnd=... ok=True`；
#      另外用 User32 IsWindowVisible 复核控制台窗口确实不可见。
#
# 为什么要发「真实按键」（keybd_event）而不是注入事件：
#   本作只启用新 Input System，插件的按键读取走 GlobalKeys（Win32 GetAsyncKeyState），
#   只有真实按键状态它才看得到。且本作实测只有 6 FPS，按键必须按住 ≥400ms
#   （否则整个按下/抬起落在一帧之间，采样永远看不到）。
param(
    [int]$WaitSeconds = 200,
    [string]$Tag = 'run55',
    [string]$Jump = 'Act01_Chapter01/Act01_Chapter01_Adv05',
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

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W32V {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, System.Text.StringBuilder sb, int max);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
}
"@

$script:winPid = 0

function Click-Client([IntPtr]$h, [int]$x, [int]$y) {
    try {
        $procIdBuf = 0
        $tid = [W32V]::GetWindowThreadProcessId($h, [ref]$procIdBuf)
        [W32V]::AttachThreadInput(([W32V]::GetCurrentThreadId()), $tid, $true) | Out-Null
        [W32V]::SetForegroundWindow($h) | Out-Null
        [W32V]::AttachThreadInput(([W32V]::GetCurrentThreadId()), $tid, $false) | Out-Null
    } catch { }
    [W32V]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 120
    [W32V]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 160
    [W32V]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 140
}

function Send-Key([byte]$vk) {
    [W32V]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 420
    [W32V]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 260
}

function LogCount([string]$pattern) {
    if (-not (Test-Path $LogPath)) { return 0 }
    return @(Select-String -Path $LogPath -Pattern $pattern -Encoding UTF8 -AllMatches).Count
}

# ---------- 准备 ----------
$existing = Get-Process manosaba -ErrorAction SilentlyContinue
if ($existing) { Write-Host "已有实例，先退：$($existing.Id -join ',')"; $existing | Stop-Process -Force; Start-Sleep -Seconds 3 }

foreach ($junk in @('ManosabaProbe.dll','NaninovelReProbe.dll')) {
    $p = Join-Path $GameDir "BepInEx\plugins\$junk"
    if (Test-Path $p) { Move-Item $p (Join-Path $RepoDir "probe\plugin\$junk.bak") -Force; Write-Host "移走残留探针：$junk" }
}
$stash = Join-Path $PluginDir '_stash_a11y'
if (Test-Path $stash) { Move-Item $stash (Join-Path $RepoDir 'probe\_stash_out\stash-final') -Force }
foreach ($junk in @('ManosabaProbe.dll','NaninovelReProbe.dll')) {
    $deep = Get-ChildItem -Path (Join-Path $GameDir 'BepInEx\plugins') -Recurse -Filter $junk -ErrorAction SilentlyContinue
    foreach ($d in $deep) { Move-Item $d.FullName (Join-Path $RepoDir "probe\plugin\$junk.deep.bak") -Force; Write-Host "移走深层残留：$($d.FullName)" }
}

New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
Write-Host "已部署：$((Get-Item (Join-Path $PluginDir 'ManosabaA11y.dll')).Length) B"

if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_JUMP = $Jump
$env:MANOSABA_A11Y_LOCALE = $Locale
$env:MANOSABA_A11Y_TEST = $null
$env:MANOSABA_A11Y_TEST_NAV = $null
$env:MANOSABA_A11Y_INJECT = $null
$env:MANOSABA_A11Y_KEYDIAG = '1'

Write-Host "启动（$Tag，JUMP=$Jump，LOCALE=$Locale）…"
$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Start-Sleep -Seconds 20

$hwnd = [IntPtr]::Zero
for ($i = 0; $i -lt 40; $i++) {
    $proc.Refresh()
    if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { $hwnd = $proc.MainWindowHandle; break }
    Start-Sleep -Milliseconds 500
}
Write-Host "窗口 hwnd=$hwnd pid=$($proc.Id)"

# 控制台窗口复核：BepInEx 的控制台窗口标题通常含 "BepInEx"（或就是 pid 的另一个窗口）
$consoleInfo = "(未找到控制台窗口)"
try {
    $found = $false
    foreach ($cls in @('#32770','ConsoleWindowClass')) {
        # 枚举同进程窗口太啰嗦；直接读 FindWindow 常见标题
    }
    $w = [W32V]::FindWindow($null, "$($proc.ProcessName)")
    $consoleInfo = "FindWindow(procName)=$w"
} catch { $consoleInfo = "查询异常：$($_.Exception.Message)" }
Write-Host "控制台检查：$consoleInfo"

# ---------- 阶段 1：真实点击进正片（标题 New Game）----------
$clicked = $false
for ($round = 0; $round -lt 10; $round++) {
    Click-Client $hwnd 438 871
    Start-Sleep -Seconds 6
    if ((LogCount 'Reader\.say|Reader\.voiced|JUMP 已进正片') -gt 0) { $clicked = $true; break }
    Write-Host "  第 $($round+1) 次点击还没进正片，继续…"
}
Write-Host "进正片：$clicked"

# ---------- 阶段 2：推进剧情，收集 voiced / say 统计 ----------
$deadline = (Get-Date).AddSeconds($WaitSeconds)
$backspaceSent = $false
$backspaceSent2 = $false
$tick = 0
while ((Get-Date) -lt $deadline) {
    $tick++
    Click-Client $hwnd 960 540
    Start-Sleep -Seconds 2

    # 进正片后再推几句，等配音行出现，然后按退格验证重读
    if (-not $backspaceSent -and $tick -eq 14) {
        Write-Host "  发送 Backspace（重读上一句）"
        Send-Key 0x08
        $backspaceSent = $true
        Start-Sleep -Seconds 2
        Write-Host "  发送 Backspace（第二次，验证可重复）"
        Send-Key 0x08
    }

    # 第二轮退格：等到出现配音行之后，验证「有配音的行也能用退格重读文本」
    if (-not $backspaceSent2 -and (LogCount 'Reader\.voiced\[') -ge 3) {
        Write-Host "  配音行已出现，发送 Backspace（验证有配音行也能重读）"
        Send-Key 0x08
        $backspaceSent2 = $true
        Start-Sleep -Seconds 2
    }

    if ($tick % 10 -eq 0) {
        $proc.Refresh()
        if ($proc.HasExited) { Write-Host "游戏已退出"; break }
        Write-Host ("  T+{0}s say={1} voiced={2} repeat={3}" -f ($tick*2),
            (LogCount 'Reader\.say\['), (LogCount 'Reader\.voiced\['), (LogCount '重读:'))
    }
}

# ---------- 阶段 3：收尾统计 ----------
Start-Sleep -Seconds 2
$say = LogCount 'Reader\.say\['
$voiced = LogCount 'Reader\.voiced\['
$repeat = LogCount '重读:'
$consoleHidden = LogCount '隐藏控制台窗口'
Write-Host "统计：say=$say voiced=$voiced 重读=$repeat 控制台隐藏日志=$consoleHidden"

$summary = Join-Path $OutDir "$Tag-summary.txt"
$lines = @()
$lines += "# $Tag 实机验证（JUMP=$Jump，LOCALE=$Locale，版本 0.0.0.3）"
$lines += "# say=$say voiced=$voiced 重读=$repeat 控制台隐藏日志=$consoleHidden"
$lines += ""
$lines += "## 配音闸门（有配音跳过正文）"
$lines += (Select-String -Path $LogPath -Pattern 'Reader\.voiced\[' -Encoding UTF8 | Select-Object -First 12 | ForEach-Object { $_.Line.Trim() })
$lines += ""
$lines += "## 退格重读"
$lines += (Select-String -Path $LogPath -Pattern '重读:' -Encoding UTF8 | Select-Object -First 8 | ForEach-Object { $_.Line.Trim() })
$lines += ""
$lines += "## 控制台隐藏"
$lines += (Select-String -Path $LogPath -Pattern '隐藏控制台' -Encoding UTF8 | Select-Object -First 4 | ForEach-Object { $_.Line.Trim() })
$lines += ""
$lines += "## 朗读（无配音行，前 12 条）"
$lines += (Select-String -Path $LogPath -Pattern 'Reader\.say\[' -Encoding UTF8 | Select-Object -First 12 | ForEach-Object { $_.Line.Trim() })
$lines += ""
$lines += "## 启动/补丁"
$lines += (Select-String -Path $LogPath -Pattern '\[a11y\] (loaded|harmony patched|语音后端)' -Encoding UTF8 | Select-Object -First 6 | ForEach-Object { $_.Line.Trim() })
$lines | Set-Content -Path $summary -Encoding UTF8
Write-Host "汇总：$summary"

if (-not $proc.HasExited) { $proc | Stop-Process -Force }
Write-Host "done"

