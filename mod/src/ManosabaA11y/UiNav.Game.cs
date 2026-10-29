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

            // ---- 标题画面（run41 实测路径，5 个按钮：ModalUI/TitleUI/Wrapper/Buttons/*）----
            // 这几个按钮子树里没有 TMP 标签，只有一张图 + 一个用「Ver. 1.1.2」这种
            // 版本号冒充行文字的坑（见 GameTextIsNoise），所以必须靠名字表。
            { "Button_NewGame",  "新游戏" },
            { "Button_LoadGame", "读取存档" },
            { "Button_Gallery",  "鉴赏模式" },
            { "Button_Exit",     "退出游戏" },

            // ---- 通用按钮（多个面板复用同一个名字，语义一致）----
            { "CloseButton",    "关闭" },
            { "OpenButton",     "打开菜单" },
            { "AutoToggle",     "自动播放" },
            { "PlayVoiceButton","重播语音" },

            // ---- 设置页签（OptionsUI/Wrapper/Tabs/*）----
            { "Tab_Audio",      "音频页签" },
            { "Tab_Graphics",   "画面页签" },
            { "Tab_Message",    "消息页签" },

            // ---- 引擎初始化进度条（run47 实测：EngineInitializationUI(Clone)/Wrapper/Progress）----
            // 启动阶段唯一可导航的控件，不给名字就会被念成「未命名滑条」。
            { "Progress",       "加载进度" },

            // ---- 魔女手册（图鉴）页签与按钮 ----
            // 依据：玩家 2026-10-29 11:20 那轮日志里的 44 条
            //   [UiNav] 未命名控件：类型=开关 原始名=Tab_Maps 路径=…/WitchBookUI/Wrapper/Tabs/Tab_Maps
            //   [UiNav] 未命名控件：类型=按钮 原始名=PresentEvidenceButton 路径=…/Page_Clues/LeftArea/PresentEvidenceButton/Button
            //   [UiNav] 未命名控件：类型=按钮 原始名=Wrapper 路径=…/WitchBookButtonUI/Wrapper/Button
            // 页签本身只有 CanvasGroup（_defaultLabel/_highlightedLabel），TMP 文字在子物体里且常处于
            // 未激活状态，平台层默认只认激活文字 → 这里按对象名兜底，同时 NearbyText() 会再试一次
            // 「子树（含未激活）里的 TMP 文字」。
            { "Tab_Clues",  "线索" },
            { "Tab_Rules",  "规定" },
            { "Tab_Maps",   "地图" },
            { "Tab_Notes",  "记录" },
            // run59 实测多出来的第五个页签（run31 的转储里没有它，因为当时还没通关到人物页）
            { "Tab_Profiles", "人物" },
            { "PresentEvidenceButton", "出示证据" },
            { "WitchBookButtonUI",     "魔女手册" },
            { "WitchBookUI",           "魔女手册" },

            // ---- 分页箭头：魔女手册 / 存读档 / 鉴赏三处共用同一套对象名（run59 实测） ----
            //   WitchBookUI (Clone)/Wrapper/ItemList/Pagination/MovingArrows/ButtonNext
            //   UI/SaveLoadUI/Wrapper/Pagination/MovingArrows/ButtonNext
            //   GalleryUI/Wrapper/StillPanel/Pagination/MovingArrows/ButtonNext
            { "ButtonNext", "下一页" },
            { "ButtonPrev", "上一页" },
            // WitchBookUI (Clone)/Wrapper/Pages/Page_Profiles/LeftArea/LoupeButton
            { "LoupeButton", "放大查看" },
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

            // ---- 魔女手册·地图：地点（run59 实测）----
            // 地图上每个地点是一个名叫 Focus 的按钮，父物体是英文地点 id：
            //   WitchBookUI (Clone)/Wrapper/Pages/Page_Maps/F1_1/Infirmary/Focus
            // 中文名来自游戏自己的本地化表（textassets/ui/zh-Hans/CustomUI.txt 的
            //   WitchBook.Map.Infirmary: 医务室 …），不是我们编的。
            // 注意 UnknownArea 原文是「<cspace=-0.2em><space=4>？？？」（富文本），这里取纯文字。
            { "Infirmary",           "医务室" },
            { "Courtroom",           "审判庭" },
            { "CourtroomHallway",    "审判庭入口过道" },
            { "Courtyard",           "中庭" },
            { "EntranceHall",        "玄关大厅" },
            { "FrontGuesthouse",     "招待所" },
            { "FlowerFields",        "往花田方向" },
            { "Lake",                "往湖方向" },
            { "Incinerator",         "焚烧炉" },
            { "Kitchen",             "厨房" },
            { "Library",             "图书室" },
            { "Lounge",              "会客厅" },
            { "MessHall",            "食堂" },
            { "NoahStudio",          "诺亚的画室" },
            { "Parlor",              "接客室" },
            { "PrisonEntrance",      "监牢入口" },
            { "RecreationRoom",      "娱乐室" },
            { "Restroom",            "女厕" },
            { "SecondFloorHallway",  "2F大厅" },
            { "Showers",             "淋浴房" },
            { "SolitaryConfinement", "惩罚室" },
            { "StorageRoom",         "杂物处" },
            { "Sunroom",             "日光房" },
            { "Wall",                "围墙" },
            { "Warehouse",           "仓库" },
            { "BasementHallway",     "牢房" },
            { "ChamberGnome",        "地精之室" },
            { "ChamberSalamander",   "火精之室" },
            { "ChamberUndine",       "水精之室" },
            { "UnknownArea",         "未知区域" },

            // ---- 魔女手册·地图：楼层容器（同上实测路径里的 F1_1 / F2_1 / F2_2 / B1_1）----
            { "F1_1", "1F" },
            { "F2_1", "2F" },
            { "F2_2", "2F" },
            { "B1_1", "地下1F" },
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

                // 对象名别名（含祖先）：魔女手册的页签/按钮子树里一个字都没有，
                // 只能靠这条。平台层第 3 步也会查同一张表，这里提前返回是为了让
                // 「别名 + 当前值」能拼在一起（开关类），并且不再被同行兄弟文字抢走。
                Transform a = s.transform;
                for (int depth = 0; a != null && depth < 4; depth++)
                {
                    string n = a.name;
                    string alias;
                    if (!string.IsNullOrEmpty(n) && NameAlias.TryGetValue(n, out alias))
                    {
                        string value = "";
                        try { value = CurrentValueOf(s); } catch { }
                        return value.Length > 0 ? alias + "，" + value : alias;
                    }
                    a = a.parent;
                }

                // 名字太泛（Button / Wrapper / Frame / Toggle …）时，读子树里**未激活**的
                // TMP 文字：魔女手册页签的标签挂在 CanvasGroup 下面，平台层只认激活文字，
                // 所以一行都取不到（玩家实测就是「未命名开关」。这里主动补一次。
                // 滑条/下拉框排除：它们子树里的文字是**当前值**（100% / 1280×720），
                // 当标签念会变成「100%，滑条」。
                if (s.TryCast<Slider>() == null && s.TryCast<Dropdown>() == null)
                {
                    string near = InactiveTextOf(s.transform, 3);
                    if (near.Length > 0) return near;
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 读控件子树里（含未激活对象）第一段像人话的文字。
        /// 只用于「平台层什么都没读到」的兜底，且长度限制很严：太长多半是整段正文，不是控件名。
        /// </summary>
        private static string InactiveTextOf(Transform root, int maxDepth)
        {
            if (root == null) return "";
            try
            {
                TMP_Text[] tmps = root.GetComponentsInChildren<TMP_Text>(true);
                if (tmps != null)
                {
                    foreach (TMP_Text t in tmps)
                    {
                        if (t == null || t.text == null) continue;
                        string v = t.text.Replace("\n", " ").Trim();
                        if (v.Length == 0 || v.Length > 24) continue;
                        if (GameTextIsNoise(v)) continue;
                        return v;
                    }
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

        /// <summary>
        /// 面板组的中文名（逐作钩子 <see cref="A11yHost.GameGroupName"/>）。
        ///
        /// 依据 run41 实机：标题菜单的组根叫 Wrapper，平台层只能兜底成「面板」；
        /// 而读屏用户最需要知道的是「现在是标题画面还是设置页」，所以从组根往上
        /// 找第一个认识的容器名。都被找不到就返回空串，让平台走中性兜底。
        /// </summary>
        private static readonly Dictionary<string, string> GroupAlias =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TitleUI",     "标题画面" },
            { "SaveLoadUI",  "存读档" },
            { "OptionsUI",   "系统设置" },
            { "GalleryUI",   "鉴赏模式" },
            { "LogUI",       "对话记录" },
            { "SoundTestUI", "音乐鉴赏" },
            { "WitchBook",   "魔女手册" },
            // 魔女手册真实对象名是 WitchBookUI（run59 转储：…/UI/WitchBookUI (Clone)/Wrapper/...）
            { "WitchBookUI", "魔女手册" },
            { "Page_Maps",     "地图页" },
            { "Page_Clues",    "线索页" },
            { "Page_Profiles", "人物页" },
            { "Page_Notes",    "记录页" },
            { "Page_Rules",    "规定页" },
            // 开发者调试面板：正式流程里不激活，但转储里能看到，给个名字免得读成乱码
            { "DebugCanvas", "调试面板" },
            { "ControlPanel","手机菜单" },
            { "SmartPhone",  "手机菜单" },
            { "DebateUI",    "辩论" },
            { "LoadingUI",   "加载" },
            { "EngineInitializationUI", "引擎初始化" },
        };

        internal static string GameGroupName(Transform root)
        {
            if (root == null) return "";
            try
            {
                // 先看组根自己叫什么：魔女手册的 5 个页签各自成一个组（组根就是 Tab_Clues 这些），
                // 如果一路往上找到 WitchBookUI，10 个组会全叫「魔女手册」，那组播报等于没说
                // （run60 实测就是「全部 10 组：【魔女手册 1 项】×6…」）。
                string self = root.name;
                if (!string.IsNullOrEmpty(self))
                {
                    string selfHit;
                    if (GroupAlias.TryGetValue(self, out selfHit)) return selfHit;
                    if (self.StartsWith("Tab_", StringComparison.Ordinal) && NameAlias.TryGetValue(self, out selfHit))
                        return selfHit + "页签";
                }

                Transform t = root.parent;
                for (int up = 0; up < 8 && t != null; up++)
                {
                    string n = t.name;
                    if (!string.IsNullOrEmpty(n))
                    {
                        string hit;
                        if (GroupAlias.TryGetValue(n, out hit)) return hit;
                        // 组根是 Wrapper 这类泛名时，上一级的 Tab_* 就是最好的组名
                        if (n.StartsWith("Tab_", StringComparison.Ordinal) && NameAlias.TryGetValue(n, out hit))
                            return hit + "页签";
                    }
                    t = t.parent;
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 逐作噪声钩子：这些文字是版本号 / 版权行，不是控件标签。
        ///
        /// run41 实测的坑：标题菜单 5 个按钮的「同行文字」都取到了
        /// TitleUI/Wrapper/VersionNumberLabel 的 Ver. 1.1.2，于是整排按钮全念
        /// 「Ver. 1.1.2」。平台层不给逐作以外的改法，唯一正确的口子就是这里 ——
        /// Noise 过滤同时作用于「自己子树」和「同行兄弟」两条取文路径。
        /// </summary>
        internal static bool GameTextIsNoise(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            string t = s.Trim();
            if (t.StartsWith("Ver.", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("ver ", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.StartsWith("Copyright", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
