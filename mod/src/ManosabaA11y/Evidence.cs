using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ManosabaA11y
{
    /// <summary>
    /// 「出示证据 / 选证物」这一步的支持代码。
    ///
    /// 游戏侧事实（re\decompiled\signatures 实查）：
    /// - `WitchTrials.Models.WitchTrialsNaninovelManager.IsPresentEvidenceMode`：是否处于「要出示证据」模式。
    /// - `WitchTrials.Views.WitchBookPageBase.Show(bool asPresentEvidenceMode)` 与
    ///   `_presentEvidenceButton`（`WitchTrialsButton`）、`OnPresentEvidenceAsync()` → `UniTask&lt;string&gt;`。
    /// - `WitchBookScreen._confirmToChoiceEvidenceDialogData`（`GigaCreation.Essentials.Ui.DialogData`）
    ///   说明按下「出示证据」之后会弹一个确认对话框，对话框实现是
    ///   `GigaCreation.Essentials.Ui.RebuildableDialog`（`MessageLabels` / `ResponseButtons`）。
    /// - 剧本命令 `@choiceEvidence` 的参数是 `CorrectEvidenceId`（例：Trial04 的 `1-10`）。
    ///
    /// 当前阶段只做**取证转储**（MANOSABA_A11Y_EVDIAG=1）：先搞清真实界面里有哪些控件、怎么点，
    /// 再写交互（本作按钮只认真实输入注入，见 MapChoice.ClickByInput 的注释）。
    /// </summary>
    internal static class Evidence
    {
        internal static bool IsMode { get; private set; }

        private static WitchTrials.Models.WitchTrialsNaninovelManager _mgr;
        private static bool _warnedMgr;
        private static float _nextFind;
        private static float _nextDump;
        private static int _scannedScript = int.MinValue;

        internal static void Tick()
        {
            float now = Time.unscaledTime;
            if (_mgr == null && now >= _nextFind)
            {
                _nextFind = now + 5f;
                try
                {
                    // WitchTrialsNaninovelManager 是纯托管服务（: Il2CppSystem.Object，不是 UnityEngine.Object），
                    // 拿不到 FindObjectOfType，只能从 Naninovel 的服务定位器取接口再 TryCast（实查签名确认）。
                    if (Naninovel.Engine.Initialized)
                    {
                        var svc = Naninovel.Engine.GetService<WitchTrials.Models.IWitchTrialsNaninovelManager>();
                        if (svc != null) _mgr = svc.TryCast<WitchTrials.Models.WitchTrialsNaninovelManager>();
                        if (_mgr != null) A11yHost.Diag("EVIDENCE: 找到 WitchTrialsNaninovelManager（服务定位器）");
                    }
                }
                catch (Exception e)
                {
                    if (!_warnedMgr) { _warnedMgr = true; A11yHost.Diag("EVIDENCE: 找不到 manager: " + e.Message); }
                }
            }

            if (_mgr != null)
            {
                try { IsMode = _mgr.IsPresentEvidenceMode; }
                catch (Exception e) { A11yHost.Diag("EVIDENCE: 读 IsPresentEvidenceMode 失败: " + e.Message); }
            }

            // 交互层：只有游戏自己把 IsPresentEvidenceMode 置真（=剧本走到 @choiceEvidence）才工作。
            // 正常阅读时该标志恒 false，所以这块不会干扰平时的魔女手册浏览。
            try
            {
                // 门控（0.0.0.9 实机标定）：`IsPresentEvidenceMode` 在真正的选证物界面里
                // 仍然是 **false**（ev1 实测，手册确实被限制成只剩证物页、PresentEvidenceButton 已亮），
                // 所以不能只信它。改用「手册可见 + 允许类别被限定（RestrictionCategories 非空）」判定，
                // 这两个条件在普通阅读态都不成立（dump 里 catN=-1）。
                if (Plugin.TestEvForce || IsMode || BookIsEvidenceMode()) UpdateChoice();
                else ResetChoice();
            }
            catch (Exception e) { A11yHost.Diag("EVIDENCE 交互: " + e.Message); }

            if (!Plugin.CfgEvDiag) return;
            if (now < _nextDump) return;
            _nextDump = now + 6f;
            try { Dump(); } catch (Exception e) { A11yHost.Diag("EVIDENCE dump: " + e.Message); }
        }

        /// <summary>一次性扫描：在**当前播放的剧本**里找 `@choiceEvidence` 那一行的行号。</summary>
        /// <remarks>
        /// 为什么要这个：`@choiceEvidence` 只在魔女审判的剧本里出现，而审判要靠正常流程
        /// 打半天才走到。找到行号之后就能用 `LoadAndPlayAtLine(script, 行号, 0)` 一步到位。
        /// 只记行号与命令类型名，不记台词（IP 红线）。
        /// </remarks>
        private static void ScanScriptForChoiceEvidence(Naninovel.Script script)
        {
            if (script == null) return;
            int id;
            try { id = script.GetInstanceID(); } catch { return; }
            if (_scannedScript == id) return;
            _scannedScript = id;

            // 实查（ilspycmd）：`Script.lines` 是 **public 字段** `Il2CppReferenceArray<ScriptLine>`，
            // 而 `Lines` 属性返回的 IReadOnlyList 在 interop 里不是 List（TryCast 失败，实测日志
            // 「CHOICEEV: Lines 不是 List」），所以直接读字段。
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Naninovel.ScriptLine> lines = null;
            try { lines = script.lines; } catch (Exception e) { A11yHost.Diag("CHOICEEV: 取 lines 字段失败 " + e.Message); return; }
            if (lines == null) { A11yHost.Diag("CHOICEEV: lines 为空"); return; }

            int found = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                Naninovel.ScriptLine line = lines[i];
                if (line == null) continue;
                // 每条命令行只挂**一个**命令（实查 Naninovel.CommandScriptLine 只有 `command` 字段）
                Naninovel.CommandScriptLine csl = line.TryCast<Naninovel.CommandScriptLine>();
                if (csl == null) continue;
                Naninovel.Command c = null;
                try { c = csl.command; } catch { }
                if (c == null) continue;
                if (c.TryCast<WitchTrials.Views.ChoiceEvidence>() != null)
                {
                    found++;
                    A11yHost.Diag("CHOICEEV: script=" + script.Path + " line=" + i + " lineIndex=" + csl.LineIndex);
                }
            }
            A11yHost.Diag("CHOICEEV: 扫描完成 script=" + script.Path + " lines=" + lines.Length + " 命中=" + found);
        }

        /// <summary>一次性转储：只记类型/对象名/可见性，不记台词原文（IP 红线）。</summary>
        private static void Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("EVIDENCE dump mode=").Append(IsMode);

            // 播放位置：用来确认「有没有真的落到 @choiceEvidence 那一行」。
            // 只记索引/命令类型名，不记台词（IP 红线）。
            try
            {
                Naninovel.IScriptPlayer sp = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>();
                if (sp != null)
                {
                    sb.Append(" idx=").Append(sp.PlayedIndex);
                    string cmd = "null";
                    try
                    {
                        Naninovel.Command c = sp.PlayedCommand;
                        if (c != null) cmd = c.GetType().Name;
                    }
                    catch { cmd = "?"; }
                    sb.Append(" cmd=").Append(cmd);
                    string path = "";
                    try
                    {
                        Naninovel.Script sc = sp.PlayedScript;
                        path = sc == null ? "" : sc.Path;
                        if (sc != null) ScanScriptForChoiceEvidence(sc);
                    }
                    catch (Exception se) { A11yHost.Diag("CHOICEEV: " + se.Message); }
                    sb.Append(" script=").Append(string.IsNullOrEmpty(path) ? "?" : path);
                    sb.Append(" playing=").Append(sp.Playing);
                }
            }
            catch (Exception e) { sb.Append(" idx=err(").Append(e.Message).Append(")"); }

            try
            {
                var uis = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchBookUi>();
                sb.Append(" ui=").Append(uis == null ? 0 : uis.Length);
                if (uis != null)
                {
                    for (int i = 0; i < uis.Length && i < 3; i++)
                    {
                        var ui = uis[i];
                        if (ui == null) continue;
                        sb.Append(" [ui").Append(i).Append(" active=").Append(ui.gameObject.activeInHierarchy);
                        try
                        {
                            var scr = ui._witchBookScreen;
                            if (scr != null)
                            {
                                sb.Append(" scrMode=").Append((int)scr.PresentationMode);
                                int catN = -1;
                                var cats = scr.RestrictionCategories;
                                if (cats != null)
                                {
                                    var lst = cats.TryCast<Il2CppSystem.Collections.Generic.List<WitchTrials.Models.WitchBookCategory>>();
                                    catN = lst != null ? lst.Count : -2;
                                }
                                sb.Append(" catN=").Append(catN);
                                int areaN = -1;
                                var areas = scr.RestrictionAreas;
                                if (areas != null)
                                {
                                    var lst = areas.TryCast<Il2CppSystem.Collections.Generic.List<WitchTrials.Models.MapAreaKind>>();
                                    areaN = lst != null ? lst.Count : -2;
                                }
                                sb.Append(" areaN=").Append(areaN);
                            }
                        }
                        catch (Exception e) { sb.Append(" scrErr=").Append(e.GetType().Name); }
                        sb.Append(']');
                    }
                }
            }
            catch (Exception e) { sb.Append(" uiErr=").Append(e.GetType().Name); }

            // 页：哪个页在显示（`_presentEvidenceButton` 定义在泛型基类上，逐作层拿不到，
            // 改为按对象名找 WitchTrialsButton；run 取证时再看真实名字是什么）
            try
            {
                var pages = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchBookPageBase>();
                sb.Append("\nEVIDENCE pages=").Append(pages == null ? 0 : pages.Length);
                for (int i = 0; pages != null && i < pages.Length && i < 8; i++)
                {
                    var p = pages[i];
                    if (p == null) continue;
                    sb.Append(" [").Append(p.GetType().Name).Append(" active=").Append(p.gameObject.activeInHierarchy).Append(']');
                }
            }
            catch (Exception e) { sb.Append(" pagesErr=").Append(e.GetType().Name); }

            // 「出示证据」按钮：手册里所有带 Present/Evidence 字样的按钮（名字来自实查，不猜）
            try
            {
                var btns = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchTrialsButton>();
                sb.Append("\nEVIDENCE wtBtns=").Append(btns == null ? 0 : btns.Length);
                int shown = 0;
                for (int i = 0; btns != null && i < btns.Length; i++)
                {
                    var b = btns[i];
                    if (b == null) continue;
                    string n = b.name == null ? "" : b.name.ToLowerInvariant();
                    bool hit = n.Contains("present") || n.Contains("evidence") || n.Contains("submit") || n.Contains("confirm");
                    if (!hit) continue;
                    if (shown++ >= 8) break;
                    sb.Append(" [").Append(b.name).Append("/act=").Append(b.gameObject.activeInHierarchy);
                    var sel = b.TryCast<Selectable>();
                    if (sel != null) sb.Append("/inter=").Append(sel.interactable);
                    try { sb.Append("/vis=").Append(UiVis.Visible(b)); } catch (Exception) { }
                    sb.Append(']');
                }
            }
            catch (Exception e) { sb.Append(" wtErr=").Append(e.GetType().Name); }

            // 线索卡（手册底部的卡片按钮）
            try
            {
                var items = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchBookItemButton>();
                int live = 0;
                sb.Append("\nEVIDENCE items=").Append(items == null ? 0 : items.Length);
                for (int i = 0; items != null && i < items.Length; i++)
                {
                    var it = items[i];
                    if (it == null || !it.gameObject.activeInHierarchy) continue;
                    bool visItem = true;
                    try { visItem = UiVis.Visible(it); } catch (Exception) { }
                    if (!visItem) continue;
                    if (live < 12)
                    {
                        string lbl = "";
                        try
                        {
                            if (it._label != null)
                            {
                                lbl = it._label.text ?? "";
                                lbl = System.Text.RegularExpressions.Regex.Replace(lbl, "<[^<>]{1,120}>", "");
                                lbl = lbl.Replace("\r", "").Replace("\n", "");
                                lbl = TextProc.Clean(lbl);
                            }
                        }
                        catch (Exception) { lbl = "?"; }
                        sb.Append(" [").Append(live).Append(':').Append(lbl).Append(']');
                    }
                    live++;
                }
                sb.Append(" live=").Append(live);
            }
            catch (Exception e) { sb.Append(" itemsErr=").Append(e.GetType().Name); }

            // 确认对话框（RebuildableDialog）
            try
            {
                var dlgs = Resources.FindObjectsOfTypeAll<GigaCreation.Essentials.Ui.RebuildableDialog>();
                sb.Append("\nEVIDENCE dialogs=").Append(dlgs == null ? 0 : dlgs.Length);
                for (int i = 0; dlgs != null && i < dlgs.Length && i < 4; i++)
                {
                    var d = dlgs[i];
                    if (d == null) continue;
                    sb.Append(" [dlg").Append(i).Append(" active=").Append(d.gameObject.activeInHierarchy);
                    int msgN = -1, btnN = -1;
                    try
                    {
                        var ml = d.MessageLabels;
                        if (ml != null)
                        {
                            var lst = ml.TryCast<Il2CppSystem.Collections.Generic.List<GigaCreation.Essentials.Ui.RebuildableLabel>>();
                            msgN = lst != null ? lst.Count : -2;
                        }
                    }
                    catch (Exception) { msgN = -3; }
                    try
                    {
                        var rb = d.ResponseButtons;
                        if (rb != null)
                        {
                            var lst = rb.TryCast<Il2CppSystem.Collections.Generic.List<GigaCreation.Essentials.Ui.RebuildableButton>>();
                            btnN = lst != null ? lst.Count : -2;
                        }
                    }
                    catch (Exception) { btnN = -3; }
                    sb.Append(" msg=").Append(msgN).Append(" btn=").Append(btnN);
                    try { sb.Append(" vis=").Append(UiVis.Visible(d)); } catch (Exception) { }
                    try
                    {
                        var ml = d.MessageLabels;
                        var lst = ml == null ? null : ml.TryCast<Il2CppSystem.Collections.Generic.List<GigaCreation.Essentials.Ui.RebuildableLabel>>();
                        if (lst != null && lst.Count > 0)
                        {
                            var tmps = lst[0].GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                            if (tmps != null && tmps.Length > 0)
                                sb.Append(" txt=").Append(TextProc.Clean(tmps[0].text ?? ""));
                        }
                    }
                    catch (Exception) { }
                    try
                    {
                        var rb = d.ResponseButtons;
                        var lst2 = rb == null ? null : rb.TryCast<Il2CppSystem.Collections.Generic.List<GigaCreation.Essentials.Ui.RebuildableButton>>();
                        if (lst2 != null)
                        {
                            for (int k = 0; k < lst2.Count; k++)
                            {
                                var tmps = lst2[k].GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                                string bt = tmps != null && tmps.Length > 0 ? TextProc.Clean(tmps[0].text ?? "") : "";
                                sb.Append(" b").Append(k).Append('=').Append(bt);
                            }
                        }
                    }
                    catch (Exception) { }
                    sb.Append(']');
                }
            }
            catch (Exception e) { sb.Append(" dlgErr=").Append(e.GetType().Name); }

            A11yHost.Diag(sb.ToString());
        }

        /// <summary>
        /// 取证用：调游戏自己的 <c>WitchBookUi.ChoiceEvidenceAsync(Clue)</c>，让手册真正进入
        /// 「选证物 / 出示证据」模式（等价于剧本里的 <c>@choiceEvidence Clue ...</c> 打开界面那一步）。
        /// 只用于自动化取证：它返回的 <c>UniTask&lt;string&gt;</c> 没人接结果，所以剧情不会因此推进。
        /// </summary>
        // ==================== 出示证据（选证物）交互 ====================
        //
        // 设计原则（和地图选择 MapChoice 一致）：
        // - 判据不用「猜」的可见性，而用游戏自己的状态：manager.IsPresentEvidenceMode。
        // - 列表来自当前页里**可见的**线索卡（WitchBookItemButton），名字取卡的 _label。
        // - 点卡/点「出示证据」都用 InputInject 发真实输入：本作按钮只认真实输入注入
        //   （合成派发 ExecuteEvents + onClick.Invoke 实测游戏不认，见 MapChoice 注释）。
        // - 按数字键 → 先复述名字 → 点卡；卡片点中后 0.8s 自动点「出示证据」→ 再处理确认对话框。

        private const int PageSize = 9;

        private sealed class Item
        {
            internal WitchTrials.Views.WitchBookItemButton Btn;
            internal string Name;
        }

        private static readonly System.Collections.Generic.List<Item> _items =
            new System.Collections.Generic.List<Item>();
        private static string _announcedKey = "";
        private static float _nextScan;
        private static int _page;
        private static float _clickPresentAt;
        private static bool _presentClicked;
        /// <summary>
        /// 卡实例 → 名字 缓存。
        /// 为什么必须有：`Scan()` 每 0.5 秒重建一次列表，而证物卡本身没有文字，
        /// 如果不缓存，`NeedsNames()` 永远为真 → 会每 0.5 秒重新枚举一遍名字（ev2 实测死循环）。
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<int, string> _nameCache =
            new System.Collections.Generic.Dictionary<int, string>();
        private static int _selectedIndex;
        private static int _enumIndex = -1;
        private static float _enumAt;
        private static readonly System.Collections.Generic.List<WitchTrials.Views.WitchBookItemButton> _enumCands =
            new System.Collections.Generic.List<WitchTrials.Views.WitchBookItemButton>();
        private static readonly System.Collections.Generic.List<string> _enumNames =
            new System.Collections.Generic.List<string>();
        private static float _dialogAt;
        private static string _dialogKey = "";
        private static float _dialogActionAt;
        private static int _dialogPick;
        private static float _evPickAt;
        private static float _nextScanLog;

        private static void ResetChoice()
        {
            if (_items.Count > 0) _items.Clear();
            _announcedKey = "";
            _dialogKey = "";
            _page = 0;
            _clickPresentAt = 0f;
            _presentClicked = false;
            _enumIndex = -1;
            _selectedIndex = 0;
            _enumCands.Clear();
            _enumNames.Clear();
        }

        private static void UpdateChoice()
        {
            float now = Time.unscaledTime;

            // 1) 确认对话框：出现时优先念它，并且此时不再念线索列表
            if (HandleDialog(now)) return;

            // 2) 扫描当前页可见的线索卡（0.5s 一次，6 FPS 下够用）
            if (now >= _nextScan)
            {
                _nextScan = now + 0.5f;
                Scan();
            }

            if (_items.Count == 0) return;

            // 3) 名字缺（证物卡是纯图标）→ 先把名字枚举出来，再播报
            if (_enumIndex >= 0) { EnumStep(now); return; }
            if (NeedsNames()) { StartEnum(); return; }

            // 4) 内容变了才播报（避免每帧重复念）
            string key = Key();
            if (key != _announcedKey)
            {
                _announcedKey = key;
                Announce();
            }

            // 4) 数字键选择（翻页时按页码偏移）
            int firstOnPage = _page * PageSize;
            for (int i = 1; i <= PageSize && firstOnPage + i <= _items.Count; i++)
            {
                if (!FocusGate.CanReadChoiceKeys()) continue;
                if (!Tapped(KeyCode.Alpha0 + i) && !Tapped(KeyCode.Keypad0 + i)) continue;
                Activate(firstOnPage + i);
                return;
            }

            // 5) 翻页（超过 9 件时；按钮先选卡，所以用 < > 太冷僻，这里用 PageUp/PageDown）
            if (_items.Count > PageSize)
            {
                int pages = (_items.Count + PageSize - 1) / PageSize;
                if (KeyEdge.Pressed(KeyCode.PageDown))
                {
                    _page = (_page + 1) % pages;
                    _announcedKey = "";
                    Repeat.Say("第 " + (_page + 1) + " 页，共 " + pages + " 页。", true);
                }
                else if (KeyEdge.Pressed(KeyCode.PageUp))
                {
                    _page = (_page + pages - 1) % pages;
                    _announcedKey = "";
                    Repeat.Say("第 " + (_page + 1) + " 页，共 " + pages + " 页。", true);
                }
            }

            // 4.6) 回车 / 空格 → 出示当前选中的证物（由玩家自己触发，我们不代劳）
            if (Tapped(KeyCode.Return) || Tapped(KeyCode.KeypadEnter) || Tapped(KeyCode.Space))
            {
                if (_selectedIndex > 0)
                {
                    _presentClicked = false;
                    ClickPresentButton();
                    return;
                }
                Repeat.Say("请先用数字键选一件证物。");
                return;
            }

            // 6) 测试钩子（MANOSABA_A11Y_TEST_EVPICK=1）：播报 → 6 秒后自动选第 1 件 →
            //    再 6 秒后模拟「回车出示」。只在测试里用，正式游玩必须由玩家自己按回车。
            if (Plugin.TestEvPick && _announcedKey.Length > 0)
            {
                if (_selectedIndex == 0)
                {
                    if (_evPickAt <= 0f) _evPickAt = now + 6f;
                    else if (now >= _evPickAt)
                    {
                        _evPickAt = 0f;
                        A11yHost.Diag("EVIDENCE 测试钩子：自动选择第 1 件");
                        Activate(1);
                        return;
                    }
                }
                else if (!_presentClicked)
                {
                    if (_evPickAt <= 0f) _evPickAt = now + 6f;
                    else if (now >= _evPickAt)
                    {
                        _evPickAt = 0f;
                        _presentClicked = true;
                        A11yHost.Diag("EVIDENCE 测试钩子：自动按回车出示");
                        ClickPresentButton();
                        return;
                    }
                }
            }
        }

        /// <summary>手册是否处在「选证物」态（被命令限制成只剩证物页）。</summary>
        private static bool BookIsEvidenceMode()
        {
            try
            {
                var uis = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchBookUi>();
                for (int i = 0; i < uis.Length; i++)
                {
                    var ui = uis[i];
                    if (ui == null || ui.gameObject == null || !ui.gameObject.activeInHierarchy) continue;
                    // 不要用 UiVis.Visible 判整本手册：选证物时弹出的确认对话框是一层全屏遮罩
                    // （`blocksRaycasts`/`interactable` 被改），UiVis 会把底下的手册判成「隐藏」，
                    // 于是整个交互层停摆——ev9 实测：对话框出现后连日志都不再输出。
                    WitchTrials.Views.WitchBookScreen scr = null;
                    try { scr = ui._witchBookScreen; } catch (Exception) { }
                    if (scr == null) continue;
                    // 必须「允许类别里包含 Clue」才算选证物态：
                    // 地图选择（@choiceMapArea）也会把手册限制成只剩地图页，
                    // 那时 RestrictionCategories 也是非空的——只看「非空」会把地图选择
                    // 误判成选证物，读屏就会在地图上念「请出示证据」（实机 tri2 日志里
                    // catN=1 与 areaN=1 同时出现，说明两类限制会并存）。
                    int catN = -1;
                    try
                    {
                        var cats = scr.RestrictionCategories;
                        var lst = cats == null ? null
                            : cats.TryCast<Il2CppSystem.Collections.Generic.List<WitchTrials.Models.WitchBookCategory>>();
                        if (lst != null)
                        {
                            catN = lst.Count;
                        }
                    }
                    catch (Exception) { }
                    // 用「线索页（Page_Clues）是否处于激活」来区分证物选择与地图选择：
                    // 选证物时手册被限制成只剩证物页，地图选择时只剩地图页。
                    // （实机 ev12：只要求「类别里有 Clue」会把真正的选证物也挡掉 —— 游戏给的
                    //  RestrictionCategories 并不总是 Clue，所以改成看页面。）
                    bool cluePageActive = false;
                    try
                    {
                        Transform cluePage = FindByName(ui.transform, "Page_Clues", 6);
                        cluePageActive = cluePage != null && cluePage.gameObject != null
                                         && cluePage.gameObject.activeInHierarchy;
                    }
                    catch (Exception) { }
                    if (catN < 1 || !cluePageActive) continue;

                    // 地图限制非空 = 正在进行「选择地点」，交给 MapChoice，别抢
                    int areaN = -1;
                    try
                    {
                        var areas = scr.RestrictionAreas;
                        var al = areas == null ? null
                            : areas.TryCast<Il2CppSystem.Collections.Generic.List<WitchTrials.Models.MapAreaKind>>();
                        if (al != null) areaN = al.Count;
                    }
                    catch (Exception) { }
                    if (areaN > 0) continue;

                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>卡片上有没有字：没有就得靠枚举（点一张、读页面标题）。</summary>
        private static bool NeedsNames()
        {
            if (_items.Count == 0) return false;
            for (int i = 0; i < _items.Count && i < 12; i++)
                if (string.IsNullOrEmpty(_items[i].Name)) return true;
            return false;
        }

        private static void StartEnum()
        {
            _enumCands.Clear();
            _enumNames.Clear();
            int n = _items.Count < 12 ? _items.Count : 12;
            for (int i = 0; i < n; i++)
            {
                _enumCands.Add(_items[i].Btn);
                _enumNames.Add("");
            }
            _enumIndex = 0;
            _enumAt = Time.unscaledTime;
            A11yHost.Diag("EVIDENCE 开始枚举名字: 卡=" + n);
        }

        /// <summary>
        /// 一张一张点、读页面标题，把名字补上。
        /// 为什么只能这样：证物卡是**纯图标**（`_label` 空、子树也没有 TMP），名字只出现在
        /// 选中之后页面右侧的标题里（ev107–ev110 实测）。点卡只是「选中」，不会提交。
        /// </summary>
        private static void EnumStep(float now)
        {
            if (now < _enumAt) return;
            if (_enumIndex > 0 && _enumIndex - 1 < _enumNames.Count)
            {
                string t = PageTitle();
                if (!string.IsNullOrEmpty(t)) _enumNames[_enumIndex - 1] = t;
            }
            if (_enumIndex >= _enumCands.Count)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].Name = i < _enumNames.Count && !string.IsNullOrEmpty(_enumNames[i])
                        ? _enumNames[i] : ("证物 " + (i + 1));
                    try
                    {
                        int iid = _items[i].Btn == null ? 0 : _items[i].Btn.GetInstanceID();
                        if (iid != 0) _nameCache[iid] = _items[i].Name;
                    }
                    catch (Exception) { }
                }
                if (_enumCands.Count > 0) ClickButton(_enumCands[0]._button);
                StringBuilder lg = new StringBuilder("EVIDENCE 名字枚举完成:");
                for (int i = 0; i < _enumNames.Count; i++) lg.Append(' ').Append(i + 1).Append('=').Append(_enumNames[i]);
                A11yHost.Diag(lg.ToString());
                _enumIndex = -1;
                _announcedKey = "";
                return;
            }
            ClickButton(_enumCands[_enumIndex]._button);
            _enumAt = now + 0.55f;
            _enumIndex++;
        }

        /// <summary>线索页右侧那块的标题文字（选中一张卡之后才有）。</summary>
        private static string PageTitle()
        {
            try
            {
                Transform page = FindVisiblePage();
                if (page == null) return "";
                var tmps = page.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                if (tmps == null) return "";
                for (int i = 0; i < tmps.Length; i++)
                {
                    var tm = tmps[i];
                    if (tm == null || tm.gameObject == null || !tm.gameObject.activeInHierarchy) continue;
                    bool vis = true;
                    try { vis = UiVis.Visible(tm); } catch (Exception) { }
                    if (!vis) continue;
                    string t = CleanText(tm.text ?? "");
                    if (t.Length >= 2 && t.Length <= 40) return t;
                }
            }
            catch (Exception e) { A11yHost.Diag("EVIDENCE.PageTitle: " + e.Message); }
            return "";
        }

        private static bool Tapped(KeyCode k)
        {
            try { return KeyEdge.Pressed(k); } catch (Exception) { return false; }
        }

        private static string Key()
        {
            StringBuilder sb = new StringBuilder();
            int first = _page * PageSize;
            for (int i = 0; i < PageSize && first + i < _items.Count; i++)
                sb.Append(_items[first + i].Name).Append('|');
            return "_p" + _page + ":" + sb.ToString();
        }

        /// <summary>扫当前页里可见的线索卡（含文字、有按钮、可交互）。</summary>
        private static void Scan()
        {
            float now2 = Time.unscaledTime;
            _items.Clear();
            Transform root = FindBookRoot();
            if (root == null) return;
            var btns = root.GetComponentsInChildren<WitchTrials.Views.WitchBookItemButton>(true);
            if (btns == null) return;
            int nActive = 0, nVis = 0, nBtn = 0, nName = 0;
            for (int i = 0; i < btns.Length; i++)
            {
                var it = btns[i];
                if (it == null || it.gameObject == null) continue;
                if (!it.gameObject.activeInHierarchy) continue;
                nActive++;
                bool vis = true;
                try { vis = UiVis.Visible(it); } catch (Exception) { }
                if (!vis) continue;
                nVis++;
                UnityEngine.UI.Button b = null;
                try { b = it._button; } catch (Exception) { }
                if (b == null || !b.interactable) continue;
                nBtn++;
                string name = CardName(it);
                if (string.IsNullOrEmpty(name))
                {
                    int iid = 0;
                    try { iid = it.GetInstanceID(); } catch (Exception) { }
                    string cached;
                    if (iid != 0 && _nameCache.TryGetValue(iid, out cached)) name = cached;
                }
                if (!string.IsNullOrEmpty(name)) nName++;
                var e = new Item();
                e.Btn = it;
                e.Name = name;
                _items.Add(e);
            }
            if (_items.Count > 0) A11yHost.Diag("EVIDENCE 线索卡=" + _items.Count + " 页=" + (_page + 1));
            else if (now2 >= _nextScanLog)
            {
                _nextScanLog = now2 + 5f;
                A11yHost.Diag("EVIDENCE 扫描: 页=" + (root == null ? "<无>" : root.name)
                              + " 卡=" + (btns == null ? -1 : btns.Length)
                              + " 激活=" + nActive + " 可见=" + nVis + " 有按钮=" + nBtn + " 有名字=" + nName);
            }
        }

        /// <summary>当前显示的线索页（`Page_Clues`）；找不到就退回整本手册。</summary>
        private static Transform FindVisiblePage()
        {
            try
            {
                var uis = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchBookUi>();
                for (int i = 0; i < uis.Length; i++)
                {
                    var ui = uis[i];
                    if (ui == null || ui.gameObject == null || !ui.gameObject.activeInHierarchy) continue;
                    Transform t = ui.transform;
                    Transform page = FindByName(t, "Page_Clues", 6);
                    return page != null ? page : t;
                }
            }
            catch (Exception e) { A11yHost.Diag("EVIDENCE 找线索页: " + e.Message); }
            return null;
        }

        /// <summary>
        /// 手册根节点（`WitchBookUi`）。
        ///
        /// **为什么不是 `Page_Clues`**：底部那排胶片式卡片是**所有页共用**的
        /// `Wrapper/ItemList`，不在 `Page_Clues` 底下（ev107 实测 `Page_Clues 卡=0`）。
        /// 所以从根开始扫、再用 `UiVis.Visible` 只保留当前页可见的那几张。
        /// </summary>
        private static Transform FindBookRoot()
        {
            try
            {
                var uis = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchBookUi>();
                for (int i = 0; i < uis.Length; i++)
                {
                    var ui = uis[i];
                    if (ui == null || ui.gameObject == null || !ui.gameObject.activeInHierarchy) continue;
                    return ui.transform;
                }
            }
            catch (Exception e) { A11yHost.Diag("EVIDENCE 找手册根: " + e.Message); }
            return null;
        }

        private static Transform FindByName(Transform root, string name, int depth)
        {
            if (root == null || depth < 0) return null;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c == null) continue;
                if (c.name == name) return c;
                Transform r = FindByName(c, name, depth - 1);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>
        /// 取一张线索卡的显示名。
        ///
        /// ev109 实测：手册底部胶片条上的证物卡**只有图、`_label` 是空串**
        /// （不像楼层卡那样带文字），所以先读 `_label`，为空就在子树里找第一段
        /// 有效 TMP 文本（地图卡就是这条路取到「1F」「会客厅」的）。
        /// </summary>
        private static string CardName(WitchTrials.Views.WitchBookItemButton it)
        {
            try
            {
                string fromLabel = CleanText(SafeLabel(it));
                if (!string.IsNullOrEmpty(fromLabel)) return fromLabel;
                var tmps = it.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                if (tmps == null) return "";
                for (int i = 0; i < tmps.Length && i < 6; i++)
                {
                    var tm = tmps[i];
                    if (tm == null || tm.gameObject == null || !tm.gameObject.activeInHierarchy) continue;
                    string s2 = CleanText(tm.text ?? "");
                    if (!string.IsNullOrEmpty(s2)) return s2;
                }
                return "";
            }
            catch (Exception) { return ""; }
        }

        private static string SafeLabel(WitchTrials.Views.WitchBookItemButton it)
        {
            try { return it._label == null ? "" : (it._label.text ?? ""); }
            catch (Exception) { return ""; }
        }

        private static string CleanText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            raw = System.Text.RegularExpressions.Regex.Replace(raw, "<[^<>]{1,120}>", "");
            raw = raw.Replace("\r", "").Replace("\n", "");
            string s = TextProc.Clean(raw);
            return TextProc.IsTrivial(s) ? "" : s;
        }

        private static void Announce()
        {
            int first = _page * PageSize;
            int n = 0;
            while (n < PageSize && first + n < _items.Count) n++;
            StringBuilder sb = new StringBuilder();
            sb.Append("请出示证据。可选的证物共 ").Append(_items.Count).Append(" 件");
            if (_page > 0 || _items.Count > PageSize) sb.Append("（第 ").Append(_page + 1).Append(" 页）");
            sb.Append("：");
            for (int i = 0; i < n; i++)
            {
                sb.Append(i + 1).Append('、').Append(_items[first + i].Name).Append('。');
            }
            if (_items.Count > PageSize) sb.Append("用 PageUp 或 PageDown 翻页。");
            sb.Append("按数字键选择。");
            string line = sb.ToString();
            A11yHost.Diag("EVIDENCE 播报: " + line);
            Repeat.Say(line, true);
            Reader.Remember(line);
        }

        private static void Activate(int oneBased)
        {
            int idx = oneBased - 1;
            if (idx < 0 || idx >= _items.Count) return;
            Item e = _items[idx];
            _selectedIndex = oneBased;
            // ★ 先点后念：原来是“先说已选中、再去点”，点击失败时读屏就撤了谎。
            //   ClickButton 返回值代表注入点击是否真的发出（坐标越界、拿不到
            //   RectTransform 都会返回 false）。
            bool clicked = ClickButton(e.Btn != null ? e.Btn._button : null);
            A11yHost.Diag("EVIDENCE 数字键 " + oneBased + " -> " + e.Name + " clicked=" + clicked);
            Repeat.Say(clicked ? (e.Name + "。已选中。按回车出示这件证据。")
                               : (e.Name + "。选中没成功，请再按一次。"));
            _clickPresentAt = 0f;
            _presentClicked = false;
        }

        /// <summary>点「出示证据」按钮（当前页里可见的那个）。</summary>
        private static void ClickPresentButton()
        {
            try
            {
                var btns = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchTrialsButton>();
                for (int i = 0; i < btns.Length; i++)
                {
                    var b = btns[i];
                    if (b == null || b.gameObject == null) continue;
                    if (b.name.IndexOf("PresentEvidence", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    if (!b.gameObject.activeInHierarchy) continue;
                    bool vis = true;
                    try { vis = UiVis.Visible(b); } catch (Exception) { }
                    // `WitchTrialsButton` 是 MonoBehaviour 壳（`RebuildableButton : MonoBehaviour`），
                    // 不是 Selectable —— ev3 实测 `TryCast<Selectable>()` 得到 null，ClickButton 直接静默返回，
                    // 表现就是「念了『已出示证据』但游戏毫无反应」。要往它的 `_uiButton` 里点。
                    UnityEngine.UI.Button inner = null;
                    try { inner = b._uiButton; } catch (Exception) { }
                    if (inner == null) inner = b.GetComponentInChildren<UnityEngine.UI.Button>(true);
                    Selectable sel = inner;
                    if (sel == null) { A11yHost.Diag("EVIDENCE: PresentEvidenceButton 里找不到 Unity Button"); continue; }
                    if (!vis || !sel.interactable) continue;
                    bool ok = ClickButton(sel);
                    A11yHost.Diag("EVIDENCE 点了「出示证据」 name=" + b.name + " ok=" + ok);
                    Repeat.Say(ok ? "已出示证据。" : "出示没成功，请再按一次回车。");
                    return;
                }
                A11yHost.Diag("EVIDENCE: 找不到可点的「出示证据」按钮");
            }
            catch (Exception e) { A11yHost.Diag("EVIDENCE 点出示失败: " + e.Message); }
        }

        /// <summary>真实输入点击：换算屏幕坐标后走 InputSystem 事件队列（本作只认这个）。</summary>
        private static bool ClickButton(Selectable sel)
        {
            if (sel == null) return false;
            try
            {
                bool ok = sel.TryCast<UnityEngine.UI.Button>() != null;
                UnityEngine.RectTransform rt = sel.transform.TryCast<UnityEngine.RectTransform>();
                if (rt == null) { A11yHost.Diag("EVIDENCE 注入点击：拿不到 RectTransform"); return false; }
                UnityEngine.Canvas canvas = sel.GetComponentInParent<UnityEngine.Canvas>();
                UnityEngine.Camera cam = null;
                if (canvas != null && canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay)
                    cam = canvas.worldCamera;
                // WorldSpace 画布（手册的「出示证据」按钮就在这层）没有 worldCamera，
                // 用 null 换算出来是负数屏幕坐标（ev4 实测 screen=-7,-2），点击落到屏幕外。
                if (canvas != null && canvas.renderMode == UnityEngine.RenderMode.WorldSpace && cam == null)
                    cam = UnityEngine.Camera.main;
                UnityEngine.Vector2 sp = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, rt.position);
                // 越界就不点（WorldSpace 换算失败时会得到负数坐标，点到屏幕外毫无反应，
                // 更糟的是可能点到别的按钮上）。日志里留下算出来的坐标是这台全黑桌面唯一的落点证据。
                if (sp.x < 0f || sp.y < 0f || sp.x > UnityEngine.Screen.width || sp.y > UnityEngine.Screen.height)
                {
                    A11yHost.Diag("EVIDENCE 注入点击放弃：坐标越界 name=" + sel.name
                                  + " screen=" + sp.x.ToString("0") + "," + sp.y.ToString("0")
                                  + " canvas=" + (canvas == null ? "?" : canvas.renderMode.ToString()));
                    return false;
                }
                InputInject.Click(sp.x, sp.y, 0.16f);
                A11yHost.Diag("EVIDENCE 注入点击 name=" + sel.name + " screen="
                              + sp.x.ToString("0") + "," + sp.y.ToString("0")
                              + " canvas=" + (canvas == null ? "?" : canvas.renderMode.ToString()));
                return ok;
            }
            catch (Exception e) { A11yHost.Diag("EVIDENCE.ClickButton: " + e.Message); return false; }
        }

        /// <summary>确认对话框（「确定要出示这件证物吗？」）：念内容 + 按钮，数字键点。</summary>
        private static bool HandleDialog(float now)
        {
            try
            {
                var dlgs = Resources.FindObjectsOfTypeAll<GigaCreation.Essentials.Ui.RebuildableDialog>();
                for (int i = 0; i < dlgs.Length; i++)
                {
                    var d = dlgs[i];
                    if (d == null || d.gameObject == null || !d.gameObject.activeInHierarchy) continue;
                    bool vis = true;
                    try { vis = UiVis.Visible(d); } catch (Exception) { }
                    if (!vis) continue;
                    var resp = d.ResponseButtons;
                    var lst = resp == null ? null
                        : resp.TryCast<Il2CppSystem.Collections.Generic.List<GigaCreation.Essentials.Ui.RebuildableButton>>();
                    if (lst == null || lst.Count == 0) continue;

                    string msg = DialogText(d);
                    string key = msg + "|" + lst.Count;
                    if (key != _dialogKey)
                    {
                        _dialogKey = key;
                        StringBuilder sb = new StringBuilder();
                        if (!string.IsNullOrEmpty(msg)) sb.Append(msg).Append('。');
                        for (int k = 0; k < lst.Count; k++)
                        {
                            string t = ButtonText(lst[k]);
                            sb.Append(k + 1).Append('、').Append(string.IsNullOrEmpty(t) ? "确定" : t).Append('。');
                        }
                        sb.Append("按数字键选择。");
                        string line = sb.ToString();
                        A11yHost.Diag("EVIDENCE 对话框: " + line);
                        Repeat.Say(line, true);
                        Reader.Remember(line);
                    }

                    // 测试钩子（MANOSABA_A11Y_TEST_EVYES=1）：5 秒后自动按最后一个按钮（「是」）。
                    // 只用于自动化验收；正式游玩必须由玩家自己选，绝不能代答。
                    if (Plugin.TestEvYes)
                    {
                        if (_dialogAt <= 0f) _dialogAt = now + 5f;
                        else if (now >= _dialogAt)
                        {
                            _dialogAt = 0f;
                            var rb2 = lst[lst.Count - 1];
                            A11yHost.Diag("EVIDENCE 测试钩子：自动按对话框最后一个按钮");
                            if (rb2 != null) { try { ClickButton(rb2._uiButton); } catch (Exception) { } }
                            _dialogKey = "";
                            return true;
                        }
                    }

                    for (int k = 1; k <= lst.Count && k <= PageSize; k++)
                    {
                        if (!FocusGate.CanReadChoiceKeys()) continue;
                        if (!Tapped(KeyCode.Alpha0 + k) && !Tapped(KeyCode.Keypad0 + k)) continue;
                        var rb = lst[k - 1];
                        A11yHost.Diag("EVIDENCE 对话框数字键 " + k);
                        if (rb != null) { try { ClickButton(rb._uiButton); } catch (Exception) { } }
                        _dialogKey = "";
                        return true;
                    }
                    return true;
                }
                _dialogKey = "";
            }
            catch (Exception e) { A11yHost.Diag("EVIDENCE 对话框: " + e.Message); }
            return false;
        }

        private static string DialogText(GigaCreation.Essentials.Ui.RebuildableDialog d)
        {
            try
            {
                var ml = d.MessageLabels;
                var lst = ml == null ? null
                    : ml.TryCast<Il2CppSystem.Collections.Generic.List<GigaCreation.Essentials.Ui.RebuildableLabel>>();
                if (lst == null || lst.Count == 0) return "";
                var tmps = lst[0].GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                if (tmps == null || tmps.Length == 0) return "";
                string s = TextProc.Clean(tmps[0].text ?? "");
                return TextProc.IsTrivial(s) ? "" : s;
            }
            catch (Exception) { return ""; }
        }

        private static string ButtonText(GigaCreation.Essentials.Ui.RebuildableButton b)
        {
            try
            {
                if (b == null) return "";
                var tmps = b.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                if (tmps == null || tmps.Length == 0) return "";
                string s = TextProc.Clean(tmps[0].text ?? "");
                return TextProc.IsTrivial(s) ? "" : s;
            }
            catch (Exception) { return ""; }
        }

        /// <summary>手册 UI 是否已经存在（不要求可见）。</summary>
        internal static bool HasBookUi
        {
            get
            {
                try
                {
                    var uis = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchBookUi>();
                    return uis != null && uis.Length > 0 && uis[0] != null;
                }
                catch (Exception) { return false; }
            }
        }

        /// <summary>
        /// 取证用（MANOSABA_A11Y_TEST_EVCMD=2）：直接构造并执行游戏自己的 <c>ChoiceEvidence</c> 命令
        /// （`@choiceEvidence Clue 1-10` 的托管形态）。它能走完命令的完整流程：设置呈现模式、
        /// 限制类别、打开手册、等玩家按下「出示证据」——这是我们手搓 UI 调用做不到的。
        /// 只用于自动化取证：返回值没人接，剧情不会推进。
        /// </summary>
        internal static bool TryRunChoiceEvidenceCommand(string evidenceId)
        {
            try
            {
                var cmd = new WitchTrials.Views.ChoiceEvidence();
                if (!string.IsNullOrEmpty(evidenceId)) cmd.CorrectEvidenceId = evidenceId;
                Naninovel.UniTaskExtensions.Forget(cmd.Execute(NewToken()));
                A11yHost.Diag("EVIDENCE TEST: 已执行 ChoiceEvidence 命令（CorrectEvidenceId=" + evidenceId + "）");
                return true;
            }
            catch (Exception e)
            {
                A11yHost.Diag("EVIDENCE TEST: ChoiceEvidence 命令失败: " + e.Message);
                return true;
            }
        }

        /// <summary>
        /// 造一个可用的 <c>Naninovel.AsyncToken</c>。
        ///
        /// **重要教训**：`AsyncToken` 在 il2cpp 里是 **value type（struct）**，interop 的
        /// `token = null` 等于塞了个空 box，游戏内部一碰 `token.CancellationToken` 就
        /// NullReferenceException（我们前几次 `ChoiceEvidenceAsync(..., null)` 的 NRE 就是它）。
        /// 必须像游戏自己那样造一个默认 token：`new AsyncToken(default(CancellationToken))`。
        /// </summary>
        internal static Naninovel.AsyncToken NewToken()
        {
            try { return new Naninovel.AsyncToken(new Il2CppSystem.Threading.CancellationToken(), false); }
            catch (Exception e)
            {
                A11yHost.Diag("EVIDENCE: 造 AsyncToken 失败: " + e.Message);
                return default(Naninovel.AsyncToken);
            }
        }

        internal static bool TryStartChoiceEvidence()
        {
            try
            {
                if (!Naninovel.Engine.Initialized) return false;
                var uis = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchBookUi>();
                if (uis == null) return false;
                for (int i = 0; i < uis.Length; i++)
                {
                    var ui = uis[i];
                    if (ui == null) continue;
                    // 不再要求「手册已经打开」：ChoiceEvidenceAsync 自己负责打开界面，
                    // 之前手动打开再调用（屏幕上已有手册）时它内部 NRE，怀疑状态冲突。
                    A11yHost.Diag("EVIDENCE TEST: 用 ui[" + i + "] active=" + ui.gameObject.activeInHierarchy);

                    var cats = new Il2CppSystem.Collections.Generic.List<WitchTrials.Models.WitchBookCategory>();
                    cats.Add(WitchTrials.Models.WitchBookCategory.Clue);
                    var arg = cats.TryCast<Il2CppSystem.Collections.Generic.IReadOnlyList<WitchTrials.Models.WitchBookCategory>>();
                    A11yHost.Diag("EVIDENCE TEST: cats=" + cats.Count + " arg=" + (arg != null));

                    // 逐步 try/catch：哪一步抛异常都能在日志里看见（第一版整体调用只报一个 NRE，定位不了）
                    try { ui.SetPresentationMode(WitchTrials.Views.WitchBookPresentationMode.Local); A11yHost.Diag("EVIDENCE TEST: SetPresentationMode(Local) ok"); }
                    catch (Exception e) { A11yHost.Diag("EVIDENCE TEST: SetPresentationMode 失败: " + e.Message); }

                    if (arg != null)
                    {
                        try { ui.SetRestrictionCategories(arg); A11yHost.Diag("EVIDENCE TEST: SetRestrictionCategories ok"); }
                        catch (Exception e) { A11yHost.Diag("EVIDENCE TEST: SetRestrictionCategories 失败: " + e.Message); }
                    }

                    try
                    {
                        Naninovel.UniTaskExtensions.Forget(ui.ChoiceEvidenceAsync(arg, NewToken()));
                        A11yHost.Diag("EVIDENCE TEST: 已发起 ChoiceEvidenceAsync(Clue)");
                    }
                    catch (Exception e) { A11yHost.Diag("EVIDENCE TEST: ChoiceEvidenceAsync 失败: " + e.Message); }
                    return true;
                }
            }
            catch (Exception e)
            {
                A11yHost.Diag("EVIDENCE TEST: TryStartChoiceEvidence 失败: " + e.Message);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 取证用：对当前显示的手册页调用游戏自己的 <c>Show(asPresentEvidenceMode: true)</c>，
        /// 让「出示证据」按钮出现在界面上（正式流程里这步是 <c>@choiceEvidence</c> 命令做的）。
        /// 返回 true 表示已经调用过、不必再调。
        /// </summary>
        internal static bool TryShowPresentMode()
        {
            try
            {
                var pages = Resources.FindObjectsOfTypeAll<WitchTrials.Views.WitchBookPageBase>();
                if (pages == null) return false;
                for (int i = 0; i < pages.Length; i++)
                {
                    var p = pages[i];
                    if (p == null || !p.gameObject.activeInHierarchy) continue;
                    p.Show(true);
                    A11yHost.Diag("EVIDENCE TEST: 已对 " + p.GetType().Name + " 调用 Show(asPresentEvidenceMode: true)");
                    return true;
                }
            }
            catch (Exception e)
            {
                A11yHost.Diag("EVIDENCE TEST: Show(true) 失败: " + e.Message);
            }
            return false;
        }
    }
}
