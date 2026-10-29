// NaninovelReProbe —— naninovel-re 的运行时验证探针（scope: Elringus.Naninovel.* 核心层）
//
// 目的（对应 re\naninovel-api.md 第 9 节「待探针验证清单」）：
//   1. TextPrinterManager.Print(struct 参数) 能否被 Harmony detour；prefix 能否拿到 printerId / 文本 / 作者
//   2. RevealableText.set_Text(string) postfix 能否拿到最终显示文本 + 所属面板 Author
//   3. 打印事件与 IScriptPlayer 状态的先后（Playing / WaitingForInput / PlayedIndex / PlayedScript.Path）
//   4. DelegateSupport.ConvertDelegate 能否订阅 Action<bool> / Action<Script>（blittable 规则实证）
//   5. IAudioManager.GetPlayedVoice / IAudioTrack 返回形态
//   6. 自动进入剧本：程序化点击 Naninovel.UI.TitleNewGameButton.OnButtonClick()（无人值守，避免盲点鼠标）
//
// 所有结论以 RE_* 前缀写入 BepInEx\LogOutput.log，并同步落到
//   D:\DSHWorkSpace\manosaba_a11y\re\logs\re-probe.log
// 有 struct 参数的高风险补丁可用哨兵文件禁用（崩溃后无需重编译）：
//   若存在 D:\DSHWorkSpace\manosaba_a11y\re\probe\DISABLE_STRUCT_PATCHES 则跳过第 1、7 项。

using System;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Naninovel;
using Naninovel.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NaninovelReProbe
{
    [BepInPlugin("manosaba.re.probe", "Naninovel RE Probe", "0.1.0")]
    public class ReProbePlugin : BasePlugin
    {
        public override void Load()
        {
            ReProbeLog.Init(base.Log);
            ReProbeLog.W("=== NaninovelReProbe 0.1.0 Load() ===");
            ReProbeLog.W("unityVersion=" + Application.unityVersion + " product=" + Application.productName);
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<ReProbeBehaviour>();
                AddComponent<ReProbeBehaviour>();
                ReProbeLog.W("RE_BEHAVIOUR_OK (RegisterTypeInIl2Cpp + AddComponent)");
            }
            catch (Exception e) { ReProbeLog.W("RE_BEHAVIOUR_FAIL " + e); }
            // 注意：Harmony 安装延后到 Behaviour.Update()（首帧）。在 BasePlugin.Load() 阶段直接 detour 会对
            // 尚未完成初始化的 IL2CPP 类型（如 Naninovel.UniTask 静态构造）做 native trampoline 生成，实测
            // 触发 AccessViolationException 直接杀进程（2026-10-28 probe1）。延后到首帧后安装即稳定。
            ReProbeLog.W("RE_PATCH_DEFERRED 补丁安装延后到 ReProbeBehaviour.Update() 首帧");
        }
    }

    internal static class ReProbeLog
    {
        private static ManualLogSource _log;
        private static string _file;
        private static readonly object _gate = new object();

        public static void Init(ManualLogSource log)
        {
            _log = log;
            try
            {
                var dir = @"D:\DSHWorkSpace\manosaba_a11y\re\logs";
                Directory.CreateDirectory(dir);
                _file = Path.Combine(dir, "re-probe.log");
                File.WriteAllText(_file, "# NaninovelReProbe runtime evidence " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\n", Encoding.UTF8);
            }
            catch { _file = null; }
        }

        public static void W(string msg)
        {
            try { _log?.LogInfo(msg); } catch { }
            lock (_gate)
            {
                if (_file == null) return;
                try { File.AppendAllText(_file, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n", Encoding.UTF8); } catch { }
            }
        }

        public static void E(string msg) { try { _log?.LogError(msg); } catch { } W("[ERR] " + msg); }

        public static string Clip(string s, int n)
        {
            if (s == null) return "<null>";
            s = s.Replace("\r", "\\r").Replace("\n", "\\n");
            return s.Length <= n ? s : s.Substring(0, n) + "…(" + s.Length + ")";
        }
    }

    public class ReProbeBehaviour : MonoBehaviour
    {
        public ReProbeBehaviour(IntPtr ptr) : base(ptr) { }

        private ITextPrinterManager _printer;
        private IScriptPlayer _player;
        private IAudioManager _audio;
        private bool _subscribed;
        private bool _autoPlaySet;
        private bool _noAutoLoad;
        private bool _noAutoLoadRead;
        private float _nextAutoPlayAssert;
        private bool _skipOn;

        // 路径来自 text-extract 已提取的 zh-Hans 剧本文件（textassets\scripts\zh-Hans\），非猜测。
        // 第一步进辩论脚本（Trial01 第 4 行即 @printDebate，用于验证 printDebate 是否走 TextPrinterManager.Print）；
        // 第二步进含 @choice 的 ADV 脚本（Adv05 第 92 行 #0101Adv05_Choice001），配合 skip 快速推进到选项。
        private struct PlayStep { public float After; public string Path; public bool SkipAfter; }
        private static readonly PlayStep[] _playSteps = new[]
        {
            new PlayStep { After = 12f, Path = "Act01_Chapter01/Act01_Chapter01_Trial01", SkipAfter = false },
            new PlayStep { After = 95f, Path = "Act01_Chapter01/Act01_Chapter01_Adv05", SkipAfter = true },
        };
        private float _readyAt = -1f;
        private int _titleClickTries;
        private float _nextTitleTry;
        private float _nextUiScan;
        private int _uiScans;
        private bool _uiClicked;
        private bool _loadPlayed;
        private int _playStepIndex;
        private float _skipOnAt = -1f;
        private float _skipOffAt = -1f;
        private float _nextStateLog;
        private int _stateLogs;

        private Il2CppSystem.Action<bool> _onWaiting;
        private Il2CppSystem.Action<bool> _onAutoPlay;
        private Il2CppSystem.Action<Script> _onPlay;
        private Il2CppSystem.Action<Script> _onStop;

        private static bool SafeBool(Func<bool> f)
        {
            try { return f(); } catch { return false; }
        }

        private void Update()
        {
            try
            {
                if (_printer == null)
                {
                    try { _printer = Engine.GetService<ITextPrinterManager>(); }
                    catch { }
                    if (_printer == null) return;
                    _readyAt = Time.realtimeSinceStartup;
                    ReProbeLog.W("RE_ENGINE_READY t=" + _readyAt.ToString("F1")
                        + " printerType=" + SafeType(_printer) + " defaultPrinterId='" + Safe("printerId", () => _printer.DefaultPrinterId) + "'");
                    try
                    {
                        ReProbeLog.W("RE_DEBATE_PRINTER_ID static PrintDebateText.DebatePrinterId='" + WitchTrials.Views.PrintDebateText.DebatePrinterId + "'");
                    }
                    catch (Exception e) { ReProbeLog.E("RE_DEBATE_PRINTER_ID_FAIL " + e.GetType().Name + ": " + e.Message); }
                    if (!_noAutoLoadRead)
                    {
                        _noAutoLoadRead = true;
                        try { _noAutoLoad = System.IO.File.Exists(@"D:\DSHWorkSpace\manosaba_a11y\re\probe\NO_AUTO_LOAD"); } catch { }
                        ReProbeLog.W("RE_NO_AUTO_LOAD " + _noAutoLoad + "（true=只挂探针，不自动点击/autoplay/LoadAndPlay）");
                    }
                    // 引擎就绪后再安装 Harmony：避免在 IL2CPP 类型静态构造完成前生成 native trampoline（probe1 崩溃根因）
                    if (!ReProbePatches.Installed) ReProbePatches.InstallAll();
                    TrySubscribe();
                }

                if (_player == null)
                {
                    try { _player = Engine.GetService<IScriptPlayer>(); } catch { }
                }

                if (_player != null && !_autoPlaySet && !_noAutoLoad)
                {
                    _autoPlaySet = true;
                    try
                    {
                        _player.SetAutoPlayEnabled(true);
                        ReProbeLog.W("RE_AUTOPLAY_OK SetAutoPlayEnabled(true) 已调用，便于无人值守推进台词");
                    }
                    catch (Exception e) { ReProbeLog.E("RE_AUTOPLAY_FAIL " + e.GetType().Name + ": " + e.Message); }
                }

                var now = Time.realtimeSinceStartup;

                // 自动进入剧本：只在还没有任何打印事件时尝试点“New Game”
                if (_readyAt > 0 && ReProbePatches.PrintCalls == 0 && ReProbePatches.SetTextCalls == 0
                    && _titleClickTries < 4 && !_noAutoLoad && now > _readyAt + 6f && now > _nextTitleTry)
                {
                    _nextTitleTry = now + 8f;
                    _titleClickTries++;
                    TryClickNewGame();
                }

                // 通用 UI 按钮扫描（不依赖具体类型名，只看 uGUI Button + TMP 文本）：
                // 本作标题不是 Naninovel TitleMenu（probe2 实证 titleNewGameButtons=0），标题按钮来自游戏自绘 UI。
                // 命中关键词后直接 onClick.Invoke()，等价玩家点击，无需鼠标/焦点。LoadAndPlay 成功后不再点，避免误触。
                if (!_uiClicked && !_loadPlayed && !_noAutoLoad && ReProbePatches.PrintCalls == 0 && now > _nextUiScan && _uiScans < 3)
                {
                    _nextUiScan = now + 5f;
                    _uiScans++;
                    try { TryScanUiButtons(); }
                    catch (Exception e) { ReProbeLog.E("RE_UI_SCAN_FAIL " + e.GetType().Name + ": " + e.Message); }
                }

                // 进正片主路径：ScriptPlayerExtensions.LoadAndPlay（裸 Play 只取已加载资源，probe2 实证会抛 not loaded）。
                // 实查签名（Elringus.Naninovel.Runtime.decompiled.cs:174559）：
                //   public static UniTask LoadAndPlay(this IScriptPlayer player, string scriptPath, int playlistIndex = 0)
                if (_player != null && !_noAutoLoad && _readyAt > 0 && _playStepIndex < _playSteps.Length
                    && now > _readyAt + _playSteps[_playStepIndex].After)
                {
                    var step = _playSteps[_playStepIndex];
                    _playStepIndex++;
                    try
                    {
                        var task = ScriptPlayerExtensions.LoadAndPlay(_player, step.Path, 0);
                        _loadPlayed = true;
                        ReProbeLog.W("RE_LOADPLAY_OK #" + _playStepIndex + " ScriptPlayerExtensions.LoadAndPlay('" + step.Path + "', 0) 已调用 skipAfter=" + step.SkipAfter);
                        GC.KeepAlive(task);
                        if (step.SkipAfter) { _skipOnAt = now + 6f; _skipOffAt = now + 36f; }
                    }
                    catch (Exception e)
                    {
                        ReProbeLog.E("RE_LOADPLAY_FAIL path='" + step.Path + "' " + e.GetType().Name + ": " + e.Message);
                    }
                }

                // 自动播放会被剧本命令（如 System_Title）重置，定期重申；否则 probe3 只推进到第 1 句。
                if (_player != null && !_noAutoLoad && _loadPlayed && now > _nextAutoPlayAssert)
                {
                    _nextAutoPlayAssert = now + 8f;
                    try
                    {
                        if (!SafeBool(() => _player.AutoPlayActive))
                        {
                            _player.SetAutoPlayEnabled(true);
                            ReProbeLog.W("RE_AUTOPLAY_REASSERT t=" + now.ToString("F0"));
                        }
                    }
                    catch (Exception e) { ReProbeLog.E("RE_AUTOPLAY_REASSERT_FAIL " + e.GetType().Name + ": " + e.Message); }
                }

                // skip 窗口：快速推进到远处命令（选项/辩论），只用于验证钩子命中，不验证朗读体验
                if (_player != null && !_noAutoLoad && _skipOnAt > 0 && now > _skipOnAt && now < _skipOffAt)
                {
                    if (!_skipOn)
                    {
                        _skipOn = true;
                        try { _player.SetSkipEnabled(true); ReProbeLog.W("RE_SKIP_ON t=" + now.ToString("F0")); }
                        catch (Exception e) { ReProbeLog.E("RE_SKIP_ON_FAIL " + e.GetType().Name + ": " + e.Message); }
                    }
                }
                else if (_skipOn && now >= _skipOffAt)
                {
                    _skipOn = false;
                    try { _player.SetSkipEnabled(false); ReProbeLog.W("RE_SKIP_OFF t=" + now.ToString("F0")); }
                    catch (Exception e) { ReProbeLog.E("RE_SKIP_OFF_FAIL " + e.GetType().Name + ": " + e.Message); }
                }

                if (_player != null && now > _nextStateLog)
                {
                    _nextStateLog = now + 10f;
                    _stateLogs++;
                    LogState();
                    if (_stateLogs >= 20)
                    {
                        ReProbeLog.W("RE_SUMMARY printCalls=" + ReProbePatches.PrintCalls
                            + " printHits=" + ReProbePatches.PrintHits
                            + " setTextCalls=" + ReProbePatches.SetTextCalls
                            + " setTextEmpty=" + ReProbePatches.SetTextEmpty
                            + " printsWithAuthor=" + ReProbePatches.PrintsWithAuthor
                            + " printPartsLogged=" + ReProbePatches.PrintPartsLogged
                            + " waitLogs=" + ReProbePatches.WaitLogs
                            + " modifiedWaitHits=" + ReProbePatches.ModifiedWaitHits
                            + " choiceLogs=" + ReProbePatches.ChoiceLogs
                            + " choiceAddHits=" + ReProbePatches.ChoiceAddHits
                            + " debateAddMsgHits=" + ReProbePatches.DebateAddMsgHits
                            + " debateSetTextHits=" + ReProbePatches.DebateSetTextHits
                            + " advAuthorHits=" + ReProbePatches.AdvAuthorHits
                            + " buttonClicks=" + ReProbePatches.ButtonClicks
                            + " authorReads=" + ReProbePatches.AuthorReads);
                    }
                }
            }
            catch (Exception e) { ReProbeLog.E("RE_UPDATE_EX " + e.GetType().Name + ": " + e.Message); }
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            try
            {
                _player = Engine.GetService<IScriptPlayer>();
                if (_player == null) { ReProbeLog.W("RE_SUB_FAIL player=null"); return; }

                _onWaiting = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<bool>>(new Action<bool>(v =>
                    ReProbeLog.W("RE_EVENT OnWaitingForInput " + v + " printCalls=" + ReProbePatches.PrintCalls)));
                _onAutoPlay = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<bool>>(new Action<bool>(v =>
                    ReProbeLog.W("RE_EVENT OnAutoPlay " + v)));
                _onPlay = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Script>>(new Action<Script>(s =>
                    ReProbeLog.W("RE_EVENT OnPlay script=" + Safe("scriptPath", () => s.Path))));
                _onStop = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Script>>(new Action<Script>(s =>
                    ReProbeLog.W("RE_EVENT OnStop script=" + Safe("scriptPath", () => s.Path))));

                _player.add_OnWaitingForInput(_onWaiting);
                ReProbeLog.W("RE_SUB_OK OnWaitingForInput(Action<bool>)");
                _player.add_OnAutoPlay(_onAutoPlay);
                ReProbeLog.W("RE_SUB_OK OnAutoPlay(Action<bool>)");
                _player.add_OnPlay(_onPlay);
                _player.add_OnStop(_onStop);
                ReProbeLog.W("RE_SUB_OK OnPlay/OnStop(Action<Script>)");
            }
            catch (Exception e) { ReProbeLog.E("RE_SUB_FAIL " + e.GetType().Name + ": " + e.Message); }
        }

        private void TryClickNewGame()
        {
            try
            {
                var btns = UnityEngine.Object.FindObjectsOfType<TitleNewGameButton>(true);
                ReProbeLog.W("RE_TITLE_SCAN titleNewGameButtons=" + (btns?.Length ?? -1));
                if (btns == null || btns.Length == 0) return;
                var b = btns[0];
                try { ReProbeLog.W("RE_TITLE_INFO name='" + b.name + "' activeInHierarchy=" + b.gameObject.activeInHierarchy + " interactable=" + Safe("interactable", () => b.Interactable)); } catch { }
                b.OnButtonClick();
                ReProbeLog.W("RE_TITLE_CLICK_OK Naninovel.UI.TitleNewGameButton.OnButtonClick() 已调用");
            }
            catch (Exception e) { ReProbeLog.E("RE_TITLE_CLICK_FAIL " + e.GetType().Name + ": " + e.Message); }
        }

        // 扫描全场景 uGUI Button（含 inactive），打印 type/name/active/TMP 文本；
        // 命中「新游戏/开始」类关键词则 onClick.Invoke()。跳过退出/关闭类，避免误触杀进程。
        private void TryScanUiButtons()
        {
            var buttons = UnityEngine.Object.FindObjectsOfType<Button>(true);
            var total = buttons == null ? -1 : buttons.Length;
            var sb = new StringBuilder("RE_UI_BUTTONS n=" + total);
            int logged = 0;
            Button target = null;
            string targetDesc = null;
            if (buttons != null)
            {
                foreach (var b in buttons)
                {
                    if (b == null) continue;
                    string txt = "";
                    try
                    {
                        var t = b.GetComponentInChildren<TMP_Text>(true);
                        if (t != null) txt = t.text;
                    }
                    catch { }
                    var nm = b.name ?? "";
                    var active = true;
                    try { active = b.gameObject.activeInHierarchy; } catch { }
                    if (logged < 14)
                    {
                        sb.Append(" | ").Append(nm).Append("{txt='").Append(ReProbeLog.Clip(txt, 24))
                          .Append("',active=").Append(active).Append(",interactable=").Append(Safe("intr", () => b.interactable)).Append("}");
                        logged++;
                    }
                    if (target != null) continue;
                    var hay = (nm + " " + txt).ToLowerInvariant();
                    if (hay.Contains("quit") || hay.Contains("exit") || hay.Contains("終了") || hay.Contains("退出") || hay.Contains("close")) continue;
                    if (!active) continue;
                    if (hay.Contains("new game") || hay.Contains("newgame") || hay.Contains("start")
                        || hay.Contains("新游戏") || hay.Contains("开始") || hay.Contains("開始") || hay.Contains("はじめ"))
                    {
                        target = b;
                        targetDesc = "name='" + nm + "' text='" + ReProbeLog.Clip(txt, 32) + "'";
                    }
                }
            }
            ReProbeLog.W(sb.ToString());
            if (target == null) { ReProbeLog.W("RE_UI_NO_MATCH"); return; }
            try
            {
                target.onClick.Invoke();
                _uiClicked = true;
                ReProbeLog.W("RE_UI_CLICK_OK " + targetDesc + " onClick.Invoke() 已调用");
            }
            catch (Exception e) { ReProbeLog.E("RE_UI_CLICK_FAIL " + targetDesc + " " + e.GetType().Name + ": " + e.Message); }
        }

        private void LogState()
        {
            var sb = new StringBuilder("RE_STATE ");
            sb.Append("playing=").Append(Safe("playing", () => _player.Playing));
            sb.Append(" waitingInput=").Append(Safe("wait", () => _player.WaitingForInput));
            sb.Append(" autoPlay=").Append(Safe("auto", () => _player.AutoPlayActive));
            sb.Append(" skip=").Append(Safe("skip", () => _player.SkipActive));
            sb.Append(" playedIndex=").Append(Safe("idx", () => _player.PlayedIndex));
            sb.Append(" count=").Append(Safe("cnt", () => _player.PlayedCommandsCount));
            sb.Append(" script='").Append(Safe("script", () => _player.PlayedScript?.Path)).Append("'");
            sb.Append(" spot=").Append(Safe("spot", () =>
            {
                var s = _player.PlaybackSpot;
                return s.ScriptPath + ":" + s.LineIndex + ":" + s.InlineIndex;
            }));
            sb.Append(" cmd=").Append(Safe("cmd", () => _player.PlayedCommand == null ? "<null>" : _player.PlayedCommand.GetType().Name));
            sb.Append(" voice=").Append(Safe("voice", () =>
            {
                if (_audio == null) _audio = Engine.GetService<IAudioManager>();
                return _audio == null ? "<no-manager>" : _audio.GetPlayedVoice();
            }));
            sb.Append(" panelText=[").Append(Safe("panelText", () =>
            {
                var panels = UnityEngine.Object.FindObjectsOfType<RevealableTextPrinterPanel>(true);
                if (panels != null)
                {
                    foreach (var p in panels)
                    {
                        if (p == null || !p.gameObject.activeInHierarchy) continue;
                        var msgs = p.Messages;
                        if (msgs == null || msgs.Count == 0) continue;
                        var m = msgs[msgs.Count - 1];
                        string author; try { author = ReProbePatches.AuthorDesc(m); } catch { author = "?"; }
                        return "[" + p.name + "/" + p.GetType().Name + "] text=[" + ReProbeLog.Clip(Safe("t", () => (string)m.Text), 120)
                            + "] author={" + author + "}";
                    }
                }
                return "<none>";
            })).Append("]");
            sb.Append(" printCalls=").Append(ReProbePatches.PrintCalls);
            sb.Append(" printHits=").Append(ReProbePatches.PrintHits);
            sb.Append(" setTextCalls=").Append(ReProbePatches.SetTextCalls);
            sb.Append(" debateAddMsgHits=").Append(ReProbePatches.DebateAddMsgHits);
            sb.Append(" debateSetTextHits=").Append(ReProbePatches.DebateSetTextHits);
            sb.Append(" choiceLogs=").Append(ReProbePatches.ChoiceLogs);
            sb.Append(" choiceAddHits=").Append(ReProbePatches.ChoiceAddHits);
            try
            {
                var panels = UnityEngine.Object.FindObjectsOfType<ChoiceHandlerPanel>(true);
                sb.Append(" choicePanels=").Append(panels?.Length ?? -1);
                var buttons = UnityEngine.Object.FindObjectsOfType<ChoiceHandlerButton>(true);
                if (buttons != null && buttons.Length > 0)
                {
                    sb.Append(" choiceButtons=").Append(buttons.Length);
                    for (int i = 0; i < buttons.Length && i < 6; i++)
                    {
                        var b = buttons[i];
                        sb.Append(" |").Append(i).Append("='").Append(Safe("btn", () => b.gameObject.name)).Append("'");
                        try { sb.Append("=").Append(ReProbeBehaviour._ChoiceText(b.ChoiceState)); } catch { sb.Append("=<no-state>"); }
                    }
                }
            }
            catch (Exception e) { sb.Append(" choiceScan=ERR:").Append(e.GetType().Name); }

            try
            {
                var tpanels = UnityEngine.Object.FindObjectsOfType<UITextPrinterPanel>(true);
                sb.Append(" printerPanels=").Append(tpanels?.Length ?? -1);
                if (tpanels != null)
                {
                    for (int i = 0; i < tpanels.Length && i < 8; i++)
                    {
                        var p = tpanels[i];
                        if (p == null) continue;
                        string msgCount;
                        try { msgCount = p.Messages == null ? "null" : p.Messages.Count.ToString(); } catch { msgCount = "ERR"; }
                        sb.Append(" |").Append(i).Append(":").Append(p.GetType().Name)
                          .Append(" name='").Append(Safe("n", () => p.name)).Append("'")
                          .Append(" active=").Append(Safe("a", () => p.gameObject.activeInHierarchy))
                          .Append(" msgs=").Append(msgCount);
                    }
                }
            }
            catch (Exception e) { sb.Append(" printerPanelScan=ERR:").Append(e.GetType().Name); }

            ReProbeLog.W(sb.ToString());
        }

        internal static string _ChoiceText(ChoiceState c)
        {
            try { return ReProbeLog.Clip((string)c.summary, 80); } catch (Exception e) { return "<ERR " + e.GetType().Name + ">"; }
        }

        private static string SafeType(object o) { try { return o.GetType().FullName; } catch { return "<err>"; } }

        private static string Safe(string tag, Func<object> f)
        {
            try { var v = f(); return v == null ? "<null>" : v.ToString(); }
            catch (Exception e) { return "<ERR:" + tag + ":" + e.GetType().Name + ">"; }
        }
    }

    internal static class ReProbePatches
    {
        internal static int PrintCalls;
        internal static int SetTextCalls;
        internal static int WaitLogs;
        internal static int ChoiceLogs;
        internal static int ButtonClicks;
        internal static int AuthorReads;
        internal static int PrintHits;
        internal static int ChoiceAddHits;
        internal static int DebateAddMsgHits;
        internal static int DebateSetTextHits;
        internal static int ModifiedWaitHits;
        internal static int AdvAuthorHits;
        internal static int SetTextEmpty;
        internal static int PrintsWithAuthor;
        internal static int PrintPartsLogged;
        private static int _setTextLogged;
        private static readonly System.Collections.Generic.HashSet<string> _seenTexts = new System.Collections.Generic.HashSet<string>();
        private static bool _structPatchOk;
        private static string _lastPanelDesc = "<none>";

        private static Harmony _harmony;
        private static bool _installed;

        internal static bool Installed => _installed;

        internal static void InstallAll()
        {
            if (_installed) return;
            _installed = true;
            _harmony = new Harmony("manosaba.re.probe");
            ReProbeLog.W("RE_HARMONY ver=" + _harmony.GetType().Assembly.GetName().Version);

            // --- 低风险：string / bool / 无参 ---
            Patch("RevealableText.set_Text", AccessTools.Method(typeof(RevealableText), "set_Text"), null,
                AccessTools.Method(typeof(ReProbePatches), nameof(SetTextPost)));
            Patch("UITextPrinterPanel.SetWaitForInputIndicatorVisible",
                AccessTools.Method(typeof(UITextPrinterPanel), "SetWaitForInputIndicatorVisible"),
                AccessTools.Method(typeof(ReProbePatches), nameof(WaitPrefix)), null);
            // ScriptableButton.OnButtonClick 的 detour 实测在安装阶段触发 AccessViolation（probe1），已移除；
            // 按钮证据改由 RE_TITLE_* 与 RE_HIT_* 记录。

            // --- 高风险：struct 参数（PrintedMessage / ChoiceState）---
            bool disableStruct;
            try { disableStruct = File.Exists(@"D:\DSHWorkSpace\manosaba_a11y\re\probe\DISABLE_STRUCT_PATCHES"); }
            catch { disableStruct = false; }
            if (disableStruct)
            {
                ReProbeLog.W("RE_STRUCT_PATCHES_SKIPPED (哨兵文件存在)");
                return;
            }
            Patch("TextPrinterManager.Print", AccessTools.Method(typeof(TextPrinterManager), "Print"),
                AccessTools.Method(typeof(ReProbePatches), nameof(PrintPrefix)), null);
            Patch("ChoiceHandlerPanel.AddChoiceButton", AccessTools.Method(typeof(ChoiceHandlerPanel), "AddChoiceButton"),
                AccessTools.Method(typeof(ReProbePatches), nameof(AddChoicePrefix)), null);
            Patch("UITextPrinterPanel.AddMessage", AccessTools.Method(typeof(UITextPrinterPanel), "AddMessage"),
                AccessTools.Method(typeof(ReProbePatches), nameof(AddMessagePrefix)), null);

            // --- 零绑定风险命中探针（无参 prefix）：只证明"这个方法真的被调用过" ---
            // 说明：override 链上的补丁不会由基类补丁捕获（DebateTextPrinterPanel 覆写 AddMessage/SetText、
            // RevealableTextPrinterPanelModified 覆写 SetWaitForInputIndicatorVisible），这些命中探针是唯一可靠证据。
            Patch("hit:TextPrinterManager.Print", AccessTools.Method(typeof(TextPrinterManager), "Print"),
                AccessTools.Method(typeof(ReProbePatches), nameof(HitPrint)), null);
            Patch("hit:ChoiceHandlerPanel.AddChoiceButton", AccessTools.Method(typeof(ChoiceHandlerPanel), "AddChoiceButton"),
                AccessTools.Method(typeof(ReProbePatches), nameof(HitChoiceAdd)), null);
            PatchByName("hit:DebateTextPrinterPanel.AddMessage", "WitchTrials.Views.DebateTextPrinterPanel", "AddMessage",
                AccessTools.Method(typeof(ReProbePatches), nameof(HitDebateAddMsg)), null);
            PatchByName("hit:DebateTextPrinterPanel.SetText", "WitchTrials.Views.DebateTextPrinterPanel", "SetText",
                AccessTools.Method(typeof(ReProbePatches), nameof(HitDebateSetText)), null);
            PatchByName("hit:RevealableTextPrinterPanelModified.SetWaitForInputIndicatorVisible",
                "GigaCreation.NaninovelExtender.Ui.RevealableTextPrinterPanelModified", "SetWaitForInputIndicatorVisible",
                AccessTools.Method(typeof(ReProbePatches), nameof(HitModifiedWait)), null);
            PatchByName("hit:WitchTrialsTextPrinterPanel.SetMessageAuthor",
                "WitchTrials.Views.WitchTrialsTextPrinterPanel", "SetMessageAuthor",
                AccessTools.Method(typeof(ReProbePatches), nameof(HitAdvAuthor)), null);
        }

        private static void PatchByName(string label, string typeName, string methodName,
            System.Reflection.MethodInfo prefix, System.Reflection.MethodInfo postfix)
        {
            var t = AccessTools.TypeByName(typeName);
            if (t == null) { ReProbeLog.W("RE_PATCH_SKIP " + label + " type-not-found=" + typeName); return; }
            var m = AccessTools.Method(t, methodName);
            Patch(label, m, prefix, postfix);
        }

        private static void Patch(string label, System.Reflection.MethodInfo target, System.Reflection.MethodInfo prefix, System.Reflection.MethodInfo postfix)
        {
            try
            {
                if (target == null) { ReProbeLog.W("RE_PATCH_FAIL " + label + " target=null"); return; }
                _harmony.Patch(target,
                    prefix == null ? null : new HarmonyMethod(prefix),
                    postfix == null ? null : new HarmonyMethod(postfix));
                ReProbeLog.W("RE_PATCH_OK " + label + " target=" + target.DeclaringType?.FullName + "." + target.Name + "(" + Sig(target) + ")"
                    + (prefix != null ? " +prefix" : "") + (postfix != null ? " +postfix" : ""));
                if (label.StartsWith("TextPrinterManager") || label.StartsWith("UITextPrinterPanel.AddMessage")) _structPatchOk = true;
            }
            catch (Exception e) { ReProbeLog.E("RE_PATCH_FAIL " + label + " " + e.GetType().Name + ": " + e.Message); }
        }

        private static string Sig(System.Reflection.MethodInfo m)
        {
            var ps = m.GetParameters();
            var sb = new StringBuilder();
            for (int i = 0; i < ps.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(ps[i].ParameterType.Name).Append(' ').Append(ps[i].Name);
            }
            return sb.ToString();
        }

        // ---- 补丁体（全部 try/catch，异常只记日志不抛给游戏）----

        internal static void SetTextPost(RevealableText __instance, string value)
        {
            try
            {
                SetTextCalls++;
                if (string.IsNullOrEmpty(value)) SetTextEmpty++;
                var key = __instance.GetInstanceID() + "|" + value;
                bool fresh = _seenTexts.Add(key);
                if (!fresh && _setTextLogged > 200) return;
                if (_setTextLogged < 40 || value != null && value.Length > 0)
                {
                    _setTextLogged++;
                    ReProbeLog.W("RE_SETTEXT #" + _setTextLogged + " inst='" + __instance.name + "'"
                        + " panel={" + PanelDesc(__instance) + "}"
                        + " text=[" + ReProbeLog.Clip(value, 160) + "]");
                }
            }
            catch (Exception e) { ReProbeLog.E("RE_SETTEXT_EX " + e.GetType().Name + ": " + e.Message); }
        }

        private static string PanelDesc(RevealableText inst)
        {
            try
            {
                var panel = inst.GetComponentInParent<RevealableTextPrinterPanel>();
                if (panel == null) return "panel=null";
                try
                {
                    var a = panel.Author;
                    _lastPanelDesc = "panel='" + panel.name + "' authorId='" + (a.Id ?? "<null>") + "'"
                        + " authorLabel='" + ReProbeLog.Clip(a.Label, 40) + "'";
                    AuthorReads++;
                    return _lastPanelDesc;
                }
                catch (Exception e) { return "panel='" + panel.name + "' author=ERR:" + e.GetType().Name; }
            }
            catch (Exception e) { return "panel=ERR:" + e.GetType().Name; }
        }

        internal static void WaitPrefix(bool visible)
        {
            try
            {
                WaitLogs++;
                if (WaitLogs <= 30) ReProbeLog.W("RE_WAIT_INPUT_VISIBLE " + visible + " printCalls=" + PrintCalls + " setTextCalls=" + SetTextCalls);
            }
            catch (Exception e) { ReProbeLog.E("RE_WAIT_EX " + e.Message); }
        }

        internal static void ButtonPrefix(ScriptableButton __instance)
        {
            try
            {
                ButtonClicks++;
                string label = "<none>";
                try { label = __instance.gameObject.name; } catch { }
                ReProbeLog.W("RE_BUTTON_CLICK #" + ButtonClicks + " type=" + __instance.GetType().Name + " name='" + label
                    + "' parent='" + (__instance.transform != null && __instance.transform.parent != null ? __instance.transform.parent.name : "?") + "'");
            }
            catch (Exception e) { ReProbeLog.E("RE_BUTTON_EX " + e.Message); }
        }

        internal static void PrintPrefix(string printerId, PrintedMessage message, bool append, float speed)
        {
            try
            {
                PrintCalls++;
                object text = null; string textErr = null;
                try { text = (string)message.Text; } catch (Exception e) { textErr = e.GetType().Name; }
                string author = AuthorDesc(message);
                if (!author.StartsWith("<narration")) PrintsWithAuthor++;
                if (PrintCalls <= 60)
                {
                    ReProbeLog.W("RE_PRINT #" + PrintCalls + " printerId='" + printerId + "'"
                        + " append=" + append + " speed=" + speed
                        + " author={" + author + "}"
                        + " text=[" + ReProbeLog.Clip(textErr != null ? "<ERR " + textErr + ">" : (string)text, 160) + "]");
                }
                LogParts(message, PrintCalls);
            }
            catch (Exception e) { ReProbeLog.E("RE_PRINT_EX " + e.GetType().Name + ": " + e.Message); }
        }

        // 记录 LocalizableText.parts[i]：id=台词行 ID（如 0101Trial01_Sherry001）、spot=PlaybackSpot（剧透闸门用）、text=已本地化片段。
        // PrintedMessage.text 是 LocalizableText 值类型，parts 是 Il2CppReferenceArray<LocalizableTextPart>（有 Length/索引器，见 Il2CppArrayBase`1）。
        internal static void LogParts(PrintedMessage message, int n)
        {
            if (PrintPartsLogged >= 40) return;
            try
            {
                var parts = message.Text.parts;
                if (parts == null) return;
                int count = parts.Length;
                var sb = new System.Text.StringBuilder();
                int lim = Math.Min(count, 3);
                for (int i = 0; i < lim; i++)
                {
                    try
                    {
                        var p = parts[i];
                        sb.Append(" [" + i + "]{id='" + (p.id ?? "<null>") + "' plain=" + p.PlainText
                            + " spot=" + Safe(() => p.spot.ScriptPath + ":" + p.spot.LineIndex)
                            + " text=[" + ReProbeLog.Clip(p.text, 60) + "]}");
                    }
                    catch (Exception e) { sb.Append(" [" + i + "]{ERR:" + e.GetType().Name + "}"); }
                }
                PrintPartsLogged++;
                ReProbeLog.W("RE_PRINT_PARTS id#" + n + " count=" + count + sb);
            }
            catch (Exception e) { ReProbeLog.E("RE_PRINT_PARTS_EX " + e.GetType().Name + ": " + e.Message); }
        }

        internal static void AddChoicePrefix(ChoiceState choice)
        {
            try
            {
                ChoiceLogs++;
                ReProbeLog.W("RE_CHOICE_ADD #" + ChoiceLogs + " id='" + Safe(() => choice.id)
                    + "' summary=[" + ReProbeLog.Clip(ReProbeBehaviour._ChoiceText(choice), 120) + "]"
                    + " locked=" + Safe(() => choice.locked) + " nested=" + Safe(() => choice.nested)
                    + " hostedAt=" + Safe(() => choice.hostedAt.ScriptPath + ":" + choice.hostedAt.LineIndex));
            }
            catch (Exception e) { ReProbeLog.E("RE_CHOICE_EX " + e.GetType().Name + ": " + e.Message); }
        }

        internal static void AddMessagePrefix(UITextPrinterPanel __instance, PrintedMessage message)
        {
            try
            {
                if (PrintCalls <= 60)
                    ReProbeLog.W("RE_PANEL_ADDMESSAGE type=" + __instance.GetType().Name + " name='" + __instance.name + "'"
                        + " author={" + AuthorDesc(message) + "} text=[" + ReProbeLog.Clip(Safe(() => (string)message.Text), 160) + "]");
            }
            catch (Exception e) { ReProbeLog.E("RE_PANEL_MSG_EX " + e.Message); }
        }

        // ---- 零绑定风险命中探针：无参 prefix，只能证明方法被调用 ----

        internal static void HitPrint()
        {
            try { PrintHits++; if (PrintHits <= 40) ReProbeLog.W("RE_HIT_PRINT #" + PrintHits + " type=" + "TextPrinterManager.Print"); }
            catch { }
        }

        internal static void HitChoiceAdd()
        {
            try { ChoiceAddHits++; if (ChoiceAddHits <= 40) ReProbeLog.W("RE_HIT_CHOICE_ADD #" + ChoiceAddHits); }
            catch { }
        }

        internal static void HitDebateAddMsg()
        {
            try
            {
                DebateAddMsgHits++;
                if (DebateAddMsgHits <= 40) ReProbeLog.W("RE_HIT_DEBATE_ADDMESSAGE #" + DebateAddMsgHits);
            }
            catch { }
        }

        internal static void HitDebateSetText(string value)
        {
            try
            {
                DebateSetTextHits++;
                if (DebateSetTextHits <= 40)
                    ReProbeLog.W("RE_HIT_DEBATE_SETTEXT #" + DebateSetTextHits + " len=" + (value == null ? -1 : value.Length)
                        + " text=[" + ReProbeLog.Clip(value, 160) + "]");
            }
            catch { }
        }

        internal static void HitModifiedWait(bool visible)
        {
            try
            {
                ModifiedWaitHits++;
                if (ModifiedWaitHits <= 40) ReProbeLog.W("RE_HIT_MODIFIED_WAIT " + visible + " #" + ModifiedWaitHits);
            }
            catch { }
        }

        internal static void HitAdvAuthor()
        {
            try { AdvAuthorHits++; if (AdvAuthorHits <= 40) ReProbeLog.W("RE_HIT_ADV_SETMESSAGEAUTHOR #" + AdvAuthorHits); }
            catch { }
        }

        internal static string AuthorDesc(PrintedMessage message)
        {
            try
            {
                // probe3 实测：旁白时 PrintedMessage.Author（Nullable<MessageAuthor> getter，native token get_Author）会抛 NRE；
                // 但 PrintedMessage 有公开字段 author（MessageAuthor 引用类型），旁白时直接为 null，读字段更稳。
                MessageAuthor a = null;
                string fieldErr = null;
                try { a = message.author; } catch (Exception e) { fieldErr = e.GetType().Name; }
                if (a == null) return fieldErr == null ? "<narration|null>" : "<field-err:" + fieldErr + ">";
                string id = null, label = null;
                try { id = a.Id; } catch { id = "<ERR>"; }
                try { label = (string)a.Label; } catch { label = "<ERR>"; }
                return "id='" + (id ?? "<null>") + "' label='" + ReProbeLog.Clip(label, 40) + "'";
            }
            catch (Exception e) { return "ERR:" + e.GetType().Name; }
        }

        private static bool SafeBool(Func<bool> f)
        {
            try { return f(); } catch { return false; }
        }

        private static string Safe(Func<object> f)
        {
            try { var v = f(); return v == null ? "<null>" : v.ToString(); }
            catch (Exception e) { return "<ERR:" + e.GetType().Name + ">"; }
        }
    }
}


