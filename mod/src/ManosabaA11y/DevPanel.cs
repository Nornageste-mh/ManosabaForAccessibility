using System;
using GigaCreation.Tools.Debugging.Core;
using GigaCreation.Tools.Debugging.General;
using UnityEngine;

namespace ManosabaA11y
{
    /// <summary>
    /// 游戏内置开发者面板的开关（Ctrl+Alt+Shift+F3）。
    ///
    /// 背景：
    ///   本作用了 GigaCreation 的调试工具（`GigaCreation.Tools.Debugging.*`）：
    ///   `DebugOptionsBase` 里有静态字段 `s_debugManager`，`DebugManager.IsDebugMode`
    ///   是 UniRx 的 `ReactiveProperty&lt;bool&gt;`；`DebugPanel` 订阅它来显示/隐藏面板内容
    ///   （`DebugPanel.Start()` 里订阅 `_debugManager.IsDebugMode`，之后 `ShowContent()/HideContent()`）。
    ///   所以只要把 `IsDebugMode.Value` 翻一下，就能显示/收起开发者面板（含 `DebugCommandButtons`
    ///   的暂停/慢放/快放/回调试首页/解锁全部 CG/强制时间到等按钮）。
    ///
    /// 无障碍纪律：
    ///   - 这是**调试功能**，会扩大可达范围；配置项「允许激活开发者选项」默认关闭，
    ///     只有玩家主动打开才会响应组合键。
    ///   - 只在玩家按组合键时翻转一次，不做轮询式干预。
    /// </summary>
    internal static class DevPanel
    {
        private static bool _warnedNoManager;
        private static float _lastToggleAt = -10f;
        private static float _lastModDiagAt = -10f;

        /// <summary>当前开发者模式是否开启（读不到就按关闭算）。</summary>
        internal static bool IsDebugMode
        {
            get
            {
                try { return DebugOptionsBase.IsDebugMode; }
                catch { return false; }
            }
        }

        /// <summary>翻转开发者模式。返回 true 表示确实翻成功了。</summary>
        internal static bool Toggle()
        {
            try
            {
                IDebugManager mgr = EnsureManager();
                if (mgr != null)
                {
                    UniRx.IReactiveProperty<bool> prop = mgr.IsDebugMode;
                    if (prop != null)
                    {
                        UniRx.ReactiveProperty<bool> rp = prop.TryCast<UniRx.ReactiveProperty<bool>>();
                        if (rp != null)
                        {
                            bool next = !rp.Value;
                            rp.Value = next;
                            A11yHost.Diag("DevPanel: IsDebugMode -> " + (next ? "true" : "false"));
                            bool shown = SetPanelVisible(next);
                            return true;
                        }
                    }
                }

                // 退路：管理器拿不到（release 下常见）就直接开/合面板本体。
                bool nowOn = IsPanelShown();
                bool ok2 = SetPanelVisible(!nowOn);
                A11yHost.Diag("DevPanel: 直接切面板 visible " + nowOn + " -> " + (!nowOn) + " ok=" + ok2);
                return ok2;
            }
            catch (Exception e)
            {
                A11yHost.Diag("DevPanel.Toggle 异常: " + e.GetType().Name + ": " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// 拿调试管理器；release 构建里游戏自己不创建（实测 `s_debugManager == null`，
        /// 而 `s_isInitialized == true`），所以这里补建一个带 `initialMode=true` 的。
        /// 静态字段有 public setter（反编译确认），补建不会动游戏存档。
        /// </summary>
        private static IDebugManager EnsureManager()
        {
            try
            {
                IDebugManager mgr = DebugOptionsBase.s_debugManager;
                if (mgr != null) return mgr;

                if (!_warnedNoManager)
                {
                    _warnedNoManager = true;
                    A11yHost.Diag("DevPanel: s_debugManager 为空，尝试补建 DebugManager(true)");
                }

                var created = new GigaCreation.Tools.Debugging.Core.DebugManager(true);
                if (created == null) return null;

                // interop 下具体类不会隐式转接口，必须 TryCast。
                IDebugManager createdMgr = created.TryCast<IDebugManager>();
                if (createdMgr == null)
                {
                    A11yHost.Diag("DevPanel: DebugManager 转 IDebugManager 失败");
                    return null;
                }

                DebugOptionsBase.s_debugManager = createdMgr;
                A11yHost.Diag("DevPanel: 已补建 DebugManager，IsDebugMode="
                              + (createdMgr.IsDebugMode != null && createdMgr.IsDebugMode.Value));
                return createdMgr;
            }
            catch (Exception e)
            {
                A11yHost.Diag("DevPanel.EnsureManager 异常: " + e.GetType().Name + ": " + e.Message);
                return null;
            }
        }

        private static GigaCreation.Tools.Debugging.Ui.DebugPanel Panel()
        {
            try
            {
                if (_instantiated != null) return _instantiated;

                var panels = Resources.FindObjectsOfTypeAll<GigaCreation.Tools.Debugging.Ui.DebugPanel>();
                if (panels == null || panels.Length == 0) return null;

                // 优先用「已在场景里」的实例；release 构建实测只有 prefab 资产
                // （`scene=<prefab/无场景>`，游戏从来没实例化过它），那就自己实例化一份，
                // 挂到场景里的 Canvas 下（否则 UI 没有画布、看不见）。
                foreach (var p in panels)
                {
                    if (p != null && p.gameObject.scene.IsValid())
                    {
                        _instantiated = p;
                        return p;
                    }
                }

                var prefabPanel = panels[0];
                if (prefabPanel == null) return null;

                A11yHost.Diag("DevPanel: 场景里没有 DebugPanel 实例，实例化 prefab 面板本体");
                GameObject clone = UnityEngine.Object.Instantiate(prefabPanel.gameObject);
                if (clone == null) return null;
                clone.name = prefabPanel.name + "(A11yDevPanel)";

                Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    clone.transform.SetParent(canvas.transform, false);
                    A11yHost.Diag("DevPanel: 已挂到 Canvas " + canvas.name + " sortingOrder=" + canvas.sortingOrder);
                }
                else
                {
                    A11yHost.Diag("DevPanel: 场景里没有 Canvas，面板可能不可见");
                }

                var found = clone.GetComponent<GigaCreation.Tools.Debugging.Ui.DebugPanel>();
                if (found == null) return null;

                _instantiated = found;
                return found;
            }
            catch (Exception e)
            {
                A11yHost.Diag("DevPanel.Panel 异常: " + e.GetType().Name + ": " + e.Message);
                return null;
            }
        }

        private static GigaCreation.Tools.Debugging.Ui.DebugPanel _instantiated;

        private static bool IsPanelShown()
        {
            var p = Panel();
            return p != null && p.gameObject.activeInHierarchy;
        }

        private static bool SetPanelVisible(bool show)
        {
            var p = Panel();
            if (p == null)
            {
                A11yHost.Diag("DevPanel: 场景里找不到 DebugPanel");
                return false;
            }

            try
            {
                if (show)
                {
                    // 实测：DebugPanel 自己 activeSelf 已经是 true，但父链上有未激活的
                    // 祖先（游戏只在调试构建里启用那一支），所以 activeInHierarchy 仍 false。
                    // 开面板时把祖先链一起点亮（最多 8 层，只动未激活的那几层）。
                    Transform t = p.transform;
                    for (int i = 0; t != null && i < 8; i++)
                    {
                        if (!t.gameObject.activeSelf)
                        {
                            A11yHost.Diag("DevPanel: 激活祖先 " + t.name);
                            t.gameObject.SetActive(true);
                        }
                        t = t.parent;
                    }
                    if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
                    p.ShowContent();
                }
                else
                {
                    p.HideContent();
                }
                A11yHost.Diag("DevPanel: DebugPanel show=" + show + " activeInHierarchy=" + p.gameObject.activeInHierarchy);
                return true;
            }
            catch (Exception e)
            {
                A11yHost.Diag("DevPanel.SetPanelVisible 异常: " + e.GetType().Name + ": " + e.Message);
                return false;
            }
        }

        private static bool WinHeld(KeyCode a, KeyCode b)
        {
            try { return GlobalKeys.Held(a) || GlobalKeys.Held(b); }
            catch { return false; }
        }

        // ====================================================================
        // 一次性侦察（MANOSABA_A11Y_DEBUGUI=1）
        // ====================================================================
        //
        // 为什么需要：release 构建里 `DebugOptionsBase.s_debugManager` 实测是 null，
        // 说明游戏根本没创建调试管理器；但场景里可能还留着 `DebugPanel`
        // 或 `EnableDebugModeButton`（后者的 `_debugManager` 字段可能就是唯一入口）。
        // 这段只在环境变量打开时跑，把现场清点一遍，用来决定「面板到底能不能开」。
        private static int _dumped;
        private static float _nextDumpAt = 15f;

        private static void DumpIfDue()
        {
            if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_DEBUGUI") != "1") return;
            if (_dumped >= 2) return;
            float now = Time.realtimeSinceStartup;
            if (now < _nextDumpAt) return;
            _dumped++;
            _nextDumpAt = now + 20f;

            try
            {
                A11yHost.Diag("DEBUGUI[" + _dumped + "] t=" + now.ToString("F1")
                              + " s_isInitialized=" + DebugOptionsBase.s_isInitialized
                              + " s_debugManager=" + (DebugOptionsBase.s_debugManager == null ? "null" : "非空")
                              + " IsDebugMode=" + IsDebugMode);

                var panels = Resources.FindObjectsOfTypeAll<GigaCreation.Tools.Debugging.Ui.DebugPanel>();
                A11yHost.Diag("DEBUGUI[" + _dumped + "] DebugPanel=" + (panels == null ? 0 : panels.Length));
                if (panels != null)
                {
                    for (int i = 0; i < panels.Length; i++)
                    {
                        var p = panels[i];
                        if (p == null) continue;
                        string contents = "";
                        try
                        {
                            var cs = p._contents;
                            if (cs != null)
                                for (int k = 0; k < cs.Length && k < 12; k++)
                                    contents += (k > 0 ? "," : "") + (cs[k] == null ? "-" : cs[k].name);
                        }
                        catch (Exception e) { contents = "<读不到 " + e.GetType().Name + ">"; }
                        A11yHost.Diag("DEBUGUI[" + _dumped + "]   panel " + i + " name=" + p.name
                                      + " activeSelf=" + p.gameObject.activeSelf
                                      + " activeInHierarchy=" + p.gameObject.activeInHierarchy
                                      + " scene=" + (p.gameObject.scene.IsValid() ? p.gameObject.scene.name : "<prefab/无场景>")
                                      + " parent=" + (p.transform.parent == null ? "null" : p.transform.parent.name)
                                      + " showBtn=" + (p._showButton == null ? "null" : p._showButton.name)
                                      + " hideBtn=" + (p._hideButton == null ? "null" : p._hideButton.name)
                                      + " contents=[" + contents + "]");
                    }
                }

                var enables = Resources.FindObjectsOfTypeAll<GigaCreation.Tools.Debugging.Ui.EnableDebugModeButton>();
                A11yHost.Diag("DEBUGUI[" + _dumped + "] EnableDebugModeButton=" + (enables == null ? 0 : enables.Length));
                if (enables != null)
                {
                    for (int i = 0; i < enables.Length && i < 8; i++)
                    {
                        var b = enables[i];
                        if (b == null) continue;
                        A11yHost.Diag("DEBUGUI[" + _dumped + "]   enableBtn " + i + " name=" + b.name
                                      + " active=" + b.gameObject.activeInHierarchy
                                      + " mgr=" + (b._debugManager == null ? "null" : "非空"));
                    }
                }

                var cmds = Resources.FindObjectsOfTypeAll<WitchTrials.Debugging.DebugCommandButtons>();
                A11yHost.Diag("DEBUGUI[" + _dumped + "] DebugCommandButtons=" + (cmds == null ? 0 : cmds.Length));
                if (cmds != null)
                {
                    for (int i = 0; i < cmds.Length && i < 4; i++)
                    {
                        var c = cmds[i];
                        if (c == null) continue;
                        A11yHost.Diag("DEBUGUI[" + _dumped + "]   cmdButtons " + i + " name=" + c.name
                                      + " active=" + c.gameObject.activeInHierarchy
                                      + " panel=" + (c._debugPanel == null ? "null" : c._debugPanel.name));
                    }
                }

                // 场景里那个叫 `Debug` 的根对象（scene-tree 实查）——SRDebugger 的挂载点。
                GameObject dbgRoot = GameObject.Find("Debug");
                if (dbgRoot == null)
                {
                    dbgRoot = GameObject.Find("/Debug");
                }
                if (dbgRoot == null)
                {
                    A11yHost.Diag("DEBUGUI[" + _dumped + "] 场景里没有叫 Debug 的对象");
                }
                else
                {
                    string comps = "";
                    try
                    {
                        var cs = dbgRoot.GetComponents<Component>();
                        for (int i = 0; i < cs.Length; i++)
                            comps += (i > 0 ? "," : "") + (cs[i] == null ? "null" : cs[i].GetType().Name);
                    }
                    catch (Exception e) { comps = "<读不到 " + e.GetType().Name + ">"; }
                    string kids = "";
                    try
                    {
                        for (int i = 0; i < dbgRoot.transform.childCount && i < 20; i++)
                        {
                            var ch = dbgRoot.transform.GetChild(i);
                            kids += (i > 0 ? "," : "") + ch.name + "(" + ch.gameObject.activeSelf + ")";
                        }
                    }
                    catch (Exception e) { kids = "<读不到 " + e.GetType().Name + ">"; }
                    A11yHost.Diag("DEBUGUI[" + _dumped + "] Debug 对象 activeSelf=" + dbgRoot.activeSelf
                                  + " activeInHierarchy=" + dbgRoot.activeInHierarchy
                                  + " comps=[" + comps + "] children=[" + kids + "]");
                }
            }
            catch (Exception e)
            {
                A11yHost.Diag("DEBUGUI 异常: " + e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>每帧调用：接组合键 Ctrl+Alt+Shift+F3。</summary>
        internal static void Update()
        {
            try
            {
                DumpIfDue();

                if (Plugin.CfgAllowDevPanel == null || !Plugin.CfgAllowDevPanel.Value) return;

                // 0.6 秒内只处理一次，避免按住时连环翻转。
                float now = Time.realtimeSinceStartup;
                if (now - _lastToggleAt < 0.6f) return;

                // 诊断：三个修饰键同时按住时每秒记一次，用来区分「组合键没送到」与
                // 「送来了但 F3 没被读到」（自动化走 keybd_event，Win32 与 InputSystem 两条路不同）。
                bool mods = (InputSysKeys.CtrlHeld && InputSysKeys.AltHeld && InputSysKeys.ShiftHeld)
                            || (WinHeld(KeyCode.LeftControl, KeyCode.RightControl)
                                && WinHeld(KeyCode.LeftAlt, KeyCode.RightAlt)
                                && WinHeld(KeyCode.LeftShift, KeyCode.RightShift));
                if (mods && now - _lastModDiagAt > 1f)
                {
                    _lastModDiagAt = now;
                    A11yHost.Diag("DevPanel: 修饰键按住，F3 isys=" + InputSysKeys.Pressed(KeyCode.F3)
                                  + " win32=" + GlobalKeys.Held(KeyCode.F3));
                }

                // ★ 用 Plugin.SpecDown（Hotkey 的 Win32 全局兜底 + 新 Input System 并集）。
                //   0.0.5 实测：自动化 keybd_event 注入的键**进不了** InputSystem 的
                //   Keyboard.current（Raw Input 不收注入事件），但 Win32 GetAsyncKeyState 收，
                //   所以只读 InputSysKeys 会「明明按了却毫无反应」。
                if (!Plugin.SpecDown("Ctrl+Alt+Shift+F3")) return;

                _lastToggleAt = now;
                bool ok = Toggle();
                if (!ok) return;

                bool on = IsDebugMode;
                A11yHost.Diag("DevPanel: 组合键触发，当前 IsDebugMode=" + (on ? "true" : "false"));
                Repeat.Say(on ? "开发者面板，已开启。" : "开发者面板，已关闭。", true);
            }
            catch (Exception e)
            {
                A11yHost.Diag("DevPanel.Update 异常: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
