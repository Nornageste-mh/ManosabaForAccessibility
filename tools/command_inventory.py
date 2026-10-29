#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Clean command inventory from the raw MonoBehaviour payload dumps.

Re-parses every evidence/raw/src/*/*.bin with the SerializeReference token stream
(same layout as tools/extract_source_cmds.py) and tallies (TypeName, Namespace)
pairs whose namespace is a known Naninovel / game-side namespace. This filters the
false positives (line ids, labels, speaker names) that a raw substring scan picks up.

Outputs textassets/command-inventory.json + a digest on stdout.
Usage: python -u tools/command_inventory.py
"""
from __future__ import annotations

import collections
import glob
import json
import os
import re

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(PROJECT, "evidence", "raw", "src")
OUT = os.path.join(PROJECT, "textassets", "command-inventory.json")

NS_OK = re.compile(r"^(Naninovel\.[A-Za-z.]+|GigaCreation\.[A-Za-z.]+|WitchTrials\.[A-Za-z.]+)$")
NAME_OK = re.compile(r"^[A-Z][A-Za-z0-9]*$")
ASM_OK = re.compile(r"^[A-Za-z_][\w.]*$")


def u32(b: bytes, off: int) -> int:
    return int.from_bytes(b[off:off + 4], "little")


def read_str(b: bytes, off: int):
    if off + 4 > len(b):
        return None
    n = u32(b, off)
    if n < 1 or n > 400 or off + 4 + n > len(b):
        return None
    raw = b[off + 4: off + 4 + n]
    try:
        t = raw.decode("utf-8")
    except UnicodeDecodeError:
        return None
    if any(ord(c) < 0x20 and c != "\t" for c in t):
        return None
    nxt = off + 4 + n
    nxt += (4 - (n % 4)) % 4
    return t, nxt


def scan(b: bytes):
    out = []
    off, n = 0, len(b)
    while off + 12 <= n:
        r = read_str(b, off)
        if r:
            name, p1 = r
            r2 = read_str(b, p1)
            if r2:
                ns, p2 = r2
                r3 = read_str(b, p2)
                if r3:
                    asm, p3 = r3
                    if NAME_OK.match(name) and NS_OK.match(ns) and ASM_OK.match(asm):
                        out.append((name, ns))
                        off = p3
                        continue
        off += 1
    return out


def main() -> int:
    totals = collections.Counter()
    ns_totals = collections.Counter()
    per_script: dict[str, dict[str, int]] = {}
    debug = collections.Counter()
    game = collections.Counter()
    files = sorted(glob.glob(os.path.join(RAW, "*", "*.bin")))
    for p in files:
        name = os.path.basename(p)[:-4]
        hits = scan(open(p, "rb").read())
        c = collections.Counter(h[0] for h in hits)
        totals.update(c)
        ns_totals.update(h[1] for h in hits)
        per_script[name] = dict(sorted(c.items(), key=lambda kv: -kv[1]))
        (debug if name.startswith("Debug_") else game).update(c)
    out = {
        "scripts": len(files),
        "totals": dict(totals.most_common()),
        "namespaces": dict(ns_totals.most_common()),
        "game_scripts_only": dict(game.most_common()),
        "debug_scripts": dict(debug.most_common()),
        "per_script": per_script,
    }
    json.dump(out, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(f"scripts={len(files)} distinct_types={len(totals)} total_records={sum(totals.values())}")
    print("\n== namespaces ==")
    for k, v in ns_totals.most_common():
        print(f"  {k:40s} {v}")
    print("\n== command types (all scripts) ==")
    for k, v in totals.most_common():
        d = debug.get(k, 0)
        print(f"  {k:30s} {v:6d}  (debug {d})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
