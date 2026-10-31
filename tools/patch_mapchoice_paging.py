import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:100])
    s = s.replace(old, new, 1)


# 0) 每页项数常量 + 页码字段
rep('''        private static HashSet<string> _allowedKinds;''',
    '''        private static HashSet<string> _allowedKinds;
        /// <summary>每页 9 项（数字键 1-9）。名单超过 9 个时用 PageUp/PageDown 在名单内翻页
        /// （用户 2026-10-31 选定：只翻名单，不动楼层，避免楼层切换把游戏的 await 挂断）。</summary>
        private const int PageSize = 9;
        private static int _page;''')

# 1) 数字键 → 全局序号（跨页）
rep('''                int pick = 0;
                for (int i = 1; i <= 9 && i <= _entries.Count; i++)''',
    '''                int pick = 0;
                int firstOnPage = _page * PageSize;
                for (int i = 1; i <= PageSize && firstOnPage + i <= _entries.Count; i++)''')

# pick 之后要换算成全局序号
rep('''                if (pick > 0)
                {
                    Activate(pick);''',
    '''                if (pick > 0)
                {
                    Activate(firstOnPage + pick);''')

# 2) PageUp/PageDown：名单超过一页时翻名单；否则（浏览态）翻楼层
rep('''                if (!_choiceMode)
                {
                    if (KeyEdge.Pressed(KeyCode.PageDown)) { SwitchFloor(1); return; }
                    if (KeyEdge.Pressed(KeyCode.PageUp)) { SwitchFloor(-1); return; }
                }''',
    '''                if (_entries.Count > PageSize)
                {
                    if (KeyEdge.Pressed(KeyCode.PageDown)) { TurnPage(1); return; }
                    if (KeyEdge.Pressed(KeyCode.PageUp)) { TurnPage(-1); return; }
                }
                else if (!_choiceMode)
                {
                    if (KeyEdge.Pressed(KeyCode.PageDown)) { SwitchFloor(1); return; }
                    if (KeyEdge.Pressed(KeyCode.PageUp)) { SwitchFloor(-1); return; }
                }''')

# 3) 播报：按页输出 + 页码提示
rep('''            for (int i = 0; i < _entries.Count && i < 9; i++)
            {
                Entry e = _entries[i];''',
    '''            int pages = (_entries.Count + PageSize - 1) / PageSize;
            if (_page >= pages) _page = 0;
            int from = _page * PageSize;
            if (pages > 1) sb.Append("第").Append(_page + 1).Append("页，共").Append(pages).Append("页。");
            for (int i = from; i < _entries.Count && i < from + PageSize; i++)
            {
                Entry e = _entries[i];''')

# 4) 翻页提示
rep('''            if (!hasRestriction && HasOtherFloors()) sb.Append("其它楼层按 PageUp 或 PageDown 切换。");''',
    '''            if (pages > 1) sb.Append("PageUp 或 PageDown 翻页。");
            else if (!hasRestriction && HasOtherFloors()) sb.Append("其它楼层按 PageUp 或 PageDown 切换。");''')

# 5) TurnPage 实现（放在 SwitchFloor 之前）
rep('''        // ------------------------------------------------------------------ 播报
        private static void Announce()''',
    '''        /// <summary>在名单内翻页（只在名单超过一页时可用）。</summary>
        private static void TurnPage(int dir)
        {
            int pages = (_entries.Count + PageSize - 1) / PageSize;
            if (pages <= 1) return;
            _page = (_page + dir + pages) % pages;
            _announcedKey = "";          // 强制重新播报新一页
            A11yHost.Diag("MAPCHOICE 翻页 -> 第 " + (_page + 1) + " 页 / 共 " + pages + " 页");
        }

        // ------------------------------------------------------------------ 播报
        private static void Announce()''')

# 6) Clear 重置页码
rep('''            _entries.Clear();
            _announcedKey = "";''',
    '''            _entries.Clear();
            _announcedKey = "";
            _page = 0;''')

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('paging added')
