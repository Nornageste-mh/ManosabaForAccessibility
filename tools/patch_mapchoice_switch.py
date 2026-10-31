import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:70])
    s = s.replace(old, new)


# 1) 记录手册根
rep("""        private static Transform _visibleFloor;""",
    """        private static Transform _visibleFloor;
        private static Transform _root;""")

# 2) Observe 增加 root 参数
rep("""        internal static void Observe(Transform scope)
        {
            if (Time.unscaledTime < _nextScanAt) return;
            _nextScanAt = Time.unscaledTime + ScanInterval;
            _scope = scope;""",
    """        internal static void Observe(Transform scope, Transform root)
        {
            if (Time.unscaledTime < _nextScanAt) return;
            _nextScanAt = Time.unscaledTime + ScanInterval;
            _scope = scope;
            _root = root;""")

# 3) live：只认「当前显示楼层」上游戏显示了名牌的地点
rep("""                // 注意：不能再按「当前可见楼层」过滤 —— `@choiceMapArea` 的名单会跨楼层
                // （Adv25 的 8 个里 6 个在 1F、娱乐室在 2F、牢房在地下1F），只列当前层会让
                // 玩家误以为没得选。游戏给「允许去的地点」显示名牌，按楼层分组念出来即可。
                bool live = _choiceMode && active && b != null && interactable && listed
                            && labelAlpha > 0.5f && !string.IsNullOrEmpty(name);""",
    """                // 判据（run96/run97 实机标定）：名牌可见 = 这一层允许去。
                //   · 当前楼层：允许的地点 labelAlpha=1.00，不允许的=0.00（run96 实测 1F 六个，
                //     与截图里看得见的名牌完全一致）
                //   · 未显示的楼层：MapFloor.Show(restriction) 还没跑，名牌都还是 1，
                //     不能当判据（run97 因此把 2F 的「图书室」也念成了可前往，选了无效）
                // 所以只看当前楼层；要别的楼层就切楼层（PageUp/PageDown），切换后游戏会
                // 重新按允许名单 Show，判据随之变准。
                bool onFloor = _visibleFloor != null && IsUnder(a.transform, _visibleFloor);
                bool live = _choiceMode && active && b != null && interactable && listed && onFloor
                            && labelAlpha > 0.5f && !string.IsNullOrEmpty(name);""")

# 4) 播报：当前楼层 + 提示切楼层
rep("""            StringBuilder sb = new StringBuilder();
            sb.Append("地图上可前往 ").Append(_entries.Count).Append(" 个地点。");""",
    """            StringBuilder sb = new StringBuilder();
            string curFloor = _visibleFloor == null ? "" : UiNav.FloorNameOf(_visibleFloor.name);
            sb.Append("地图上可前往 ").Append(_entries.Count).Append(" 个地点。");
            if (!string.IsNullOrEmpty(curFloor)) sb.Remove(sb.Length - 1, 1).Append("（").Append(curFloor).Append("）：");""")

rep("""            sb.Append("按数字键选择。");
            string line = sb.ToString();""",
    """            if (HasOtherFloors()) sb.Append("其它楼层按 PageUp 或 PageDown 切换。");
            sb.Append("按数字键选择。");
            string line = sb.ToString();""")

# 5) Tick：楼层切换键
rep("""                if (pick == 0) return;
                Activate(pick);""",
    """                if (pick > 0) { Activate(pick); return; }

                // 楼层切换：地图上有多个楼层时，允许名单可能跨层（Adv25 的 8 个里
                // 6 个在 1F、娱乐室在 2F、牢房在地下1F），必须能翻楼层才选得到。
                if (KeyEdge.Pressed(KeyCode.PageDown)) { SwitchFloor(1); return; }
                if (KeyEdge.Pressed(KeyCode.PageUp)) { SwitchFloor(-1); return; }""")

# 6) 新增 SwitchFloor / HasOtherFloors / 楼层按钮收集
rep("""        // ------------------------------------------------------------------ 播报""",
    """        // ------------------------------------------------------------------ 楼层切换
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
                if (it._label != null) return TextProc.Clean(it._label.text);
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

        // ------------------------------------------------------------------ 播报""")

# 7) Clear 里清 root
rep("""            _scope = null;
            _testDone = false;""",
    """            _scope = null;
            _root = null;
            _visibleFloor = null;
            _testDone = false;""")

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('patched floor switching')
