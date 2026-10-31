import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:90])
    s = s.replace(old, new, 1)


# 1) 真输入注入点击：把地点按钮的世界坐标换算成屏幕坐标，再往 InputSystem 事件队列塞一次
#    按下/抬起。理由（run117/run118 实测）：合成指针事件 + onClick.Invoke 都派发了（ok=True），
#    但游戏的 `OnChoiceMapAreaAsync` 依然不返回 —— 说明它等的不是这些合成入口，
#    而是真实输入路径。本作只启用新 Input System（run31），InputInject 走 InputSystem 队列。
rep('''        /// <summary>
        /// 点一个地图地点。优先派发 EventSystem 的指针点击（走游戏的完整点击链），
        /// 没有再退回 Button.onClick —— 两者都试，实测哪个管用看诊断日志。
        /// </summary>''',
    '''        /// <summary>把地点按钮换算成屏幕坐标，通过 InputSystem 事件队列做一次真实点击。</summary>
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
        /// </summary>''')

# 2) Activate：先合成派发，再补一次真输入注入（两条路同时给，谁管用都能通）
rep('''                bool ok = e.Area != null ? ClickArea(e.Area) : false;
                if (!ok) e.Button.onClick.Invoke();
                A11yHost.Diag("MAPCHOICE 点击派发 ok=" + ok + " kind="
                              + (e.Area == null ? "?" : SafeKind(e.Area)));''',
    '''                bool ok = e.Area != null ? ClickArea(e.Area) : false;
                if (!ok) e.Button.onClick.Invoke();
                A11yHost.Diag("MAPCHOICE 点击派发 ok=" + ok + " kind="
                              + (e.Area == null ? "?" : SafeKind(e.Area)));
                // 合成派发之后补一次「真实输入」点击：run117/run118 证明光靠合成派发，
                // 游戏的 OnChoiceMapAreaAsync 不会返回（地图页一直不关）。
                if (e.Area != null) ClickByInput(e.Area);''')

# 3) TestPickKind 也走注入
rep('''                    Repeat.Say(SafeKind(a));
                    ClickArea(a);
                    return;''',
    '''                    Repeat.Say(SafeKind(a));
                    ClickArea(a);
                    ClickByInput(a);
                    return;''')

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('patched ClickByInput')
