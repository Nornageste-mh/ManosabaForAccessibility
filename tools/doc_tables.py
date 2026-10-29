#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Emit markdown tables + odd-case samples used by the textassets docs."""
import json
import os
import re
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TXT = os.path.join(ROOT, "textassets")
A = json.load(open(os.path.join(TXT, "analysis.json"), encoding="utf-8"))
V = json.load(open(os.path.join(TXT, "voice-index.json"), encoding="utf-8"))
DOCS = os.path.join(TXT, "scripts", "zh-Hans")
TAGSTRIP = re.compile(r"<[^>]*>")

recs = []
for dirpath, _d, names in os.walk(DOCS):
    for n in sorted(names):
        if not n.endswith(".txt"):
            continue
        p = os.path.join(dirpath, n)
        rel = os.path.relpath(p, ROOT).replace("\\", "/")
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
print("records:", len(recs))
print("roles:", dict(collections.Counter(r["role"] for r in recs)))

print("\n### ASCII letters after stripping tags")
shown = 0
for r in recs:
    s = TAGSTRIP.sub("", r["text"])
    if re.search(r"[A-Za-z]", s):
        print(f'  {r["file"]}:{r["line"]} id={r["id"]} role={r["role"]} text={s[:80]!r}')
        shown += 1
        if shown >= 12:
            break
print("\n### ASCII digits after stripping tags")
shown = 0
for r in recs:
    s = TAGSTRIP.sub("", r["text"])
    if re.search(r"[0-9]", s):
        print(f'  {r["file"]}:{r["line"]} id={r["id"]} role={r["role"]} text={s[:80]!r}')
        shown += 1
        if shown >= 10:
            break

print("\n### print / toast samples (quoted-param form)")
for r in recs:
    if r["role"] in ("print", "toast"):
        print(f'  {r["role"]:8s} {r["file"]}:{r["line"]} ann={r["ann"][:110]!r} text={r["text"][:70]!r}')
        if r["role"] == "print" and r["line"] > 3000:
            pass

print("\n### per-act table")
acts = collections.defaultdict(lambda: collections.Counter())
for r in recs:
    act = r["file"].split("scripts/zh-Hans/")[1].split("/")[0]
    a = acts[act]
    a["files"] += 0
    a["lines"] += 1
    a["chars"] += len(r["text"])
    a[r["role"]] += 1
files_by_act = collections.Counter()
for dirpath, _d, names in os.walk(DOCS):
    for n in names:
        if n.endswith(".txt"):
            rel = os.path.relpath(os.path.join(dirpath, n), ROOT).replace("\\", "/")
            files_by_act[rel.split("scripts/zh-Hans/")[1].split("/")[0]] += 1
print("| act | files | lines | chars | dialogue | narration | choice | toast | print | printDebate |")
print("|---|---|---|---|---|---|---|---|---|---|")
tot = collections.Counter()
for act in sorted(acts):
    a = acts[act]
    print(f'| {act} | {files_by_act[act]} | {a["lines"]} | {a["chars"]} | {a["dialogue"]} | '
          f'{a["narration"]} | {a["choice"]} | {a["toast"]} | {a["print"]} | {a["printDebate"]} |')
    tot.update({k: a[k] for k in ("lines", "chars", "dialogue", "narration", "choice", "toast", "print", "printDebate")})
print(f'| **total** | {sum(files_by_act.values())} | {tot["lines"]} | {tot["chars"]} | {tot["dialogue"]} | '
      f'{tot["narration"]} | {tot["choice"]} | {tot["toast"]} | {tot["print"]} | {tot["printDebate"]} |')

print("\n### voice bundles table")
print("| bundle | clips | bytes |")
print("|---|---|---|")
for k in sorted(V["bundles"]):
    b = V["bundles"][k]
    print(f'| `{k}` | {b["clips"]} | {b["size"]:,} |')
print(f'| **total ({len(V["bundles"])})** | {sum(b["clips"] for b in V["bundles"].values())} | '
      f'{sum(b["size"] for b in V["bundles"].values()):,} |')

print("\n### speakers table (top 20)")
sp = A["speakers"]
zh = A["speaker_zh"]
print("| speaker id | zh display name | lines |")
print("|---|---|---|")
for k, v in sorted(sp.items(), key=lambda kv: -kv[1])[:20]:
    print(f'| `{k}` | {zh.get(k)} | {v} |')

print("\n### punctuation table")
print("| char | count |")
print("|---|---|")
for k, v in sorted(A["punct"].items(), key=lambda kv: -kv[1]):
    print(f'| `{k}` | {v} |')
print("\n### ascii table")
print("| char | count |")
print("|---|---|")
for k, v in sorted(A["ascii"].items(), key=lambda kv: -kv[1]):
    print(f'| `{k}` | {v} |')
print("\n### misc:", A["misc"])
print("### len:", A["len"])
print("### id_shape:", A["id_shape"])
print("### empty_by_role:", A["empty_by_role"], "empty:", A["empty"])
print("### voice_hit:", A["voice_hit"], "uniq:", A["voice_hit_uniq"], "missing:", A["voice_missing_uniq"])
print("### voice_by_role:", A["voice_by_role"], "no_voice_by_role:", A["no_voice_by_role"])
print("### annot_ok:", A["annot_ok"], "bad:", A["annot_bad_count"], A["annot_bad_sample"])
print("### multi_blocks:", A["multi_blocks"], "br_lines:", A["br_lines"], "join_violations:", A["join_violations"])
print("### link_targets:", A["link_targets"], A["link_target_sample"])
print("### colors:", A["colors"], "sizes:", A["sizes"])
print("### source:", json.dumps(A["source"], ensure_ascii=False)[:600])
