# -*- coding: utf-8 -*-
"""Strip IL2CPP-interop boilerplate bodies from ilspycmd output, keep declarations.

Usage:
  python extract_sig.py <src_dir> <out_dir> [--index index.txt]

Produces one cleaned .txt per source .cs (declaration level only), plus an index.
"""
import os
import re
import sys

TYPE_KW = re.compile(r'\b(class|struct|interface|enum)\s+([A-Za-z_<>0-9.`]+)')
ATTR_RE = re.compile(r'^\s*\[')
NATIVE_FLD = re.compile(r'^\s*private static readonly (System\.)?IntPtr Native')
PROXY_FIELD = re.compile(r'^\s*static \w+\(\);$')


def next_nonblank(lines, i):
    j = i + 1
    while j < len(lines) and not lines[j].strip():
        j += 1
    return j


def match_brace(lines, i):
    """lines[i] is a line whose stripped content starts a block '{' somewhere;
    return index of line where depth returns to 0."""
    depth = 0
    k = i
    while k < len(lines):
        depth += lines[k].count('{') - lines[k].count('}')
        if depth == 0 and '{' in ''.join(lines[i:k + 1]):
            return k
        k += 1
    return len(lines) - 1


def is_compiler_name(name):
    return name.startswith('_') or name.startswith('<') or name.startswith('__') or name in ('c',)


def parse_block(lines, indent, out):
    i = 0
    while i < len(lines):
        raw = lines[i]
        s = raw.strip()
        if not s or s == '{' or s == '}':
            i += 1
            continue
        if s.startswith('//') or s.startswith('#'):
            i += 1
            continue
        nxt = next_nonblank(lines, i)
        has_body = nxt < len(lines) and lines[nxt].strip() == '{'
        if has_body:
            # find end of body
            end = match_brace(lines, nxt)
            m = TYPE_KW.search(s)
            if m:
                # nested type: recurse over its body
                out.append(indent + re.sub(r'\s+', ' ', s).strip())
                inner_start = nxt + 1
                parse_block(lines[inner_start:end], indent + '  ', out)
                i = end + 1
                continue
            # member declaration: may continue over lines between decl and '{'
            decl = raw.strip()
            cont = []
            for j in range(i + 1, nxt):
                t = lines[j].strip()
                if not t or t.startswith('['):
                    continue
                # drop ctor initializers ': this(...)' / ': base(...)'
                if t.startswith(': this(') or t.startswith(': base('):
                    continue
                cont.append(t)
            if cont:
                decl = decl + ' ' + ' '.join(cont)
            decl = re.sub(r'\s+', ' ', decl).strip()
            out.append(indent + decl)
            i = end + 1
            continue
        # no body: could be using/namespace/attribute/blank-field
        i += 1


def clean_file(path):
    with open(path, 'r', encoding='utf-8') as f:
        lines = f.read().splitlines()
    # record il2cpp identity (top level only)
    top_id = None
    for ln in lines:
        m = re.search(r'IL2CPP\.GetIl2CppClass\("([^"]+)", "([^"]*)", "([^"]+)"\)', ln)
        if m:
            top_id = m.groups()
            break
    # drop the using block and static intptr field declarations before parsing
    body = [ln for ln in lines if not ln.startswith('using ')]
    body = [ln for ln in body if not NATIVE_FLD.match(ln)]
    out = []
    parse_block(body, '', out)
    # post-filter noise
    kept = []
    for ln in out:
        t = ln.strip()
        if not t:
            continue
        if PROXY_FIELD.match(t):
            continue
        if re.match(r'^static \w+\(\);?$', t):
            continue
        if t.startswith(': this(') or t.startswith(': base('):
            continue
        # compiler-generated backing fields / closures at top of type: keep but mark
        kept.append(ln.rstrip())
    return top_id, kept


def summarize(kept):
    """One-line index summary: base, field names, method names."""
    base = ''
    fields = []
    methods = []
    for ln in kept:
        # indentation is normalised by parse_block: 0 = top level type,
        # 2 spaces = member of the top level type, 4+ = nested type member.
        depth_indent = len(ln) - len(ln.lstrip(' '))
        nested_type = bool(re.match(r'^(public|private|protected|internal)?\s*(static\s+)?(sealed\s+)?(abstract\s+)?'
                                    r'(new\s+)?(class|struct|interface|enum)\b', ln.strip()))
        if depth_indent == 0:
            if nested_type and ' : ' in ln.strip():
                base = ln.strip().split(' : ', 1)[1]
            continue
        if depth_indent != 2:
            continue
        t = ln.strip()
        if nested_type:
            continue
        if '(' in t:
            name = t.split('(')[0].split()
            methods.append(name[-1] if name else t)
        elif t:
            toks = t.rstrip('{ ').split()
            if toks:
                fields.append(toks[-1])
    return base, fields, methods


def main():
    src = sys.argv[1]
    outdir = sys.argv[2]
    index_path = None
    if '--index' in sys.argv:
        index_path = sys.argv[sys.argv.index('--index') + 1]
    os.makedirs(outdir, exist_ok=True)
    idx = []
    files = sorted(f for f in os.listdir(src) if f.endswith('.cs'))
    for f in files:
        name = f[:-3]
        path = os.path.join(src, f)
        top_id, kept = clean_file(path)
        text = []
        if top_id:
            text.append('[il2cpp] dll=%s ns=%s name=%s' % top_id)
        text.append('[source] %s' % f)
        text.append('')
        text.extend(kept)
        with open(os.path.join(outdir, name + '.txt'), 'w', encoding='utf-8') as o:
            o.write('\n'.join(text) + '\n')
        base, fields, methods = summarize(kept)
        idx.append('%s | base=%s | fields=%s | methods=%s' % (
            name, base,
            ','.join(fields[:40]),
            ','.join(methods[:80])))
    if index_path:
        with open(index_path, 'w', encoding='utf-8') as o:
            o.write('\n'.join(idx) + '\n')
    print('wrote %d signature files' % len(files))


if __name__ == '__main__':
    main()
