#!/usr/bin/env python3
"""Recon: dump Addressables bundle -> asset path container + object types.

Reads every .bundle under StreamingAssets/aa/StandaloneWindows64 and prints the
AssetBundle m_Container (asset path -> PPtr) plus object type/name inventory.
Output: evidence/aa_container.json  (machine readable)
        evidence/aa_container.txt   (human readable)
"""
import json
import os
import sys
import traceback

ROOT = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa\StandaloneWindows64"
OUT = r"D:\DSHWorkSpace\manosaba_a11y\evidence"

try:
    import UnityPy
except Exception as e:  # pragma: no cover
    print("[FATAL] UnityPy missing:", e)
    sys.exit(2)


def iter_bundles():
    for dirpath, _dirnames, filenames in os.walk(ROOT):
        for fn in sorted(filenames):
            if fn.endswith(".bundle"):
                yield os.path.join(dirpath, fn)


def container_of(env):
    """Return list of (path, path_id) from the AssetBundle object."""
    out = []
    for obj in env.objects:
        if obj.type.name != "AssetBundle":
            continue
        try:
            data = obj.read()
        except Exception:
            continue
        cont = getattr(data, "m_Container", None)
        if cont is None:
            continue
        items = cont.items() if isinstance(cont, dict) else cont
        for k, v in items:
            pid = getattr(v, "m_PathID", None)
            if pid is None and hasattr(v, "path_id"):
                pid = v.path_id
            out.append((str(k), pid))
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    report = {}
    errors = {}
    for i, path in enumerate(iter_bundles(), 1):
        rel = os.path.relpath(path, ROOT).replace("\\", "/")
        try:
            env = UnityPy.load(path)
            cont = container_of(env)
            objs = []
            for obj in env.objects:
                name = ""
                try:
                    name = obj.read().m_Name if obj.type.name in (
                        "TextAsset", "AudioClip", "MonoBehaviour", "MonoScript"
                    ) else ""
                except Exception:
                    name = ""
                objs.append({"type": obj.type.name, "path_id": obj.path_id, "name": name})
            report[rel] = {"container": cont, "objects": objs}
            print(f"[ok] {i:3d} {rel} assets={len(cont)} objs={len(objs)}")
        except Exception as e:
            errors[rel] = f"{type(e).__name__}: {e}"
            print(f"[ERR] {rel}: {e}")
            traceback.print_exc()
    with open(os.path.join(OUT, "aa_container.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, ensure_ascii=False, indent=1)
    with open(os.path.join(OUT, "aa_container.txt"), "w", encoding="utf-8") as f:
        for rel, data in report.items():
            f.write(f"### {rel}\n")
            for p, pid in data["container"]:
                f.write(f"  CONTAINER {p}  pid={pid}\n")
            for o in data["objects"]:
                f.write(f"  OBJ {o['type']:>16s} pid={o['path_id']} name={o['name']!r}\n")
            f.write("\n")
    if errors:
        with open(os.path.join(OUT, "aa_container_errors.txt"), "w", encoding="utf-8") as f:
            for k, v in errors.items():
                f.write(f"{k}\t{v}\n")
    print(f"bundles={len(report)} errors={len(errors)}")


if __name__ == "__main__":
    main()
