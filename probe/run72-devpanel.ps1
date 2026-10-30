# run72-devpanel.ps1 —— 验证「游戏内置开发者面板」开关（Ctrl+Alt+Shift+F3）
#
# 背景（用户 2026-10-30 的要求）：
#   用户听说游戏自带开发者面板，用 Ctrl+Alt+Shift+F3 打开；希望补丁能接通它，
#   测试完把配置「允许激活开发者选项」默认改为关闭。
#
# 实现：DevPanel.cs 读 `GigaCreation.Tools.Debugging.Core.DebugManager.IsDebugMode`
#   （UniRx.ReactiveProperty<bool>），按组合键时翻转它；面板由游戏自己的
#   `DebugPanel` 订阅该属性显示/隐藏。
#
# 本脚本要回答：
#   A) 组合键能不能被补丁读到（日志 `DevPanel: 组合键触发`）
#   B) IsDebugMode 翻得动吗（日志 `DevPanel: IsDebugMode -> true/false`）
#   C) 面板真的出现吗（截图 + 日志）
#   D) 现有 cfg 有没有「允许激活开发者选项」项
param(
    [int]$WaitSeconds = 120,
    [string]$Tag = 'run72'
)
$ErrorActionPreference = 'Continue'
$GameDir   = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath   = Join-Path $GameDir 'BepInEx\LogOutput.log'
$CfgPath   = Join-Path $GameDir 'BepInEx\config\manosaba.a11y.reader.cfg'
$RepoDir   = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc    = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir    = Join-Path $RepoDir 'probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W32D {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, System.Text.StringBuilder sb, int max);
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }

    // 返回该进程里第一个「不是控制台」的可见顶层窗口（Unity 主窗口）。
    public static IntPtr FindGameWindow(int targetPid) {
        IntPtr result = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            uint pid;
            GetWindowThreadProcessId(h, out pid);
            if (pid != (uint)targetPid) return true;
            var sb = new System.Text.StringBuilder(256);
            GetClassName(h, sb, 256);
            if (sb.ToString() == "ConsoleWindowClass") return true;
            result = h;
            return false;
        }, IntPtr.Zero);
        return result;
    }
}
"@

# 按进程找 Unity 主窗口：控制台窗口也是同进程的顶层窗口，必须排除 ConsoleWindowClass。
function Find-UnityWindow([int]$targetPid) {
    return [W32D]::FindGameWindow($targetPid)
}

function Focus-Game([IntPtr]$h) {
    try {
        $pidBuf = 0
        $tid = [W32D]::GetWindowThreadProcessId($h, [ref]$pidBuf)
        [W32D]::AttachThreadInput(([W32D]::GetCurrentThreadId()), $tid, $true) | Out-Null
        [W32D]::SetForegroundWindow($h) | Out-Null
        [W32D]::AttachThreadInput(([W32D]::GetCurrentThreadId()), $tid, $false) | Out-Null
    } catch { }
    Start-Sleep -Milliseconds 150
}

function Click-Client([IntPtr]$h, [int]$x, [int]$y) {
    Focus-Game $h
    [W32D]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 120
    [W32D]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 160
    [W32D]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 140
}

# Ctrl+Alt+Shift+F3（按住 F3 320ms，保证 6 FPS 下至少被采样到一帧）
function Send-DevChord([IntPtr]$h) {
    Focus-Game $h
    [W32D]::keybd_event(0x11, 0, 0, [UIntPtr]::Zero)   # Ctrl down
    [W32D]::keybd_event(0x10, 0, 0, [UIntPtr]::Zero)   # Shift down
    [W32D]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero)   # Alt down
    Start-Sleep -Milliseconds 220
    [W32D]::keybd_event(0x72, 0, 0, [UIntPtr]::Zero)   # F3 down
    Start-Sleep -Milliseconds 320
    [W32D]::keybd_event(0x72, 0, 2, [UIntPtr]::Zero)   # F3 up
    Start-Sleep -Milliseconds 160
    [W32D]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)   # Alt up
    [W32D]::keybd_event(0x10, 0, 2, [UIntPtr]::Zero)   # Shift up
    [W32D]::keybd_event(0x11, 0, 2, [UIntPtr]::Zero)   # Ctrl up
    Start-Sleep -Milliseconds 500
}

function Shot([IntPtr]$h, [string]$name) {
    try {
        $r = New-Object W32D+RECT
        [W32D]::GetWindowRect($h, [ref]$r) | Out-Null
        $w = $r.R - $r.L; $ht = $r.B - $r.T
        if ($w -le 0 -or $ht -le 0) { return }
        $bmp = New-Object System.Drawing.Bitmap $w, $ht
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        [W32D]::PrintWindow($h, $hdc, 2) | Out-Null
        $g.ReleaseHdc($hdc); $g.Dispose()
        $bmp.Save((Join-Path $OutDir "$Tag-$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        Write-Host "  截图 $Tag-$name.png"
    } catch { Write-Host "  截图失败：$($_.Exception.Message)" }
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
    if (Test-Path $p) { Move-Item $p (Join-Path $RepoDir "probe\plugin\$junk.bak") -Force }
}
foreach ($junk in @('ManosabaProbe.dll','NaninovelReProbe.dll')) {
    $deep = Get-ChildItem -Path (Join-Path $GameDir 'BepInEx\plugins') -Recurse -Filter $junk -ErrorAction SilentlyContinue
    foreach ($d in $deep) { Move-Item $d.FullName (Join-Path $RepoDir "probe\plugin\$junk.deep.bak") -Force }
}

New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
Write-Host "已部署：$((Get-Item (Join-Path $PluginDir 'ManosabaA11y.dll')).Length) B"

if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

# 测试期间确保配置允许（正式发布默认 false，需要的话脚本里临时开）
if (Test-Path $CfgPath) {
    $cfg = Get-Content $CfgPath -Raw -Encoding UTF8
    if ($cfg -match '允许激活开发者选项') {
        $cfg = [regex]::Replace($cfg, '允许激活开发者选项\s*=\s*\w+', '允许激活开发者选项 = true')
        Write-Host "cfg 已有该项，临时置 true"
    } else {
        $cfg = $cfg -replace '(?m)^\[其它\]\s*$', "[其它]`n允许激活开发者选项 = true"
        Write-Host "cfg 缺该项，追加 允许激活开发者选项 = true"
    }
    [System.IO.File]::WriteAllText($CfgPath, $cfg, (New-Object System.Text.UTF8Encoding($false)))
}

$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_TEST = $null
$env:MANOSABA_A11Y_TEST_NAV = $null
$env:MANOSABA_A11Y_INJECT = $null
$env:MANOSABA_A11Y_JUMP = $null
$env:MANOSABA_A11Y_KEYDIAG = '1'
$env:MANOSABA_A11Y_DEBUGUI = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'

Write-Host "启动（$Tag，DIAG 模式，不带 JUMP/TEST）…"
$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Start-Sleep -Seconds 22

$hwnd = [IntPtr]::Zero
# ★ 必须按进程枚举顶层窗口再排除控制台：$proc.MainWindowHandle 在本作常常先返回
#   BepInEx 控制台的 hwnd，截图/点击/按键就全打到控制台上（run72 首轮踩过）。
for ($i = 0; $i -lt 80; $i++) {
    $w = Find-UnityWindow $proc.Id
    if ($w -ne $null -and $w -ne [IntPtr]::Zero) { $hwnd = $w; break }
    Start-Sleep -Milliseconds 500
}
Write-Host "窗口 hwnd=$hwnd pid=$($proc.Id)"

# 等引擎/补丁就绪（日志出现 `[a11y] loaded` 与 `全局键:`）
for ($i = 0; $i -lt 60; $i++) {
    if ((LogCount '\[a11y\] loaded') -gt 0) { break }
    Start-Sleep -Seconds 1
}
Write-Host "补丁就绪：loaded=$((LogCount '\[a11y\] loaded'))"

Focus-Game $hwnd
Shot $hwnd 'title-before'
Write-Host "发送组合键（第 1 次，标题画面）"
Send-DevChord $hwnd
Start-Sleep -Seconds 2
Shot $hwnd 'title-after-chord1'
Write-Host "发送组合键（第 2 次，标题画面）"
Send-DevChord $hwnd
Start-Sleep -Seconds 2
Shot $hwnd 'title-after-chord2'

# 进正片再试一次（面板可能在游戏画面里才有内容）
$clicked = $false
for ($round = 0; $round -lt 8; $round++) {
    Click-Client $hwnd 438 871
    Start-Sleep -Seconds 6
    if ((LogCount 'Reader\.say') -gt 0) { $clicked = $true; break }
}
Write-Host "进正片：$clicked"
Shot $hwnd 'story-before-chord'
Write-Host "发送组合键（第 3 次，剧情画面）"
Send-DevChord $hwnd
Start-Sleep -Seconds 3
Shot $hwnd 'story-after-chord'
Write-Host "发送组合键（第 4 次，剧情画面，应收起面板）"
Send-DevChord $hwnd
Start-Sleep -Seconds 2
Shot $hwnd 'story-after-chord2'

# ---------- 统计 ----------
$chord = LogCount 'DevPanel: 组合键触发'
$toggle = LogCount 'DevPanel: IsDebugMode'
$say = LogCount 'Reader\.say'
Write-Host "统计：组合键=$chord 翻转=$toggle say=$say"

$summary = Join-Path $OutDir "$Tag-summary.txt"
$lines = @()
$lines += "# $Tag 开发者面板验证（Ctrl+Alt+Shift+F3）"
$lines += "# 组合键命中=$chord  IsDebugMode 翻转=$toggle  say=$say"
$lines += ""
$lines += "## DevPanel 日志"
$lines += (Select-String -Path $LogPath -Pattern 'DevPanel' -Encoding UTF8 | ForEach-Object { $_.Line.Trim() })
$lines += ""
$lines += "## 启动/补丁/输入后端"
$lines += (Select-String -Path $LogPath -Pattern '\[a11y\] (loaded|harmony patched|语音后端|全局键|配置)' -Encoding UTF8 | Select-Object -First 12 | ForEach-Object { $_.Line.Trim() })
$lines += ""
$lines += "## KEYDIAG（前 6 行）"
$lines += (Select-String -Path $LogPath -Pattern 'KEYDIAG' -Encoding UTF8 | Select-Object -First 6 | ForEach-Object { $_.Line.Trim() })
$lines | Set-Content -Path $summary -Encoding UTF8
Write-Host "汇总：$summary"

if (-not $proc.HasExited) { $proc | Stop-Process -Force }
Write-Host "done"
