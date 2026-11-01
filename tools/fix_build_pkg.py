import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\build.ps1"
s = io.open(p, encoding="utf-8-sig").read()

# 1) keep \u540d\u5355\u52a0\u4e0a\u66f4\u65b0\u65e5\u5fd7
s = s.replace('$keep = @("\u5b89\u88c5\u8bf4\u660e.md", "\u5e38\u89c1\u95ee\u9898.md")',
              '$keep = @("\u5b89\u88c5\u8bf4\u660e.md", "\u5e38\u89c1\u95ee\u9898.md", "\u66f4\u65b0\u65e5\u5fd7.md")', 1)

# 2) \u6253\u5305\u672b\u5c3e\u8865 LICENSE / NOTICE / \u66f4\u65b0\u65e5\u5fd7\uff08\u4ece\u4ed3\u5e93\u6839\u62f7\uff09
anchor = """# \u7b2c\u4e09\u65b9\u8bb8\u53ef\u8bc1\u4e0e\u6765\u6e90\u8bf4\u660e\uff08\u968f\u4e8c\u8fdb\u5236\u5206\u53d1\u7684\u5408\u89c4\u8981\u6c42\uff09"""
assert anchor in s
add = """# \u672c\u4ed3\u5e93\u7684\u8bb8\u53ef\u8bc1\u4e0e\u6765\u6e90\u8bf4\u660e\uff08MIT \u8981\u6c42\u968f\u5206\u53d1\u9644\u8bb8\u53ef\u6587\u672c\uff0c\u4e0d\u80fd\u53ea\u653e\u5728\u4ed3\u5e93\u91cc\uff09
$repoRoot = Split-Path -Parent $mod
foreach ($f in @("LICENSE", "NOTICE.md")) {
    $src = Join-Path $repoRoot $f
    if (Test-Path -LiteralPath $src) {
        Copy-Item -LiteralPath $src -Destination (Join-Path $pkg $f) -Force
    } else {
        Write-Host "    \u8b66\u544a\uff1a\u627e\u4e0d\u5230 $f\uff0c\u5305\u91cc\u5c06\u7f3a\u5c11\u5b83\uff08\u53d1\u5e03\u524d\u5fc5\u987b\u8865\u4e0a\uff09" -ForegroundColor Yellow
    }
}
# \u672c\u6a21\u7ec4\u81ea\u5df1\u7684\u66f4\u65b0\u65e5\u5fd7\uff08\u539f\u6765\u5305\u6839\u7684 changelog.txt \u662f BepInEx \u4e0a\u6e38\u7684\uff0c\u4e0d\u662f\u6211\u4eec\u7684\uff09
$chg = Join-Path $repoRoot "CHANGELOG.md"
if (Test-Path -LiteralPath $chg) { Copy-Item -LiteralPath $chg -Destination (Join-Path $pkg "\u66f4\u65b0\u65e5\u5fd7.md") -Force }
Ok "\u5df2\u9644 LICENSE / NOTICE.md / \u66f4\u65b0\u65e5\u5fd7.md"

"""
s = s.replace(anchor, add + anchor, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("build.ps1 packaging updated")
