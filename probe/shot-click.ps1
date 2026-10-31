# shot-click.ps1 —— 交互式辅助：给运行中的 manosaba 截图 / 按客户区相对坐标点击 / 发键
#
# 用途：手动把游戏点到「审判画面」或「魔女手册」，再用 probe 脚本取证。
#   -Shot  只截图
#   -Clicks "0.228,0.806;0.5,0.5"  按相对坐标依次点击（客户区 0..1）
#   -Key   0x1B 发一个键（vk）
# 客户区尺寸每次运行都可能不同（实测见过 1920x1080 与 1296x759 两种），
# 所以这里一律用相对坐标，避免「按旧坐标点空」。
param(
    [string]$Tag = 'manual',
    [string]$Clicks = '',
    [string]$Key = '',
    [int]$WaitMs = 900
)
$ErrorActionPreference = 'Continue'
$GameDir = 'D:\Steam\steamapps\common\manosaba_game'
$OutDir  = 'D:\DSHWorkSpace\manosaba_a11y\probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
public class W32S {
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint f, UIntPtr e);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
}
"@
$p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Host '[error] 没有运行中的 manosaba'; exit 2 }
$p.Refresh()
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { Write-Host '[error] 拿不到窗口句柄'; exit 2 }

function Focus {
    [W32S]::ShowWindow($h, 9) | Out-Null
    [W32S]::SetWindowPos($h, [IntPtr](-1), 0, 0, 0, 0, 0x0013) | Out-Null
    [W32S]::BringWindowToTop($h) | Out-Null
    [W32S]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 200
}
function ClickRel([double]$rx, [double]$ry) {
    $cr = New-Object W32S+RECT; [W32S]::GetClientRect($h, [ref]$cr) | Out-Null
    $cw = $cr.R - $cr.L; $ch = $cr.B - $cr.T
    $cx = [int]($cw * $rx); $cy = [int]($ch * $ry)
    $pt = New-Object W32S+POINT; $pt.X = $cx; $pt.Y = $cy
    [W32S]::ClientToScreen($h, [ref]$pt) | Out-Null
    [W32S]::SetCursorPos($pt.X, $pt.Y) | Out-Null
    Start-Sleep -Milliseconds 120
    [W32S]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 140
    [W32S]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds $WaitMs
    Write-Host "[click] client=($cx,$cy) rel=($rx,$ry)"
}

$cr = New-Object W32S+RECT; [W32S]::GetClientRect($h, [ref]$cr) | Out-Null
Write-Host "[info] hwnd=$h client=$($cr.R-$cr.L)x$($cr.B-$cr.T)"

Focus
if ($Clicks) {
    foreach ($pair in ($Clicks -split ';')) {
        if (-not $pair.Trim()) { continue }
        $xy = $pair -split ','
        ClickRel ([double]$xy[0]) ([double]$xy[1])
    }
}
if ($Key) {
    Focus
    $hex = $Key -replace '^0x', ''
    $vk = [byte][Convert]::ToInt32($hex, 16)
    [W32S]::keybd_event($vk, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 320
    [W32S]::keybd_event($vk, 0, 2, [UIntPtr]::Zero)
    Write-Host ("[key] vk=0x{0:X2}" -f $vk)
}

$wr = New-Object W32S+RECT; [W32S]::GetWindowRect($h, [ref]$wr) | Out-Null
$w = $wr.R - $wr.L; $hh = $wr.B - $wr.T
$bmp = New-Object System.Drawing.Bitmap $w, $hh
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[W32S]::PrintWindow($h, $hdc, 0) | Out-Null
$g.ReleaseHdc($hdc); $g.Dispose()
$path = Join-Path $OutDir "$Tag.png"
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
Write-Host "[shot] $path"
