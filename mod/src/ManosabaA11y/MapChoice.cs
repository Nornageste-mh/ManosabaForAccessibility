using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ManosabaA11y
{
    /// <summary>
    /// 魔女手册「地图」页的**地点选择**（探索/移动时出现）。
    ///
    /// 为什么需要（用户 2026-10-31 截图反馈「无法进入这些场景」）：
    ///   地图在剧情要求「去哪里」时不是一张静态图，而是**可点的地点列表**：
    ///   游戏用 `MapPage.InitializeAreas(restrictions)` → `MapFloor.Show(restrictions)`
    ///   → `MapArea.ChangeFocusButtonActivity(true)`，只把允许去的地点变成可点，
    ///   其余地点连名字都不显示（用户截图里只有 5 个地点有名字，就是被允许的那 5 个）。
    ///
    ///   读屏玩家在这里会卡死，原因有两层：
    ///     1) `MapAreaFocusButton` 本身没有任何 TMP 文字（名字在兄弟节点 `MapAreaLabel` 上），
    ///        平台层导航只能念「未命名控件」；
    ///     2) 手册打开时有一层全屏 CanvasGroup 挡住 UI 射线，平台的
    ///        `VisiblyClickable` 会把页内按钮判成「画面外或点不到」。
    ///
    /// 所以逐作层自己提供入口：**枚举当前可点的地点 → 编号播报 → 数字键直接触发**。
    ///
    /// 铁律（不扩大可达范围）：只在游戏自己把 `MapAreaFocusButton` 置为可点时
    /// 才把它算进列表；我们从不调用 `ChangeFocusButtonActivity(true)`（除了测试钩子）。
    /// </summary>
    internal static class MapChoice
    {
        private const float ScanInterval = 0.5f;

        private sealed class Entry
        {
            public WitchTrials.Views.MapArea Area;
            public string Name;
            public string Floor;      // 1F / 2F / 地下1F（来自祖先 MapFloor 名字）
            public Button Button;
        }

        private static readonly List<Entry> _entries = new List<Entry>();
        private static float _nextScanAt;
        private static string _announcedKey = "";
        private static Transform _scope;

        /// <summary>
        /// 游戏是否正处在「剧情要求选地点」的模式。
        /// 判据（run93 实机取证）：`MapPage._displayableAreas` 非空 —— 它由
        /// `MapPage.InitializeAreas(restrictions)` 用「允许去的地点」填，浏览地图时为空。
        /// 这是游戏自己的权威数据，比猜 UI 显隐（CanvasGroup alpha，已证明恒 0）可靠。
        /// </summary>
        private static bool _choiceMode;
        private static Transform _visibleFloor;
        private static Transform _root;

        internal static bool ChoiceMode
        {
            get { try { return _choiceMode; } catch { return false; } }
        }

        // 测试钩子（MANOSABA_A11Y_TEST_MAPCHOICE=1）：本机键盘注入被系统挡掉，
        // 所以用一条「选中第 1 个地点」的代码路径验收编号 → 按钮 → onClick 这段。
        private static bool _testDone;
        private static int _floorSwitchCount;
        private static float _floorTestAt;
        private static float _choiceDiagAt;
        private static bool _kindTestDone;
        /// <summary>游戏自己的允许名单（`MapPage._restrictionAreas` 的 MapAreaKind 名）。
        /// run112 实测：名单里的地点即使不在当前显示楼层，`onClick` 也会被游戏接受，
        /// 所以不用切楼层就能选到 2F/地下1F 的地点。</summary>
        private static HashSet<string> _allowedKinds;
        /// <summary>每页 9 项（数字键 1-9）。名单超过 9 个时用 PageUp/PageDown 在名单内翻页
        /// （用户 2026-10-31 选定：只翻名单，不动楼层，避免楼层切换把游戏的 await 挂断）。</summary>
        private const int PageSize = 9;
        private static int _page;
        /// <summary>测试钩子：直接按 AreaKind 点某个地点（不受「当前楼层」过滤影响）。</summary>
        internal static void TestPickKind(string kind)
        {
            if (_kindTestDone || _scope == null || string.IsNullOrEmpty(kind)) return;
            try
            {
                WitchTrials.Views.MapArea[] areas = _scope.GetComponentsInChildren<WitchTrials.Views.MapArea>(true);
                if (areas == null) return;
                for (int i = 0; i < areas.Length; i++)
                {
                    WitchTrials.Views.MapArea a = areas[i];
                    if (a == null || a._areaKind.ToString() != kind) continue;
                    _kindTestDone = true;
                    A11yHost.Diag("MAPCHOICE 测试：按 kind 点击 " + kind);
                    _choiceDiagAt = Time.unscaledTime + 2f;
                    Repeat.Say(SafeKind(a));
                    ClickArea(a);
                    ClickByInput(a);
                    return;
                }
            }
            catch (Exception e) { A11yHost.Diag("MapChoice.TestPickKind: " + e.Message); }
        }

        /// <summary>地图页当前是否真的有可点地点（供 ScreenAssist 决定 Esc 能不能关手册）。</summary>
        internal static bool HasChoice
        {
            get { try { return _entries.Count > 0; } catch { return false; } }
        }

        internal static int ChoiceCount
        {
            get { try { return _entries.Count; } catch { return 0; } }
        }

        /// <summary>每帧调用：只处理数字键（本作约 6 FPS，键必须逐帧采样）。</summary>
        internal static void Tick()
        {
            try
            {
                // 选完 1.5 秒后把游戏自己的 chosenAreaId / chosenAreaFlag 打出来：
                // 这是判断「我们的点击到底被游戏接受成了哪个地点」的唯一权威信号。
                if (_choiceDiagAt > 0f && Time.unscaledTime >= _choiceDiagAt)
                {
                    _choiceDiagAt = 0f;
                    DumpChosenVariables();
                }

                if (_entries.Count == 0) return;
                if (!HotkeysEnabled()) return;

                int pick = 0;
                int firstOnPage = _page * PageSize;
                for (int i = 1; i <= PageSize && firstOnPage + i <= _entries.Count; i++)
                {
                    // 走 KeyEdge 而不是裸 Input：本作只启用新 Input System（run31 取证），
                    // KeyEdge 在旧 API 抛异常时会退回 Win32 全局读取。
                    if (KeyEdge.Pressed(KeyCode.Alpha0 + i) || KeyEdge.Pressed(KeyCode.Keypad0 + i))
                    {
                        pick = i;
                        break;
                    }
                }

                // 测试钩子：MANOSABA_A11Y_TEST_MAPFLOOR=1 时，播报 2 秒后自动切一次楼层，
                // 用来验证「切换后判据变准」（本机键盘注入不可用，见 README 踩坑 #12）。
                if (Plugin.TestMapFloorCount > 0 && _floorSwitchCount < Plugin.TestMapFloorCount)
                {
                    if (_floorTestAt <= 0f && _announcedKey.Length > 0) { _floorTestAt = Time.unscaledTime + 2f; return; }
                    if (Time.unscaledTime >= _floorTestAt)
                    {
                        _floorSwitchCount++;
                        SwitchFloor(1);
                        _floorTestAt = Time.unscaledTime + 4f;   // 给游戏一帧多点时间刷新楼层
                    }
                    return;   // 切层没做完之前不要抢着选
                }

                if (pick == 0 && Plugin.TestMapChoiceOnce && !_testDone && _announcedKey.Length > 0)
                {
                    _testDone = true;
                    pick = Plugin.TestMapChoicePick;
                    if (pick < 1 || pick > _entries.Count) pick = 1;
                    A11yHost.Diag("MAPCHOICE 测试钩子：自动选择第 " + pick + " 个地点");
                }
                if (!string.IsNullOrEmpty(Plugin.TestMapPickKind) && !_kindTestDone)
                {
                    TestPickKind(Plugin.TestMapPickKind);
                    return;
                }

                if (pick > 0) { Activate(firstOnPage + pick); return; }

                // 楼层切换：地图上有多个楼层时，允许名单可能跨层（Adv25 的 8 个里
                // 6 个在 1F、娱乐室在 2F、牢房在地下1F），必须能翻楼层才选得到。
                if (_entries.Count > PageSize)
                {
                    if (KeyEdge.Pressed(KeyCode.PageDown)) { TurnPage(1); return; }
                    if (KeyEdge.Pressed(KeyCode.PageUp)) { TurnPage(-1); return; }
                }
                else if (!_choiceMode)
                {
                    if (KeyEdge.Pressed(KeyCode.PageDown)) { SwitchFloor(1); return; }
                    if (KeyEdge.Pressed(KeyCode.PageUp)) { SwitchFloor(-1); return; }
                }
            }
            catch (Exception e) { A11yHost.Diag("MapChoice.Tick: " + e.Message); }
        }

        /// <summary>地图页可见时每 0.5 秒调用一次：重扫可点地点并播报。</summary>
        internal static void Observe(Transform scope, Transform root)
        {
            if (Time.unscaledTime < _nextScanAt) return;
            _nextScanAt = Time.unscaledTime + ScanInterval;
            _scope = scope;
            _root = root;
            try
            {
                Refresh(scope);
                Announce();
            }
            catch (Exception e) { A11yHost.Diag("MapChoice.Observe: " + e.Message); }
        }

        /// <summary>离开地图页 / 手册关闭时调用。</summary>
        internal static void Clear()
        {
            _entries.Clear();
            _announcedKey = "";
            _page = 0;
            _scope = null;
            _root = null;
            _visibleFloor = null;
            _testDone = false;
            _floorSwitchCount = 0;
            _kindTestDone = false;
            _floorTestAt = 0f;
            // 注意：不要在这里清 _choiceDiagAt —— 选完地点后手册会关闭、
            // ScreenAssist 会调 Clear()，清掉就永远打不出 chosenAreaId 诊断（run108 实测）。
        }

        // ------------------------------------------------------------------ 扫描
        private static void Refresh(Transform scope)
        {
            _entries.Clear();
            _choiceMode = false;
            if (scope == null) return;

            WitchTrials.Views.MapArea[] areas = scope.GetComponentsInChildren<WitchTrials.Views.MapArea>(true);
            if (areas == null) return;

            // ---- 游戏自己的「本次允许去的地点」 ----
            // MapPage.InitializeAreas(restrictions) 会把结果填进 `_displayableAreas`，
            // 只有剧情要求选地点时才会先被调用（浏览态我们观测到长度为 0 / 字段为空）。
            WitchTrials.Views.MapPage page = null;
            HashSet<int> displayable = null;
            string pageInfo = "page=null";
            _visibleFloor = null;
            try { page = scope.GetComponentInChildren<WitchTrials.Views.MapPage>(true); }
            catch (Exception e) { if (DiagOn()) A11yHost.Diag("MAPCHOICE 取 MapPage 失败: " + e.Message); }

            if (page != null)
            {
                int dispLen = -1;
                string restr = "?";
                string mode = "?";
                try
                {
                    var arr = page._displayableAreas;      // Il2CppReferenceArray<MapArea>
                    if (arr != null)
                    {
                        dispLen = arr.Length;
                        if (arr.Length > 0)
                        {
                            displayable = new HashSet<int>();
                            for (int i = 0; i < arr.Length; i++)
                            {
                                WitchTrials.Views.MapArea a = arr[i];
                                if (a != null) displayable.Add(a.GetInstanceID());
                            }
                            _choiceMode = displayable.Count > 0;
                        }
                    }
                }
                catch (Exception e) { if (DiagOn()) A11yHost.Diag("MAPCHOICE 读 _displayableAreas 失败: " + e.Message); }
                try
                {
                    _allowedKinds = null;
                    var restrCol = page._restrictionAreas;
                    if (restrCol == null) restr = "null";
                    else
                    {
                        // interop 的 IReadOnlyCollection<T> 没有 Count，但底层通常是 List<T>：
                        // TryCast 成 List 就能拿到 Count 和索引器（同 PrintedMessage 的坑）。
                        var lst = restrCol.TryCast<Il2CppSystem.Collections.Generic.List<WitchTrials.Models.MapAreaKind>>();
                        if (lst != null && lst.Count > 0)
                        {
                            _allowedKinds = new HashSet<string>(StringComparer.Ordinal);
                            for (int k = 0; k < lst.Count; k++) _allowedKinds.Add(lst[k].ToString());
                            restr = "名单(" + lst.Count + ")";
                            // 只念名单里、确实是「有地图数据」的地点（避免念到蓝图之类的占位）
                            if (displayable != null)
                            {
                                HashSet<string> keep = new HashSet<string>(StringComparer.Ordinal);
                                foreach (string k in _allowedKinds) keep.Add(k);
                                _allowedKinds = keep;
                            }
                        }
                        else restr = "非 null（读不到条目）";
                    }
                }
                catch (Exception e) { restr = "err:" + e.Message; _allowedKinds = null; }
                try
                {
                    WitchTrials.Views.WitchBookUi ui = scope.GetComponentInChildren<WitchTrials.Views.WitchBookUi>(true);
                    if (ui != null && ui._witchBookScreen != null)
                        mode = ui._witchBookScreen.PresentationMode.ToString();
                }
                catch (Exception e) { mode = "err:" + e.Message; }
                pageInfo = "page=ok dispLen=" + dispLen + " restr=" + restr + " mode=" + mode
                           + " choiceMode=" + _choiceMode;
            }

            // 找「当前显示的那一层」：MapFloor 用 Canvas 显隐（实机 run95 dump 见下方诊断行）
            StringBuilder floorDiag = DiagOn() ? new StringBuilder("MAPCHOICE 楼层:") : null;
            try
            {
                WitchTrials.Views.MapFloor[] floors = scope.GetComponentsInChildren<WitchTrials.Views.MapFloor>(true);
                if (floors != null)
                {
                    // 必须先只看 Canvas.enabled：`UiVis.Visible` 对全部楼层都返回 true，
                    // 拿它当判据会永远选中第一层（run98 实测 F2_1 canvas=True 但仍取到 F1_1）。
                    for (int i = 0; i < floors.Length; i++)
                    {
                        WitchTrials.Views.MapFloor f = floors[i];
                        if (f == null) continue;
                        bool canvasOn = false;
                        try { canvasOn = f._canvas != null && f._canvas.enabled; } catch { }
                        bool vis = false;
                        try { vis = UiVis.Visible(f); } catch { }
                        if (floorDiag != null)
                            floorDiag.Append(" ").Append(f.name).Append("(canvas=").Append(canvasOn)
                                     .Append(" vis=").Append(vis)
                                     .Append(" active=").Append(f.gameObject.activeInHierarchy).Append(")");
                        if (_visibleFloor == null && canvasOn) _visibleFloor = f.transform;
                    }
                    if (_visibleFloor == null)
                    {
                        // 兜底：没有楼层 Canvas 信息时才退回 UiVis（旧版本行为）
                        for (int i = 0; i < floors.Length; i++)
                        {
                            WitchTrials.Views.MapFloor f = floors[i];
                            if (f == null) continue;
                            bool vis = false;
                            try { vis = UiVis.Visible(f); } catch { }
                            if (_visibleFloor == null && vis) _visibleFloor = f.transform;
                        }
                    }
                }
            }
            catch (Exception e) { if (floorDiag != null) floorDiag.Append(" 取 MapFloor 失败:").Append(e.Message); }
            if (floorDiag != null) A11yHost.Diag(floorDiag.ToString());

            StringBuilder diag = DiagOn() ? new StringBuilder("MAPCHOICE 扫描 areas=" + areas.Length + "  " + pageInfo
                                                             + "  可见楼层=" + (_visibleFloor == null ? "?" : _visibleFloor.name)) : null;

            for (int i = 0; i < areas.Length; i++)
            {
                WitchTrials.Views.MapArea a = areas[i];
                if (a == null) continue;

                bool active;
                try { active = a.gameObject.activeInHierarchy; } catch { active = false; }

                Button b = null;
                bool interactable = false;
                bool btnActive = false;
                float alpha = 1f;
                float labelAlpha = -1f;
                try
                {
                    WitchTrials.Views.MapAreaFocusButton fb = a._focusButton;
                    if (fb != null)
                    {
                        b = fb;
                        interactable = fb.interactable;
                        btnActive = fb.gameObject.activeInHierarchy;
                        if (fb._canvasGroup != null) alpha = fb._canvasGroup.alpha;
                    }
                }
                catch { }
                try
                {
                    WitchTrials.Views.MapAreaLabel label = a._areaLabel;
                    if (label != null && label._canvasGroup != null) labelAlpha = label._canvasGroup.alpha;
                }
                catch { }

                string name = NameOf(a);
                bool listed = displayable == null || displayable.Contains(a.GetInstanceID());
                // 真正的「这次能去」= 游戏把该地点的名牌显示出来了（MapArea.Hide() 会把
                // MapAreaLabel 的 CanvasGroup.alpha 归零），且它属于当前正在显示的楼层。
                // 实机证据（run95，@choiceMapArea 只允许 8 个地点）：
                //   1F 上 会客厅/玄关大厅/食堂/医务室/淋浴房/往湖方向 的 labelAlpha=1.00，
                //   厨房/审判庭入口过道 等未允许的是 0.00；其它楼层的地点是别的容器，另算。
                // 判据（run112 实机标定，最终版）：
                //   有 `_restrictionAreas` 名单时直接用名单 —— 名单里的地点即使所在楼层没显示，
                //   点它游戏也会接受并跳对应分支（run112 实测 RecreationRoom → Adv32「来到了娱乐室」）。
                //   读不到名单时退回旧判据（当前楼层 + 名牌可见，run96 标定）。
                // 旧判据（run96/run97）说明：名牌可见 = 这一层允许去。
                //   · 当前楼层：允许的地点 labelAlpha=1.00，不允许的=0.00（run96 实测 1F 六个，
                //     与截图里看得见的名牌完全一致）
                //   · 未显示的楼层：MapFloor.Show(restriction) 还没跑，名牌都还是 1，
                //     不能当判据（run97 因此把 2F 的「图书室」也念成了可前往，选了无效）
                // 所以只看当前楼层；要别的楼层就切楼层（PageUp/PageDown），切换后游戏会
                // 重新按允许名单 Show，判据随之变准。
                bool onFloor = _visibleFloor != null && IsUnder(a.transform, _visibleFloor);
                bool listedAndShown;
                if (_allowedKinds != null && _allowedKinds.Count > 0)
                    listedAndShown = _allowedKinds.Contains(SafeKind(a));
                else
                    listedAndShown = onFloor && labelAlpha > 0.5f;
                bool live = _choiceMode && active && b != null && interactable && listed && listedAndShown
                            && !string.IsNullOrEmpty(name);

                if (diag != null)
                {
                    diag.Append("\n  [").Append(i).Append("] ").Append(a._areaKind)
                        .Append(" 名=").Append(string.IsNullOrEmpty(name) ? "(空)" : name)
                        .Append(" 激活=").Append(active)
                        .Append(" 有按钮=").Append(b != null)
                        .Append(" 按钮激活=").Append(btnActive)
                        .Append(" 可点=").Append(interactable)
                        .Append(" btnAlpha=").Append(alpha.ToString("0.00"))
                        .Append(" labelAlpha=").Append(labelAlpha.ToString("0.00"))
                        .Append(" 在名单=").Append(listed)
                        .Append(live ? "  =>可前往" : "");
                }

                if (!live) continue;
                Entry e = new Entry();
                e.Area = a;
                e.Name = name;
                e.Floor = FloorLabelOf(a.transform);
                e.Button = b;
                _entries.Add(e);
            }

            if (diag != null)
            {
                diag.Append("\n  => 可前往 ").Append(_entries.Count).Append(" 个地点");
                A11yHost.Diag(diag.ToString());
            }
        }

        /// <summary>地点所属楼层的短名：来自祖先 MapFloor 的名字（实机路径 …/Page_Maps/F1_1/Lounge）。</summary>
        private static string FloorLabelOf(Transform t)
        {
            int guard = 0;
            while (t != null && guard++ < 16)
            {
                WitchTrials.Views.MapFloor f = null;
                try { f = t.GetComponent<WitchTrials.Views.MapFloor>(); } catch { }
                if (f != null) return UiNav.FloorNameOf(f.name);
                t = t.parent;
            }
            return "";
        }

        /// <summary>t 是否在 root 的子树里。</summary>
        private static bool IsUnder(Transform t, Transform root)
        {
            if (t == null || root == null) return false;
            int guard = 0;
            while (t != null && guard++ < 64)
            {
                if (t == root) return true;
                t = t.parent;
            }
            return false;
        }

        /// <summary>地点中文名：优先取标签文字（游戏自己本地化的），退回 MapAreaKind 别名表。</summary>
        private static string NameOf(WitchTrials.Views.MapArea a)
        {
            try
            {
                WitchTrials.Views.MapAreaLabel label = a._areaLabel;
                if (label != null && label._label != null)
                {
                    string s = TextProc.Clean(label._label.text);
                    if (!string.IsNullOrEmpty(s) && s.Length >= 2) return s;
                }
            }
            catch { }
            try { return UiNav.AreaNameOf(a._areaKind.ToString()); } catch { return ""; }
        }

        // ------------------------------------------------------------------ 楼层切换
        /// <summary>手册底部的胶片条：每张「地图」一张卡（1F / 2F / 地下1F …）。</summary>
        private static List<WitchTrials.Views.WitchBookItemButton> FloorItems()
        {
            List<WitchTrials.Views.WitchBookItemButton> list = new List<WitchTrials.Views.WitchBookItemButton>();
            if (_root == null) return list;
            try
            {
                WitchTrials.Views.WitchBookItemButton[] all =
                    _root.GetComponentsInChildren<WitchTrials.Views.WitchBookItemButton>(true);
                if (all == null) return list;
                for (int i = 0; i < all.Length; i++)
                {
                    WitchTrials.Views.WitchBookItemButton it = all[i];
                    if (it == null || it._button == null) continue;
                    if (!it.gameObject.activeInHierarchy) continue;
                    if (!UiVis.Visible(it)) continue;
                    list.Add(it);
                }
                // 胶片条按屏幕位置从左到右，用 x 排序（组件的 sibling 顺序不保证）
                list.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            }
            catch (Exception e) { A11yHost.Diag("MapChoice.FloorItems: " + e.Message); }
            return list;
        }

        private static string ItemLabel(WitchTrials.Views.WitchBookItemButton it)
        {
            try
            {
                if (it._label == null) return "";
                // 楼层卡的文字是「地下<br>1F」这种带换行/标签的两行，
                // 直接交给 TextProc.Clean 会把 <br> 变成「，」→「地下，1F」（run106 实测）。
                // 楼层名要的是连读，所以先把标签和换行全部吃掉再做普通清洗。
                string s = it._label.text ?? "";
                s = System.Text.RegularExpressions.Regex.Replace(s, "<[^<>]{1,120}>", "");
                s = s.Replace("\r", "").Replace("\n", "");
                return TextProc.Clean(s);
            }
            catch { }
            return "";
        }

        /// <summary>当前楼层之外是否还有楼层卡（有就提示玩家可以翻页）。</summary>
        private static bool HasOtherFloors()
        {
            try
            {
                List<WitchTrials.Views.WitchBookItemButton> items = FloorItems();
                int withText = 0;
                for (int i = 0; i < items.Count; i++) if (!string.IsNullOrEmpty(ItemLabel(items[i]))) withText++;
                return withText > 1;
            }
            catch { return false; }
        }

        private static void SwitchFloor(int dir)
        {
            try
            {
                List<WitchTrials.Views.WitchBookItemButton> items = FloorItems();
                if (items.Count < 2) { Repeat.Say("这里没有其它楼层。"); return; }
                string cur = _visibleFloor == null ? "" : UiNav.FloorNameOf(_visibleFloor.name);
                int idx = -1;
                for (int i = 0; i < items.Count; i++)
                {
                    string l = ItemLabel(items[i]);
                    if (string.IsNullOrEmpty(l)) continue;
                    if (!string.IsNullOrEmpty(cur) && l.Replace(" ", "").Contains(cur)) { idx = i; break; }
                }
                int next = idx < 0 ? 0 : idx + dir;
                if (next < 0) next = 0;
                if (next >= items.Count) next = items.Count - 1;
                WitchTrials.Views.WitchBookItemButton target = items[next];
                string label = ItemLabel(target);
                if (string.IsNullOrEmpty(label)) { Repeat.Say("这里没有其它楼层。"); return; }
                A11yHost.Diag("MAPCHOICE 切换楼层 " + dir + " -> " + label + "（from " + (idx < 0 ? "?" : idx.ToString()) + "）");
                try { target._button.onClick.Invoke(); }
                catch (Exception e) { A11yHost.Diag("MapChoice.SwitchFloor: " + e.Message); Repeat.Say("切换楼层失败。"); return; }
                _announcedKey = "";       // 让下一次扫描重新播报新楼层
                Repeat.Say("切换到 " + label + "。");
            }
            catch (Exception e) { A11yHost.Diag("MapChoice.SwitchFloor: " + e.Message); }
        }

        /// <summary>在名单内翻页（只在名单超过一页时可用）。</summary>
        private static void TurnPage(int dir)
        {
            int pages = (_entries.Count + PageSize - 1) / PageSize;
            if (pages <= 1) return;
            _page = (_page + dir + pages) % pages;
            _announcedKey = "";          // 强制重新播报新一页
            A11yHost.Diag("MAPCHOICE 翻页 -> 第 " + (_page + 1) + " 页 / 共 " + pages + " 页");
        }

        // ------------------------------------------------------------------ 播报
        private static void Announce()
        {
            if (_entries.Count == 0) { _announcedKey = ""; return; }

            StringBuilder key = new StringBuilder();
            for (int i = 0; i < _entries.Count; i++)
                key.Append(_entries[i].Floor).Append('/').Append(_entries[i].Name).Append('|');
            string k = key.ToString();
            if (k == _announcedKey) return;
            _announcedKey = k;

            // 按楼层分组念：地图上地点会跨层，混在一起念玩家不知道要去哪一层。
            StringBuilder sb = new StringBuilder();
            string curFloor = _visibleFloor == null ? "" : UiNav.FloorNameOf(_visibleFloor.name);
            // 只有确实跨楼层分组时才念「在XF：」（同层时那句是废话）
            bool multiFloor = false;
            for (int i = 1; i < _entries.Count; i++) if (_entries[i].Floor != _entries[i - 1].Floor) { multiFloor = true; break; }
            sb.Append("地图上可前往 ").Append(_entries.Count).Append(" 个地点");
            // 跨层时不再提前念「（1F）」——后面每个分组已经带了楼层，重复念是噪音。
            if (!multiFloor && !string.IsNullOrEmpty(curFloor)) sb.Append("（").Append(curFloor).Append("）");
            sb.Append("：");
            string lastFloor = null;
            int pages = (_entries.Count + PageSize - 1) / PageSize;
            if (_page >= pages) _page = 0;
            int from = _page * PageSize;
            if (pages > 1) sb.Append("第").Append(_page + 1).Append("页，共").Append(pages).Append("页。");
            for (int i = from; i < _entries.Count && i < from + PageSize; i++)
            {
                Entry e = _entries[i];
                if (multiFloor && e.Floor != lastFloor)
                {
                    if (!string.IsNullOrEmpty(e.Floor)) sb.Append("在").Append(e.Floor).Append("：");
                    lastFloor = e.Floor;
                }
                sb.Append(i + 1).Append("、").Append(e.Name).Append("。");
            }
            bool hasRestriction = _allowedKinds != null && _allowedKinds.Count > 0;
            if (pages > 1) sb.Append("PageUp 或 PageDown 翻页。");
            else if (!hasRestriction && HasOtherFloors()) sb.Append("其它楼层按 PageUp 或 PageDown 切换。");
            sb.Append("按数字键选择。");
            string line = sb.ToString();

            A11yHost.Diag("MAPCHOICE 播报 " + _entries.Count + " 个地点 => " + line);
            Repeat.Say(line, true);
            Reader.Remember(line);      // 选项类内容都进剧情重读缓冲（Backspace 可重听）
        }

        // ------------------------------------------------------------------ 选择
        private static void Activate(int pick)
        {
            if (pick < 1 || pick > _entries.Count) return;
            Entry e = _entries[pick - 1];
            if (e == null || e.Button == null || e.Button.gameObject == null)
            {
                Repeat.Say("这个地点已经失效。");
                return;
            }
            A11yHost.Diag("MAPCHOICE 数字键 " + pick + " -> " + e.Name + "（kind="
                          + (e.Area == null ? "?" : SafeKind(e.Area)) + "）");
            _choiceDiagAt = Time.unscaledTime + 1.5f;
            try
            {
                Repeat.Say(e.Name);          // 选前复述，确认选的是哪个地点
                bool ok = e.Area != null ? ClickArea(e.Area) : false;
                if (!ok) e.Button.onClick.Invoke();
                A11yHost.Diag("MAPCHOICE 点击派发 ok=" + ok + " kind="
                              + (e.Area == null ? "?" : SafeKind(e.Area)));
                // 合成派发之后补一次「真实输入」点击：run117/run118 证明光靠合成派发，
                // 游戏的 OnChoiceMapAreaAsync 不会返回（地图页一直不关）。
                if (e.Area != null) ClickByInput(e.Area);
            }
            catch (Exception ex)
            {
                A11yHost.Diag("MapChoice.Activate: " + ex.Message);
                Repeat.Say("选择失败。");
            }
        }

        /// <summary>把地点按钮换算成屏幕坐标，通过 InputSystem 事件队列做一次真实点击。</summary>
        private static bool ClickByInput(WitchTrials.Views.MapArea a)
        {
            if (a == null) return false;
            try
            {
                WitchTrials.Views.MapAreaFocusButton fb = a._focusButton;
                if (fb == null) return false;
                UnityEngine.RectTransform rt = fb.transform.TryCast<UnityEngine.RectTransform>();
                if (rt == null) { A11yHost.Diag("MAPCHOICE 注入点击：拿不到 RectTransform"); return false; }
                UnityEngine.Canvas canvas = fb.GetComponentInParent<UnityEngine.Canvas>();
                UnityEngine.Camera cam = null;
                if (canvas != null && canvas.renderMode != UnityEngine.RenderMode.ScreenSpaceOverlay)
                    cam = canvas.worldCamera;
                UnityEngine.Vector2 sp = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, rt.position);
                InputInject.Click(sp.x, sp.y, 0.16f);
                A11yHost.Diag("MAPCHOICE 注入点击 kind=" + SafeKind(a) + " screen="
                              + sp.x.ToString("0") + "," + sp.y.ToString("0")
                              + " canvas=" + (canvas == null ? "?" : canvas.renderMode.ToString()));
                return true;
            }
            catch (Exception e) { A11yHost.Diag("MapChoice.ClickByInput: " + e.Message); return false; }
        }

        /// <summary>
        /// 点一个地图地点。优先派发 EventSystem 的指针点击（走游戏的完整点击链），
        /// 没有再退回 Button.onClick —— 两者都试，实测哪个管用看诊断日志。
        /// </summary>
        private static bool ClickArea(WitchTrials.Views.MapArea a)
        {
            if (a == null) return false;
            WitchTrials.Views.MapAreaFocusButton fb = null;
            try { fb = a._focusButton; } catch { }
            UnityEngine.EventSystems.EventSystem es = null;
            try { es = UnityEngine.EventSystems.EventSystem.current; } catch { }
            bool dispatched = false;
            if (fb != null && fb.gameObject != null)
            {
                try
                {
                    if (es != null)
                    {
                        var ped = new UnityEngine.EventSystems.PointerEventData(es);
                        ped.button = UnityEngine.EventSystems.PointerEventData.InputButton.Left;
                        ped.clickCount = 1;
                        UnityEngine.EventSystems.ExecuteEvents.Execute(
                            fb.gameObject, ped,
                            UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                        dispatched = true;
                    }
                }
                catch (Exception e) { A11yHost.Diag("MapChoice.ClickArea 指针事件失败: " + e.Message); }
            }
            // onClick 再补一次：游戏的子类可能只在 onClick 上挂着业务
            try { if (fb != null) { fb.onClick.Invoke(); dispatched = true; } }
            catch (Exception e) { A11yHost.Diag("MapChoice.ClickArea onClick 失败: " + e.Message); }
            return dispatched;
        }

        // ------------------------------------------------------------------ 诊断
        private static string SafeKind(WitchTrials.Views.MapArea a)
        {
            try { return a._areaKind.ToString(); } catch { return "?"; }
        }

        /// <summary>按变量自己的类型取值（字符串变量读 .Number 会抛 Naninovel.Error）。</summary>
        private static string VarText(Naninovel.CustomVariableValue v)
        {
            string type = "-";
            try { type = v.Type.ToString(); } catch { }
            try
            {
                if (type == "String") return type + ":" + v.String;
                if (type == "Number") return type + ":" + v.Number.ToString("0.###");
                if (type == "Boolean") return type + ":" + v.Boolean;
            }
            catch (Exception e) { return type + ":err:" + e.Message; }
            return type;
        }

        private static void DumpChosenVariables()
        {
            try
            {
                Naninovel.ICustomVariableManager vars =
                    Naninovel.Engine.GetService<Naninovel.ICustomVariableManager>();
                if (vars == null) { A11yHost.Diag("MAPCHOICE 变量: 没有 ICustomVariableManager"); return; }
                string id = "-";
                string flag = "-";
                try { if (vars.VariableExists("chosenAreaId")) id = VarText(vars.GetVariableValue("chosenAreaId")); }
                catch (Exception e) { id = "err:" + e.Message; }
                try { if (vars.VariableExists("chosenAreaFlag")) flag = VarText(vars.GetVariableValue("chosenAreaFlag")); }
                catch (Exception e) { flag = "err:" + e.Message; }
                A11yHost.Diag("MAPCHOICE 变量: chosenAreaId=[" + id + "] chosenAreaFlag=[" + flag + "]");
            }
            catch (Exception e) { A11yHost.Diag("MAPCHOICE 变量: " + e.Message); }
        }

        // ------------------------------------------------------------------ 测试钩子
        /// <summary>
        /// 仅 MANOSABA_A11Y_TEST_MAPCHOICE=1 时由 Plugin 调用一次：
        /// 用游戏自己的 `ChangeFocusButtonActivity(true)` 把前 N 个地点置为可点，
        /// 复现「剧情要求选地点」的状态，用来验收扫描/播报/数字键链路。
        /// 这不是玩家路径（正式版无此环境变量，永不会执行）。
        /// </summary>
        internal static void SimulateChoice(int count)
        {
            if (_scope == null) { A11yHost.Diag("MAPCHOICE 模拟：还没有地图页"); return; }
            WitchTrials.Views.MapArea[] areas = _scope.GetComponentsInChildren<WitchTrials.Views.MapArea>(true);
            if (areas == null) { A11yHost.Diag("MAPCHOICE 模拟：没找到 MapArea"); return; }
            int n = 0;
            for (int i = 0; i < areas.Length && n < count; i++)
            {
                WitchTrials.Views.MapArea a = areas[i];
                if (a == null || !a.gameObject.activeInHierarchy) continue;
                try
                {
                    a.ChangeFocusButtonActivity(true);
                    if (a._areaLabel != null) a._areaLabel.Show();
                    n++;
                    A11yHost.Diag("MAPCHOICE 模拟：置为可点 " + a._areaKind + " -> " + NameOf(a));
                }
                catch (Exception e) { A11yHost.Diag("MAPCHOICE 模拟失败: " + e.Message); }
            }
            A11yHost.Diag("MAPCHOICE 模拟：共置 " + n + " 个地点");
        }

        // ------------------------------------------------------------------ 配置
        private static bool HotkeysEnabled()
        {
            try { return Plugin.CfgChoiceHotkeys == null || Plugin.CfgChoiceHotkeys.Value; }
            catch { return true; }
        }

        private static bool DiagOn()
        {
            try { return Plugin.CfgMapDiag; } catch { return false; }
        }
    }
}
