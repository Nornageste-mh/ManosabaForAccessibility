#Requires -Version 5.1
<#
    可复现构建（IL2CPP 路线）：取依赖 → 编译插件 → 部署 → 组装安装包

    用法：
        cd <本文件所在的 mod 目录>
        .\build.ps1                      # 编译 + 部署到游戏 + 组装 package\
        .\build.ps1 -NoDeploy            # 只编译 + 组装 package\，不动游戏目录
        .\build.ps1 -GameDir "D:\...\manosaba_game"

    本作是 **IL2CPP + BepInEx 6.0.0-be.788**，和 Mono 那几作的构建脚本不是同一套：
      · 入口基类是 BasePlugin（BepInEx 6 才有），TFM 是 net6.0；
      · 编译要引用游戏里 **已生成的 interop 程序集**
        （<GameDir>\BepInEx\interop\*.dll）—— 第一次运行游戏后才会生成；
      · 基础包是 BepInEx_win_x64_6.0.0-be.788.zip（带 dotnet\ 与 winhttp.dll），
        不是 BepInEx 5 的 BepInEx_win_x64_5.x.zip；
      · Il2CppInterop 若要叠上游 PR#277 的修复版 dll，用 -Il2CppInteropDll 指定。

    产物：
        .\package\    ← 直接拷进游戏根目录即可用
        <GameDir>\BepInEx\plugins\manosaba_a11y\   ← 本次构建已部署的插件

    注意：本脚本必须保存为 UTF-8 带 BOM —— Windows PowerShell 5.1 读无 BOM 的
    脚本会按 GBK 解码，中文注释变乱码并直接语法错误。
#>
[CmdletBinding()]
param(
    [string]$GameDir = "D:\Steam\steamapps\common\manosaba_game",
    [string]$BepInExVersion = "6.0.0-be.788",
    # 基础包：默认从 A11Y_CACHE / 本机其它 A11y 工程复用，没有再下载
    [string]$BepInExZip = "",
    # Il2CppInterop 修复版（PR#277，可选）：给了就叠进 package\BepInEx\core\
    [string]$Il2CppInteropDll = "",
    [string]$NvdaClientUrl = "https://download.nvaccess.org/releases/stable/nvda_2026.2_controllerClient.zip",
    [string]$CacheDir = "$env:A11Y_CACHE",
    # 只编译、不往游戏目录写文件
    [switch]$NoDeploy
)

$ErrorActionPreference = "Stop"
$mod = $PSScriptRoot
$dl = Join-Path $mod "dl"
New-Item -ItemType Directory -Force -Path $dl | Out-Null

function Step($msg) { Write-Host "`n>>> $msg" -ForegroundColor Cyan }
function Ok($msg) { Write-Host "    $msg" -ForegroundColor Green }
function Fail($msg) { Write-Host "    $msg" -ForegroundColor Red; exit 1 }

$pluginName = "ManosabaA11y"
$pluginGuid = "manosaba.a11y.reader"
$srcDir = Join-Path $mod "src\$pluginName"
$pluginCs = Join-Path $srcDir "Plugin.cs"

# ============================================================
Step "1/6  检查游戏目录与 interop"
$exe = Join-Path $GameDir "manosaba.exe"
if (-not (Test-Path -LiteralPath $exe)) {
    Fail "找不到游戏：$GameDir（用 -GameDir 指定游戏根目录）"
}
Ok $GameDir

$interop = Join-Path $GameDir "BepInEx\interop"
$needInterop = @("Assembly-CSharp.dll", "Il2Cppmscorlib.dll", "UnityEngine.CoreModule.dll",
    "UnityEngine.UI.dll", "Unity.TextMeshPro.dll")
if (-not (Test-Path -LiteralPath $interop)) {
    Fail @"
找不到 interop 目录：$interop

IL2CPP 路线必须先用 BepInEx 6 启动一次游戏，让 Il2CppInterop 生成 interop 程序集：
  1) 把 BepInEx 6.0.0-be.788 基础包解压到 $GameDir
  2) 启动一次游戏（进到标题界面即可），然后退出
  3) 确认 $interop 下出现 Assembly-CSharp.dll 等文件（不是 0 字节）
"@
}
$missing = @()
foreach ($n in $needInterop) {
    $p = Get-ChildItem -LiteralPath $interop -Filter $n -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $p) { $missing += "$n（不存在）" }
    elseif ($p.Length -eq 0) { $missing += "$n（0 字节，interop 生成失败）" }
}
if ($missing.Count -gt 0) {
    Fail @"
interop 不完整：
  $($missing -join "`n  ")

0 字节的 interop 说明 Il2CppInterop 生成时出过错：先删掉 $interop 再启动一次游戏，
然后看 BepInEx\LogOutput.log / LogOutput.log 的 Il2CppInterop 段落。
"@
}
$core = Join-Path $GameDir "BepInEx\core"
if (-not (Test-Path -LiteralPath (Join-Path $core "BepInEx.Unity.IL2CPP.dll"))) {
    Fail "找不到 BepInEx 6 core：$core（本作是 IL2CPP，不能用 BepInEx 5）"
}
Ok "interop + BepInEx 6 core 就绪"

# ============================================================
Step "2/6  获取 BepInEx $BepInExVersion 基础包（用于组装 package\）"
# 只用于打安装包；编译引用的是游戏目录里已装的那份。
$bepDir = Join-Path $mod "bep_dl"
$haveBep = Test-Path -LiteralPath (Join-Path $bepDir "BepInEx\core\BepInEx.Unity.IL2CPP.dll")
if ($haveBep) {
    Ok "已存在，跳过"
} else {
    $zip = $BepInExZip
    if (-not $zip) {
        $cands = @()
        if ($CacheDir) { $cands += (Join-Path $CacheDir "package\bepinex_be.zip") }
        $cands += "D:\DSHWorkSpace\noexistence_a11y\mod\package\bepinex_be.zip"
        $cands += (Join-Path $dl "BepInEx_win_x64_$BepInExVersion.zip")
        foreach ($c in $cands) { if ($c -and (Test-Path -LiteralPath $c)) { $zip = $c; break } }
    }
    if (-not $zip) {
        $zip = Join-Path $dl "BepInEx_win_x64_$BepInExVersion.zip"
        $url = "https://github.com/BepInEx/BepInEx/releases/download/v$BepInExVersion/BepInEx_win_x64_$BepInExVersion.zip"
        Write-Host "    下载 $url"
        Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing -TimeoutSec 300
    }
    Ok "基础包：$zip"
    if (Test-Path -LiteralPath $bepDir) { Remove-Item -LiteralPath $bepDir -Recurse -Force }
    Expand-Archive -LiteralPath $zip -DestinationPath $bepDir -Force
    if (-not (Test-Path -LiteralPath (Join-Path $bepDir "BepInEx\core\BepInEx.Unity.IL2CPP.dll"))) {
        Fail "基础包里没有 BepInEx\core\BepInEx.Unity.IL2CPP.dll —— 拿到的是 BepInEx 5 的包？本作需要 BepInEx 6"
    }
}
Ok "已解压"

# ============================================================
Step "3/6  获取 NVDA Controller Client (x64)"
$nvdaZip = Join-Path $dl "nvda_controllerClient.zip"
$nvdaDir = Join-Path $mod "nvda_dl"
$nvdaDll = $null
if (Test-Path -LiteralPath $nvdaDir) {
    $nvdaDll = Get-ChildItem $nvdaDir -Recurse -File -Filter "nvdaControllerClient.dll" |
        Where-Object { $_.DirectoryName -like "*x64*" } | Select-Object -First 1
}
if (-not $nvdaDll) {
    $cacheNvda = if ($CacheDir) { Join-Path $CacheDir "nvda_dl" } else { "" }
    # 本机已有的 x64 dll 直接复用（离线可用）；没有才下载
    $nvdaCands = @()
    if ($cacheNvda) { $nvdaCands += (Join-Path $cacheNvda "x64\nvdaControllerClient.dll") }
    $nvdaCands += "D:\DSHWorkSpace\noexistence_a11y\mod\package\nvdaControllerClient.dll"
    $localNvda = $nvdaCands | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1
    if ($localNvda) {
        New-Item -ItemType Directory -Force -Path (Join-Path $nvdaDir "x64") | Out-Null
        Copy-Item -LiteralPath $localNvda -Destination (Join-Path $nvdaDir "x64\nvdaControllerClient.dll") -Force
        Ok "复用本机已有 dll：$localNvda"
    } elseif ($cacheNvda -and (Test-Path -LiteralPath $cacheNvda)) {
        New-Item -ItemType Directory -Force -Path $nvdaDir | Out-Null
        Copy-Item (Join-Path $cacheNvda "*") $nvdaDir -Recurse -Force
    } else {
        if (-not (Test-Path -LiteralPath $nvdaZip)) {
            Invoke-WebRequest -Uri $NvdaClientUrl -OutFile $nvdaZip -UseBasicParsing -TimeoutSec 300
        }
        Expand-Archive -LiteralPath $nvdaZip -DestinationPath $nvdaDir -Force
    }
    $nvdaDll = Get-ChildItem $nvdaDir -Recurse -File -Filter "nvdaControllerClient.dll" |
        Where-Object { $_.DirectoryName -like "*x64*" } | Select-Object -First 1
}
if (-not $nvdaDll) { Fail "找不到 x64 的 nvdaControllerClient.dll" }
Ok $nvdaDll.FullName

# ============================================================
Step "4/6  发布前守卫 + 编译"

# 守卫 1：BepInPlugin 的版本号必须能被 System.Version 解析。
# 填 "0.1.0a" 这类带字母的写法，BepInEx 会判定 version is invalid 并**静默跳过整个插件**。
# IL2CPP 版 Plugin 用常量： [BepInPlugin(Guid, DisplayName, Version)]
# 所以版本号要从 const string Version = "..." 里取（模板里直接写字面量的是 Mono 版）。
$m = Select-String -LiteralPath $pluginCs -Pattern 'const\s+string\s+Version\s*=\s*"([^"]+)"' | Select-Object -First 1
if (-not $m) {
    # 兼容写法：特性里直接写字面量
    $m = Select-String -LiteralPath $pluginCs -Pattern 'BepInPlugin\([^)]*"([^"]+)"\s*\)' | Select-Object -First 1
}
if (-not $m) { Fail "在 Plugin.cs 里找不到版本号（既没有 const string Version，也没有 BepInPlugin 字面量）" }
$pluginVer = $m.Matches[0].Groups[1].Value
$parsed = $null
if (-not [System.Version]::TryParse($pluginVer, [ref]$parsed)) {
    Fail "BepInPlugin 版本号 `"$pluginVer`" 不是合法版本号（BepInEx 会跳过整个插件），改成纯数字如 0.1.0.1"
}
# 守卫 2：csproj <Version> 与 BepInPlugin 版本必须一致
try {
    $asmVer = ([xml](Get-Content -LiteralPath (Join-Path $srcDir "$pluginName.csproj") -Raw)).Project.PropertyGroup.Version |
        Where-Object { $_ } | Select-Object -First 1
    if ($asmVer -and $asmVer.Trim() -ne $pluginVer) {
        Fail "版本号不一致：csproj=$asmVer  BepInPlugin=$pluginVer（两处必须一致）"
    }
} catch { Write-Host "    （csproj 版本号读取失败，已跳过一致性检查）" -ForegroundColor Yellow }
Ok "版本号 $pluginVer（合法，且与 csproj 一致）"

# 守卫 3：编译。属性名必须与 csproj 里的 <ManosabaGameDir> 完全一致。
Push-Location $srcDir
try {
    dotnet build -c Release -v minimal -nowarn:MSB3277 -p:ManosabaGameDir="$GameDir"
    if ($LASTEXITCODE -ne 0) { Fail "编译失败（dotnet build 退出码 $LASTEXITCODE）" }
} finally { Pop-Location }
$pluginDll = Join-Path $srcDir "bin\Release\$pluginName.dll"
if (-not (Test-Path -LiteralPath $pluginDll)) { Fail "找不到编译产物：$pluginDll" }
Ok $pluginDll

# ============================================================
Step "5/6  部署到游戏（BepInEx\plugins\manosaba_a11y\）"
if ($NoDeploy) {
    Ok "已指定 -NoDeploy，跳过部署"
} else {
    # 游戏在跑时 DLL 被占用，拷贝会失败；等它退出（最多 180 秒）
    $deadline = (Get-Date).AddSeconds(180)
    $proc = Get-Process -Name "manosaba" -ErrorAction SilentlyContinue
    if ($proc) {
        Write-Host "    游戏正在运行，等待退出（DLL 被占用无法覆盖）..." -ForegroundColor Yellow
        while ($proc -and (Get-Date) -lt $deadline) {
            Start-Sleep -Seconds 3
            $proc = Get-Process -Name "manosaba" -ErrorAction SilentlyContinue
        }
        if ($proc) { Fail "游戏仍在运行，已放弃部署。关闭游戏后重跑，或加 -NoDeploy 只编译不部署。" }
    }
    $destDir = Join-Path $GameDir "BepInEx\plugins\manosaba_a11y"
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null
    Copy-Item -LiteralPath $pluginDll -Destination $destDir -Force
    Copy-Item -LiteralPath $nvdaDll.FullName -Destination $destDir -Force
    Ok "已部署：$destDir"
}

# ============================================================
Step "6/6  组装安装包 package\"
$pkg = Join-Path $mod "package"
# 保留自产的说明文件，其余重建
$keep = @("安装说明.md", "常见问题.md", "更新日志.md")
$saved = @{}
foreach ($k in $keep) {
    $p = Join-Path $pkg $k
    if (Test-Path -LiteralPath $p) { $saved[$k] = Get-Content -LiteralPath $p -Raw -Encoding UTF8 }
}
if (Test-Path -LiteralPath $pkg) { Remove-Item -LiteralPath $pkg -Recurse -Force }
New-Item -ItemType Directory -Force -Path $pkg | Out-Null
foreach ($k in $saved.Keys) {
    [System.IO.File]::WriteAllText((Join-Path $pkg $k), $saved[$k], (New-Object System.Text.UTF8Encoding $true))
}

Copy-Item (Join-Path $bepDir "*") -Destination $pkg -Recurse -Force

# 叠上游 PR#277 的 Il2CppInterop 修复版。
# 为什么默认会叠：interop 生成器在部分 IL2CPP 游戏上会生成出坏代理（PR#277），
# 修过的 Il2CppInterop.Runtime.dll 才能正确把托管数组/委托写回原生侧。
# 判据是**内容哈希**，不是版本号（两者 ProductVersion 都写 1.5.3，
#   只有 filever 的 +commit 后缀不同：基础包 dbda1cb / 修复版 f0634efa）。
# 顺序：-Il2CppInteropDll → 游戏目录里已装的那份 → 本机参考工程里的一份。
# 编译引用的是游戏目录的那份，安装包必须跟它一致，否则「编的时候是修复版、
# 装的时候是未修复版」这种不一致极难排查。
$iicDest = Join-Path $pkg "BepInEx\core\Il2CppInterop.Runtime.dll"
$iicCands = @()
if ($Il2CppInteropDll) { $iicCands += $Il2CppInteropDll }
$iicCands += (Join-Path $GameDir "BepInEx\core\Il2CppInterop.Runtime.dll")
$iicCands += "D:\DSHWorkSpace\noexistence_a11y\mod\package\Il2CppInterop.Runtime.dll"
$baseHash = ""
if (Test-Path -LiteralPath $iicDest) { $baseHash = (Get-FileHash -LiteralPath $iicDest -Algorithm SHA256).Hash }
$iicUsed = $null
foreach ($c in $iicCands) {
    if (-not $c -or -not (Test-Path -LiteralPath $c)) { continue }
    $h = (Get-FileHash -LiteralPath $c -Algorithm SHA256).Hash
    if ($h -ne $baseHash) { $iicUsed = $c; break }
}
if ($iicUsed) {
    Copy-Item -LiteralPath $iicUsed -Destination $iicDest -Force
    Ok "已叠加修复版 Il2CppInterop.Runtime.dll：$iicUsed"
} else {
    Ok "未找到内容不同的 Il2CppInterop.Runtime.dll，使用基础包自带（未修改）"
}

$plugDir = Join-Path $pkg "BepInEx\plugins\manosaba_a11y"
New-Item -ItemType Directory -Force -Path $plugDir | Out-Null
Copy-Item -LiteralPath $pluginDll -Destination $plugDir -Force
Copy-Item -LiteralPath $nvdaDll.FullName -Destination $plugDir -Force
# 同时放一份到包根：DllImport 会先查应用目录
Copy-Item -LiteralPath $nvdaDll.FullName -Destination (Join-Path $pkg "nvdaControllerClient.dll") -Force

# 本仓库的许可证与来源说明（MIT 要求随分发附许可文本，不能只放在仓库里）
$repoRoot = Split-Path -Parent $mod
foreach ($f in @("LICENSE", "NOTICE.md")) {
    $src = Join-Path $repoRoot $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $pkg $f) -Force
    } else {
        Write-Host "    警告：找不到 $f，包里将缺少它（发布前必须补上）" -ForegroundColor Yellow
    }
}
# 本模组自己的更新日志（原来包根的 changelog.txt 是 BepInEx 上游的，不是我们的）
$chg = Join-Path $repoRoot "CHANGELOG.md"
if (Test-Path -LiteralPath $chg) { Copy-Item -LiteralPath $chg -Destination (Join-Path $pkg "更新日志.md") -Force }
Ok "已附 LICENSE / NOTICE.md / 更新日志.md"

# 第三方许可证与来源说明（随二进制分发的合规要求）
$licDir = Join-Path $mod "licenses"
if (Test-Path -LiteralPath $licDir) {
    Copy-Item $licDir -Destination $pkg -Recurse -Force
    Ok "已附上 licenses\（$((Get-ChildItem $licDir -Recurse -File).Count) 个文件）"
} else {
    Write-Host "    警告：找不到 licenses\，安装包将缺少第三方许可证" -ForegroundColor Yellow
}

# 发布红线自检：interop / unity-libs / 本地配置绝不能进包
$bad = @()
foreach ($p in @("BepInEx\interop", "BepInEx\unity-libs", "BepInEx\config\$pluginGuid.cfg")) {
    if (Test-Path -LiteralPath (Join-Path $pkg $p)) { $bad += $p }
}
if ($bad.Count -gt 0) {
    Fail "安装包混入了不该分发的内容：`n  $($bad -join "`n  ")`n（interop 是机器相关的，配置是用户数据）"
}

$n = (Get-ChildItem $pkg -Recurse -File -Force).Count
Ok "安装包就绪：$pkg（$n 个文件）"

Write-Host "`n构建完成。安装：把 $pkg 里的东西全部拷进游戏根目录（有 manosaba.exe 的那一层）。`n" -ForegroundColor Cyan
