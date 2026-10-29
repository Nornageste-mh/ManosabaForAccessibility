using System;
using System.Collections.Generic;
using UnityEngine;
using Naninovel;

namespace ManosabaA11y
{
    /// <summary>
    /// 剧情文本朗读核心（L3 逐作层）。
    ///
    /// 依据（全部实查，不许猜）：
    ///   · ADV 打印链：Naninovel.TextPrinterManager.Print(string printerId, PrintedMessage message, ...)
    ///     —— 反编译 Elringus.Naninovel.Runtime.dll 得到签名；env-probe probe v3 实测该补丁可挂
    ///     （LogOutput.log：PATCH_OK TextPrinterManager.Print target=... Print(String printerId, PrintedMessage message, Boolean append, Single speed, AsyncToken token)）。
    ///   · 面板级入口：Naninovel.UI.UITextPrinterPanel.AddMessage(PrintedMessage)、
    ///     WitchTrials.Views.DebateTextPrinterPanel.AddMessage/SetMessages/SetText/AddText
    ///     （re\decompiled\signatures\witchtrials\DebateTextPrinterPanel.txt 实查；probe v3 全部 PATCH_OK）。
    ///   · 辩论没有逐字回调：DebateTextPrinter : UITextPrinter 覆写打印，把文本追加入面板的
    ///     StringBuilder 后 SetText(string) 整体刷出（witchtrials-re re\witchtrials-ui.md §1，E1 级）。
    ///     → SetText 走「新增段差分」，否则会把前面几句重念一遍。
    ///
    /// 为什么要有 _pending 缓冲（0.35 秒）：
    ///   同一句台词可能同时经过「消息级」钩子（Print / PanelAddMessage，带 Author）和
    ///   「文本级」钩子（RevealableText.set_Text / Debate 面板 SetText，只有字符串）。
    ///   立即朗读会念两遍；所以消息级只登记待念，等 0.35 秒让文本级确认到齐后再统一念一次，
    ///   文本级看到一个新鲜的 pending 就只做确认、不自己发声。
    ///
    /// ★ 唯一朗读出口是 Repeat（流水线硬约束，tools\lint_repeat.ps1 会拦 Speech.Speak( 直调）。
    /// </summary>
    internal static class Reader
    {
        /// <summary>同一句在短时间内的重复触发（隐藏面板预打印、逐字回刷）合并窗口。</summary>
        private const float DedupeWindow = 2.5f;

        /// <summary>消息级登记后等多久统一朗读（给文本级钩子留确认时间）。</summary>
        private const float PendingDelay = 0.35f;

        /// <summary>文本级钩子把 pending 视为「同一句」的时间窗。</summary>
        private const float PendingFresh = 1.2f;

        private static string _lastFp = "";
        private static float _lastAt = -999f;
        private static int _spoken;

        private sealed class Pending
        {
            public string Raw;
            public string AuthorId;
            public string AuthorLabel;
            /// <summary>剧本行 id（= 语音 clip 名），用来判断这一行有没有配音。</summary>
            public string LineKey;
            public float At;
            public string Src;
        }

        private static Pending _pending;

        /// <summary>
        /// 剧情专用的「最后一句」——只由剧情文本和选项播报写入，界面导航播报不会碰它。
        /// 玩家按重读键时念这一条（要求：只重读剧情文字和选项）。
        /// </summary>
        private static string _lastLine = "";

        /// <summary>最后一句剧情/选项（给重读键用）。</summary>
        public static string LastLine { get { return _lastLine; } }

        /// <summary>
        /// 记一条「可重读」的文本。剧情朗读与选项播报都调它；
        /// 界面导航播报**不要**调（否则退格会重读「系统设置，按钮。2 / 5」）。
        /// </summary>
        public static void Remember(string line)
        {
            if (!string.IsNullOrEmpty(line)) _lastLine = line;
        }

        /// <summary>辩论面板整段刷新用的「上次原文」，按实例区分（本作同屏可能有多个面板）。</summary>
        private static readonly Dictionary<int, string> _panelRaw = new Dictionary<int, string>();
        /// <summary>
        /// ADV 面板最近一次收到的说话人（SetMessageAuthor）。由于 Print 钩子不能挂，
        /// 说话人只能靠这条旁路：先记下来，reveal 文本到达时套用。
        /// _advAuthorAt 用来防止「上一句的说话人」被粘到下一句旁白上。
        /// </summary>
        private static string _advAuthorId = "";
        private static string _advAuthorLabel = "";
        private static float _advAuthorAt = -999f;

        /// <summary>诊断用：统计已朗读句数（日志里看这条就知道补丁到底有没有接上）。</summary>
        public static int SpokenCount { get { return _spoken; } }

        // ==================== 消息级入口：带 Author，先登记 ====================

        /// <summary>
        /// Naninovel.TextPrinterManager.Print 的 Postfix —— ADV 的权威打印点
        /// （一条台词只出现一次；逐字显示不经过它）。
        /// </summary>
        public static void OnPrint(string printerId, PrintedMessage msg)
        {
            try
            {
                if (msg == null) return;
                string raw = TextOf(msg);
                if (Plugin.TestMode)
                {
                    A11yHost.Diag("HOOK print printerId=" + printerId + " raw=" + raw);
                }
                if (string.IsNullOrEmpty(raw)) return;

                string authorId = "", authorLabel = "";
                try
                {
                    // ★ 读**字段** author，不读 Author 属性（naninovel-re probe4 实测）：
                    //   旁白时 author 字段非 null 但 id=null；而 Author（Nullable getter）在旁白会抛 NRE。
                    MessageAuthor author = msg.author;
                    if (author != null)
                    {
                        authorId = author.Id ?? "";
                        try { authorLabel = author.Label == null ? "" : author.Label.ToString(); }
                        catch { authorLabel = ""; }
                    }
                }
                catch (Exception e) { A11yHost.Diag("Reader.author: " + e.Message); }

                // ★ 辩论台词的 printerId 是 WitchTrials.Views.PrintDebateText.DebatePrinterId（实读 == "Debate"）
                Register(raw, authorId, authorLabel, "print:" + printerId, LineKeyOf(msg));
            }
            catch (Exception e) { A11yHost.Diag("Reader.OnPrint: " + e.Message); }
        }

        /// <summary>
        /// Naninovel.UI.UITextPrinterPanel.AddMessage 的 Postfix（面板级兜底 + 可见性闸门）。
        /// </summary>
        public static void OnPanelAddMessage(Component panel, PrintedMessage msg)
        {
            try
            {
                if (panel != null)
                {
                    try
                    {
                        if (UiVis.Hidden(panel.transform))
                        {
                            A11yHost.Diag("Reader.panel: 面板隐藏，跳过 " + panel.name);
                            return;
                        }
                    }
                    catch (Exception e) { A11yHost.Diag("Reader.panel.vis: " + e.Message); }
                }
                OnPrint("panel", msg);
            }
            catch (Exception e) { A11yHost.Diag("Reader.OnPanelAddMessage: " + e.Message); }
        }

        /// <summary>辩论面板 AddMessage（有 Author，辩论里说话人只能从这里拿）。</summary>
        public static void OnDebateAddMessage(PrintedMessage msg)
        {
            try { OnPrint("debate.panel", msg); }
            catch (Exception e) { A11yHost.Diag("Reader.OnDebateAddMessage: " + e.Message); }
        }

        /// <summary>
        /// Naninovel.UITextPrinter.AddMessage(PrintedMessage) 的 Postfix。
        ///
        /// ★ 这条是本作 ADV 路径上**唯一**能同时拿到「正文 + 行 id」的入口（run56 实机定位：
        ///   面板级 AddMessage 一条都不响）。行 id 来自 `message.Text.parts[i].id`，
        ///   是「这一句有没有配音」的唯一权威判据 —— 没有它，配音闸门只能全放行。
        ///
        /// 可见性：printer 的 panel 可能还没显示出来，所以这里只登记、不念；
        /// 真正出声仍然要等文本级（reveal）钩子确认，或 pending 到点后由 Tick 念。
        /// </summary>
        public static void OnPrinterAddMessage(PrintedMessage msg)
        {
            try
            {
                string printerName = "printer";
                OnPrint("printer:" + printerName, msg);
            }
            catch (Exception e) { A11yHost.Diag("Reader.OnPrinterAddMessage: " + e.Message); }
        }

        /// <summary>
        /// Naninovel.UITextPrinter.SetMessages(IReadOnlyList&lt;PrintedMessage&gt;) 的 Postfix。
        /// 载入存档 / 切面板时会整批重设，只取最后一条登记（行 id 同样来自它的 parts）。
        /// IReadOnlyList 在 interop 里没有 Count，这里只按索引安全访问（见 Patches 注释）。
        /// </summary>
        public static void OnPrinterSetMessages(Il2CppSystem.Collections.Generic.IReadOnlyList<PrintedMessage> messages)
        {
            try
            {
                if (messages == null) return;
                PrintedMessage last = LastOf(messages);
                if (last == null) return;
                OnPrinterAddMessage(last);
            }
            catch (Exception e) { A11yHost.Diag("Reader.OnPrinterSetMessages: " + e.Message); }
        }

        /// <summary>从 interop 的 IReadOnlyList 里取最后一条（没有 Count 可用，按索引探到 null）。</summary>
        private static PrintedMessage LastOf(Il2CppSystem.Collections.Generic.IReadOnlyList<PrintedMessage> list)
        {
            try
            {
                var concrete = list.TryCast<Il2CppSystem.Collections.Generic.List<PrintedMessage>>();
                if (concrete != null && concrete.Count > 0) return concrete[concrete.Count - 1];
            }
            catch { }
            PrintedMessage last = null;
            for (int i = 0; i < 64; i++)
            {
                PrintedMessage m = null;
                try { m = list[i]; } catch { break; }
                if (m == null) break;
                last = m;
            }
            return last;
        }

        /// <summary>
        /// WitchTrials.Views.WitchTrialsTextPrinterPanel.SetMessageAuthor(MessageAuthor) 的 Postfix。
        ///
        /// ★ 这是 ADV 唯一的说话人来源：TextPrinterManager.Print 不能挂（返回 UniTask，
        ///   Harmony detour 会搬坏返回值 → PrintText.Print 的 await 抛 MethodAccessException，
        ///   整部游戏卡死；2026-10-29 实机 + NOPATCH=print 二分证实）。
        /// 实测（naninovel-re probe3）：每条台词都会触发，且与 set_Text 次数一致（各 4 次）。
        ///
        /// 时序兼容：说话人可能先到（先记下来），也可能后到（补进还没念的 pending）。
        /// 两种顺序都由这里 + Register 处理，不会因为顺序不同而丢名字。
        /// </summary>
        public static void OnAdvAuthor(MessageAuthor author)
        {
            try
            {
                string id = "", label = "";
                if (author != null)
                {
                    id = author.Id ?? "";
                    try { label = author.Label == null ? "" : author.Label.ToString(); }
                    catch { label = ""; }
                }
                _advAuthorId = id;
                _advAuthorLabel = label;
                _advAuthorAt = Time.unscaledTime;

                Pending p = _pending;
                if (p != null && string.IsNullOrEmpty(p.AuthorId)
                    && Time.unscaledTime - p.At <= PendingFresh)
                {
                    p.AuthorId = id;
                    p.AuthorLabel = label;
                }

                if (Plugin.CfgDiagLog != null && Plugin.CfgDiagLog.Value)
                    A11yHost.Diag("HOOK advAuthor id=" + id + " label=" + label);
            }
            catch (Exception e) { A11yHost.Diag("Reader.OnAdvAuthor: " + e.Message); }
        }

        /// <summary>
        /// RevealableTextPrinterPanel.SetAuthorNameText(string) 的 Postfix —— 名字框的最终显示串。
        /// 本作 SetMessageAuthor 的 MessageAuthor 实测 id/label 全空（run fix2：36 次 `id= label=`），
        /// 所以说话人只能从这条「已经本地化好的显示文本」拿。旁白时它会被置空串 → 顺手清掉旧名字，
        /// 避免把上一句的说话人粘到旁白上。
        /// </summary>
        public static void OnAdvAuthorName(string text)
        {
            try
            {
                string name = (text ?? "").Trim();
                _advAuthorLabel = name;
                _advAuthorId = "";
                _advAuthorAt = Time.unscaledTime;

                Pending p = _pending;
                if (p != null && Time.unscaledTime - p.At <= PendingFresh)
                {
                    p.AuthorLabel = name;
                    p.AuthorId = "";
                }

                if (Plugin.CfgDiagLog != null && Plugin.CfgDiagLog.Value)
                    A11yHost.Diag("HOOK advAuthorName=[" + name + "]");
            }
            catch (Exception e) { A11yHost.Diag("Reader.OnAdvAuthorName: " + e.Message); }
        }
        // ==================== 文本级入口：只在新消息不来时兜底 ====================

        /// <summary>
        /// WitchTrials.Views.DebateTextPrinterPanel.SetText 的 Postfix。
        /// 面板把 StringBuilder 里累计的全部文本整体刷进来 → 做「新增段」差分。
        /// </summary>
        public static void OnDebateSetText(int instanceId, string raw)
        {
            try
            {
                string delta = raw ?? "";
                string prev;
                if (_panelRaw.TryGetValue(instanceId, out prev) && !string.IsNullOrEmpty(prev)
                    && delta.Length >= prev.Length && delta.StartsWith(prev, StringComparison.Ordinal))
                {
                    delta = delta.Substring(prev.Length);
                }
                _panelRaw[instanceId] = raw ?? "";
                if (string.IsNullOrEmpty(delta)) return;

                if (HasFreshPending()) { A11yHost.Diag("Reader.debateSetText: 已有 pending，忽略文本级"); return; }
                SpeakNow(delta, "", "", "debate:set");
            }
            catch (Exception e) { A11yHost.Diag("Reader.OnDebateSetText: " + e.Message); }
        }

        /// <summary>WitchTrials.Views.DebateTextPrinterPanel.AddText 的 Postfix（单句到达，天然增量）。</summary>
        public static void OnDebateAddText(int instanceId, string raw)
        {
            try
            {
                if (string.IsNullOrEmpty(raw)) return;
                if (HasFreshPending()) { A11yHost.Diag("Reader.debateAddText: 已有 pending，忽略文本级"); return; }
                SpeakNow(raw, "", "", "debate:add");
            }
            catch (Exception e) { A11yHost.Diag("Reader.OnDebateAddText: " + e.Message); }
        }

        /// <summary>
        /// Naninovel.UI.RevealableText.set_Text 的 Postfix（ADV 逐字显示，最后兜底）。
        /// ★ 只在「最近没有消息级钩子」时才念：逐字回刷会把同一句 set 很多次，去重窗口兜住。
        /// </summary>
        public static void OnRevealText(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return;
                if (HasFreshPending()) return;

                // 说话人：只有在最近 3 秒内收到过 SetMessageAuthor 才采用，
                // 否则宁可不念名字，也不能把上一句的说话人安到旁白上（错报比漏报更糟）。
                bool fresh = Time.unscaledTime - _advAuthorAt <= 3f;
                Register(text, fresh ? _advAuthorId : "", fresh ? _advAuthorLabel : "", "reveal");
            }
            catch (Exception e) { A11yHost.Diag("Reader.OnRevealText: " + e.Message); }
        }

        // ==================== 每帧 ====================

        /// <summary>由 PluginBehaviour.Update 调用：把到期的 pending 念出去。</summary>
        public static void Tick()
        {
            try
            {
                Pending p = _pending;
                if (p == null) return;
                if (Time.unscaledTime - p.At < PendingDelay) return;
                _pending = null;
                SpeakNow(p.Raw, p.AuthorId, p.AuthorLabel, p.Src, p.LineKey);
            }
            catch (Exception e) { A11yHost.Diag("Reader.Tick: " + e.Message); }
        }

        /// <summary>
        /// 从 PrintedMessage 里取剧本行 id（= 语音 clip 名，如 0101Adv05_Narrative001）。
        ///
        /// ★ 依据（实查，不许猜）：LocalizableText.parts 是 Il2CppReferenceArray&lt;LocalizableTextPart&gt;，
        ///   LocalizableTextPart 的 id 字段就是行 id；naninovel-re probe4 实测
        ///   `RE_PRINT_PARTS [0]{id='0101Adv05_Narrative001' ...}`。
        ///   PrintedMessage.Text 里**不含** `|#id|` 标记，正则文本是拿不到 id 的。
        /// </summary>
        private static string LineKeyOf(PrintedMessage msg)
        {
            try
            {
                var parts = msg.Text.parts;   // Il2CppReferenceArray<LocalizableTextPart>（有 Length / 索引器）
                if (parts == null || parts.Length <= 0) return "";
                LocalizableTextPart p = parts[0];
                if (p == null) return "";
                return p.id ?? "";
            }
            catch (Exception e)
            {
                A11yHost.Diag("Reader.LineKey: " + e.Message);
                return "";
            }
        }

        /// <summary>场景切换 / 读档时清状态，避免旧指纹压掉新台词。</summary>
        public static void Reset()
        {
            _lastFp = "";
            _lastAt = -999f;
            _pending = null;
            _panelRaw.Clear();
            _lastLine = "";
        }

        // ==================== 内部 ====================

        private static void Register(string raw, string authorId, string authorLabel, string src, string lineKey = "")
        {
            if (string.IsNullOrEmpty(raw)) return;

            // 同一句会有两三个入口先后到达（Print / PanelAddMessage / 面板文本钩子）：
            // 指纹相同就只刷新时间、保留先到的那条（它可能带 Author），绝不能当成两句念。
            Pending old = _pending;
            if (old != null)
            {
                if (TextProc.Fingerprint(old.Raw) == TextProc.Fingerprint(raw))
                {
                    old.At = Time.unscaledTime;
                    // 行 id 只可能从消息级入口来（文本级入口没有 PrintedMessage）：
                    // 后到的这条带了 id 就补上，避免「先到的是 reveal、拿不到配音判定」。
                    if (string.IsNullOrEmpty(old.LineKey) && !string.IsNullOrEmpty(lineKey))
                        old.LineKey = lineKey;
                    return;
                }

                // 真的是新的一句顶掉旧的一句 → 先把旧的念掉（快进时宁可多念，不可漏念）
                _pending = null;
                SpeakNow(old.Raw, old.AuthorId, old.AuthorLabel, old.Src + "(flush)", old.LineKey);
            }

            _pending = new Pending
            {
                Raw = raw,
                AuthorId = authorId,
                AuthorLabel = authorLabel,
                LineKey = lineKey,
                At = Time.unscaledTime,
                Src = src
            };
        }

        private static bool HasFreshPending()
        {
            Pending p = _pending;
            return p != null && Time.unscaledTime - p.At <= PendingFresh;
        }

        private static string TextOf(PrintedMessage msg)
        {
            // ★ 必须是「隐式转换 (string)」而不是 ToString()（run13 实机 + naninovel-re probe4 对照）：
            //   PrintedMessage.Text 返回的是 LocalizableText **值类型**，
            //   对它调用 ToString() 会抛 NullReferenceException（run12/run13 两轮都复现），
            //   而 probe4 用 `(string)message.Text` 能稳定拿到
            //   [沾满鲜血的会客厅内，<br>少女们陷入了恐慌。]。
            string viaProp = "";
            try { viaProp = (string)msg.Text ?? ""; }
            catch (Exception e) { A11yHost.Diag("Reader.TextOf.prop: " + e.Message); }

            if (!string.IsNullOrEmpty(viaProp)) return viaProp;

            string viaField = "";
            try { viaField = (string)msg.text ?? ""; }
            catch (Exception e) { A11yHost.Diag("Reader.TextOf.field: " + e.Message); }

            if (!string.IsNullOrEmpty(viaField)) return viaField;

            string viaFieldToString = "";
            try { viaFieldToString = msg.text.ToString() ?? ""; }
            catch (Exception e) { A11yHost.Diag("Reader.TextOf.field.ToString: " + e.Message); }

            if (!string.IsNullOrEmpty(viaFieldToString)) return viaFieldToString;

            if (Plugin.TestMode)
            {
                A11yHost.Diag("Reader.TextOf: 三条路都空 prop=[" + viaProp + "] field=[" + viaField + "]");
            }
            return "";
        }

        /// <summary>统一朗读路径：清洗 → 去重闸门 → 说话人 → 配音闸门 → Repeat.Say。</summary>
        private static void SpeakNow(string raw, string authorId, string authorLabel, string src, string lineKey = "")
        {
            if (string.IsNullOrEmpty(raw)) return;
            if (!TextProc.ShouldRead(raw)) return;

            string text = TextProc.Clean(raw);
            if (TextProc.IsTrivial(text)) return;

            // —— 去重：同一句 2.5 秒内只念一次（消息级与文本级钩子会重复触发）
            string fp = TextProc.Fingerprint(raw);
            float now = Time.unscaledTime;
            if (fp.Length > 0 && fp == _lastFp && now - _lastAt < DedupeWindow)
            {
                A11yHost.Diag("Reader.dedupe: " + src);
                return;
            }

            // —— 说话人：优先 Naninovel 的 MessageAuthor，其次文本里的 `Ema: ` 前缀
            string speaker = "";
            try
            {
                speaker = TextProc.Speaker(authorId);
                if (string.IsNullOrEmpty(speaker)) speaker = TextProc.Speaker(authorLabel);
                if (string.IsNullOrEmpty(speaker)) speaker = TextProc.Speaker(TextProc.ExtractAuthorId(raw));
            }
            catch (Exception e) { A11yHost.Diag("Reader.speaker: " + e.Message); }

            bool withName = speaker.Length > 0
                            && (Plugin.CfgSpeakName == null || Plugin.CfgSpeakName.Value);
            string line = withName ? (speaker + "：" + text) : text;

            _lastFp = fp;
            _lastAt = now;
            _spoken++;

            // 先记进「可重读」缓冲：**有配音的行也要记**——语音跑过去了，退格重读中文正是玩家要的
            // （《不存在》Plugin.cs:387-433 同一做法；这也是各个仓库的标配）。
            Remember(line);

            // —— 配音闸门：这一行有游戏配音时不自动朗读（否则语音和 TTS 会叠在一起互相盖住）
            bool voiced = VoiceLineIds.Has(lineKey);
            if (voiced)
            {
                try { Speech.Stop(); } catch { }   // 上一句 TTS 还没念完时，别盖住这句语音
                bool announceName = withName
                                    && (Plugin.CfgAnnounceOnVoiced == null || Plugin.CfgAnnounceOnVoiced.Value);
                if (announceName)
                {
                    try { Repeat.Note(speaker); Repeat.Say(speaker, false); } catch { }
                }
                if (Plugin.CfgDiagLog != null && Plugin.CfgDiagLog.Value)
                {
                    A11yHost.Diag("Reader.voiced[" + src + "] id=" + lineKey + " => 有配音，跳过正文"
                                  + (announceName ? "（只报名字）" : ""));
                }
                return;
            }

            // 无配音（或拿不到 id）：按配置决定念不念
            bool unknown = string.IsNullOrEmpty(lineKey);
            bool wantRead = Plugin.CfgReadUnvoiced == null || Plugin.CfgReadUnvoiced.Value;
            if (unknown && Plugin.CfgSpeakWhenUnknown != null) wantRead = Plugin.CfgSpeakWhenUnknown.Value;
            if (!wantRead)
            {
                if (Plugin.CfgDiagLog != null && Plugin.CfgDiagLog.Value)
                {
                    A11yHost.Diag("Reader.skip[" + src + "] id=" + lineKey + " => 配置为不朗读无配音剧情");
                }
                return;
            }

            if (Plugin.CfgDiagLog != null && Plugin.CfgDiagLog.Value)
            {
                A11yHost.Diag("Reader.say[" + src + "] id=" + lineKey + " raw=" + raw + " => " + line);
            }

            try { Repeat.Say(line); }
            catch (Exception e) { A11yHost.Diag("Reader.Repeat.Say: " + e.Message); }
        }
    }
}



