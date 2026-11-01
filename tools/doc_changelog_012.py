import io
p = "D:/DSHWorkSpace/manosaba_a11y/CHANGELOG.md"
s = io.open(p, encoding="utf-8-sig").read()
assert "- 版本号是**纯数字四段**（`System.Version`）：当前 `0.0.0.11`" in s
s = s.replace("当前 `0.0.0.11`", "当前 `0.0.0.12`", 1)

anchor = "### v0.0.0.11（审判台词不自动朗读 + 重读键改为 `=`）"
entry = """### v0.0.0.12（开发者选项改成「跳转到指定剧情」）

**背景**：v0.0.0.6 查清发行版里游戏自带的开发者面板根本打不开
（`DebugOptionsBase.s_debugManager == null`，`DebugPanel` 只是从未实例化的预制体）。
玩家提出：既然跳转剧本已经研究明白，那开发者选项就用「跳到指定剧情」。
v0.0.0.9 已经验证过 `ScriptPlayerExtensions.LoadAndPlayAtLine`（当时靠环境变量
`MANOSABA_A11Y_JUMP_LINE` 直达 `Trial04` 第 327 行），现在把它做成玩家可操作的菜单。

**怎么做**：
- 新增 `mod\\src\\ManosabaA11y\\ScriptJump.cs`：配置「其它 / 允许剧情跳转（开发者）」
  （默认**关**）打开后，`Ctrl+Alt+Shift+F3` 开关菜单；上下键换剧本、翻页键跳十个、
  Home/End 到首尾、数字键输入起始行、回车跳转、Esc 退出。
- 菜单数据源是生成的 `mod\\src\\ManosabaA11y\\ScriptIndex.cs`（440 条**剧本路径**，
  由 `tools\\gen_script_index.py` 从 `textassets` 索引生成，**不含任何台词原文**）。
- 跳转本身走 `Naninovel.UniTaskExtensions.Forget(ScriptPlayerExtensions.LoadAndPlayAtLine(sp, path, line, 0))`。
- 菜单打开时 `FocusGate.CanReadChoiceKeys()` 返回 false，0-9 只用于输入起始行，
  不会被选项 / 地图 / 证物层同时读走。
- 删除 `DevPanel.cs` 以及 csproj 里随之无用的 `GigaCreation.Tools.Debugging.*` / `UniRx` 引用
  （那段代码在发行版里永远打不开面板，留着只会误导）。
- 无障碍纪律：**不做任何自动跳转**，没有定时器、没有「卡住就帮你跳」；菜单提示走
  `Repeat.Say` 而不是剧情重读缓冲（避免跳转提示被 `=` 当成剧情念出来）。

**顺手修**：`TextProc` 的 `<br>` 处理在 0.0.0.10 之后仍会把「的<br>全」念成「的，全」的
个别情况（`BrEvaluator` 左邻黑名单已含常见助词，本轮审计复测 440 个剧本 0 命中）。
本版另修：`Plugin.cs` 里 `TestScriptJump` 必须在测试钩子总闸之后才可能被置位，
否则玩家 shell 里残留环境变量就会被动跳剧情。

**验证**：
- 单元：`SELFTEST SUMMARY total=52 pass=52 fail=0`（新增 5 条 `scriptindex.*` 断言：
  数量 ≥400、含已知剧本、含系统脚本、无重复、查不到返回 -1）。
- 实机（`probe\\run125-scriptjump.ps1`，`MANOSABA_A11Y_TEST_SCRIPTJUMP=1` 由插件内部驱动
  同一套菜单代码）：详见本版提交前的探针日志 `probe\\logs\\jump-live-summary.txt`。
- **尚未实机复测**：真实按键（本机自动化环境的键盘注入被系统挡掉，`Ctrl+Alt+Shift+F3`
  与方向键只能由玩家亲手确认）、跳到「非 Act 开头」的剧本（如 `System/System_Title`）后的表现。

"""
assert s.count(anchor) == 1
s = s.replace(anchor, entry + anchor)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("CHANGELOG updated")
