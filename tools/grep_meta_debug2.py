import re
p = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\il2cpp_data\Metadata\global-metadata.dat"
data = open(p, 'rb').read()

def find(pat, n=10, ctx=(80, 160)):
    out = [m.start() for m in re.finditer(re.escape(pat.encode()), data)]
    print("--- %s: %d hits" % (pat, len(out)))
    for off in out[:n]:
        s = max(0, off - ctx[0]); e = min(len(data), off + ctx[1])
        chunk = data[s:e]
        txt = ''.join(chr(c) if 32 <= c < 127 else '.' for c in chunk)
        print(hex(off), txt)

for pat in ["Ctrl+Alt+Shift", "Alt+Shift", "DebugMenu", "ToggleDebug", "toggleDebug",
            "DebugMode", "debugPanel", "DebugPanelPrefab", "debug", "ShowDebug", "OpenDebug"]:
    find(pat, n=6)
