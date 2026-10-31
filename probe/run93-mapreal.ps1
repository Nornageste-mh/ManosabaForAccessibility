# run93-mapreal.ps1 —— 用真实剧情的「选地点」命令验收地图选择
#
# 依据（evidence/raw/src 源剧本取证）：
#   Act01_Chapter01/Act01_Chapter01_Adv25 里有
#     @choiceMapArea MapAreaNames:BasementHallway,Lake,Lounge,EntranceHall,Infirmary,MessHall,RecreationRoom,Showers FlagVariableName:chosenAreaFlag
#   即真实游玩里地图是「可点地点列表」。本脚本：标题 → 读取存档槽 1 → 进正片 → JUMP 到该脚本，
#   观察：
#     · MapPage._displayableAreas / _restrictionAreas / PresentationMode 的真实取值
#     · MapChoice 是否识别出可前往地点并编号播报
#     · 数字键（测试钩子代替，本机键盘注入被系统挡掉）能否触发地点按钮
#
# 为什么必须先读档进正片：Plugin.JumpTick 只在「真的在播 Act* 剧本」之后才跳
# （run16 教训：标题画面直接 LoadAndPlay 只推进状态机，UI 还停在标题，什么都验证不到）。
param(
    [string]$Tag = 'run93',
    [string]$Locale = 'zh-Hans',
    [string]$Script = 'Act01_Chapter01/Act01_Chapter01_Adv25',
    [int]$WaitSeconds = 180,
    [int]$MapPick = 1,
    [int]$MapFloor = 0,
    [string]$PickKind = ''
)
$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath = Join-Path $GameDir 'BepInEx\LogOutput.log'
$OutDir  = 'D:\DSHWorkSpace\manosaba_a11y\probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W93 {
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@
function Focus-Game([IntPtr]$h) {
    [W93]::ShowWindow($h, 9) | Out-Null
    [W93]::SetWindowPos($h, [IntPtr](-1), 0, 0, 0, 0, 0x0013) | Out-Null
    [W93]::BringWindowToTop($h) | Out-Null
    [W93]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 180
}
function Click-Rel([IntPtr]$h, [double]$rx, [double]$ry) {
    $cr = New-Object W93+RECT; [W93]::GetClientRect($h, [ref]$cr) | Out-Null
    $cw = $cr.R - $cr.L; $ch = $cr.B - $cr.T
    if ($cw -le 0 -or $ch -le 0) { return }
    $pt = New-Object W93+POINT; $pt.X = [int]($cw * $rx); $pt.Y = [int]($ch * $ry)
    [W93]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W93]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 100
    [W93]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 140
    [W93]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 900
}
function Get-LogText {
    if (-not (Test-Path $LogPath)) { return '' }
    try {
        $fs = [IO.File]::Open($LogPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        $sr = New-Object IO.StreamReader($fs, [Text.Encoding]::UTF8)
        $t = $sr.ReadToEnd(); $sr.Dispose(); $fs.Dispose(); return $t
    } catch { return '' }
}
function Text-Has([string]$pattern) { return [regex]::IsMatch((Get-LogText), $pattern) }
function Shot([IntPtr]$h, [string]$name) {
    $r = New-Object W93+RECT; [W93]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $hh = $r.B - $r.T
    if ($w -le 0 -or $hh -le 0) { return }
    $bmp = New-Object System.Drawing.Bitmap $w, $hh
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size($w, $hh)))
    $g.Dispose()
    $bmp.Save((Join-Path $OutDir "$Tag-$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# 清理会干扰的环境变量，只留本次需要的
foreach ($v in 'MANOSABA_A11Y_DIAG','MANOSABA_A11Y_LOCALE','MANOSABA_A11Y_MAPDIAG','MANOSABA_A11Y_TEST_MAPCHOICE',
               'MANOSABA_A11Y_JUMP','MANOSABA_A11Y_TEST_ESC','MANOSABA_A11Y_TEST_REPEAT','MANOSABA_A11Y_TEST_NAV',
               'MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_DEBUGUI','MANOSABA_A11Y_TEST',
               'MANOSABA_A11Y_TEST_MAPFLOOR','MANOSABA_A11Y_TEST_MAPPICK_KIND') {
    Set-Item -Path "Env:$v" -Value ''
}
$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_LOCALE = $Locale
$env:MANOSABA_A11Y_MAPDIAG = '1'
$env:MANOSABA_A11Y_TEST_MAPCHOICE = "$MapPick"
if ($MapFloor -gt 0) { $env:MANOSABA_A11Y_TEST_MAPFLOOR = "$MapFloor" }
if ($PickKind) { $env:MANOSABA_A11Y_TEST_MAPPICK_KIND = $PickKind }
$env:MANOSABA_A11Y_KEYDIAG = '1'
$env:MANOSABA_A11Y_JUMP = $Script

Remove-Item $LogPath -ErrorAction SilentlyContinue
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
$proc = Start-Process (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir -PassThru
Write-Host "[info] pid=$($proc.Id) jump=$Script pick=$MapPick 等 34s"
Start-Sleep -Seconds 34
$h = [IntPtr]::Zero
for ($i = 0; $i -lt 30; $i++) { $proc.Refresh(); if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { $h = $proc.MainWindowHandle; break }; Start-Sleep 1 }
if ($h -eq [IntPtr]::Zero) { Write-Host '[error] 没窗口'; exit 3 }
Write-Host "[info] hwnd=$h"

# 1) 标题 → 读取
$ok = $false
for ($i = 1; $i -le 8; $i++) {
    $null = Focus-Game $h; Click-Rel $h 0.0875 0.832
    Start-Sleep 2
    if (Text-Has '读取|Load') { $ok = $true; break }
}
Write-Host "[step1] 读取界面 ok=$ok"
Shot $h 'loadlist'

# 2) 第 1 格存档 → 进正片（JumpTick 也要看到 Act* 才跳）
$ok2 = $false
for ($i = 1; $i -le 8; $i++) {
    $null = Focus-Game $h; Click-Rel $h 0.179 0.233
    Start-Sleep 4
    if (Text-Has 'Reader\.say\[|加载播报（') { $ok2 = $true; break }
}
Write-Host "[step2] 存档读取 ok=$ok2"
Shot $h 'story'

$jumpOk = $false; $choiceOk = $false; $pickOk = $false; $pickAt = Get-Date
$deadline = (Get-Date).AddSeconds($WaitSeconds)
$started = Get-Date
$lastShot = -100
while ((Get-Date) -lt $deadline) {
    Start-Sleep 3
    # 玩家自己会点地图/推进剧情：这里也点屏幕中心，既推进对话也可能点到地图地点
    $null = Focus-Game $h
    # 地图选择状态下绝对不能再点屏幕：本机点中心会真的点中地图上的地点，
    # 游戏会先以那个地点完成选择（run115 实测：数字键 7 选娱乐室，结果走的是玄关大厅分支）。
    if (-not $choiceOk) { Click-Rel $h 0.5 0.5 }
    if (-not $jumpOk -and (Text-Has 'JUMP 已进正片|JUMP LoadAndPlay 已发出')) { $jumpOk = $true; Write-Host '[step3] jump 已发出' }
    if (-not $choiceOk -and (Text-Has 'MAPCHOICE 播报 \d+ 个地点')) { $choiceOk = $true; Write-Host '[step4] 识别出可选地点'; Shot $h 'map-choice' }
    if (-not $pickOk -and $MapFloor -gt 0 -and (Text-Has 'MAPCHOICE 切换楼层')) { $pickOk = $true; Write-Host '[step5] 楼层切换已触发' }
if (-not $pickOk -and (Text-Has 'MAPCHOICE 测试：按 kind 点击')) { $pickOk = $true; $pickAt = Get-Date; Write-Host '[step5] kind 点击已触发' }
if (-not $pickOk -and (Text-Has 'MAPCHOICE 数字键 \d+ ->')) { $pickOk = $true; $pickAt = Get-Date; Write-Host '[step5] 数字键选择已触发'; Shot $h 'map-picked' }
    if ($pickOk -and ((Get-Date) - $pickAt).TotalSeconds -gt 30) { Write-Host '[step6] 选择后 30s，收尾'; break }
    $el = [int]((Get-Date) - $started).TotalSeconds
    if ($el - $lastShot -ge 30) { $lastShot = $el; Shot $h ('t' + $el) }
}

$t = Get-LogText
$scan = @([regex]::Matches($t, 'MAPCHOICE 扫描[^\r\n]*(?:\r?\n  \[[^\r\n]*)*\r?\n  => [^\r\n]*') | ForEach-Object { $_.Value })
$ann  = @([regex]::Matches($t, 'MAPCHOICE 播报[^\r\n]*') | ForEach-Object { $_.Value })
$pick = @([regex]::Matches($t, 'MAPCHOICE 数字键[^\r\n]*') | ForEach-Object { $_.Value })
$jump = @([regex]::Matches($t, 'JUMP[^\r\n]*') | ForEach-Object { $_.Value })
$sa   = @([regex]::Matches($t, 'ScreenAssist: 播报[^\r\n]*') | ForEach-Object { $_.Value })
$say  = @([regex]::Matches($t, 'Reader\.say') | ForEach-Object { $_.Value })
$err  = @([regex]::Matches($t, '(?m)^.*(?:Exception|异常)[^\r\n]*$') | ForEach-Object { $_.Value })

$lines = @()
$lines += "Tag=$Tag $(Get-Date -Format 'HH:mm:ss') jump=$Script"
$lines += "step1 读取界面=$ok  step2 存档读取=$ok2  step3 jump=$jumpOk  step4 识别地点=$choiceOk  step5 数字键选择=$pickOk"
$lines += "say=$($say.Count)"
$lines += "---- JUMP ----"; $lines += ($jump | Select-Object -Last 6)
$lines += "---- MAPCHOICE 播报 ----"; $lines += ($ann | Select-Object -Last 6)
$lines += "---- MAPCHOICE 楼层/选择 ----"
$lines += @([regex]::Matches($t, 'MAPCHOICE (?:切换楼层|测试钩子|数字键|变量|测试：按 kind)[^\r\n]*') | ForEach-Object { $_.Value } | Select-Object -Last 6)
$lines += "---- ScreenAssist 播报（最后 4 条）----"; $lines += ($sa | Select-Object -Last 4)
$lines += "---- MAPCHOICE 扫描（最后 1 次）----"; $lines += ($scan | Select-Object -Last 1)
$pickLines = @([regex]::Matches($t, 'Reader\.(?:say|voiced)\[[^\r\n]*') | ForEach-Object { $_.Value })
$lines += "---- 选择后剧情（最后 8 条朗读）----"; $lines += ($pickLines | Select-Object -Last 8)
$lines += "---- 异常（最后 5 条）----"; $lines += ($err | Select-Object -Last 5)
$lines | Set-Content (Join-Path $OutDir "$Tag-summary.txt") -Encoding UTF8
Write-Host "[done] $(Join-Path $OutDir "$Tag-summary.txt")"
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
