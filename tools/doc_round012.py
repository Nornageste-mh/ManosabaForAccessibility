import io

# ---------- 安装说明 ----------
p = "D:/DSHWorkSpace/manosaba_a11y/mod/package/安装说明.md"
s = io.open(p, encoding="utf-8-sig").read()
def rep(old, new, cnt=1):
    global s
    assert s.count(old) == cnt, "count %d for %r" % (s.count(old), old[:60])
    s = s.replace(old, new)

rep("（v0.0.0.11 实测范围）", "（v0.0.0.12 实测范围）") if "（v0.0.0.11 实测范围）" in s else None

if "剧情跳转" not in s:
    anchor = "## 快捷键"
    idx = s.find(anchor)
    assert idx > 0, "no 快捷键 section"
    # 在快捷键表后追加一节：找表格结束后
    add = """
### 剧情跳转菜单（开发者功能，v0.0.0.12 起）

> ⚠️ 这是给开发/测试用的：会直接跳到没读过的剧情、破坏存档进度、可能剧透。
> 默认关闭，需要先在 `BepInEx\\config\\manosaba.a11y.reader.cfg` 里把
> 「其它 / 允许剧情跳转（开发者）」改成 `true`，再重启游戏。

用法：按 `Ctrl` + `Alt` + `Shift` + `F3` 打开菜单（会念当前剧本与总数），然后

| 按键 | 作用 |
|---|---|
| `↑` / `↓` | 上一个 / 下一个剧本（按 Name 顺序，共 440 个） |
| `PageUp` / `PageDown` | 往前 / 往后跳 10 个剧本 |
| `Home` / `End` | 跳到第一个 / 最后一个剧本 |
| `1`-`9` | 输入起始行号（最多 5 位，会念出来） |
| `Esc` / 退格 | 关菜单 / 清零行号 |
| `回车` / `空格` | 跳到「当前剧本 + 起始行」 |

「起始行」是剧本里的行号（0 = 从剧本开头）。一般用 0 就够；
要复现某个具体位置时才需要填。

"""
    s = s[:idx] + add.lstrip("\n") + s[idx:]

io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("安装说明 updated")

# ---------- 常见问题 ----------
p2 = "D:/DSHWorkSpace/manosaba_a11y/mod/package/常见问题.md"
t = io.open(p2, encoding="utf-8-sig").read()
if "剧情跳转" not in t:
    t = t.rstrip() + """

## 按 Ctrl+Alt+Shift+F3 没有出现开发者面板？

v0.0.0.12 起那个组合键改成**剧情跳转菜单**了。原来的「打开游戏自带开发者面板」
在正式发行版里根本走不通：游戏没有创建调试管理器（`DebugOptionsBase.s_debugManager` 是 null），
调试面板只是内存里从未被实例化的预制体。

现在的用法：先在 cfg 里打开「其它 / 允许剧情跳转（开发者）」，再按组合键；
会念出当前剧本和总数，用 `↑` `↓` 换剧本、`回车` 跳转、`Esc` 退出。

注意这是开发者功能：会跳过你没读过的剧情、破坏存档进度、可能剧透，正式游玩请保持关闭。
"""
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(t)
print("常见问题 updated")

# ---------- framework-notes ----------
p3 = "D:/DSHWorkSpace/manosaba_a11y/docs/framework-notes.md"
f = io.open(p3, encoding="utf-8-sig").read().rstrip() + """

## 21. 第十四轮：开发者选项改成「剧情跳转」（0.0.0.12）

**为什么改**：0.0.0.6 已证发行版里游戏自带的调试面板打不开；玩家要求「开发者选项 = 跳到指定剧情」。
跳转 API（`ScriptPlayerExtensions.LoadAndPlayAtLine`）在 0.0.0.9 已经实机验证过，
这轮只是把它变成**玩家可操作的菜单**。

**三条经验**
1. **能跳 ≠ 跳过去会继续播**：如果跳转时画面正停在选项（`ChoiceHandlerPanel` 有按钮），
   剧本状态会切到目标脚本（回读 `sp.PlayedScript.Path` 证实是目标脚本、`playing=True`），
   但台词不会立刻打印——游戏的等待输入状态还挂着。开发功能不替玩家做选择，
   做法是**提醒一句**：「画面刚才停在选项上，跳转后如果没声音，先按数字键选一项。」
2. **回读才是证据**：`LoadAndPlayAtLine` 是异步的，发出去不等于生效。
   `ScriptJump.VerifyJump` 在 3 秒后回读 `sp.PlayedScript.Path` / `Playing` / `PlayedIndex`
   并写进日志（`SCRIPTJUMP 跳转后: want=… got=… playing=True idx=0`），
   这样「跳转成功」是可验证的，而不是靠感觉。
3. **菜单自己要用数字键，就必须让选择层让开**：`FocusGate.CanReadChoiceKeys()` 在
   `ScriptJump.Active` 时返回 false，否则 1-9 会被选项/地图/证物层同时读走。

**数据源**：`mod\\src\\ManosabaA11y\\ScriptIndex.cs` 由 `tools\\gen_script_index.py` 从
`textassets` 索引生成（440 条**剧本路径**，不含台词原文），单元断言
`scriptindex.数量不少于 400` / `无重复条目` / `查不到返回 -1` 守住表本身。
"""
io.open(p3, "w", encoding="utf-8-sig", newline="\n").write(f)
print("framework-notes updated")
