#Requires -Version 5.1
<#
    发布：构建 → 校验安装包 → 打 ZIP（IL2CPP 路线 / BepInEx 6）

    用法：
        cd <本文件所在的 mod 目录>
        .\release.ps1
        .\release.ps1 -GameDir "D:\...\manosaba_game"

    产物：.\dist\ManosabaA11y-<版本>.zip

    ★ 绝不使用 Compress-Archive 打这个包：Windows PowerShell 5.1 的它会给条目名
      写反斜杠（BepInEx\core\x.dll），Linux/macOS 与 Python 的 unzip 会解出一坨
      带反斜杠的文件名。必须走 tools\makezip.ps1（手工建条目，正斜杠）。
    ★ 版本号纯数字四段；发布 tag 写 v<版本>（如 v0.0.0.1）。
    ★ 发布包不进版本库（见 .gitignore）：每次发版都会往历史里塞一个几十 MB
      的二进制，git 对 zip 没法做增量。
#>
[CmdletBinding()]
param(
    [string]$GameDir = "D:\Steam\steamapps\common\manosaba_game",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$mod = $PSScriptRoot
$pluginName = "ManosabaA11y"
$pluginGuid = "manosaba.a11y.reader"
$pkg = Join-Path $mod "package"

function Step($msg) { Write-Host "`n>>> $msg" -ForegroundColor Cyan }
function Ok($msg) { Write-Host "    $msg" -ForegroundColor Green }
function Fail($msg) { Write-Host "    $msg" -ForegroundColor Red; exit 1 }

# ============================================================
Step "1/4  构建"
if ($SkipBuild) {
    Ok "已指定 -SkipBuild，使用现有 package\"
} else {
    & (Join-Path $mod "build.ps1") -GameDir $GameDir -NoDeploy
    if ($LASTEXITCODE -ne 0) { Fail "build.ps1 失败" }
}

$ver = ([xml](Get-Content -LiteralPath (Join-Path $mod "src\$pluginName\$pluginName.csproj") -Raw)).Project.PropertyGroup.Version |
    Where-Object { $_ } | Select-Object -First 1
if (-not $ver) { Fail "读不到 csproj 的 <Version>" }
$ver = $ver.Trim()
Ok "版本 $ver"

# ============================================================
Step "2/4  校验 package\ 内容（发布红线）"
if (-not (Test-Path -LiteralPath $pkg)) { Fail "package\ 不存在，先跑 build.ps1" }

$required = @(
    "winhttp.dll",
    "doorstop_config.ini",
    ".doorstop_version",
    "BepInEx\core\BepInEx.Core.dll",
    "BepInEx\core\BepInEx.Unity.IL2CPP.dll",
    "BepInEx\core\BepInEx.Unity.Common.dll",
    "BepInEx\core\0Harmony.dll",
    "BepInEx\core\Il2CppInterop.Runtime.dll",
    "BepInEx\plugins\manosaba_a11y\$pluginName.dll",
    "BepInEx\plugins\manosaba_a11y\nvdaControllerClient.dll",
    "nvdaControllerClient.dll",
    "licenses\THIRD-PARTY-NOTICES.txt"
)
$miss = @()
foreach ($r in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $pkg $r))) { $miss += $r }
}
if ($miss.Count -gt 0) { Fail "安装包缺必需文件：`n  $($miss -join "`n  ")" }
Ok "必需文件齐全（$($required.Count) 项）"

$md = @(Get-ChildItem -LiteralPath $pkg -File -Filter "*.md")
if ($md.Count -lt 2) { Fail "包根至少要有 2 个说明 *.md（安装说明.md、常见问题.md），实际 $($md.Count)" }
Ok "说明文档：$($md.Name -join '、')"

$forbidden = @(
    "BepInEx\interop",
    "BepInEx\unity-libs",
    "BepInEx\config\$pluginGuid.cfg",
    "BepInEx\plugins\ManosabaA11yProbe.dll",
    "BepInEx\plugins\manosaba_a11y\ManosabaA11yProbe.dll"
)
$bad = @()
foreach ($f in $forbidden) {
    if (Test-Path -LiteralPath (Join-Path $pkg $f)) { $bad += $f }
}
if ($bad.Count -gt 0) {
    Fail "安装包混入了不该分发的内容：`n  $($bad -join "`n  ")`n（interop 是机器相关的、config 是用户数据、probe 是开发工具）"
}
Ok "未混入 interop / unity-libs / config / probe"

# ============================================================
Step "3/4  打 ZIP（makezip，正斜杠条目）"
$dist = Join-Path $mod "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$zipPath = Join-Path $dist "$pluginName-$ver.zip"
& (Join-Path $mod "tools\makezip.ps1") -Source $pkg -Zip $zipPath
if ($LASTEXITCODE -ne 0) { Fail "makezip.ps1 失败" }

# 自检：条目里不能有反斜杠
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    $total = $z.Entries.Count
    $bs = @($z.Entries | Where-Object { $_.FullName.Contains('\') })
} finally { $z.Dispose() }
if ($bs.Count -gt 0) {
    Fail "ZIP 里有 $($bs.Count) 个反斜杠条目（例如 $($bs[0].FullName)）—— 必须用 makezip.ps1 重打"
}
Ok "ZIP 自检通过：$total 个条目，无反斜杠"
Ok $zipPath
$sizeMB = [math]::Round((Get-Item -LiteralPath $zipPath).Length / 1MB, 2)
Ok "大小 $sizeMB MB"

# ============================================================
Step "4/4  发布清单"
Write-Host @"
    ZIP      : $zipPath
    Git tag  : v$ver
    发布前自检：
      [ ] 已在干净的游戏目录上实测「解压 → 启动 → 能读能导航」
      [ ] 已实测「不装补丁 / 关掉补丁」游戏本身正常（回归，测试问卷第 8 节）
      [ ] 测试问卷 docs/测试问卷.md 已填实机结果（未实测的写「尚未实机复测」）
      [ ] README 的「已知问题」「游戏原生占用的键」已更新
      [ ] licenses\THIRD-PARTY-NOTICES.txt 内容与本包实际随附的组件一致

    上传：GitHub Releases，附件用上面这个 zip，tag 写 v$ver。
"@ -ForegroundColor Cyan
