#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Locate exact display lines (file:line + text) for each inline-tag class.

Read-only over textassets/scripts/zh-Hans. Writes evidence/tag-examples.txt and
prints a short digest used by textassets/text-format.md.
"""
from __future__ import annotations

import glob
import os
import re
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(PROJECT, "textassets", "scripts", "zh-Hans")
OUT = os.path.join(PROJECT, "evidence", "tag-examples.txt")

CLASSES = {
    "br": re.compile(r"<br>"),
    "link": re.compile(r"<link=\""),
    "ruby": re.compile(r"<ruby=\""),
    "color_ff0000": re.compile(r"<color=#ff0000>"),
    "color_9c8eff": re.compile(r"<color=#9c8eff>"),
    "bold": re.compile(r"<b>"),
    "size": re.compile(r"<size="),
    "brackets": re.compile(r"【"),
    "paren_open": re.compile(r"^（"),
    "dash_start": re.compile(r"^—"),
    "ellipsis_start": re.compile(r"^…"),
    "heart": re.compile("♡"),
    "note": re.compile("♪"),
    "wave": re.compile("～"),
    "squares": re.compile(r"■"),
    "ideographic_space": re.compile("\u3000"),
}


def main() -> int:
    by_class: dict[str, list[tuple[str, int, str]]] = {k: [] for k in CLASSES}
    for path in sorted(glob.glob(os.path.join(DOCS, "**", "*.txt"), recursive=True)):
        rel = os.path.relpath(path, DOCS)
        if os.path.basename(path).startswith("Debug_"):
            continue
        lid = None
        ann = None
        for i, raw in enumerate(open(path, encoding="utf-8"), 1):
            line = raw.rstrip("\n")
            if line.startswith("# "):
                lid, ann = line[2:], None
                continue
            if line.startswith("; > "):
                ann = line[4:]
                continue
            if line.startswith(";") or not line.strip():
                continue
            for k, rx in CLASSES.items():
                if len(by_class[k]) < 6 and rx.search(line):
                    by_class[k].append((rel, i, f"{lid} | {ann or '(narration)'} | {line[:110]}"))
    with open(OUT, "w", encoding="utf-8") as f:
        for k in CLASSES:
            f.write(f"== {k}\n")
            for rel, i, txt in by_class[k]:
                f.write(f"  {rel}:{i}  {txt}\n")
            f.write("\n")
    for k in CLASSES:
        print(f"== {k}")
        for rel, i, txt in by_class[k][:3]:
            print(f"  {rel}:{i}  {txt}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
