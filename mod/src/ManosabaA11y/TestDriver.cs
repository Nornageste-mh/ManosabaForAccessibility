using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Naninovel;
using UnityEngine.UI;

namespace ManosabaA11y
{
    /// <summary>
    /// 无人值守实机测试驱动器（**只**在环境变量 MANOSABA_A11Y_TEST=1 时启用）。
    ///
    /// 为什么需要它：实机验收时靠鼠标点击（probe\run5-automation.ps1）
    /// 会受窗口焦点/全屏/分辨率影响，run5 就卡在标题画面 60 次点击没进剧情。
    /// 这里改用引擎内部入口，绕开鼠标：
    ///   1) 找标题画面「开始游戏」按钮：Resources.FindObjectsOfTypeAll&lt;Button&gt;()
    ///      + 子物体文字匹配（New Game / はじめから / 开始游戏），然后 button.onClick.Invoke()；
    ///   2) 进剧情后 IScriptPlayer.SetAutoPlayEnabled(true)，让文本自动推进，不需要点屏幕。
    ///
    /// ★ 发布版必须不带这个行为：本类只在 Plugin.TestMode 为 true 时被创建，
    ///   且 TestMode 只由环境变量打开（见 Plugin.Load）。生产用户不会设置该变量。
    /// </summary>
    internal static class TestDriver
    {
        private static int _phase;
        private static float _phaseAt;
        private static bool _started;
        private static int _frames;
        private static int _storyAttempts;
        private static int _injectClicks;

        /// <summary>
        /// 纯点击模式（环境变量 MANOSABA_A11Y_TEST_PLAY=1）：
        /// 不调用 LoadAndPlay，只点标题「New Game」+ 定时真的点击屏幕中心，
        /// 走玩家完全相同的路径。用来回答「真实流程里读屏是否逐句出声」。
        /// </summary>
        internal static readonly bool PlayMode =
            Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_PLAY") == "1";

        /// <summary>
        /// 输入注入模式（环境变量 MANOSABA_A11Y_INJECT=1）：
        /// 用 UnityEngine.InputSystem.QueueStateEvent 往引擎里塞鼠标状态事件，
        /// 绕开 Win32 焦点/帧率问题（本作 6 FPS，Win32 短点击常整段落在一帧内被轮询吞掉）。
        /// </summary>
        internal static readonly bool InjectMode =
            Environment.GetEnvironmentVariable("MANOSABA_A11Y_INJECT") == "1";

        private static readonly string[] StartWords =
        {
            "New Game", "NewGame", "はじめから", "开始游戏", "開始遊戲", "新的游戏", "新游戏"
        };

        // ---- Win32 抢前台 / 模拟点击（仅测试用）----
        // 证据：run12 里 `Application.isFocused=True`、`runInBackground=True`，
        // 但脚本仍停在打印行（playedIndex 不动）→ 不是焦点问题。
        // 于是改为「真的模拟一次鼠标点击」：本作推进剧情的输入是点击/回车，
        // 只要点击能落到窗口里，引擎就会走它自己正常的继续流程（不猜测内部 API）。
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);

        private const int SW_RESTORE = 9;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        private static void ForceForeground()
        {
            try
            {
                IntPtr hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                IntPtr fg = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return;
                ShowWindow(hwnd, SW_RESTORE);

                // Windows 默认禁止后台进程抢前台：临时把输入队列 attach 到前台线程再 SetForegroundWindow
                uint fgThread = GetWindowThreadProcessId(fg, out uint _);
                uint myThread = GetCurrentThreadId();
                bool attached = false;
                if (fgThread != 0 && fgThread != myThread)
                {
                    try { attached = AttachThreadInput(myThread, fgThread, true); } catch { }
                }
                try { SetForegroundWindow(hwnd); } catch { }
                if (attached) { try { AttachThreadInput(myThread, fgThread, false); } catch { } }

                A11yHost.Diag("TESTDRIVER foreground hwnd=" + hwnd.ToInt64() + " before=" + fg.ToInt64()
                    + " after=" + GetForegroundWindow().ToInt64()
                    + " focused=" + Application.isFocused
                    + " runInBackground=" + Application.runInBackground
                    + " timeScale=" + Time.timeScale + " delta=" + Time.deltaTime);
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER ForceForeground: " + e.Message); }
        }

        /// <summary>在窗口中央做一次真实左键点击（和玩家点屏幕完全同路径）。</summary>
        private static void ClickCenter()
        {
            try
            {
                int w = Screen.width, h = Screen.height;
                if (w <= 0 || h <= 0) { A11yHost.Diag("TESTDRIVER Click: Screen 尺寸无效"); return; }
                int x = w / 2, y = h / 2;
                SetCursorPos(x, y);
                System.Threading.Thread.Sleep(40);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                System.Threading.Thread.Sleep(40);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                A11yHost.Diag("TESTDRIVER 点击中心 (" + x + "," + y + ")");
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER Click: " + e.Message); }
        }

        public static void Reset()
        {
            _phase = 0;
            _phaseAt = 0f;
            _started = false;
            _frames = 0;
        }

        // ---- 引擎输入采样器（只给无人值守测试用）----
        // 证据：interop 实查 `Naninovel.IInputSampler` 有 `bool Active / bool StartedDuringFrame /
        // void Activate(float value)`，`IInputManager` 有 `IInputSampler GetSampler(string bindingName)`。
        // 这说明脚本推进本质上是在等采样器被激活；Activate 是引擎自己的入口，
        // 等价于玩家按了一下「继续」（不是我们改游戏逻辑，也不是按坐标猜 UI）。
        private static readonly string[] SamplerNames =
        {
            "Continue", "continue", "Submit", "Advance", "Next", "Skip", "AutoPlay", "HideUI", "Pause"
        };
        private static string _continueName;
        private static float _continueReleaseAt;

        private static void ProbeSamplers()
        {
            try
            {
                Naninovel.IInputManager im = Naninovel.Engine.GetService<Naninovel.IInputManager>();
                if (im == null) { A11yHost.Diag("TESTDRIVER ProbeSamplers: inputManager 为 null"); return; }
                A11yHost.Diag("TESTDRIVER ProbeSamplers ProcessInput=" + im.ProcessInput + " InputMode=" + im.InputMode);
                foreach (string n in SamplerNames)
                {
                    try
                    {
                        Naninovel.IInputSampler s = im.GetSampler(n);
                        if (s == null) { A11yHost.Diag("TESTDRIVER sampler '" + n + "' = null"); continue; }
                        string bn = "?";
                        try { bn = s.Binding.Name; } catch { }
                        A11yHost.Diag("TESTDRIVER sampler '" + n + "' OK binding=" + bn
                                     + " enabled=" + s.Enabled + " active=" + s.Active + " value=" + s.Value);
                        if (string.IsNullOrEmpty(_continueName)) _continueName = n;
                    }
                    catch (Exception e) { A11yHost.Diag("TESTDRIVER sampler '" + n + "' 不可用: " + e.GetType().Name); }
                }
                A11yHost.Diag("TESTDRIVER ProbeSamplers 选用 continueSampler='" + (_continueName ?? "<none>") + "'");
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER ProbeSamplers: " + e.Message); }
        }

        /// <summary>用引擎输入采样器触发一次「继续」。返回是否成功发出。</summary>
        private static bool TryEngineContinue()
        {
            try
            {
                Naninovel.IInputManager im = Naninovel.Engine.GetService<Naninovel.IInputManager>();
                if (im == null) return false;
                if (string.IsNullOrEmpty(_continueName)) { ProbeSamplers(); if (string.IsNullOrEmpty(_continueName)) return false; }
                Naninovel.IInputSampler s = im.GetSampler(_continueName);
                if (s == null) return false;
                s.Activate(1f);
                _continueReleaseAt = Time.unscaledTime + 0.2f;
                return true;
            }
            catch (Exception e)
            {
                A11yHost.Diag("TESTDRIVER TryEngineContinue: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// 诊断用：每轮注入后记录「引擎侧有没有看到这次输入」。
        /// 判据是 `IInputSampler.StartedDuringFrame / Active`——这两个字段为 true 才说明
        /// 输入真的进了引擎的采样器；一直 false 就说明注入的事件没被引擎消费，
        /// 再怎么点都不会推进剧情（这也是判断「是环境问题还是补丁问题」的关键证据）。
        /// </summary>
        private static void DumpSampler(string tag)
        {
            try
            {
                string cont = "?";
                try
                {
                    Naninovel.IInputManager im = Naninovel.Engine.GetService<Naninovel.IInputManager>();
                    if (im != null && !string.IsNullOrEmpty(_continueName))
                    {
                        Naninovel.IInputSampler s = im.GetSampler(_continueName);
                        if (s != null) cont = "active=" + s.Active + " started=" + s.StartedDuringFrame
                                            + " ended=" + s.EndedDuringFrame + " value=" + s.Value;
                    }
                }
                catch (Exception e) { cont = "<err " + e.GetType().Name + ">"; }

                string pl = "?";
                try
                {
                    Naninovel.IScriptPlayer sp = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>();
                    if (sp != null)
                    {
                        string cmd = "?";
                        try { cmd = sp.PlayedCommand != null ? sp.PlayedCommand.GetType().Name : "<null>"; } catch { }
                        pl = "playing=" + sp.Playing + " waiting=" + sp.WaitingForInput + " auto=" + sp.AutoPlayActive
                           + " index=" + sp.PlayedIndex + " count=" + sp.PlayedCommandsCount + " cmd=" + cmd;
                    }
                }
                catch (Exception e) { pl = "<err " + e.GetType().Name + ">"; }

                A11yHost.Diag("TESTDRIVER[" + tag + "] sampler " + _continueName + " " + cont + " | player " + pl);
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER DumpSampler: " + e.Message); }
        }

        /// <summary>
        /// 注入模式专用：把「标题画面的 New Game」用 InputSystem 状态事件点下去。
        /// 优先用 TitleUi._newGameButton 的 RectTransform 换算屏幕坐标（Overlay Canvas 下
        /// rectTransform.position 就是屏幕坐标），拿不到就退回标题截图实测的 (330, 1080-770)。
        /// </summary>
        private static bool InjectStartButton()
        {
            try
            {
                WitchTrials.Views.TitleUi[] tus = Resources.FindObjectsOfTypeAll<WitchTrials.Views.TitleUi>();
                if (tus != null)
                {
                    foreach (WitchTrials.Views.TitleUi t in tus)
                    {
                        if (t == null) continue;
                        Button nb = null;
                        try { nb = t._newGameButton; } catch { }
                        if (nb == null) continue;
                        RectTransform rt = nb.transform.TryCast<RectTransform>();
                        if (rt == null) continue;
                        // RectTransform.position 在不同 Canvas 渲染模式下含义不同
                        // （run23 实测直接用它得到 (-7.18,-4.99)，明显是画布局部坐标而不是像素），
                        // 所以必须经 RectTransformUtility 换算成屏幕坐标。
                        Canvas canvas = rt.GetComponentInParent<Canvas>();
                        Camera cam = canvas != null ? canvas.worldCamera : null;
                        Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
                        A11yHost.Diag("TESTDRIVER inject 标题 New Game 位置 raw=" + rt.position.x + "," + rt.position.y
                                     + " screen=" + sp.x + "," + sp.y
                                     + " canvas=" + (canvas != null ? canvas.renderMode.ToString() : "null")
                                     + " active=" + nb.gameObject.activeInHierarchy);
                        InputInject.Click(sp.x, sp.y);
                        return true;
                    }
                }
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER InjectStartButton: " + e.Message); }
            A11yHost.Diag("TESTDRIVER inject 标题按钮没找到，用固定坐标 (330,310)");
            InputInject.Click(330f, 310f);
            return true;
        }

        /// <summary>由 PluginBehaviour.Update 每帧调用（仅测试模式）。</summary>
        public static void Update()
        {
            _frames++;
            float now = Time.unscaledTime;
            if (_phaseAt <= 0f) _phaseAt = now;
            if (InjectMode) { InputInject.Tick(); if (_continueReleaseAt > 0f && now >= _continueReleaseAt) { _continueReleaseAt = 0f; try { Naninovel.IInputManager im = Naninovel.Engine.GetService<Naninovel.IInputManager>(); if (im != null && !string.IsNullOrEmpty(_continueName)) im.GetSampler(_continueName).Activate(0f); } catch { } } }

            try
            {
                if (!Naninovel.Engine.Initialized)
                {
                    if (_frames % 300 == 0) A11yHost.Diag("TESTDRIVER 等引擎初始化 frame=" + _frames);
                    return;
                }
            }
            catch (Exception e)
            {
                A11yHost.Diag("TESTDRIVER engine: " + e.Message);
                return;
            }

            try
            {
                switch (_phase)
                {
                    case 0:
                        // 引擎起来后先诊一下：当前脚本、打印器、面板
                        if (now - _phaseAt < 5f) return;
                        DumpState("init");
                        DumpConfig();
                        Next(now);
                        break;

                    case 1:
                        // 还没点进游戏 → 用按钮 onClick 直接开始
                        if (InjectMode)
                        {
                            // 注入模式：等标题脚本布置好按钮，再用 InputSystem 状态事件点 New Game
                            if (now - _phaseAt < 22f) return;
                            DumpState("beforeInjectStart");
                            InjectStartButton();
                            Next(now);
                            break;
                        }
                        if (_started && now - _phaseAt < 20f) return;
                        _started = TryStartGame();
                        DumpState("afterStart attempt=" + _started);
                        Next(now);
                        break;

                    case 2:
                        // 进剧情后打开自动播放，让台词自己往下走
                        if (now - _phaseAt < 3f) return;
                        if (InjectMode)
                        {
                            // 注入模式**绝不能**用 LoadAndPlay 抄近道：
                            // run23 实测它只推进脚本状态机（scriptPlayer path=Adv01）而画面仍停在标题，
                            // 测出来的「有没有念」根本不代表玩家路径。这里只观察游戏自己的流程。
                            DumpState("afterInjectStart say=" + Reader.SpokenCount);
                            ProbeSamplers();
                        }
                        else if (!PlayMode) TryAutoPlay();
                        else A11yHost.Diag("TESTDRIVER 纯点击模式：不 LoadAndPlay，改用真实点击推进");
                        Next(now);
                        break;

                    case 3:
                        // 注入模式：每 2.5 秒往 InputSystem 塞一次点击（按住 0.28s），
                        // 同时用引擎自己的输入采样器触发一次「继续」（等价于玩家按了继续键）。
                        if (InjectMode)
                        {
                            if (now - _phaseAt < 2.5f) return;
                            InputInject.Click(960f, 540f);
                            // 键盘通道：本作「继续」绑定里有键盘；实测鼠标点击在 6 FPS 下常常不触发。
                            InputInject.PressKey(UnityEngine.InputSystem.Key.Enter);
                            bool cont = TryEngineContinue();
                            _injectClicks++;
                            DumpSampler("click#" + _injectClicks);
                            if (_injectClicks % 4 == 0)
                            {
                                DumpState("inject say=" + Reader.SpokenCount + " clicks=" + _injectClicks + " continue=" + cont);
                            }
                            _phaseAt = now;
                            return;
                        }
                        // 纯点击模式每 4 秒点一次（模拟玩家）；默认模式每 15 秒报一次进度
                        if (now - _phaseAt < (PlayMode ? 4f : 15f)) { return; }
                        if (PlayMode)
                        {
                            DumpState("play say=" + Reader.SpokenCount);
                            ForceForeground();
                            ClickCenter();
                            _phaseAt = now;
                            return;
                        }
                        DumpState("progress say=" + Reader.SpokenCount);
                        // 抢前台 + 真的点一下屏幕（本作推进剧情靠点击/回车；
                        // 证据：run12 isFocused=True 也没推进 → 重点是「产生一次真实输入」）
                        ForceForeground();
                        ClickCenter();
                        // 标题脚本/进正片会重置自动播放（naninovel-re probe3 实测），每轮重申一次
                        try
                        {
                            Naninovel.IScriptPlayer sp3 = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>();
                            if (sp3 != null && sp3.Playing && !sp3.AutoPlayActive) sp3.SetAutoPlayEnabled(true);
                        }
                        catch (Exception e3) { A11yHost.Diag("TESTDRIVER reautoplay: " + e3.Message); }
                        // 还没念出任何一句话 → 说明没进正片，继续重试（加载脚本是异步的，要给它时间）
                        if (Reader.SpokenCount == 0 && _storyAttempts < 5) TryAutoPlay();
                        _phaseAt = now;
                        break;
                }
            }
            catch (Exception e)
            {
                A11yHost.Diag("TESTDRIVER update: " + e.Message);
                Next(now);
            }
        }

        private static void Next(float now)
        {
            _phase++;
            _phaseAt = now;
        }

        // ==================== 状态诊断 ====================

        /// <summary>
        /// 读游戏自己的「起始剧本 / 调试开关」静态字段（全部来自 re\decompiled\signatures 实查）：
        ///   WitchTrials.Debugging.DebugOptions.StartGameScenario / SkipSplashScreen / SkipEntrySequenceAtFirstTime
        ///   WitchTrials.Views.TitleUi.NextScenarioVarName / GameProgressVarName / StartGameLabelName
        /// 这些字段的值决定「New Game 到底播哪个脚本、能不能跳开场」，比猜脚本路径可靠。
        /// </summary>
        private static void DumpConfig()
        {
            try
            {
                Type dbg = Type.GetType("WitchTrials.Debugging.DebugOptions, Assembly-CSharp");
                if (dbg != null)
                {
                    A11yHost.Diag("TESTDRIVER cfg DebugOptions.StartGameScenario='" + StaticStr(dbg, "StartGameScenario") + "'"
                        + " SkipSplash=" + StaticStr(dbg, "SkipSplashScreen")
                        + " SkipEntry=" + StaticStr(dbg, "SkipEntrySequenceAtFirstTime")
                        + " DebugSkip=" + StaticStr(dbg, "DebugSkip"));
                }
                else { A11yHost.Diag("TESTDRIVER cfg DebugOptions 类型未找到"); }
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER cfg DebugOptions: " + e.Message); }

            try
            {
                Type tui = Type.GetType("WitchTrials.Views.TitleUi, Assembly-CSharp");
                if (tui != null)
                {
                    A11yHost.Diag("TESTDRIVER cfg TitleUi.NextScenarioVarName='" + StaticStr(tui, "NextScenarioVarName") + "'"
                        + " GameProgressVarName='" + StaticStr(tui, "GameProgressVarName") + "'"
                        + " StartGameLabelName='" + StaticStr(tui, "StartGameLabelName") + "'");
                }
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER cfg TitleUi: " + e.Message); }

            try
            {
                WitchTrials.Models.IWitchTrialsNaninovelManager iface =
                    Naninovel.Engine.GetService<WitchTrials.Models.IWitchTrialsNaninovelManager>();
                WitchTrials.Models.WitchTrialsNaninovelManager mgr = iface == null
                    ? null : iface.TryCast<WitchTrials.Models.WitchTrialsNaninovelManager>();
                if (mgr != null)
                {
                    A11yHost.Diag("TESTDRIVER cfg StartGameScenarioName='" + mgr.StartGameScenarioName + "'");
                }
                else { A11yHost.Diag("TESTDRIVER cfg 拿不到 WitchTrialsNaninovelManager"); }
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER cfg manager: " + e.Message); }
        }

        private static string StaticStr(Type t, string field)
        {
            try
            {
                System.Reflection.FieldInfo f = t.GetField(field,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (f == null) return "<无此字段>";
                object v = f.GetValue(null);
                return v == null ? "<null>" : v.ToString();
            }
            catch (Exception e) { return "<err " + e.GetType().Name + ">"; }
        }

        private static void DumpState(string tag)
        {
            try
            {
                A11yHost.Diag("TESTDRIVER[" + tag + "] sceneCount=" + UnityEngine.SceneManagement.SceneManager.sceneCount);
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    UnityEngine.SceneManagement.Scene s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    A11yHost.Diag("TESTDRIVER[" + tag + "] scene=" + s.name + " loaded=" + s.isLoaded);
                }

                try
                {
                    Naninovel.IScriptPlayer sp = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>();
                    if (sp != null)
                    {
                        string path = "";
                        try { path = sp.PlayedScript == null ? "" : sp.PlayedScript.Path; } catch { }
                        A11yHost.Diag("TESTDRIVER[" + tag + "] scriptPlayer playing=" + sp.Playing
                                     + " path=" + path + " auto=" + sp.AutoPlayActive + " waiting=" + sp.WaitingForInput);
                    }
                }
                catch (Exception e) { A11yHost.Diag("TESTDRIVER[" + tag + "] scriptPlayer: " + e.Message); }

                // 进程/时间/焦点状态：用来判断「脚本卡住」是不是因为 Unity 认为窗口不在前台、
                // 时间被冻结，或者 Naninovel 的 ContinueInputUI 根本没进 waiting 状态。
                try
                {
                    A11yHost.Diag("TESTDRIVER[" + tag + "] time timeScale=" + Time.timeScale
                                 + " delta=" + Time.deltaTime + " unscaledDelta=" + Time.unscaledDeltaTime
                                 + " focused=" + Application.isFocused + " runInBackground=" + Application.runInBackground
                                 + " frame=" + Time.frameCount);
                }
                catch (Exception e) { A11yHost.Diag("TESTDRIVER[" + tag + "] time: " + e.Message); }

                // ContinueInputUI：Naninovel 的「点击继续」输入对象；visible=True 才是真的在等输入
                try
                {
                    Naninovel.UI.ContinueInputUI[] cis = Resources.FindObjectsOfTypeAll<Naninovel.UI.ContinueInputUI>();
                    A11yHost.Diag("TESTDRIVER[" + tag + "] continueInputs=" + (cis == null ? 0 : cis.Length));
                    if (cis != null)
                    {
                        foreach (Naninovel.UI.ContinueInputUI ci in cis)
                        {
                            if (ci == null) continue;
                            bool vis = false;
                            try { vis = ci.Visible; } catch { }
                            bool act = false;
                            try { act = ci.gameObject.activeInHierarchy; } catch { }
                            bool trig = false;
                            try { GameObject t = ci.trigger; trig = t != null && t.activeInHierarchy; } catch { }
                            A11yHost.Diag("TESTDRIVER[" + tag + "] continueInput visible=" + vis + " active=" + act + " triggerActive=" + trig);
                        }
                    }
                }
                catch (Exception e) { A11yHost.Diag("TESTDRIVER[" + tag + "] continueInput: " + e.Message); }

                // 打印机面板现场：有文字说明剧情真的在走，没文字说明脚本卡住了（而不是补丁没挂上）
                try
                {
                    Naninovel.UI.UITextPrinterPanel[] panels = Resources.FindObjectsOfTypeAll<Naninovel.UI.UITextPrinterPanel>();
                    A11yHost.Diag("TESTDRIVER[" + tag + "] panels=" + (panels == null ? 0 : panels.Length));
                    if (panels != null)
                    {
                        foreach (Naninovel.UI.UITextPrinterPanel p in panels)
                        {
                            if (p == null) continue;
                            string ptype = p.GetType().Name;
                            bool pactive = false;
                            try { pactive = p.gameObject.activeInHierarchy; } catch { }
                            string author = "";
                            try
                            {
                                Naninovel.UI.RevealableTextPrinterPanel rp = p.TryCast<Naninovel.UI.RevealableTextPrinterPanel>();
                                if (rp != null) author = rp.Author == null ? "<null>" : rp.Author.Id;
                            }
                            catch { }
                            string txt = "";
                            try
                            {
                                TMPro.TMP_Text t = p.GetComponentInChildren<TMPro.TMP_Text>();
                                if (t != null && t.text != null) txt = t.text.Replace("\n", " ");
                            }
                            catch { }
                            if (txt.Length > 120) txt = txt.Substring(0, 120);
                            A11yHost.Diag("TESTDRIVER[" + tag + "] panel " + ptype + " active=" + pactive
                                         + " author=" + author + " text=[" + txt + "]");
                        }
                    }
                }
                catch (Exception e) { A11yHost.Diag("TESTDRIVER[" + tag + "] panels: " + e.Message); }

                Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
                A11yHost.Diag("TESTDRIVER[" + tag + "] buttons=" + (buttons == null ? 0 : buttons.Length));
                if (buttons != null)
                {
                    // 只打「有名字/有文字/激活」的那些，且按 (名字, 文字) 去重 ——
                    // 本作有 220 个存档槽按钮，不去重会把日志淹掉、看不到标题菜单。
                    var seen = new System.Collections.Generic.HashSet<string>();
                    int shown = 0;
                    foreach (Button b in buttons)
                    {
                        if (b == null) continue;
                        string label = LabelOf(b);
                        bool active = false;
                        string goName = "";
                        string parent = "";
                        try
                        {
                            active = b.gameObject.activeInHierarchy;
                            goName = b.gameObject.name;
                            Transform pt = b.transform.parent;
                            parent = pt == null ? "<root>" : pt.name;
                        }
                        catch { }
                        string key = goName + "§" + label;
                        if (!seen.Add(key)) continue;
                        A11yHost.Diag("TESTDRIVER[" + tag + "] button name='" + goName + "' parent='" + parent
                                     + "' label='" + label + "' active=" + active);
                        if (++shown >= 60) break;
                    }
                }
            }
            catch (Exception e)
            {
                A11yHost.Diag("TESTDRIVER[" + tag + "] dump: " + e.Message);
            }
        }

        private static string LabelOf(Button b)
        {
            try
            {
                TMPro.TMP_Text t = b.GetComponentInChildren<TMPro.TMP_Text>();
                if (t != null && t.text != null) return t.text.Replace("\n", " ");
            }
            catch { }
            try
            {
                Text u = b.GetComponentInChildren<Text>();
                if (u != null && u.text != null) return u.text.Replace("\n", " ");
            }
            catch { }
            return "";
        }

        // ==================== 开始游戏 ====================

        private static bool TryStartGame()
        {
            // 1) 首选：Naninovel 自带的「新游戏」按钮组件（re\decompiled\signatures\TitleNewGameButton.txt：
            //    Naninovel.UI.TitleNewGameButton : ScriptableButton，public override void OnButtonClick()）。
            //    它是预制体上的真实入口，不走鼠标、不看窗口焦点。
            try
            {
                Naninovel.UI.TitleNewGameButton[] ng = Resources.FindObjectsOfTypeAll<Naninovel.UI.TitleNewGameButton>();
                if (ng != null && ng.Length > 0)
                {
                    A11yHost.Diag("TESTDRIVER 找到 TitleNewGameButton x" + ng.Length);
                    foreach (Naninovel.UI.TitleNewGameButton b in ng)
                    {
                        if (b == null) continue;
                        try
                        {
                            if (!b.gameObject.activeInHierarchy) continue;
                            b.OnButtonClick();
                            A11yHost.Diag("TESTDRIVER 已调用 TitleNewGameButton.OnButtonClick");
                            return true;
                        }
                        catch (Exception e) { A11yHost.Diag("TESTDRIVER TitleNewGameButton: " + e.Message); }
                    }
                }
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER find TitleNewGameButton: " + e.Message); }

            // 2) 次选：本作标题 UI 的 _newGameButton（re\decompiled\signatures\witchtrials\TitleUi.txt：
            //    public unsafe Button _newGameButton）——直接触发它的 onClick，
            //    走游戏自己的「新游戏」绑定，而不是 StartGame()（那个是继续播放，实测报
            //    "Failed resume playback: player doesn't have played script"）。
            try
            {
                WitchTrials.Views.TitleUi[] tus = Resources.FindObjectsOfTypeAll<WitchTrials.Views.TitleUi>();
                if (tus != null && tus.Length > 0)
                {
                    A11yHost.Diag("TESTDRIVER 找到 TitleUi x" + tus.Length);
                    foreach (WitchTrials.Views.TitleUi t in tus)
                    {
                        if (t == null) continue;
                        try
                        {
                            Button nb = t._newGameButton;
                            if (nb == null) { A11yHost.Diag("TESTDRIVER TitleUi._newGameButton = null"); continue; }
                            int n = 0;
                            try { n = nb.onClick.GetPersistentEventCount(); } catch { }
                            A11yHost.Diag("TESTDRIVER _newGameButton active=" + nb.gameObject.activeInHierarchy
                                         + " listeners=" + n);
                            nb.onClick.Invoke();
                            A11yHost.Diag("TESTDRIVER 已 Invoke TitleUi._newGameButton.onClick");
                            return true;
                        }
                        catch (Exception e) { A11yHost.Diag("TESTDRIVER _newGameButton: " + e.Message); }
                    }
                }
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER find TitleUi: " + e.Message); }

            // 3) 再退一步：直接让 ScriptPlayer 播第一段剧本（不依赖标题画面流程）。
            //    这样即便没有 UI 流程，也能验证打印钩子/朗读链。
            try
            {
                Naninovel.IScriptPlayer sp0 = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>();
                if (sp0 != null)
                {
                    string[] candidates =
                    {
                        "Act01_Chapter01/Act01_Chapter01_Adv01",
                        "Act01_Chapter01_Adv01",
                        "Act01_Chapter01/Act01_Chapter01_Adv01.nani"
                    };
                    foreach (string c in candidates)
                    {
                        try
                        {
                            sp0.Play(c, 0);
                            A11yHost.Diag("TESTDRIVER ScriptPlayer.Play('" + c + "') 调用成功 playing=" + sp0.Playing);
                            return true;
                        }
                        catch (Exception e)
                        {
                            A11yHost.Diag("TESTDRIVER Play('" + c + "'): " + e.Message);
                        }
                    }
                }
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER Play fallback: " + e.Message); }

            // 3) 兜底：按按钮文字找（富文本标签先剥掉）
            try
            {
                Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
                if (buttons == null) return false;

                Button best = null;
                foreach (Button b in buttons)
                {
                    if (b == null) continue;
                    string label = System.Text.RegularExpressions.Regex.Replace(LabelOf(b), "<[^<>]*>", "");
                    if (label.Length == 0) continue;
                    foreach (string w in StartWords)
                    {
                        if (label.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            best = b;
                            A11yHost.Diag("TESTDRIVER 命中开始按钮 '" + label + "'");
                            break;
                        }
                    }
                    if (best != null) break;
                }

                if (best == null)
                {
                    A11yHost.Diag("TESTDRIVER 未找到开始按钮（TitleNewGameButton/TitleUi/文字匹配都失败）");
                    return false;
                }

                best.onClick.Invoke();
                A11yHost.Diag("TESTDRIVER 已 Invoke 开始按钮");
                return true;
            }
            catch (Exception e)
            {
                A11yHost.Diag("TESTDRIVER TryStartGame: " + e.Message);
                return false;
            }
        }

        private static void TryAutoPlay()
        {
            try
            {
                Naninovel.IScriptPlayer sp = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>();
                if (sp == null) { A11yHost.Diag("TESTDRIVER autoplay: 没有 ScriptPlayer"); return; }

                string path = "";
                try { path = sp.PlayedScript == null ? "" : sp.PlayedScript.Path; } catch { }

                // 还在标题脚本里（System/System_Title）或者根本没播 → 直接进正片脚本
                if (!sp.Playing || path.StartsWith("System"))
                {
                    if (_storyAttempts < 5)
                    {
                        _storyAttempts++;
                        A11yHost.Diag("TESTDRIVER autoplay: 当前 path='" + path + "' playing=" + sp.Playing
                                      + "，尝试进正片（第 " + _storyAttempts + " 次）");
                        TryPlayStory(sp);
                    }
                }

                sp.SetAutoPlayEnabled(true);
                A11yHost.Diag("TESTDRIVER autoplay=true playing=" + sp.Playing);
            }
            catch (Exception e)
            {
                A11yHost.Diag("TESTDRIVER autoplay: " + e.Message);
            }
        }

        /// <summary>
        /// 让 ScriptPlayer 播正片第一段。优先用游戏自己的「新游戏起始剧本名」
        /// （re\decompiled\signatures\witchtrials-models\WitchTrialsNaninovelManager.txt：
        ///  override string StartGameScenarioName，基类 NaninovelManager 同名字段）。
        /// </summary>
        private static void TryPlayStory(Naninovel.IScriptPlayer sp)
        {
            var names = new System.Collections.Generic.List<string>();

            // 诊断用：MANOSABA_A11Y_JUMP=<Naninovel 脚本路径> 时优先跳这个脚本，
            // 用来把「选项 / 辩论」这类后面章节的补丁点拉到前面验证。
            try
            {
                string jump = Environment.GetEnvironmentVariable("MANOSABA_A11Y_JUMP");
                if (!string.IsNullOrEmpty(jump))
                {
                    names.Add(jump);
                    A11yHost.Diag("TESTDRIVER 使用 MANOSABA_A11Y_JUMP='" + jump + "'");
                }
            }
            catch { }

            try
            {
                WitchTrials.Models.IWitchTrialsNaninovelManager iface =
                    Naninovel.Engine.GetService<WitchTrials.Models.IWitchTrialsNaninovelManager>();
                WitchTrials.Models.WitchTrialsNaninovelManager mgr = iface == null
                    ? null : iface.TryCast<WitchTrials.Models.WitchTrialsNaninovelManager>();
                if (mgr != null)
                {
                    string n = mgr.StartGameScenarioName;
                    A11yHost.Diag("TESTDRIVER StartGameScenarioName='" + n + "'");
                    if (!string.IsNullOrEmpty(n)) names.Add(n);
                }
                else
                {
                    A11yHost.Diag("TESTDRIVER 没拿到 WitchTrialsNaninovelManager");
                }
            }
            catch (Exception e) { A11yHost.Diag("TESTDRIVER StartGameScenarioName: " + e.Message); }

            names.Add("Act01_Chapter01/Act01_Chapter01_Adv01");
            names.Add("Act01_Chapter01_Adv01");

            foreach (string c in names)
            {
                if (string.IsNullOrEmpty(c)) continue;
                try
                {
                    // Play() 只播「已加载」脚本；未加载会抛 not loaded。LoadAndPlay 先加载再播。
                    Naninovel.ScriptPlayerExtensions.LoadAndPlay(sp, c, 0).Forget();
                    A11yHost.Diag("TESTDRIVER LoadAndPlay('" + c + "') 已发起 playing=" + sp.Playing);
                    return;
                }
                catch (Exception e) { A11yHost.Diag("TESTDRIVER LoadAndPlay('" + c + "') 失败: " + e.Message); }

                try
                {
                    sp.Play(c, 0);
                    A11yHost.Diag("TESTDRIVER Play('" + c + "') 后 playing=" + sp.Playing);
                    if (sp.Playing) return;
                }
                catch (Exception e) { A11yHost.Diag("TESTDRIVER Play('" + c + "') 失败: " + e.Message); }
            }
        }
    }
}

