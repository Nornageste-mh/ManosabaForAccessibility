import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\ScreenAssist.cs'
s = io.open(p, encoding='utf-8-sig').read()

old_start = '        private static string CurrentPageTitle(Transform root, out string tabName)'
i = s.index(old_start)
# 找到方法结束（下一个 "\n        /// <summary>"）
j = s.index('        /// <summary>', i)

new = '''        private static string CurrentPageTitle(Transform root, out string tabName)
        {
            tabName = "";
            try
            {
                // 第一优先：真正「显示出来」的页面。
                // 依据 run124：剧情用 @choiceMapArea 打开手册时是**限制态**——只显示地图页、
                // 右侧 5 个页签被隐藏（用户截图里只有一块红色「地图」横幅），但 Tab_Clues 的
                // Toggle.isOn 仍是 true。只看 Toggle 会把地图错认成线索页，MapChoice 就永远
                // 不会被调用（日志里一条 MAPCHOICE 扫描都没有）。
                Transform pages = FindDescendant(root, "Pages", 4);
                if (pages != null)
                {
                    for (int i = 0; i < pages.childCount; i++)
                    {
                        Transform c = pages.GetChild(i);
                        if (c == null || !c.name.StartsWith("Page_", StringComparison.Ordinal)) continue;
                        if (!c.gameObject.activeInHierarchy) continue;
                        if (!UiVis.Visible(c)) continue;
                        string nm = c.name.Substring(5);
                        string al = UiNav.TabTitleOf("Tab_" + nm);
                        if (string.IsNullOrEmpty(al)) continue;
                        tabName = "Tab_" + nm;
                        return al;
                    }
                }

                // 兜底：页签 Toggle 的状态（正常浏览态下两者一致）。
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

'''

s = s[:i] + new + s[j:]
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('CurrentPageTitle rewritten')
