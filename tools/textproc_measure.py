#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""TextProc-oriented measurements: openings, monologue parens, 【】, chunk lengths."""
import json
import os
import re
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TXT = os.path.join(ROOT, "textassets")
DOCS = os.path.join(TXT, "scripts", "zh-Hans")
A = json.load(open(os.path.join(TXT, "analysis.json"), encoding="utf-8"))
TAGSTRIP = re.compile(r"<[^>]*>")

recs = []
for dirpath, _d, names in os.walk(DOCS):
    for n in sorted(names):
        if not n.endswith(".txt"):
            continue
        p = os.path.join(dirpath, n)
        rel = os.path.relpath(p, ROOT).replace("\\", "/")
        if "/Debug/" in rel:
            continue
        cur = None
        for i, ln in enumerate(open(p, encoding="utf-8-sig").read().split("\n"), 1):
            if ln.startswith("# "):
                if cur:
                    recs.append(cur)
                cur = {"file": rel, "line": i, "id": ln[2:].strip(), "ann": None, "trans": []}
            elif cur is None:
                continue
            elif ln.startswith("; > "):
                cur["ann"] = ln
            elif ln.startswith("; ") or ln.strip() == "":
                pass
            else:
                cur["trans"].append(ln)
        if cur:
            recs.append(cur)
for r in recs:
    r["text"] = "".join(r["trans"])
    a = r["ann"] or ""
    m = re.match(r"^; > (@\w+)", a)
    if m:
        r["role"] = {"@choice": "choice", "@toast": "toast", "@print": "print",
                     "@printDebate": "printDebate"}.get(m.group(1), m.group(1))
    elif re.match(r"^; > [A-Za-z0-9_]+: ", a):
        r["role"] = "dialogue"
    elif a:
        r["role"] = "note"
    else:
        r["role"] = "narration"
    r["plain"] = TAGSTRIP.sub("", r["text"])

print("game records (non-Debug):", len(recs))
for role in ("dialogue", "narration", "print", "printDebate", "choice", "toast"):
    sub = [r for r in recs if r["role"] == role]
    if not sub:
        continue
    n = len(sub)
    print(f"\n[{role}] n={n}")
    print("  starts with （:", sum(1 for r in sub if r["plain"].startswith("（")))
    print("  starts with 【:", sum(1 for r in sub if r["plain"].startswith("【")))
    print("  contains 【】:", sum(1 for r in sub if "【" in r["plain"]))
    print("  contains ♡:", sum(1 for r in sub if "♡" in r["plain"]),
          " ♪:", sum(1 for r in sub if "♪" in r["plain"]))
    print("  starts with '…':", sum(1 for r in sub if r["plain"].startswith("…")))
    print("  starts with '—':", sum(1 for r in sub if r["plain"].startswith("—")))
    print("  ends with '？':", sum(1 for r in sub if r["plain"].rstrip().endswith("？")),
          " '！':", sum(1 for r in sub if r["plain"].rstrip().endswith("！")),
          " '……':", sum(1 for r in sub if r["plain"].rstrip().endswith("…")))
    ln = sorted(len(r["plain"]) for r in sub)
    print(f"  plain chars: p50={ln[n // 2]} p90={ln[int(n * 0.9)]} max={ln[-1]}")
    print("  >120 chars:", sum(1 for x in ln if x > 120), " >60:", sum(1 for x in ln if x > 60))

print("\n== openings (first 12 chars, top 15) ==")
op = collections.Counter(r["plain"][:2] for r in recs if r["plain"])
for k, v in op.most_common(15):
    print(f"  {k!r} {v}")
print("\n== closing (last 8 chars, top 15) ==")
cl = collections.Counter(r["plain"][-2:] for r in recs if r["plain"])
for k, v in cl.most_common(15):
    print(f"  {k!r} {v}")

print("\n== br position: leading <br> count ==")
print("  text starting with <br>:", sum(1 for r in recs if r["text"].startswith("<br>")))
print("  text ending with <br>:", sum(1 for r in recs if r["text"].rstrip().endswith("<br>")))
print("  consecutive <br><br>:", sum(1 for r in recs if "<br><br>" in r["text"]))
print("  leading/trailing spaces:", A["misc"]["leading_space"], A["misc"]["trailing_space"])

print("\n== u3000 usage samples ==")
s = 0
for r in recs:
    if "\u3000" in r["text"]:
        print(f'  {r["file"]}:{r["line"]} {r["text"][:70]!r}')
        s += 1
        if s >= 4:
            break
print("  blocks with u3000:", sum(1 for r in recs if "\u3000" in r["text"]),
      " total u3000 chars:", sum(r["text"].count("\u3000") for r in recs))

print("\n== link tag context ==")
for r in recs:
    if '<link="' in r["text"]:
        print(f'  {r["file"]}:{r["line"]} id={r["id"]} plain={r["plain"][:80]!r}')
        break
print("  lines with link:", sum(1 for r in recs if '<link="' in r["text"]))

print("\n== choice ids shape sample ==")
ch = [r["id"] for r in recs if r["role"] == "choice"]
print("  ", ch[:10])
print("== toast ids ==")
print("  ", sorted({r["id"] for r in recs if r["role"] == "toast"}))

print("\n== id prefix taxonomy (non-debug) ==")
pref = collections.Counter()
for i in {r["id"] for r in recs}:
    if re.match(r"^\d{4}[A-Za-z]", i):
        pref[i[:4] + "/" + re.match(r"^\d{4}([A-Za-z]+\d*)", i).group(1)] += 1
    elif i.startswith("~"):
        pref["~hash"] += 1
    else:
        pref[i] += 1
for k, v in sorted(pref.items(), key=lambda kv: -kv[1])[:20]:
    print(f"  {k} {v}")
