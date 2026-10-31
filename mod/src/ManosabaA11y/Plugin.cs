using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace ManosabaA11y
{
    /// <summary>
    /// 插件入口（IL2CPP 路线）。
    ///
    /// ★ IL2CPP 与 Mono 的入口基类不一样，别照抄 Mono 模板：
    ///   Mono  ：<c>BaseUnityPlugin</c>（本身就是 MonoBehaviour，有 Awake/Update）
    ///   IL2CPP：<c>BepInEx.Unity.IL2CPP.BasePlugin</c>（**普通类**，没有 Awake/Update；
    ///           只有 <c>Log</c>/<c>Config</c>/<c>Load()</c>/<c>Unload()</c>/<c>AddComponent&lt;T&gt;()</c>）
    /// 所以每帧逻辑必须放进一个 MonoBehaviour（见文件末尾的 PluginBehaviour），
    /// 在 Load() 里 AddComponent 挂上去。
    /// </summary>
    [BepInPlugin(Guid, DisplayName, Version)]
    public class Plugin : BasePlugin
    {
        /// <summary>与 mod\package\BepInEx\config 下生成的配置文件名绑定，发布后不可改。</summary>
        public const string Guid = "manosaba.a11y.reader";

        /// <summary>
        /// 纯数字，四段。**不要在版本号里加字母** —— BepInPlugin 的版本参数是
        /// System.Version，`0.1.0a` 这种会直接抛异常导致插件加载失败。
        /// 必须与 csproj 的 &lt;Version&gt; 一致（build.ps1 第三道守卫会查）。
        ///
        /// 版本策略（0.0.0.1 起）：**克制**。只有「玩家能感知到的功能」或
        /// 「实机事故修复」才 +1 最后一段；文档、打包、内部重构不动版本号。
        /// </summary>
        public const string Version = "0.0.0.8";

        public const string DisplayName = "魔法少女ノ魔女裁判 A11y Reader";

        internal static ManualLogSource L;

        // ==================== 朗读 ====================
        internal static ConfigEntry<bool> CfgReadUnvoiced;
        /// <summary>有配音的行是否还报一次说话人名（文本交给游戏语音念）。</summary>
        internal static ConfigEntry<bool> CfgAnnounceOnVoiced;
        internal static ConfigEntry<bool> CfgSpeakWhenUnknown;
        internal static ConfigEntry<bool> CfgSpeakName;
        internal static ConfigEntry<bool> CfgReadChoices;
        internal static ConfigEntry<bool> CfgChoiceHotkeys;
        internal static ConfigEntry<float> CfgRealTimeSeconds;
        internal static ConfigEntry<string> CfgSilenceKey;
        internal static ConfigEntry<string> CfgRepeatKey;
        internal static ConfigEntry<string> CfgSpeechBackend;
        internal static ConfigEntry<string> CfgTextLocale;
        internal static ConfigEntry<bool> CfgReadPhone;
        internal static ConfigEntry<bool> CfgReadPhoneSticker;
        internal static ConfigEntry<bool> CfgAnnounceOnSkipStop;
        internal static ConfigEntry<bool> CfgAnnounceLoading;

        // ==================== 界面导航 ====================
        internal static ConfigEntry<bool> CfgMenuNav;
        /// <summary>导航开关键（组合键写法）。默认 Alt+Shift+F1 —— 因为本作标题画面会吃掉 Tab。</summary>
        internal static ConfigEntry<string> CfgNavKey;
        /// <summary>除组合键外是否再接受 Tab 当导航开关（Boot/加载阶段有效，标题画面可能被吃掉）。</summary>
        internal static ConfigEntry<bool> CfgNavKeyAllowTab;
        /// <summary>运行中切换语音后端的按键（默认 F7）。</summary>
        internal static ConfigEntry<string> CfgCycleBackendKey;
        internal static ConfigEntry<bool> CfgSortByPosition;
        internal static ConfigEntry<bool> CfgVisibleOnly;
        internal static ConfigEntry<bool> CfgQuitConfirm;
        internal static ConfigEntry<string> CfgQuitNames;
        internal static ConfigEntry<string> CfgExcludeNames;

        // ==================== 其它 ====================
        internal static ConfigEntry<bool> CfgStartupHint;
        internal static ConfigEntry<bool> CfgDiagLog;
        /// <summary>隐藏 BepInEx 的控制台窗口（默认开）。</summary>
        internal static ConfigEntry<bool> CfgHideConsole;
        /// <summary>
        /// 允许激活游戏内置开发者面板（Ctrl+Alt+Shift+F3）。
        /// ★ 调试功能，会扩大可达范围：正式发布默认关闭，只有玩家自己打开才响应组合键。
        /// </summary>
        internal static ConfigEntry<bool> CfgAllowDevPanel;

        private Harmony _harmony;

        /// <summary>
        /// 实机测试模式：只有环境变量 MANOSABA_A11Y_TEST=1 时打开。
        /// 打开后强制诊断日志 + 启用 TestDriver（用引擎内部入口点开始游戏、开自动播放，
        /// 绕开鼠标/窗口焦点问题）。生产用户不会设置该变量，发布版行为不变。
        /// </summary>
        internal static bool TestMode;
        /// <summary>
        /// 导航验收模式：只有环境变量 MANOSABA_A11Y_TEST_NAV=1 时打开。
        /// 由插件自己合成「按了一次 Tab」，用来验证导航朗读链路 ——
        /// 自动化点击/按键在本作 6 FPS + 全屏下会被吞（见 docs\framework-notes.md §9），
        /// 但 Tab 这条钩子是逐作层自己的（A11yHost.NavTogglePressed），
        /// 合成它不会碰游戏逻辑，只影响本进程的测试。
        /// </summary>
        internal static bool TestNavMode;
        /// <summary>
        /// 选项验收模式：只有环境变量 MANOSABA_A11Y_TEST_CHOICE=1 时打开。
        /// 本机键盘注入被系统挡掉（SendInput 返回 0，见 docs\framework-notes.md §17），
        /// 所以改由插件在选项播报后自己走一次「选中第 1 项」的代码路径，
        /// 验证「编号 → 按钮 → onClick」这段。生产用户不会设置该变量。
        /// </summary>
        internal static bool TestChoiceOnce;
        /// <summary>
        /// 地图地点选择验收：只有 MANOSABA_A11Y_TEST_MAPCHOICE=1 时打开。
        /// 由 MapChoice 在地图页可见时把前 5 个地点「置为可点」（游戏自己的
        /// ChangeFocusButtonActivity），复现剧情要求选地点的状态，再自动选第 1 个。
        /// </summary>
        internal static bool TestMapChoiceOnce;
        /// <summary>MANOSABA_A11Y_TEST_MAPFLOOR=1：播报后自动切一次楼层，验证切换后判据变准。</summary>
        internal static int TestMapFloorCount;
        /// <summary>MANOSABA_A11Y_TEST_MAPPICK_KIND：按 AreaKind 直接点某个地点（不受楼层过滤）。</summary>
        internal static string TestMapPickKind;
        /// <summary>TEST_MAPCHOICE 用：自动选第几个地点（1 起）。</summary>
        internal static int TestMapChoicePick = 1;
        /// <summary>地图页扫描诊断（MANOSABA_A11Y_MAPDIAG=1）：打印每个 MapArea 的可点状态。</summary>
        internal static bool CfgMapDiag;
        private static float _nextSyntheticNavAt;
        private static int _syntheticNavCount;

        /// <summary>TestNavMode 下的「Tab 是否按下」：每 25 秒合成一次。</summary>
        private static bool SyntheticNavPressed()
        {
            float now = Time.realtimeSinceStartup;
            if (now < _nextSyntheticNavAt) return false;
            _nextSyntheticNavAt = now + 25f;
            _syntheticNavCount++;
            A11yHost.Diag("TEST NAV: 合成 Tab 第 " + _syntheticNavCount + " 次 t=" + now.ToString("F1"));
            return true;
        }

        /// <summary>
        /// 「这一帧要不要切换导航模式」（平台层唯一的开关键入口）。
        ///
        /// 为什么默认不再只有 Tab（2026-10-29 用户实机反馈）：
        ///   「主界面还没加载出来时按 Tab 有反应，主界面加载后按 Tab 就没反应了。」
        /// 也就是标题画面出来后，游戏自己把 Tab 当成菜单键吃掉了。
        ///
        /// 处理：主键改成游戏不会用的组合键（默认 Alt+Shift+F1），
        /// Tab 保留为备用键（可由「同时允许 Tab」关掉）。
        /// 组合键解析走平台层 Hotkey（自带边沿闩锁与 try/catch）。
        /// </summary>
        private static bool NavKeyPressed()
        {
            string spec = CfgNavKey == null ? "" : (CfgNavKey.Value ?? "");
            bool chord = false;
            try { chord = Hotkey.Down(spec); } catch { chord = false; }
            if (!chord) chord = SpecDown(spec);
            if (chord)
            {
                A11yHost.Diag("导航开关键按下: " + spec);
                return true;
            }

            if (CfgNavKeyAllowTab != null && CfgNavKeyAllowTab.Value)
            {
                if (KeyEdge.Pressed(KeyCode.Tab) || InputSysKeys.Pressed(KeyCode.Tab))
                {
                    A11yHost.Diag("导航开关键按下: Tab");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 配置里的按键是否在这一帧刚被按下：平台层 <see cref="Hotkey"/>（旧 Input + Win32 兜底）
        /// 或新 Input System（<see cref="InputSysKeys"/>）任一路命中即可。
        ///
        /// 0.0.0.5 实测：自动化用 keybd_event 注入的键**进不了** GetAsyncKeyState
        /// （同进程自测 sawKey=False），而真实玩家在新 Input System 里的按键一定能读到。
        /// 两路并集既不影响真实玩家，也让「退格重读」这类功能键在两种环境都能验证。
        /// </summary>
        internal static bool SpecDown(string spec)
        {
            if (Hotkey.Down(spec)) return true;

            try
            {
                Hotkey.Spec s;
                string why;
                if (!Hotkey.Parse(spec, out s, out why)) return false;
                return InputSysKeys.Down(s.Key, s.Alt, s.Ctrl, s.Shift);
            }
            catch
            {
                return false;
            }
        }

        public override void Load()
        {
            L = Log;
            A11yHost.Log = Log;

            BindConfig();
            HideConsoleWindow();

            // ---------- 填契约 ----------
            A11yHost.CfgSpeechBackend = CfgSpeechBackend;
            A11yHost.CfgDiagLog = CfgDiagLog;
            A11yHost.CfgVisibleOnly = CfgVisibleOnly;
            A11yHost.CfgSortByPosition = CfgSortByPosition;
            A11yHost.CfgExcludeNames = CfgExcludeNames;
            A11yHost.CfgQuitConfirm = CfgQuitConfirm;
            A11yHost.CfgQuitNames = CfgQuitNames;

            A11yHost.BlockGameAdvance = () => UiNav.BlockGameAdvance;
            A11yHost.ShouldMuteUnitySubmit = () => UiNav.ShouldMuteUnitySubmit;

            // 逐作标签钩子：本作大量控件子树里一个字都没有（run31 转储 label=''），
            // 靠对象名/路径名补中文（依据见 UiNav.Game.cs 顶部注释）。
            A11yHost.GameLabelOf = UiNav.GameLabelOf;
            A11yHost.GameGroupName = UiNav.GameGroupName;
            A11yHost.GameTextIsNoise = UiNav.GameTextIsNoise;

            // 每帧一次：界面导航 + 选项面板。
            // 逐作逻辑一律 try/catch 隔离，一个子系统的异常不能让整帧瘫痪。
            A11yHost.TickUiNav = () =>
            {
                if (CfgMenuNav == null || CfgMenuNav.Value)
                {
                    try { UiNav.Update(); } catch (Exception e) { A11yHost.Diag("UiNav: " + e.Message); }
                }
                try { Choices.Update(); } catch (Exception e) { A11yHost.Diag("Choices: " + e.Message); }
            };

            // 键盘读取兜底 —— 本作**必须开**，理由（实机取证 probe\logs\run31-ui-probe.txt）：
            //   UIPROBE legacyInput=FAIL
            //   System.InvalidOperationException: You are trying to read Input using the
            //   UnityEngine.Input class, but you have switched active Input handling to
            //   Input System package in Player Settings.
            // Player Settings 只启用了新 Input System，旧 UnityEngine.Input 整体不可用；
            // 平台层的 Tab 导航 / 方向键 / 回车 / 数字键都经 KeyEdge，KeyEdge 在旧 API
            // 抛异常时退回 GlobalKeys（Win32 GetAsyncKeyState）—— 前提是这里打开开关。
            // 不开的表现：所有键盘热键静默失效（每次读取都被当作「没按」）。
            A11yHost.UseGlobalKeys = true;

            // 强制触发平台层的懒同步（GlobalKeys.SyncWithHostSwitch），否则
            // Hotkey.GlobalHeld 为 null → Hotkey.Down 的「全局读取」整段被跳过，
            // 功能键（Backspace / 0 / Alt+Shift+F1）在旧 Input API 不可用的本作里
            // 会静默失效。0.0.0.5 实测：不预热时 run64 全程 `重读=0`。
            try
            {
                bool globalReading = GlobalKeys.Reading;
                L.LogInfo("[a11y] 全局键: reading=" + globalReading
                          + " available=" + GlobalKeys.Available
                          + " backspaceHeld=" + GlobalKeys.Held(KeyCode.Backspace));
            }
            catch (Exception e)
            {
                L.LogWarning("[a11y] 全局键预热失败: " + e.Message);
            }
            // 选项验收：播报后由插件自己选一次第 1 项（仅 MANOSABA_A11Y_TEST_CHOICE=1）
            if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_CHOICE") == "1")
            {
                TestChoiceOnce = true;
                L.LogInfo("[a11y] TEST CHOICE MODE: 选项播报后自动选择第 1 项");
            }
            // 地图地点选择验收（仅 MANOSABA_A11Y_TEST_MAPCHOICE=N，N=要选第几个地点）。
            // 本机键盘注入被系统挡掉（见 framework-notes 键盘一节），所以用代码路径验收
            // 「编号 → 按钮 → onClick → 游戏分支」整段；正式版没有这个环境变量。
            int mapPick;
            if (int.TryParse(Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_MAPCHOICE"), out mapPick) && mapPick > 0)
            {
                TestMapChoiceOnce = true;
                TestMapChoicePick = mapPick;
                L.LogInfo("[a11y] TEST MAPCHOICE MODE: 播报后自动选择第 " + mapPick + " 个地点");
            }
            TestMapPickKind = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_MAPPICK_KIND");
            // 地图扫描诊断（仅 MANOSABA_A11Y_MAPDIAG=1）
            CfgMapDiag = Environment.GetEnvironmentVariable("MANOSABA_A11Y_MAPDIAG") == "1";
            // 楼层切换验收：播报后自动切一次楼层（仅 MANOSABA_A11Y_TEST_MAPFLOOR=1）
            int mapFloor;
            if (int.TryParse(Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_MAPFLOOR"), out mapFloor) && mapFloor > 0)
            {
                TestMapFloorCount = mapFloor;
                L.LogInfo("[a11y] TEST MAPFLOOR MODE: 播报后自动切换 " + mapFloor + " 次楼层");
            }
            // 导航验收：合成 Tab（仅 MANOSABA_A11Y_TEST_NAV=1）
            if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_NAV") == "1")
            {
                TestNavMode = true;
                _nextSyntheticNavAt = Time.realtimeSinceStartup + 45f;
                A11yHost.NavTogglePressed = SyntheticNavPressed;
                L.LogInfo("[a11y] TEST NAV MODE: 45s 后每 25s 合成一次 Tab");
            }
            else
            {
                // 默认：可配置组合键（Alt+Shift+F1）+ 可选 Tab 备用键。
                A11yHost.NavTogglePressed = NavKeyPressed;
            }

            A11yHost.Validate();

            // ---------- 每帧驱动 ----------
            // BasePlugin 不是 MonoBehaviour，没有 Update，必须挂一个组件。
            try
            {
                AddComponent<PluginBehaviour>();
                L.LogInfo("[a11y] behaviour added");
            }
            catch (Exception e)
            {
                L.LogError("[a11y] AddComponent failed: " + e);
            }

            // ---------- 补丁 ----------
            try
            {
                _harmony = new Harmony(Guid);
                // 逐个显式挂：Harmony 的 PatchAll(Type) 不保证会扫嵌套类，漏挂时不会报错
                // （实测：PatchAll(typeof(Patches)) 后 Print 钩子完全不触发，见 CHANGELOG）。
                //
                // MANOSABA_A11Y_NOPATCH=print,reveal,choice,probe 只为**定位问题**而存在：
                // 2026-10-29 实机发现「打了补丁 → 剧情停在第一句，纯净模式正常推进」，
                // 需要逐组关掉补丁做二分，找出是哪条补丁破坏了 Naninovel 的 async 状态机。
                // 正常玩家不该设置这个变量。
                string noPatch = Environment.GetEnvironmentVariable("MANOSABA_A11Y_NOPATCH") ?? "";
                bool skipPanel = noPatch.IndexOf("panel", StringComparison.OrdinalIgnoreCase) >= 0;
                bool skipReveal = noPatch.IndexOf("reveal", StringComparison.OrdinalIgnoreCase) >= 0;
                bool skipChoice = noPatch.IndexOf("choice", StringComparison.OrdinalIgnoreCase) >= 0;
                bool skipDebate = noPatch.IndexOf("debate", StringComparison.OrdinalIgnoreCase) >= 0;
                bool skipAuthor = noPatch.IndexOf("author", StringComparison.OrdinalIgnoreCase) >= 0;
                L.LogInfo("[a11y] NOPATCH=" + (noPatch.Length == 0 ? "<none>" : noPatch));

                // ★ 这里**没有** TextPrinterManager.Print 的补丁：它返回 UniTask（非 blittable 结构体），
                //   Harmony detour 会搬坏返回值，让 PrintText.Print 的 await 抛
                //   MethodAccessException 并把剧情永久卡死（2026-10-29 实机 + 二分证据）。
                //   文本主钩子是 RevealableText.set_Text，见 Patches.Patch_RevealText 注释。
                if (!skipPanel) _harmony.PatchAll(typeof(Patches.Patch_PanelAddMessage));
                // printer 级 AddMessage / SetMessages：本作 ADV 真入口（面板级一条都不响），
                // 也是「行 id」的唯一来源 → 配音闸门依赖它。返回值 void，detour 安全。
                _harmony.PatchAll(typeof(Patches.Patch_PrinterAddMessage));
                _harmony.PatchAll(typeof(Patches.Patch_PrinterSetMessages));
                if (!skipChoice) _harmony.PatchAll(typeof(Patches.Patch_AddChoiceButton));
                if (!skipReveal) _harmony.PatchAll(typeof(Patches.Patch_RevealText));
                if (!skipAuthor) { Patches.ApplyAdvAuthor(_harmony); Patches.ApplyAdvAuthorName(_harmony); }
                // 辩论面板的 SetText/AddText 用 AccessTools 手工挂（逐个 try/catch，
                // 单条失败不让整个补丁链失效）。依据见 Patches.ApplyManual 注释。
                if (!skipDebate) Patches.ApplyManual(_harmony);
                // 加载界面可见性（Giga LoadingUiExtended）——事件驱动，补轮询查不到的短板。
                Patches.ApplyLoadingUi(_harmony);

                // 把已挂补丁数打进日志：漏挂是静默故障，必须能一眼看出来。
                int patched = 0;
                try { foreach (MethodBase _ in _harmony.GetPatchedMethods()) patched++; } catch { patched = -1; }
                L.LogInfo("[a11y] harmony patched, patchedMethods=" + patched);
            }
            catch (Exception e)
            {
                L.LogError("[a11y] harmony patch failed: " + e.Message);
            }

            L.LogInfo("[a11y] loaded v" + Version + " (IL2CPP, BepInEx 6)");
            // 开发者面板开关的配置值要打出来：BepInEx 的 cfg 里如果缺项/拼错，症状是
            // 「按了组合键完全没反应」，正是 0.0.0.5 功能键失效时的排查成本。
            try
            {
                L.LogInfo("[a11y] DevPanel 配置: allow=" + (CfgAllowDevPanel != null && CfgAllowDevPanel.Value)
                          + " isys=" + InputSysKeys.Available);
            }
            catch (Exception e) { L.LogWarning("[a11y] DevPanel 配置读取失败: " + e.Message); }


            // 功能键配置在启动时**解析一次并写日志**：Hotkey.Down 对认不出的键名静默返回 false，
            // 症状是「按了没反应、日志什么都没有」——正是 0.0.0.3 第一次实机退格失效的排查成本。
            try
            {
                Hotkey.Spec sp; string why;
                if (Hotkey.Parse(CfgRepeatKey.Value, out sp, out why))
                    L.LogInfo("[a11y] 重读按键=" + CfgRepeatKey.Value + " -> KeyCode." + sp.Key);
                else
                    L.LogWarning("[a11y] 重读按键认不出来: " + why);

                if (Hotkey.Parse(CfgSilenceKey.Value, out sp, out why))
                    L.LogInfo("[a11y] 沉默按键=" + CfgSilenceKey.Value + " -> KeyCode." + sp.Key);
                else
                    L.LogWarning("[a11y] 沉默按键认不出来: " + why);

                if (Hotkey.Parse(CfgCycleBackendKey.Value, out sp, out why))
                    L.LogInfo("[a11y] 切换语音后端按键=" + CfgCycleBackendKey.Value + " -> KeyCode." + sp.Key);
                else
                    L.LogWarning("[a11y] 切换语音后端按键认不出来: " + why);
            }
            catch (Exception e) { L.LogWarning("[a11y] 功能键解析: " + e.Message); }
        }

        public override bool Unload()
        {
            try { Speech.Shutdown(); } catch { }
            try { if (_harmony != null) _harmony.UnpatchSelf(); } catch { }
            try { A11yHost.Clear(); } catch { }
            return true;
        }

        // ====================================================================
        // 控制台窗口隐藏
        //
        // 本作是 Unity，BepInEx 会在玩家机器上开一个控制台黑窗口。
        // 那个窗口会抢焦点、在切场景时闪一下，读屏（NVDA/争渡）还会把它的内容
        // 当成活动窗口念 —— 对读屏用户是纯噪声，所以加载后直接隐藏。
        //
        // ★ 运行时隐藏而不是改 `BepInEx.cfg`：不该动别人的配置文件，
        //   而且玩家可能本来就装了 BepInEx（cfg 里有他自己的设置）。
        //   参考实现《不存在》Plugin.cs:348-378 是同一做法。
        // ====================================================================
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private const int SwHide = 0;

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        private static void HideConsoleWindow()
        {
            try
            {
                if (CfgHideConsole == null || !CfgHideConsole.Value) return;
                IntPtr h = GetConsoleWindow();
                if (h == IntPtr.Zero)
                {
                    A11yHost.Diag("隐藏控制台: 没有控制台窗口（可能未分配/已关闭）");
                    return;
                }

                // ★ 安全闸门：只隐藏真正的控制台窗口（窗口类必须是 ConsoleWindowClass）。
                //   第一版直接 ShowWindow(SW_HIDE) 打到了游戏主窗口上（run55 实机：
                //   日志里的 hwnd 与游戏主窗口句柄相同 → 后面的点击全部点空），
                //   所以这里必须先验类名/进程号，宁可不隐藏也不能把游戏窗口藏了。
                var cls = new StringBuilder(64);
                GetClassName(h, cls, cls.Capacity);
                var title = new StringBuilder(128);
                GetWindowText(h, title, title.Capacity);
                uint pid;
                GetWindowThreadProcessId(h, out pid);

                string className = cls.ToString();
                bool sameProcess = pid == GetCurrentProcessId();
                L.LogInfo("[a11y] 控制台候选 hwnd=" + h + " class=[" + className + "] title=[" + title + "] pid=" + pid + " 本进程=" + sameProcess);

                if (!string.Equals(className, "ConsoleWindowClass", StringComparison.OrdinalIgnoreCase))
                {
                    L.LogInfo("[a11y] 跳过隐藏：窗口类不是 ConsoleWindowClass（不是控制台窗口）");
                    return;
                }

                bool ok = ShowWindow(h, SwHide);
                L.LogInfo("[a11y] 隐藏控制台窗口 hwnd=" + h + " ok=" + ok);
            }
            catch (Exception e)
            {
                A11yHost.Diag("隐藏控制台失败: " + e.Message);
            }
        }

        // ====================================================================
        // 配置：分组名固定三组「朗读」「界面导航」「其它」
        // （流水线 §P2；换名字会让老用户的 cfg 失效）
        // ====================================================================
        private void BindConfig()
        {
            CfgReadUnvoiced = Config.Bind("朗读", "朗读无配音剧情", true,
                "有语音的台词由游戏自己播，再念一遍会重叠；没配音的行必须念。");
            CfgAnnounceOnVoiced = Config.Bind("朗读", "有配音时也报说话人", false,
                "默认关闭：有配音的行完全安静，只留游戏语音（玩家要文本按 Backspace 重读）。\n" +
                "打开 = 有配音的行仍念一遍说话人名字。");
            CfgSpeakWhenUnknown = Config.Bind("朗读", "无法判定时也朗读", true,
                "拿不到「这行有没有配音」的判断时，宁可多念一遍也不要漏。");
            CfgSpeakName = Config.Bind("朗读", "朗读时带上说话人", true,
                "念成「爱丽丝：……」。");
            CfgReadChoices = Config.Bind("朗读", "朗读选项", true,
                "选项面板出现时把每条选项念出来。");
            CfgChoiceHotkeys = Config.Bind("朗读", "数字键选择选项", true,
                "按 1-9 直接选对应选项。");
            CfgRealTimeSeconds = Config.Bind("朗读", "限时选择时长", 20f,
                "限时选项的倒计时。读屏用户需要在朗读完之后留出决策时间，所以远长于默认值。");
            CfgSilenceKey = Config.Bind("朗读", "沉默按键", "0",
                "按一下立刻停止朗读（不改变游戏状态）。");
            CfgRepeatKey = Config.Bind("朗读", "重读按键", "Backspace",
                "重读上一句剧情/选项。本作只启用新 Input System，键名照 Unity KeyCode 写。");
            CfgSpeechBackend = Config.Bind("朗读", "语音后端", "自动",
                "自动 / NVDA / SAPI / 争渡（具体取值见 Speech.CycleAvailable 支持的列表）。");
            CfgReadPhone = Config.Bind("朗读", "朗读手机消息", true,
                "手机界面里的消息文本。逐作 P3 接上后生效。");
            CfgReadPhoneSticker = Config.Bind("朗读", "朗读手机表情", true,
                "表情/贴纸的描述文字。逐作 P3 接上后生效。");
            CfgAnnounceOnSkipStop = Config.Bind("朗读", "快进停止时补念当前句", true,
                "快进时台词被跳过，停下时补念当前这一句。");
            CfgAnnounceLoading = Config.Bind("朗读", "播报加载中", true,
                "加载时念一句「正在加载。」——读屏玩家在加载期没有任何线索，容易以为卡死。\n" +
                "本作实测有明显的黑屏加载段（点新游戏后），所以默认开。");

            CfgMenuNav = Config.Bind("界面导航", "菜单键盘导航", true,
                "进入界面导航后，用方向键在可交互控件间移动并朗读，回车激活。");
            CfgNavKey = Config.Bind("界面导航", "导航开关键", "Alt+Shift+F1",
                "进入/退出界面导航的快捷键（组合键写法，例如 Ctrl+Alt+G）。\n" +
                "★ 默认不是 Tab：本作标题画面会把 Tab 吃掉（主界面出现前按 Tab 有反应、\n" +
                "  出现后没反应），所以主键改成组合键。改坏了也能用下面的「同时允许 Tab」救回来。");
            CfgNavKeyAllowTab = Config.Bind("界面导航", "同时允许 Tab", true,
                "除上面的组合键外，再接受 Tab 当导航开关（Boot/加载阶段有效，标题画面可能被游戏吃掉）。");
            CfgCycleBackendKey = Config.Bind("朗读", "切换语音后端按键", "F7",
                "按一下切到下一个可用的语音后端，并念出新的后端名。\n" +
                "★ 同时装了 NVDA 和争渡的机器，「语音后端 = 自动」会按平台顺序挑中 NVDA；\n" +
                "  如果你平时用争渡却听不到声音，在游戏里按这个键换到争渡即可。");
            CfgSortByPosition = Config.Bind("界面导航", "按屏幕位置排序控件", true,
                "按屏幕坐标（上→下、左→右）排，而不是 Hierarchy 顺序。");
            CfgVisibleOnly = Config.Bind("界面导航", "只导航看得见的控件", true,
                "隐藏/透明/在屏幕外的控件不念。");
            CfgQuitConfirm = Config.Bind("界面导航", "退出前二次确认", true,
                "「退出游戏」只按一下就关掉整个会话，读屏用户分不清它和旁边的按钮，所以补一道确认。");
            CfgQuitNames = Config.Bind("界面导航", "退出确认对象名", "",
                "逗号分隔的 GameObject 名；只有精确命中的按钮才要二次确认。\n" +
                "★ 本作标题菜单与游戏内菜单的「退出」按钮对象名尚未逐个取证（标题菜单是自定义 UI，\n" +
                "  不是 UnityEngine.UI.Button），所以默认留空 = 不启用二次确认。\n" +
                "  取证方法：开「界面诊断日志」进一次标题/菜单，从 [UiNav] 行里抄对象名。");
            CfgExcludeNames = Config.Bind("界面导航", "排除的对象名", "",
                "逗号分隔的 GameObject 名，命中就不纳入导航。\n" +
                "★ 本作存档槽（220 个 Button 全是 <cspace=0.12em>NO DATA）这类控件可以用它排除，\n" +
                "  名字从 [UiNav] 诊断日志实查。");

            CfgStartupHint = Config.Bind("其它", "启动时播报", true,
                "进游戏时念一句「无障碍补丁已加载」。");
            CfgHideConsole = Config.Bind("其它", "隐藏控制台窗口", true,
                "加载后自动隐藏 BepInEx 的控制台窗口（黑窗口会抢前台/闪屏，读屏也读不到它）。\n" +
                "★ 只隐藏窗口，不改 BepInEx.cfg；需要看日志时把它关掉重启即可。");
            CfgTextLocale = Config.Bind("其它", "文本语言", "跟随游戏",
                "跟随游戏 / 简体中文 / 日本語。\n" +
                "★ 游戏本体的语言下拉框在 Options → Message 页（「言語 / Language」），读屏用户摸不到，\n" +
                "  所以在这里直接指定要朗读哪种语言的文本。实机默认是「日本語」。");
            CfgDiagLog = Config.Bind("其它", "界面诊断日志", false,
                "把导航/文本提取的诊断信息写进 BepInEx 日志，只在排查时打开。");
            CfgAllowDevPanel = Config.Bind("其它", "允许激活开发者选项", false,
                "开启后按 Ctrl+Alt+Shift+F3 可以显示/收起游戏内置的开发者面板\n" +
                "（暂停、慢放、快放、回调试首页、解锁全部 CG、强制时间到等）。\n" +
                "★ 这是**测试功能**，会扩大可达范围、可能剧透或破坏存档体验：\n" +
                "  正式游玩请保持关闭（默认关闭）；测试需要时再打开。\n" +
                "★ 面板来自游戏自带的 GigaCreation 调试工具，不是本补丁伪造的。\n" +
                "★ 实测（v0.0.0.6）：正式发行版里游戏自己**没有**创建调试管理器\n" +
                "  （DebugOptionsBase.s_debugManager 为 null），调试面板只是内存里的\n" +
                "  预制体（从未被实例化），所以这个开关在发行版里按了也不会有面板；\n" +
                "  开发构建（Editor/Dev Build）才有完整面板。保留开关是为了开发构建可用。");

            // 无人值守实机测试开关：只有环境变量 MANOSABA_A11Y_TEST=1 时强制打开诊断日志，
            // 让自动化脚本能从 LogOutput.log 里核对「哪一句被念了 / 哪条补丁没接上」。
            try
            {
                if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST") == "1")
                {
                    TestMode = true;
                    CfgDiagLog.Value = true;
                    L.LogInfo("[a11y] TEST MODE: diag on + TestDriver on");
                }
                // 只开诊断日志、不要 TestDriver 介入（实机点鼠标走玩家路径时用这个）
                else if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_DIAG") == "1")
                {
                    CfgDiagLog.Value = true;
                    L.LogInfo("[a11y] DIAG MODE: diag on, TestDriver off");
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 每帧驱动（IL2CPP 路线：BasePlugin 没有 Update，必须自己挂 MonoBehaviour）。
    ///
    /// ★ 不在这里直接调 Speech：唯一朗读出口是 Repeat（流水线硬约束）。
    /// ★ Speech.Init 放在第一帧而不是 Load()：初始化要加载 NVDA 客户端/COM，
    ///   放在 Load() 里失败会把整条插件加载链拖死（参考实现同样是首帧再 Init）。
    /// </summary>
    public class PluginBehaviour : MonoBehaviour
    {
        private bool _speechInited;
        private int _ticks;

        public void Update()
        {
            _ticks++;

            // 地图页数字键选择要靠 InputSystem 事件队列注一次真实点击（run117/run118：
            // 合成指针事件游戏不认），所以 Tick 必须每帧都走，不能只在测试注入模式下开。
            try { InputInject.Tick(); } catch { }

            if (!_speechInited)
            {
                _speechInited = true;
                try
                {
                    Speech.Init(Plugin.L);
                    Plugin.L.LogInfo("[a11y] 语音后端: " + Speech.BackendName);
                }
                catch (Exception e)
                {
                    Plugin.L.LogError("[a11y] Speech.Init: " + e.Message);
                }

                if (Plugin.CfgStartupHint != null && Plugin.CfgStartupHint.Value)
                {
                    // 念出后端名：用户「听不到声音」时，第一句就能定位是不是后端挑错了
                    // （同时装 NVDA 与争渡的机器，自动档会挑 NVDA）。
                    try { Repeat.Say("魔法少女ノ魔女裁判 无障碍补丁已加载。语音后端 " + Speech.BackendName); } catch { }
                }
            }

            // 自动化测试：纯逻辑单元自测（MANOSABA_A11Y_SELFTEST=1）。
            // 只在启动时跑一次，把 PASS/FAIL 写进日志，供 tests\Suite.ps1 断言。
            try { SelfTest.Tick(); } catch (Exception e) { A11yHost.Diag("SelfTest: " + e.Message); }

            // 键盘链路诊断（仅 MANOSABA_A11Y_KEYDIAG=1）：每 0.5 秒把「平台层到底能不能读到
            // 方向键/回车」打出来。为什么需要：本作只启用新 Input System，裸 Input 恒抛异常，
            // 所有键盘读取都靠 GlobalKeys（GetAsyncKeyState）兜底；一旦兜底失效，
            // 表现是「导航能进但方向键没反应」这种非常难猜的静默故障。
            try { KeyDiagTick(); } catch (Exception e) { A11yHost.Diag("KeyDiag: " + e.Message); }

            // 运行中切换语音后端（默认 F7）。实机反馈：机器上同时跑 NVDA 和争渡，
            // 自动档挑了 NVDA，玩家实际用争渡 → 一句都听不到。必须给一个游戏内逃生键，
            // 不能要求先去改 cfg 再重启。
            try
            {
                if (Plugin.CfgCycleBackendKey != null && Hotkey.Down(Plugin.CfgCycleBackendKey.Value))
                {
                    string msg = Speech.CycleAvailable();
                    if (!string.IsNullOrEmpty(msg)) Repeat.Say(msg, true);
                    Plugin.L.LogInfo("[a11y] 语音后端切换 -> " + Speech.BackendName);
                }
            }
            catch (Exception e) { A11yHost.Diag("CycleBackend: " + e.Message); }

            // 重读 / 沉默（默认 Backspace / 0）。
            // ★ 为什么必须自己读键：本作只启用新 Input System，裸 UnityEngine.Input 恒抛
            //   InvalidOperationException，所以走 SpecDown（Hotkey 的 Win32 兜底 + 新 Input System）。
            //   平台层只提供 Repeat 缓冲，不会自己去读这两个键。
            try { HotkeyTick(); }
            catch (Exception e) { A11yHost.Diag("Hotkey: " + e.Message); }

            // 测试专用：注入一次键盘（仅 MANOSABA_A11Y_TEST_KEYS=1）。
            // 目的：验证「新 Input System 读取功能键」这条链路（真实玩家按键一定在这里）。
            try { TestKeyInjectTick(); }
            catch (Exception e) { A11yHost.Diag("TestKeyInject: " + e.Message); }

            // 开发者面板开关（Ctrl+Alt+Shift+F3；配置「允许激活开发者选项」默认关闭）
            try { DevPanel.Update(); }
            catch (Exception e) { A11yHost.Diag("DevPanel: " + e.Message); }

            try { if (A11yHost.TickUiNav != null) A11yHost.TickUiNav(); }
            catch (Exception e) { A11yHost.Diag("Tick: " + e.Message); }

            // 剧情文本朗读队列（消息级钩子只登记，这里到点统一念，避免一句念两遍）
            try { Reader.Tick(); }
            catch (Exception e) { A11yHost.Diag("Reader.Tick: " + e.Message); }

            // 加载播报（本作点「新游戏」后有明显的黑屏加载段）
            try { LoadingWatch.Update(); }
            catch (Exception e) { A11yHost.Diag("LoadingWatch: " + e.Message); }

            // 整页界面辅助（魔女手册 / 对话记录）：平台导航扫不到关闭按钮与地图地点，
            // 这里自动播报页面内容 + Esc 直接调 CloseButton（详见 ScreenAssist.cs 注释）。
            try { ScreenAssist.Update(); }
            catch (Exception e) { A11yHost.Diag("ScreenAssist: " + e.Message); }

            // 界面取证探针（仅 MANOSABA_A11Y_PROBE_UI=1）：转储控件对象名，供别名表取证
            try { UiProbe.Update(); }
            catch (Exception e) { A11yHost.Diag("UiProbe: " + e.Message); }

            // 实机测试驱动器（仅 MANOSABA_A11Y_TEST=1）：点开始按钮 + 开自动播放
            if (Plugin.TestMode)
            {
                try { TestDriver.Update(); }
                catch (Exception e) { A11yHost.Diag("TestDriver: " + e.Message); }
            }

            // 剧本跳转（仅诊断用 MANOSABA_A11Y_JUMP=<脚本路径>）：直接加载指定剧本，
            // 用来快速验证「带说话人的对白 / 辩论 / 选项」这些前面几十句旁白覆盖不到的分支。
            // 正常玩家不会设置这个变量；它跳过游戏流程，所以**不能**当验收路径（只用来测朗读链）。
            try { JumpTick(); } catch (Exception e) { A11yHost.Diag("JumpTick: " + e.Message); }

            // 文本语言（配置里指定，读屏用户摸不到游戏内语言下拉框）
            try { Locale.Update(); } catch (Exception e) { A11yHost.Diag("Locale.Update: " + e.Message); }
        }

        // ==================== 重读 / 沉默 ====================
        //
        // 玩家要求（2026-10-29）：退格重读上一句，**只包含剧情文字和选项**。
        // 为什么不能直接用平台层的 Repeat.Again()：界面导航的播报（「系统设置，按钮。2 / 5」）
        // 也走 Repeat.Say，直接 Again 会重读那条导航文本，不是玩家要的「上一句剧情」。
        // 所以 Reader 自己维护一条「剧情专用」的最后一句（含选项播报），这里只重读它。
        private static void HotkeyTick()
        {
            string repeatSpec = Plugin.CfgRepeatKey == null ? "" : (Plugin.CfgRepeatKey.Value ?? "");
            if (repeatSpec.Length > 0 && SpecDownBeh(repeatSpec))
            {
                string last = Reader.LastLine;
                if (string.IsNullOrEmpty(last))
                {
                    Repeat.Say("还没有朗读过内容。", true);
                }
                else
                {
                    A11yHost.Diag("重读: " + last);
                    Repeat.Say(last, true);
                }
            }

            string silenceSpec = Plugin.CfgSilenceKey == null ? "" : (Plugin.CfgSilenceKey.Value ?? "");
            if (silenceSpec.Length > 0 && SpecDownBeh(silenceSpec))
            {
                try { Speech.Stop(); } catch (Exception e) { A11yHost.Diag("沉默: " + e.Message); }
            }
        }

        /// <summary>
        /// 功能键读取：平台层 <see cref="Hotkey"/> 与「新 Input System」两路并集。
        /// 具体实现见 <see cref="Plugin.SpecDown"/>（放在 Plugin 类里，导航开关键也要用）。
        /// </summary>
        private static bool SpecDownBeh(string spec)
        {
            return Plugin.SpecDown(spec);
        }

        // ==================== 测试专用：注入 Backspace ====================
        // 只在 MANOSABA_A11Y_TEST_KEYS=1 时启用；发布版不会走到。
        // 为什么要这个：自动化环境里 keybd_event 注入的键进不了 GetAsyncKeyState
        // （0.0.0.5 实测 sawKey=False），而「新 Input System」能读到游戏自己队列里的按键状态。
        // 这条路径可以端到端验证「读键 → Reader.LastLine → Repeat.Say」。
        private static bool _keyInjectRead;
        private static bool _keyInjectOn;
        private static float _nextKeyInjectAt = -1f;
        private static int _keyInjects;

        // 另一个测试钩子：MANOSABA_A11Y_TEST_REPEAT=1 → 40s 时按一次「重读」的代码路径
        // （不经过读键，只验证 Reader.LastLine → Repeat.Say 这一段）。
        // 为什么需要：自动化环境里 Win32/InputSystem 两条注入都进不来（实测），
        // 而读键链路已经由单元用例（Hotkey.Parse）与 KEYDIAG（reading=true）覆盖。
        private static bool _testRepeatRead;
        private static bool _testRepeatOn;
        private static float _nextTestRepeatAt = -1f;
        private static int _testRepeats;

        // 整页界面 Esc 关闭的测试钩子（MANOSABA_A11Y_TEST_ESC=1）
        private static bool _testEscRead;
        private static bool _testEscOn;
        private static float _nextTestEscAt = -1f;
        private static int _testEscs;
        private static bool _testMapSimulated;

        private static void TestKeyInjectTick()
        {
            if (!_testRepeatRead)
            {
                _testRepeatRead = true;
                _testRepeatOn = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_REPEAT") == "1";
                if (_testRepeatOn)
                {
                    Plugin.L.LogInfo("[a11y] TEST REPEAT MODE: 40s 起每 25s 触发一次重读（验证 LastLine → Repeat.Say）");
                    _nextTestRepeatAt = Time.realtimeSinceStartup + 40f;
                }
            }
            if (_testRepeatOn && Time.realtimeSinceStartup >= _nextTestRepeatAt)
            {
                _nextTestRepeatAt = Time.realtimeSinceStartup + 25f;
                _testRepeats++;
                string last = Reader.LastLine;
                Plugin.L.LogInfo("[a11y] TEST REPEAT #" + _testRepeats + ": 触发重读 t=" + Time.realtimeSinceStartup.ToString("F1")
                               + " last=" + (string.IsNullOrEmpty(last) ? "<null>" : "len" + last.Length));
                if (string.IsNullOrEmpty(last)) Repeat.Say("还没有朗读过内容。", true);
                else { A11yHost.Diag("重读: " + last); Repeat.Say(last, true); }
            }

            if (!_keyInjectRead)
            {
                _keyInjectRead = true;
                _keyInjectOn = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_KEYS") == "1";
                if (_keyInjectOn)
                {
                    Plugin.L.LogInfo("[a11y] TEST KEYS MODE: 每 20s 注入一次 Backspace（验证新 Input System 读键链路）");
                    _nextKeyInjectAt = Time.realtimeSinceStartup + 30f;
                }
            }

            // 整页界面的 Esc 关闭测试钩子（自动化环境键盘注入被系统挡掉，见 ScreenAssist.SimulateEscape 注释）
            if (!_testEscRead)
            {
                _testEscRead = true;
                _testEscOn = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_ESC") == "1";
                if (_testEscOn)
                {
                    Plugin.L.LogInfo("[a11y] TEST ESC MODE: 45s 起每 20s 尝试关闭整页界面（魔女手册/记录）");
                    _nextTestEscAt = Time.realtimeSinceStartup + 45f;
                }
            }
            if (_testEscOn && Time.realtimeSinceStartup >= _nextTestEscAt)
            {
                _nextTestEscAt = Time.realtimeSinceStartup + 20f;
                _testEscs++;
                bool handled = ScreenAssist.SimulateEscape();
                Plugin.L.LogInfo("[a11y] TEST ESC #" + _testEscs + ": 触发关闭 t=" + Time.realtimeSinceStartup.ToString("F1")
                               + " handled=" + handled);
            }

            // 地图地点选择验收：地图页一出现就把前 5 个地点置为可点（只做一次）
            if (Plugin.TestMapChoiceOnce && !_testMapSimulated)
            {
                if (ScreenAssist.MapVisible)
                {
                    _testMapSimulated = true;
                    // 不再用 SimulateChoice 强行 Show 标签：那会把「游戏没允许去的地点」
                    // 也弄成可见，污染判据。这里保持游戏原生状态，只看我们认不认得出来。
                    Plugin.L.LogInfo("[a11y] TEST MAPCHOICE: 地图页已出现（保持游戏原生状态）");
                }
            }

            if (!_keyInjectOn) return;

            InputInject.Tick(); // 到点抬起（不 Tick 的话键会一直处于按下状态）

            float now = Time.realtimeSinceStartup;
            if (now < _nextKeyInjectAt) return;
            _nextKeyInjectAt = now + 20f;
            _keyInjects++;
            Plugin.L.LogInfo("[a11y] TEST KEYS: 注入 Backspace #" + _keyInjects + " t=" + now.ToString("F1") + " last=" + (Reader.LastLine == null ? "<null>" : "len" + Reader.LastLine.Length));
            InputInject.PressKey(UnityEngine.InputSystem.Key.Backspace, 0.35f);
        }

        // ==================== 键盘链路诊断 ====================
        // 本作只启用新 Input System（旧 UnityEngine.Input 恒抛 InvalidOperationException），
        // 所以平台层的 Tab/方向键/回车全部要经 GlobalKeys（Win32 GetAsyncKeyState）兜底。
        // 一旦兜底失效，症状是「导航能进、方向键没反应」——静默且极难猜，所以留一个开关。
        private static bool _keyDiagRead;
        private static bool _keyDiagOn;
        private static float _keyDiagAt;

        private static void KeyDiagTick()
        {
            if (!_keyDiagRead)
            {
                _keyDiagRead = true;
                _keyDiagOn = Environment.GetEnvironmentVariable("MANOSABA_A11Y_KEYDIAG") == "1";
                if (_keyDiagOn) Plugin.L.LogInfo("[a11y] KEYDIAG MODE: 每 0.5s 打印键盘读取状态");
            }
            if (!_keyDiagOn) return;

            float now = Time.realtimeSinceStartup;
            if (now < _keyDiagAt) return;
            _keyDiagAt = now + 0.5f;

            try
            {
                Plugin.L.LogInfo("KEYDIAG reading=" + GlobalKeys.Reading
                    + " available=" + GlobalKeys.Available
                    + " up=" + GlobalKeys.Held(KeyCode.UpArrow)
                    + " down=" + GlobalKeys.Held(KeyCode.DownArrow)
                    + " enter=" + GlobalKeys.Held(KeyCode.Return)
                    + " tab=" + GlobalKeys.Held(KeyCode.Tab)
                    + " f1=" + GlobalKeys.Held(KeyCode.F1)
                    + " alt=" + GlobalKeys.Held(KeyCode.LeftAlt)
                    + " shift=" + GlobalKeys.Held(KeyCode.LeftShift)
                    + " back=" + GlobalKeys.Held(KeyCode.Backspace)
                    + " esc=" + GlobalKeys.Held(KeyCode.Escape)
                    + " escIsys=" + InputSysKeys.Held(KeyCode.Escape)
                    + " backspaceVk=" + (GlobalKeys.Reading ? "on" : "off")
                    + " repeat=" + (Plugin.CfgRepeatKey == null ? "<null>" : "[" + Plugin.CfgRepeatKey.Value + "]")
                    + " last=" + (Reader.LastLine == null ? "<null>" : "len" + Reader.LastLine.Length)
                    + " isys=" + InputSysKeys.Available
                    + " isysBack=" + InputSysKeys.Held(KeyCode.Backspace));
            }
            catch (Exception e) { Plugin.L.LogInfo("KEYDIAG 异常: " + e.Message); }
        }

        private static string _jumpPath;
        private static bool _jumpPathRead;
        private static bool _jumped;
        private static float _jumpAt = -1f;
        private static bool _storySeen;

        private void JumpTick()
        {
            if (!_jumpPathRead)
            {
                _jumpPathRead = true;
                _jumpPath = Environment.GetEnvironmentVariable("MANOSABA_A11Y_JUMP");
                if (!string.IsNullOrEmpty(_jumpPath))
                    Plugin.L.LogInfo("[a11y] JUMP=" + _jumpPath + "（诊断用：跳过游戏流程直接加载剧本）");
            }
            if (string.IsNullOrEmpty(_jumpPath) || _jumped) return;
            if (!Naninovel.Engine.Initialized) return;

            // 只有**真的进了正片**之后再跳。理由（run16 实机）：在标题画面直接 LoadAndPlay
            // 只推进了脚本状态机，UI 还停在标题 —— 那种状态下什么补丁点都验证不到。
            if (!_storySeen)
            {
                try
                {
                    Naninovel.IScriptPlayer sp0 = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>();
                    string p0 = "";
                    try { p0 = sp0 == null || sp0.PlayedScript == null ? "" : sp0.PlayedScript.Path; } catch { }
                    if (sp0 != null && sp0.Playing && p0 != null && p0.StartsWith("Act"))
                    {
                        _storySeen = true;
                        _jumpAt = Time.unscaledTime + 4f;
                        Plugin.L.LogInfo("[a11y] JUMP 已进正片（path=" + p0 + "），4 秒后跳转到 " + _jumpPath);
                    }
                }
                catch { }
                return;
            }

            if (_jumpAt < 0f) { _jumpAt = Time.unscaledTime + 4f; return; }
            if (Time.unscaledTime < _jumpAt) return;

            _jumped = true;
            try
            {
                Naninovel.IScriptPlayer sp = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>();
                if (sp == null) { Plugin.L.LogWarning("[a11y] JUMP 失败：拿不到 IScriptPlayer"); return; }
                Naninovel.UniTaskExtensions.Forget(Naninovel.ScriptPlayerExtensions.LoadAndPlay(sp, _jumpPath, 0));
                Plugin.L.LogInfo("[a11y] JUMP LoadAndPlay 已发出: " + _jumpPath);
            }
            catch (Exception e) { Plugin.L.LogWarning("[a11y] JUMP 异常: " + e.Message); }
        }

        public void OnDestroy()
        {
            try { Speech.Shutdown(); } catch { }
            try { A11yHost.Clear(); } catch { }
        }
    }

    // Harmony 补丁集中在 Patches.cs（含 ADV 打印链、辩论面板、选项面板）。
}




