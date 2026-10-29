# NaninovelReProbe — 运行时探针（naninovel-re 交付物）

用途：在《魔法少女ノ魔女裁判》(IL2CPP, BepInEx 6) 运行时**实证** `re\naninovel-api.md` /
`re\patch-targets.md` 中标注「待探针验证」的条目。所有结论必须来自本探针的 `RE_*` 日志，不接受推断。

- 源码：`re\probe\NaninovelReProbe\`（BepInEx 6 `BasePlugin`，net6.0，引用本作 `BepInEx\core` + `BepInEx\interop`）
- 产物：`re\probe\NaninovelReProbe\bin\Release\NaninovelReProbe.dll`
- 运行：`pwsh -NoProfile -ExecutionPolicy Bypass -File re\probe\run_re_probe.ps1 -TimeoutSec 240 -Tag re`
- 输出：`re\logs\re-probe.log`（插件双写）、`re\logs\re-probe-run-<Tag>-<HHmmss>.log`（RE_ 行）、
  `re\logs\LogOutput-<Tag>.log`（完整 BepInEx 日志）、`re\logs\re-probe-journal-<Tag>.log`（归档）
- 哨兵：`re\probe\DISABLE_STRUCT_PATCHES` 存在时跳过 struct 参数补丁（或运行时 `-DisableStructPatches`）

## 补丁清单（安装时打 `RE_PATCH_OK <label> target=...`）

| label | 目标 | 形态 | 目的 |
|---|---|---|---|
| `TextPrinterManager.Print` | `Naninovel.TextPrinterManager.Print(string, PrintedMessage, bool, float, AsyncToken)` | prefix 带参数 | #1 printDebate 是否走这里；#2 struct 参数能否绑定 |
| `RevealableText.set_Text` | `Naninovel.UI.RevealableText.set_Text(string)` | postfix | 台词最终写入串 + 上游面板 author |
| `UITextPrinterPanel.SetWaitForInputIndicatorVisible` | 基类 | prefix(bool) | 点击等待指示器（基类，只能捕获未覆写的面板） |
| `ScriptableButton.OnButtonClick` | `Naninovel.UI.ScriptableButton` | prefix | 按钮点击（New Game / 选项 / 菜单） |
| `ChoiceHandlerPanel.AddChoiceButton` | `Naninovel.UI.ChoiceHandlerPanel` | prefix(ChoiceState) | 选项新增（struct 风险） |
| `UITextPrinterPanel.AddMessage` | 基类 | prefix(PrintedMessage) | 打印消息（struct 风险；游戏面板多为覆写） |
| `hit:TextPrinterManager.Print` | 同上 | **无参** prefix | 零绑定风险的"被调用"证据 |
| `hit:ChoiceHandlerPanel.AddChoiceButton` | 同上 | **无参** prefix | 同上 |
| `hit:DebateTextPrinterPanel.AddMessage` | `WitchTrials.Views.DebateTextPrinterPanel` | **无参** prefix | 辩论面板是否直接收消息（不经 manager） |
| `hit:DebateTextPrinterPanel.SetText` | 同上 | prefix(string) | 辩论面板直接 SetText 通道 |
| `hit:RevealableTextPrinterPanelModified.SetWaitForInputIndicatorVisible` | `GigaCreation.NaninovelExtender.Ui....` | prefix(bool) | ADV 面板覆写链（基类补丁抓不到） |
| `hit:WitchTrialsTextPrinterPanel.SetMessageAuthor` | `WitchTrials.Views.WitchTrialsTextPrinterPanel` | **无参** prefix | ADV 面板说话人通道 |

## 行为探针（`ReProbeBehaviour`）

- `RE_ENGINE_READY`：`Engine.GetService<ITextPrinterManager>()` 首次可用时刻、`DefaultPrinterId`。
- `RE_SUB_OK / RE_SUB_FAIL`：用 `DelegateSupport.ConvertDelegate<Il2CppSystem.Action<bool|Script>>` 订阅
  `IScriptPlayer.OnWaitingForInput / OnAutoPlay / OnPlay / OnStop`；`RE_EVENT ...` 为回调。
- `RE_AUTOPLAY_OK`：`IScriptPlayer.SetAutoPlayEnabled(true)`，让台词在无人值守下自动推进。
- `RE_TITLE_SCAN / RE_TITLE_INFO / RE_TITLE_CLICK_OK / FAIL`：程序化点 `Naninovel.UI.TitleNewGameButton.OnButtonClick()`（不走鼠标）。
- `RE_PLAY_FALLBACK_OK / FAIL`：若 24s 仍无打印，改用 `IScriptPlayer.Play("<已提取脚本路径>")` 作对照。
- 每 10s `RE_STATE`：`playing/waitingInput/autoPlay/skip/playedIndex/count/script/spot/cmd/voice`、
  `panelText=[面板类型/名字 text=[...] author={...}]`、`printCalls/printHits/setTextCalls/...`、
  场景内全部 `UITextPrinterPanel`（`type/name/active/msgs`）、`ChoiceHandlerPanel/ChoiceHandlerButton` 与选项摘要。
- 18 次状态后 `RE_SUMMARY` 汇总全部计数器。

## 判定规则

- 只有 `RE_PATCH_OK` 且出现对应 `RE_HIT_*` / `RE_PRINT` / `RE_SETTEXT` 才算「该补丁在真机被调用」；
  只有 `RE_PATCH_OK` 没有命中 → 记为「可挂载但本路径未触发」。
- 补丁体全部 try/catch，异常打 `RE_*_EX` 且不向游戏抛出；安装失败打 `RE_PATCH_FAIL <label>`。
- 日志中的台词文本仅用于技术验证，不复制进公开文档（`re\` 已 gitignore）。

## 运行记录（2026-10-28）

| Tag | 时长 | 结果 | 证据 |
|---|---|---|---|
| probe1 | ~14s（崩溃） | `BasePlugin.Load()` 里直接 Harmony 安装 → `Il2CppDetourMethodPatcher` 生成 trampoline 时 `AccessViolationException`。**修复**：安装延后到 `Engine.GetService<ITextPrinterManager>()` 成功之后；移除 `ScriptableButton.OnButtonClick` 补丁 | `re-probe-run-probe1-185732.log` |
| probe2 | 180s | 补丁全部安装成功；标题阶段无打印；裸 `IScriptPlayer.Play()` 抛 `not loaded` | `re-probe-run-probe2-185840.log` |
| probe3 | 200s | `LoadAndPlay(Adv01)` 进正片；`Print` 1 次/句、`set_Text` 4 次（3 空）；`AddMessage` 0；`DebateTextPrinterPanel.SetText` 挂载失败 | `re-probe-run-probe3-190340.log` |
| probe4 | 240s | 新增 `parts[i].id/.spot` 与 `PrintDebateText.DebatePrinterId` 证据；`AuthorDesc` 改读 `PrintedMessage.author` 字段（旁白 = `id=null` 而非 NRE）；Trial01 `LoadAndPlay` 未能执行辩论脚本；Adv05 进正片后 autoplay 停在首句（`printCalls=1` 恒定 2 分钟） | `re-probe-run-probe4-191710.log`（103 行 RE_*） |

probe4 关键行（原文摘录，技术验证用）：

```
RE_DEBATE_PRINTER_ID static PrintDebateText.DebatePrinterId='Debate'
RE_PRINT #1 printerId='Normal' append=False speed=1 author={id='<null>' label=''} text=[...]
RE_PRINT_PARTS id#1 count=1 [0]{id='0101Adv05_Narrative001' plain=False spot=Act01_Chapter01/Act01_Chapter01_Adv05:11 text=[]}
RE_SUMMARY printCalls=1 printHits=1 setTextCalls=4 setTextEmpty=3 printsWithAuthor=1 printPartsLogged=1 choiceLogs=0 choiceAddHits=0 debateAddMsgHits=0 debateSetTextHits=0 advAuthorHits=4 buttonClicks=0 authorReads=4
```

结论已回填 `re\naninovel-api.md` 第 9 节与 `re\patch-targets.md` 第 0 节。
