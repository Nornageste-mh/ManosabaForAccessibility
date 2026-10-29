#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Print the concrete evidence lines cited in textassets/*.md (file + line + snippet).

Read-only. Reuses the same doc parser as analyze_text.py.
"""
import json
import os
import re
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TXT = os.path.join(ROOT, "textassets")
SCRIPTS = os.path.join(TXT, "scripts", "zh-Hans")

HDR = re.compile(r"^; (.+?) to (.+?) localization document for '(.+?)' Naninovel scenario script\s*$")
LABEL = re.compile(r"^#\s+(\S+)\s*$")
ANN_DLG = re.compile(r"^; > (.+?): \|#(.+?)\|\s*$")
ANN_CMD = re.compile(r"^; > (@\w+)\s*\|#(.+?)\|\s*$")


def parse():
    recs = []
    for dirpath, _d, names in os.walk(SCRIPTS):
        for n in sorted(names):
            if not n.endswith(".txt"):
                continue
            p = os.path.join(dirpath, n)
            rel = os.path.relpath(p, ROOT).replace("\\", "/")
            lines = open(p, encoding="utf-8-sig").read().split("\n")
            cur = None
            for idx, ln in enumerate(lines):
                m = LABEL.match(ln)
                if m:
                    if cur:
                        cur["text"] = "".join(t for _l, t in cur["trans"])
                        recs.append(cur)
                    cur = {"file": rel, "line": idx + 1, "id": m.group(1),
                           "ann": None, "ja": [], "trans": []}
                    continue
                if cur is None:
                    continue
                md = ANN_DLG.match(ln)
                mc = ANN_CMD.match(ln)
                if md:
                    cur["ann"] = ("speaker", md.group(1))
                elif mc:
                    cur["ann"] = ("cmd", mc.group(1))
                elif ln.startswith("; "):
                    cur["ja"].append((idx + 1, ln[2:]))
                elif ln.strip():
                    cur["trans"].append((idx + 1, ln))
            if cur:
                cur["text"] = "".join(t for _l, t in cur["trans"])
                recs.append(cur)
    return recs


def examples(recs, pred, n=3, width=100):
    out = []
    for r in recs:
        if pred(r):
            out.append(f'{r["file"]}:{r["line"]}  id={r["id"]}  {r["text"][:width]}')
            if len(out) >= n:
                break
    return out


def main():
    recs = parse()
    print("== role / annotation kinds ==")
    roles = collections.Counter()
    cmds = collections.Counter()
    for r in recs:
        if not r["ann"]:
            roles["narration"] += 1
        elif r["ann"][0] == "speaker":
            roles["dialogue"] += 1
        else:
            roles["cmd" + r["ann"][1]] += 1
            cmds[r["ann"][1]] += 1
    print(dict(roles), dict(cmds))

    print("\n== annotation command examples ==")
    for c in cmds:
        print(f'-- {c}')
        for line in examples(recs, lambda r, c=c: r["ann"] and r["ann"][1] == c, 4):
            print("  ", line)

    print("\n== first lines of a doc (header + 2 blocks) ==")
    p = os.path.join(TXT, "scripts", "zh-Hans", "Act01_Chapter01", "Act01_Chapter01_Trial00.txt")
    for i, ln in enumerate(open(p, encoding="utf-8-sig").read().split("\n")[:10], 1):
        print(f"  {os.path.relpath(p, ROOT)}:{i}: {ln}")

    print("\n== narration examples (no annotation) ==")
    for line in examples(recs, lambda r: not r["ann"], 3):
        print("  ", line)

    print("\n== choice examples ==")
    for line in examples(recs, lambda r: r["ann"] and r["ann"][1] == "@choice", 8, 60):
        print("  ", line)

    print("\n== toast examples ==")
    for line in examples(recs, lambda r: r["ann"] and r["ann"][1] == "@toast", 6, 80):
        print("  ", line)

    print("\n== printDebate examples ==")
    for line in examples(recs, lambda r: r["ann"] and r["ann"][1] == "@printDebate", 6, 120):
        print("  ", line)

    print("\n== <link> examples and target histogram ==")
    lt = collections.Counter()
    lidx = []
    for r in recs:
        for m in re.finditer(r"<link=\"([^\"]+)\">(.*?)</link>", r["text"]):
            lt[m.group(1)] += 1
            if len(lidx) < 6:
                lidx.append(f'{r["file"]}:{r["line"]}  {m.group(1)}  ->  {m.group(2)}')
    print("distinct targets:", len(lt), "total:", sum(lt.values()))
    print("target samples:", list(lt)[:8])
    for x in lidx:
        print("  ", x)

    print("\n== ruby examples ==")
    n = 0
    for r in recs:
        for m in re.finditer(r"<ruby=\"([^\"]*)\">([^<]*)</ruby>", r["text"]):
            print(f'  {r["file"]}:{r["line"]}  ruby="{m.group(1)}" base="{m.group(2)}"')
            n += 1
            break
        if n >= 8:
            break

    print("\n== color / b / size examples ==")
    for tag in ["color", "b", "size"]:
        c = 0
        for r in recs:
            if re.search(r"<" + tag + r"[=>]", r["text"]):
                print(f'  [{tag}] {r["file"]}:{r["line"]}  {r["text"][:110]}')
                c += 1
                if c >= 3:
                    break

    print("\n== text beginning with '<' (all, grouped) ==")
    starts = [r for r in recs if r["text"].lstrip().startswith("<")]
    print("count:", len(starts))
    kinds = collections.Counter(re.match(r"\s*<([a-zA-Z]+)", r["text"]).group(1) for r in starts)
    print("kinds:", dict(kinds))
    for r in starts[:5]:
        print("  ", f'{r["file"]}:{r["line"]}  {r["text"][:100]}')

    print("\n== lines containing '@' or '{' or '[' or '|' in display text ==")
    for ch in ["@", "{", "[", "|", "\\", "]"]:
        hit = [r for r in recs if ch in r["text"]]
        print(f"  {ch!r}: {len(hit)}")
        for r in hit[:2]:
            print("     ", f'{r["file"]}:{r["line"]}  {r["text"][:100]}')

    print("\n== length distribution by role ==")
    byrole = collections.defaultdict(list)
    for r in recs:
        k = "narration" if not r["ann"] else ("dialogue" if r["ann"][0] == "speaker" else r["ann"][1])
        byrole[k].append(len(r["text"]))
    for k, v in byrole.items():
        v = sorted(v)
        print(f"  {k:14s} n={len(v):6d} mean={sum(v)/len(v):6.1f} p50={v[len(v)//2]} p90={v[int(len(v)*0.9)]} max={v[-1]}")

    print("\n== longest lines ==")
    for r in sorted(recs, key=lambda r: -len(r["text"]))[:5]:
        print(f'  {len(r["text"]):4d} {r["file"]}:{r["line"]}  {r["text"][:90]}')

    print("\n== br by role ==")
    b = collections.Counter()
    for r in recs:
        k = "narration" if not r["ann"] else ("dialogue" if r["ann"][0] == "speaker" else r["ann"][1])
        b[k] += r["text"].count("<br>")
    print("total br by role:", dict(b))
    print("lines with br:", sum(1 for r in recs if "<br>" in r["text"]))

    print("\n== speaker id -> zh name map check ==")
    cn = {}
    p = os.path.join(TXT, "ui", "zh-Hans", "CharacterNames.txt")
    for ln in open(p, encoding="utf-8-sig"):
        m = re.match(r"^(.+?): (.*)$", ln.rstrip("\n"))
        if m:
            cn[m.group(1)] = m.group(2)
    spk = collections.Counter(r["ann"][1] for r in recs if r["ann"] and r["ann"][0] == "speaker")
    for k, v in spk.most_common():
        print(f'  {k:14s} {v:6d}  zh={cn.get(k)!r}')
    print("  missing zh:", [k for k in spk if k not in cn])

    print("\n== duplicate id examples ==")
    ids = collections.Counter(r["id"] for r in recs)
    for k, v in ids.items():
        if v > 1 and not k.startswith("~"):
            for r in recs:
                if r["id"] == k:
                    print(f'  {k} x{v}  e.g. {r["file"]}:{r["line"]}  {r["text"][:60]}')
                    break

    print("\n== empty translation examples ==")
    for r in recs:
        if not r["text"].strip():
            print(f'  {r["file"]}:{r["line"]}  id={r["id"]}  ann={r["ann"]}')
            if len([1]) and r["file"].count("Debug") == 0:
                pass
    print()
    emp = [r for r in recs if not r["text"].strip()]
    print("empty total", len(emp), "by file dir:", collections.Counter(
        os.path.basename(os.path.dirname(r["file"])) for r in emp))

    print("\n== hash ids (~hex) ==")
    h = [r for r in recs if r["id"].startswith("~")]
    print("count", len(h), "files", len(set(r["file"] for r in h)))
    print("files:", sorted(set(os.path.basename(os.path.dirname(r["file"])) for r in h)))
    for r in h[:5]:
        print("  ", f'{r["file"]}:{r["line"]}  {r["id"]}  {r["text"][:60]}')

    print("\n== fullwidth space / leading-char samples ==")
    fw = [r for r in recs if "\u3000" in r["text"]]
    print("lines with u3000:", len(fw))
    for r in fw[:4]:
        print("  ", f'{r["file"]}:{r["line"]}  {r["text"][:80]!r}')


if __name__ == "__main__":
    main()
