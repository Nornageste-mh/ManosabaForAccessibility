#!/usr/bin/env python3
"""Diagnostic: try loading each candidate bundle one at a time, print progress.

Run with `python -u` so partial output survives a native crash.
"""
import os
import sys

import UnityPy

AA = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa\StandaloneWindows64"

kinds = ("general-localization-zhhans-scripts-", "general-localization-zhhans-text_",
         "naninovel-scripts_", "general-managedtext_")
files = [f for f in sorted(os.listdir(AA)) if f.endswith(".bundle") and f.startswith(kinds)]
print(f"candidates={len(files)}", flush=True)
for fn in files:
    print("LOAD", fn, flush=True)
    try:
        env = UnityPy.load(os.path.join(AA, fn))
        n = sum(1 for _ in env.objects)
        print("  ok objects=", n, flush=True)
        for obj in env.objects:
            if getattr(obj, "container", None):
                d = obj.read()
                t = getattr(d, "m_Script", None)
                print("   ", obj.type.name, "text_len=", len(t) if t else None, flush=True)
                break
    except Exception as e:
        print("  ERR", type(e).__name__, e, flush=True)
print("DONE", flush=True)
