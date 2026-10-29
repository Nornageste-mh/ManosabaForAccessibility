#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Voice coverage check: which doc lines have a same-named AudioClip?

Scans textassets/scripts/zh-Hans/**.txt (the zh-Hans Naninovel localization documents)
block by block and matches `# <lineId>` against textassets/voice-index.tsv clip names.
Prints per-role coverage and concrete file:line samples of lines with / without voice.
Read-only; no game files touched.
"""
from __future__ import annotations

import collections
import glob
import os
import re
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(PROJECT, "textassets", "scripts", "zh-Hans")
TSV = os.path.join(PROJECT, "textassets", "voice-index.tsv")

LABEL = re.compile(r"^# (\S+)\s*$")
ANN = re.compile(r"^; > (.*)$")
PH = re.compile(r"\|#(.+?)\|")


def load_clips() -> set[str]:
    out = set()
    with open(TSV, encoding="utf-8") as f:
        next(f)
        for line in f:
            p = line.rstrip("\n").split("\t")
            if len(p) >= 3:
                out.add(p[2])
    return out


def role_of(ann: str | None) -> str:
    if not ann:
        return "narration"
    m = re.match(r"^@(\w+)", ann)
    if m:
        return m.group(1)
    if re.match(r"^[A-Za-z0-9_]+: ", ann):
        return "dialogue"
    return "narration"  # free-text translator note block, still printed


def parse_blocks(path: str):
    """Yield (line_no, id, ann) for each doc block that has display text."""
    cur_id = None
    cur_line = 0
    ann = None
    started = False
    for i, raw in enumerate(open(path, encoding="utf-8"), 1):
        line = raw.rstrip("\n")
        m = LABEL.match(line)
        if m:
            cur_id, cur_line, ann, started = m.group(1), i, None, False
            continue
        if cur_id is None:
            continue
        a = ANN.match(line)
        if a and not started:
            ann = a.group(1)
            continue
        if line.startswith(";"):
            continue
        if not line.strip():
            if started:
                cur_id, ann, started = None, None, False
            continue
        if not started:
            started = True
            yield cur_line, cur_id, ann


def main() -> int:
    clips = load_clips()
    files = sorted(glob.glob(os.path.join(DOCS, "**", "*.txt"), recursive=True))
    per_role = collections.Counter()
    hit = collections.Counter()
    samples_hit: dict[str, list[tuple[str, int, str]]] = collections.defaultdict(list)
    samples_miss: dict[str, list[tuple[str, int, str]]] = collections.defaultdict(list)
    ids: set[str] = set()
    for path in files:
        rel = os.path.relpath(path, DOCS)
        if os.path.basename(path).startswith("Debug_"):
            continue
        for lno, lid, ann in parse_blocks(path):
            ids.add(lid)
            role = role_of(ann)
            per_role[role] += 1
            if lid in clips:
                hit[role] += 1
                if len(samples_hit[role]) < 3:
                    samples_hit[role].append((rel, lno, lid))
            elif len(samples_miss[role]) < 3:
                samples_miss[role].append((rel, lno, lid))
    print(f"gamescript docs scanned: {len(files)}  unique ids: {len(ids)}  clips: {len(clips)}")
    print(f"ids with clip: {len(ids & clips)}  ids without: {len(ids - clips)}")
    print("\nrole | blocks | with voice | coverage")
    for r in sorted(per_role, key=lambda r: -per_role[r]):
        n, h = per_role[r], hit[r]
        print(f"  {r:12s} {n:6d} {h:6d}  {100.0 * h / n:5.1f}%")
    print("\n== samples: WITH voice ==")
    for r in sorted(samples_hit):
        for rel, lno, lid in samples_hit[r]:
            print(f"  {r:12s} {rel}:{lno} id={lid}")
    print("\n== samples: WITHOUT voice ==")
    for r in sorted(samples_miss):
        for rel, lno, lid in samples_miss[r]:
            print(f"  {r:12s} {rel}:{lno} id={lid}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
