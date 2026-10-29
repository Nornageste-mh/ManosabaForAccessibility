#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Analyze extracted zh-Hans Naninovel localization documents.

Read-only over textassets/. Writes textassets/analysis.json + prints a digest.
Every number is derived from the extracted files; examples keep file+line refs.
"""
import json
import os
import re
import sys
import collections

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TXT = os.path.join(ROOT, "textassets")
SCRIPTS = os.path.join(TXT, "scripts", "zh-Hans")

HDR = re.compile(r"^; (.+?) to (.+?) localization document for '(.+?)' Naninovel scenario script\s*$")
LABEL = re.compile(r"^#\s+(\S+)\s*$")
# Any annotation line: source line with localizable text replaced by |#lineId|.
# Command params may precede/follow the placeholder (e.g. "button:...", quoted source text),
# so never anchor the placeholder to the start/end of the annotation.
ANN_ANY = re.compile(r"^; > (.*)$")
ANN_PH = re.compile(r"\|#(.+?)\|")
ANN_CMD = re.compile(r"^(@\w+)")
ANN_SPE = re.compile(r"^([A-Za-z0-9_]+): ")
TAG = re.compile(r"<([a-zA-Z]+)((?:=[^<>]*|=\"[^\"]*\")?)>")
LINK = re.compile(r"<link=\"([^\"]+)\">")
RUBY = re.compile(r"<ruby=\"([^\"]*)\">([^<]*)</ruby>")
COLOR = re.compile(r"<color=(#[0-9a-fA-F]{6,8})>")
SIZE = re.compile(r"<size=([^<>]+)>")

PUNCT = ["…", "——", "—", "，", "。", "！", "？", "（", "）", "「", "」", "、", "；", "：",
         "♡", "♪", "～", "〜", "・", "◆", "■", "☆", "★", "♥", "✕", "×", "→", "←", "↑", "↓"]
ASCII = ["!", "?", ",", ".", ":", ";", "~", "*", "&", "%", "$", "#", "@", "+", "=", "/", "|", "\\",
         "\"", "'", "(", ")", "[", "]", "{", "}", "^", "_", "`"]


def normlen(s):
    return len(s)


def main():
    voice_names = set()
    vpath = os.path.join(TXT, "voice-index.tsv")
    with open(vpath, encoding="utf-8") as f:
        next(f)
        for line in f:
            parts = line.rstrip("\n").split("\t")
            if len(parts) >= 3:
                voice_names.add(parts[2])

    files = []
    for dirpath, _dirs, names in os.walk(SCRIPTS):
        for n in names:
            if n.endswith(".txt"):
                files.append(os.path.join(dirpath, n))
    files.sort()

    recs = []
    headers = collections.Counter()
    for p in files:
        rel = os.path.relpath(p, ROOT).replace("\\", "/")
        lines = open(p, encoding="utf-8-sig").read().split("\n")
        h = HDR.match(lines[0]) if lines else None
        header = h.groups() if h else (None, None, None)
        headers[header[0] + " -> " + str(header[1])] += 1
        i = 1
        cur = None
        while i < len(lines):
            m = LABEL.match(lines[i])
            if m:
                if cur:
                    recs.append(cur)
                cur = {"file": rel, "line": i + 1, "id": m.group(1), "ann": None,
                       "ann_kind": None, "ja": [], "trans": [], "script": header[2]}
                i += 1
                continue
            ln = lines[i]
            if cur is None:
                i += 1
                continue
            md = ANN_ANY.match(ln)
            if md:
                rest = md.group(1).strip()
                ph = ANN_PH.search(rest)
                mcmd = ANN_CMD.match(rest)
                mspe = ANN_SPE.match(rest)
                if ph and mcmd:
                    cur["ann"] = ("cmd", mcmd.group(1), ph.group(1))
                    cur["ann_line"] = i + 1
                elif ph and mspe:
                    cur["ann"] = ("speaker", mspe.group(1), ph.group(1))
                    cur["ann_line"] = i + 1
                else:  # translator/source note, NOT display text
                    cur["note"] = rest
                    cur["note_line"] = i + 1
            elif ln.startswith("; "):
                cur["ja"].append((i + 1, ln[2:]))
            elif ln.strip() == "":
                cur["ja"].append((i + 1, ""))
            else:
                cur["trans"].append((i + 1, ln))
            i += 1
        if cur:
            recs.append(cur)

    # classify
    for r in recs:
        a = r["ann"]
        if a and a[0] == "speaker":
            r["role"] = "dialogue"
            r["speaker"] = a[1]
        elif a and a[0] == "cmd":
            r["role"] = {"@choice": "choice", "@toast": "toast", "@print": "print",
                         "@printDebate": "printDebate"}.get(a[1], "cmd:" + a[1])
            r["speaker"] = None
        elif r.get("note"):
            r["role"] = "note"
            r["speaker"] = None
        else:
            r["role"] = "narration"
            r["speaker"] = None
        # join: non-final physical lines already end with <br>; keep everything
        r["text"] = "".join(t for _l, t in r["trans"])
        r["multi"] = len(r["trans"])
        r["ja_text"] = "".join(t for _l, t in r["ja"])
        r["annot_ok"] = bool(a) and a[2] == r["id"]

    # ---- aggregates ----
    def cnt(key):
        c = collections.Counter()
        for r in recs:
            c[r[key]] += 1
        return c

    role = cnt("role")
    speakers = collections.Counter(r["speaker"] for r in recs if r["speaker"])
    ann_kinds = collections.Counter(r["ann"][1] if r["ann"] and r["ann"][0] == "cmd" else None for r in recs)
    ann_ok = sum(1 for r in recs if r["annot_ok"])
    ann_bad = [r for r in recs if r["ann"] and not r["annot_ok"]]

    zh_chars = sum(len(r["text"]) for r in recs)
    ja_chars = sum(len(r["ja_text"]) for r in recs)
    empty = [r for r in recs if r["text"].strip() == ""]
    empty_by_role = collections.Counter(r["role"] for r in empty)
    multi = [r for r in recs if r["multi"] > 1]
    br = sum(r["text"].count("<br>") for r in recs)
    br_lines = sum(1 for r in recs if "<br>" in r["text"])
    # join check: every continuation line except last ends with <br>
    viol = 0
    for r in recs:
        for idx, (_l, t) in enumerate(r["trans"][:-1]):
            if not t.endswith("<br>"):
                viol += 1

    ids = collections.Counter(r["id"] for r in recs)
    dup_ids = {k: v for k, v in ids.items() if v > 1}
    voice_hit = sum(1 for r in recs if r["id"] in voice_names)
    uniq_ids = set(ids)
    voice_hit_uniq = sum(1 for i in uniq_ids if i in voice_names)
    voice_by_role = collections.Counter()
    no_voice_by_role = collections.Counter()
    for r in recs:
        (voice_by_role if r["id"] in voice_names else no_voice_by_role)[r["role"]] += 1

    # tags
    tag_kind = collections.Counter()
    tag_examples = collections.defaultdict(list)
    link_targets = collections.Counter()
    link_texts = []
    ruby_pairs = []
    colors = collections.Counter()
    sizes = collections.Counter()
    for r in recs:
        t = r["text"]
        for mm in TAG.finditer(t):
            kind = mm.group(1).lower()
            tag_kind[kind] += 1
            if len(tag_examples[kind]) < 3:
                tag_examples[kind].append({"file": r["file"], "line": r["line"],
                                           "tag": mm.group(0), "text": t[:70]})
        for mm in LINK.finditer(t):
            link_targets[mm.group(1)] += 1
        for mm in RUBY.finditer(t):
            if len(ruby_pairs) < 8:
                ruby_pairs.append({"file": r["file"], "line": r["line"],
                                   "base": mm.group(2), "ruby": mm.group(1), "text": t[:70]})
        for mm in COLOR.finditer(t):
            colors[mm.group(1)] += 1
        for mm in SIZE.finditer(t):
            sizes[mm.group(1)] += 1

    # examples of each role
    role_ex = collections.defaultdict(list)
    for r in recs:
        if len(role_ex[r["role"]]) < 3 and r["text"].strip():
            role_ex[r["role"]].append({"file": r["file"], "line": r["line"], "id": r["id"],
                                       "text": r["text"][:80]})

    # choice / toast id sets
    choice_recs = [r for r in recs if r["role"] == "choice"]
    toast_recs = [r for r in recs if r["role"] == "toast"]
    other_cmd = [r for r in recs if r["role"].startswith("cmd:")]

    # id shape taxonomy
    im = collections.Counter()
    id_re = re.compile(r"^(?P<actch>\d{4})(?P<script>[A-Za-z]+\d*)_(?P<who>[A-Za-z]+)(?P<num>\d+)$")
    id_re2 = re.compile(r"^(?P<act>[A-Za-z]+)(?P<script>[A-Za-z]+\d*)_(?P<who>[A-Za-z]+)(?P<num>\d+)$")
    id_re3 = re.compile(r"^~[0-9a-f]+$")
    for i in uniq_ids:
        if id_re.match(i):
            im["A: <actch4><Script>_<Who><NNN>"] += 1
        elif id_re3.match(i):
            im["HASH: ~hex (auto id)"] += 1
        elif id_re2.match(i):
            im["B: <Named>_<Who><NNN>"] += 1
        else:
            im["C: other"] += 1
    hash_ids = [i for i in uniq_ids if i.startswith("~")]

    # char / punctuation
    pun = collections.Counter()
    asc = collections.Counter()
    for r in recs:
        t = r["text"]
        for p in PUNCT:
            n = t.count(p)
            if n:
                pun[p] += n
        for a in ASCII:
            n = t.count(a)
            if n:
                asc[a] += n
    misc = {
        "fullwidth_space_u3000": sum(r["text"].count("\u3000") for r in recs),
        "tab": sum(r["text"].count("\t") for r in recs),
        "nbsp": sum(r["text"].count("\u00a0") for r in recs),
        "zwsp": sum(r["text"].count("\u200b") for r in recs),
        "cr": sum(r["text"].count("\r") for r in recs),
        "line_break_literal": sum(r["text"].count("\\n") for r in recs),
        "leading_space": sum(1 for r in recs if r["text"][:1] in (" ", "\u3000")),
        "trailing_space": sum(1 for r in recs if r["text"][-1:] in (" ", "\u3000")),
    }
    leading_lt = [r for r in recs if r["text"].lstrip().startswith("<")]
    paren_narr = sum(1 for r in recs if r["text"].startswith("（"))

    lens = [len(r["text"]) for r in recs if r["text"]]
    lens.sort()

    def pct(p):
        if not lens:
            return 0
        k = max(0, min(len(lens) - 1, int(round(p / 100.0 * (len(lens) - 1)))))
        return lens[k]

    # per-file stats
    per_file = []
    by_script = collections.defaultdict(lambda: {"lines": 0, "chars": 0, "choice": 0,
                                                 "dialogue": 0, "narration": 0, "toast": 0,
                                                 "files": 0, "voice": 0})
    for r in recs:
        d = by_script[r["script"]]
        d["lines"] += 1
        d["chars"] += len(r["text"])
        if r["role"] in ("choice", "toast", "dialogue", "narration"):
            d[r["role"]] += 1
        if r["id"] in voice_names:
            d["voice"] += 1
    for p in files:
        rel = os.path.relpath(p, ROOT).replace("\\", "/")
        lines = open(p, encoding="utf-8-sig").read().split("\n")
        h = HDR.match(lines[0]) if lines else None
        per_file.append({"file": rel, "script": h.group(3) if h else None, "phys_lines": len(lines)})
    for s in by_script:
        by_script[s]["files"] = sum(1 for f in per_file if f["script"] == s)

    # speaker map from CharacterNames
    charmap = {}
    cn = os.path.join(TXT, "ui", "zh-Hans", "CharacterNames.txt")
    if os.path.exists(cn):
        for line in open(cn, encoding="utf-8-sig"):
            mm = re.match(r"^(.+?): (.*)$", line.rstrip("\n"))
            if mm:
                charmap[mm.group(1)] = mm.group(2)

    # source reconciliation
    src = None
    sc = os.path.join(TXT, "source-commands.json")
    if os.path.exists(sc):
        src = json.load(open(sc, encoding="utf-8"))

    out = {
        "files": len(files),
        "records": len(recs),
        "unique_ids": len(uniq_ids),
        "rare_headers": {k: v for k, v in headers.items() if v < len(files)},
        "headers": dict(headers),
        "roles": dict(role),
        "speakers": dict(speakers),
        "speaker_zh": {k: charmap.get(k) for k in speakers},
        "ann_kinds": {str(k): v for k, v in ann_kinds.items()},
        "annot_ok": ann_ok,
        "annot_bad_count": len(ann_bad),
        "annot_bad_sample": [{"file": r["file"], "line": r["line"], "id": r["id"]} for r in ann_bad[:5]],
        "zh_chars": zh_chars,
        "ja_chars": ja_chars,
        "empty": len(empty),
        "empty_by_role": dict(empty_by_role),
        "empty_sample": [{"file": r["file"], "line": r["line"], "id": r["id"], "role": r["role"]} for r in empty[:8]],
        "multi_blocks": len(multi),
        "br_total": br,
        "br_lines": br_lines,
        "join_violations": viol,
        "dup_ids": dup_ids,
        "voice_hit": voice_hit,
        "voice_hit_uniq": voice_hit_uniq,
        "voice_missing_uniq": len(uniq_ids) - voice_hit_uniq,
        "voice_by_role": dict(voice_by_role),
        "no_voice_by_role": dict(no_voice_by_role),
        "tags": dict(tag_kind),
        "tag_examples": {k: v for k, v in tag_examples.items()},
        "link_targets": len(link_targets),
        "link_target_sample": list(link_targets)[:8],
        "ruby": ruby_pairs,
        "ruby_kind": collections.Counter(
            ("latin" if re.fullmatch(r"[A-Za-z0-9 ]+", p["ruby"]) else "cjk") for p in ruby_pairs),
        "colors": dict(colors),
        "sizes": dict(sizes),
        "role_examples": {k: v for k, v in role_ex.items()},
        "choice_count": len(choice_recs),
        "toast_count": len(toast_recs),
        "other_cmd_count": len(other_cmd),
        "other_cmd_sample": [{"file": r["file"], "line": r["line"], "id": r["id"], "role": r["role"]} for r in other_cmd[:8]],
        "id_shape": dict(im),
        "hash_ids": len(hash_ids),
        "hash_sample": hash_ids[:5],
        "punct": dict(pun),
        "ascii": dict(asc),
        "misc": misc,
        "leading_lt": len(leading_lt),
        "leading_lt_sample": [{"file": r["file"], "line": r["line"], "text": r["text"][:90]} for r in leading_lt[:6]],
        "paren_narr": paren_narr,
        "len": {"min": lens[0] if lens else 0, "max": lens[-1] if lens else 0,
                "p50": pct(50), "p90": pct(90), "p99": pct(99),
                "gt120": sum(1 for x in lens if x > 120), "gt200": sum(1 for x in lens if x > 200),
                "gt400": sum(1 for x in lens if x > 400), "overall": sum(lens)},
        "scripts": dict(by_script),
        "source": {k: v for k, v in (src or {}).items() if k in ("line_kinds", "commands", "command_ns", "total")},
        "per_file_count": len(per_file),
    }
    with open(os.path.join(TXT, "analysis.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)

    def show(*args):
        print(" ".join(str(a) for a in args))

    show("files", out["files"])
    show("records", out["records"])
    show("unique_ids", out["unique_ids"])
    show("roles", out["roles"])
    show("zh_chars", out["zh_chars"])
    show("empty", out["empty"])
    show("empty_by_role", out["empty_by_role"])
    show("annot_ok", out["annot_ok"], )
    show("annot_bad", out["annot_bad_count"])
    show("multi_blocks", out["multi_blocks"])
    show("br_total", out["br_total"], )
    show("join_violations", out["join_violations"])
    show("dup_ids", dup_ids)
    show("voice_hit", out["voice_hit"], "uniq", out["voice_hit_uniq"], "missing", out["voice_missing_uniq"])
    show("voice_by_role", out["voice_by_role"])
    show("no_voice_by_role", out["no_voice_by_role"])
    show("tags", out["tags"])
    show("link_targets", out["link_targets"])
    show("ruby_kind", out["ruby_kind"])
    show("colors", out["colors"])
    show("sizes", out["sizes"])
    show("choice_count", out["choice_count"], "toast_count", out["toast_count"])
    show("other_cmd", out["other_cmd_count"], out["other_cmd_sample"])
    show("id_shape", out["id_shape"])
    show("punct", out["punct"])
    show("ascii", out["ascii"])
    show("misc", misc)
    show("leading_lt", out["leading_lt"], )
    show("len", out["len"])
    print("--- role examples ---")
    for k, v in out["role_examples"].items():
        print(k, json.dumps(v[:2], ensure_ascii=False))
    print("--- tag examples ---")
    for k, v in out["tag_examples"].items():
        print(k, json.dumps(v[:2], ensure_ascii=False))
    print("--- leading_lt samples ---")
    for s in out["leading_lt_sample"]:
        print(json.dumps(s, ensure_ascii=False))
    print("--- source line kinds ---")
    print(json.dumps(out["source"].get("line_kinds"), ensure_ascii=False))


if __name__ == "__main__":
    main()
