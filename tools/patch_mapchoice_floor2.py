import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:70])
    s = s.replace(old, new, 1)


# 1) 楼层选择：Canvas.enabled 优先，UiVis 只作兜底
rep("""                WitchTrials.Views.MapFloor[] floors = scope.GetComponentsInChildren<WitchTrials.Views.MapFloor>(true);
                if (floors != null)
                {
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
                        if (_visibleFloor == null && (canvasOn || vis)) _visibleFloor = f.transform;
                    }
                }""",
    """                WitchTrials.Views.MapFloor[] floors = scope.GetComponentsInChildren<WitchTrials.Views.MapFloor>(true);
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
                }""")

# 2) 播报：单层不再重复「在1F：」
rep("""            StringBuilder sb = new StringBuilder();
            string curFloor = _visibleFloor == null ? "" : UiNav.FloorNameOf(_visibleFloor.name);
            sb.Append("地图上可前往 ").Append(_entries.Count).Append(" 个地点。");
            if (!string.IsNullOrEmpty(curFloor)) sb.Remove(sb.Length - 1, 1).Append("（").Append(curFloor).Append("）：");
            string lastFloor = null;
            for (int i = 0; i < _entries.Count && i < 9; i++)
            {
                Entry e = _entries[i];
                if (e.Floor != lastFloor)
                {
                    if (!string.IsNullOrEmpty(e.Floor)) sb.Append("在").Append(e.Floor).Append("：");
                    lastFloor = e.Floor;
                }
                sb.Append(i + 1).Append("、").Append(e.Name).Append("。");
            }""",
    """            StringBuilder sb = new StringBuilder();
            string curFloor = _visibleFloor == null ? "" : UiNav.FloorNameOf(_visibleFloor.name);
            sb.Append("地图上可前往 ").Append(_entries.Count).Append(" 个地点");
            if (!string.IsNullOrEmpty(curFloor)) sb.Append("（").Append(curFloor).Append("）");
            sb.Append("：");
            // 只有确实跨楼层分组时才念「在XF：」（同层时那句是废话）
            bool multiFloor = false;
            for (int i = 1; i < _entries.Count; i++) if (_entries[i].Floor != _entries[i - 1].Floor) { multiFloor = true; break; }
            string lastFloor = null;
            for (int i = 0; i < _entries.Count && i < 9; i++)
            {
                Entry e = _entries[i];
                if (multiFloor && e.Floor != lastFloor)
                {
                    if (!string.IsNullOrEmpty(e.Floor)) sb.Append("在").Append(e.Floor).Append("：");
                    lastFloor = e.Floor;
                }
                sb.Append(i + 1).Append("、").Append(e.Name).Append("。");
            }""")

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('patched floor priority + announce wording')
