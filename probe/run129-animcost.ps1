# run129-animcost.ps1 —— 固定场景下量「Animator 每帧成本」（0.0.17 卡顿问题的决定性对照）
#
# 为什么再量一次：run128 的三轮是「边点边推进」，每轮停在不同剧情位置，帧率从 8 到 15 乱跳，
# 无法证明停动画有没有用。这里改成**固定场景 + 静止不动**：
#   读第 1 格存档 → 跳到 Act01_Chapter01/Act01_Chapter01_Adv05 第 11 行（有立绘/背景）→ 40 秒不点击
# 同一行、同一个画面，只有「停不停 Animator」这一个变量，才比得出来。
param(
    [string]$Tag = 'run129',
    [switch]$StopAnim,
    [string]$JumpLine = 'Act01_Chapter01/Act01_Chapter01_Adv05|11',
    [int]$IdleSeconds = 45,
    [string]$ScanCmd = ''
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
public class W129 {
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

$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
$env:MANOSABA_A11Y_PERFDIAG = '1'
$env:MANOSABA_A11Y_JUMP_LINE = $JumpLine
if ($ScanCmd) {
    $env:MANOSABA_A11Y_SCAN_CMD = $ScanCmd
    # 扫描是在 EVIDENCE dump 里做的，所以检索行号时必须同时开 EVDIAG
    $env:MANOSABA_A11Y_EVDIAG = '1'
    "S=SCAN_CMD=$ScanCmd (EVDIAG=1)"
} else {
    $env:MANOSABA_A11Y_SCAN_CMD = ''
    $env:MANOSABA_A11Y_EVDIAG = ''
}
if ($StopAnim) { $env:MANOSABA_A11Y_TEST_STOPANIM = '1'; "S=停动画=开" } else { $env:MANOSABA_A11Y_TEST_STOPANIM = ''; "S=停动画=关" }
foreach ($n in @('MANOSABA_A11Y_TEST','MANOSABA_A11Y_JUMP','MANOSABA_A11Y_TEST_NAV',
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
Start-Sleep -Seconds 34
$hwnd = [W129]::FindGame($proc.Id)
"S=窗口 hwnd=$hwnd"
if ($hwnd -eq [IntPtr]::Zero) { "E=没找到游戏窗口"; exit 3 }

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
    [void][W129]::ShowWindow($hwnd, 9)
    [void][W129]::SetWindowPos($hwnd, [IntPtr](-1), 0, 0, 0, 0, 0x0013)
    [void][W129]::SetForegroundWindow($hwnd)
}
function Click-Rel([double]$rx, [double]$ry) {
    $r = New-Object W129+RECT
    if (-not [W129]::GetClientRect($hwnd, [ref]$r)) { return $false }
    $p = New-Object W129+POINT
    $p.X = [int]($rx * ($r.R - $r.L)); $p.Y = [int]($ry * ($r.B - $r.T))
    [void][W129]::ClientToScreen($hwnd, [ref]$p)
    [void][W129]::SetCursorPos($p.X, $p.Y)
    [W129]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 80
    [W129]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    return $true
}

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
if (-not $entered) { Get-Process -Name manosaba -ErrorAction SilentlyContinue | Stop-Process -Force; exit 2 }

# 等跳行真的发生
$sw = [Diagnostics.Stopwatch]::StartNew()
$jumped = $false
while ($sw.Elapsed.TotalSeconds -lt 40) {
    if ((Log-Text) -match 'JUMP LoadAndPlayAtLine 已发出') { $jumped = $true; break }
    Start-Sleep -Milliseconds 400
}
"S=跳行已发出=$jumped"
Start-Sleep -Seconds 3

# 静止观察窗口：不点击，只读 PERF
Start-Sleep -Seconds $IdleSeconds
function Log-Matches([string]$Pattern) {
    $t = Log-Text
    if (-not $t) { return @() }
    return @(($t -split "`r?`n") | Select-String -Pattern $Pattern | ForEach-Object { $_.Line })
}
$perf = @((Log-Matches 'PERF 10s[^\r\n]*') | Select-Object -Last 4)
$lines = @()
$lines += "Tag=$Tag 停动画=$StopAnim 跳行=$JumpLine 时刻=$((Get-Date).ToString('HH:mm:ss'))"
$lines += "---- PERF 10s（末 4 个窗口；静止场景） ----"
$lines += $perf
$lines += "---- 跳行/场景 ----"
$lines += @(Log-Matches 'JUMP [^\r\n]*|SCRIPTJUMP[^\r\n]*')
$lines += @(Log-Matches 'SCANCMD:[^\r\n]*|CHOICEEV: 扫描完成[^\r\n]*')
$lines += "---- 停动画证据 ----"
$lines += @(Log-Matches 'LOWPERF 测试: 停用|STOPANIM[^\r\n]*')

$sums = @()
foreach ($l in $perf) {
    $fps = ([regex]::Match($l,'fps=([\d.]+)')).Groups[1].Value
    $fm  = ([regex]::Match($l,'frameMs=([\d.]+)')).Groups[1].Value
    $mx  = ([regex]::Match($l,'maxMs=(\d+)')).Groups[1].Value
    $s1  = ([regex]::Match($l,'slow>100=(\d+)')).Groups[1].Value
    $s2  = ([regex]::Match($l,'slow>200=(\d+)')).Groups[1].Value
    $sums += "fps=$fps frameMs=$fm maxMs=$mx slow>100=$s1 slow>200=$s2"
}
$lines += "---- 汇总 ----"
$lines += $sums

$out = Join-Path $OutDir "$Tag-summary.txt"
$lines | Set-Content -Path $out -Encoding UTF8
"汇总: $out"
$sums | ForEach-Object { "S=$_" }

Get-Process -Name manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
