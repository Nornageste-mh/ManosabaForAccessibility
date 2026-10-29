// ============================================================================
// 界面取证探针（仅诊断用，MANOSABA_A11Y_PROBE_UI=1 时启用）
//
// 用途：把当前界面里所有「可交互控件」的**对象名 / 父级链 / 显示文字 / 可见性**
// 一次性写进日志，用来给 UiNav.Game.cs 的别名表取证。
// 依据：Framework 铁律「不许猜对象名」—— 别名表每一条都要能指回一次这类转储。
//
// 不是玩家功能：默认不开，也不需要常开（开一次拷走日志即可）。
// ============================================================================
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ManosabaA11y
{
    internal static class UiProbe
    {
        private static bool _read;
        private static bool _enabled;
        private static bool _done;
        private static float _due = -1f;
        private static int _round;

        internal static void Update()
        {
            if (_done) return;
            if (!_read)
            {
                _read = true;
                try { _enabled = Environment.GetEnvironmentVariable("MANOSABA_A11Y_PROBE_UI") == "1"; }
                catch { _enabled = false; }
                if (!_enabled) { _done = true; return; }
                _due = Time.realtimeSinceStartup + 12f;
                A11yHost.Diag("UIPROBE 已排程：12 秒后开始，之后每 40 秒转储一次（最多 12 次；标题/魔女手册/审判画面前后各覆盖到）");
            }
            if (!_enabled) return;
            if (Time.realtimeSinceStartup < _due) return;

            _round++;
            Dump();

            // 早期每 18 秒一次（引擎初始化 → 标题），之后每 40 秒一次：
            // 魔女手册、审判/辩论这些画面都要靠真实操作走进去，转储太早会漏掉。
            if (_round >= 12) { _done = true; return; }
            _due = Time.realtimeSinceStartup + (_round < 3 ? 18f : 40f);
        }

        private static string PathOf(Transform t)
        {
            var sb = new StringBuilder(96);
            int depth = 0;
            while (t != null && depth < 6)
            {
                if (sb.Length > 0) sb.Insert(0, '/');
                sb.Insert(0, t.name);
                t = t.parent;
                depth++;
            }
            return sb.ToString();
        }

        private static void Dump()
        {
            try
            {
                A11yHost.Diag("UIPROBE scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                             + " frame=" + Time.frameCount);

                // 旧输入 API 是否可用：平台层的热键（重读/沉默/导航）全都走 Input.GetKeyDown，
                // 本作若只启用了新 Input System，这些热键会整体失效 —— 必须先证实。
                try
                {
                    bool f4 = Input.GetKey(KeyCode.F4);
                    A11yHost.Diag("UIPROBE legacyInput=OK f4=" + f4);
                }
                catch (Exception e)
                {
                    A11yHost.Diag("UIPROBE legacyInput=FAIL " + e.GetType().Name + ": " + e.Message);
                }

                Selectable[] all = Resources.FindObjectsOfTypeAll<Selectable>();
                A11yHost.Diag("UIPROBE selectables=" + (all == null ? 0 : all.Length));
                if (all == null) return;

                var seen = new System.Collections.Generic.HashSet<string>();
                int shown = 0;
                foreach (Selectable s in all)
                {
                    if (s == null) continue;
                    string type = s.GetType().Name;
                    string go = "";
                    string path = "";
                    bool active = false, interactable = false;
                    try
                    {
                        go = s.gameObject.name;
                        path = PathOf(s.transform);
                        active = s.gameObject.activeInHierarchy;
                        interactable = s.IsInteractable();
                    }
                    catch { }

                    // 控件上的文字：TMP 优先，其次旧 UI.Text
                    var txt = new StringBuilder(64);
                    try
                    {
                        TMPro.TMP_Text[] tmps = s.GetComponentsInChildren<TMPro.TMP_Text>(true);
                        if (tmps != null)
                        {
                            foreach (TMPro.TMP_Text t in tmps)
                            {
                                if (t == null) continue;
                                string v = t.text == null ? "" : t.text.Replace("\n", " ").Trim();
                                if (v.Length == 0) continue;
                                if (txt.Length > 0) txt.Append(" | ");
                                txt.Append(v);
                                if (txt.Length > 60) break;
                            }
                        }
                        if (txt.Length == 0)
                        {
                            Text[] ut = s.GetComponentsInChildren<Text>(true);
                            if (ut != null)
                            {
                                foreach (Text t in ut)
                                {
                                    if (t == null || string.IsNullOrEmpty(t.text)) continue;
                                    if (txt.Length > 0) txt.Append(" | ");
                                    txt.Append(t.text.Replace("\n", " ").Trim());
                                    if (txt.Length > 60) break;
                                }
                            }
                        }
                    }
                    catch { }

                    string label = txt.ToString();
                    string resolved = "";
                    try { resolved = UiNav.GameLabelOf(s); } catch { }
                    string key = type + "§" + path + "§" + label;
                    if (!seen.Add(key)) continue;
                    bool vis = false;
                    try { vis = UiVis.Visible(s); } catch { }

                    A11yHost.Diag("UIPROBE item type=" + type + " name='" + go + "' path='" + path
                                 + "' label='" + label + "' resolved='" + resolved + "' active=" + active
                                 + " interactable=" + interactable + " visible=" + vis);
                    if (++shown >= 120) { A11yHost.Diag("UIPROBE …（超过 120 条，截断）"); break; }
                }

                DumpLabels();
            }
            catch (Exception e)
            {
                A11yHost.Diag("UIPROBE 失败: " + e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>
        /// 单独转储两类「美术字标签」组件：魔女手册的页签/按钮文字都挂在这上面，
        /// 而它们的 GameObject 往往是**未激活**的（切页时才激活），平台层读不到。
        /// 这一节是别名表与 InactiveTextOf 兜底的取证依据。
        /// </summary>
        private static void DumpLabels()
        {
            try
            {
                var rl = Resources.FindObjectsOfTypeAll<GigaCreation.Essentials.Ui.RebuildableLabel>();
                if (rl != null && rl.Length > 0)
                {
                    A11yHost.Diag("UIPROBE rebuildableLabels=" + rl.Length);
                    int n = 0;
                    foreach (var l in rl)
                    {
                        if (l == null) continue;
                        string path = "";
                        string text = "";
                        string id = "";
                        bool active = false;
                        try
                        {
                            path = PathOf(l.transform);
                            active = l.gameObject.activeInHierarchy;
                            id = l.LabelId == null ? "" : l.LabelId;
                            if (l._uiLabel != null && l._uiLabel.text != null)
                                text = l._uiLabel.text.Replace("\n", " ").Trim();
                        }
                        catch { }
                        A11yHost.Diag("UIPROBE label path='" + path + "' id='" + id + "' text='" + text
                                     + "' active=" + active);
                        if (++n >= 80) { A11yHost.Diag("UIPROBE …（标签超过 80 条，截断）"); break; }
                    }
                }
            }
            catch (Exception e)
            {
                A11yHost.Diag("UIPROBE 标签转储失败: " + e.GetType().Name + ": " + e.Message);
            }

            try
            {
                var ml = Resources.FindObjectsOfTypeAll<WitchTrials.Views.MapAreaLabel>();
                if (ml != null && ml.Length > 0)
                {
                    A11yHost.Diag("UIPROBE mapAreaLabels=" + ml.Length);
                    foreach (var m in ml)
                    {
                        if (m == null) continue;
                        string path = "";
                        string text = "";
                        try
                        {
                            path = PathOf(m.transform);
                            if (m._label != null && m._label.text != null)
                                text = m._label.text.Replace("\n", " ").Trim();
                        }
                        catch { }
                        A11yHost.Diag("UIPROBE mapLabel path='" + path + "' text='" + text + "'");
                    }
                }
            }
            catch (Exception e)
            {
                A11yHost.Diag("UIPROBE 地图标签转储失败: " + e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
