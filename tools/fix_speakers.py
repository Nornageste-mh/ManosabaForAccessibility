import io

def rw(path, fn):
    s = io.open(path, encoding="utf-8-sig").read()
    s2 = fn(s)
    assert s2 != s, path
    io.open(path, "w", encoding="utf-8-sig", newline="\n").write(s2)
    print("patched", path)

base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

# ---------- 1) SpeakerNames: TryMap ----------
def f1(s):
    old = """        public static string Resolve(string authorId)
        {
            if (string.IsNullOrEmpty(authorId)) return "";
            string v;
            if (Map.TryGetValue(authorId, out v)) return v;
            // \u5e26\u540e\u7f00\u7684\u53d8\u4f53\uff08Creature<Name> \u5df2\u5355\u5217\uff0c\u8fd9\u91cc\u515c\u5e95\u88f8\u540d\u5339\u914d\uff09
            if (Map.TryGetValue("Creature" + authorId, out v)) return v;
            return authorId;
        }"""
    new = """        /// <summary>*}
        /// \u53ea\u67e5\u8868\uff1a\u547d\u4e2d\u8fd4\u56de\u4e2d\u6587\u540d\uff0c\u672a\u547d\u4e2d\u8fd4\u56de\u7a7a\u4e32\uff08\u8c03\u7528\u65b9\u81ea\u5df1\u51b3\u5b9a\u662f\u5426\u56de\u843d\u539f id\uff09\u3002
        /// \u4e3a\u4ec0\u4e48\u8981\u533a\u5206 Resolve \u548c TryMap\uff1a\u539f\u6765 Resolve \u672a\u547d\u4e2d\u65f6\u76f4\u63a5\u8fd4\u56de\u539f id\uff0c
        /// \u5bfc\u81f4 TextProc.Speaker \u6c38\u8fdc\u975e\u7a7a\uff0c`Creature<\u52a8\u6001\u540d>` \u8fd9\u7c7b id \u4f1a\u88ab\u5ff5\u6210\u82f1\u6587\uff1b
        /// \u800c\u201c\u6ca1\u6709\u5bf9\u5e94\u4e2d\u6587\u540d\u5b57\u201d\u7684 id \u5c31\u4e0d\u8be5\u54cd\u3002
        /// </summary>
        public static bool TryMap(string authorId, out string name)
        {
            name = "";
            if (string.IsNullOrEmpty(authorId)) return false;
            string v;
            if (Map.TryGetValue(authorId, out v)) { name = v; return true; }
            if (Map.TryGetValue("Creature" + authorId, out v)) { name = v; return true; }
            return false;
        }

        public static string Resolve(string authorId)
        {
            if (string.IsNullOrEmpty(authorId)) return "";
            string v;
            if (TryMap(authorId, out v)) return v;
            return authorId;
        }"""
    assert old in s
    return s.replace(old, new, 1)

rw(base + r"\SpeakerNames.cs", f1)
