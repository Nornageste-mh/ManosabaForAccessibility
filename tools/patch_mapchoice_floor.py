import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()

# 1) live 判据：当前显示的楼层 + 游戏自己显示的标签
old = """                string name = NameOf(a);
                bool listed = displayable == null || displayable.Contains(a.GetInstanceID());
                bool live = _choiceMode && active && b != null && interactable && listed && !string.IsNullOrEmpty(name);"""
new = """                string name = NameOf(a);
                bool listed = displayable == null || displayable.Contains(a.GetInstanceID());
                // 真正的「这次能去」= 游戏把该地点的名牌显示出来了（MapArea.Hide() 会把
                // MapAreaLabel 的 CanvasGroup.alpha 归零），且它属于当前正在显示的楼层。
                // 实机证据（run95，@choiceMapArea 只允许 8 个地点）：
                //   1F 上 会客厅/玄关大厅/食堂/医务室/淋浴房/往湖方向 的 labelAlpha=1.00，
                //   厨房/审判庭入口过道 等未允许的是 0.00；其它楼层的地点是别的容器，另算。
                bool onFloor = _visibleFloor == null || IsUnder(a.transform, _visibleFloor);
                bool live = _choiceMode && active && b != null && interactable && listed && onFloor
                            && labelAlpha > 0.5f && !string.IsNullOrEmpty(name);"""
if old not in s:
    raise SystemExit('live block not found')
s = s.replace(old, new)

# 2) 记录可见楼层
old2 = """            string pageInfo = "page=null";"""
new2 = """            string pageInfo = "page=null";
            _visibleFloor = null;"""
s = s.replace(old2, new2)

# 3) 在 pageInfo 计算之后找可见楼层（遍历 MapFloor）
old3 = """            StringBuilder diag = DiagOn() ? new StringBuilder("MAPCHOICE 扫描 areas=" + areas.Length + "  " + pageInfo) : null;"""
new3 = """            // 找「当前显示的那一层」：MapFloor 用 Canvas 显隐（实机 run95 dump 见下方诊断行）
            StringBuilder floorDiag = DiagOn() ? new StringBuilder("MAPCHOICE 楼层:") : null;
            try
            {
                WitchTrials.Views.MapFloor[] floors = scope.GetComponentsInChildren<WitchTrials.Views.MapFloor>(true);
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
                }
            }
            catch (Exception e) { if (floorDiag != null) floorDiag.Append(" 取 MapFloor 失败:").Append(e.Message); }
            if (floorDiag != null) A11yHost.Diag(floorDiag.ToString());

            StringBuilder diag = DiagOn() ? new StringBuilder("MAPCHOICE 扫描 areas=" + areas.Length + "  " + pageInfo
                                                             + "  可见楼层=" + (_visibleFloor == null ? "?" : _visibleFloor.name)) : null;"""
if old3 not in s:
    raise SystemExit('diag line not found')
s = s.replace(old3, new3)

# 4) 加字段与工具方法
old4 = """        private static bool _choiceMode;"""
new4 = """        private static bool _choiceMode;
        private static Transform _visibleFloor;"""
if old4 not in s:
    raise SystemExit('field anchor not found')
s = s.replace(old4, new4)

old5 = """        /// <summary>地点中文名"""
new5 = """        /// <summary>t 是否在 root 的子树里。</summary>
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

        /// <summary>地点中文名"""
if old5 not in s:
    raise SystemExit('NameOf anchor not found')
s = s.replace(old5, new5)

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('patched MapChoice')
