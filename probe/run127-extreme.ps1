# run127-extreme.ps1 —— 「极限档（画面全黑）」安全性取证（0.0.0.15）
#
# 要回答的问题：极限档把画面整个丢掉以后，**点击与朗读还在不在**。
# 为什么必须实机验：UGUI 的射线检测只认「已注册且未被 cull」的图元，
# 一旦用 Canvas/CanvasRenderer 去隐藏 UI，点击会一起失效（游戏软锁）。
# 我们的做法是把 UI 图元的颜色刷黑（不影响射线），所以这一轮就是来证明它真的不影响。
#
# 流程：读第 1 格存档进正片 → 等插件在 t=70s 切到极限档 → 在 70~150s 之间持续点击并统计
#       `Reader.say` 是否还在增长 → 150s 还原后再点几下确认没坏。
param(
    [int]$WaitSeconds = 210,
    [string]$Tag = 'run127'
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
public class W127 {
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
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
$env:MANOSABA_A11Y_TEST_EXTREME = '1'
foreach ($n in @('MANOSABA_A11Y_TEST','MANOSABA_A11Y_JUMP','MANOSABA_A11Y_JUMP_LINE','MANOSABA_A11Y_TEST_NAV',
                 'MANOSABA_A11Y_TEST_REPEAT','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_EVDIAG',
                 'MANOSABA_A11Y_TEST_EVCMD','MANOSABA_A11Y_TEST_EVFORCE','MANOSABA_A11Y_TEST_EVPICK',
                 'MANOSABA_A11Y_TEST_EVYES','MANOSABA_A11Y_TEST_EVMODE','MANOSABA_A11Y_TEST_MAPCHOICE',
                 'MANOSABA_A11Y_TEST_MAPFLOOR','MANOSABA_A11Y_TEST_MAPPICK_KIND','MANOSABA_A11Y_MAPDIAG',
                 'MANOSABA_A11Y_TEST_ESC','MANOSABA_A11Y_TEST_KEYS','MANOSABA_A11Y_TEST_SCRIPTJUMP',
                 'MANOSABA_A11Y_TEST_CHOICE','MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_SELFTEST',
                 'MANOSABA_A11Y_PERFTEST','MANOSABA_A11Y_TEST_LOWPERF')) {
    Set-Item -Path ("env:" + $n) -Value '' -ErrorAction SilentlyContinue
}

$proc = Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
"S=启动 pid=$($proc.Id)"
Start-Sleep -Seconds 34

$hwnd = [W127]::FindGame($proc.Id)
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
function Count-Say { $t = Log-Text; if (-not $t) { return 0 }; return ([regex]::Matches($t, 'Reader\.say\[')).Count }
function Focus-Game {
    [void][W127]::ShowWindow($hwnd, 9)
    [void][W127]::SetWindowPos($hwnd, [IntPtr](-1), 0, 0, 0, 0, 0x0013)
    [void][W127]::SetForegroundWindow($hwnd)
}
function Click-Rel([double]$rx, [double]$ry) {
    $r = New-Object W127+RECT
    if (-not [W127]::GetClientRect($hwnd, [ref]$r)) { return $false }
    $p = New-Object W127+POINT
    $p.X = [int]($rx * ($r.R - $r.L)); $p.Y = [int]($ry * ($r.B - $r.T))
    [void][W127]::ClientToScreen($hwnd, [ref]$p)
    [void][W127]::SetCursorPos($p.X, $p.Y)
    [W127]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 90
    [W127]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero)
    return $true
}

# ---- 进正片：点「读取」→ 第 1 格存档 ----
$entered = $false
for ($i = 0; $i -lt 12; $i++) {
    Focus-Game
    [void](Click-Rel 0.0875 0.832)
    Start-Sleep -Seconds 3
    [void](Click-Rel 0.179 0.233)
    Start-Sleep -Seconds 6
    if ((Count-Say) -gt 0) { $entered = $true; "S=进正片 第$($i+1)次"; break }
}
"S=进入正片=$entered"

$sayBefore = Count-Say
"S=极限档前 say=$sayBefore"

# ---- 等极限档 ----
$sw = [Diagnostics.Stopwatch]::StartNew()
while ($sw.Elapsed.TotalSeconds -lt 90) {
    $t = Log-Text
    if ($t -match 'TEST EXTREME: 切到极限档') { break }
    Start-Sleep -Milliseconds 500
}
$sayAtExtreme = Count-Say
"S=切到极限档 say=$sayAtExtreme"

# ---- 极限档期间持续点击，看 say 是否还在涨 ----
for ($i = 0; $i -lt 26; $i++) {
    Focus-Game
    [void](Click-Rel 0.5 0.5)
    Start-Sleep -Milliseconds 2200
}
$sayAfterExtreme = Count-Say
"S=极限档期间点击后 say=$sayAfterExtreme 增长=$($sayAfterExtreme - $sayAtExtreme)"

# ---- 等还原，再点几下 ----
$sw2 = [Diagnostics.Stopwatch]::StartNew()
while ($sw2.Elapsed.TotalSeconds -lt 90) {
    $t = Log-Text
    if ($t -match 'TEST EXTREME: 还原到关闭档') { break }
    Start-Sleep -Milliseconds 500
}
for ($i = 0; $i -lt 10; $i++) {
    Focus-Game
    [void](Click-Rel 0.5 0.5)
    Start-Sleep -Milliseconds 2200
}
$sayFinal = Count-Say
"S=还原后 say=$sayFinal 增长=$($sayFinal - $sayAfterExtreme)"

function Log-Matches([string]$Pattern) {
    $t = Log-Text
    if (-not $t) { return @() }
    return @(($t -split "`r?`n") | Select-String -Pattern $Pattern | ForEach-Object { $_.Line })
}

$lines = @()
$lines += "Tag=$Tag 时刻=$((Get-Date).ToString('HH:mm:ss'))"
$lines += "进入正片=$entered 极限档前say=$sayBefore 切到极限时say=$sayAtExtreme 极限档期间=$sayAfterExtreme 还原后=$sayFinal"
$lines += "极限档期间增长=$($sayAfterExtreme - $sayAtExtreme) 还原后增长=$($sayFinal - $sayAfterExtreme)"
$lines += "---- TEST EXTREME ----"
$lines += (Log-Matches 'TEST EXTREME[^\r\n]*|LOWPERF 极限[^\r\n]*|LOWPERF 档位[^\r\n]*')
$lines += "---- 朗读/叙述（末 8 条） ----"
$lines += @((Log-Matches 'Reader\.say\[|Reader\.skipped') | Select-Object -Last 8)
$lines += "---- 错误 ----"
$lines += @((Log-Matches 'Exception|Failed to patch|HarmonyException') | Select-Object -Last 8)

$ok1 = ($sayAfterExtreme - $sayAtExtreme) -gt 0
$ok2 = @($lines | Select-String -Pattern 'LOWPERF 极限: 相机=\d+ renderer=\d+').Count -ge 1
$ok3 = @($lines | Select-String -Pattern 'LOWPERF 极限: 还原 \d+ 个相机').Count -ge 1
$lines += "---- 结论 ----"
$lines += "极限档黑屏后点击仍能推进剧情=$ok1 极限档确实丢弃画面=$ok2 关闭后已还原=$ok3"

$out = Join-Path $OutDir "$Tag-summary.txt"
$lines | Set-Content -Path $out -Encoding UTF8
"汇总: $out"

Get-Process -Name manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
if ($ok1 -and $ok2 -and $ok3) { exit 0 } else { exit 2 }
