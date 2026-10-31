import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:90])
    s = s.replace(old, new, 1)


# 1) 统一的地点点按：先走 EventSystem 指针事件（真正的点击链，包含 IPointerClickHandler），
#    再退回 onClick.Invoke()。
#    为什么：run117 实机数字键 3（食堂）之后地图页一直没关、选不中（run115 的 7 选中过），
#    说明单靠 onClick.Invoke() 不是游戏真正认的入口；`MapAreaFocusButton` 是 Button 子类，
#    也可能只在指针事件里才通知面板。
rep('''        // ------------------------------------------------------------------ 诊断
        private static string SafeKind(WitchTrials.Views.MapArea a)''',
    '''        /// <summary>
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
        private static string SafeKind(WitchTrials.Views.MapArea a)''')

# 2) Activate 用它
rep('''            try
            {
                Repeat.Say(e.Name);          // 选前复述，确认选的是哪个地点
                e.Button.onClick.Invoke();
            }''',
    '''            try
            {
                Repeat.Say(e.Name);          // 选前复述，确认选的是哪个地点
                bool ok = e.Area != null ? ClickArea(e.Area) : false;
                if (!ok) e.Button.onClick.Invoke();
                A11yHost.Diag("MAPCHOICE 点击派发 ok=" + ok + " kind="
                              + (e.Area == null ? "?" : SafeKind(e.Area)));
            }''')

# 3) TestPickKind 用它
rep('''                    WitchTrials.Views.MapAreaFocusButton fb = a._focusButton;
                    if (fb != null) { Repeat.Say(SafeKind(a)); fb.onClick.Invoke(); }
                    return;''',
    '''                    Repeat.Say(SafeKind(a));
                    ClickArea(a);
                    return;''')

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('patched ClickArea')
