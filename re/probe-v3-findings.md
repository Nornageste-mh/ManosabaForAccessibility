# Harmony 探针结论（run5，v4 探针 0.4.0.0）

> 结论面向 Lead：本文件只写**有日志证据的确认项**与**明确的未确认项**，不推测。
> 证据日志：`probe/logs/LogOutput-run5-harmony-20261028-183438.log`（PID 13620，2026-10-28 18:34:38 启动，18:39:23 由 Lead kill）。
> 探针源码：`probe/plugin/ManosabaProbe/ProbePlugin.cs`（v4，0.4.0.0，含 `ProbePatches` 类）。

## 0. 一句话结论

**Harmony 路线成立**：9 个补丁目标全部 `PATCH_OK`（0 失败），包括最关键的 `Naninovel.TextPrinterManager.Print`
和 `Naninovel.UI.ChoiceHandlerPanel.AddChoiceButton`；但 run5 **没能进入 ADV**（自动化点击未命中标题菜单，
场景 1450 帧一直 `Boot`），所以 **Print 是否真的会被调用、正文长什么样，仍未确认** —— 需 Lead 实机验证（见 §5）。

## 1. run5 实测确认项（有原样日志行）

### 1.1 补丁全部挂上（9/9）

```
[Info   :ManosabaProbe] [ManosabaProbe] PATCH_OK TextPrinterManager.Print target=Naninovel.TextPrinterManager.Print(String printerId, PrintedMessage message, Boolean append, Single speed, AsyncToken token)
[Info   :ManosabaProbe] [ManosabaProbe] PATCH_OK RevealableText.set_Text target=Naninovel.UI.RevealableText.set_Text(String value)
[Info   :ManosabaProbe] [ManosabaProbe] PATCH_OK WitchTrialsTextPrinterPanel.SetMessageAuthor target=WitchTrials.Views.WitchTrialsTextPrinterPanel.SetMessageAuthor(MessageAuthor author)
[Info   :ManosabaProbe] [ManosabaProbe] PATCH_OK DebateTextPrinterPanel.SetText target=WitchTrials.Views.DebateTextPrinterPanel.SetText(String text)
[Info   :ManosabaProbe] [ManosabaProbe] PATCH_OK DebateTextPrinterPanel.AddMessage target=WitchTrials.Views.DebateTextPrinterPanel.AddMessage(PrintedMessage message)
[Info   :ManosabaProbe] [ManosabaProbe] PATCH_OK DebateTextPrinterPanel.SetMessages target=WitchTrials.Views.DebateTextPrinterPanel.SetMessages(IReadOnlyList`1 messages)
[Info   :ManosabaProbe] [ManosabaProbe] PATCH_OK UITextPrinterPanel.AddMessage target=Naninovel.UI.UITextPrinterPanel.AddMessage(PrintedMessage message)
[Info   :ManosabaProbe] [ManosabaProbe] PATCH_OK UITextPrinterPanel.SetMessages target=Naninovel.UI.UITextPrinterPanel.SetMessages(IReadOnlyList`1 messages)
[Info   :ManosabaProbe] [ManosabaProbe] PATCH_OK ChoiceHandlerPanel.AddChoiceButton target=Naninovel.UI.ChoiceHandlerPanel.AddChoiceButton(ChoiceState choice)
[Info   :ManosabaProbe] [ManosabaProbe] [ManosabaProbe] PATCH_SUMMARY ok=9 fail=0
```

要点：
- `TextPrinterManager.Print` 的**真实签名**是 5 参数（不是 2 参数），HarmonyX 按方法名+签名匹配成功：
  `Print(String printerId, PrintedMessage message, Boolean append, Single speed, AsyncToken token)`。
  → 写 Postfix 时用 `__0..__4` 或按类型/名字注入都安全；**不要**只写 `(string, PrintedMessage)` 两个参数，
  否则 `Harmony` 匹配会失败（run5 用的 postfix 只声明了前 4 个参数 `printerId, message, append, speed`，能工作）。
- `DebateTextPrinterPanel` 只有 `SetText(String)`，**没有** `AddText`（Lead 指令里提到的 `AddText` 不存在，
  探针用 `AddMessage` 替代，同样命中）。
- `WitchTrials.Views.WitchTrialsTextPrinterPanel.SetMessageAuthor(MessageAuthor author)` 存在且可挂。

### 1.2 Naninovel 服务全部可用（run4 起稳定复现，run5 再次确认）

```
[Info   :ManosabaProbe] NANINOVEL_ENGINE_INITIALIZED frame=128 scene='Boot' realtime=21.84
[Info   :ManosabaProbe] PRINTER_MANAGER_OK runtimeType=Naninovel.ITextPrinterManager defaultPrinterId='Normal' baseRevealSpeed=0.8 baseAutoDelay=0.5
[Info   :ManosabaProbe] CHOICE_HANDLER_MANAGER_OK runtimeType=Naninovel.IChoiceHandlerManager choiceButtonLoader=True
[Info   :ManosabaProbe] SCRIPT_PLAYER_OK runtimeType=Naninovel.IScriptPlayer playing=False waitingForInput=False playedIndex=0
[Info   :ManosabaProbe] UI_MANAGER_OK runtimeType=Naninovel.IUIManager fontName='SourceHanSerifSC' fontSize=-1 anyModal=False
```

补充：`MANAGER_DETAIL TryCast<TextPrinterManager> = True`，但 `GetActor("Normal")` 在**标题画面**抛
`Il2CppException: Naninovel.Error: Can't find 'Normal' actor.` —— 说明 `DefaultPrinterId='Normal'` 只是配置值，
标题画面下 printer actor 还没 spawn。要读 actor 必须等进入 ADV 之后（或 `Engine.Initialized` 之后再等一帧）。

### 1.3 事件订阅路线已被证伪（与 Lead 结论一致）

run3 实测异常（原文）：

```
[Error  :ManosabaProbe] [ManosabaProbe] 订阅打印事件失败: System.ArgumentException: Delegate has parameter of type Naninovel.PrintMessageArgs (non-blittable struct) which is not supported
   at Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate[TIl2Cpp](Delegate) ...
   at Il2CppSystem.Action`1.op_Implicit(...)
   at ManosabaProbe.ProbeBehaviour.HookNaninovel()
```

→ **最终结论：`add_OnPrintStarted` 不可用，必须走 Harmony**（与 Lead m00257 的判断一致）。别再尝试
`DelegateSupport.ConvertDelegate`、也别尝试 `Il2CppSystem.Action<PrintMessageArgs>` 的隐式转换。

## 2. run5 各类补丁触发计数（回答"哪条链真正抓到了台词"）

run5 结束时的汇总（`PRINT=0 ...`）：

| 计数 | 值 | 说明 |
|---|---|---|
| `PATCH_SUMMARY ok/fail` | **9 / 0** | 全部挂载成功 |
| `PRINT #`（TextPrinterManager.Print） | **0** | 没进 ADV，Print 从未被调用 → **未验证** |
| `SETTEXT #`（RevealableText.set_Text） | **0** | 同上 |
| `AUTHOR #`（WitchTrialsTextPrinterPanel.SetMessageAuthor） | **0** | 同上 |
| `BASE_ADDMESSAGE` / `BASE_SETMESSAGES`（UITextPrinterPanel） | **0 / 0** | 同上 |
| `DEBATE_SETTEXT` / `DEBATE_ADDMESSAGE` / `DEBATE_SETMESSAGES` | **0 / 0 / 0** | 同上 |
| `CHOICE #`（ChoiceHandlerPanel.AddChoiceButton） | **0** | 同上 |
| `SCRIPTDUMP` | **0** | 依赖 `PrintTotal > 0`，故未触发 |
| `DIALOGUE_CHANGED`（v3 轮询兜底） | **0** | 标题画面无 RevealableText 文本变化 |

**结论：run5 没有任何一条链抓到台词**——不是补丁失效，而是根本没进游戏（见 §3）。
唯一"抓到过台词"的证据来自 run3 的 v2 轮询探针（`TEXT_PROBE`），不是 Harmony 链：

```
TEXT_PROBE[900] active Naninovel<Runtime>/UI/TextPrinter/NormalPrinter/Wrapper/MessageLabel = "——这里是囚牢。"
TEXT_PROBE[900] inactive Naninovel<Runtime>/UI/LogUI/Wrapper/ScrollView/Viewport/Content/LogMessage(Clone)/MessageLabel = "——这里是囚牢。"
```

## 3. 为什么 run5 没进游戏（踩坑记录，已修）

- run5 自动化只点了一次 `(450,950)`；60 次推进后 `PRINT` 仍为 0、`heartbeat ... scene='Boot'` 直到 frame=1440。
- 截图（`probe/logs/run5-adv60-183838.png`）显示**一直停留在标题菜单**，说明点击根本没被游戏接收。
- 原因两条（详见 `re/env-facts.md` G11）：
  1. **窗口不在前台**：Unity 在窗口失去焦点时不处理鼠标；后台进程的 `SetForegroundWindow` 会被静默拒绝。
     v1 脚本没有校验 `GetForegroundWindow()`，直接点了 60 次空。
  2. **坐标**：Lead 判读 "New Game" 中心约 `(400,880)`（1920x1080 全屏、窗口位于 0,0）；v1 的 `(450,950)`
     落在判定框边缘。
- `probe/run5-automation.ps1` **v2 已修**：`Force-Foreground`（`ShowWindow(SW_RESTORE)` + `AttachThreadInput`
  + `SetForegroundWindow` + 校验返回值/`GetForegroundWindow`）→ 多候选坐标 `('400,880','450,950','410,910','380,860')`
  逐个试、每点两次（先聚焦再点击）→ 每次点完用 `PRINT # / DIALOGUE_CHANGED / CHOICE #` 判定是否进入 →
  全失败再 Enter/Space 兜底 → 推进阶段鼠标点 `(960,540)` 与空格键交替。
- 脚本已通过 `[Parser]::ParseFile` 语法校验（0 error），但 **v2 尚未实机跑过**（实例已交 Lead）。

## 4. A / B / C 三问

| 问题 | 答复 | 证据等级 |
|---|---|---|
| **A. `Print` Postfix 能否挂上、能否拿到 message.Text/Author** | **能挂上**：`PATCH_OK TextPrinterManager.Print target=...Print(String printerId, PrintedMessage message, Boolean append, Single speed, AsyncToken token)`，`PATCH_SUMMARY ok=9 fail=0`。**能否拿到 Text/Author：未确认**（run5 未进 ADV，Print 一次都没触发；读字段的 Postfix 代码没有被执行过） | 挂载=已确认；取值=未确认 |
| **B. 原文里有没有 `\|#<lineId>\|` 标记和 `Ema:` 作者前缀** | **未确认**。run5 日志中没有 `SampleText` 相关内容（fw-skeleton 的 0.1.0 版插件不打 SampleText；我们的探针也没拿到 Print 原文）。唯一观察到的原文是 run3 的 UI 轮询值 `"——这里是囚牢。"`，**不含** `\|#`、**不含** `Ema:` 前缀（只是渲染后的显示文本，不代表 Print 入参原文） | 未确认（观察仅限 UI 渲染文本） |
| **C. 同一条文本是否重复触发 Print/set_Text** | **未确认**。探针已内置去重计数（`PRINT ... dup=N`、`SETTEXT ... dup=N`，`Bump()` 字典），但 run5 没有触发任何一次，所以没有数据 | 未确认 |

## 5. 给 Lead 的实机验证清单（用修好的脚本）

```powershell
# 1) 起游戏（会自动检查单实例）
pwsh -File D:\DSHWorkSpace\manosaba_a11y\probe\run_probe.ps1 -TimeoutSec 900 -Tag lead-a11y -Launch exe -PollSec 10
# 2) 另一个窗口/后台跑驱动（v2：焦点校验 + 多候选坐标 + Enter/Space 兜底）
pwsh -File D:\DSHWorkSpace\manosaba_a11y\probe\run5-automation.ps1 -TotalTimeoutSec 600 -Tag lead-a11y
```

进去之后要看我们的日志里这些行是否出现（前缀 `[ManosabaProbe]`）：

- `PRINT #1 printerId=Normal append=False speed=... author={id=... label=...} dup=1 hasPipeHash=True|False text=[...]`
  → **回答 B**：`hasPipeHash=True` 表示原文含 `|#`；`author={id=...,label=...}` 给出说话人；`text=[...]` 看有没有 `Ema:` 前缀。
- `SETTEXT #1 ... path=... dup=1 text=[...]` → 同一条是否被多个面板重复设置（配合 `dup` 判断）。
- `BASE_ADDMESSAGE #1 ...`（`UITextPrinterPanel` 会被 `WitchTrialsTextPrinterPanel`/`DebateTextPrinterPanel`
  override，看日志里的 `type=` 就知道哪条链先到、是否同时触发）。
- `CHOICE #1 id=... summary=[...] locked=... buttonPath=... liveButtons=N fieldProbe=ButtonsProp=? choiceButtonsField=?`
  → 选项文本与按钮列表可得性。
- `SCRIPTDUMP ... playedScript.path='...'` → 当前剧本路径（剧透闸门用 `Script.Path`，注意 `Script` **没有 Name 属性**）。

如果到第一个 `@choice` 前就卡住，至少拿到 5 条 `PRINT #` 即可回答 A/B。

## 6. 已知风险（供 Lead 参考，均为日志可验证的观察）

1. `UITextPrinterPanel.AddMessage` 是基类方法，子类 `WitchTrialsTextPrinterPanel` / `DebateTextPrinterPanel` 若
   `override` 了它，Harmony 的 `Patch(基类方法)` **不会**拦到子类 override 的调用（除非子类 `call base`）。
   所以"哪条链真正拿到台词"必须看实际日志，不能靠推断 —— 这也是 §5 要同时看 `PRINT` 与 `BASE_ADDMESSAGE` 的原因。
2. `RevealableText.set_Text` 每次逐字显示可能被调用多次（取决于 Naninovel 的 reveal 实现），日志可能很密；
   探针已做"前 40 次全打、之后只打新文本"的限流，仍建议 Lead 先跑 60 秒看量级。
3. Print 的 `message.Text` 是 `LocalizableText`：`(string)message.Text` 走的是 `op_Implicit`，
   **本地化切换后**（`IUIManager` 的 locale）是否返回已翻译文本未验证；run5 的 `fontName='SourceHanSerifSC'`
   说明当前字体已按中文配置加载。

## 7. 本次 run 的实例占用（已释放）

| 时间 | 事件 |
|---|---|
| 18:34:38 | run5-harmony 启动（PID 13620），v4 探针加载、9/9 PATCH_OK |
| 18:35:07 | 引擎初始化（frame=128, realtime=21.84s），标题画面 |
| 18:35:12–18:38:6x | 自动化点击未生效，场景保持 `Boot`，无 PRINT |
| **18:39:23** | **进程退出（Lead kill），实例释放；18:4x 起 Lead 接管** |
