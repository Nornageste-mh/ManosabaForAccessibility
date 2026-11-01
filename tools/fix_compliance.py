import io
# 1) NOTICE.md\uff1a\u4fee\u6b63\u201c\u4e0d\u968f\u9644 NVDA\u201d\u4e0e\u6b7b\u94fe docs/licenses.md
p = r"D:\DSHWorkSpace\manosaba_a11y\NOTICE.md"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("| NVDA Controller Client | LGPL-2.1 | NV Access | \u672c\u4ed3\u5e93\u4e0d\u968f\u9644\uff1b\u7531\u4f7f\u7528\u65b9\u81ea\u884c\u53d6\u5f97 |",
              "| NVDA Controller Client | LGPL-2.1 | NV Access | **\u968f\u5305\u9644\u5e26** `nvdaControllerClient.dll`\uff08\u5305\u6839\u4e0e `BepInEx\\plugins\\manosaba_a11y\\` \u5404\u4e00\u4efd\uff09\uff0c\u8bb8\u53ef\u8bc1\u5168\u6587\u89c1 `licenses\\LGPL-2.1-NVDA-ControllerClient.txt` |", 1)
s = s.replace("\u9010\u6587\u4ef6\u5bf9\u5e94\u5173\u7cfb\u89c1 [`docs/licenses.md`](docs/licenses.md)\u3002",
              "\u9010\u6587\u4ef6\u5bf9\u5e94\u5173\u7cfb\u89c1 `licenses\\THIRD-PARTY-NOTICES.txt`\uff08\u5305\u5185\u540c\u540d\u6587\u4ef6\uff09\u3002", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("NOTICE.md fixed")

# 2) THIRD-PARTY-NOTICES\uff1a\u8865\u4e0a PR#277 \u7684\u5b9e\u9645\u6388\u6743\u72b6\u6001\uff08\u8865\u4e01\u4e0d\u968f\u5305\uff0c\u6539\u53d6\u4e0a\u6e38\u9884\u7f16\u8bd1\u7248\uff09
p2 = r"D:\DSHWorkSpace\manosaba_a11y\mod\licenses\THIRD-PARTY-NOTICES.txt"
t = io.open(p2, encoding="utf-8-sig").read()
old = " \u5b8c\u6574 diff\u3001\u63d0\u4ea4\u4fe1\u606f\u4e0e\u590d\u73b0\u6b65\u9aa4\u90fd\u5728\u540c\u76ee\u5f55\u7684 il2cppinterop-pr277.patch \u91cc\u3002"
new = (" \u2605 \u4fee\u6b63\uff082026-11-01\uff09\uff1a\u4e0a\u9762\u539f\u6765\u5199\u7684\u201c\u8865\u4e01\u968f\u5305\u201d\u4e0e\u5b9e\u9645\u4e0d\u7b26\u2014\u2014\n"
       "  \u5305\u91cc\u6ca1\u6709\uff08\u4e5f\u4e0d\u5e94\u8be5\u6709\uff09.il2cppinterop-pr277.patch\uff1a\u5b9e\u9645\u968f\u5305\u7684\u662f\n"
       "  \u4e0a\u6e38\u53d1\u5e03\u7684\u9884\u7f16\u8bd1 Il2CppInterop.Runtime.dll\uff08filever \u5e26 +commit f0634efa\uff09\uff0c\u800c\u975e\n"
       "  \u672c\u4ed3\u5e93\u81ea\u884c\u7f16\u8bd1\u7684\u4fee\u6539\u7248\u3002\u82e5\u672a\u6765\u6539\u6210\u81ea\u884c\u7f16\u8bd1\u7684 LGPL-3.0 \u4fee\u6539\u7248\uff0c\n"
       "  \u5fc5\u987b\u540c\u65f6\u63d0\u4f9b\u6e90\u7801\u4e0e\u8865\u4e01\u3002")
if old in t:
    t = t.replace(old, new, 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(t)
print("THIRD-PARTY-NOTICES patched")
