#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Enumerate every annotation shape (`; > ...`) in the zh-Hans localization docs."""
import glob
import os
import re
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPTS = os.path.join(ROOT, "textassets", "scripts", "zh-Hans")

shapes = collections.Counter()
examples = collections.defaultdict(list)

for p in glob.glob(os.path.join(SCRIPTS, "**", "*.txt"), recursive=True):
    for ln in open(p, encoding="utf-8-sig"):
        ln = ln.rstrip("\n")
        if not ln.startswith("; > "):
            continue
        s = ln[4:]
        if re.fullmatch(r"[A-Za-z0-9_]+: \|#[^|]+\|", s):
            k = "DIALOGUE  Speaker: |#id|"
        elif re.fullmatch(r'@\w+ "?\|#[^|]+\|"?', s):
            k = 'CMDTEXT   @cmd "|#id|"'
        elif re.fullmatch(r'@\w+ "?\|#[^|]+\|"? .*', s):
            k = 'CMDTEXT+  @cmd "|#id|" <more params>'
        elif re.fullmatch(r"[^@|]+", s):
            k = "COMMENT   (free text)"
        else:
            k = "OTHER"
        shapes[k] += 1
        if len(examples[k]) < 200:
            examples[k].append((os.path.relpath(p, ROOT), s))

print("== annotation shape counts ==")
for k, v in shapes.most_common():
    print(f"  {v:7d}  {k}")

print("\n== examples per shape (deduped by skeleton) ==")
for k, items in examples.items():
    print("---", k)
    seen = set()
    for rel, s in items:
        sk = re.sub(r'"\|#[^|]+\|"', '"|#ID|"', s)
        sk = re.sub(r"\|#[^|]+\|", "|#ID|", sk)
        sk = re.sub(r"\d+", "N", sk)
        sk = re.sub(r"goto:[A-Za-z0-9_]+", "goto:LABEL", sk)
        if sk in seen:
            continue
        seen.add(sk)
        print("   ", rel, "|", s)
        if len(seen) >= 10:
            break

print("\n== command names appearing in annotations ==")
cmds = collections.Counter()
for k, items in examples.items():
    for _rel, s in items:
        m = re.match(r"^(@\w+)", s)
        if m:
            cmds[m.group(1)] += 1
print(dict(cmds))

print("\n== files with @choice annotations (count per file, top 10) ==")
per = collections.Counter()
for p in glob.glob(os.path.join(SCRIPTS, "**", "*.txt"), recursive=True):
    n = 0
    for ln in open(p, encoding="utf-8-sig"):
        if ln.startswith("; > @choice"):
            n += 1
    if n:
        per[os.path.relpath(p, ROOT)] = n
for k, v in per.most_common(10):
    print(f"  {v:4d}  {k}")
print("files with choices:", len(per), "total:", sum(per.values()))
