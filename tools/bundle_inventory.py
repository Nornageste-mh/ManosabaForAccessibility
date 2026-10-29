#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Print bundle inventory facts for asset-map.md (from textassets/index.json + aa dir)."""
import json
import os
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TXT = os.path.join(ROOT, "textassets")
AA = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa"
IDX = json.load(open(os.path.join(TXT, "index.json"), encoding="utf-8"))

print("index keys:", list(IDX.keys())[:8])
assets = IDX["assets"] if isinstance(IDX, dict) and "assets" in IDX else IDX
print("index entries:", len(assets))
by_bundle = collections.Counter(a["bundle"] for a in assets)
by_kind = collections.Counter(a.get("kind") for a in assets)
print("kinds:", dict(by_kind))
print("\n== bundles that provided extracted text assets (bundle: count) ==")
sizes = {}
if os.path.isdir(os.path.join(AA, "StandaloneWindows64")):
    for n in os.listdir(os.path.join(AA, "StandaloneWindows64")):
        sizes[n] = os.path.getsize(os.path.join(AA, "StandaloneWindows64", n))
tot = 0
for b, c in sorted(by_bundle.items()):
    sz = sizes.get(b, 0)
    tot += sz
    print(f"  {b}  assets={c}  bytes={sz:,}")
print(f"  TOTAL bytes of those bundles: {tot:,}")

print("\n== all aa bundle files grouped by prefix ==")
groups = collections.Counter()
gb = collections.Counter()
for n, sz in sizes.items():
    parts = n.split("_assets_")[0]
    groups[parts] += 1
    gb[parts] += sz
for g, c in sorted(groups.items()):
    print(f"  {g:60s} files={c:2d} bytes={gb[g]:,}")
print("  bundle files:", len(sizes), " total bytes:", sum(sizes.values()))
print("  non-bundle entries:", sorted(set(os.listdir(AA)) - {"StandaloneWindows64"}))

print("\n== naninovel source bundles ==")
for n in sorted(sizes):
    if n.startswith("naninovel-scripts"):
        print(f"  {n} {sizes[n]:,}")

print("\n== zh-Hans script bundle names ==")
for n in sorted(sizes):
    if "zhhans-scripts" in n:
        print(f"  {n} {sizes[n]:,}")

print("\n== zhhans text bundle ==")
for n in sorted(sizes):
    if "zhhans" in n and "scripts" not in n:
        print(f"  {n} {sizes[n]:,}")

print("\n== catalog locale strings ==")
cat = os.path.join(AA, "catalog.bin")
b = open(cat, "rb").read()
for needle in (b"zh-Hans", b"Localization/zh-Hans"):
    start = 0
    while True:
        i = b.find(needle, start)
        if i < 0:
            break
        print(f"  {needle!r}@{i}: {b[max(0,i-90):i+60]!r}")
        start = i + 1
