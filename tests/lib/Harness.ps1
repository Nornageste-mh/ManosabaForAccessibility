# Harness.ps1 —— 自动化测试套件的公共设施（启动游戏、驱动输入、读日志、断言、出报告）
#
# 约定：
#   * 每个用例都是独立进程生命周期（启动 → 操作 → 断言 → 杀进程），失败不污染下一个用例。
#   * 断言只依赖「日志里必须出现的证据行」+ 截图，不依赖像素比对（6 FPS 下截图不稳）。
#   * 所有日志都是带约定前缀的结构化行：`[a11y] Reader.say[reveal] …`、`CHOICE 播报 …`。
#     新增功能时顺手加一行日志，就等于给测试留了观测点 —— 这是本项目「可测性」的做法。
#
# 用法：由 tests\Suite.ps1 dot-source，不单独运行。

$ErrorActionPreference = 'Continue'

$script:GameDir   = 'D:\Steam\steamapps\common\manosaba_game'
# Harness.ps1 位于 tests\lib\ → 上三级才是仓库根（lib → tests → 仓库根）
$script:RepoDir   = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSCommandPath))
$script:LogPath   = Join-Path $GameDir 'BepInEx\LogOutput.log'
$script:CfgPath   = Join-Path $GameDir 'BepInEx\config\manosaba.a11y.reader.cfg'
$script:DllSrc    = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$script:PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$script:OutDir    = Join-Path $RepoDir 'tests\reports'
$script:ShotDir   = Join-Path $OutDir 'shots'

New-Item -ItemType Directory -Force -Path $OutDir, $ShotDir | Out-Null
Add-Type -AssemblyName System.Drawing

if (-not ('TestHarness.W32' -as [type])) {
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
namespace TestHarness {
public class W32 {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }

    // 该进程里第一个「不是控制台」的顶层窗口 = Unity 主窗口。
    // 为什么需要：$proc.MainWindowHandle 会拿到 BepInEx 控制台窗口（同进程），
    // 光标/按键全打进控制台，用例全部假失败（run72 踩过）。
    public static IntPtr FindGameWindow(int targetPid) {
        IntPtr result = IntPtr.Zero;
        EnumWindows(delegate(IntPtr h, IntPtr p) {
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (pid != (uint)targetPid) return true;
            var sb = new StringBuilder(256); GetClassName(h, sb, 256);
            if (sb.ToString() == "ConsoleWindowClass") return true;
            result = h; return false;
        }, IntPtr.Zero);
        return result;
    }
}
}
"@
}

function Get-Paths { return @{ GameDir = $script:GameDir; Log = $script:LogPath; Cfg = $script:CfgPath
                               DllSrc = $script:DllSrc; Plugins = $script:PluginDir
                               Repo = $script:RepoDir; Out = $script:OutDir; Shots = $script:ShotDir } }

# ---------------------------------------------------------------- 环境准备
function Invoke-Deploy {
    if (-not (Test-Path $script:DllSrc)) { throw "找不到构建产物：$script:DllSrc（先跑 mod\build.ps1）" }
    New-Item -ItemType Directory -Force -Path $script:PluginDir | Out-Null
    Copy-Item $script:DllSrc (Join-Path $script:PluginDir 'ManosabaA11y.dll') -Force

    # 干扰项清零：探针 dll 会自己 patch/播报，混进来会让断言假绿假红。
    $probe = Join-Path $script:GameDir 'BepInEx\plugins\ManosabaProbe.dll'
    if (Test-Path $probe) { Remove-Item $probe -Force -ErrorAction SilentlyContinue }
    $stash = Join-Path $script:GameDir 'BepInEx\plugins\_stash_a11y'
    if (Test-Path $stash) { Move-Item $stash (Join-Path $script:RepoDir 'probe\_stash_out') -Force -ErrorAction SilentlyContinue }

    # nvdaControllerClient.dll：Speech 找得到 NVDA 通道用；缺了也不影响日志断言。
    $nvda = Join-Path $script:PluginDir 'nvdaControllerClient.dll'
    if (-not (Test-Path $nvda)) {
        $src = Join-Path $script:RepoDir 'mod\nvda_dl\x64\nvdaControllerClient.dll'
        if (Test-Path $src) { Copy-Item $src $nvda -Force }
    }
}

function Set-ConfigValue([string]$Key, [string]$Value) {
    if (-not (Test-Path $script:CfgPath)) { return }
    $lines = Get-Content $script:CfgPath -Encoding UTF8
    $out = New-Object System.Collections.Generic.List[string]
    foreach ($l in $lines) {
        if ($l -match "^\s*$([regex]::Escape($Key))\s*=") { $out.Add("$Key = $Value") } else { $out.Add($l) }
    }
    Set-Content -Path $script:CfgPath -Value $out -Encoding UTF8
}

# ---------------------------------------------------------------- 进程生命周期
function Start-Game {
    param([hashtable]$Env = @{}, [int]$ReadySeconds = 34)

    # 上一个用例的进程可能还没退干净（Kill 之后要几百毫秒），最多等 30 秒再自己清掉，
    # 否则下一个用例会以「已有 manosaba.exe 在跑」直接 0 秒失败（full4 的 story.speaker/trial.debate）。
    for ($i = 0; $i -lt 30; $i++) {
        $old = @(Get-Process manosaba -ErrorAction SilentlyContinue)
        if ($old.Count -eq 0) { break }
        if ($i -eq 10) { $old | Stop-Process -Force -ErrorAction SilentlyContinue }
        Start-Sleep -Seconds 1
    }
    if (Get-Process manosaba -ErrorAction SilentlyContinue) {
        throw '已有 manosaba.exe 在跑 —— 用例之间必须互斥，先关掉再跑'
    }
    # 不依赖 Steam：游戏目录里有 steam_appid.txt，直接用 manosaba.exe 启动即可
    # （用户 2026-10-30 指正：本作不依赖 Steam，测试不需要先开 Steam）。

    Invoke-Deploy
    Remove-Item $script:LogPath -Force -ErrorAction SilentlyContinue

    foreach ($k in $Env.Keys) { Set-Item -Path "Env:$k" -Value $Env[$k] }
    $exe = Join-Path $script:GameDir 'manosaba.exe'
    $p = Start-Process $exe -WorkingDirectory $script:GameDir -PassThru
    if ($ReadySeconds -gt 0) { Start-Sleep -Seconds $ReadySeconds }

    $hwnd = [IntPtr]::Zero
    for ($i = 0; $i -lt 60; $i++) {
        $hwnd = [TestHarness.W32]::FindGameWindow($p.Id)
        if ($hwnd -ne [IntPtr]::Zero) { break }
        Start-Sleep -Milliseconds 500
    }
    return @{ Process = $p; Hwnd = $hwnd }
}

function Stop-Game {
    param($Game)
    try { if ($Game -and $Game.Process -and -not $Game.Process.HasExited) { $Game.Process.Kill() } } catch { }
    Start-Sleep -Milliseconds 800
}

# ---------------------------------------------------------------- 输入
function Focus-Game([IntPtr]$h) {
    # 抢前台必须能被验证：Windows 有前台锁，游戏窗口被别的窗口盖住时
    # SetForegroundWindow 会静默失败 —— 表现就是「点击全部点空、用例假失败」
    # （2026-10-30 全量套件 9 个用例挂在「进入正片」的根因）。
    for ($i = 0; $i -lt 3; $i++) {
        try {
            [TestHarness.W32]::ShowWindow($h, 9) | Out-Null          # SW_RESTORE
            [TestHarness.W32]::BringWindowToTop($h) | Out-Null
            $pidBuf = 0
            $tid = [TestHarness.W32]::GetWindowThreadProcessId($h, [ref]$pidBuf)
            $me = [TestHarness.W32]::GetCurrentThreadId()
            [TestHarness.W32]::AttachThreadInput($me, $tid, $true) | Out-Null
            [TestHarness.W32]::BringWindowToTop($h) | Out-Null
            [TestHarness.W32]::SetWindowPos($h, [IntPtr](-1), 0, 0, 0, 0, 0x0013) | Out-Null  # HWND_TOPMOST|NOMOVE|NOSIZE|NOACTIVATE
            [TestHarness.W32]::SetForegroundWindow($h) | Out-Null
            [TestHarness.W32]::AttachThreadInput($me, $tid, $false) | Out-Null
        } catch { }
        Start-Sleep -Milliseconds 200
        if ([TestHarness.W32]::GetForegroundWindow() -eq $h) { return $true }

        # 前台锁的常规绕过：先点一下 Alt（模拟用户活动），再抢一次
        [TestHarness.W32]::keybd_event(0x12, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 80
        [TestHarness.W32]::keybd_event(0x12, 0, 2, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 120
        [TestHarness.W32]::SetForegroundWindow($h) | Out-Null
        Start-Sleep -Milliseconds 150
        if ([TestHarness.W32]::GetForegroundWindow() -eq $h) { return $true }
    }
    Write-Host "[warn] 抢前台失败：GetForegroundWindow 仍不是游戏窗口，后续点击/按键可能全部无效" -ForegroundColor Yellow
    return $false
}

function Click-Client([IntPtr]$h, [int]$x, [int]$y) {
    $null = Focus-Game $h   # 不接住的话返回值会漏进调用者的输出（曾把 Match 数组变成混合数组）
    [TestHarness.W32]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 120
    [TestHarness.W32]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 170
    [TestHarness.W32]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 150
}

# 相对坐标版：客户区尺寸会变（实测见过 1280×720 与 1920×1080），写死像素会点空。
# 注意 Click-Client 用的是**屏幕坐标**（SetCursorPos），这里要先把客户区坐标换成屏幕坐标。
function Click-Rel([IntPtr]$h, [double]$rx, [double]$ry) {
    $r = New-Object 'TestHarness.W32+RECT'
    [TestHarness.W32]::GetClientRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $hh = $r.B - $r.T
    if ($w -le 0 -or $hh -le 0) { return $false }
    $pt = New-Object 'TestHarness.W32+POINT'
    $pt.X = [int]($w * $rx); $pt.Y = [int]($hh * $ry)
    [TestHarness.W32]::ClientToScreen($h, [ref]$pt) | Out-Null
    Click-Client $h $pt.X $pt.Y
    return $true
}

# 按住 $HoldMs 再抬：本作 6 FPS（delta≈0.165s），瞬时按键会整个落在两帧之间被吞。
function Send-Key([IntPtr]$h, [byte]$vk, [int]$HoldMs = 420, [byte[]]$ExtraDown = @(), [bool]$Extended = $false) {
    $null = Focus-Game $h
    foreach ($m in $ExtraDown) { [TestHarness.W32]::keybd_event($m, 0, 0, [UIntPtr]::Zero) }
    Start-Sleep -Milliseconds 120
    # ★ 只有方向键/Home/End/Insert/Delete/PageUp/PageDown 这类「扩展键」才带
    #   KEYEVENTF_EXTENDEDKEY(0x0001)；给 Backspace 也带会让 GetAsyncKeyState 读不到（0x08 实测）。
    $downFlags = if ($Extended) { 0x0001 } else { 0x0000 }
    $upFlags = $downFlags -bor 0x0002
    [TestHarness.W32]::keybd_event($vk, 0, $downFlags, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $HoldMs
    [TestHarness.W32]::keybd_event($vk, 0, $upFlags, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 120
    foreach ($m in $ExtraDown) { [TestHarness.W32]::keybd_event($m, 0, 0x0003, [UIntPtr]::Zero) }
    Start-Sleep -Milliseconds 250
}

function Send-NavChord([IntPtr]$h) { Send-Key $h 0x70 320 @(0x11, 0x10, 0x12) }   # Alt+Shift+F1

function Shot([IntPtr]$h, [string]$Name) {
    try {
        $r = New-Object TestHarness.W32+RECT
        [TestHarness.W32]::GetWindowRect($h, [ref]$r) | Out-Null
        $w = $r.R - $r.L; $ht = $r.B - $r.T
        if ($w -le 0 -or $ht -le 0) { return $null }
        $bmp = New-Object System.Drawing.Bitmap $w, $ht
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $hdc = $g.GetHdc()
        [TestHarness.W32]::PrintWindow($h, $hdc, 2) | Out-Null
        $g.ReleaseHdc($hdc); $g.Dispose()
        $path = Join-Path $script:ShotDir ($Name + '.png')
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        return $path
    } catch { return $null }
}

# ---------------------------------------------------------------- 日志
function Get-LogText {
    if (-not (Test-Path $script:LogPath)) { return '' }
    # ★ 必须用 FileShare.ReadWrite 打开：BepInEx 的日志句柄不允许默认共享，
    #   直接 `[IO.File]::ReadAllText` 会抛「文件正被另一个进程使用」，
    #   表现是「游戏明明在写日志，套件却一直读到空串」（本套件第一版的真实 bug）。
    for ($i = 0; $i -lt 3; $i++) {
        try {
            $fs = [IO.File]::Open($script:LogPath, [IO.FileMode]::Open, [IO.FileAccess]::Read,
                                  [IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete)
            $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8)
            $txt = $sr.ReadToEnd()
            $sr.Dispose(); $fs.Dispose()
            return $txt
        } catch { Start-Sleep -Milliseconds 120 }
    }
    return ''
}

function Get-LogMatches([string]$Pattern) {
    $txt = Get-LogText
    if (-not $txt) { return @() }
    # 注意：PowerShell 返回数组会把单元素数组展开成标量 —— 调用侧一律用 @() 包住再索引
    # （`$m[0]` 若拿到字符串会变成取第一个字符，证据里出现过 "C"）。
    return @([regex]::Matches($txt, $Pattern) | ForEach-Object { $_.Value })
}

# 轮询等待：返回 @{ Ok; Line; Waited }。$Pattern 是正则（多行模式已开）。
function Wait-Log {
    param([string]$Pattern, [int]$TimeoutSec = 60, [int]$PollMs = 500)
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        $txt = Get-LogText
        if ($txt) {
            $m = [regex]::Match($txt, $Pattern, 'IgnoreCase,Multiline')
            if ($m.Success) { return @{ Ok = $true; Line = $m.Value; Waited = [int]$sw.Elapsed.TotalSeconds } }
        }
        Start-Sleep -Milliseconds $PollMs
    }
    return @{ Ok = $false; Line = ''; Waited = $TimeoutSec }
}

# ---------------------------------------------------------------- 进入正片（每个剧情类用例的公共前置）
# 结论来自 run17–run19：只有「真点标题 New Game」这条路能进正片；
# LoadAndPlay 只推进状态机、画面仍在标题（run16 截图证据）。
function Enter-Story {
    param($Game, [int]$MaxTries = 10, [int]$WaitSec = 25)
    $attempts = 0
    while ($attempts -lt $MaxTries) {
        $attempts++
        $null = Click-Rel $Game.Hwnd 0.220 0.832   # 标题「新游戏」（相对坐标，尺寸会变）
        $r = Wait-Log -Pattern 'Reader\.say\[|JUMP 已进正片|加载播报（' -TimeoutSec $WaitSec
        if ($r.Ok) {
            # 再推进几下，让对白真正出来（加载播报/黑屏加载段之后才到第一句）
            for ($k = 0; $k -lt 3; $k++) { Start-Sleep -Seconds 2; $null = Click-Rel $Game.Hwnd 0.75 0.75 }
            return @{ Ok = $true; Attempts = $attempts }
        }
    }
    return @{ Ok = $false; Attempts = $attempts }
}

# ---------------------------------------------------------------- 边点边等（剧情推进的通用手段）
# 本作 6 FPS + 「每句要先播完再点下一句」，所以「点 N 次再统一断言」一定会假失败；
# 正确姿势是「点一次 → 看日志够了没 → 不够再点」，直到满足或超时。
function Advance-Until {
    param($Game, [string]$Pattern, [int]$MinCount = 1, [int]$TimeoutSec = 120,
          [int]$ClickX = 960, [int]$ClickY = 540, [int]$ClickMs = 1800)
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        # ★ @() 包住：只有一个匹配时 PowerShell 会把单元素数组拆成字符串，
        #   于是 `$Matches[0]` 变成取第一个字符（曾导致证据行显示为 "C"）。
        $m = @(Get-LogMatches $Pattern)
        if ($m.Count -ge $MinCount) { return @{ Ok = $true; Matches = $m; Waited = [int]$sw.Elapsed.TotalSeconds } }
        Click-Client $Game.Hwnd $ClickX $ClickY
        Start-Sleep -Milliseconds $ClickMs
    }
    return @{ Ok = $false; Matches = @(Get-LogMatches $Pattern); Waited = $TimeoutSec }
}

# ---------------------------------------------------------------- 用例结果
function New-CaseResult {
    param([string]$Id, [string]$Title, [string]$Group)
    return [ordered]@{ Id = $Id; Title = $Title; Group = $Group; Pass = $false
                       Duration = 0; Checks = @(); Evidence = @(); Shots = @(); Error = '' }
}

function Add-Check {
    param($Result, [string]$Name, [bool]$Ok, [string]$Detail = '')
    $Result.Checks += [ordered]@{ Name = $Name; Ok = $Ok; Detail = $Detail }
    if (-not $Ok) { $Result.Pass = $false }
}

# ---------------------------------------------------------------- 配置项读写（用例级临时改 cfg）
function Get-ConfigValue([string]$Key) {
    if (-not (Test-Path $script:CfgPath)) { return $null }
    $m = Select-String -Path $script:CfgPath -Pattern "^\s*$([regex]::Escape($Key))\s*=\s*(.*)$" -Encoding UTF8 |
         Select-Object -First 1
    if ($null -eq $m) { return $null }
    return $m.Matches[0].Groups[1].Value.Trim()
}

# 用例声明 `Cfg = @{ '有配音时也报说话人' = 'true' }`：跑之前改、跑完还原。
# 为什么需要：这几项是 Config.Bind 在启动时读的，进程内改 cfg 不生效，只能启动前改。
function Apply-CfgOverrides($Overrides) {
    $saved = @{}
    foreach ($k in $Overrides.Keys) {
        $saved[$k] = Get-ConfigValue $k
        Set-ConfigValue $k $Overrides[$k]
    }
    return $saved
}

function Restore-CfgOverrides($Saved) {
    foreach ($k in $Saved.Keys) {
        if ($null -ne $Saved[$k]) { Set-ConfigValue $k $Saved[$k] }
    }
}

function Write-Report {
    param($Results, [string]$Tag, [string]$Version)
    $stamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $total = $Results.Count
    $failed = @($Results | Where-Object { -not $_.Pass }).Count
    $passed = $total - $failed

    $jsonPath = Join-Path $script:OutDir ("$Tag-result.json")
    ($Results | ConvertTo-Json -Depth 8) | Set-Content -Path $jsonPath -Encoding UTF8

    $md = New-Object System.Collections.Generic.List[string]
    $md.Add("# 自动化测试报告 $Tag")
    $md.Add('')
    $md.Add("- 时间：$stamp")
    $md.Add("- 模组版本：$Version")
    $md.Add("- 用例：$total 个，通过 $passed，失败 $failed")
    $md.Add("- 明细 JSON：``$([IO.Path]::GetFileName($jsonPath))``")
    $md.Add('')
    $md.Add('| 用例 | 分组 | 结果 | 耗时(s) | 证据 |')
    $md.Add('| --- | --- | --- | --- | --- |')
    foreach ($r in $Results) {
        $badge = if ($r.Pass) { 'PASS' } else { 'FAIL' }
        $ev = ($r.Evidence | Select-Object -First 1) -replace '\|', '\|'
        $md.Add("| ``$($r.Id)`` $($r.Title) | $($r.Group) | $badge | $($r.Duration) | $ev |")
    }
    $md.Add('')
    foreach ($r in $Results) {
        $md.Add("## $($r.Id) — $($r.Title)")
        $md.Add('')
        $md.Add("- 分组：$($r.Group)")
        $md.Add("- 结果：$(if ($r.Pass) { 'PASS' } else { 'FAIL' })（耗时 $($r.Duration) s）")
        if ($r.Error) { $md.Add("- 异常：``$($r.Error)``") }
        if ($r.Checks.Count -gt 0) {
            $md.Add('')
            $md.Add('| 断言 | 结果 | 细节 |')
            $md.Add('| --- | --- | --- |')
            foreach ($c in $r.Checks) {
                $ok = if ($c.Ok) { 'PASS' } else { 'FAIL' }
                $d = ($c.Detail -replace '\|', '\|')
                $md.Add("| $($c.Name) | $ok | ``$d`` |")
            }
        }
        if ($r.Evidence.Count -gt 0) {
            $md.Add('')
            $md.Add('证据行：')
            foreach ($e in $r.Evidence) { $md.Add('- `' + ($e -replace '`', '') + '`') }
        }
        if ($r.Shots.Count -gt 0) {
            $md.Add('')
            foreach ($s in $r.Shots) { $md.Add("- 截图：``$s``") }
        }
        $md.Add('')
    }

    $mdPath = Join-Path $script:OutDir ("$Tag-report.md")
    Set-Content -Path $mdPath -Value $md -Encoding UTF8
    return @{ Md = $mdPath; Json = $jsonPath; Passed = $passed; Failed = $failed; Total = $total }
}
