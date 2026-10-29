#!/usr/bin/env python3
"""Probe one voice bundle: list AudioClip containers / names without decoding audio."""
import os
import sys

import UnityPy

AA = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa\StandaloneWindows64"
target = sys.argv[1] if len(sys.argv) > 1 else "general-voice-commonbad11_assets_all.bundle"
p = os.path.join(AA, target)
print("LOAD", target, flush=True)
env = UnityPy.load(p)
types = {}
for obj in env.objects:
    types[obj.type.name] = types.get(obj.type.name, 0) + 1
print("types:", types, flush=True)
shown = 0
for obj in env.objects:
    if obj.type.name != "AudioClip":
        continue
    cont = getattr(obj, "container", None)
    print(f"  clip pid={obj.path_id} container={cont!r}", flush=True)
    shown += 1
    if shown >= 12:
        break
# also try m_Name via read on first clip
for obj in env.objects:
    if obj.type.name == "AudioClip":
        try:
            d = obj.read()
            print("first clip name:", getattr(d, "m_Name", None), flush=True)
        except Exception as e:
            print("read err:", e, flush=True)
        break
