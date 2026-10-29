using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Naninovel;
using Naninovel.UI;
using System.Reflection;
using UnityEngine;

namespace ManosabaProbe
{
    /// <summary>
    /// 探针插件 v3。
    /// v1：证明 Chainloader 加载 / interop 绑定 / ClassInject / MonoBehaviour 存活。
    /// v2：Engine.Initialized 轮询 + GetService&lt;ITextPrinterManager&gt; + 定时场景树 dump。
    /// v3：不再订阅 OnPrintStarted（il2cpp 拒绝非 blittable struct 参数的托管委托，见日志），
    ///     改为轮询 RevealableText / TextPrinterManager / UI 面板，周期性 dump 场景树与 UI 文案，
    ///     并探测 IChoiceHandlerManager / IScriptPlayer / IUIManager 可用性。
    /// </summary>
    [BepInPlugin(Guid, "ManosabaProbe", "0.4.0.0")]
    public class ProbePlugin : BasePlugin
    {
        public const string Guid = "manosaba.a11y.probe";
        internal static ManualLogSource L;

        public override void Load()
        {
            L = Log;
            L.LogInfo("[ManosabaProbe] ===== plugin Load() 进入 (v0.4.0.0 Harmony) =====");
            try { ProbePatches.Install(); }
            catch (Exception e) { L.LogError("[ManosabaProbe] Harmony 安装阶段异常: " + e); }

            try
            {
                L.LogInfo("[ManosabaProbe] BepInEx core assembly = " + typeof(BasePlugin).Assembly.GetName().Name
                          + " " + typeof(BasePlugin).Assembly.GetName().Version);
                L.LogInfo("[ManosabaProbe] Paths.BepInExRootPath = " + Paths.BepInExRootPath);
                L.LogInfo("[ManosabaProbe] AppDomain assemblies = " + AppDomain.CurrentDomain.GetAssemblies().Length);
            }
            catch (Exception e) { L.LogError("[ManosabaProbe] meta log failed: " + e); }

            try
            {
                L.LogInfo("[ManosabaProbe] UnityEngine.Application.unityVersion = " + Application.unityVersion);
                L.LogInfo("[ManosabaProbe] Application.productName = " + Application.productName);
                L.LogInfo("[ManosabaProbe] Application.version = " + Application.version);
                L.LogInfo("[ManosabaProbe] Application.platform = " + Application.platform);
                L.LogInfo("[ManosabaProbe] Application.dataPath = " + Application.dataPath);
                L.LogInfo("[ManosabaProbe] INTEROP_BINDING_OK");
            }
            catch (Exception e)
            {
                L.LogError("[ManosabaProbe] INTEROP_BINDING_FAILED: " + e);
            }

            bool injected = false;
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<ProbeBehaviour>();
                injected = true;
                L.LogInfo("[ManosabaProbe] CLASSINJECT_OK (RegisterTypeInIl2Cpp<ProbeBehaviour>)");
            }
            catch (Exception e)
            {
                L.LogError("[ManosabaProbe] CLASSINJECT_FAILED: " + e);
            }

            if (!injected)
            {
                L.LogError("[ManosabaProbe] 注入失败，跳过 MonoBehaviour；改为 Load() 内直接 dump 一次");
                try { SceneDumper.Dump("load-fallback"); }
                catch (Exception e) { L.LogError("[ManosabaProbe] fallback dump failed: " + e); }
                return;
            }

            try
            {
                var go = new GameObject("ManosabaProbeHost");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<ProbeBehaviour>();
                L.LogInfo("[ManosabaProbe] == PLUGIN LOADED OK == probe behaviour 已挂到 ManosabaProbeHost");
            }
            catch (Exception e)
            {
                L.LogError("[ManosabaProbe] AddComponent<ProbeBehaviour> 失败: " + e);
            }
        }
    }

    public class ProbeBehaviour : MonoBehaviour
    {
        private int _frames;
        private int _lastBeat = -1;
        private float _initRealtime;
        private bool _engineOk;
        private bool _engineErrLogged;
        private bool _servicesLogged;
        private bool _mgrDetailLogged;
        private int _dumpCount;
        private float _nextDumpAt;
        private float _nextDialogueProbe;
        private float _nextChoiceProbe;
        private string _lastDialogueKey = "";
        private string _lastChoiceKey = "";
        private string _lastScene = "";
        private bool _dialogueDumped;
        private bool _scriptDumped;

        private ITextPrinterManager _printerManager;

        private void Update()
        {
            try
            {
                _frames++;
                if (_frames / 120 != _lastBeat)
                {
                    _lastBeat = _frames / 120;
                    ProbePlugin.L?.LogInfo("[ManosabaProbe] heartbeat frame=" + _frames + " scene='" + SafeSceneName() + "'");
                }

                string scene = SafeSceneName();
                if (_engineOk && scene != _lastScene)
                {
                    _lastScene = scene;
                    ProbePlugin.L?.LogInfo("[ManosabaProbe] SCENE_CHANGED to '" + scene + "' frame=" + _frames);
                    SceneDumper.Dump("scene-" + Sanitize(scene));
                }

                if (!_engineOk)
                {
                    try
                    {
                        if (Engine.Initialized)
                        {
                            _engineOk = true;
                            _initRealtime = Time.realtimeSinceStartup;
                            _lastScene = scene;
                            _nextDumpAt = _initRealtime + 10f;
                            ProbePlugin.L?.LogInfo("[ManosabaProbe] NANINOVEL_ENGINE_INITIALIZED frame=" + _frames
                                                   + " scene='" + scene + "' realtime=" + _initRealtime.ToString("F2"));
                            ResolveServices();
                            SceneDumper.Dump("engine-init");
                        }
                    }
                    catch (Exception e)
                    {
                        if (!_engineErrLogged)
                        {
                            _engineErrLogged = true;
                            ProbePlugin.L?.LogError("[ManosabaProbe] Engine.Initialized 轮询异常: " + e);
                        }
                    }
                }

                if (!_engineOk) return;

                float now = Time.realtimeSinceStartup;

                // 周期性场景树 dump（每 20s，最多 12 次）
                if (now >= _nextDumpAt && _dumpCount < 12)
                {
                    _dumpCount++;
                    _nextDumpAt = now + 20f;
                    SceneDumper.Dump("t" + (int)(now - _initRealtime) + "s");
                }

                // 对话框文本轮询（每 0.5s；文本变化才打日志，避免刷屏）
                if (now >= _nextDialogueProbe)
                {
                    _nextDialogueProbe = now + 0.5f;
                    DialogueProbe();
                }

                // 选项按钮轮询（每 2s）
                if (now >= _nextChoiceProbe)
                {
                    _nextChoiceProbe = now + 2f;
                    ChoiceProbe();
                }

                // 打印管理器细节只打一次（真实类型 / 能否 TryCast / actor 是否可读）
                if (!_mgrDetailLogged && now > _initRealtime + 3f)
                {
                    _mgrDetailLogged = true;
                    ManagerDetailProbe();
                }

                // 第一条台词由 Harmony Print 补丁捕获后，dump 一次 IScriptPlayer 定位（剧本 / 行 / 指令）
                if (!_scriptDumped && ProbePatches.PrintTotal > 0)
                {
                    ScriptDump();
                }
            }
            catch (Exception e)
            {
                ProbePlugin.L?.LogError("[ManosabaProbe] Update 异常: " + e);
            }
        }

        // ---------------- IScriptPlayer 定位（剧本/行/指令） ----------------

        private void ScriptDump()
        {
            _scriptDumped = true;
            var L = ProbePlugin.L;
            try
            {
                var sp = Engine.GetService<IScriptPlayer>();
                if (sp == null) { L?.LogInfo("[ManosabaProbe] SCRIPTDUMP scriptPlayer=null"); return; }
                L?.LogInfo("[ManosabaProbe] SCRIPTDUMP runtimeType=" + sp.GetType().FullName
                           + " playing=" + sp.Playing + " playedIndex=" + sp.PlayedIndex
                           + " completing=" + sp.Completing + " waitingForInput=" + sp.WaitingForInput
                           + " skipActive=" + sp.SkipActive + " autoPlayActive=" + sp.AutoPlayActive);
                try
                {
                    var script = sp.PlayedScript;
                    if (script == null) L?.LogInfo("[ManosabaProbe] SCRIPTDUMP playedScript=null");
                    else
                    {
                        string path = null;
                        try { path = script.Path; } catch (Exception e) { path = "<ERR " + e.GetType().Name + ">"; }
                        string linesCount = "?";
                        try { var lines = script.Lines; linesCount = CountOf(lines); } catch (Exception e) { linesCount = "<ERR " + e.GetType().Name + ">"; }
                        L?.LogInfo("[ManosabaProbe] SCRIPTDUMP playedScript.path='" + path + "' lines=" + linesCount);
                    }
                }
                catch (Exception e) { L?.LogInfo("[ManosabaProbe] SCRIPTDUMP PlayedScript 失败: " + e.GetType().Name + ": " + e.Message); }
                try
                {
                    var spot = sp.PlaybackSpot;
                    L?.LogInfo("[ManosabaProbe] SCRIPTDUMP playbackSpot=" + spot);
                }
                catch (Exception e) { L?.LogInfo("[ManosabaProbe] SCRIPTDUMP PlaybackSpot 失败: " + e.GetType().Name + ": " + e.Message); }
                try
                {
                    var cmd = sp.PlayedCommand;
                    L?.LogInfo("[ManosabaProbe] SCRIPTDUMP playedCommand=" + cmd);
                }
                catch (Exception e) { L?.LogInfo("[ManosabaProbe] SCRIPTDUMP PlayedCommand 失败: " + e.GetType().Name + ": " + e.Message); }
                try
                {
                    var cmds = sp.ExecutingCommands;
                    L?.LogInfo("[ManosabaProbe] SCRIPTDUMP executingCommands=" + (cmds == null ? "<null>" : cmds.GetType().FullName));
                }
                catch (Exception e) { L?.LogInfo("[ManosabaProbe] SCRIPTDUMP ExecutingCommands 失败: " + e.GetType().Name + ": " + e.Message); }
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] SCRIPTDUMP 异常: " + e); }
        }

        /// <summary>interop 的 IReadOnlyList 投影只有索引器，没有 Count 属性 → 先试反射，再退化为索引探测。</summary>
        private static string CountOf<T>(Il2CppSystem.Collections.Generic.IReadOnlyList<T> list)
        {
            if (list == null) return "<null>";
            try
            {
                var p = list.GetType().GetProperty("Count");
                if (p != null) return p.GetValue(list).ToString();
                var f = list.GetType().GetField("size", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return f.GetValue(list).ToString();
            }
            catch { }
            for (int i = 0; i < 20000; i++)
            {
                try { var _ = list[i]; }
                catch { return i + "(byIndex)"; }
            }
            return ">=20000(byIndex)";
        }

        private static string SafeSceneName()
        {
            try { return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name; }
            catch { return "?"; }
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            var sb = new StringBuilder(s.Length);
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }

        // ---------------- 服务可用性 ----------------

        private void ResolveServices()
        {
            var L = ProbePlugin.L;
            if (_servicesLogged) return;
            _servicesLogged = true;

            try
            {
                _printerManager = Engine.GetService<ITextPrinterManager>();
                L?.LogInfo("[ManosabaProbe] PRINTER_MANAGER_OK runtimeType=" + _printerManager.GetType().FullName
                           + " defaultPrinterId='" + _printerManager.DefaultPrinterId
                           + "' baseRevealSpeed=" + _printerManager.BaseRevealSpeed
                           + " baseAutoDelay=" + _printerManager.BaseAutoDelay);
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] GetService<ITextPrinterManager> 失败: " + e); }

            try
            {
                var cm = Engine.GetService<IChoiceHandlerManager>();
                L?.LogInfo("[ManosabaProbe] CHOICE_HANDLER_MANAGER_OK runtimeType=" + (cm == null ? "<null>" : cm.GetType().FullName)
                           + " choiceButtonLoader=" + (cm != null && cm.ChoiceButtonLoader != null));
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] GetService<IChoiceHandlerManager> 失败: " + e); }

            try
            {
                var sp = Engine.GetService<IScriptPlayer>();
                L?.LogInfo("[ManosabaProbe] SCRIPT_PLAYER_OK runtimeType=" + (sp == null ? "<null>" : sp.GetType().FullName)
                           + " playing=" + (sp != null && sp.Playing)
                           + " waitingForInput=" + (sp != null && sp.WaitingForInput)
                           + " playedIndex=" + (sp != null ? sp.PlayedIndex : -1));
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] GetService<IScriptPlayer> 失败: " + e); }

            try
            {
                var ui = Engine.GetService<IUIManager>();
                L?.LogInfo("[ManosabaProbe] UI_MANAGER_OK runtimeType=" + (ui == null ? "<null>" : ui.GetType().FullName)
                           + " fontName='" + (ui != null ? ui.FontName : "?") + "'"
                           + " fontSize=" + (ui != null ? ui.FontSize : -1)
                           + " anyModal=" + (ui != null && ui.AnyModal));
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] GetService<IUIManager> 失败: " + e); }
        }

        private void ManagerDetailProbe()
        {
            var L = ProbePlugin.L;
            if (_printerManager == null) { L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL: printerManager 为空，跳过"); return; }
            try
            {
                L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL runtimeType=" + _printerManager.GetType().FullName);
                var concrete = _printerManager.TryCast<TextPrinterManager>();
                L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL TryCast<TextPrinterManager> = " + (concrete != null));
                if (concrete != null)
                {
                    try
                    {
                        var actor = concrete.GetActor(_printerManager.DefaultPrinterId);
                        L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL GetActor('" + _printerManager.DefaultPrinterId + "') = "
                                   + (actor == null ? "<null>" : actor.GetType().FullName));
                        if (actor != null)
                        {
                            try
                            {
                                var msgs = actor.Messages;
                                L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL Messages != null = " + (msgs != null)
                                           + " type=" + (msgs == null ? "<null>" : msgs.GetType().FullName));
                                for (int i = 0; i < 8; i++)
                                {
                                    try
                                    {
                                        var m = msgs[i];
                                        string txt = "";
                                        try { txt = (string)m.Text; } catch (Exception e3) { txt = "<" + e3.GetType().Name + ">"; }
                                        L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL msg[" + i + "] text=\"" + SceneDumper.Clip(txt, 120)
                                                   + "\" author=" + (m.Author.HasValue ? "yes" : "no"));
                                    }
                                    catch (Exception e3)
                                    {
                                        L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL msg[" + i + "] 读取结束/失败: " + e3.GetType().Name);
                                        break;
                                    }
                                }
                            }
                            catch (Exception e3) { L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL Messages 读取失败: " + e3.GetType().Name + ": " + e3.Message); }

                            try
                            {
                                L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL actor.RevealProgress=" + actor.RevealProgress.ToString("F2"));
                            }
                            catch (Exception e3) { L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL RevealProgress 读取失败: " + e3.GetType().Name); }
                        }
                    }
                    catch (Exception e2) { L?.LogInfo("[ManosabaProbe] MANAGER_DETAIL GetActor 失败: " + e2.GetType().Name + ": " + e2.Message); }
                }
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] MANAGER_DETAIL 失败: " + e); }
        }

        // ---------------- 对话框文本 ----------------

        private void DialogueProbe()
        {
            var L = ProbePlugin.L;
            try
            {
                var rts = UnityEngine.Object.FindObjectsOfType<RevealableText>(true);
                if (rts == null || rts.Length == 0) return;
                var sb = new StringBuilder();
                string key = "";
                for (int i = 0; i < rts.Length; i++)
                {
                    try
                    {
                        var rt = rts[i];
                        if (rt == null) continue;
                        string txt = null;
                        try { txt = rt.Text; } catch (Exception e2) { txt = "<" + e2.GetType().Name + ">"; }
                        if (string.IsNullOrWhiteSpace(txt)) continue;
                        string path = SceneDumper.PathOf(rt.transform);
                        float alpha = SceneDumper.EffectiveAlpha(rt.transform);
                        float prog = -1f;
                        try { prog = rt.RevealProgress; } catch { }
                        sb.AppendLine("DIALOGUE frame=" + _frames + " alpha=" + alpha.ToString("F2")
                                      + " revealProgress=" + prog.ToString("F2")
                                      + " path=" + path
                                      + " text=\"" + SceneDumper.Clip(txt, 160) + "\"");
                        key += path + "|" + txt + ";";
                    }
                    catch (Exception e2)
                    {
                        sb.AppendLine("DIALOGUE frame=" + _frames + " !! item " + i + " " + e2.GetType().Name + ": " + e2.Message);
                    }
                }
                if (sb.Length == 0) return;
                if (key != _lastDialogueKey)
                {
                    _lastDialogueKey = key;
                    L?.LogInfo("[ManosabaProbe] DIALOGUE_CHANGED\n" + sb);
                    if (!_dialogueDumped)
                    {
                        _dialogueDumped = true;
                        SceneDumper.Dump("dialogue-detected");
                    }
                }
            }
            catch (Exception e)
            {
                ProbePlugin.L?.LogError("[ManosabaProbe] DialogueProbe 失败: " + e.GetType().Name + ": " + e.Message);
            }
        }

        // ---------------- 选项按钮 ----------------

        private void ChoiceProbe()
        {
            var L = ProbePlugin.L;
            try
            {
                var buttons = UnityEngine.Object.FindObjectsOfType<ChoiceHandlerButton>(true);
                var sb = new StringBuilder();
                string key = "";
                int n = 0;
                for (int i = 0; i < buttons.Length; i++)
                {
                    try
                    {
                        var b = buttons[i];
                        if (b == null) continue;
                        string path = SceneDumper.PathOf(b.transform);
                        float alpha = SceneDumper.EffectiveAlpha(b.transform);
                        bool aih = b.gameObject.activeInHierarchy;
                        string texts = SceneDumper.CollectChildTexts(b.transform, 3);
                        sb.AppendLine("CHOICE frame=" + _frames + " activeInHierarchy=" + aih
                                      + " alpha=" + alpha.ToString("F2")
                                      + " path=" + path
                                      + " texts={" + texts + "}");
                        key += path + "|" + texts + ";";
                        n++;
                    }
                    catch (Exception e2)
                    {
                        sb.AppendLine("CHOICE frame=" + _frames + " !! item " + i + " " + e2.GetType().Name);
                    }
                }
                if (n > 0 && key != _lastChoiceKey)
                {
                    _lastChoiceKey = key;
                    L?.LogInfo("[ManosabaProbe] CHOICE_VISIBLE count=" + n + "\n" + sb);
                }
            }
            catch (Exception e)
            {
                ProbePlugin.L?.LogError("[ManosabaProbe] ChoiceProbe 失败: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }

    internal static class SceneDumper
    {
        private const int MaxLines = 700;
        private const int MaxTextHarvest = 150;

        internal static void Dump(string why)
        {
            var sb = new StringBuilder();
            int lineCount = 0;
            sb.AppendLine("# ManosabaProbe scene dump reason=" + why + " time=" + DateTime.Now.ToString("s"));
            sb.AppendLine("# unity=" + Application.unityVersion + " product=" + Application.productName);
            try
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                sb.AppendLine("# activeScene name='" + scene.name + "' path='" + scene.path + "' isLoaded=" + scene.isLoaded);
                var roots = scene.GetRootGameObjects();
                sb.AppendLine("# rootGameObjects=" + roots.Length);
                lineCount += 4;
                for (int i = 0; i < roots.Length && lineCount < MaxLines; i++)
                {
                    try { lineCount += Walk(roots[i].transform, 0, sb, MaxLines - lineCount); }
                    catch (Exception e) { sb.AppendLine("  !! walk error: " + e.GetType().Name + ": " + e.Message); lineCount++; }
                }
            }
            catch (Exception e)
            {
                sb.AppendLine("!! scene dump failed: " + e);
            }

            // UI 文本收割：打印机/对话框优先，SaveLoad 的 "Empty"/"NO DATA" 噪音跳过
            try
            {
                var tmp = UnityEngine.Object.FindObjectsOfType<TMPro.TMP_Text>(true);
                var priority = new List<string>();
                var rest = new List<string>();
                for (int i = 0; i < tmp.Length; i++)
                {
                    try
                    {
                        var t = tmp[i];
                        if (t == null) continue;
                        string s = t.text;
                        if (string.IsNullOrWhiteSpace(s)) continue;
                        string path = PathOf(t.transform);
                        if (path.Contains("SaveLoadUI") && (s.Contains("Empty") || s.Contains("NO DATA"))) continue;
                        string line = "TEXT " + (t.gameObject.activeInHierarchy ? "A" : "I") + " " + path
                                      + " <" + t.GetType().Name + "> = \"" + Clip(s, 200) + "\"";
                        if (path.Contains("TextPrinter") || path.Contains("WitchBook") || path.Contains("Choice")
                            || path.Contains("Trial") || path.Contains("Debate")) priority.Add(line);
                        else rest.Add(line);
                    }
                    catch { }
                }
                sb.AppendLine("# ---- UI TEXT HARVEST priority=" + priority.Count + " rest=" + rest.Count + " ----");
                int written = 0;
                foreach (var l in priority) { if (written++ >= MaxTextHarvest) break; sb.AppendLine(l); }
                foreach (var l in rest) { if (written++ >= MaxTextHarvest) break; sb.AppendLine(l); }
            }
            catch (Exception e) { sb.AppendLine("# UI text harvest failed: " + e); }

            string outDir = Path.Combine(Paths.BepInExRootPath, "ManosabaProbe");
            try
            {
                Directory.CreateDirectory(outDir);
                string path = Path.Combine(outDir, "scene-tree-" + why + ".txt");
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
                ProbePlugin.L?.LogInfo("[ManosabaProbe] scene dump written: " + path + " (" + sb.Length + " chars)");
            }
            catch (Exception e)
            {
                ProbePlugin.L?.LogError("[ManosabaProbe] scene dump write failed: " + e);
            }

            try
            {
                string[] lines = sb.ToString().Split('\n');
                int n = Math.Min(lines.Length, 100);
                for (int i = 0; i < n; i++) ProbePlugin.L?.LogInfo("[TREE] " + lines[i].TrimEnd('\r'));
                ProbePlugin.L?.LogInfo("[ManosabaProbe] [TREE] ... 共 " + lines.Length + " 行，完整文件见 BepInEx\\ManosabaProbe\\scene-tree-" + why + ".txt");
            }
            catch (Exception e) { ProbePlugin.L?.LogError("[ManosabaProbe] tree log failed: " + e); }
        }

        private static int Walk(Transform t, int depth, StringBuilder sb, int remaining)
        {
            int used = 0;
            var go = t.gameObject;
            string indent = new string(' ', Math.Min(depth * 2, 40));
            var line = new StringBuilder();
            line.Append(indent).Append(go.name).Append(go.activeSelf ? "" : " [inactive]").Append(" {");
            try
            {
                var comps = go.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    var c = comps[i];
                    if (c == null) { line.Append("null,"); continue; }
                    line.Append(c.GetType().Name);
                    try
                    {
                        if (c is UnityEngine.UI.Text ut) line.Append("('" + Clip(ut.text, 60) + "')");
                        else if (c is TMPro.TMP_Text tt) line.Append("('" + Clip(tt.text, 60) + "')");
                    }
                    catch { }
                    line.Append(',');
                }
            }
            catch (Exception e) { line.Append("!comps:" + e.GetType().Name); }
            line.Append('}');

            try
            {
                var rt = t.TryCast<RectTransform>();
                if (rt != null)
                {
                    var sd = rt.sizeDelta; var ap = rt.anchoredPosition;
                    line.Append(" [RT sizeDelta=").Append(sd.x.ToString("F0")).Append('x').Append(sd.y.ToString("F0"))
                        .Append(" pos=").Append(ap.x.ToString("F0")).Append(',').Append(ap.y.ToString("F0"))
                        .Append(" activeSelf=").Append(rt.gameObject.activeSelf)
                        .Append(" activeInHierarchy=").Append(rt.gameObject.activeInHierarchy).Append(']');
                }
            }
            catch { }

            try
            {
                var cg = go.TryCast<CanvasGroup>();
                if (cg != null)
                {
                    line.Append(" [CG alpha=").Append(cg.alpha.ToString("F2"))
                        .Append(" interactable=").Append(cg.interactable)
                        .Append(" blocksRaycasts=").Append(cg.blocksRaycasts).Append(']');
                }
            }
            catch { }

            sb.AppendLine(line.ToString());
            used++;
            if (used >= remaining) return used;
            int childCount = 0;
            try { childCount = t.childCount; } catch { }
            for (int i = 0; i < childCount; i++)
            {
                if (used >= remaining) break;
                try { used += Walk(t.GetChild(i), depth + 1, sb, remaining - used); }
                catch (Exception e) { sb.AppendLine(indent + "  !! child error " + e.GetType().Name); used++; }
            }
            return used;
        }

        internal static string PathOf(Transform t)
        {
            if (t == null) return "<null>";
            var parts = new List<string>();
            try
            {
                while (t != null) { parts.Add(t.gameObject.name); t = t.parent; }
            }
            catch { }
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>沿父链找第一个 CanvasGroup，返回其 alpha（找不到返回 1）。UiNav 判断可见性用这个。</summary>
        internal static float EffectiveAlpha(Transform t)
        {
            try
            {
                for (int depth = 0; t != null && depth < 6; depth++)
                {
                    var cg = t.gameObject.TryCast<CanvasGroup>();
                    if (cg != null) return cg.alpha;
                    t = t.parent;
                }
            }
            catch { }
            return 1f;
        }

        internal static string CollectChildTexts(Transform t, int maxDepth)
        {
            var sb = new StringBuilder();
            try { CollectChildTexts(t, maxDepth, 0, sb); } catch { }
            return sb.ToString();
        }

        private static void CollectChildTexts(Transform t, int maxDepth, int depth, StringBuilder sb)
        {
            if (t == null || depth > maxDepth) return;
            try
            {
                var tmp = t.gameObject.TryCast<TMPro.TMP_Text>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                {
                    if (sb.Length > 0) sb.Append(" | ");
                    sb.Append('"').Append(Clip(tmp.text, 120)).Append('"');
                }
                int n = t.childCount;
                for (int i = 0; i < n; i++) CollectChildTexts(t.GetChild(i), maxDepth, depth + 1, sb);
            }
            catch { }
        }

        internal static string Clip(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\r", "\\r").Replace("\n", "\\n");
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }

    /// <summary>
    /// Harmony 补丁集合：只观测、不改行为。
    /// 之所以用 Harmony 而不是 ITextPrinterManager.OnPrintStarted：
    /// il2cpp 委托转换拒绝 non-blittable struct 参数（见 env-facts.md G10）。
    /// 每个补丁单独 try/catch 安装，失败会把异常原文打进日志（PATCH_FAIL），绝不静默跳过。
    /// </summary>
    internal static class ProbePatches
    {
        internal static Harmony Instance;
        internal static int PrintTotal;
        internal static int SetTextTotal;
        internal static int AuthorTotal;
        internal static int ChoiceTotal;
        internal static int BaseAddMessageTotal;
        internal static int BaseSetMessagesTotal;
        internal static int DebateAddMessageTotal;
        internal static int DebateSetTextTotal;
        internal static int DebateSetMessagesTotal;

        private static readonly Dictionary<string, int> PrintTexts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> SetTexts = new Dictionary<string, int>();

        internal static void Install()
        {
            var L = ProbePlugin.L;
            try { Instance = new Harmony("manosaba.a11y.probe.harmony"); }
            catch (Exception e) { L?.LogError("[ManosabaProbe] PATCH_FAIL Harmony 构造函数: " + e); return; }

            var plan = new (string label, Type type, string method, string postfix)[]
            {
                ("TextPrinterManager.Print", typeof(TextPrinterManager), "Print", nameof(Print_Post)),
                ("RevealableText.set_Text", typeof(RevealableText), "set_Text", nameof(SetText_Post)),
                ("WitchTrialsTextPrinterPanel.SetMessageAuthor", typeof(WitchTrials.Views.WitchTrialsTextPrinterPanel), "SetMessageAuthor", nameof(Author_Post)),
                ("DebateTextPrinterPanel.SetText", typeof(WitchTrials.Views.DebateTextPrinterPanel), "SetText", nameof(DebateSetText_Post)),
                ("DebateTextPrinterPanel.AddMessage", typeof(WitchTrials.Views.DebateTextPrinterPanel), "AddMessage", nameof(DebateAddMessage_Post)),
                ("DebateTextPrinterPanel.SetMessages", typeof(WitchTrials.Views.DebateTextPrinterPanel), "SetMessages", nameof(DebateSetMessages_Post)),
                ("UITextPrinterPanel.AddMessage", typeof(Naninovel.UI.UITextPrinterPanel), "AddMessage", nameof(BaseAddMessage_Post)),
                ("UITextPrinterPanel.SetMessages", typeof(Naninovel.UI.UITextPrinterPanel), "SetMessages", nameof(BaseSetMessages_Post)),
                ("ChoiceHandlerPanel.AddChoiceButton", typeof(ChoiceHandlerPanel), "AddChoiceButton", nameof(Choice_Post)),
            };

            int ok = 0, fail = 0;
            foreach (var p in plan)
            {
                try
                {
                    var target = AccessTools.Method(p.type, p.method);
                    if (target == null)
                    {
                        fail++;
                        L?.LogError("[ManosabaProbe] PATCH_FAIL " + p.label + ": AccessTools.Method 返回 null（方法名/重载不存在）");
                        continue;
                    }
                    var pmi = AccessTools.Method(typeof(ProbePatches), p.postfix);
                    if (pmi == null)
                    {
                        fail++;
                        L?.LogError("[ManosabaProbe] PATCH_FAIL " + p.label + ": 找不到 postfix " + p.postfix);
                        continue;
                    }
                    Instance.Patch(target, postfix: new HarmonyMethod(pmi));
                    ok++;
                    L?.LogInfo("[ManosabaProbe] PATCH_OK " + p.label + " target=" + target.DeclaringType?.FullName + "." + target.Name + "(" + ParamSig(target) + ")");
                }
                catch (Exception e)
                {
                    fail++;
                    L?.LogError("[ManosabaProbe] PATCH_FAIL " + p.label + ": " + e.GetType().Name + ": " + e.Message);
                }
            }
            L?.LogInfo("[ManosabaProbe] PATCH_SUMMARY ok=" + ok + " fail=" + fail);
        }

        private static string ParamSig(MethodBase m)
        {
            try
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
            catch { return "?"; }
        }

        private static object ReadMember(object o, string name)
        {
            if (o == null) return null;
            try
            {
                var t = o.GetType();
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (p != null) return p.GetValue(o);
                var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null) return f.GetValue(o);
            }
            catch { }
            return null;
        }

        private static string AsString(object o)
        {
            if (o == null) return "<null>";
            try
            {
                var s = o as string;
                if (s != null) return s;
                var op = o.GetType().GetMethod("op_Implicit", new[] { o.GetType() });
                if (op != null && op.ReturnType == typeof(string)) return (string)op.Invoke(null, new[] { o });
            }
            catch { }
            try { return o.ToString(); } catch { return "<no-toString>"; }
        }

        /// <summary>PrintedMessage.Author 是 Il2CppSystem.Nullable&lt;MessageAuthor&gt;；逐字段反射读，读不到就记异常名。</summary>
        private static string AuthorInfo(Naninovel.PrintedMessage m)
        {
            try
            {
                var a = m.Author;
                if (a == null) return "<author-null>";
                var has = ReadMember(a, "hasValue");
                bool hasValue = !(has is bool) || (bool)has;
                if (!hasValue) return "<none>";
                var v = ReadMember(a, "value");
                if (v == null) return "<none>";
                string id = AsString(ReadMember(v, "Id"));
                string label = AsString(ReadMember(v, "Label"));
                return "id=" + id + " label=" + label;
            }
            catch (Exception e) { return "<ERR " + e.GetType().Name + ": " + e.Message + ">"; }
        }

        private static string TextOf(Naninovel.PrintedMessage m)
        {
            try { return (string)m.Text; }
            catch (Exception e)
            {
                try { return "<ERR " + e.GetType().Name + ": " + e.Message + ">"; }
                catch { return "<ERR>"; }
            }
        }

        private static int Bump(Dictionary<string, int> d, string key)
        {
            if (key == null) key = "<null>";
            int c;
            if (d.TryGetValue(key, out c)) { c++; d[key] = c; }
            else { c = 1; d[key] = 1; }
            return c;
        }

        // ---- 1) 打印总入口 ----
        internal static void Print_Post(string printerId, Naninovel.PrintedMessage message, bool append, float speed)
        {
            var L = ProbePlugin.L;
            try
            {
                PrintTotal++;
                string text = TextOf(message);
                int dup = Bump(PrintTexts, text);
                L?.LogInfo("[ManosabaProbe] PRINT #" + PrintTotal + " printerId=" + printerId
                           + " append=" + append + " speed=" + speed
                           + " author={" + AuthorInfo(message) + "} dup=" + dup
                           + " hasPipeHash=" + (text != null && text.Contains("|#"))
                           + " text=[" + SceneDumper.Clip(text, 300) + "]");
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] PRINT_Post 异常: " + e); }
        }

        // ---- 2) 面板文字设置 ----
        internal static void SetText_Post(RevealableText __instance, string value)
        {
            var L = ProbePlugin.L;
            try
            {
                SetTextTotal++;
                int dup = Bump(SetTexts, value);
                if (SetTextTotal > 40 && dup == 1) return;   // 前 40 次全打，之后只打新文本
                string panel = __instance == null ? "<null>" : SceneDumper.PathOf(__instance.transform);
                L?.LogInfo("[ManosabaProbe] SETTEXT #" + SetTextTotal + " type=" + (__instance == null ? "?" : __instance.GetType().Name)
                           + " dup=" + dup + " path=" + panel + " text=[" + SceneDumper.Clip(value, 300) + "]");
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] SETTEXT_Post 异常: " + e); }
        }

        // ---- 3) 说话人 ----
        internal static void Author_Post(Naninovel.MessageAuthor author)
        {
            var L = ProbePlugin.L;
            try
            {
                AuthorTotal++;
                if (AuthorTotal > 5) return;
                string id = null, label = null;
                try { id = author.Id; } catch (Exception e) { id = "<ERR " + e.GetType().Name + ">"; }
                try { label = (string)author.Label; } catch (Exception e) { label = "<ERR " + e.GetType().Name + ">"; }
                L?.LogInfo("[ManosabaProbe] AUTHOR #" + AuthorTotal + " id=" + id + " label=" + label);
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] AUTHOR_Post 异常: " + e); }
        }

        // ---- 4) 论战面板 ----
        internal static void DebateSetText_Post(WitchTrials.Views.DebateTextPrinterPanel __instance, string text)
        {
            var L = ProbePlugin.L;
            try
            {
                DebateSetTextTotal++;
                L?.LogInfo("[ManosabaProbe] DEBATE_SETTEXT #" + DebateSetTextTotal + " path="
                           + (__instance == null ? "<null>" : SceneDumper.PathOf(__instance.transform))
                           + " text=[" + SceneDumper.Clip(text, 200) + "]");
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] DEBATE_SETTEXT 异常: " + e); }
        }

        internal static void DebateAddMessage_Post(WitchTrials.Views.DebateTextPrinterPanel __instance, Naninovel.PrintedMessage message)
        {
            var L = ProbePlugin.L;
            try
            {
                DebateAddMessageTotal++;
                L?.LogInfo("[ManosabaProbe] DEBATE_ADDMESSAGE #" + DebateAddMessageTotal + " path="
                           + (__instance == null ? "<null>" : SceneDumper.PathOf(__instance.transform))
                           + " author={" + AuthorInfo(message) + "} text=[" + SceneDumper.Clip(TextOf(message), 200) + "]");
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] DEBATE_ADDMESSAGE 异常: " + e); }
        }

        internal static void DebateSetMessages_Post(WitchTrials.Views.DebateTextPrinterPanel __instance, Il2CppSystem.Collections.Generic.IReadOnlyList<Naninovel.PrintedMessage> messages)
        {
            var L = ProbePlugin.L;
            try
            {
                DebateSetMessagesTotal++;
                L?.LogInfo("[ManosabaProbe] DEBATE_SETMESSAGES #" + DebateSetMessagesTotal + " path="
                           + (__instance == null ? "<null>" : SceneDumper.PathOf(__instance.transform))
                           + " count=" + SafeCount(messages) + " items=[" + JoinMessages(messages, 3) + "]");
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] DEBATE_SETMESSAGES 异常: " + e); }
        }

        // ---- 5) 基类面板（没被 override 的面板走这里）----
        internal static void BaseAddMessage_Post(Naninovel.UI.UITextPrinterPanel __instance, Naninovel.PrintedMessage message)
        {
            var L = ProbePlugin.L;
            try
            {
                BaseAddMessageTotal++;
                L?.LogInfo("[ManosabaProbe] BASE_ADDMESSAGE #" + BaseAddMessageTotal + " type="
                           + (__instance == null ? "<null>" : __instance.GetType().Name)
                           + " path=" + (__instance == null ? "<null>" : SceneDumper.PathOf(__instance.transform))
                           + " author={" + AuthorInfo(message) + "} text=[" + SceneDumper.Clip(TextOf(message), 200) + "]");
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] BASE_ADDMESSAGE 异常: " + e); }
        }

        internal static void BaseSetMessages_Post(Naninovel.UI.UITextPrinterPanel __instance, Il2CppSystem.Collections.Generic.IReadOnlyList<Naninovel.PrintedMessage> messages)
        {
            var L = ProbePlugin.L;
            try
            {
                BaseSetMessagesTotal++;
                L?.LogInfo("[ManosabaProbe] BASE_SETMESSAGES #" + BaseSetMessagesTotal + " type="
                           + (__instance == null ? "<null>" : __instance.GetType().Name)
                           + " path=" + (__instance == null ? "<null>" : SceneDumper.PathOf(__instance.transform))
                           + " count=" + SafeCount(messages) + " items=[" + JoinMessages(messages, 3) + "]");
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] BASE_SETMESSAGES 异常: " + e); }
        }

        // ---- 6) 选项 ----
        internal static void Choice_Post(ChoiceHandlerPanel __instance, Naninovel.ChoiceState choice)
        {
            var L = ProbePlugin.L;
            try
            {
                ChoiceTotal++;
                string id = null, summary = null, buttonPath = null;
                bool locked = false;
                try { id = choice.Id; } catch (Exception e) { id = "<ERR " + e.GetType().Name + ">"; }
                try { summary = (string)choice.Summary; } catch (Exception e) { summary = "<ERR " + e.GetType().Name + ": " + e.Message + ">"; }
                try { locked = choice.Locked; } catch { }
                try { buttonPath = choice.ButtonPath; } catch { }

                string fieldProbe = "none";
                try
                {
                    var prop = typeof(ChoiceHandlerPanel).GetProperty("Buttons", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var fld = typeof(ChoiceHandlerPanel).GetField("choiceButtons", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    fieldProbe = "ButtonsProp=" + (prop != null) + " choiceButtonsField=" + (fld != null);
                }
                catch (Exception e) { fieldProbe = "<ERR " + e.GetType().Name + ">"; }

                int liveButtons = -1;
                try { liveButtons = UnityEngine.Object.FindObjectsOfType<ChoiceHandlerButton>(true).Length; } catch { }

                L?.LogInfo("[ManosabaProbe] CHOICE #" + ChoiceTotal + " id=" + id + " summary=[" + SceneDumper.Clip(summary, 200) + "]"
                           + " locked=" + locked + " buttonPath=" + buttonPath
                           + " panel=" + (__instance == null ? "<null>" : SceneDumper.PathOf(__instance.transform))
                           + " liveButtons=" + liveButtons + " fieldProbe=" + fieldProbe);
            }
            catch (Exception e) { L?.LogError("[ManosabaProbe] CHOICE_Post 异常: " + e); }
        }

        // ---- 工具 ----
        private static string SafeCount(Il2CppSystem.Collections.Generic.IReadOnlyList<Naninovel.PrintedMessage> list)
        {
            if (list == null) return "<null>";
            try
            {
                var c = ReadMember(list, "Count");
                if (c is int) return ((int)c).ToString();
                // 退化：逐个索引探测（IReadOnlyList 投影只有索引器）
                int n = 0;
                for (int i = 0; i < 64; i++)
                {
                    try { var _ = list[i]; n++; }
                    catch { break; }
                }
                return n + "(byIndex)";
            }
            catch (Exception e) { return "<ERR " + e.GetType().Name + ">"; }
        }

        private static string JoinMessages(Il2CppSystem.Collections.Generic.IReadOnlyList<Naninovel.PrintedMessage> list, int max)
        {
            if (list == null) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < max; i++)
            {
                try
                {
                    var m = list[i];
                    if (sb.Length > 0) sb.Append(" || ");
                    sb.Append("#").Append(i).Append("{" + AuthorInfo(m) + "}=[").Append(SceneDumper.Clip(TextOf(m), 200)).Append(']');
                }
                catch { break; }
            }
            return sb.ToString();
        }
    }
}
