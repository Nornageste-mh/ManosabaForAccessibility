using System;
using System.Collections.Generic;
using Naninovel;
using UnityEngine;

namespace ManosabaA11y
{
    /// <summary>
    /// 文本语言切换。
    ///
    /// 为什么需要：游戏本体默认语言跟随系统/上次选择，实机标题画面实测是**日本語**
    /// （scene-tree-t50s：UI 文本 "初期設定に戻す"），而 games 的 config.json 声明
    ///   "available_text_lang": ["ja", "zh-Hans"],
    ///   "unavailable_lang_fallback": { "default": "ja", "zh-Hant": "zh-Hans" }
    /// 中文剧本与界面都在 zh-Hans 资源里。读屏用户**看不见语言下拉框**（Options → Message 页里叫
    /// 「言語 / Language」，实测当前项 "日本語"），所以补丁必须能直接在配置里指定，
    /// 否则用户听到的是日文，等于没有中文化。
    ///
    /// 依据（ilspycmd 实查 Elringus.Naninovel.Runtime.dll）：
    ///   · Naninovel.ILocalizationManager：SelectedLocale(string)、AvailableLocales、
    ///     LocaleAvailable(string)、SelectLocale(string) → UniTask
    ///   · Naninovel.UniTaskExtensions.Forget(this UniTask) —— 不 await 也能驱动完成任务
    /// </summary>
    internal static class Locale
    {
        private const string FollowGame = "跟随游戏";

        private static bool _tried;

        /// <summary>每帧调用；只在引擎就绪后尝试一次。</summary>
        public static void Update()
        {
            if (_tried) return;
            try
            {
                if (!Naninovel.Engine.Initialized) return;
                _tried = true;

                string want = Plugin.CfgTextLocale == null ? FollowGame : (Plugin.CfgTextLocale.Value ?? FollowGame);
                // 实机自动化测试用环境变量覆盖（不改用户 cfg）：MANOSABA_A11Y_LOCALE=zh-Hans
                try
                {
                    string env = Environment.GetEnvironmentVariable("MANOSABA_A11Y_LOCALE");
                    if (!string.IsNullOrEmpty(env)) want = env;
                }
                catch { }
                string target = ToLocaleId(want);

                Naninovel.ILocalizationManager l10n = null;
                try { l10n = Naninovel.Engine.GetService<Naninovel.ILocalizationManager>(); }
                catch (Exception e) { A11yHost.Diag("Locale.get: " + e.Message); }
                if (l10n == null) { A11yHost.Diag("Locale: 没有 ILocalizationManager"); return; }

                string current = "";
                try { current = l10n.SelectedLocale; } catch { }

                if (target.Length == 0)
                {
                    A11yHost.Diag("Locale: 跟随游戏 = " + current);
                    return;
                }

                A11yHost.Diag("Locale: selected=" + current + " target=" + target);

                if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase)) return;

                try
                {
                    if (!l10n.LocaleAvailable(target))
                    {
                        Plugin.L.LogWarning("[a11y] 语言 " + target + " 不可用，保持 " + current);
                        Repeat.Say("所请求的语言不可用，保持当前语言。");
                        return;
                    }
                }
                catch { }

                Plugin.L.LogInfo("[a11y] 切换文本语言 " + current + " -> " + target);
                try
                {
                    // Forget：不等待也不能丢异常（UniTaskExtensions 实查存在）
                    l10n.SelectLocale(target).Forget();
                }
                catch (Exception e)
                {
                    A11yHost.Diag("Locale.select: " + e.Message);
                    return;
                }

                Repeat.Say("文本语言已切换为" + want + "。");
            }
            catch (Exception e)
            {
                A11yHost.Diag("Locale: " + e.Message);
            }
        }

        private static string ToLocaleId(string want)
        {
            if (string.IsNullOrEmpty(want)) return "";
            string s = want.Trim();
            if (s == FollowGame) return "";
            if (s == "简体中文" || s.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase)) return "zh-Hans";
            if (s == "日本語" || s == "日语" || s.Equals("ja", StringComparison.OrdinalIgnoreCase)) return "ja";
            if (s == "繁體中文" || s == "繁体中文" || s.Equals("zh-Hant", StringComparison.OrdinalIgnoreCase)) return "zh-Hant";
            if (s == "English" || s.Equals("en", StringComparison.OrdinalIgnoreCase)) return "en";
            return s;   // 也允许直接填 locale id
        }
    }
}
