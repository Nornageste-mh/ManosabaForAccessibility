// ============================================================================
// 《魔法少女ノ魔女裁判》逐作区（UiNav 的 partial class 补充）
//
// 这个文件是 A11yFramework 模板 UiNav.Game.cs 的逐作实现，平台层保持不动。
//
// ★ 铁律：别名表与路径标签的每一条都必须有依据，不许猜对象名。
//   本作依据全部来自一次运行时控件转储（probe\run31-probe.ps1 →
//   probe\logs\run31-ui-probe.txt，UIPROBE 行），例如：
//     path='Wrapper/Menu/SmartPhone/HomeContent/MenuButtons/Button_Save'   label=''
//     path='ControlPanel/Wrapper/Menu/SmartPhone/HomeContent/CloseButton'  label=''
//     path='OptionsUI/Wrapper/Tabs/Tab_Audio'                              label=''
//     path='Wrapper/Pages/Page_Audio/OptionModules/VoiceVolume/Slider'     label=''
//   —— 这些控件**子树里一个字都没有**（label=''），不靠别名就只能念对象名。
//   转储里还看到 79 个 Selectable 中大量是预制体实例（active=False），
//   说明这份表覆盖的是「手机菜单 / 设置 / 画廊 / 记录 / 辩论 HUD」这一整套界面。
// ============================================================================
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ManosabaA11y
{
    internal static partial class UiNav
    {
        /// <summary>
        /// 对象名 → 中文。只在控件子树里一个字都没有时才会用到（平台层取值顺序
        /// 「子树文字 → 同行兄弟文字 → 别名表 → 对象名兜底」），所以不会盖掉正常按钮的朗读。
        /// </summary>
        private static readonly Dictionary<string, string> NameAlias =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // ---- 手机菜单（Wrapper/Menu/SmartPhone/HomeContent/MenuButtons/*）----
            { "Button_Save",    "保存" },
            { "Button_Load",    "读取存档" },
            { "Button_Log",     "对话记录" },
            { "Button_Options", "系统设置" },
            { "Button_Title",   "返回标题" },

            // ---- 通用按钮（多个面板复用同一个名字，语义一致）----
            { "CloseButton",    "关闭" },
            { "OpenButton",     "打开菜单" },
            { "AutoToggle",     "自动播放" },
            { "PlayVoiceButton","重播语音" },

            // ---- 设置页签（OptionsUI/Wrapper/Tabs/*）----
            { "Tab_Audio",      "音频页签" },
            { "Tab_Graphics",   "画面页签" },
            { "Tab_Message",    "消息页签" },
        };

        /// <summary>
        /// 路径级标签：控件本身没文字、对象名又太泛（Frame / Button / Dropdown / Slider）时，
        /// 用最近的祖先目录名判断它是什么。顺序是从近到远，命中即返回。
        ///
        /// 依据同样是 run31 的 UIPROBE 转储（路径原文见每条注释）。
        /// </summary>
        private static readonly Dictionary<string, string> PathAlias =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // OptionsUI/Wrapper/Pages/Page_*/ResetToDefaultButton/Frame
            { "ResetToDefaultButton", "恢复默认" },
            // DebateUI/Wrapper/SkipButton/Button（辩论 HUD 的跳过）
            { "SkipButton",           "跳过辩论" },

            // ---- 设置项：数值滑条 ----
            { "MasterVolume",  "主音量" },
            { "BgmVolume",     "音乐音量" },
            { "SfxVolume",     "音效音量" },
            { "VoiceVolume",   "语音音量" },
            { "MessageSpeed",  "文字显示速度" },
            { "AutoDelay",     "自动播放间隔" },

            // ---- 设置项：下拉框 ----
            { "Language",          "语言" },
            { "ResolutionDropDown","分辨率" },
            { "FrameRateDropDown", "帧率" },

            // ---- 设置项：开关（开关本身只有「オン/オフ」这种值文字，名字在路径里） ----
            { "SkipMode",                "跳过模式" },
            { "ToggleSkipMode",          "跳过开关" },
            { "FadeToReadMessage",       "淡入文字后朗读" },
            { "WaitVoicePlayingOnAutoMode", "自动模式等待语音播完" },
            { "PostProcessingToggle",    "后期处理" },
            { "ScreenModeToggle",        "显示模式" },
            { "CriticalChoiceHint",      "关键选项提示" },
        };

        /// <summary>
        /// 设置面板里那些「美术字标签」的对象名 → 中文（平台层挂钩子时优先于别名表）。
        /// 返回 "" 表示本作这一层没有额外信息，让平台层继续走别名/兄弟文本。
        /// </summary>
        internal static string GameLabelOf(Selectable s)
        {
            if (s == null) return "";
            try
            {
                Transform t = s.transform;
                for (int depth = 0; t != null && depth < 6; depth++)
                {
                    string n = t.name;
                    if (string.IsNullOrEmpty(n)) { t = t.parent; continue; }

                    string hit;
                    if (PathAlias.TryGetValue(n, out hit))
                    {
                        // 滑条/下拉框都要把「当前值」补上，否则读屏只知道是什么、不知道现在是几。
                        string value = "";
                        try { value = CurrentValueOf(s); } catch { }
                        return value.Length > 0 ? hit + "，" + value : hit;
                    }
                    t = t.parent;
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 读当前值：滑条读值、下拉框读选项文字、开关读值文字（「オン / オフ」）。
        /// 都读不到就返回空串（宁可少念，不乱念）。
        /// </summary>
        private static string CurrentValueOf(Selectable s)
        {
            try
            {
                Slider sl = s.TryCast<Slider>();
                if (sl != null)
                {
                    float pct = sl.maxValue > sl.minValue
                        ? (sl.value - sl.minValue) / (sl.maxValue - sl.minValue) : 0f;
                    return Mathf.RoundToInt(pct * 100f) + "%";
                }
            }
            catch { }

            try
            {
                Dropdown dd = s.TryCast<Dropdown>();
                if (dd != null && dd.options != null && dd.value >= 0 && dd.value < dd.options.Count)
                {
                    string v = dd.options[dd.value].text;
                    return v == null ? "" : v.Trim();
                }
            }
            catch { }

            return "";
        }

        /// <summary>
        /// 名字输入框的角色。本作正文流程实测没有姓名输入（run31 转储里 79 个 Selectable
        /// 没有一个 TMP_InputField），所以这里只做保守兜底：读占位提示，读不到就返回空。
        /// </summary>
        private static string InputFieldRoleName(TMP_InputField inf)
        {
            try
            {
                if (inf == null) return "";
                int id = inf.GetInstanceID();
                string cached;
                if (_inputRoles.TryGetValue(id, out cached)) return cached;

                string role = "";
                try
                {
                    if (inf.placeholder != null)
                    {
                        string ph = inf.placeholder is TMP_Text
                            ? ((TMP_Text)inf.placeholder).text : inf.placeholder.name;
                        if (!string.IsNullOrEmpty(ph)) role = ph.Trim();
                    }
                }
                catch { }

                _inputRoles[id] = role;
                return role;
            }
            catch { return ""; }
        }

        /// <summary>
        /// IL2CPP 下唯一安全的向下转型：<c>as</c> / <c>is</c> 对 interop 代理类型恒失败。
        /// 返回 null 表示「不是这个类型 / 对象已销毁」，调用方按不可用处理。
        /// </summary>
        private static T As<T>(UnityEngine.Object o)
            where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
        {
            if (o == null) return null;
            try { return ((Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase)o).TryCast<T>(); }
            catch { return null; }
        }
    }
}
