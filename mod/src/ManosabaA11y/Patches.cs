using System;
using System.Reflection;
using HarmonyLib;
using Naninovel;
using UnityEngine;

namespace ManosabaA11y
{
    /// <summary>
    /// Harmony 补丁集中点（L3 逐作层）。
    ///
    /// ★ 每条补丁的依据都必须写清「哪个程序集/类型/方法、怎么验证的」，
    ///   不许猜方法名（Framework 三条铁律之一）。
    ///
    /// 本作两条打印链（witchtrials-re re\witchtrials-ui.md §1，E1 级证据）：
    ///   1) ADV：Naninovel.TextPrinterManager.Print(string printerId, PrintedMessage message, ...)
    ///      —— 权威打印点，逐字显示不经过它，所以一条台词只触发一次。
    ///   2) 辩论：WitchTrials.Views.DebateTextPrinter : Naninovel.UITextPrinter 覆写了打印，
    ///      文本进 DebateTextPrinterPanel 的 StringBuilder 后 SetText(string) 整体刷新，
    ///      另有 AddText(string) 单句追加入口 —— 两个都要挂，并对 SetText 做增量差分。
    ///
    /// ★ IL2CPP 下不能订阅 ITextPrinterManager.OnPrintStarted：
    ///   实测 `DelegateSupport.ConvertDelegate<Il2CppSystem.Action<PrintMessageArgs>>` 抛
    ///   "Delegate has parameter of type Naninovel.PrintMessageArgs (non-blittable struct) which is not supported"
    ///   （env-probe run3，re\probe-v3-findings.md）→ 只能走 Harmony。
    /// </summary>
    internal static class Patches
    {
        // ==================== ADV：权威打印点 ====================

        /// <summary>
        /// Naninovel.TextPrinterManager.Print（Elringus.Naninovel.Runtime.dll 实查签名）：
        ///   public unsafe virtual UniTask Print(string printerId, PrintedMessage message,
        ///       bool append = false, float speed = 1f, AsyncToken token = null)
        ///
        /// ★ 必须是 **Prefix** 而不是 Postfix（run12 实机证据）：
        ///   Postfix 阶段 message 里的 LocalizableText 已被 Release，
        ///   `msg.Text` 与 `msg.text` 都抛 NullReferenceException（日志 Reader.TextOf.prop/.field 都报 NRE），
        ///   而 naninovel-re probe4 用 Prefix 能稳定拿到
        ///   text=[沾满鲜血的会客厅内，<br>少女们陷入了恐慌。]。
        ///   所以要在游戏消费/释放之前取文本。
        /// </summary>
        // ==================== ADV：说话人（唯一安全的入口）====================
        //
        // ★★ 惨痛教训（2026-10-29 实机，见 CHANGELOG「Print 补丁会杀死异步协程」）★★
        //   曾经在这里挂过 `Naninovel.TextPrinterManager.Print` 的 Prefix 来拿 PrintedMessage，
        //   结果**整部游戏的剧情卡死在第一句**：
        //     System.MethodAccessException: Attempt to access method
        //       'Naninovel.Async.IAwaiter.get_IsCompleted' on type 'Naninovel.UniTask' failed.
        //       at Naninovel.Commands.PrintText.Print(...)
        //   原因：`Print` 的返回类型是 **UniTask（非 blittable 结构体）**，Harmony 的原生 detour
        //   把返回值搬坏，调用方的 async 状态机 await 时直接抛异常，命令永远不结束
        //   （实机表现就是 playing=True / waiting=False 死等，任何输入都推不动）。
        //   二分实机证据：MANOSABA_A11Y_NOPATCH=print → 剧情恢复正常推进（Reader.say 连续 9 句）。
        //   结论：**永远不要 patch 返回 UniTask / Task / 非 blittable 结构体的 Naninovel 方法。**
        //   文本改由 RevealableText.set_Text（void，同参考实现 noexistence 的主钩子）承担。
        //
        // 说话人则挂这个：WitchTrialsTextPrinterPanel.SetMessageAuthor(MessageAuthor)（void）。
        // 依据：naninovel-re probe3 实测该点 PATCH_OK 且每条台词触发（ADV 真正的说话人入口）；
        //       签名见 re\decompiled\signatures\witchtrials\WitchTrialsTextPrinterPanel.txt。
        internal static void ApplyAdvAuthor(Harmony harmony)
        {
            Type panel = null;
            try { panel = Type.GetType("WitchTrials.Views.WitchTrialsTextPrinterPanel, Assembly-CSharp"); }
            catch (Exception e) { A11yHost.Diag("Patches.advAuthor type: " + e.Message); }
            if (panel == null)
            {
                Plugin.L.LogWarning("[a11y] WitchTrialsTextPrinterPanel 未找到，ADV 说话人朗读会退化为无名");
                return;
            }

            try
            {
                MethodInfo m = AccessTools.Method(panel, "SetMessageAuthor",
                    new Type[] { typeof(Naninovel.MessageAuthor) });
                if (m == null) { Plugin.L.LogWarning("[a11y] 未找到 SetMessageAuthor"); return; }
                harmony.Patch(m, postfix: new HarmonyMethod(
                    typeof(Patches).GetMethod(nameof(AdvAuthorPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Plugin.L.LogInfo("[a11y] patched " + panel.Name + ".SetMessageAuthor");
            }
            catch (Exception e) { Plugin.L.LogWarning("[a11y] patch SetMessageAuthor failed: " + e.Message); }
        }

        /// <summary>
        /// Naninovel.UI.RevealableTextPrinterPanel.SetAuthorNameText(string) 的 Postfix。
        ///
        /// 为什么还要这一条：SetMessageAuthor 拿到的 MessageAuthor 在本作里实测 id/label 都是空
        /// （run fix2 日志 36 次 `HOOK advAuthor id= label=`），而真正显示在名字框里的是本地化后的
        /// 中文名（「樱羽艾玛」）。SetAuthorNameText 是**最终显示串**，直接拿来念最稳。
        /// 签名依据：re\decompiled\signatures\naninovel-subset\Naninovel.UI.RevealableTextPrinterPanel.txt
        ///   public unsafe virtual void SetAuthorNameText(string text)
        /// 它是 void + string，detour 安全（对比 Print 的 UniTask 返回值事故）。
        /// </summary>
        internal static void ApplyAdvAuthorName(Harmony harmony)
        {
            try
            {
                MethodInfo m = AccessTools.Method(typeof(Naninovel.UI.RevealableTextPrinterPanel),
                    "SetAuthorNameText", new Type[] { typeof(string) });
                if (m == null) { Plugin.L.LogWarning("[a11y] 未找到 SetAuthorNameText"); return; }
                harmony.Patch(m, postfix: new HarmonyMethod(
                    typeof(Patches).GetMethod(nameof(AdvAuthorNamePostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Plugin.L.LogInfo("[a11y] patched RevealableTextPrinterPanel.SetAuthorNameText");
            }
            catch (Exception e) { Plugin.L.LogWarning("[a11y] patch SetAuthorNameText failed: " + e.Message); }
        }

        private static void AdvAuthorNamePostfix([HarmonyArgument(0)] string text)
        {
            try { Reader.OnAdvAuthorName(text); }
            catch (Exception e) { A11yHost.Diag("Patches.AdvAuthorNamePostfix: " + e.Message); }
        }
        private static void AdvAuthorPostfix(Naninovel.MessageAuthor __0)
        {
            try { Reader.OnAdvAuthor(__0); }
            catch (Exception e) { A11yHost.Diag("Patches.AdvAuthorPostfix: " + e.Message); }
        }
        // ==================== ADV：面板级兜底（带可见性闸门）====================

        /// <summary>
        /// Naninovel.UI.UITextPrinterPanel.AddMessage(PrintedMessage)：
        /// 面板级追加入口（env-probe probe v3 实测可挂：PATCH_OK UITextPrinterPanel.AddMessage）。
        /// 比 actor 级多拿到面板 Transform，可以挡掉隐藏面板；与 Print 用同一去重窗口。
        /// 注意：辩论面板 DebateTextPrinterPanel 覆写了 AddMessage，基类补丁接不到它，
        /// 辩论走下面 ApplyManual 挂的 SetText/AddText。
        /// </summary>
        [HarmonyPatch(typeof(Naninovel.UI.UITextPrinterPanel), "AddMessage")]
        [HarmonyPatch(new Type[] { typeof(Naninovel.PrintedMessage) })]
        internal static class Patch_PanelAddMessage
        {
            [HarmonyPostfix]
            internal static void Postfix(Naninovel.UI.UITextPrinterPanel __instance,
                                        [HarmonyArgument(0)] Naninovel.PrintedMessage message)
            {
                Reader.OnPanelAddMessage(__instance, message);
            }
        }

        // ==================== ADV：printer 级 AddMessage（行 id 的唯一可靠来源）====================

        /// <summary>
        /// Naninovel.UITextPrinter.AddMessage(PrintedMessage)。
        ///
        /// 为什么必须是它（2026-10-29 run56 实机定位）：
        ///   本作 ADV 路径**从不调用** `Naninovel.UI.UITextPrinterPanel.AddMessage`
        ///   （给面板挂的补丁一条日志都没有），真正被调的是 actor 级的这一条。
        ///   没有它就拿不到 `PrintedMessage`，也就拿不到 `Text.parts[i].id`（行 id）——
        ///   配音闸门（有配音的行不自动朗读）完全无从判断。
        ///
        /// 为什么这次可以挂（对比被删除的 Print 补丁）：
        ///   `UITextPrinter.AddMessage` 的返回类型是 **void**（ilspycmd 实查），
        ///   Harmony 原生 detour 不需要搬非 blittable 的返回值，
        ///   不会重演 `TextPrinterManager.Print` 返回 `UniTask` 导致 async 状态机
        ///   `MethodAccessException`、整条剧情卡死的那个事故。
        /// </summary>
        [HarmonyPatch(typeof(Naninovel.UITextPrinter), "AddMessage")]
        [HarmonyPatch(new Type[] { typeof(Naninovel.PrintedMessage) })]
        internal static class Patch_PrinterAddMessage
        {
            [HarmonyPostfix]
            internal static void Postfix(Naninovel.UITextPrinter __instance,
                                        [HarmonyArgument(0)] Naninovel.PrintedMessage message)
            {
                try { Reader.OnPrinterAddMessage(message); }
                catch (Exception e) { A11yHost.Diag("Patches.PrinterAddMessage: " + e.Message); }
            }
        }

        [HarmonyPatch(typeof(Naninovel.UITextPrinter), "SetMessages")]
        [HarmonyPatch(new Type[] { typeof(Il2CppSystem.Collections.Generic.IReadOnlyList<Naninovel.PrintedMessage>) })]
        internal static class Patch_PrinterSetMessages
        {
            [HarmonyPostfix]
            internal static void Postfix(Naninovel.UITextPrinter __instance,
                                        [HarmonyArgument(0)] Il2CppSystem.Collections.Generic.IReadOnlyList<Naninovel.PrintedMessage> messages)
            {
                try { Reader.OnPrinterSetMessages(messages); }
                catch (Exception e) { A11yHost.Diag("Patches.PrinterSetMessages: " + e.Message); }
            }
        }

        // ==================== ADV：逐字渲染点（兜底 + 拿最终显示串）====================

        /// <summary>
        /// Naninovel.UI.RevealableText.set_Text(string) 的 Postfix。
        /// 依据：env-probe probe v3 / naninovel-re probe3 实测 PATCH_OK，且每题固定 4 次调用
        /// （#1~#3 是空串的面板重置、#4 才是正文）→ Reader.OnRevealText 里过滤空串并按实例去重。
        /// 它给的是**最终渲染串**（含 ruby/link 已经过 TMP 处理前后的形态），
        /// 是 Print 拿不到文本时的最后兜底。
        /// </summary>
        [HarmonyPatch(typeof(Naninovel.UI.RevealableText), "set_Text")]
        [HarmonyPatch(new Type[] { typeof(string) })]
        internal static class Patch_RevealText
        {
            [HarmonyPostfix]
            internal static void Postfix([HarmonyArgument(0)] string value)
            {
                Reader.OnRevealText(value);
            }
        }

        // ==================== 选项：面板出现时登记 ====================

        /// <summary>
        /// Naninovel.UI.ChoiceHandlerPanel.AddChoiceButton(ChoiceState)：
        /// 本作 TrialChoiceHandlerPanel / AdvChoiceHandlerButton 都继承这条链（witchtrials-re 实查）。
        /// 只登记面板，**不在这里读文字**（按钮刚创建、TMP 还没写 —— 参考实现踩过的坑），
        /// 朗读由 Choices.Update 每帧扫描、内容变化时触发。
        /// </summary>
        [HarmonyPatch(typeof(Naninovel.UI.ChoiceHandlerPanel), "AddChoiceButton")]
        [HarmonyPatch(new Type[] { typeof(Naninovel.ChoiceState) })]
        internal static class Patch_AddChoiceButton
        {
            [HarmonyPostfix]
            internal static void Postfix(Naninovel.UI.ChoiceHandlerPanel __instance)
            {
                try { Choices.OnButtonAdded(__instance); }
                catch (Exception e) { A11yHost.Diag("Patches.AddChoiceButton: " + e.Message); }
            }
        }

        // ==================== 加载：界面级可见性（事件驱动，比轮询稳）====================

        /// <summary>
        /// GigaCreation.NaninovelExtender.Ui.LoadingUiExtended.HandleVisibilityChanged(bool visible)：
        /// 本作「点新游戏后黑屏 + 右下进度条」用的就是 Giga 的加载界面（实机 UIPROBE 对象名
        /// `LoadingUI/Wrapper/ProgressBar`；类型签名见 re\decompiled\signatures\giga-ui\LoadingUiExtended.txt）。
        /// 轮询查不到它（对象可能不在默认层级/未激活），改成挂可见性回调：void + bool，detour 安全。
        /// </summary>
        internal static void ApplyLoadingUi(Harmony harmony)
        {
            Type t = null;
            try { t = Type.GetType("GigaCreation.NaninovelExtender.Ui.LoadingUiExtended, GigaCreation.NaninovelExtender.UI"); }
            catch (Exception e) { A11yHost.Diag("Patches.loading type: " + e.Message); }
            if (t == null)
            {
                Plugin.L.LogWarning("[a11y] LoadingUiExtended 未找到，加载播报降级为轮询");
                return;
            }
            try
            {
                MethodInfo m = AccessTools.Method(t, "HandleVisibilityChanged", new Type[] { typeof(bool) });
                if (m == null) { Plugin.L.LogWarning("[a11y] 未找到 LoadingUiExtended.HandleVisibilityChanged"); return; }
                harmony.Patch(m, postfix: new HarmonyMethod(
                    typeof(Patches).GetMethod(nameof(LoadingVisibilityPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Plugin.L.LogInfo("[a11y] patched LoadingUiExtended.HandleVisibilityChanged");
            }
            catch (Exception e) { Plugin.L.LogWarning("[a11y] patch LoadingUiExtended failed: " + e.Message); }
        }

        private static void LoadingVisibilityPostfix([HarmonyArgument(0)] bool visible)
        {
            try
            {
                A11yHost.Diag("HOOK loadingUi visible=" + visible);
                if (visible) LoadingWatch.Announce("LoadingUiExtended");
            }
            catch (Exception e) { A11yHost.Diag("Patches.LoadingVisibilityPostfix: " + e.Message); }
        }

        // ==================== 辩论：面板文本入口（手工挂，失败不致命）====================

        /// <summary>
        /// WitchTrials.Views.DebateTextPrinterPanel.SetText(string) / AddText(string)
        /// 是普通实例方法、无重载（re\decompiled\signatures\witchtrials\DebateTextPrinterPanel.txt 实查）：
        ///   public unsafe void SetText(string text)
        ///   public unsafe void AddText(string text)
        /// 用 AccessTools 手工挂 + 逐条 try/catch，避免某一条挂不上时拖垮整个 PatchAll。
        /// </summary>
        internal static void ApplyManual(Harmony harmony)
        {
            Type panel = null;
            try { panel = Type.GetType("WitchTrials.Views.DebateTextPrinterPanel, Assembly-CSharp"); }
            catch (Exception e) { A11yHost.Diag("Patches.debate type: " + e.Message); }

            if (panel == null)
            {
                Plugin.L.LogWarning("[a11y] DebateTextPrinterPanel 未找到，辩论朗读可能不生效");
                return;
            }

            PatchStringMethod(harmony, panel, "SetText");
            PatchStringMethod(harmony, panel, "AddText");
            PatchMessageMethod(harmony, panel);
        }

        // ==================== 辩论：单条消息入口（拿 Author）====================

        /// <summary>
        /// WitchTrials.Views.DebateTextPrinterPanel.AddMessage(PrintedMessage) /
        /// SetMessages(IReadOnlyList&lt;PrintedMessage&gt;) —— 辩论的说话人只能从这两个入口拿到
        /// （SetText 只有整段字符串，没有 Author）。
        /// 签名依据：re\decompiled\signatures\witchtrials\DebateTextPrinterPanel.txt；
        /// env-probe probe v3 实测两者 PATCH_OK。
        /// </summary>
        private static void PatchMessageMethod(Harmony harmony, Type panel)
        {
            try
            {
                MethodInfo add = AccessTools.Method(panel, "AddMessage",
                    new Type[] { typeof(Naninovel.PrintedMessage) });
                if (add != null)
                {
                    harmony.Patch(add, postfix: new HarmonyMethod(
                        typeof(Patches).GetMethod(nameof(DebateMessagePostfix),
                            BindingFlags.Static | BindingFlags.NonPublic)));
                    Plugin.L.LogInfo("[a11y] patched " + panel.Name + ".AddMessage");
                }
            }
            catch (Exception e) { Plugin.L.LogWarning("[a11y] patch AddMessage failed: " + e.Message); }

            try
            {
                Type listType = typeof(Il2CppSystem.Collections.Generic.IReadOnlyList<Naninovel.PrintedMessage>);
                MethodInfo set = AccessTools.Method(panel, "SetMessages", new Type[] { listType });
                if (set != null)
                {
                    harmony.Patch(set, postfix: new HarmonyMethod(
                        typeof(Patches).GetMethod(nameof(DebateMessagesPostfix),
                            BindingFlags.Static | BindingFlags.NonPublic)));
                    Plugin.L.LogInfo("[a11y] patched " + panel.Name + ".SetMessages");
                }
            }
            catch (Exception e) { Plugin.L.LogWarning("[a11y] patch SetMessages failed: " + e.Message); }
        }

        private static void DebateMessagePostfix(Naninovel.PrintedMessage __0)
        {
            try { Reader.OnDebateAddMessage(__0); }
            catch (Exception e) { A11yHost.Diag("Patches.DebateMessagePostfix: " + e.Message); }
        }

        /// <summary>SetMessages 是整体替换：只把最后一条交给 Reader（去重窗口兜住回刷）。</summary>
        private static void DebateMessagesPostfix(
            Il2CppSystem.Collections.Generic.IReadOnlyList<Naninovel.PrintedMessage> __0)
        {
            try
            {
                if (__0 == null) return;

                int count = 0;
                // interop 的 IReadOnlyList<T> 只暴露索引器、没有 Count（ilspycmd 实查 Il2Cppmscorlib.dll），
                // 先 TryCast 具体 List 拿 Count，拿不到就按索引探到空为止。
                Il2CppSystem.Collections.Generic.List<Naninovel.PrintedMessage> list =
                    __0.TryCast<Il2CppSystem.Collections.Generic.List<Naninovel.PrintedMessage>>();
                if (list != null)
                {
                    try { count = list.Count; } catch { count = 0; }
                }
                if (count <= 0)
                {
                    for (int i = 0; i < 64; i++)
                    {
                        try
                        {
                            Naninovel.PrintedMessage m = __0[i];
                            if (m == null) break;
                            count = i + 1;
                        }
                        catch { break; }
                    }
                }
                if (count <= 0) return;

                Reader.OnDebateAddMessage(__0[count - 1]);
            }
            catch (Exception e) { A11yHost.Diag("Patches.DebateMessagesPostfix: " + e.Message); }
        }

        private static void PatchStringMethod(Harmony harmony, Type type, string name)
        {
            try
            {
                MethodInfo target = AccessTools.Method(type, name, new Type[] { typeof(string) });
                if (target == null)
                {
                    Plugin.L.LogWarning("[a11y] 未找到 " + type.Name + "." + name + "(string)");
                    return;
                }
                harmony.Patch(target, postfix: new HarmonyMethod(
                    typeof(Patches).GetMethod(nameof(DebateStringPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                Plugin.L.LogInfo("[a11y] patched " + type.Name + "." + name);
            }
            catch (Exception e)
            {
                Plugin.L.LogWarning("[a11y] patch " + name + " failed: " + e.Message);
            }
        }

        /// <summary>SetText/AddText 共用 Postfix：按方法名分流，实例 id 做差分。</summary>
        private static void DebateStringPostfix(object __instance, string __0, MethodBase __originalMethod)
        {
            try
            {
                int id = 0;
                UnityEngine.Object o = __instance as UnityEngine.Object;
                if (o != null) { try { id = o.GetInstanceID(); } catch { } }
                else if (__instance != null) id = __instance.GetHashCode();

                string name = __originalMethod != null ? __originalMethod.Name : "";
                if (name == "AddText") Reader.OnDebateAddText(id, __0);
                else Reader.OnDebateSetText(id, __0);
            }
            catch (Exception e) { A11yHost.Diag("Patches.DebateStringPostfix: " + e.Message); }
        }
    }
}



