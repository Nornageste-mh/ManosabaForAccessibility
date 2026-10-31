using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ManosabaA11y
{
    /// <summary>
    /// 选项框朗读与键盘选择（《魔法少女ノ魔女裁判》）。
    ///
    /// 依据：
    ///   · 选项面板类型链（witchtrials-re 反编译实查）：
    ///       WitchTrials.Views.TrialChoiceHandlerPanel
    ///         : GigaCreation.NaninovelExtender.Ui.ChoiceHandlerPanelModified
    ///         : Naninovel.UI.ChoiceHandlerPanel
    ///     按钮：WitchTrials.Views.AdvChoiceHandlerButton / TrialChoiceHandlerButton
    ///         : Naninovel.UI.ChoiceHandlerButton
    ///   · 选项文本：ChoiceState.Summary（LocalizableText，== 本地化后的中文）
    ///   · 本作选项量大：全剧本 @choice 1184 条（text-extract 统计）——
    ///     读屏玩家拿不到选项文本等于不能玩，所以这块和台词同级重要。
    ///
    /// 三个实测坑沿用参考实现（同为 Naninovel IL2CPP）：
    ///   1) 不能在 AddChoiceButton 里读文字 —— 按钮刚创建、TMP 还没写；
    ///   2) 容器第 0 个子物体不是第一个选项 —— 要过滤「激活 + 有 Button + 有文字 + interactable」再编号；
    ///   3) 文字可能在子树多处 —— 拼接子树 TMP/UGUI Text 最多 3 段。
    /// </summary>
    internal static class Choices
    {
        private static Naninovel.UI.ChoiceHandlerPanel _panel;
        private static readonly List<string> _labels = new List<string>();
        private static readonly List<Button> _buttons = new List<Button>();
        private static string _announcedKey = "";
        private static float _announcedAt = -999f;
        private static float _notLiveAt = 0f;
        private static int _lastChildCount = -1;
        // 测试钩子只用一次（MANOSABA_A11Y_TEST_CHOICE），避免每帧重复点选项
        private static bool _testChoiceDone;

        /// <summary>由 Harmony 在 AddChoiceButton 之后调用：只记下面板，不做朗读。</summary>
        internal static void OnButtonAdded(Naninovel.UI.ChoiceHandlerPanel panel)
        {
            if (panel != null) _panel = panel;
        }

        private static Naninovel.UI.ChoiceHandlerPanel Panel()
        {
            try
            {
                if (_panel != null && _panel) return _panel;
                _panel = UnityEngine.Object.FindObjectOfType<Naninovel.UI.ChoiceHandlerPanel>();
                return _panel;
            }
            catch { return null; }
        }

        private static Transform Container(Naninovel.UI.ChoiceHandlerPanel panel)
        {
            try
            {
                RectTransform c = panel.ButtonsContainer;
                return c == null ? null : c.transform;
            }
            catch { return null; }
        }

        /// <summary>子树里所有非空文本拼起来（最多 3 段）。</summary>
        private static string TextOf(Transform t)
        {
            try
            {
                List<string> parts = new List<string>();
                TMPro.TMP_Text[] tmps = t.GetComponentsInChildren<TMPro.TMP_Text>(true);
                if (tmps != null)
                {
                    foreach (TMPro.TMP_Text x in tmps)
                    {
                        if (x == null) continue;
                        string s = x.text;
                        if (string.IsNullOrEmpty(s)) continue;
                        s = s.Trim();
                        if (s.Length == 0 || parts.Contains(s)) continue;
                        parts.Add(s);
                        if (parts.Count >= 3) break;
                    }
                }
                if (parts.Count == 0)
                {
                    Text[] txts = t.GetComponentsInChildren<Text>(true);
                    if (txts != null)
                    {
                        foreach (Text x in txts)
                        {
                            if (x == null) continue;
                            string s = x.text;
                            if (string.IsNullOrEmpty(s)) continue;
                            s = s.Trim();
                            if (s.Length == 0 || parts.Contains(s)) continue;
                            parts.Add(s);
                            if (parts.Count >= 3) break;
                        }
                    }
                }
                return string.Join(" ", parts.ToArray());
            }
            catch { return ""; }
        }

        /// <summary>选项文本走 TextProc 清洗（本作选项也可能带 &lt;br&gt;</summary>、ruby）。</summary>
        private static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = TextProc.Clean(s);
            s = s.Replace("（", "").Replace("）", "").Replace("(", "").Replace(")", "");
            return s.Trim();
        }

        private static void Refresh(Naninovel.UI.ChoiceHandlerPanel panel)
        {
            _labels.Clear();
            _buttons.Clear();

            Transform container = Container(panel);
            if (container == null) return;

            int n = container.childCount;
            _lastChildCount = n;
            bool diag = Plugin.CfgDiagLog != null && Plugin.CfgDiagLog.Value;
            StringBuilder sb = diag ? new StringBuilder() : null;
            if (sb != null) sb.Append("选项容器 ").Append(container.name).Append(" childCount=").Append(n);

            for (int i = 0; i < n; i++)
            {
                Transform ch = null;
                try { ch = container.GetChild(i); } catch { }
                if (ch == null) continue;

                bool live;
                try { live = ch.gameObject.activeInHierarchy; } catch { live = false; }

                Button b = null;
                try { b = ch.GetComponent<Button>(); } catch { }
                if (b == null) { try { b = ch.GetComponentInChildren<Button>(true); } catch { } }

                string label = TextOf(ch);

                if (sb != null)
                    sb.Append("\n    [").Append(i).Append("] ").Append(live ? "激活" : "未激活")
                      .Append(b != null ? " 有Button" : " 无Button")
                      .Append(" 文字=").Append(string.IsNullOrEmpty(label) ? "(空)" : label);

                // 只把「激活 + 点得动 + 有文字」的当成真正的选项
                if (!live || b == null || string.IsNullOrEmpty(label)) continue;
                if (!b.interactable) continue;

                _labels.Add(Clean(label));
                _buttons.Add(b);
            }

            if (sb != null && n > 0)
            {
                sb.Append("\n    => 认定为选项 ").Append(_labels.Count).Append(" 项");
                A11yHost.Diag(sb.ToString());
            }
        }

        internal static void Update()
        {
            Naninovel.UI.ChoiceHandlerPanel panel = Panel();

            // 面板「在画面上」= 物体激活 **且** 没有被 CanvasGroup 藏起来。
            // 只看 activeInHierarchy 会踩坑：选完之后那批按钮是延迟移除的，
            // 如果这时面板被淡出，残留按钮会被当成新选项念出来。
            bool live = panel != null && UiVis.Visible(panel);

            if (!live)
            {
                // 本作选项面板是淡入淡出的：UiVis 判可见会在动画帧里闪一下 false 再 true。
                // 若收到 false 立刻清空缓冲，下一帧重扫就会把同一组选项再念一遍
                // （run32 实测念了两遍）。所以给 1.5 秒宽限期，超时才真的清空；
                // _announcedKey 不清 —— 同一组文字的选项只念一次。
                if (_notLiveAt <= 0f) _notLiveAt = Time.unscaledTime;
                if (Time.unscaledTime - _notLiveAt > 1.5f && _labels.Count > 0)
                {
                    _labels.Clear();
                    _buttons.Clear();
                }
                return;
            }
            _notLiveAt = 0f;

            bool wantRead = Plugin.CfgReadChoices == null || Plugin.CfgReadChoices.Value;
            bool hotkeys = Plugin.CfgChoiceHotkeys == null || Plugin.CfgChoiceHotkeys.Value;

            // 容器子物体数量变了就重扫（每帧全量扫代价太高）
            int cc = -1;
            try { Transform c = Container(panel); if (c != null) cc = c.childCount; } catch { }
            if (cc != _lastChildCount || _labels.Count == 0) Refresh(panel);

            // 文字是按钮创建之后才写上去的 → 「内容变了才播报」天然挡掉读到空串那一版。
            string key = string.Join("|", _labels.ToArray());
            if (key.Length > 0 && key != _announcedKey && _labels.Count > 0)
            {
                _announcedKey = key;
                _announcedAt = Time.unscaledTime;
                string announcement = null;
                if (wantRead)
                {
                    // 整组选项拼成一段念出去（而不是逐条），理由：
                    //   1) 逐条念时退格重读只能重读最后一条，读屏玩家没法回听整组；
                    //   2) 一次念完让「选项」在语义上是一件事。
                    StringBuilder sb = new StringBuilder();
                    sb.Append("共 ").Append(_labels.Count).Append(" 个选项。");
                    for (int i = 0; i < _labels.Count; i++)
                        sb.Append("选项 ").Append(i + 1).Append("：").Append(_labels[i]).Append("。");
                    if (hotkeys) sb.Append("按数字键选择。");
                    announcement = sb.ToString();

                    Repeat.Say(announcement);   // Note + Speak，退格能重读整组选项
                    // 选项播报也进「剧情专用重读缓冲」：玩家按重读键要能重听选项，
                    // 但界面导航播报（「系统设置，按钮。2 / 5」）不会进这条缓冲。
                    Reader.Remember(announcement);
                }
                // 诊断里带上**实际要念的整组文案**：自动化测试直接断言这一行，
                // 不用去猜语音层吐了什么（对比只看 key 的老版本，这里能看到编号与按键提示）。
                A11yHost.Diag("CHOICE 播报 " + _labels.Count + " 项: " + key
                              + (announcement == null ? "" : " => " + announcement));
            }

            // ---- 数字键直选 ----
            if (!hotkeys) return;
            if (_buttons.Count == 0) return;

            int pick = 0;
            try
            {
                for (int i = 1; i <= 9; i++)
                {
                    // 走 KeyEdge 而不是裸 Input：本作只启用了新 Input System，
                    // UnityEngine.Input 读取会抛 InvalidOperationException（实机 run31 取证），
                    // KeyEdge 会在旧 API 不可用时退回 Win32 全局读取（A11yHost.UseGlobalKeys）。
                    if (KeyEdge.Pressed(KeyCode.Alpha0 + i) || KeyEdge.Pressed(KeyCode.Keypad0 + i))
                    {
                        pick = i;
                        break;
                    }
                }
            }
            catch { pick = 0; }

            // ---- 测试钩子：本机键盘注入被系统挡掉（SendInput 返回 0），
            //      所以用 MANOSABA_A11Y_TEST_CHOICE=1 走同一条「选中第 1 项」的代码路径，
            //      验证的是「编号 → 按钮 → onClick」这段，而不是按键采样（那部分由单元自测覆盖）。
            if (pick == 0 && Plugin.TestChoiceOnce && _announcedKey.Length > 0 && !_testChoiceDone)
            {
                _testChoiceDone = true;
                pick = 1;
                A11yHost.Diag("CHOICE 测试钩子：自动选择第 1 项");
            }

            if (pick == 0) return;
            A11yHost.Diag("CHOICE 数字键按下 " + pick);

            if (pick > _buttons.Count)
            {
                Repeat.Say("只有 " + _buttons.Count + " 个选项。");
                return;
            }

            try
            {
                Button b = _buttons[pick - 1];
                if (b == null || b.gameObject == null) { Repeat.Say("选项已失效。"); return; }

                if (wantRead) Repeat.Say(_labels[pick - 1]);   // 选前复述，确认选的是哪个
                b.onClick.Invoke();
                A11yHost.Diag("CHOICE 数字键 " + pick + " -> " + _labels[pick - 1]);
            }
            catch (Exception e)
            {
                A11yHost.Diag("Choices.Update: " + e.Message);
                Repeat.Say("选择失败。");
            }
        }

        /// <summary>供 Patches 在面板重建时调用（面板切换场景后旧引用会失效）。</summary>
        internal static void Reset()
        {
            _panel = null;
            _labels.Clear();
            _buttons.Clear();
            _announcedKey = "";
            _announcedAt = -999f;
            _notLiveAt = 0f;
            _lastChildCount = -1;
        }
    }
}
