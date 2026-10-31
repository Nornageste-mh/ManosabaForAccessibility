import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:70])
    s = s.replace(old, new, 1)


# 1) 允许名单字段
rep("""        private static bool _kindTestDone;""",
    """        private static bool _kindTestDone;
        /// <summary>游戏自己的允许名单（`MapPage._restrictionAreas` 的 MapAreaKind 名）。
        /// run112 实测：名单里的地点即使不在当前显示楼层，`onClick` 也会被游戏接受，
        /// 所以不用切楼层就能选到 2F/地下1F 的地点。</summary>
        private static HashSet<string> _allowedKinds;""")

# 2) Refresh 里读名单
rep("""                try { restr = page._restrictionAreas == null ? "null" : "非null"; } catch (Exception e) { restr = "err:" + e.Message; }""",
    """                try
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
                catch (Exception e) { restr = "err:" + e.Message; _allowedKinds = null; }""")

# 3) live 判据：有名单就用名单（跨楼层也能选）；没有名单才退回「当前楼层 + 名牌可见」
rep("""                // 判据（run96/run97 实机标定）：""",
    """                // 判据（run112 实机标定，最终版）：
                //   有 `_restrictionAreas` 名单时直接用名单 —— 名单里的地点即使所在楼层没显示，
                //   点它游戏也会接受并跳对应分支（run112 实测 RecreationRoom → Adv32「来到了娱乐室」）。
                //   读不到名单时退回旧判据（当前楼层 + 名牌可见，run96 标定）。
                // 旧判据（run96/run97）说明：""")

rep("""                bool onFloor = _visibleFloor != null && IsUnder(a.transform, _visibleFloor);
                bool live = _choiceMode && active && b != null && interactable && listed && onFloor
                            && labelAlpha > 0.5f && !string.IsNullOrEmpty(name);""",
    """                bool onFloor = _visibleFloor != null && IsUnder(a.transform, _visibleFloor);
                bool listedAndShown;
                if (_allowedKinds != null && _allowedKinds.Count > 0)
                    listedAndShown = _allowedKinds.Contains(SafeKind(a));
                else
                    listedAndShown = onFloor && labelAlpha > 0.5f;
                bool live = _choiceMode && active && b != null && interactable && listed && listedAndShown
                            && !string.IsNullOrEmpty(name);""")

# 4) 播报提示：有名单时不再提示切楼层
rep("""            if (HasOtherFloors()) sb.Append("其它楼层按 PageUp 或 PageDown 切换。");""",
    """            bool hasRestriction = _allowedKinds != null && _allowedKinds.Count > 0;
            if (!hasRestriction && HasOtherFloors()) sb.Append("其它楼层按 PageUp 或 PageDown 切换。");""")

# 5) 选地点期间禁止切楼层：实机会把 await 挂断（run105/106 软锁在加载界面），
#    浏览态（不是选择状态）才允许翻楼层。
rep("""                if (KeyEdge.Pressed(KeyCode.PageDown)) { SwitchFloor(1); return; }
                if (KeyEdge.Pressed(KeyCode.PageUp)) { SwitchFloor(-1); return; }""",
    """                if (!_choiceMode)
                {
                    if (KeyEdge.Pressed(KeyCode.PageDown)) { SwitchFloor(1); return; }
                    if (KeyEdge.Pressed(KeyCode.PageUp)) { SwitchFloor(-1); return; }
                }""")

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('patched restriction-list based choice')
