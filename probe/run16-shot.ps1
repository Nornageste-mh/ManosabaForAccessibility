# run16-shot.ps1 —— 只看画面：部署 + 启动（TEST=1）+ 若干时间点截图，然后杀进程
param(
    [int]$WaitSeconds = 120,
    [string]$Tag = 'run16'
)
$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$LogPath = Join-Path $GameDir 'BepInEx\LogOutput.log'
$RepoDir = 'D:\DSHWorkSpace\manosaba_a11y'
$DllSrc = Join-Path $RepoDir 'mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll'
$PluginDir = Join-Path $GameDir 'BepInEx\plugins\manosaba_a11y'
$OutDir = Join-Path $RepoDir 'probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
Start-Sleep -Seconds 2
New-Item -ItemType Directory -Force -Path $PluginDir | Out-Null
Copy-Item $DllSrc (Join-Path $PluginDir 'ManosabaA11y.dll') -Force
Write-Output ("deploy {0} B" -f (Get-Item (Join-Path $PluginDir 'ManosabaA11y.dll')).Length)

$env:MANOSABA_A11Y_TEST = '1'
$env:MANOSABA_A11Y_LOCALE = 'zh-Hans'
Start-Process -FilePath (Join-Path $GameDir 'manosaba.exe') -WorkingDirectory $GameDir | Out-Null

# 截图函数：PrintWindow 抓窗口内容
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W32Shot {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@
function Shot([string]$name) {
    $p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $p) { Write-Output "no process for $name"; return }
    $h = $p.MainWindowHandle
    [W32Shot]::ShowWindow($h, 9) | Out-Null
    [W32Shot]::SetForegroundWindow($h) | Out-Null
    $r = New-Object W32Shot+RECT
    [W32Shot]::GetWindowRect($h, [ref]$r) | Out-Null
    $w = $r.R - $r.L; $ht = $r.B - $r.T
    if ($w -le 0 -or $ht -le 0) { Write-Output "bad rect for $name"; return }
    $bmp = New-Object System.Drawing.Bitmap($w, $ht)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [W32Shot]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $path = Join-Path $OutDir ("{0}-{1}.png" -f $Tag, $name)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "shot -> $path ($w x $ht)"
}

Start-Sleep -Seconds 45
Shot 't45'
Start-Sleep -Seconds 30
Shot 't75'
Start-Sleep -Seconds 30
Shot 't105'
Start-Sleep -Seconds 15
Shot 't120'

$summary = Join-Path $OutDir ($Tag + '-a11y-summary.txt')
@(
    "===== $Tag summary $(Get-Date -Format HH:mm:ss) =====",
    ("HOOK print={0} say={1} TESTDRIVER={2}" -f @(Select-String -Path $LogPath -Pattern 'HOOK print' -ErrorAction SilentlyContinue).Count, @(Select-String -Path $LogPath -Pattern 'Reader.say' -ErrorAction SilentlyContinue).Count, @(Select-String -Path $LogPath -Pattern 'TESTDRIVER' -ErrorAction SilentlyContinue).Count),
    "----- cfg / scriptPlayer / panels -----",
    (Select-String -Path $LogPath -Pattern 'cfg |scriptPlayer |panel |Reader.say|HOOK print' -ErrorAction SilentlyContinue | Select-Object -First 80 | ForEach-Object { $_.Line })
) | Set-Content -Path $summary -Encoding UTF8
Write-Output "summary -> $summary"

Get-Process manosaba -ErrorAction SilentlyContinue | ForEach-Object { Write-Output "kill pid=$($_.Id)"; Stop-Process -Id $_.Id -Force }
Write-Output "DONE"
