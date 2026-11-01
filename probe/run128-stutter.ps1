# run128-stutter.ps1 —— CG / 立绘 / 过场出现时的卡顿取证（0.0.17 探查）
#
# 为什么要这个：平均帧率看不出「CG 一出来就顿一下」，必须数「长帧」。
# 插件侧每 10 秒打一行：
#   PERF 10s fps=.. frameMs=.. maxMs=.. slow>100=.. slow>200=.. gc0=+.. gc1=+.. gc2=+.. costs=..
# 本探针做两轮（关闭档 / 强力档），用同样的点击节奏推进同一段剧情，比较长帧数与 GC 次数。
param(
    [string]$Tag = 'run128',
    [string]$Mode = '关闭',
    [int]$Clicks = 42,
    [int]$WaitSeconds = 200,
    [switch]$StopAnim
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

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W128 {
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, System.Text.StringBuilder s, int n);
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    public static IntPtr FindGame(int pid) {
        IntPtr found = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            uint wp; GetWindowThreadProcessId(h, out wp);
            if (wp != (uint)pid) return true;
            var sb = new System.Text.StringBuilder(256); GetClassName(h, sb, 256);
            string cls = sb.ToString();
            if (cls == "ConsoleWindowClass" || cls == "IME" || cls == "MSCTFIME UI") return true;
            RECT r; GetClientRect(h, out r);
            if (r.R - r.L < 200) return true;
            found = h; return false;
        }, IntPtr.Zero);
        return found;
    }
}
"@

Get-Process -Name manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

# 档位：临时改 cfg（结束时还原）
$cfgBackup = $null
if (Test-Path $CfgPath) {
    $cfgBackup = [IO.File]::ReadAllText($CfgPath)
    $t = $cfgBackup -replace '(?m)^低性能模式档位\s*=.*$', ("低性能模式档位 = " + $Mode)
    if ($t -notmatch '低性能模式档位') { $t = $t.TrimEnd() + "`r`n低性能模式档位 = " + $Mode + "`r`n" }
    [IO.File]::WriteAllText($CfgPath, $t, (New-Object Text.UTF8Encoding($false)))
    "S=档位设为 $Mode"
}

$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
$env:MANOSABA_A11Y_PERFDIAG = '1'
if ($StopAnim) { $env:MANOSABA_A11Y_TEST_STOPANIM = '1'; "S=只停动画（对照实验）" } else { $env:MANOSABA_A11Y_TEST_STOPANIM = '' }
foreach ($n in @('MANOSABA_A11Y_TEST','MANOSABA_A11Y_JUMP','MANOSABA_A11Y_JUMP_LINE','MANOSABA_A11Y_TEST_NAV',
                 'MANOSABA_A11Y_TEST_REPEAT','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_EVDIAG',
                 'MANOSABA_A11Y_TEST_EVCMD','MANOSABA_A11Y_TEST_EVFORCE','MANOSABA_A11Y_TEST_EVPICK',
                 'MANOSABA_A11Y_TEST_EVYES','MANOSABA_A11Y_TEST_EVMODE','MANOSABA_A11Y_TEST_MAPCHOICE',
                 'MANOSABA_A11Y_TEST_MAPFLOOR','MANOSABA_A11Y_TEST_MAPPICK_KIND','MANOSABA_A11Y_MAPDIAG',
                 'MANOSABA_A11Y_TEST_ESC','MANOSABA_A11Y_TEST_KEYS','MANOSABA_A11Y_TEST_SCRIPTJUMP',
                 'MANOSABA_A11Y_TEST_CHOICE','MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_SELFTEST',
                 'MANOSABA_A11Y_PERFTEST','MANOSABA_A11Y_TEST_LOWPERF','MANOSABA_A11Y_TEST_EXTREME')) {
    Set-Item -Path ("env:" + $n) -Value '' -ErrorAction SilentlyContinue
}

$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
"S=启动 pid=$($proc.Id)"
Start-Sleep -Seconds 34
$hwnd = [W128]::FindGame($proc.Id)
"S=窗口 hwnd=$hwnd"
if ($hwnd -eq [IntPtr]::Zero) { if ($cfgBackup) { [IO.File]::WriteAllText($CfgPath, $cfgBackup, (New-Object Text.UTF8Encoding($false))) }; "E=没找到游戏窗口"; exit 3 }

function Log-Text {
    if (-not (Test-Path $LogPath)) { return '' }
    try {
        $fs = [IO.File]::Open($LogPath, [IO.FileMode]::Open, [IO.FileAccess]::Read,
                              ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8)
        $t = $sr.ReadToEnd(); $sr.Dispose(); $fs.Dispose(); return $t
    } catch { return '' }
}
function Focus-Game {
    [void][W128]::ShowWindow($hwnd, 9)
    [void][W128]::SetWindowPos($hwnd, [IntPtr](-1), 0, 0, 0, 0, 0x0013)
    [void][W128]::SetForegroundWindow($hwnd)
}
function Click-Rel([double]$rx, [double]$ry) {
    $r = New-Object W128+RECT
    if (-not [W128]::GetClientRect($hwnd, [ref]$r)) { return $false }
    $p = New-Object W128+POINT
    $p.X = [int]($rx * ($r.R - $r.L)); $p.Y = [int]($ry * ($r.B - $r.T))
    [void][W128]::ClientToScreen($hwnd, [ref]$p)
    [void][W128]::SetCursorPos($p.X, $p.Y)
    [W128]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 80
    [W128]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    return $true
}

# 进正片（读第 1 格存档）
$entered = $false
for ($i = 0; $i -lt 12; $i++) {
    Focus-Game
    [void](Click-Rel 0.0875 0.832)
    Start-Sleep -Seconds 3
    [void](Click-Rel 0.179 0.233)
    Start-Sleep -Seconds 6
    if ((Log-Text) -match 'Reader\.say\[') { $entered = $true; "S=进正片 第$($i+1)次"; break }
}
"S=进入正片=$entered"

# 推进剧情：每 2.5 秒点一次，覆盖 CG/立绘/过场出现的时段
for ($i = 0; $i -lt $Clicks; $i++) {
    Focus-Game
    [void](Click-Rel 0.5 0.86)
    Start-Sleep -Milliseconds 2500
}

Start-Sleep -Seconds 4
function Log-Matches([string]$Pattern) {
    $t = Log-Text
    if (-not $t) { return @() }
    return @(($t -split "`r?`n") | Select-String -Pattern $Pattern | ForEach-Object { $_.Line })
}

$perf = @(Log-Matches 'PERF 10s[^\r\n]*')
$lines = @()
$lines += "Tag=$Tag 档位=$Mode 只停动画=$StopAnim 时刻=$((Get-Date).ToString('HH:mm:ss')) 进入正片=$entered 点击数=$Clicks"
$lines += "---- PERF 10s（fps / 平均帧时 / 最长帧 / 长帧数 / GC） ----"
$lines += $perf
$lines += "---- 档位与低性能模式 ----"
$lines += @(Log-Matches 'LOWPERF 档位|PERF urp') 

# 汇总数字
$fps = @(); $maxMs = @(); $slow100 = 0; $slow200 = 0; $gcSum = 0
foreach ($l in $perf) {
    if ($l -match 'fps=([\d.]+)') { $fps += [double]$Matches[1] }
    if ($l -match 'maxMs=(\d+)') { $maxMs += [int]$Matches[1] }
    if ($l -match 'slow>100=(\d+)') { $slow100 += [int]$Matches[1] }
    if ($l -match 'slow>200=(\d+)') { $slow200 += [int]$Matches[1] }
    if ($l -match 'gc0=\+(\d+)') { $gcSum += [int]$Matches[1] }
}
$avgFps = if ($fps.Count -gt 0) { [math]::Round(($fps | Measure-Object -Average).Average, 1) } else { 0 }
$worst = if ($maxMs.Count -gt 0) { ($maxMs | Measure-Object -Maximum).Maximum } else { 0 }
$lines += "---- 汇总 ----"
$lines += "窗口数=$($fps.Count) 平均fps=$avgFps 最坏单帧=${worst}ms 长帧>100ms=$slow100 长帧>200ms=$slow200 GC0次数=$gcSum"

$out = Join-Path $OutDir "$Tag-summary.txt"
$lines | Set-Content -Path $out -Encoding UTF8
"汇总: $out"
"S=平均fps=$avgFps 最坏帧=${worst}ms 长帧>100=$slow100 长帧>200=$slow200 GC0=$gcSum"

Get-Process -Name manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
if ($cfgBackup) {
    [IO.File]::WriteAllText($CfgPath, $cfgBackup, (New-Object Text.UTF8Encoding($false)))
    "S=cfg 已还原"
}
