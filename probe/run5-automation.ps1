# run5-automation.ps1 (v2) —— 无人值守驱动《魔法少女ノ魔女裁判》进入第一段 ADV 并连续推进台词
#
# 由来：v1 只用固定点 (450,950) 点一次 "New Game"，run5 实测没命中（60 次推进 PRINT 仍为 0，
# 场景一直 'Boot'）。Lead 从 run5-adv60 截图判读 New Game 中心约 (400,880)。v2 因此改成：
#   1) 强制抢前台（ShowWindow + AttachThreadInput + SetForegroundWindow + 校验 GetForegroundWindow），
#      因为 Unity 在窗口非前台时会吞掉鼠标点击——这是 v1 失败最可能的原因之一；
#   2) "New Game" 用一组候选坐标逐个试，每次点完用日志变化（PRINT / DIALOGUE_CHANGED / 场景变化）判定是否进游戏，
#      失败就换下一个候选，最后再用 Enter/Space 兜底；
#   3) 推进同时用鼠标点 (960,540) 和空格键，避免某一种输入不被接受。
#
# 依赖：游戏已由 run_probe.ps1 启动。本脚本只负责"等窗口 → 进新游戏 → 推进 → 收集证据"。
# 用法：
#   pwsh -File run5-automation.ps1 -TotalTimeoutSec 900
#   pwsh -File run5-automation.ps1 -Tag myRun -NewGameCandidates '400,880','450,950'
param(
    [int]$TotalTimeoutSec = 900,
    [string]$Tag = 'run5',
    [string[]]$NewGameCandidates = @('400,880', '450,950', '410,910', '380,860'),
    [string]$LogPath = 'D:\Steam\steamapps\common\manosaba_game\BepInEx\LogOutput.log',
    [string]$ShotDir = 'D:\DSHWorkSpace\manosaba_a11y\probe\logs'
)

Add-Type -AssemblyName System.Drawing
Add-Type -Namespace Aut2 -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
[DllImport("user32.dll")] public static extern IntPtr SetActiveWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
[DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
[DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, uint d, System.UIntPtr e);
[DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, System.UIntPtr extra);
[DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, System.IntPtr w, System.IntPtr l);
[DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
[DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
[DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int i);
[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
[DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
public struct RECT { public int L, T, R, B; }
public struct POINT { public int X, Y; }
'@

$script:t0 = Get-Date
function Now { (Get-Date).ToString('HH:mm:ss') }
function Elapsed { [int]((Get-Date) - $script:t0).TotalSeconds }
function LogCount([string]$Pattern) {
    try { return @(Select-String -Path $LogPath -Pattern $Pattern -ErrorAction SilentlyContinue).Count } catch { return 0 }
}
function LogTail([string]$Pattern, [int]$n) {
    try { Select-String -Path $LogPath -Pattern $Pattern -ErrorAction SilentlyContinue | Select-Object -Last $n | ForEach-Object { $_.Line } } catch { }
}
function FindGameWindow {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue
    if (-not $p) { return [IntPtr]::Zero }
    if ($p.MainWindowHandle -ne 0) { return $p.MainWindowHandle }
    return [IntPtr]::Zero
}
function Shot([string]$T) {
    $h = FindGameWindow
    if ($h -eq [IntPtr]::Zero) { Write-Output "SHOT_SKIP no-window"; return }
    $cr = New-Object Aut2.U+RECT
    [void][Aut2.U]::GetClientRect($h, [ref]$cr)
    if ($cr.R -le 0 -or $cr.B -le 0) { Write-Output "SHOT_SKIP zero-size"; return }
    $bmp = New-Object System.Drawing.Bitmap($cr.R, $cr.B)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc(); [void][Aut2.U]::PrintWindow($h, $hdc, 2); $g.ReleaseHdc($hdc); $g.Dispose()
    $out = Join-Path $ShotDir ("$Tag-" + $T + '-' + (Get-Date -Format HHmmss) + '.png')
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Write-Output "SHOT $out"
}

# 关键：抢前台。Unity 只在窗口获得焦点时处理鼠标；后台进程默认无权抢前台，
# 用 AttachThreadInput 挂到当前前台线程后 SetForegroundWindow 才能成功。
function Force-Foreground {
    $h = FindGameWindow
    if ($h -eq [IntPtr]::Zero) { return $false }
    for ($i = 0; $i -lt 8; $i++) {
        $fg = [Aut2.U]::GetForegroundWindow()
        if ($fg -eq $h) { return $true }
        [void][Aut2.U]::ShowWindow($h, 9)   # SW_RESTORE
        [void][Aut2.U]::BringWindowToTop($h)
        $fpid = 0
        $fthread = [Aut2.U]::GetWindowThreadProcessId($fg, [ref]$fpid)
        $mythread = [Aut2.U]::GetCurrentThreadId()
        if ($fthread -ne 0 -and $fthread -ne $mythread) { [void][Aut2.U]::AttachThreadInput($mythread, $fthread, $true) }
        [void][Aut2.U]::SetForegroundWindow($h)
        [void][Aut2.U]::SetActiveWindow($h)
        if ($fthread -ne 0 -and $fthread -ne $mythread) { [void][Aut2.U]::AttachThreadInput($mythread, $fthread, $false) }
        Start-Sleep -Milliseconds 250
        if ([Aut2.U]::GetForegroundWindow() -eq $h) { return $true }
    }
    return $false
}
function Move-ClientClick([int]$x, [int]$y) {
    $h = FindGameWindow
    if ($h -eq [IntPtr]::Zero) { return }
    $pt = New-Object Aut2.U+POINT; $pt.X = $x; $pt.Y = $y
    [void][Aut2.U]::ClientToScreen($h, [ref]$pt)
    [void][Aut2.U]::SetCursorPos($pt.X, $pt.Y)
    Start-Sleep -Milliseconds 120
    [Aut2.U]::mouse_event(0x0002, 0, 0, 0, [System.UIntPtr]::Zero)
    Start-Sleep -Milliseconds 70
    [Aut2.U]::mouse_event(0x0004, 0, 0, 0, [System.UIntPtr]::Zero)
}
function Send-Vk([byte]$vk) {
    [Aut2.U]::keybd_event($vk, 0, 0, [System.UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [Aut2.U]::keybd_event($vk, 0, 2, [System.UIntPtr]::Zero)   # KEYEVENTF_KEYUP
}
function Post-ClientClick([int]$x, [int]$y) {
    # 兜底：直接投递鼠标消息（前台抢不到时的最后手段）
    $h = FindGameWindow
    if ($h -eq [IntPtr]::Zero) { return }
    $lp = [System.IntPtr]::new((($y -band 0xFFFF) -shl 16) -bor ($x -band 0xFFFF))
    [void][Aut2.U]::PostMessage($h, 0x0200, [System.IntPtr]::Zero, $lp)   # WM_MOUSEMOVE
    [void][Aut2.U]::PostMessage($h, 0x0201, [System.IntPtr]::new(1), $lp) # WM_LBUTTONDOWN
    Start-Sleep -Milliseconds 60
    [void][Aut2.U]::PostMessage($h, 0x0202, [System.IntPtr]::Zero, $lp)   # WM_LBUTTONUP
}
function Get-EnteredFlag {
    # 判定"是否已进入游戏"：出现打印/对话，或场景树不再是 Boot，或不再是标题画面日志状态
    if ((LogCount 'PRINT #') -gt 0) { return $true }
    if ((LogCount 'DIALOGUE_CHANGED') -gt 0) { return $true }
    if ((LogCount 'CHOICE #') -gt 0) { return $true }
    return $false
}

$deadline = $script:t0.AddSeconds($TotalTimeoutSec)

# ---- 1) 等窗口 + 插件加载 ----
Write-Output "STEP1 $(Now) 等 manosaba 窗口与插件加载"
while ((Get-Date) -lt $deadline) {
    $h = FindGameWindow
    if ($h -ne [IntPtr]::Zero -and (LogCount 'PLUGIN LOADED OK') -gt 0) { break }
    Start-Sleep -Seconds 3
}
$hwnd = FindGameWindow
if ($hwnd -eq [IntPtr]::Zero) { Write-Output 'FATAL no manosaba window'; exit 1 }
$pm = Get-Process manosaba
Write-Output ("WINDOW hwnd={0} title='{1}' style=0x{2:X8} exstyle=0x{3:X8}" -f $hwnd, $pm.MainWindowTitle,
    [Aut2.U]::GetWindowLong($hwnd, -16), [Aut2.U]::GetWindowLong($hwnd, -20))
Write-Output "T=$((Elapsed))s PATCH_OK=$(LogCount 'PATCH_OK ') PATCH_FAIL=$(LogCount 'PATCH_FAIL ')"
LogTail 'PATCH_OK|PATCH_FAIL|PATCH_SUMMARY' 20 | ForEach-Object { Write-Output $_ }
LogTail 'PLUGIN LOADED OK' 1 | ForEach-Object { Write-Output $_ }

# ---- 2) 等 Naninovel 引擎初始化 ----
Write-Output "STEP2 $(Now) 等 NANINOVEL_ENGINE_INITIALIZED"
while ((Get-Date) -lt $deadline -and (LogCount 'NANINOVEL_ENGINE_INITIALIZED') -eq 0) { Start-Sleep -Seconds 3 }
LogTail 'NANINOVEL_ENGINE_INITIALIZED|PRINTER_MANAGER_OK|CHOICE_HANDLER_MANAGER_OK|SCRIPT_PLAYER_OK|UI_MANAGER_OK' 6 | ForEach-Object { Write-Output $_ }
Start-Sleep -Seconds 4

# ---- 3) 抢前台 + 逐个候选坐标点 New Game ----
$fgOk = Force-Foreground
Write-Output "FOCUS foreground_ok=$fgOk fg=$([Aut2.U]::GetForegroundWindow()) game=$hwnd"
Shot 'title'

$entered = $false
foreach ($cand in $NewGameCandidates) {
    if ($entered) { break }
    if ((Get-Date) -ge $deadline) { break }
    $xy = $cand.Split(',')
    $cx = [int]$xy[0]; $cy = [int]$xy[1]
    for ($round = 1; $round -le 2 -and -not $entered; $round++) {
        [void](Force-Foreground)
        Write-Output "CLICK_NEWGAME cand=($cx,$cy) round=$round T=$((Elapsed))s"
        Move-ClientClick $cx $cy
        Start-Sleep -Seconds 4
        if (Get-EnteredFlag) { $entered = $true; break }
        # 点两次可以命中"先聚焦、再点击"的 Unity 行为
        Move-ClientClick $cx $cy
        Start-Sleep -Seconds 4
        if (Get-EnteredFlag) { $entered = $true; break }
    }
    if (-not $entered) {
        # 兜底：Enter / Space 推进（有些版本用键盘确认标题菜单）
        Write-Output "KEY_FALLBACK cand=($cx,$cy) Enter/Space"
        [void](Force-Foreground)
        Send-Vk 0x0D   # VK_RETURN
        Start-Sleep -Seconds 2
        Send-Vk 0x20   # VK_SPACE
        Start-Sleep -Seconds 3
        if (Get-EnteredFlag) { $entered = $true }
    }
    Shot ("ng-{0}x{1}" -f $cx, $cy)
}
Write-Output "ENTERED=$entered PRINT=$(LogCount 'PRINT #') SETTEXT=$(LogCount 'SETTEXT #')"

if (-not $entered) {
    Write-Output 'WARNING 所有候选坐标都未进入游戏；继续按推进流程尝试，日志里会保留证据'
}

# ---- 4) 连续推进 ----
$advance = 0
$lastReport = 0
$firstPrintSeen = $false
while ((Get-Date) -lt $deadline) {
    $prints = LogCount 'PRINT #'
    if (-not $firstPrintSeen -and $prints -gt 0) {
        $firstPrintSeen = $true
        Write-Output "FIRST_PRINT at T=$((Elapsed))s PRINT=$prints"
        Shot 'first-print'
    }
    if ($prints -ge 8) { Write-Output "STEP4 stop: PRINT=$prints (>=8)"; break }
    if ($advance -gt 0 -and ($advance % 15) -eq 0) { Shot ("adv{0}" -f $advance) }

    [void](Force-Foreground)
    if ($advance % 2 -eq 0) { Move-ClientClick 960 540 } else { Send-Vk 0x20 }   # 交替：点击 / 空格
    $advance++
    Start-Sleep -Seconds 3

    if ((Elapsed) - $lastReport -ge 30) {
        $lastReport = [int](Elapsed)
        Write-Output ("T={0}s advance={1} PRINT={2} SETTEXT={3} CHOICE={4} DIALOGUE_CHANGED={5} BASE_ADDMESSAGE={6}" -f
            (Elapsed), $advance, (LogCount 'PRINT #'), (LogCount 'SETTEXT #'), (LogCount 'CHOICE #'),
            (LogCount 'DIALOGUE_CHANGED'), (LogCount 'BASE_ADDMESSAGE'))
    }
}

# ---- 5) 汇总证据 ----
Write-Output "===== SUMMARY T=$((Elapsed))s ====="
Write-Output ("PRINT={0} SETTEXT={1} AUTHOR={2} CHOICE={3} BASE_ADDMESSAGE={4} BASE_SETMESSAGES={5} DEBATE={6} DIALOGUE_CHANGED={7} SCRIPTDUMP={8}" -f
    (LogCount 'PRINT #'), (LogCount 'SETTEXT #'), (LogCount 'AUTHOR #'), (LogCount 'CHOICE #'),
    (LogCount 'BASE_ADDMESSAGE'), (LogCount 'BASE_SETMESSAGES'), (LogCount 'DEBATE_'),
    (LogCount 'DIALOGUE_CHANGED'), (LogCount 'SCRIPTDUMP'))
Write-Output '----- PRINT 行 -----'; LogTail 'PRINT #' 12 | ForEach-Object { Write-Output $_ }
Write-Output '----- SETTEXT 行 -----'; LogTail 'SETTEXT #' 8 | ForEach-Object { Write-Output $_ }
Write-Output '----- 其他关键行 -----'
LogTail 'BASE_ADDMESSAGE|BASE_SETMESSAGES|DEBATE_SETTEXT|DEBATE_ADDMESSAGE|DEBATE_SETMESSAGES|CHOICE #|CHOICE_VISIBLE|AUTHOR #|SCRIPTDUMP|DIALOGUE_CHANGED' 25 | ForEach-Object { Write-Output $_ }
Shot 'final'
Write-Output "DONE $(Now)"
