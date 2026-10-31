# cap-screen.ps1 —— 用屏幕拷贝截图（PrintWindow 对 DXGI 翻转模型会得到白图）
param([string]$Tag = 'cap', [switch]$Focus)
$ErrorActionPreference = 'Continue'
$OutDir = 'D:\DSHWorkSpace\manosaba_a11y\probe\logs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W32C {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@
$p = Get-Process manosaba -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Host '[error] 没有运行中的 manosaba'; exit 2 }
$p.Refresh()
$h = $p.MainWindowHandle
if ($Focus) {
    [W32C]::ShowWindow($h, 9) | Out-Null
    [W32C]::SetWindowPos($h, [IntPtr](-1), 0, 0, 0, 0, 0x0013) | Out-Null
    [W32C]::BringWindowToTop($h) | Out-Null
    [W32C]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 600
}
$r = New-Object W32C+RECT
[W32C]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.R - $r.L; $hh = $r.B - $r.T
$bmp = New-Object System.Drawing.Bitmap $w, $hh
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, (New-Object System.Drawing.Size($w, $hh)))
$g.Dispose()
$path = Join-Path $OutDir "$Tag.png"
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
Write-Host "[shot] $path  rect=$($r.L),$($r.T),${w}x${hh}"
