#!/usr/bin/env python3
"""Extract Naninovel script text assets from the manosaba Addressables bundles.

Sources (bundle glob) and their internal asset paths:

  general-localization-zhhans-scripts-*  -> Assets/#WitchTrials/Localization/zh-Hans/Text/Scripts/<Act>/<Name>.txt
      zh-Hans localized Naninovel script text (TextAsset.m_Script == .nani text body)

  general-localization-zhhans-text_*     -> Assets/#WitchTrials/Localization/zh-Hans/Text/{DefaultUI,CustomUI,CharacterNames,Locales}.txt
      zh-Hans managed-text (UI labels, character display names)

  naninovel-scripts_*                    -> Assets/#WitchTrials/Scenarios/<Act>/<Name>.nani
      original (source-language) Naninovel.Script assets, kept for the locale comparison only

  general-managedtext_*                  -> Assets/#WitchTrials/ManagedText/*.txt  (source-language UI text)

Outputs (all under textassets/, which is .gitignore'd):
  textassets/scripts/zh-Hans/<Act>/<Name>.txt
  textassets/scripts/source/<Act>/<Name>.nani
  textassets/ui/zh-Hans/<Name>.txt
  textassets/ui/source/<Name>.txt
  textassets/index.json   (asset path, bundle, path_id, byte length, sha1)

Re-runnable: overwrites outputs, deterministic ordering.
"""
import hashlib
import json
import os
import re
import sys
import traceback

try:
    import UnityPy
except Exception as e:  # pragma: no cover
    print("[FATAL] UnityPy missing:", e)
    sys.exit(2)

AA = r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\StreamingAssets\aa\StandaloneWindows64"
PROJ = r"D:\DSHWorkSpace\manosaba_a11y"
OUT = os.path.join(PROJ, "textassets")
SCRIPTS = os.path.join(OUT, "scripts")
UI = os.path.join(OUT, "ui")
INDEX = os.path.join(OUT, "index.json")

# Reading the source-language Naninovel Script assets (MonoBehaviour with an IL2CPP
# typetree) hard-crashes UnityPy 1.25.0 (0xC0000005) on this build, so it is opt-in.
INCLUDE_SOURCE = "--with-source" in sys.argv

RE_SCRIPT_PATH = re.compile(
    r"^Assets/#WitchTrials/(?:Localization/zh-Hans/Text/Scripts|Scenarios)/(?P<rest>.+)$")
RE_UI_PATH = re.compile(
    r"^Assets/#WitchTrials/(?:Localization/zh-Hans/Text|ManagedText)/(?P<rest>.+)$")


def bundles():
    for fn in sorted(os.listdir(AA)):
        if not fn.endswith(".bundle"):
            continue
        if fn.startswith("general-localization-zhhans-scripts-"):
            yield fn, "zh-Hans-script"
        elif fn.startswith("general-localization-zhhans-text_"):
            yield fn, "zh-Hans-ui"
        elif fn.startswith("naninovel-scripts_") and INCLUDE_SOURCE:
            yield fn, "source-script"
        elif fn.startswith("general-managedtext_"):
            yield fn, "source-ui"


def safe_write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def read_text_asset(obj):
    """Return (container_path, text) for a TextAsset."""
    data = obj.read()
    txt = getattr(data, "m_Script", None)
    if txt is None:
        return None, None
    if isinstance(txt, bytes):
        txt = txt.decode("utf-8-sig", errors="replace")
    return obj.container, txt


def read_naninovel_script(obj):
    """Naninovel.Script is a ScriptableObject; text lives in `scriptText` (or similar)."""
    data = obj.read()
    for attr in ("scriptText", "ScriptText", "text", "m_Text"):
        v = getattr(data, attr, None)
        if isinstance(v, str) and v:
            return obj.container, v
    # fall back to any long string field
    best = None
    for k, v in vars(data).items():
        if isinstance(v, str) and len(v) > (len(best[1]) if best else 20):
            best = (k, v)
    if best:
        return obj.container, best[1]
    return None, None


def main():
    os.makedirs(OUT, exist_ok=True)
    index = []
    errors = []
    counts = {"zh-Hans-script": 0, "zh-Hans-ui": 0, "source-script": 0, "source-ui": 0}

    for fn, kind in bundles():
        path = os.path.join(AA, fn)
        try:
            env = UnityPy.load(path)
        except Exception as e:
            errors.append({"bundle": fn, "error": f"{type(e).__name__}: {e}"})
            continue
        for obj in env.objects:
            cont = getattr(obj, "container", None)
            if not cont:
                continue
            base = os.path.basename(cont)
            if kind in ("zh-Hans-script", "source-script"):
                m = RE_SCRIPT_PATH.match(cont)
                if not m:
                    continue
                rest = m.group("rest").replace("\\", "/")
                if kind == "zh-Hans-script":
                    out = os.path.join(SCRIPTS, "zh-Hans", rest)
                    if not out.endswith(".txt"):
                        out = out + ".txt"
                    cp, txt = read_text_asset(obj)
                else:
                    out = os.path.join(SCRIPTS, "source", rest)
                    if not out.endswith(".nani"):
                        out = out + ".nani"
                    cp, txt = read_naninovel_script(obj)
            else:
                m = RE_UI_PATH.match(cont)
                if not m:
                    continue
                rest = m.group("rest").replace("\\", "/")
                sub = "zh-Hans" if kind == "zh-Hans-ui" else "source"
                out = os.path.join(UI, sub, rest)
                if not out.endswith(".txt"):
                    out = out + ".txt"
                cp, txt = read_text_asset(obj)
            if txt is None:
                errors.append({"bundle": fn, "container": cont, "error": "no text field"})
                continue
            safe_write(out, txt)
            counts[kind] += 1
            index.append({
                "kind": kind,
                "bundle": fn,
                "container": cont,
                "path_id": obj.path_id,
                "out": os.path.relpath(out, PROJ).replace("\\", "/"),
                "chars": len(txt),
                "lines": txt.count("\n") + 1,
                "sha1": hashlib.sha1(txt.encode("utf-8")).hexdigest(),
            })

    index.sort(key=lambda r: (r["kind"], r["out"]))
    with open(INDEX, "w", encoding="utf-8") as f:
        json.dump({"counts": counts, "assets": index, "errors": errors}, f,
                  ensure_ascii=False, indent=1)
    print("counts:", counts)
    print("errors:", len(errors))
    for e in errors[:20]:
        print("  ", e)


if __name__ == "__main__":
    main()
