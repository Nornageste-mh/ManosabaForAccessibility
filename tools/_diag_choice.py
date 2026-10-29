#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Cross-check doc choice/printDebate counts against source payload markers."""
import glob
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BINS = glob.glob(os.path.join(ROOT, "evidence", "raw", "src", "*", "*.bin"))
DOCS = glob.glob(os.path.join(ROOT, "textassets", "scripts", "zh-Hans", "**", "*.txt"), recursive=True)

doc_by_name = {os.path.basename(p)[:-4]: p for p in DOCS}


def doc_counts(name):
    p = doc_by_name.get(name)
    if not p:
        return None
    s = open(p, encoding="utf-8-sig").read()
    c = {}
    for cmd in ("@choice", "@toast", "@print", "@printDebate"):
        c[cmd] = s.count("; > " + cmd)
    return c


rows = []
for p in BINS:
    name = os.path.basename(p)[:-4]
    b = open(p, "rb").read()
    marks = {k: b.count(k.encode()) for k in
             ("AddChoice", "StopForChoice", "ShowToastUI", "PrintCommand", "PrintDebateCommand",
              "PlayVoice", "Wait", "ResetText", "AddChoiceCommand")}
    d = doc_counts(name)
    if d is None:
        continue
    rows.append((name, len(b), marks, d))

print("== sample scripts ==")
for name, size, marks, d in rows[:6]:
    print(name, size, marks, d)

tot = {k: sum(r[2][k] for r in rows) for k in rows[0][2]}
print("\n== totals over %d scripts ==" % len(rows))
print(tot)
print("doc annotations:", {k: sum(r[3][k] for r in rows) for k in ("@choice", "@toast", "@print", "@printDebate")})

# ratio AddChoice vs doc choice per script: any script where they match?
match = [r for r in rows if r[2]["AddChoice"] == r[3]["@choice"]]
print("\nscripts where AddChoice == doc @choice:", len(match))
print("scripts where StopForChoice == doc @choice:", len([r for r in rows if r[2]["StopForChoice"] == r[3]["@choice"]]))
for r in rows:
    if r[2]["AddChoice"] > 20:
        print("  big-choice script:", r[0], "AddChoice", r[2]["AddChoice"], "doc", r[3]["@choice"])
