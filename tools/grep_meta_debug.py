import re
p = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\il2cpp_data\Metadata\global-metadata.dat"
data = open(p, 'rb').read()

def find(pat, n=12):
    out = [m.start() for m in re.finditer(re.escape(pat.encode()), data)]
    print("--- %s: %d hits" % (pat, len(out)))
    for off in out[:n]:
        s = max(0, off - 70); e = min(len(data), off + 140)
        chunk = data[s:e]
        txt = ''.join(chr(c) if 32 <= c < 127 else '.' for c in chunk)
        print(hex(off), txt)

for pat in ["SRDebug", "DebugPanel", "EnableDebugMode", "_longPressDuration", "DebugCanvas",
            "AllowDebug", "DebugOptions", "isDebugBuild", "debugMode", "EnableDeveloper",
            "DebugManager", "DebugDisplay", "KeyCode.F3", "F3"]:
    find(pat)
