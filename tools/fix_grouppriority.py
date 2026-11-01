import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\UiNav.Game.cs"
s = io.open(p, encoding="utf-8-sig").read()
add = '''
        /// <summary>
        /// 组的排序权重（平台层 `UiNav.GameGroupPriority` 钩子）。
        /// 为什么需要：平台默认全部返回 100，等于按扫描顺序排；本作把「标题菜单 / 存读档 /
        /// 设置」这类**整页界面**排前面，把游戏内 HUD（手机菜单、辩论）排后面，
        /// 玩家一进导航就先听到「你现在在哪个界面」。
        /// </summary>
        internal static int GameGroupPriority(Transform root)
        {
            try
            {
                if (root == null) return 100;
                string n = root.name ?? "";
                if (n.IndexOf("TitleUI", StringComparison.OrdinalIgnoreCase) >= 0) return 10;
                if (n.IndexOf("SaveLoadUI", StringComparison.OrdinalIgnoreCase) >= 0) return 20;
                if (n.IndexOf("OptionsUI", StringComparison.OrdinalIgnoreCase) >= 0) return 30;
                if (n.IndexOf("WitchBookUI", StringComparison.OrdinalIgnoreCase) >= 0) return 40;
                if (n.IndexOf("LogUI", StringComparison.OrdinalIgnoreCase) >= 0) return 50;
                if (n.IndexOf("GalleryUI", StringComparison.OrdinalIgnoreCase) >= 0) return 60;
                if (n.IndexOf("SoundTestUI", StringComparison.OrdinalIgnoreCase) >= 0) return 70;
                if (n.IndexOf("ControlPanel", StringComparison.OrdinalIgnoreCase) >= 0) return 200;
                if (n.IndexOf("SmartPhone", StringComparison.OrdinalIgnoreCase) >= 0) return 210;
                if (n.IndexOf("DebateUI", StringComparison.OrdinalIgnoreCase) >= 0) return 220;
                return 100;
            }
            catch { return 100; }
        }
'''
s = s.rstrip()
assert s.endswith("}")
# \u63d2\u5728\u7c7b\u672b\u5c3e\uff08\u6700\u540e\u4e00\u4e2a }\uff09\u4e4b\u524d
idx = s.rfind("    }")
s = s[:idx] + add + s[idx:] + "\n"
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("UiNav.GamePriority added")

p2 = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs"
s2 = io.open(p2, encoding="utf-8-sig").read()
old = "            A11yHost.GameGroupName = UiNav.GameGroupName;"
assert old in s2
s2 = s2.replace(old, old + "\n            A11yHost.GameGroupPriority = UiNav.GameGroupPriority;", 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(s2)
print("group priority wired")
