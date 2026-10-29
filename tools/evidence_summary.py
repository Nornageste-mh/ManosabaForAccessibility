#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Print the last evidence blocks needed by the textassets/*.md docs.

Read-only. Numbers come from textassets/analysis.json + voice-index + catalog.
"""
import json
import os
import re
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TXT = os.path.join(ROOT, "textassets")
A = json.load(open(os.path.join(TXT, "analysis.json"), encoding="utf-8"))
V = json.load(open(os.path.join(TXT, "voice-index.json"), encoding="utf-8"))

print("== roles ==")
for k, v in sorted(A["roles"].items(), key=lambda kv: -kv[1]):
    print(f"  {k:12s} {v}")

print("\n== ASCII after stripping markup (per role) ==")
TAGSTRIP = re.compile(r"<[^>]*>")
per_role = collections.Counter()
per_role_ascii = collections.Counter()
per_role_digit = collections.Counter()
for r in A.get("_records", []):
    pass
# re-parse docs quickly to test post-strip content
DOCS = os.path.join(TXT, "scripts", "zh-Hans")
for dirpath, _d, names in os.walk(DOCS):
    for n in sorted(names):
        if not n.endswith(".txt"):
            continue
        p = os.path.join(dirpath, n)
        block = None
        for i, ln in enumerate(open(p, encoding="utf-8-sig").read().split("\n"), 1):
            if ln.startswith("# "):
                if block:
                    t = block
                    stripped = TAGSTRIP.sub("", t)
                    per_role_ascii[bool(re.search(r"[A-Za-z]", stripped))] += 1
                    per_role_digit[bool(re.search(r"[0-9]", stripped))] += 1
                    per_role["total"] += 1
                block = ""
            elif block is not None and ln and not ln.startswith("; ") and ln.strip() != "":
                block += ln
        if block is not None:
            stripped = TAGSTRIP.sub("", block)
            per_role_ascii[bool(re.search(r"[A-Za-z]", stripped))] += 1
            per_role_digit[bool(re.search(r"[0-9]", stripped))] += 1
            per_role["total"] += 1
print("  blocks checked:", per_role["total"])
print("  blocks with ASCII letters after strip:", per_role_ascii[True])
print("  blocks with ASCII digits after strip:", per_role_digit[True])

print("\n== tag open/close counts in display text ==")
tags = collections.Counter()
for dirpath, _d, names in os.walk(DOCS):
    for n in names:
        if not n.endswith(".txt"):
            continue
        for ln in open(os.path.join(dirpath, n), encoding="utf-8-sig").read().split("\n"):
            if not ln or ln.startswith("; ") or ln.startswith("# "):
                continue
            for m in re.finditer(r"</?([a-zA-Z]+)", ln):
                tags[m.group(1).lower()] += 1
for k, v in sorted(tags.items(), key=lambda kv: -kv[1]):
    print(f"  {k:10s} {v}")

print("\n== per-act aggregation ==")
acts = collections.defaultdict(lambda: {"files": 0, "lines": 0, "chars": 0, "choice": 0,
                                        "dialogue": 0, "narration": 0, "toast": 0, "printDebate": 0})
for script, d in A["scripts"].items():
    act = script.split("/")[0] if script else "?"
    a = acts[act]
    a["files"] += 1
    for k in ("lines", "chars", "choice", "dialogue", "narration", "toast", "printDebate"):
        if k in d:
            a[k] += d[k]
for act in sorted(acts):
    print(" ", act, dict(acts[act]))

print("\n== voice bundles ==")
print("  bundles:", len(V["bundles"]), "clips:",
      sum(b["clips"] for b in V["bundles"].values()),
      "unique:", sum(b["unique_names"] for b in V["bundles"].values()))
sizes = {k: b["size"] for k, b in V["bundles"].items()}
print("  total bundle bytes:", sum(sizes.values()))
print("  clips per bundle min/max:", min(b["clips"] for b in V["bundles"].values()),
      max(b["clips"] for b in V["bundles"].values()))
print("  largest bundles:")
for k, b in sorted(V["bundles"].items(), key=lambda kv: -kv[1]["clips"])[:5]:
    print(f"    {k} clips={b['clips']} bytes={b['size']}")

print("\n== voice id prefixes ==")
prefix = collections.Counter()
for line in open(os.path.join(TXT, "voice-index.tsv"), encoding="utf-8"):
    parts = line.rstrip("\n").split("\t")
    if len(parts) < 3 or parts[2] == "clip_name":
        continue
    name = parts[2]
    prefix[name.split("_")[0]] += 1
for k, v in sorted(prefix.items(), key=lambda kv: -kv[1])[:25]:
    print(f"  {k:22s} {v}")

print("\n== catalog locale evidence ==")
cat = os.path.join(r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa", "catalog.bin")
b = open(cat, "rb").read()
for needle in (b"zh-Hans", b"zhhans", b"Localization/zh-Hans", b"Text/Scripts", b"Scripts/Act01"):
    print(f"  {needle!r}: {b.count(needle)}")
print("  catalog size:", len(b))
