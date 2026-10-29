#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""List SerializeReference type tags whose assembly is Assembly-CSharp (game-side types).

The exported MonoBehaviour payloads are 4-byte aligned; type tags have the layout
[u32 name_len][name][pad][u32 ns_len][ns][pad][u32 asm_len][asm][pad]. This scans every
aligned offset for a three-string run and keeps the ones ending in Assembly-CSharp,
which the main command inventory filtered out (hyphenated assembly name).
"""
from __future__ import annotations

import collections
import glob
import os
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(PROJECT, "evidence", "raw", "src")


def pad4(n: int) -> int:
    return (4 - (n % 4)) % 4


def read3(b: bytes, off: int):
    """Try to read 3 consecutive length-prefixed, 4-byte-padded strings."""
    out = []
    p = off
    for _ in range(3):
        if p + 4 > len(b):
            return None
        n = int.from_bytes(b[p:p + 4], "little")
        if not (1 <= n <= 64) or p + 4 + n > len(b):
            return None
        raw = b[p + 4:p + 4 + n]
        if raw[0] < 0x20:
            return None
        try:
            s = raw.decode("utf-8")
        except UnicodeDecodeError:
            return None
        if any(c in "\x00\t" for c in s):
            return None
        p = p + 4 + n + pad4(n)
        out.append(s)
    return out, p


def main() -> int:
    counter: collections.Counter = collections.Counter()
    per_file: dict[str, collections.Counter] = {}
    for path in sorted(glob.glob(os.path.join(RAW, "*", "*.bin"))):
        b = open(path, "rb").read()
        c: collections.Counter = collections.Counter()
        off = 0
        while off + 12 <= len(b):
            r = read3(b, off)
            if r:
                (name, ns, asm), nxt = r
                if asm == "Assembly-CSharp":
                    c[(name, ns)] += 1
                    counter[(name, ns)] += 1
            off += 4
        if c:
            per_file[os.path.basename(path)] = c
    print(f"Assembly-CSharp type tags: {sum(counter.values())} hits / {len(counter)} distinct")
    for (name, ns), n in counter.most_common(40):
        print(f"  {n:6d}  {name:30s} {ns}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
