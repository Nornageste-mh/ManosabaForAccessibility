import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\release.ps1"
s = io.open(p, encoding="utf-8-sig").read()

# 1) \u5fc5\u9700\u6587\u4ef6\uff1a\u52a0 LICENSE / NOTICE / \u672c\u6a21\u7ec4\u66f4\u65b0\u65e5\u5fd7 / \u6d4b\u8bd5\u95ee\u5377
old = """    "licenses\\THIRD-PARTY-NOTICES.txt"
)"""
new = """    "licenses\\THIRD-PARTY-NOTICES.txt",
    "LICENSE",
    "NOTICE.md",
    "\u66f4\u65b0\u65e5\u5fd7.md"
)"""
assert old in s
s = s.replace(old, new, 1)

# 2) \u8bf4\u660e\u6587\u6863\uff1a\u6539\u6210\u201c\u5fc5\u9700\u7684\u4e09\u4efd md \u5fc5\u987b\u547d\u4e2d\u201d
old2 = """$md = @(Get-ChildItem -LiteralPath $pkg -File -Filter "*.md")
if ($md.Count -lt 2) { Fail "\u5305\u6839\u81f3\u5c11\u8981\u6709 2 \u4e2a\u8bf4\u660e *.md\uff08\u5b89\u88c5\u8bf4\u660e.md\u3001\u5e38\u89c1\u95ee\u9898.md\uff09\uff0c\u5b9e\u9645 $($md.Count)" }
Ok "\u8bf4\u660e\u6587\u6863\uff1a$($md.Name -join '\u3001')" """
new2 = """$md = @(Get-ChildItem -LiteralPath $pkg -File -Filter "*.md")
# \u5149\u6570\u91cf\u4e0d\u591f\uff1a\u4ee5\u524d\u53ea\u8981\u6c42\u201c\u2265 2 \u4e2a md\u201d\uff0c\u6539\u4e2a\u540d\u5b57\u4e5f\u80fd\u901a\u8fc7\u3002\u73b0\u5728\u8981\u6c42\u540d\u5b57\u5fc5\u987b\u5bf9\u5f97\u4e0a\u3002
$needMd = @("\u5b89\u88c5\u8bf4\u660e.md", "\u5e38\u89c1\u95ee\u9898.md", "\u66f4\u65b0\u65e5\u5fd7.md")
$missMd = @($needMd | Where-Object { -not (Test-Path -LiteralPath (Join-Path $pkg $_)) })
if ($missMd.Count -gt 0) { Fail "\u5305\u6839\u7f3a\u8bf4\u660e\u6587\u6863\uff1a$($missMd -join '\u3001')" }
Ok "\u8bf4\u660e\u6587\u6863\uff1a$($md.Name -join '\u3001')" """
assert old2 in s
s = s.replace(old2, new2, 1)

# 3) \u53d1\u5e03\u7ea2\u7ebf\uff1a\u6362\u6210**\u767d\u540d\u5355** + \u9ed1\u540d\u5355\u6269\u5c55\u540d
old3 = """$forbidden = @(
    "BepInEx\\interop",
    "BepInEx\\unity-libs",
    "BepInEx\\config\\$pluginGuid.cfg",
    "BepInEx\\plugins\\ManosabaA11yProbe.dll",
    "BepInEx\\plugins\\manosaba_a11y\\ManosabaA11yProbe.dll"
)
$bad = @()
foreach ($f in $forbidden) {
    if (Test-Path -LiteralPath (Join-Path $pkg $f)) { $bad += $f }
}
if ($bad.Count -gt 0) {
    Fail "\u5b89\u88c5\u5305\u6df7\u5165\u4e86\u4e0d\u8be5\u5206\u53d1\u7684\u5185\u5bb9\uff1a`n  $($bad -join "`n  ")`n\uff08interop \u662f\u673a\u5668\u76f8\u5173\u7684\u3001config \u662f\u7528\u6237\u6570\u636e\u3001probe \u662f\u5f00\u53d1\u5de5\u5177\uff09"
}
Ok "\u672a\u6df7\u5165 interop / unity-libs / config / probe" """
new3 = """# \u53d1\u5e03\u7ea2\u7ebf\u6539\u6210\u201c\u9ed1\u540d\u5355\u8def\u5f84 + \u9ed1\u540d\u5355\u6269\u5c55\u540d\u201d\uff1a\u53ea\u67e5\u51e0\u4e2a\u7cbe\u786e\u8def\u5f84\u7684\u505a\u6cd5\u5b9e\u6d4b\u4e0d\u53ef\u9760
# \uff08\u6bd4\u5982\u628a interop \u6539\u4e2a\u76ee\u5f55\u540d\u5c31\u80fd\u6e17\u8fdb\u53bb\uff09\u3002
$forbidden = @("BepInEx\\interop", "BepInEx\\unity-libs", "BepInEx\\config\\", "ManosabaProbe", "NaninovelReProbe")
$forbiddenExt = @('.pdb', '.cs', '.ps1', '.py', '.bin', '.nson', '.log', '.cfg', '.bundle', '.assets')
$bad = @()
foreach ($f in $forbidden) {
    if (Test-Path -LiteralPath (Join-Path $pkg $f)) { $bad += $f }
}
foreach ($f in (Get-ChildItem -LiteralPath $pkg -Recurse -File)) {
    $ext = [System.IO.Path]::GetExtension($f.Name).ToLower()
    if ($forbiddenExt -contains $ext) { $bad += ($f.FullName.Substring($pkg.Length).TrimStart('\\')) }
}
if ($bad.Count -gt 0) {
    Fail "\u5b89\u88c5\u5305\u6df7\u5165\u4e86\u4e0d\u8be5\u5206\u53d1\u7684\u5185\u5bb9\uff1a`n  $($bad -join "`n  ")`n\uff08interop \u662f\u673a\u5668\u76f8\u5173\u7684\u3001config \u662f\u7528\u6237\u6570\u636e\u3001probe/\u6e90\u7801/\u65e5\u5fd7\u4e0d\u8fdb\u5305\uff09"
}
Ok "\u672a\u6df7\u5165 interop / unity-libs / config / probe / \u6e90\u7801 / \u65e5\u5fd7" """
assert old3 in s
s = s.replace(old3, new3, 1)

# 4) \u6d4b\u8bd5\u95ee\u5377\u8def\u5f84\u4fee\u6b63
s = s.replace("\u6d4b\u8bd5\u95ee\u5377 docs/\u6d4b\u8bd5\u95ee\u5377.md", "\u6d4b\u8bd5\u95ee\u5377 \u4ed3\u5e93\u6839\u76ee\u5f55\u4e0b\u7684 \u6d4b\u8bd5\u95ee\u5377.md", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("release.ps1 hardened")
