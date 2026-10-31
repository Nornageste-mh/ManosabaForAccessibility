using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ManosabaA11y
{
    /// <summary>
    /// 「整屏界面」辅助：魔女手册 / 对话记录这类整页界面，平台的导航扫描会卡住。
    ///
    /// 为什么需要（用户 2026-10-31 反馈）：
    ///   · 魔女手册「记录」页只有页签和翻页箭头能被扫到，**关闭按钮被判定成
    ///     「画面外或点不到」**（平台层的 VisiblyClickable 用 UI 射线判断，书本
    ///     打开动画里有一层全屏 CanvasGroup 挡在按钮上面），于是玩家进去出不来；
    ///   · 「地图」页的地点标签不是 Selectable（是 MapAreaLabel 上的文字），
    ///     平台层只遍历 Selectable，所以地点永远念不到。
    ///
    /// 这里做三件**逐作层**的事（不改平台层、不猜对象名，全部来自运行时转储）：
    ///   1. 界面出现时自动播报：界面名 + 当前页签 + 当前页可见文字（含地图地点名）；
    ///   2. Esc：直接调用本界面的 CloseButton.OnClick（绕开被挡住的射线判断）；
    ///   3. 事件去抖，同一界面同一页只念一次，翻页/换页签会重新念。
    ///
    /// 为什么可以绕开射线：这是「读屏自己的入口」，不是替玩家扩大可达范围——
    /// 关闭按钮本来就是玩家点得到的，只是平台层的射线判定被遮挡层骗了。
    /// </summary>
    internal static class ScreenAssist
    {
        private const float ScanInterval = 0.5f;

        private static float _nextScanAt;
        private static Transform _active;
        private static string _activeTitle;
        private static string _lastKey;
        private static float _lastAnnounceAt;

        // 界面根对象名 → 播报名。全部来自 run31/run59 的运行时转储。
        private static readonly Dictionary<string, string> ScreenTitles =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "WitchBookUI", "魔女手册" },
            { "LogUI",       "对话记录" },
        };

        internal static void Update()
        {
            try
            {
                // Esc 每帧都查：本作只有 6 FPS 左右，0.5s 节流会漏掉「快速按一下」的键。
                if (_active != null) HandleCloseKey(_active, _activeTitle);

                if (Time.unscaledTime < _nextScanAt) return;
                _nextScanAt = Time.unscaledTime + ScanInterval;

                Transform found = FindVisibleScreen(out string title);
                if (found == null)
                {
                    _active = null;
                    _activeTitle = null;
                    _lastKey = null;
                    return;
                }

                if (found != _active)
                {
                    _active = found;
                    _activeTitle = title;
                    _lastKey = null;
                }

                AnnounceIfChanged(found, title);
            }
            catch (Exception e) { A11yHost.Diag("ScreenAssist: " + e.Message); }
        }

        // ------------------------------------------------------------------ 找界面
        /// <summary>
        /// 测试钩子用：等价于「玩家按了 Esc」——只做关闭，不做按键检测。
        /// 自动化环境里键盘注入被系统挡掉（SendInput 返回 0），所以验收走这里；
        /// 真实按键路径仍然是 Update() 里每帧的 Plugin.SpecDown("Escape")。
        /// </summary>
        internal static bool SimulateEscape()
        {
            if (_active == null) return false;
            CloseScreen(_active, _activeTitle);
            return true;
        }

        private static Transform FindVisibleScreen(out string title)
        {
            title = null;
            try
            {
                Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
                if (all == null) return null;
                for (int i = 0; i < all.Length; i++)
                {
                    Transform t = all[i];
                    if (t == null) continue;
                    string n = t.name;
                    if (string.IsNullOrEmpty(n)) continue;
                    // 转储里的真实名字带 "(Clone)"：WitchBookUI (Clone)
                    string bare = n.Replace(" (Clone)", "");
                    if (!ScreenTitles.TryGetValue(bare, out string nm)) continue;
                    if (!t.gameObject.scene.IsValid()) continue;          // 预制体资源
                    if (!UiVis.Visible(t)) continue;
                    title = nm;
                    return t;
                }
            }
            catch (Exception e) { A11yHost.Diag("ScreenAssist.Find: " + e.Message); }
            return null;
        }

        // ------------------------------------------------------------------ Esc 关闭
        private static void HandleCloseKey(Transform root, string title)
        {
            bool esc = false;
            try { esc = Plugin.SpecDown("Escape"); } catch { }
            if (!esc) return;
            CloseScreen(root, title);
        }

        /// <summary>真正执行「关闭本界面」：找 CloseButton 并调用它。</summary>
        private static void CloseScreen(Transform root, string title)
        {
            Transform close = FindDescendant(root, "CloseButton", 5);
            if (close == null) close = FindDescendant(root, "Button_Close", 5);
            if (close == null)
            {
                A11yHost.Diag("ScreenAssist: " + title + " 没找到关闭按钮，Esc 忽略");
                return;
            }
            Button b = close.GetComponent<Button>();
            if (b == null) { A11yHost.Diag("ScreenAssist: 关闭按钮不是 Button"); return; }
            try
            {
                b.onClick.Invoke();
                A11yHost.Diag("ScreenAssist: Esc 关闭 " + title + "（path=" + PathOf(close) + "）");
                Repeat.Say("已关闭" + title + "。", true);
                _active = null;
                _lastKey = null;
            }
            catch (Exception e) { A11yHost.Diag("ScreenAssist.Esc: " + e.Message); }
        }

        // ------------------------------------------------------------------ 播报
        private static void AnnounceIfChanged(Transform root, string title)
        {
            string page = CurrentPageTitle(root, out string tabName);
            // 只念「当前页」里的东西：手册 5 个页面的对象都常驻，不限定范围会念出别的页
            // （run86 实测：地图页也把线索页的钢笔和 13 个角色名念了一遍）。
            Transform scope = root;
            if (!string.IsNullOrEmpty(tabName) && tabName.StartsWith("Tab_", StringComparison.OrdinalIgnoreCase))
            {
                Transform pageRoot = FindDescendant(root, "Page_" + tabName.Substring(4), 4);
                if (pageRoot != null) scope = pageRoot;
            }
            string body;
            if (page.StartsWith("地图", StringComparison.Ordinal))
            {
                // 地图页只念「当前楼层的地点名」：Page_Maps 下还挂着角色名标签，
                // 混在一起会让玩家以为在念人物的位置（run90 实测）。
                body = CollectMapAreas(scope);
                if (string.IsNullOrEmpty(body)) body = CollectText(scope, 18, 260);
            }
            else
            {
                body = CollectText(scope, 18, 260);
            }
            string key = page + "|" + body;
            if (key == _lastKey) return;
            _lastKey = key;
            if (Time.unscaledTime - _lastAnnounceAt < 1.0f) return;   // 防止一帧内刷两遍
            _lastAnnounceAt = Time.unscaledTime;

            StringBuilder sb = new StringBuilder();
            sb.Append(title);
            if (!string.IsNullOrEmpty(page)) sb.Append("，").Append(page);
            sb.Append("。");
            if (!string.IsNullOrEmpty(body)) sb.Append("内容：").Append(body).Append("。");
            string line = sb.ToString();
            A11yHost.Diag("ScreenAssist: 播报 " + line);
            A11yHost.Diag("ScreenAssist: 页签=" + tabName + " 范围=" + PathOf(scope));
            Repeat.Say(line, true);
        }

        /// <summary>当前页签（Tab_* 里 isOn 的那个）→ 中文页签名；tabName 回传对象名（如 Tab_Maps）。</summary>
        private static string CurrentPageTitle(Transform root, out string tabName)
        {
            tabName = "";
            try
            {
                Toggle[] toggles = root.GetComponentsInChildren<Toggle>(true);
                if (toggles == null) return "";
                for (int i = 0; i < toggles.Length; i++)
                {
                    Toggle t = toggles[i];
                    if (t == null || !t.isOn) continue;
                    string alias = UiNav.TabTitleOf(t.name);
                    if (!string.IsNullOrEmpty(alias)) { tabName = t.name; return alias; }
                }
            }
            catch (Exception e) { A11yHost.Diag("ScreenAssist.Page: " + e.Message); }
            return "";
        }

        /// <summary>
        /// 地图页的地点标签：`WitchTrials.Views.MapAreaLabel` 不是 Selectable，也不一定有
        /// TMP 子物体被 CollectText 抓到（它自己的 `_label` 是 TMP），所以单独取一遍。
        /// 依据：run31 转储的 `Page_Maps/.../MapArea` + run59 截图里的「医务室」标签。
        /// </summary>
        private static string CollectMapAreas(Transform scope)
        {
            try
            {
                // 楼层容器（run89 实测路径：Page_Maps/F1_1/...、Page_Maps/F2_1/...、B1_1）
                Transform floor = null;
                for (int i = 0; i < scope.childCount; i++)
                {
                    Transform c = scope.GetChild(i);
                    if (c == null) continue;
                    if (FloorName(c.name) == null) continue;
                    if (!c.gameObject.activeInHierarchy) continue;
                    // 当前楼层通常只有一个是「可见」的；都不满足时退回第一个激活的
                    if (floor == null) floor = c;
                    if (UiVis.Visible(c)) { floor = c; break; }
                }
                Transform use = floor != null ? floor : scope;

                // MapAreaLabel 的 CanvasGroup 平时 alpha=0（只有悬停/选中时才显示），
                // 但文字是齐全的，所以这里**不看可见性**，只看 activeInHierarchy。
                WitchTrials.Views.MapAreaLabel[] labels = use.GetComponentsInChildren<WitchTrials.Views.MapAreaLabel>(true);
                if (labels == null || labels.Length == 0) return "";
                StringBuilder sb = new StringBuilder();
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                int n = 0;
                for (int i = 0; i < labels.Length && n < 24; i++)
                {
                    WitchTrials.Views.MapAreaLabel ml = labels[i];
                    if (ml == null || !ml.gameObject.activeInHierarchy) continue;
                    string s = TextProc.Clean(ml._label == null ? "" : ml._label.text);
                    if (string.IsNullOrEmpty(s) || s.Length < 2) continue;
                    if (!seen.Add(s)) continue;
                    if (sb.Length > 0) sb.Append("、");
                    sb.Append(s);
                    n++;
                }
                string floorName = floor == null ? null : FloorName(floor.name);
                string list = sb.ToString();
                if (string.IsNullOrEmpty(list)) return "";
                return floorName == null ? list : floorName + "：" + list;
            }
            catch (Exception e) { A11yHost.Diag("ScreenAssist.Areas: " + e.Message); return ""; }
        }

        /// <summary>楼层容器名 → 中文楼层名（依据 UiNav.Game.cs 的 PathAlias 同一张表）。</summary>
        private static string FloorName(string name)
        {
            switch (name)
            {
                case "F1_1": return "1F";
                case "F2_1": return "2F";
                case "F2_2": return "2F";
                case "B1_1": return "地下1F";
                default: return null;
            }
        }

        /// <summary>当前页面上可见的 TMP 文字（自动去重、去噪声、限长）。</summary>
        private static string CollectText(Transform root, int maxItems, int maxChars)
        {
            try
            {
                TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
                if (texts == null) return "";
                StringBuilder sb = new StringBuilder();
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                int items = 0;
                for (int i = 0; i < texts.Length && items < maxItems; i++)
                {
                    TMP_Text tt = texts[i];
                    if (tt == null || !tt.gameObject.activeInHierarchy) continue;
                    if (!UiVis.Visible(tt.transform)) continue;
                    string raw = tt.text;
                    if (string.IsNullOrEmpty(raw)) continue;
                    string s = TextProc.Clean(raw);
                    if (string.IsNullOrEmpty(s) || s.Length < 2) continue;
                    if (UiNav.GameTextIsNoise(s)) continue;
                    // 过滤「本地化键名」这类英文标识（实测线索页里混进一个 Description）。
                    if (LooksLikeKey(s)) continue;
                    if (!seen.Add(s)) continue;
                    if (sb.Length + s.Length + 1 > maxChars) break;
                    if (sb.Length > 0) sb.Append("，");
                    sb.Append(s);
                    items++;
                }
                return sb.ToString();
            }
            catch (Exception e) { A11yHost.Diag("ScreenAssist.Text: " + e.Message); return ""; }
        }

        // ------------------------------------------------------------------ 工具
        /// <summary>纯 ASCII 且含字母 → 当作键名/标识符丢掉（例：Description）。</summary>
        private static bool LooksLikeKey(string s)
        {
            bool hasLetter = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c > 127) return false;                    // 有非 ASCII（汉字/假名/符号）就留下
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) hasLetter = true;
            }
            return hasLetter;
        }

        private static Transform FindDescendant(Transform root, string name, int maxDepth)
        {
            try
            {
                Queue<KeyValuePair<Transform, int>> q = new Queue<KeyValuePair<Transform, int>>();
                q.Enqueue(new KeyValuePair<Transform, int>(root, 0));
                while (q.Count > 0)
                {
                    KeyValuePair<Transform, int> kv = q.Dequeue();
                    Transform t = kv.Key;
                    if (t == null) continue;
                    if (t != root && string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase)) return t;
                    if (kv.Value >= maxDepth) continue;
                    for (int i = 0; i < t.childCount; i++) q.Enqueue(new KeyValuePair<Transform, int>(t.GetChild(i), kv.Value + 1));
                }
            }
            catch { }
            return null;
        }

        private static string PathOf(Transform t)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                int guard = 0;
                while (t != null && guard++ < 8)
                {
                    if (sb.Length > 0) sb.Insert(0, "/");
                    sb.Insert(0, t.name);
                    t = t.parent;
                }
                return sb.ToString();
            }
            catch { return "?"; }
        }
    }
}
