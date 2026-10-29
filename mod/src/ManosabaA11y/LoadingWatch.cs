// ============================================================================
// 加载播报（逐作层）
//
// 为什么需要：读屏玩家在加载期间完全不知道游戏在干什么 —— 没有画面、也没有声音线索，
// 很容易以为卡死了然后去乱按。本作实测的加载点不少：
//   · 点「新游戏」后先黑屏 + 右下角进度条（实机截图 probe\logs\run17-adv10.png）
//   · Naninovel 预载剧本 / 语音资源
// 一句「正在加载」就能解决「为什么现在没反应」这个最要紧的疑问。
//
// 三条通路都盯（做法照 noexistence_a11y 的 LoadingWatch.cs）：
//   1. Naninovel.UI.LoadingPanel 的可见性（interop 实查存在该类型）
//   2. 对象名 `LoadingUI` 的面板 —— ★ 本作实机 UIPROBE 转储里加载界面就叫这个名字
//      （probe\logs\run31-ui-probe.txt：path='LoadingUI/Wrapper/ProgressBar'），
//      而 Naninovel 的 LoadingPanel 类型在本作不一定挂在那棵树上，所以必须双保险
//   3. Unity 场景名里带 load 的切换（启动是单场景 Boot，留作兜底）
//
// 判可见性走 UiVis（CanvasGroup.alpha），和导航、朗读同一套标准 ——
// 面板只是被实例化但没显示时不应该播报。
// ============================================================================
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ManosabaA11y
{
    internal static class LoadingWatch
    {
        private static Naninovel.UI.LoadingPanel _panel;
        private static Transform _loadingUi;
        private static int _rescanAt;
        private static bool _panelVisible;
        private static bool _typeBroken;
        private static bool _loggedFound;

        private static string _lastScene = "";
        private static bool _sceneAnnounced;

        /// <summary>
        /// 同一波加载里只播一次，避免面板闪烁时反复念。
        /// 参考作用的是 3 秒；本作实测「点新游戏」后加载界面会连着显隐好几次
        /// （run36 日志 `HOOK loadingUi` True/False 交替 4 轮，3 秒窗内全播了），
        /// 所以放宽到 8 秒 —— 玩家听到的仍是「进入加载了」，不会重复轰炸。
        /// </summary>
        private const float Debounce = 8f;
        private static float _lastAnnounce = -999f;

        internal static void Announce(string why)
        {
            if (Plugin.CfgAnnounceLoading == null || !Plugin.CfgAnnounceLoading.Value) return;
            if (Time.realtimeSinceStartup - _lastAnnounce < Debounce) return;
            _lastAnnounce = Time.realtimeSinceStartup;
            A11yHost.Diag("加载播报（" + why + "）");

            // ★ 必须**打断**，不能排队。加载提示回答的正是「为什么现在没反应」，
            //   时效性就是它的全部价值；被它打断的那句可以按重读键再听（平台层 Repeat 只留最近几条）。
            Repeat.Say("正在加载。", true);
        }

        private static Naninovel.UI.LoadingPanel FindPanel()
        {
            try
            {
                if (_panel != null && _panel) return _panel;
                if (Time.frameCount < _rescanAt) return null;
                _panel = UnityEngine.Object.FindObjectOfType<Naninovel.UI.LoadingPanel>();
                if (_panel != null && !_loggedFound)
                {
                    _loggedFound = true;
                    A11yHost.Diag("LoadingWatch: 找到 Naninovel LoadingPanel，对象名=" + _panel.name);
                }
                return _panel;
            }
            catch (Exception e)
            {
                if (!_typeBroken)
                {
                    _typeBroken = true;
                    A11yHost.Diag("LoadingPanel 类型不可用，加载播报降级为对象名/场景名：" + e.GetType().Name);
                }
                return null;
            }
        }

        /// <summary>
        /// 按对象名找加载界面。依据是实机 UIPROBE 转储里确实有 `LoadingUI/Wrapper/ProgressBar`，
        /// 不是猜的。用 FindObjectsOfTypeAll 是因为它可能处于未激活状态。
        /// </summary>
        private static Transform FindLoadingUi()
        {
            try
            {
                if (_loadingUi != null && _loadingUi) return _loadingUi;
                Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
                if (all == null) return null;
                for (int i = 0; i < all.Length; i++)
                {
                    Transform t = all[i];
                    if (t == null) continue;
                    if (!string.Equals(t.name, "LoadingUI", StringComparison.Ordinal)) continue;
                    _loadingUi = t;
                    A11yHost.Diag("LoadingWatch: 找到 LoadingUI 对象（按对象名）");
                    return t;
                }
            }
            catch { }
            return null;
        }

        internal static void Update()
        {
            if (Plugin.CfgAnnounceLoading == null || !Plugin.CfgAnnounceLoading.Value) return;

            // ---- 1) Naninovel 的加载面板 ----
            if (!_typeBroken)
            {
                Naninovel.UI.LoadingPanel p = FindPanel();
                if (p != null)
                {
                    bool vis = UiVis.Visible(p);
                    if (vis && !_panelVisible) Announce("LoadingPanel");
                    _panelVisible = vis;
                }
            }

            // ---- 2) 对象名 LoadingUI ----
            try
            {
                Transform lu = FindLoadingUi();
                if (lu != null)
                {
                    bool vis = lu.gameObject.activeInHierarchy && !UiVis.Hidden(lu);
                    if (vis && !_uiVisible) Announce("LoadingUI");
                    _uiVisible = vis;
                }
            }
            catch { }

            // ---- 3) Unity 场景切换 ----
            try
            {
                string sc = SceneManager.GetActiveScene().name;
                if (sc != _lastScene)
                {
                    _lastScene = sc;
                    _sceneAnnounced = false;
                }
                if (!_sceneAnnounced && !string.IsNullOrEmpty(sc)
                    && sc.IndexOf("load", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _sceneAnnounced = true;
                    Announce("场景 " + sc);
                }
            }
            catch { }
        }

        private static bool _uiVisible;
    }
}
