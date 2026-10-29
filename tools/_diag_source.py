#!/usr/bin/env python3
"""Diagnose source .nani scripts: dump raw MonoBehaviour bytes without typetree parsing."""
import os
import sys

import UnityPy

AA = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa\StandaloneWindows64"
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "evidence", "raw")
os.makedirs(OUT, exist_ok=True)

name = sys.argv[1]
env = UnityPy.load(os.path.join(AA, name))
print("bundle", name, flush=True)
for obj in env.objects:
    try:
        cont = getattr(obj, "container", None)
        sz = getattr(obj, "byte_size", None)
        print(f"obj path_id={obj.path_id} type={obj.type.name} size={sz} container={cont!r}", flush=True)
        if obj.type.name != "MonoBehaviour":
            continue
        try:
            head = obj.object_reader.parse_monobehaviour_head()
            print("   head:", head, flush=True)
        except Exception as e:
            print("   head err:", type(e).__name__, e, flush=True)
        raw = obj.get_raw_data()
        print("   raw len:", len(raw), flush=True)
        base = (cont or f"pid{obj.path_id}").replace("/", "_").replace("#", "").replace("\\", "_")
        out = os.path.join(OUT, base + ".bin")
        with open(out, "wb") as f:
            f.write(raw)
        print("   wrote", out, flush=True)
    except Exception as e:
        print("obj err", type(e).__name__, e, flush=True)
