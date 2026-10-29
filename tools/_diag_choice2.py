#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Reconcile doc-localized command counts with source payload command markers."""
import glob
import os
import re
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BINS = sorted(glob.glob(os.path.join(ROOT, "evidence", "raw", "src", "*", "*.bin")))
DOCS = glob.glob(os.path.join(ROOT, "textassets", "scripts", "zh-Hans", "**", "*.txt"), recursive=True)
doc_by_name = {os.path.basename(p)[:-4]: p for p in DOCS}


def annot_counts(name):
    p = doc_by_name.get(name)
    if not p:
        return None
    s = open(p, encoding="utf-8-sig").read()
    out = collections.Counter()
    for ln in s.split("\n"):
        if not ln.startswith("; > "):
            continue
        rest = ln[4:]
        m = re.match(r"^(@\w+)", rest)
        if m:
            out[m.group(1)] += 1
        elif re.match(r"^[A-Za-z0-9_]+: \|#", rest):
            out["Speaker"] += 1
    return out


rows = []
for p in BINS:
    name = os.path.basename(p)[:-4]
    b = open(p, "rb").read()
    ac = annot_counts(name) or collections.Counter()
    rows.append({
        "name": name,
        "debug": name.startswith("Debug_"),
        "add": b.count(b"AddChoice"),
        "stop": b.count(b"StopForChoice"),
        "doc_choice": ac.get("@choice", 0),
        "toast": b.count(b"ShowToastUI"),
        "doc_toast": ac.get("@toast", 0),
        "debate": b.count(b"PrintDebate"),
        "doc_debate": ac.get("@printDebate", 0),
        "print": b.count(b"PrintCommand"),
        "doc_print": ac.get("@print", 0),
        "voice": b.count(b"PlayVoice"),
        "lines": sum(ac.values()),
        "doc_all": sum(ac.values()),
    })

for key in ("add", "stop", "doc_choice", "toast", "doc_toast", "debate", "doc_debate", "voice"):
    tot = sum(r[key] for r in rows)
    dbg = sum(r[key] for r in rows if r["debug"])
    print(f"{key:12s} total={tot:6d}  debug={dbg:5d}  game={tot - dbg:6d}")

print("\n== scripts with most unlocalized AddChoice ==")
for r in sorted(rows, key=lambda r: -(r["add"] - r["doc_choice"]))[:12]:
    print(f'  {r["name"]:40s} add={r["add"]:4d} stop={r["stop"]:4d} doc={r["doc_choice"]:4d}')

print("\n== game-script totals (non-Debug) ==")
g = [r for r in rows if not r["debug"]]
print("scripts", len(g), "sum doc annotations", sum(r["doc_all"] for r in g))
print("AddChoice", sum(r["add"] for r in g), "StopForChoice", sum(r["stop"] for r in g),
      "doc @choice", sum(r["doc_choice"] for r in g), "ratio stop/doc",
      sum(r["stop"] for r in g) / max(1, sum(r["doc_choice"] for r in g)))
print("ShowToastUI", sum(r["toast"] for r in g), "doc @toast", sum(r["doc_toast"] for r in g))
print("PrintDebate", sum(r["debate"] for r in g), "doc @printDebate", sum(r["doc_debate"] for r in g))
print("PlayVoice", sum(r["voice"] for r in g))
