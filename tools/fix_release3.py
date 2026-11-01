import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\release.ps1"
s = io.open(p, encoding="utf-8-sig").read()

# 1) required \u6587\u4ef6
s = s.replace('    "licenses\\THIRD-PARTY-NOTICES.txt"\n)',
              '    "licenses\\THIRD-PARTY-NOTICES.txt",\n    "LICENSE",\n    "NOTICE.md",\n    "\u66f4\u65b0\u65e5\u5fd7.md"\n)', 1)

# 3) \u53d1\u5e03\u7ea2\u7ebf\u9ed1\u540d\u5355\uff08\u7cbe\u786e\u884c\u66ff\u6362\uff09
start = s.index('$forbidden = @(')
end = s.index('Ok "\u672a\u6df7\u5165 interop / unity-libs / config / probe"')
endlen = len('Ok "\u672a\u6df7\u5165 interop / unity-libs / config / probe"')
new_block = '''# \u53d1\u5e03\u7ea2\u7ebf\u6539\u6210\u201c\u9ed1\u540d\u5355\u8def\u5f84 + \u9ed1\u540d\u5355\u6269\u5c55\u540d\u201d\uff1a\u53ea\u67e5\u51e0\u4e2a\u7cbe\u786e\u8def\u5f84\u505a\u4e0d\u4f4f
# \uff08\u628a interop \u76ee\u5f55\u6539\u4e2a\u540d\u5c31\u80fd\u6e17\u8fdb\u5305\u91cc\uff09\u3002
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
Ok "\u672a\u6df7\u5165 interop / unity-libs / config / probe / \u6e90\u7801 / \u65e5\u5fd7"'''
s = s[:start] + new_block + s[end + endlen:]

# 4) \u6d4b\u8bd5\u95ee\u5377\u8def\u5f84
s = s.replace("\u6d4b\u8bd5\u95ee\u5377 docs/\u6d4b\u8bd5\u95ee\u5377.md", "\u6d4b\u8bd5\u95ee\u5377\uff08\u4ed3\u5e93\u6839\u76ee\u5f55\u4e0b\u7684 \u6d4b\u8bd5\u95ee\u5377.md\uff09", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("release.ps1 updated")
