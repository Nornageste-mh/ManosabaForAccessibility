# run6-a11y.ps1 —— Lead 的实机验收脚本（无人值守）
# 目的：部署 ManosabaA11y 新版 → 启动游戏（测试模式+强制 zh-Hans）→ 点 New Game → 连续推进
#       → 从 BepInEx 日志里读 `[a11y] Reader.say` 判断朗读链是否真的通 → 收尾杀进程。
# 用法：pwsh -File probe\run6-a11y.ps1 [-AdvanceSeconds 90] [-Tag run6]
param(
    [int]$AdvanceSeconds = 90,
    [string]$Tag = 'run6',
    [switch]$KeepGame
)

$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc  = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$ShotDir = Join-Path $RepoDir 'probe\logs'
$Summary = Join-Path $ShotDir ($Tag + '-a11y-summary.txt')

Add-Type -AssemblyName System.Drawing
Add-Type -Namespace Aut -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
[DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
[DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, uint d, System.UIntPtr e);
[DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
[DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
public struct RECT { public int L, T, R, B; }
'@

$script:t0 = Get-Date
function Now { (Get-Date).ToString('HH:mm:ss') }
function Elapsed { [int]((Get-Date) - $script:t0).TotalSeconds }
function Say([string]$s) { Write-Output ("[{0} T={1,3}s] {2}" -f (Now), (Elapsed), $s) }

function LogCount([string]$Pattern) {
    try { return @(Select-String -Path $LogPath -Pattern $Pattern -ErrorAction SilentlyContinue).Count } catch { return 0 }
}
function LogTail([string]$Pattern, [int]$n) {
    try { Select-String -Path $LogPath -Pattern $Pattern -ErrorAction SilentlyContinue | Select-Object -Last $n | ForEach-Object { $_.Line } } catch { }
}
function Shot([string]$tag) {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue
    if (-not $p) { Say "SHOT_SKIP no-process"; return }
    $h = $p.MainWindowHandle
    if ($h -eq 0) { Say "SHOT_SKIP no-hwnd"; return }
    $cr = New-Object Aut.U+RECT
    [void][Aut.U]::GetClientRect($h, [ref]$cr)
    if ($cr.R -le 0 -or $cr.B -le 0) { Say "SHOT_SKIP zero-size"; return }
    $bmp = New-Object System.Drawing.Bitmap($cr.R, $cr.B)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [void][Aut.U]::PrintWindow($h, $hdc, 2)
    $g.ReleaseHdc($hdc); $g.Dispose()
    $out = Join-Path $ShotDir ($Tag + '-' + $tag + '-' + (Get-Date -Format HHmmss) + '.png')
    $bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
    Say "SHOT $out"
}
function Click([int]$x, [int]$y) {
    [void][Aut.U]::SetCursorPos($x, $y)
    Start-Sleep -Milliseconds 90
    [Aut.U]::mouse_event(0x0002, 0, 0, 0, [System.UIntPtr]::Zero)
    Start-Sleep -Milliseconds 70
    [Aut.U]::mouse_event(0x0004, 0, 0, 0, [System.UIntPtr]::Zero)
}

# ---------------- 1) 部署 ----------------
Say "STEP1 部署 DLL 并清理探针插件"
Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Say "kill 残留实例 pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Start-Sleep -Seconds 2

if (-not (Test-Path $DllSrc)) { Say "FATAL 没有构建产物 $DllSrc"; exit 1 }
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
Say ("deploy {0} B -> {1}" -f (Get-Item (Join-Path $PluginDir 'ManosabaA11y.dll')).Length, $PluginDir)

$probeDll = Join-Path $GameDir 'BepInEx\plugins\ManosabaProbe.dll'
if (Test-Path $probeDll) {
    $bak = Join-Path $RepoDir ('probe\plugin\ManosabaProbe\bin\Release\ManosabaProbe.deployed-' + (Get-Date -Format HHmmss) + '.dll.bak')
    Move-Item $probeDll $bak -Force
    Say "探针插件已移出 plugins（备份 $bak）"
}

# ---------------- 2) 启动（测试模式 + 强制简体中文）----------------
Say "STEP2 启动游戏 MANOSABA_A11Y_TEST=1 MANOSABA_A11Y_LOCALE=zh-Hans"
$env:MANOSABA_A11Y_TEST = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
$exe = Join-Path $GameDir 'manosaba.exe'
Start-Process -FilePath $exe -WorkingDirectory $GameDir | Out-Null

# 等窗口
$deadline = $script:t0.AddSeconds(60)
while ((Get-Date) -lt $deadline) {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue
    if ($p -and $p.MainWindowHandle -ne 0) { break }
    Start-Sleep -Seconds 2
}
$p = Get-Process manosaba -ErrorAction SilentlyContinue
if (-not $p) { Say "FATAL 游戏没起来"; exit 1 }
$hwnd = $p.MainWindowHandle
Say ("窗口 hwnd={0} title='{1}'" -f $hwnd, $p.MainWindowTitle)

# 等插件：harmony patched + 语言切换 + 语音后端
$deadline = $script:t0.AddSeconds(180)
while ((Get-Date) -lt $deadline) {
    $hp = LogCount 'harmony patched'
    $pl = LogCount '语音后端'
    if ($hp -gt 0 -and $pl -gt 0) { break }
    Start-Sleep -Seconds 3
}
Say ("plugin: harmony=$(LogCount 'harmony patched') speech=$(LogCount '语音后端')")
LogTail '\[a11y\]' 12 | ForEach-Object { Say "  $_" }

# 等 Naninovel 引擎（标题画面）
$deadline = $script:t0.AddSeconds(200)
while ((Get-Date) -lt $deadline) {
    if ((LogCount 'NANINOVEL_ENGINE_INITIALIZED') -gt 0) { break }
    # 有的版本插件不打印这条；用选项/标题 UI 出现的日志兜底没有，就靠时间
    if ((Elapsed) -gt 120) { break }
    Start-Sleep -Seconds 3
}
Start-Sleep -Seconds 6
Shot 'title'

# ---------------- 3) 点 New Game ----------------
[void][Aut.U]::SetForegroundWindow($hwnd)
Start-Sleep -Milliseconds 400
Say "STEP3 点击 New Game 候选坐标 (400,880) → (450,950) → (400,900)"
Click 400 880
Start-Sleep -Seconds 4
if ((LogCount 'Reader.say') -eq 0 -and (LogCount 'Reader.pending') -eq 0) {
    Click 450 950
    Start-Sleep -Seconds 3
    Click 400 900
    Start-Sleep -Seconds 3
}
Shot 'afterNewGame'
Say ("进剧情检测: Reader.say=$(LogCount 'Reader.say') a11yLines=$(LogCount '\[a11y\]')")

# ---------------- 4) 推进 ----------------
Say "STEP4 连续推进 ${AdvanceSeconds}s（点 (960,540)）"
$adv = 0
$deadline = (Get-Date).AddSeconds($AdvanceSeconds)
$printedAtFirst = $false
while ((Get-Date) -lt $deadline) {
    $says = LogCount 'Reader.say'
    if (-not $printedAtFirst -and $says -gt 0) {
        $printedAtFirst = $true
        Say "FIRST_SAY T=$((Elapsed))s"
        Shot 'first-say'
    }
    if ($says -ge 12) { Say "STEP4 已抓到 $says 句朗读，提前结束"; break }
    [void][Aut.U]::SetForegroundWindow($hwnd)
    Click 960 540
    $adv++
    Start-Sleep -Milliseconds 2400
    if (($adv % 5) -eq 0) {
        Say ("adv={0} Reader.say={1} dedupe={2} a11y={3}" -f $adv, (LogCount 'Reader.say'), (LogCount 'Reader.dedupe'), (LogCount '\[a11y\]'))
    }
}
Shot 'final'
Say ("STEP4 结束 adv={0} Reader.say={1} dedupe={2}" -f $adv, (LogCount 'Reader.say'), (LogCount 'Reader.dedupe'))

# ---------------- 5) 收证据 ----------------
Say "STEP5 汇总"
$lines = @()
$lines += "===== $Tag a11y summary $(Now) ====="
$lines += "Reader.say=$(LogCount 'Reader.say') Reader.dedupe=$(LogCount 'Reader.dedupe') a11yLines=$(LogCount '\[a11y\]')"
$lines += "harmony=$(LogCount 'harmony patched') speech=$(LogCount '语音后端') locale=$(LogCount 'Locale:')"
$lines += "----- Reader.say 前 20 条 -----"
$lines += (Select-String -Path $LogPath -Pattern 'Reader.say' -ErrorAction SilentlyContinue | Select-Object -First 20 | ForEach-Object { $_.Line })
$lines += "----- 其它 [a11y] 尾部 30 条 -----"
$lines += (LogTail 'Reader\.|Locale|harmony|语音后端|Patches' 30)
$lines += "----- 错误 -----"
$lines += (LogTail 'Reader\.(OnPrint|OnPanelAddMessage|Tick|speaker|author).*Exception|harmony patch failed|未找到' 15)
$lines | Set-Content -Path $Summary -Encoding UTF8
Say "summary -> $Summary"
$lines | ForEach-Object { Write-Output $_ }

if (-not $KeepGame) {
    Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Say "收尾 kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
    Say "DONE 实例已释放 $((Now))"
} else {
    Say "KEEP 游戏保留在跑（pid=$((Get-Process manosaba).Id)）"
}
