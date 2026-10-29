#!/usr/bin/env python3
"""Extract source .nani payloads (raw MonoBehaviour bytes) and enumerate
Naninovel script-line kinds + command types used by each scenario script.

Why raw: UnityPy cannot build a type tree for these IL2CPP MonoBehaviour payloads
in this build (native crash 0xC0000005), so ObjectReader.get_raw_data() is used and
the Unity SerializeReference records are parsed directly. Each SerializeReference
element starts with three length-prefixed strings:

    uint32 len, "<TypeName>", pad, uint32 len, "<Namespace>", pad, uint32 len, "<Assembly>", pad

Script lines are ("TextScriptLine"|"GenericTextScriptLine"|"CommandScriptLine"|
"LabelScriptLine"|"CommentScriptLine"|"EmptyScriptLine", "Naninovel",
"Elringus.Naninovel.Runtime"); commands are ("<CommandName>",
"Naninovel.Commands", "Elringus.Naninovel.Runtime") or game-side namespaces.

Outputs:
  evidence/raw/src/<bundle>/<ScriptName>.bin   raw payload per script asset
  textassets/source-commands.json              per-script line-kind + command histogram
Usage: python -u tools/extract_source_cmds.py [--no-dump]
"""
from __future__ import annotations

import collections
import hashlib
import json
import os
import re
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AA = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa\StandaloneWindows64"
RAW_DIR = os.path.join(PROJECT, "evidence", "raw", "src")
OUT_JSON = os.path.join(PROJECT, "textassets", "source-commands.json")

LINE_KINDS = {
    "TextScriptLine",
    "GenericTextScriptLine",
    "CommandScriptLine",
    "LabelScriptLine",
    "CommentScriptLine",
    "EmptyScriptLine",
}
LINE_ID_RE = re.compile(rb"(?<![0-9A-Za-z_])(\d{4}(?:Adv|Trial|Bad|Narrative)?\d*_[A-Za-z]+[0-9]+)(?![0-9A-Za-z_])")


def u32(b: bytes, off: int) -> int:
    return int.from_bytes(b[off:off + 4], "little")


def read_str(b: bytes, off: int) -> tuple[str, int] | None:
    """Read a Unity serialized string at off; return (text, next_offset)."""
    if off + 4 > len(b):
        return None
    n = u32(b, off)
    if n < 1 or n > 400 or off + 4 + n > len(b):
        return None
    raw = b[off + 4: off + 4 + n]
    try:
        t = raw.decode("utf-8")
    except UnicodeDecodeError:
        return None
    if any(ord(c) < 0x20 and c not in "\t" for c in t):
        return None
    nxt = off + 4 + n
    nxt += (4 - (n % 4)) % 4
    return t, nxt


def scan_triples(b: bytes) -> list[tuple[str, str, str, int]]:
    """All SerializeReference type headers in payload order: (name, ns, asm, offset)."""
    out = []
    off = 0
    n = len(b)
    while off + 12 <= n:
        r = read_str(b, off)
        if r:
            name, p1 = r
            r2 = read_str(b, p1)
            if r2:
                ns, p2 = r2
                r3 = read_str(b, p2)
                if r3:
                    asm, _p3 = r3
                    # namespaces/assemblies are dotted identifiers or bare words; require both
                    # to look like technical identifiers to avoid capturing dialogue text
                    if re.fullmatch(r"[A-Za-z_][\w.]*", ns) and re.fullmatch(r"[A-Za-z_][\w.]*", asm) \
                            and re.fullmatch(r"[A-Za-z_][\w.]*", name):
                        out.append((name, ns, asm, off))
                        off = _p3
                        continue
        off += 1
    return out


def main() -> int:
    import UnityPy

    dump = "--no-dump" not in sys.argv
    bundles = sorted(n for n in os.listdir(AA) if n.startswith("naninovel-scripts") and n.endswith(".bundle"))
    result: dict = {"bundles": {}, "scripts": {}, "line_kind_totals": {}, "command_totals": {}, "errors": []}
    line_totals: collections.Counter = collections.Counter()
    cmd_totals: collections.Counter = collections.Counter()
    for bi, bundle in enumerate(bundles, 1):
        bdir = os.path.join(RAW_DIR, bundle.replace(".bundle", ""))
        if dump:
            os.makedirs(bdir, exist_ok=True)
        print(f"[{bi}/{len(bundles)}] {bundle}", flush=True)
        try:
            env = UnityPy.load(os.path.join(AA, bundle))
        except Exception as exc:  # noqa: BLE001
            result["errors"].append({"bundle": bundle, "error": f"load: {exc}"})
            print("  load fail", exc, flush=True)
            continue
        binfo = {"objects": 0, "monobehaviours": 0, "bytes": 0}
        for obj in env.objects:
            binfo["objects"] += 1
            if obj.type.name != "MonoBehaviour":
                continue
            binfo["monobehaviours"] += 1
            cont = getattr(obj, "container", None) or f"pid{obj.path_id}"
            try:
                raw = obj.get_raw_data()
            except Exception as exc:  # noqa: BLE001
                result["errors"].append({"bundle": bundle, "container": cont, "error": f"raw: {exc}"})
                continue
            binfo["bytes"] += len(raw)
            name = os.path.splitext(os.path.basename(cont))[0]
            if dump:
                with open(os.path.join(bdir, name + ".bin"), "wb") as f:
                    f.write(raw)
            triples = scan_triples(raw)
            kinds = collections.Counter()
            cmds = collections.Counter()
            for tname, tns, tasm, _off in triples:
                if tns == "Naninovel" and tname in LINE_KINDS:
                    kinds[tname] += 1
                elif tname not in LINE_KINDS:
                    cmds[tname] += 1
            line_totals.update(kinds)
            cmd_totals.update(cmds)
            result["scripts"][name] = {
                "bundle": bundle,
                "container": cont,
                "bytes": len(raw),
                "sha1": hashlib.sha1(raw).hexdigest(),
                "line_kinds": dict(sorted(kinds.items())),
                "commands": dict(sorted(cmds.items())),
                "command_total": sum(cmds.values()),
            }
        result["bundles"][bundle] = binfo
        print(f"  mbs={binfo['monobehaviours']} bytes={binfo['bytes']}", flush=True)

    result["line_kind_totals"] = dict(sorted(line_totals.items(), key=lambda kv: -kv[1]))
    result["command_totals"] = dict(sorted(cmd_totals.items(), key=lambda kv: -kv[1]))
    with open(OUT_JSON, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=1)
    print("LINE KINDS:", json.dumps(result["line_kind_totals"], ensure_ascii=False), flush=True)
    print("COMMANDS :", json.dumps(result["command_totals"], ensure_ascii=False), flush=True)
    print("scripts:", len(result["scripts"]), "errors:", len(result["errors"]), flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
