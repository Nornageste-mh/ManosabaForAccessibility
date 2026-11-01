import io
p = "D:/DSHWorkSpace/manosaba_a11y/测试问卷.md"
s = io.open(p, encoding="utf-8-sig").read()
if "剧情跳转" not in s:
    s = s.rstrip() + """

## 9. 剧情跳转菜单（开发者功能，v0.0.0.12 新增）

> 先在 cfg 里把「其它 / 允许剧情跳转（开发者）」改成 `true` 再重启游戏。

| 检查项 | 结果 |
|---|---|
| 按 `Ctrl`+`Alt`+`Shift`+`F3` 能听到「剧情跳转。当前 …，共 440 个剧本」 | 尚未实机复测（自动化键盘注入被系统挡掉） |
| `↓` 换剧本会念新剧本名，`↑` 会回去 | 尚未实机复测 |
| `PageDown` 一次跳 10 个剧本 | 尚未实机复测 |
| 数字键输入起始行会念「起始行 N。」 | 尚未实机复测 |
| `回车` 后能听到「已跳到 …，从第 N 行开始」，剧情真的换过去 | ✅ 插件内部驱动已验证（`SCRIPTJUMP 跳转后: want=… got=…Adv20 playing=True idx=0`）；真实按键待确认 |
| `Esc` 退出菜单会念「已退出剧情跳转。」 | 尚未实机复测 |
| 跳到正在等选项的画面时，会提醒「先按数字键选一项」 | 尚未实机复测 |
"""
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("测试问卷 updated")

p2 = "D:/DSHWorkSpace/manosaba_a11y/无障碍可行性验证.md"
t = io.open(p2, encoding="utf-8-sig").read()
if "ScriptJump" not in t:
    t = t.rstrip() + """

## 剧情跳转菜单（v0.0.0.12）

| 项 | 值 |
|---|---|
| 组合键 | `Ctrl`+`Alt`+`Shift`+`F3`（配置「其它 / 允许剧情跳转（开发者）」，默认关） |
| 代码 | `mod\\src\\ManosabaA11y\\ScriptJump.cs` |
| 数据源 | 生成的 `ScriptIndex.cs`（440 条剧本路径，`tools\\gen_script_index.py` 生成，不含台词） |
| 跳转 API | `Naninovel.ScriptPlayerExtensions.LoadAndPlayAtLine(sp, path, line, 0)` + `UniTaskExtensions.Forget` |
| 生效证据 | 3 秒后回读 `sp.PlayedScript.Path` / `Playing` / `PlayedIndex`，写 `SCRIPTJUMP 跳转后: want=… got=…` |
| 实机结果 | `SCRIPTJUMP 打开菜单 index=17 共 440 个剧本` → `选中 index=19 path=…Adv20` → `跳转 path=…Adv20 line=0` → `跳转后: want=…Adv20 got=…Adv20 playing=True idx=0` |
| 未覆盖 | 真实按键（本机键盘注入被系统挡掉）、跳到 `System/*` 这类非剧情脚本后的表现 |
| 纪律 | 不做任何自动跳转；菜单提示走 `Repeat.Say`，不写进剧情重读缓冲 |
"""
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(t)
print("无障碍可行性验证 updated")
