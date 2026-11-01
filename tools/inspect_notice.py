import io, re
# NOTICE.md: \u4fee\u6b63 NVDA \u4e0e PR#277 \u8865\u4e01\u7684\u4e0d\u5b9e\u63cf\u8ff0
p = r"D:\DSHWorkSpace\manosaba_a11y\NOTICE.md"
s = io.open(p, encoding="utf-8-sig").read()
old_hits = [l for l in s.split("\n") if "nvdaControllerClient" in l or "PR#277" in l or "il2cppinterop-pr277" in l or "docs/licenses.md" in l]
print("\n".join(old_hits))
