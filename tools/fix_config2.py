import io, re
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()
# \u9010\u884c\u5220\uff1a\u627e\u5230 Bind \u5f00\u5934\u884c \u2192 \u5220\u5230\u8be5\u8bed\u53e5\u7ed3\u675f\uff08\u4ee5 ");" \u7ed3\u5c3e\u7684\u884c\uff09
names = ["CfgRealTimeSeconds", "CfgReadPhone =", "CfgReadPhoneSticker", "CfgAnnounceOnSkipStop"]
lines = s.split("\n")
out, i, removed = [], 0, []
while i < len(lines):
    ln = lines[i]
    if any(n in ln for n in names) and "Config.Bind" in ln:
        removed.append(ln.strip()[:40])
        while i < len(lines) and not lines[i].rstrip().endswith(");"):
            i += 1
        i += 1
        continue
    out.append(ln)
    i += 1
s = "\n".join(out)
# \u5b57\u6bb5\u58f0\u660e\u4e5f\u4e00\u8d77\u5220\uff08\u5426\u5219 4 \u4e2a\u96f6\u8bfb\u53d6\u5b57\u6bb5\u7167\u6837\u5b58\u5728\uff09
for f in ["CfgRealTimeSeconds", "CfgReadPhone", "CfgReadPhoneSticker", "CfgAnnounceOnSkipStop"]:
    s = re.sub(r"\n        internal static ConfigEntry<[^>]+> " + f + r";", "", s)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("removed:", removed)
