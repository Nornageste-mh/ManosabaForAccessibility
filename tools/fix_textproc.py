import io

def rw(path, fn):
    s = io.open(path, encoding="utf-8-sig").read()
    s2 = fn(s)
    assert s2 != s, path
    io.open(path, "w", encoding="utf-8-sig", newline="\n").write(s2)
    print("patched", path.split("\\")[-1])

base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

def f_tp(s):
    # 1) Speaker: 未映射且非中文 → 不念
    old = """            string mapped = SpeakerNames.Resolve(s);
            return string.IsNullOrEmpty(mapped) ? s : mapped;"""
    new = """            string mapped;
            if (SpeakerNames.TryMap(s, out mapped)) return mapped;
            // \u672a\u547d\u4e2d\u8868\uff1a\u4e2d\u6587\u540d\u539f\u6837\u5ff5\uff0c\u7eaf\u897f\u6587 id\uff08\u5982 `Creature<\u52a8\u6001\u540d>`\uff09\u4e0d\u5ff5\u2014\u2014
            // \u5426\u5219\u8bfb\u5c4f\u91cc\u4f1a\u51fa\u73b0\u4e00\u4e32\u82f1\u6587\u5b57\u6bcd\uff0c\u542c\u4f17\u4e5f\u731c\u4e0d\u51fa\u662f\u8c01\u3002
            return ContainsCjk(s) ? s : "";"""
    assert old in s
    s = s.replace(old, new, 1)

    # 2) BrEvaluator: 左侧粘着助词不补逗号
    old = """                    case '\u300d': case '\u300f': case '\uff09': case ')': case '\uff1e':
                        return "";"""
    new = """                    case '\u300d': case '\u300f': case '\uff09': case ')': case '\uff1e':
                        return "";
                    // \u5de6\u4fa7\u662f\u7c98\u7740\u52a9\u8bcd/\u8fde\u63a5\u8bcd\u65f6\u4e5f\u4e0d\u80fd\u8865\u9017\u53f7\uff1a\u8bed\u6599\u91cc\u6709 1,353 \u5904
                    // `<br>` \u7d27\u8ddf\u5728\u300c\u7684/\u662f/\u4e86/\u5728/\u5730/\u5f97\u2026\u300d\u540e\u9762\uff08\u5982\u300c\u5899\u4e0a\u7684<br>\u5168\u65b0\u300d\uff09\uff0c
                    // \u65e0\u6761\u4ef6\u8865\u9017\u53f7\u5c31\u4f1a\u5ff5\u6210\u300c\u5899\u4e0a\u7684\uff0c\u5168\u65b0\u300d\u3002
                    case '\u7684': case '\u4e86': case '\u7740': case '\u8fc7': case '\u5730': case '\u5f97':
                    case '\u662f': case '\u5728': case '\u548c': case '\u4e0e': case '\u53ca': case '\u6216':
                    case '\u628a': case '\u88ab': case '\u5c31': case '\u90fd': case '\u4e5f': case '\u8fd8':
                    case '\u800c': case '\u4e14': case '\u4f46': case '\u5e76':
                        return "";"""
    assert old in s
    s = s.replace(old, new, 1)

    # 3) 空格归一：加 CJK 边缘空格清理 + 破折号后不吃逗号
    old = """            s = SpaceRe.Replace(s, " ");"""
    new = """            s = SpaceRe.Replace(s, " ");
            // \u672c\u4f5c\u5267\u672c\u7528\u300c<br>\\u3000\u300d\u505a\u7f29\u8fdb\uff083,342 \u5904\uff09\uff0c\u628a U+3000 \u6362\u6210\u534a\u89d2\u7a7a\u683c\u540e
            // \u4f1a\u5728\u53e5\u4e2d\u7559\u4e0b\u4e00\u4e2a\u5b64\u7acb\u7a7a\u683c\u88ab TTS \u5ff5\u6210\u505c\u987f\uff08\u5b9e\u4f8b\uff1a\u300c\u2026\u2026 1\uff0c\u5f88\u5f00\u5fc3\u300d\uff09\u3002
            s = RemoveCjkEdgeSpaces(s);"""
    assert old in s
    s = s.replace(old, new, 1)

    old = """            s = CommaAfterStopRe.Replace(s, "$1");"""
    new = """            s = CommaAfterStopRe.Replace(s, "$1");
            // \u7834\u6298\u53f7/\u7701\u7565\u53f7\u540e\u4e0d\u518d\u8865\u9017\u53f7\uff08\u8bed\u6599 6 \u5904\u300c\u2014\u2014\uff0c\u300d\uff09
            s = DashCommaRe.Replace(s, "$1");"""
    assert old in s
    s = s.replace(old, new, 1)

    # 4) 句末只收逗号/空格，保留句号
    old = """            s = s.Trim().Trim('\uff0c', '\u3002', ' ');"""
    new = """            // \u53ea\u53bb\u5c3e\u90e8\u9017\u53f7\u4e0e\u7a7a\u767d\uff1b\u53e5\u53f7\u4fdd\u7559\uff08TTS \u9760\u5b83\u6536\u5c3e\u964d\u8c03\uff09
            s = s.Trim().Trim('\uff0c', ' ');"""
    assert old in s
    s = s.replace(old, new, 1)

    # 5) ruby: 拉丁旁注只读正文
    old = """            if (note.Length == 0) return body;
            if (IsKanaOnly(note)) return body;"""
    new = """            if (note.Length == 0) return body;
            if (IsKanaOnly(note)) return body;
            // \u62c9\u4e01\u6cd5\u540d/\u82f1\u6587\u65c1\u6ce8\uff08Balloon / SALAMANDER / pyrokinesis\u2026\uff09\u53ea\u5ff5\u6b63\u6587\uff1a
            // \u4e2d\u6587 TTS \u5ff5\u62fc\u5199\u53c8\u6162\u53c8\u542c\u4e0d\u61c2\uff0c\u4e0e textassets\\text-format.md \u7684\u7ed3\u8bba\u4e00\u81f4\u3002
            if (IsLatinOnly(note)) return body;"""
    assert old in s
    s = s.replace(old, new, 1)

    # 6) 追加辅助方法（放 IsKanaOnly 之后）
    old = """        /// <summary>\u65c1\u6ce8\u662f\u4e0d\u662f\u7eaf\u65e5\u6587\u5047\u540d\uff08\u542b\u957f\u97f3\u7b26/\u4e2d\u70b9\uff09\u3002</summary>"""
    new = """        /// <summary>\u662f\u4e0d\u662f\u53ea\u6709\u897f\u6587\u5b57\u6bcd/\u6570\u5b57/\u7a7a\u683c/\u8fde\u5b57\u7b26\uff08\u5373\u62c9\u4e01\u6cd5\u540d\u3001\u82f1\u6587\u4e13\u7528\u540d\uff09\u3002</summary>
        private static bool IsLatinOnly(string s)
        {
            bool any = false;
            foreach (char c in s)
            {
                if (char.IsWhiteSpace(c)) continue;
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                          || c == '-' || c == '\\'' || c == '&';
                if (!ok) return false;
                any = true;
            }
            return any;
        }

        /// <summary>\u5220\u6389\u7d27\u8d34\u4e2d\u6587/\u5168\u89d2\u6807\u70b9\u7684\u5355\u4e2a\u534a\u89d2\u7a7a\u683c\uff08\u5267\u672c\u91cc &lt;br&gt;\\u3000 \u505a\u7f29\u8fdb\u4ea7\u751f\u7684\uff09\u3002</summary>
        private static string RemoveCjkEdgeSpaces(string s)
        {
            if (s.IndexOf(' ') < 0) return s;
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == ' ')
                {
                    char prev = sb.Length > 0 ? sb[sb.Length - 1] : '\\0';
                    char next = i + 1 < s.Length ? s[i + 1] : '\\0';
                    if (IsCjkOrFullWidth(prev) || IsCjkOrFullWidth(next)) continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool IsCjkOrFullWidth(char c)
        {
            if (c >= '\\u4e00' && c <= '\\u9fff') return true;
            if (c >= '\\u3000' && c <= '\\u303f') return true;
            if (c >= '\\uff00' && c <= '\\uffef') return true;
            if (c >= '\\u3040' && c <= '\\u30ff') return true;
            return false;
        }

        /// <summary>\u6709\u4e2d\u6587\uff08CJK\uff09\u5b57\u7b26\u5417\uff1a\u7528\u4e8e\u5224\u65ad\u4e00\u4e2a\u672a\u6620\u5c04\u7684\u8bf4\u8bdd\u4eba id \u8be5\u4e0d\u8be5\u5ff5\u3002</summary>
        public static bool ContainsCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
                if (c >= '\\u4e00' && c <= '\\u9fff') return true;
            return false;
        }

        /// <summary>\u65c1\u6ce8\u662f\u4e0d\u662f\u7eaf\u65e5\u6587\u5047\u540d\uff08\u542b\u957f\u97f3\u7b26/\u4e2d\u70b9\uff09\u3002</summary>"""
    assert old in s
    s = s.replace(old, new, 1)

    # 7) 新增 DashCommaRe 字段（放 SpaceRe 附近）
    old = """        private static readonly Regex SpaceRe ="""
    new = """        // \u7834\u6298\u53f7/\u7701\u7565\u53f7\u540e\u9762\u591a\u51fa\u6765\u7684\u9017\u53f7
        private static readonly Regex DashCommaRe =
            new Regex(@"([\u2014\u2026])[ \\t]*[\uff0c,][ \\t]*", RegexOptions.Compiled);

        private static readonly Regex SpaceRe ="""
    assert old in s
    s = s.replace(old, new, 1)
    return s

rw(base + r"\TextProc.cs", f_tp)
