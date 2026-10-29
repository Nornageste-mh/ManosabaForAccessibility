#!/usr/bin/env python3
"""Extract the voice-asset index (AudioClip names + container paths) from all
general-voice-* Addressables bundles of MANOSABA.

Outputs:
  textassets/voice-index.tsv   bundle<TAB>container<TAB>clip_name<TAB>path_id
  textassets/voice-index.json  summary per bundle (counts, errors)

Re-runnable: bundles already recorded in voice-index.json are skipped.
Usage: python -u tools/extract_voice.py [--force] [--only SUBSTR]
"""
from __future__ import annotations

import argparse
import json
import os
import sys
import traceback

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AA = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa\StandaloneWindows64"
OUT_DIR = os.path.join(PROJECT, "textassets")
TSV_PATH = os.path.join(OUT_DIR, "voice-index.tsv")
JSON_PATH = os.path.join(OUT_DIR, "voice-index.json")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--only", default=None)
    args = ap.parse_args()

    import UnityPy

    os.makedirs(OUT_DIR, exist_ok=True)
    state = {"aa_dir": AA, "bundles": {}, "no_container_clips": 0}
    if os.path.exists(JSON_PATH) and not args.force:
        with open(JSON_PATH, encoding="utf-8") as f:
            state = json.load(f)
        state.setdefault("bundles", {})

    names = sorted(
        n for n in os.listdir(AA) if n.startswith("general-voice") and n.endswith(".bundle")
    )
    if args.only:
        names = [n for n in names if args.only in n]
    # small first: validate the pipeline on cheap bundles before the 386 MB ones
    names.sort(key=lambda n: os.path.getsize(os.path.join(AA, n)))

    all_rows: list[tuple[str, str, str, str]] = []
    done = set()
    if os.path.exists(TSV_PATH) and not args.force:
        with open(TSV_PATH, encoding="utf-8") as f:
            for line in f:
                line = line.rstrip("\n")
                if not line or line.startswith("#"):
                    continue
                parts = line.split("\t")
                if len(parts) == 4:
                    all_rows.append(tuple(parts))  # type: ignore[arg-type]
                    done.add(parts[0])

    for i, name in enumerate(names, 1):
        if name in done and name in state["bundles"] and not args.force:
            print(f"[{i}/{len(names)}] skip {name}", flush=True)
            continue
        path = os.path.join(AA, name)
        size = os.path.getsize(path)
        print(f"[{i}/{len(names)}] {name} ({size/1e6:.1f} MB)", flush=True)
        try:
            env = UnityPy.load(path)
            clips = []
            for obj in env.objects:
                if obj.type.name != "AudioClip":
                    continue
                cont = getattr(obj, "container", None)
                cname = ""
                if cont:
                    cname = os.path.basename(cont).rsplit(".", 1)[0]
                else:
                    try:
                        cname = obj.read().m_Name
                    except Exception:
                        cname = ""
                    state["no_container_clips"] += 1
                clips.append((cont or "", cname, str(obj.path_id)))
            for cont, cname, pid in clips:
                all_rows.append((name, cont, cname, pid))
            state["bundles"][name] = {
                "size": size,
                "clips": len(clips),
                "unique_names": len({c[1] for c in clips}),
                "status": "ok",
            }
            print(f"    clips={len(clips)}", flush=True)
            del env
        except Exception as exc:  # noqa: BLE001 - keep going on per-bundle failures
            msg = f"{type(exc).__name__}: {exc}"
            print("    FAIL", msg, flush=True)
            traceback.print_exc()
            state["bundles"][name] = {"size": size, "clips": 0, "status": "fail", "error": msg}
        # incremental persist so a crash does not lose progress
        write_tsv(all_rows)
        with open(JSON_PATH, "w", encoding="utf-8") as f:
            json.dump(state, f, ensure_ascii=False, indent=2)

    write_tsv(all_rows)
    with open(JSON_PATH, "w", encoding="utf-8") as f:
        json.dump(state, f, ensure_ascii=False, indent=2)
    total = sum(v.get("clips", 0) for v in state["bundles"].values())
    print(f"DONE bundles={len(state['bundles'])} rows={len(all_rows)} clips_sum={total}", flush=True)
    return 0


def write_tsv(rows: list[tuple[str, str, str, str]]) -> None:
    with open(TSV_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write("bundle\tcontainer\tclip_name\tpath_id\n")
        for r in rows:
            f.write("\t".join(r) + "\n")


if __name__ == "__main__":
    sys.exit(main())
