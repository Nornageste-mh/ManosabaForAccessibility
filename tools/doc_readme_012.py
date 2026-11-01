import io
p = "D:/DSHWorkSpace/manosaba_a11y/README.md"
s = io.open(p, encoding="utf-8-sig").read()

def rep(old, new, cnt=1):
    global s
    assert s.count(old) == cnt, "count %d for %r" % (s.count(old), old[:50])
    s = s.replace(old, new)

# 1) 快捷键表：追加剧情跳转
rep("""| `回车` / `空格` | 导航模式下激活控件；否则交给游戏推进剧情 |""",
    """| `回车` / `空格` | 导航模式下激活控件；否则交给游戏推进剧情 |
| `Ctrl`+`Alt`+`Shift`+`F3` | **剧情跳转菜单**（开发者功能，默认关闭，需先在 cfg 里打开「允许剧情跳转（开发者）」） |""")

# 2) 进度表：新增一行（放在审判倒计时行之后）
rep("""| **魔女手册 / 对话记录整页界面** |""",
    """| **剧情跳转菜单（开发者）** | ✅ 实机验证（0.0.0.12）：`SCRIPTJUMP 打开菜单 index=17 共 440 个剧本` → 下移两项 → `SCRIPTJUMP 选中 index=19 path=Act01_Chapter01/Act01_Chapter01_Adv20` → `SCRIPTJUMP 跳转 path=…Adv20 line=0`；真实按键未复测（本机键盘注入被系统挡掉） |
| **魔女手册 / 对话记录整页界面** |""")

# 3) 踩坑第 9 条：改成「跳转菜单」的结论
rep("""   `Ctrl+Alt+Shift+F3` 没有面板可开（这是开发构建功能）。配置「允许激活开发者选项」
   因此默认关闭，只对开发构建有意义。""",
    """   `Ctrl+Alt+Shift+F3` 没有面板可开（这是开发构建功能）。
   **0.0.0.12 起改成「跳转到指定剧情」**（`ScriptJump.cs` + 生成的 `ScriptIndex.cs`）：
   组合键打开菜单，上下键换剧本、数字键输入起始行、回车跳转，走
   `ScriptPlayerExtensions.LoadAndPlayAtLine`（0.0.0.9 已实机验证过这条 API）。
   配置项相应改名「允许剧情跳转（开发者）」，仍默认关闭。""")
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("README updated")
