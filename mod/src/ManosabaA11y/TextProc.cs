using System;
using System.Text;
using System.Text.RegularExpressions;

namespace ManosabaA11y
{
    /// <summary>
    /// 原始文本 → 可朗读文本。《魔法少女ノ魔女裁判》专项。
    ///
    /// 依据（全部来自实查，不许猜）：
    ///   · 剧本与本地化文档：textassets\scripts\zh-Hans（440 个文件，3.86M 字符，text-extract 导出）
    ///   · 说话人表：textassets\ui\zh-Hans\CharacterNames.txt（managed text，键=AuthorId）
    ///   · 行 id 形态：`|#0101Adv02_Ema001|`、`|#0101Trial01_Sherry001|`（ADV 与辩论都带）
    ///   · 标签统计：&lt;br&gt; 51433、&lt;ruby="..."&gt; 4182、&lt;link="..."&gt; 2396、
    ///     &lt;color=#...&gt; 265、&lt;b&gt; 146、&lt;sprite&gt; 6、&lt;size&gt; 4、■ 类方块 301
    ///
    /// 设计原则（来自 Framework 流水线 §6）：
    ///   · 正则只处理本作实查到的形态；
    ///   · 拿不准的形态输出原文（宁可多念，不可漏念）；
    ///   · 纯函数：不读游戏状态、无副作用，可用本地剧本离线回归。
    /// </summary>
    internal static class TextProc
    {
        // ==================== 本作的正则 ====================

        /// <summary>行 id 标记：`|#0101Adv02_Ema001|`。念之前剥掉，但要留下 id 供配音判定。</summary>
        private static readonly Regex LineKeyRe =
            new Regex(@"\|#([^|]+)\|", RegexOptions.Compiled);

        /// <summary>作者前缀：`Ema: ` / `JailerB: `（剧本里作者 id 是 ASCII 标识符 + 全/半角冒号）。</summary>
        private static readonly Regex AuthorPrefixRe =
            new Regex(@"^\s*([A-Za-z][A-Za-z0-9_]*)\s*[:：]\s*", RegexOptions.Compiled);

        /// <summary>ruby：`&lt;ruby="Balloon"&gt;气球&lt;/ruby&gt;`。旁注与正文都要读（读屏线性化要求）。</summary>
        private static readonly Regex RubyRe =
            new Regex(@"<ruby=[""']([^""']*)[""']\s*>(.*?)</ruby>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>link：`&lt;link="Objection_..."&gt;用血画下&lt;/link&gt;`。链接文本是正文（异议触发词），只读内容。</summary>
        private static readonly Regex LinkRe =
            new Regex(@"<link=[""'][^""']*[""']\s*>(.*?)</link>", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>换行：`&lt;br&gt;` / `&lt;br/&gt;`，游戏里用来分句。</summary>
        private static readonly Regex BrRe =
            new Regex(@"<\s*br\s*/?\s*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>其余 TMP/Naninovel 富文本标签（color/b/i/size/sprite/cspace/speed/...）。</summary>
        private static readonly Regex TagRe =
            new Regex(@"<[^<>]{1,120}>", RegexOptions.Compiled);

        /// <summary>被抹除/遮蔽的方块：`█ ■ □` → 「方块」。</summary>
        private static readonly Regex BlockRe =
            new Regex(@"[█■□]+", RegexOptions.Compiled);

        /// <summary>连续空白。</summary>
        // 破折号/省略号后面多出来的逗号
        private static readonly Regex DashCommaRe =
            new Regex(@"([—…])[ \t]*[，,][ \t]*", RegexOptions.Compiled);

        private static readonly Regex SpaceRe =
            new Regex(@"[ \t\u3000]{2,}", RegexOptions.Compiled);

        /// <summary>重复的句读（&lt;br&gt; 换行替换后会造出「，，」「……，」这类）。</summary>
        private static readonly Regex DupCommaRe =
            new Regex(@"[,，]{2,}", RegexOptions.Compiled);

        /// <summary>句读紧跟在终止标点后（「。，」「！，」）——去掉多余的逗号。</summary>
        private static readonly Regex CommaAfterStopRe =
            new Regex(@"([。！？…!?])[，,]", RegexOptions.Compiled);

        /// <summary>整行只有标点/符号（纯装饰行，不念）。</summary>
        private static readonly Regex OnlyPunctRe =
            new Regex(@"^[\s\p{P}\p{S}]*$", RegexOptions.Compiled);

        /// <summary>Naninovel 行内控制标记：`[i]`、`/pg`、`[wait 0.3]` 这类方括号指令。</summary>
        private static readonly Regex InlineCommandRe =
            new Regex(@"\[[A-Za-z][^\]]{0,60}\]", RegexOptions.Compiled);

        /// <summary>分页/换页标记。</summary>
        private static readonly Regex PageBreakRe =
            new Regex(@"/pg\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ==================== 对外入口 ====================

        /// <summary>
        /// 取行 id（配音/剧透闸门用）。拿不到返回空串 —— 调用方必须按「未知」处理。
        /// </summary>
        public static string ExtractLineKey(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            Match m = LineKeyRe.Match(raw);
            return m.Success ? m.Groups[1].Value.Trim() : "";
        }

        /// <summary>
        /// 取作者 id（`Ema: ` 这种前缀；没有则空串）。
        /// 注意：运行时作者多数来自 Naninovel 的 MessageAuthor，这里的兜底只在文本自带前缀时用。
        /// </summary>
        public static string ExtractAuthorId(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            Match m = AuthorPrefixRe.Match(raw);
            return m.Success ? m.Groups[1].Value : "";
        }

        /// <summary>
        /// 说话人：把原始名字转成要念的名字。没有名字就返回空串。
        /// 规则：查 SpeakerNames（AuthorId→中文名）；查不到就原样返回（不吞名字）。
        /// </summary>
        public static string Speaker(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string s = raw.Trim().Trim('【', '】', '[', ']', '「', '」', '『', '』', '(', ')', '（', '）');
            if (s.Length == 0) return "";
            // 遮蔽名：`? ? ?` / `？ ？ ？` / `???` —— 念「问号问号问号」是噪音，按剧本语义念「未知」。
            string compact = Regex.Replace(s, @"[\s?？]", "");
            if (compact.Length == 0) return "未知";
            string mapped;
            if (SpeakerNames.TryMap(s, out mapped)) return mapped;
            // 未命中表：中文名原样念，纯西文 id（如 `Creature<动态名>`）不念——
            // 否则读屏里会出现一串英文字母，听众也猜不出是谁。
            return ContainsCjk(s) ? s : "";
        }

        /// <summary>
        /// 台词正文：把原始文本转成可朗读文本。
        /// </summary>
        public static string Clean(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            string s = raw;

            // 1) 行 id 标记剥掉（不念，但要先 ExtractLineKey）
            s = LineKeyRe.Replace(s, "");

            // 2) 行首作者前缀剥掉（作者由 Patches 单独念）
            s = AuthorPrefixRe.Replace(s, "");

            // 3) 换行 → 停顿（先于通用标签剥离，否则 <br/> 会被 TagRe 直接删掉、句子粘连）
            //    ☆ 不能无条件换成「，」：实机日志里出现过
            //      `原来是做梦。她才刚安下心来——<br>眼前的光景就逐渐清晰了起来。`
            //      被念成「……安下心来——，眼前……」这种破折号后多一个逗号的别扭读法。
            //      规则：前一个字符已经是句读/破折号/引号收尾时，换行直接省略；否则补「，」。
            s = BrRe.Replace(s, BrEvaluator);

            // 4) ruby：正文 + 旁注都读；旁注是纯假名（日文读音）时只读正文，
            //    因为中文 TTS 念假名会得到一串乱码音。
            //    ★ 这里用命名方法而不是 lambda：net6.0 目标下引用 IF 层 interop 程序集时，
            //      lambda 会触发编译器生成 NullableAttribute，而 Il2Cppmscorlib 里没有它的 ctor
            //      → error CS0656。命名方法不经过那条路径。
            s = RubyRe.Replace(s, RubyEvaluator);

            // 5) link：只保留可点击文本
            s = LinkRe.Replace(s, "$1");

            // 6) 其余富文本标签
            s = TagRe.Replace(s, "");

            // 7) 遮蔽方块
            s = BlockRe.Replace(s, "方块");

            // 8) 行内命令 / 分页
            s = InlineCommandRe.Replace(s, "");
            s = PageBreakRe.Replace(s, "，");

            // 9) 空白与换行归一
            s = s.Replace("\r\n", "\n").Replace('\r', '\n');
            s = s.Replace('\n', '，').Replace('\t', ' ');
            s = s.Replace('\u3000', ' ');
            s = SpaceRe.Replace(s, " ");
            // 本作剧本用「<br>\u3000」做缩进（3,342 处），把 U+3000 换成半角空格后
            // 会在句中留下一个孤立空格被 TTS 念成停顿（实例：「…… 1，很开心」）。
            s = RemoveCjkEdgeSpaces(s);

            // 10) 标点归一（<br> 造成的一串逗号、终止标点后的逗号）
            s = DupCommaRe.Replace(s, "，");
            s = CommaAfterStopRe.Replace(s, "$1");
            // 破折号/省略号后不再补逗号（语料 6 处「——，」）
            s = DashCommaRe.Replace(s, "$1");
            s = Regex.Replace(s, @"\s*([，。！？；：、])\s*", "$1");

            // 只去尾部逗号与空白；句号保留（TTS 靠它收尾降调）
            s = s.Trim().Trim('，', ' ');
            return s;
        }

        /// <summary>
        /// 这一行**要不要朗读**（整行丢弃 vs 清洗后朗读）。
        /// </summary>
        public static bool ShouldRead(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return false;
            // 本地化文档里的注释行（`; ...`）不该念
            if (raw.TrimStart().StartsWith(";", StringComparison.Ordinal)) return false;
            string clean = Clean(raw);
            if (clean.Length == 0) return false;
            if (OnlyPunctRe.IsMatch(clean)) return false;
            return true;
        }

        /// <summary>清洗后只剩标点/符号 → 视为空（去重用）。</summary>
        public static bool IsTrivial(string clean)
        {
            if (string.IsNullOrEmpty(clean)) return true;
            return OnlyPunctRe.IsMatch(clean);
        }

        /// <summary>
        /// 去重指纹：去掉空白与标点，只比正文（同一句在隐藏面板与可见面板各触发一次时用）。
        /// </summary>
        public static string Fingerprint(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            StringBuilder sb = new StringBuilder(raw.Length);
            foreach (char c in Clean(raw))
            {
                if (char.IsWhiteSpace(c)) continue;
                if (char.IsPunctuation(c) || char.IsSymbol(c)) continue;
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>ruby 的替换体（命名方法，见 Clean 第 4 步注释）。</summary>
        /// <summary>
        /// `&lt;br&gt;` 替换器：前面是句读/破折号/引号收尾时直接省略，否则补一个「，」。
        /// 命名方法（非 lambda）理由同 RubyEvaluator：net6.0 + Il2Cppmscorlib 下 lambda 会 CS0656。
        /// </summary>
        private static string BrEvaluator(Match m)
        {
            try
            {
                string s = m.Result("$`"); // 已处理过的前缀（Regex.Match 的 $` 是左侧原文）
                if (s.Length == 0) return "";
                char c = s[s.Length - 1];
                switch (c)
                {
                    case '。': case '！': case '？': case '!': case '?':
                    case '…': case '—': case '、': case '，': case ',':
                    case '；': case ';': case '：': case ':':
                    case '」': case '』': case '）': case ')': case '＞':
                        return "";
                    // 左侧是粘着助词/连接词时也不能补逗号：语料里有 1,353 处
                    // `<br>` 紧跟在「的/是/了/在/地/得…」后面（如「墙上的<br>全新」），
                    // 无条件补逗号就会念成「墙上的，全新」。
                    case '的': case '了': case '着': case '过': case '地': case '得':
                    case '是': case '在': case '和': case '与': case '及': case '或':
                    case '把': case '被': case '就': case '都': case '也': case '还':
                    case '而': case '且': case '但': case '并':
                        return "";
                }

                // 右侧是「的/了/着/过/地/得/吗/呢/吧/啊/呀/哦」这类粘着语素时也不能补逗号，
                // 否则会念出「用血画下，的蝴蝶绘画」这种断在助词前面的句子（run38 辩论实机日志）。
                string rest = m.Result("$'");
                for (int i = 0; i < rest.Length; i++)
                {
                    char r = rest[i];
                    if (r == ' ' || r == '\u3000' || r == '\t' || r == '\r' || r == '\n') continue;
                    switch (r)
                    {
                        case '的': case '了': case '着': case '过': case '地': case '得':
                        case '吗': case '呢': case '吧': case '啊': case '呀': case '哦':
                        case '。': case '，': case '！': case '？': case '、': case '；': case '：':
                            return "";
                    }
                    break;
                }
                return "，";
            }
            catch { return "，"; }
        }

        private static string RubyEvaluator(Match m)
        {
            string note = (m.Groups[1].Value ?? "").Trim();
            string body = (m.Groups[2].Value ?? "").Trim();
            if (note.Length == 0) return body;
            if (IsKanaOnly(note)) return body;
            // 拉丁法名/英文旁注（Balloon / SALAMANDER / pyrokinesis…）只念正文：
            // 中文 TTS 念拼写又慢又听不懂，与 textassets\text-format.md 的结论一致。
            if (IsLatinOnly(note)) return body;
            if (body.Length == 0) return note;
            return body + "（" + note + "）";
        }

        /// <summary>是不是只有西文字母/数字/空格/连字符（即拉丁法名、英文专用名）。</summary>
        private static bool IsLatinOnly(string s)
        {
            bool any = false;
            foreach (char c in s)
            {
                if (char.IsWhiteSpace(c)) continue;
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')
                          || c == '-' || c == '\'' || c == '&';
                if (!ok) return false;
                any = true;
            }
            return any;
        }

        /// <summary>删掉紧贴中文/全角标点的单个半角空格（剧本里 &lt;br&gt;\u3000 做缩进产生的）。</summary>
        private static string RemoveCjkEdgeSpaces(string s)
        {
            if (s.IndexOf(' ') < 0) return s;
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == ' ')
                {
                    char prev = sb.Length > 0 ? sb[sb.Length - 1] : '\0';
                    char next = i + 1 < s.Length ? s[i + 1] : '\0';
                    if (IsCjkOrFullWidth(prev) || IsCjkOrFullWidth(next)) continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool IsCjkOrFullWidth(char c)
        {
            if (c >= '\u4e00' && c <= '\u9fff') return true;
            if (c >= '\u3000' && c <= '\u303f') return true;
            if (c >= '\uff00' && c <= '\uffef') return true;
            if (c >= '\u3040' && c <= '\u30ff') return true;
            return false;
        }

        /// <summary>有中文（CJK）字符吗：用于判断一个未映射的说话人 id 该不该念。</summary>
        public static bool ContainsCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s)
                if (c >= '\u4e00' && c <= '\u9fff') return true;
            return false;
        }

        /// <summary>旁注是不是纯日文假名（含长音符/中点）。</summary>
        private static bool IsKanaOnly(string s)
        {
            bool any = false;
            foreach (char c in s)
            {
                if (char.IsWhiteSpace(c)) continue;
                bool kana = (c >= '\u3040' && c <= '\u30FF') || c == '\u30FC' || c == '\u30FB';
                if (!kana) return false;
                any = true;
            }
            return any;
        }
    }
}
