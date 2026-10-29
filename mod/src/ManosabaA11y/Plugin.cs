using System;
using System.Reflection;
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
        public const string Version = "0.0.0.1";

        public const string DisplayName = "魔法少女ノ魔女裁判 A11y Reader";

        internal static ManualLogSource L;

        // ==================== 朗读 ====================
        internal static ConfigEntry<bool> CfgReadUnvoiced;
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
        internal static ConfigEntry<bool> CfgSortByPosition;
        internal static ConfigEntry<bool> CfgVisibleOnly;
        internal static ConfigEntry<bool> CfgQuitConfirm;
        internal static ConfigEntry<string> CfgQuitNames;
        internal static ConfigEntry<string> CfgExcludeNames;

        // ==================== 其它 ====================
        internal static ConfigEntry<bool> CfgStartupHint;
        internal static ConfigEntry<bool> CfgDiagLog;

        private Harmony _harmony;

        /// <summary>
        /// 实机测试模式：只有环境变量 MANOSABA_A11Y_TEST=1 时打开。
        /// 打开后强制诊断日志 + 启用 TestDriver（用引擎内部入口点开始游戏、开自动播放，
        /// 绕开鼠标/窗口焦点问题）。生产用户不会设置该变量，发布版行为不变。
        /// </summary>
        internal static bool TestMode;

        public override void Load()
        {
            L = Log;
            A11yHost.Log = Log;

            BindConfig();

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
        }

        public override bool Unload()
        {
            try { Speech.Shutdown(); } catch { }
            try { if (_harmony != null) _harmony.UnpatchSelf(); } catch { }
            try { A11yHost.Clear(); } catch { }
            return true;
        }

        // ====================================================================
        // 配置：分组名固定三组「朗读」「界面导航」「其它」
        // （流水线 §P2；换名字会让老用户的 cfg 失效）
        // ====================================================================
        private void BindConfig()
        {
            CfgReadUnvoiced = Config.Bind("朗读", "朗读无配音剧情", true,
                "有语音的台词由游戏自己播，再念一遍会重叠；没配音的行必须念。");
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
                "重念当前这句。");
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
                "Tab 键在可交互控件间移动并朗读，方向键/回车等按平台层规则工作。");
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
            CfgTextLocale = Config.Bind("其它", "文本语言", "跟随游戏",
                "跟随游戏 / 简体中文 / 日本語。\n" +
                "★ 游戏本体的语言下拉框在 Options → Message 页（「言語 / Language」），读屏用户摸不到，\n" +
                "  所以在这里直接指定要朗读哪种语言的文本。实机默认是「日本語」。");
            CfgDiagLog = Config.Bind("其它", "界面诊断日志", false,
                "把导航/文本提取的诊断信息写进 BepInEx 日志，只在排查时打开。");

            // 无人值守实机测试开关：��环境变量 MANOSABA_A11Y_TEST=1 时强制打开诊断日志，
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
                    try { Repeat.Say("魔法少女ノ魔女裁判 无障碍补丁已加载"); } catch { }
                }
            }

            try { if (A11yHost.TickUiNav != null) A11yHost.TickUiNav(); }
            catch (Exception e) { A11yHost.Diag("Tick: " + e.Message); }

            // 剧情文本朗读队列（消息级钩子只登记，这里到点统一念，避免一句念两遍）
            try { Reader.Tick(); }
            catch (Exception e) { A11yHost.Diag("Reader.Tick: " + e.Message); }

            // 加载播报（本作点「新游戏」后有明显的黑屏加载段）
            try { LoadingWatch.Update(); }
            catch (Exception e) { A11yHost.Diag("LoadingWatch: " + e.Message); }

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




