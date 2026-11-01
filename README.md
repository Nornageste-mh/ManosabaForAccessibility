# ManosabaA11y

> 《魔法少女ノ魔女裁判》的读屏无障碍补丁。
> 平台层与契约层来自 [A11yFramework](https://github.com/Nornageste-mh/Framework)。

## 说明

让使用屏幕阅读器的玩家也能游玩本作：剧情文本自动朗读、选项可读可键选、
菜单可键盘导航。

- 路线：**IL2CPP + BepInEx 6.0.0-be.788**（游戏是 Unity 6000.0.48f1 / IL2CPP / Naninovel）
- 插件入口：`BepInEx.Unity.IL2CPP.BasePlugin`，TFM `net6.0`
- **不做什么**：不改游戏文件；不解包/不重打包游戏资源；不朗读任何未经实机验证的推测内容；
  不做 OCR（文本来自引擎事件与场景对象，不截图识别）；不上传任何数据。

## 安装（仅限正版玩家）

见 [`mod/package/安装说明.md`](mod/package/安装说明.md)。一句话版：把发布包里的东西
整体拷进有 `manosaba.exe` 的那一层，启动游戏，听到「无障碍补丁已加载」即成功。

第一次启动会比较慢：BepInEx 6 的 Il2CppInterop 要生成 `BepInEx\interop\`（160 个程序集），
生成完再启动就快了。**不要把 interop 目录发给别人**（跟机器/游戏版本绑定）。

## 主要快捷键

| 快捷键 | 功能 |
|---|---|
| `1`-`9` | 选择对应编号的选项 |
| `0` | 立刻停止朗读（沉默键，不改变游戏状态） |
| `=` | **重读上一句剧情/选项**（有配音的句子也会被念出来） |
| `Alt`+`Shift`+`F1` | 进入 / 退出界面导航模式（**主键**；`Tab` 会失效，见下） |
| `Tab` | 备用导航键（Boot/加载阶段有效；标题画面出现后会被游戏吃掉） |
| `F7` | 切换语音后端（自动 → Tolk → NVDA → 争渡 → SAPI），并念出新的后端名 |
| `Alt`+`Shift`+`F2` | **低性能模式**五档循环：关闭 → 轻度 → 均衡 → 强力 → 极限 → 关闭（默认关闭；见下） |
| `↑` `↓` | 上一项 / 下一项 |
| `←` `→` | 调整滑条 |
| `回车` / `空格` | 导航模式下激活控件；否则交给游戏推进剧情 |
| `Ctrl`+`Alt`+`Shift`+`F3` | **剧情跳转菜单**（开发者功能，默认关闭，需先在 cfg 里打开「允许剧情跳转（开发者）」） |
| `Home` / `End` | 第一项 / 最后一项 |
| `PageUp` / `PageDown` | 切换面板组；**地图选择时**翻页（名单超过 9 项）；地图浏览时切楼层 |
| `Esc` | 关闭魔女手册 / 对话记录这类整页界面（并念「已关闭…」） |

> `0.0.0.11` 起默认重读键是 `=`（Backspace 会与本作自带的“返回上一句”冲突）；
> 审判议论的台词默认不自动朗读（带配音），但每句都进重读缓冲，按 `=` 一样能听。

**低性能模式（`0.0.0.16` 起是五档循环，默认关闭）**：按 `Alt`+`Shift`+`F2` 一轮一轮往上加，
听到「低性能模式：轻度 / 均衡 / 强力 / 极限」，再按一次回到「已关闭」（并把画面设置全部还原）。

| 档位 | 做了什么 | 实测（标题画面） |
| --- | --- | --- |
| 轻度 | URP 画质降级（渲染缩放 0.8、关 MSAA/主光阴影/软阴影/阴影距离）+ 关掉 800 多个柔边阴影 | ≈14.4 帧 |
| 均衡 | 再降渲染缩放 0.65、**窗口 0.85×**、质量等级最低、纹理半分辨率、关抗锯齿与阴影、LOD 0.7 | ≈17.3 帧 |
| 强力 | 再降渲染缩放 0.5、**窗口 0.70×**、纹理 1/4、关各向异性与实时反射、关 VSync 锁 30 帧、停粒子 | ≈20.4 帧 |
| 极限 | **窗口 0.50×** + 画面整个丢掉（相机不画、Renderer 停用、UI 刷黑，只剩黑窗口）+ 强制最低 LOD/单骨骼、关所有 URP Volume、相机后处理全关、**停所有 Animator**、锁 30 帧 | 比强力再高 |

（关闭档 ≈10.5 帧；这个表是测试机 兆芯 KX-U6780A + 核显、标题画面量的，收益取决于场景。）

**关于「CG/立绘/过场一出现就卡」（`0.1.0.0` 查证）**：这类卡顿是**主线程**造成的，
把画面降级（渲染缩放、关阴影、纹理 1/4、窗口缩小）几乎没用（实测 12.1 帧 vs 基线 13.5 帧）。
唯一沾边的是「界面动画」——但两轮测量互相矛盾（15.5 帧 / 8.1 帧），拿不到可靠结论，
所以只留了一个**默认关闭**的可选开关「性能 / 停止界面动画（强力档起）」，想试可以打开。
剩下的主要是游戏自己的逐字文本排版与 UI 重建，模组侧改不动。

**极限档为什么还能操作**：隐藏 UI 用的是「把每个 UI 图元的颜色刷黑」，不是关 Canvas/CanvasRenderer——
后两者会让 UGUI 的射线检测一起失效，点击就全废了（游戏软锁）。刷颜色不参与射线判定，
所以画面全黑时点击照样能推进剧情（`probe\logs\run127a-summary.txt`：黑屏期间点击 26 次，
朗读从 1 句涨到 13 句）。怕误触可以在配置里关掉「性能 / 允许极限档（画面全黑）」。

「降窗口分辨率」从 0.0.16 起**算进档位**（均衡 0.85× / 强力 0.70× / 极限 0.50×，都以原生分辨率为基准）。
不想让窗口尺寸变就关掉配置里的「性能 / 允许改变窗口分辨率」（默认开，关掉后其它手段照旧生效）。

**为什么导航不是 Tab 了**（2026-10-29 用户实机反馈）：本作标题画面出现后会把 `Tab`
当自己的菜单键吃掉，所以「主界面加载前按 Tab 有反应、加载后没反应」。主键因此改成
`Alt`+`Shift`+`F1`，`Tab` 保留作备用（配置「界面导航 / 同时允许 Tab」可关）。
两个键都能在 `BepInEx\config\manosaba.a11y.reader.cfg` 里改。

**听不到声音？先按 `F7`**：同时装了 NVDA 和争渡的机器，`语音后端 = 自动` 会按平台顺序
挑中 NVDA；如果你平时用争渡，就会一句都听不到。在游戏里按 `F7` 换到争渡即可，
启动播报也会念出当前后端名。

**游戏原生占用的键**（模组不会去抢，也不要拿来当重读键）：
本作是 Naninovel，`Ctrl` 快进、`Enter`/`Space`/鼠标左键推进、`Esc` 菜单。
补丁只在**界面导航模式**和**选项编号键**上接管输入，其余按键原样交给游戏。

## 当前进度（v0.0.0.13）

| 功能 | 状态 |
|---|---|
| 插件加载 / 启动播报（带语音后端名） | ✅ 实机验证 |
| 语音后端选择与运行中切换（`F7`） | ✅ 实机验证（`争渡读屏` ↔ `SAPI`） |
| 低性能模式（`Alt`+`Shift`+`F2`，默认关闭） | ✅ 实机验证（开启/还原、柔边阴影 579 个、低帧率提醒）；画面降级在本机无收益 |
| 对话朗读（含旁白与对白） | ✅ 实机验证（单轮连续 82 句无异常） |
| 说话人朗读（`樱羽艾玛：……`） | ✅ 实机验证（旁白不报名字） |
| 文本清洗（`<br>`/`<ruby>`/`<link>`/`<color>` 等） | ✅ 实机验证 |
| **有配音的行不自动朗读**（默认完全安静，`Backspace` 可 TTS 重读） | ✅ 实机验证（run58：22 条配音行跳过，退格念回原文；v0.0.0.6 起默认连名字也不报） |
| **`=` 重读上一句剧情/选项** | ✅ 实机验证（run57/run58 用的是旧默认键 Backspace；0.0.0.11 起默认改为 `=`，已单元验证可解析）；导航播报不会被退回 |
| **BepInEx 控制台窗口** | ✅ **v0.1.0.0 起不创建**（随包 `BepInEx\config\BepInEx.cfg` 里 `[Logging.Console] Enabled = false`）——不是「建出来再藏」，所以开局连闪一下都没有；旧版靠 `ShowWindow(SW_HIDE)` 隐藏，实测必须先校验 `ConsoleWindowClass`，否则会把游戏窗口一起藏掉 |
| 选项朗读 / 数字键选择 | ✅ 实机验证（第 1 章第 5 幕，2 个选项，数字键选中后走对应分支） |
| 加载播报（点「新游戏」后的黑屏加载段） | ✅ 实机验证（`LoadingUiExtended` 可见性钩子） |
| 菜单 / 设置界面中文名与当前值 | ✅ 别名表按实机取证建立（探针转储 79 个 `Selectable`） |
| 标题菜单导航（组合键进入 / 组名 / 按钮名 / 方向键移动） | ✅ 实机验证：`导航模式，标题画面，共 5 项。退出游戏，按钮。1 / 5`；下拉键 → `系统设置，按钮。2 / 5`、`鉴赏模式，按钮。3 / 5`；`Home` → 回到第 1 项 |
| 审判辩论（`@printDebate`）朗读 | ✅ 实机验证（run59 完整审判段落：16 句朗读、34 条辩论日志；辩论不报名字） |
| 审判画面 HUD（跳过按钮） | ✅ `DebateUI/Wrapper/SkipButton/Button → 跳过辩论` |
| 审判倒计时 / HP（0.0.0.10 新增） | ⚠️ 已挂三个 void 补丁（`Timer.SetTime` / `Timer.ForceRemainingTimeToNearlyZero` / `DebateUi.OnCurrentHpChanged`），只在跨过 5分/2分/1分/30秒/10秒阈值时报一句；**未实机复测**（需打到审判战斗段） |
| **剧情跳转菜单（开发者）** | ✅ 实机验证（0.0.0.12）：`SCRIPTJUMP 打开菜单 index=17 共 440 个剧本` → 下移两项 → `SCRIPTJUMP 选中 index=19 path=Act01_Chapter01/Act01_Chapter01_Adv20` → `SCRIPTJUMP 跳转 path=…Adv20 line=0`；真实按键未复测（本机键盘注入被系统挡掉） |
| **魔女手册 / 对话记录整页界面** | ✅ 实机验证（run91）：打开即念「魔女手册，线索页签。内容：…」，地图页念「1F：会客厅、玄关大厅、…」；`Esc` 直接关闭并念「已关闭魔女手册。」（平台层射线被遮罩挡住，改由逐作层自己处理） |
| 魔女手册（图鉴）控件命名 | ✅ 实机验证（run61：10 个组 = 记录页签/规定页签/地图页签/人物页签/魔女手册 14 项/线索页签；地图 29 个地点全部念中文名） |
| 魔女手册方向键逐项移动 | ⚠️ 代码路径与标题菜单相同，但自动化合成扩展键不稳定，**未复测**（请实机按方向键确认） |
| **地图页选择地点（`@choiceMapArea`）** | ✅ 实机验证（run122）：读游戏自己的允许名单，一次念全 8 个地点（含 2F/地下1F 跨层），数字键 3/7/8 分别走食堂/娱乐室/牢房分支；名单超 9 项时 `PageUp`/`PageDown` 翻页 |
| **审判「出示证据 / 选证物」（`@choiceEvidence`）** | ✅ 实机验证（run124）：证物卡是纯图标，靠「点一张读标题」枚举出名字后播报「请出示证据。可选的证物共 1 件：1、二阶堂希罗原本持有的钢笔。按数字键选择。」；数字键=选中、**回车/空格=出示**（不替玩家按「是」），确认框照读文案与按钮；实测选「是」后剧本从 `idx=191` 走到 `idx=197` |
| 地图浏览态楼层切换（`PageUp`/`PageDown`） | ⚠️ 只编译验证：选择态禁用切层（会软锁），浏览态切层未单独复测 |
| 游戏文本语言切换（简体中文 ↔ 日文） | ✅ 实机双向验证（zh-Hans / ja 两种输出） |

详细依据见 [`CHANGELOG.md`](CHANGELOG.md) 与 [`无障碍可行性验证.md`](无障碍可行性验证.md)。

## 已知问题 / 注意事项

- **IL2CPP 的 Harmony 铁律：不要 patch 返回 `UniTask` 的方法。**
  `Naninovel.TextPrinterManager.Print` 就是这么被踩掉的：补丁一挂，剧情停在第一句、
  任何输入都推不动，日志里是 `MethodAccessException: ... 'Naninovel.Async.IAwaiter.get_IsCompleted'`。
  现在文本改由 `RevealableText.set_Text`（返回 void）取，已修。
- IL2CPP 的 `as` / `is` 对 interop 代理类型**恒失败**，必须用 `TryCast<T>()`（陷阱 I1）。
- 原生接口写回的托管数组必须用 `Il2CppStructArray<T>`（陷阱 I2）。
- 对象名 / 方法名**不许猜**，一律以 probe 场景树与反编译实查为准（陷阱 I3）。
- 若 `BepInEx\interop\` 里出现 **0 字节** dll，说明 interop 生成失败：删掉整个
  interop 目录再启动一次游戏，然后看 `BepInEx\LogOutput.log`。
- 窗口焦点：本作实测可以抢到前台（`focused=True`），但**旧输入 API `UnityEngine.Input`
  在本作整体不可用**（Player Settings 只启用 Input System，实机探针原文异常：
  `You are trying to read Input using the UnityEngine.Input class...`）。
  因此 `A11yHost.UseGlobalKeys = true` —— 热键走 Win32 `GetAsyncKeyState` 兜底，
  不依赖 Unity 的输入后端。**自动点击推进剧情在本作上不可靠**（约 6 FPS，点击常被吞），
  本项目所有验收都是真人点击 + 日志核对。
- 本作 UI 有两类按钮：`WitchTrials.Views.TitleUi._newGameButton` 是标准
  `UnityEngine.UI.Button`；而 `WitchTrialsButton : RebuildableButton : MonoBehaviour`
  内部包着一个 `public unsafe Button _uiButton`。导航扫描遇到后者要
  `TryCast<Button>(btn._uiButton)`，不能假设所有可点对象都是 `Selectable`。
- **纯图标的卡片要自己补名字**：证物卡（`WitchBookItemButton`）既没有 `_label` 文字，子树里也没有
  TMP 文本，名字只出现在「选中之后页面右侧的标题」里 —— 只能点一张读一次，并且**必须缓存**
  （不缓存会每 0.5 秒重扫、反复枚举）。
- **WorldSpace 画布要回退 `Camera.main`**：`RectTransformUtility.WorldToScreenPoint(null, …)` 在
  WorldSpace 画布上算出来是负坐标（实测 `screen=-7,-2`），点击会落到屏幕外，而界面毫无反馈。
  注入点击前一定要做屏幕坐标边界检查。
- **`IsPresentEvidenceMode` 不可信**：真正的选证物界面里它也是 `false`。判据要读游戏自己的限制
  （`WitchBookScreen.RestrictionCategories` 含 `Clue` 且 `RestrictionAreas` 为空）。
- **无障碍层不能替玩家做决策**：数字键只负责「选中」，出示必须由玩家按回车触发；确认框里的
  「是」永远由玩家自己按键。
- `BepInEx` 会**递归扫描 `BepInEx\plugins` 的子目录**：把插件挪进 `plugins\_stash\`
  并不会禁用它，必须挪到游戏目录之外。做「无补丁对照组」时踩过这个坑。

## 构建

```powershell
cd mod
.\build.ps1 -GameDir "D:\Steam\steamapps\common\manosaba_game"   # 编译 + 部署 + 组装 package\
.\build.ps1 -NoDeploy                                            # 只编译 + 组装包，不动游戏
.\release.ps1                                                    # 校验 + 打 dist\ManosabaA11y-<版本>.zip
```

前置条件：游戏已装 BepInEx 6.0.0-be.788 并**启动过一次**（否则没有 interop 可引用）；
本机 .NET SDK 8 即可编 net6.0（SDK 会从 NuGet 缓存还原 6.0 的引用程序集）。

## 自动化测试

剧本 34,836 个文本块 / 79 万字，人工全流程回放不可行；本项目改为**按界面/剧情阶段/场景/模块**做自动化验收：

```powershell
pwsh -File tests\Suite.ps1 -Only unit.selftest   # 单元层：纯逻辑 45 条断言（约 40 秒）
pwsh -File tests\Suite.ps1                       # 全部 11 个用例：标题/对话/说话人/配音/退格/选项/辩论/加载/整页界面/语言
```

- 单元层在插件进程内跑（`mod\src\ManosabaA11y\SelfTest.cs`，`MANOSABA_A11Y_SELFTEST=1`）：IL2CPP 下 `TextProc` 等类型进程外引用不到，放运行时里跑才可信。
- 集成层每个用例**独立启动一次游戏**，真点击进正片 → 跳目标剧本 → 断言日志证据行，产出 `tests\reports\<Tag>-report.md` + JSON + 截图；退出码 = 失败数。
- 测试**不需要先开 Steam**：游戏目录有 `steam_appid.txt`。
- 键盘类断言（`Esc`/`Backspace`）走插件内钩子 `MANOSABA_A11Y_TEST_ESC` / `MANOSABA_A11Y_TEST_REPEAT`：
  本机自动化环境的键盘注入被系统挡掉（`SendInput` 返回 0），鼠标注入可用。
- 细节、新增用例模板、已知局限见 [`tests/README.md`](tests/README.md)。

### 目录结构

```
mod/
  build.ps1           构建：检查 interop → 取 BepInEx 6/NVDA 客户端 → 编译 → 部署 → 组装 package\
  release.ps1         校验发布红线 → makezip 打 zip → ZipFile 自检无反斜杠条目
  src/ManosabaA11y/
    ManosabaA11y.csproj    net6.0 / AllowUnsafeBlocks / CodePage 65001 / ManosabaGameDir 属性
    Plugin.cs              入口（BasePlugin）+ 配置 + PluginBehaviour(MonoBehaviour) + Patches 占位
    A11yHost.cs            契约层（逐字节来自 Framework，不改）
    Speech/Nvda/Sapi/Zdsr/Hotkey/KeyEdge/GlobalKeys/Repeat/UiNav.cs   平台层（逐字节来自 Framework，不改）
    TextProc.cs            文本清洗/说话人/该不该读（逐作）
    Reader.cs              朗读编排：pending 缓冲 + 去重 + 可见性 + 说话人前缀
    Patches.cs             Harmony 补丁点（每一处都注明实查依据）
    Choices.cs             选项面板：每帧扫容器、内容变化才播报、数字键选择
    Locale.cs              文本语言（跟随游戏 / 简体中文 / 日本語）
    SpeakerNames.cs        AuthorId → 中文名（来自游戏 CharacterNames.txt 实查）
    UiVis.cs               可见性判定（沿父链查 CanvasGroup.alpha；Naninovel 面板隐藏时对象仍 active）
    UiNav.Game.cs          UiNav 的 partial 逐作区（别名表 + 输入框角色）
    InputInject.cs         仅测试用：InputSystem 状态事件注入（发布版不启用）
    TestDriver.cs          仅测试用：MANOSABA_A11Y_TEST=1 时自动走流程并 dump 状态
  tools/makezip.ps1      打 zip（正斜杠条目）
  licenses/              第三方许可证 + THIRD-PARTY-NOTICES.txt
  package/               构建产物：直接拷进游戏根目录
```

### 模组做了什么

| 功能 | 挂载点 |
|---|---|
| 插件加载 / 配置 / 每帧驱动 | `Plugin.Load()` + `AddComponent<PluginBehaviour>()`（BepInEx 6 的 BasePlugin 没有 Update） |
| 朗读出口 | `Repeat`（平台层唯一出口；L3 不许直接调 `Speech.Speak`） |
| 启动播报 | `PluginBehaviour.Update()` 首帧 `Speech.Init` 后 `Repeat.Say` |
| 对话 / 旁白文本 | `Naninovel.UI.RevealableText.set_Text` Postfix → `Reader.OnRevealText` |
| 行 id / 配音判定 | `Naninovel.UITextPrinter.AddMessage(PrintedMessage)` + `SetMessages(...)` Postfix（返回 `void`，可安全挂）→ `Text.parts[i].id` → `VoiceLineIds` |
| 说话人 | `WitchTrialsTextPrinterPanel.SetMessageAuthor(MessageAuthor)` + `RevealableTextPrinterPanel.SetAuthorNameText(string)` Postfix |
| 选项 | `Naninovel.UI.ChoiceHandlerPanel.AddChoiceButton(ChoiceState)` Postfix → `Choices.OnButtonAdded` |
| 审判辩论 | `DebateTextPrinterPanel.AddMessage(PrintedMessage)` / `SetMessages(IReadOnlyList<PrintedMessage>)` |
| 文本语言切换 | `ILocalizationManager.SelectLocale("zh-Hans")` |
| 界面导航 | 平台层 `UiNav` 每帧扫描（`TickUiNav` 钩子）+ `UiNav.Game.cs` 逐作规则 |

### 几条踩过的坑（改之前请先读）

1. **BepInEx 6 的 `BasePlugin` 不是 MonoBehaviour**：没有 `Awake`/`Update`，每帧逻辑必须
   `AddComponent<一个 MonoBehaviour>()`（同 Mono 模板的 `BaseUnityPlugin` 写法在 IL2CPP 下编不过）。
2. **framework 的 `new_game.py` L1 列表漏了 `GlobalKeys.cs`**（KeyEdge.cs 直接引用它），
   新作骨架会 CS0103；已手工补入，见 [`docs/framework-notes.md`](docs/framework-notes.md)。
3. **`Resources.FindObjectsOfTypeAll` 在 IL2CPP 收 `Il2CppSystem.Type`**，不是 `System.Type`，
   要 `Il2CppType.From(...)` 转；`as Component` 恒 null，要 `TryCast<Component>()`。
4. **不要用 `Compress-Archive` 打发布包**：Windows PowerShell 5.1 会写反斜杠条目，
   Linux/macOS 解压成一坨；一律走 `tools/makezip.ps1`。
5. **`BepInPlugin` 版本号必须纯数字四段**（`System.Version`）；带字母会让 BepInEx
   **静默跳过整个插件**，日志只有一行 Warning。
6. **永远不要 patch 返回 `UniTask`/`Task`/非 blittable 结构体的方法**（本作实测：
   `TextPrinterManager.Print` 的 Prefix 会搬坏返回值 → `PrintText.Print` 的 `await` 抛
   `MethodAccessException` → 剧情永久卡死、任何输入都推不动）。要文本就挂返回 `void`
   的 `UITextPrinter.AddMessage` / `RevealableText.set_Text`。
7. **面板级 `UITextPrinterPanel.AddMessage` 在本作 ADV 路径不触发**（挂了也一条日志没有）：
   ADV 真入口是 actor 级的 `Naninovel.UITextPrinter.AddMessage`，行 id 只能从这里拿。
8. **`ShowWindow(GetConsoleWindow(), SW_HIDE)` 必须先校验窗口类名**：实测本作里
   `GetConsoleWindow()` 返回的句柄可能就是游戏主窗口，直接隐藏会把游戏窗口一起藏掉。
9. **发行版里没有可用的开发者面板**：`DebugOptionsBase.s_debugManager` 实测是 null
   （`s_isInitialized` 却是 true），`DebugPanel` / `EnableDebugModeButton` /
   `DebugCommandButtons` 只有内存里的 prefab 资产、从未实例化 → 发行版按
   `Ctrl+Alt+Shift+F3` 没有面板可开（这是开发构建功能）。
   **0.0.0.12 起改成「跳转到指定剧情」**（`ScriptJump.cs` + 生成的 `ScriptIndex.cs`）：
   组合键打开菜单，上下键换剧本、数字键输入起始行、回车跳转，走
   `ScriptPlayerExtensions.LoadAndPlayAtLine`（0.0.0.9 已实机验证过这条 API）。
   配置项相应改名「允许剧情跳转（开发者）」，仍默认关闭。
10. **整页界面不要指望平台层的可达性判定**：魔女手册打开时有一层全屏 CanvasGroup 挡在
    关闭按钮上面，`UiNav.VisiblyClickable` 的 UI 射线会判「点不到」（玩家实际能点）——
    平台层按约定不能改，逐作层的做法是 `ScreenAssist.cs`：自己播报页面内容 + `Esc` 直接
    调 `CloseButton.onClick`。地图地点（`MapAreaLabel`）根本不是 `Selectable`，平台层
    不会扫，必须单独取。
11. **本机自动化环境的键盘注入不可靠**：`keybd_event`/`SendInput` 实测被系统挡掉
    （`SendInput` 返回 0），鼠标注入可用；所以验证 Esc/退格这类键位时，用插件内的
    `MANOSABA_A11Y_TEST_ESC` / `MANOSABA_A11Y_TEST_REPEAT` 钩子，真实按键留给玩家确认。
12. **窗口客户区尺寸会变**（实测见过 1920×1080 与 1280×720 两种）：自动化脚本一律用
    **相对坐标**（`GetClientRect` 后按比例算），写死像素坐标会「点空」。
13. **`PrintWindow` 对 DXGI 翻转模型窗口会返回白图**：截图改为 `GetWindowRect` +
    `CopyFromScreen`（见 `probe\cap-screen.ps1`）。

14. **本作的按钮不能靠合成派发点**：`onClick.Invoke()` 与 `ExecuteEvents.pointerClickHandler`
    都返回成功（诊断 `点击派发 ok=True`），但 `MapPage.OnChoiceMapAreaAsync` 的 await 一直不返回。
    必须 `InputInject.Click(screenX, screenY)` 往 `InputSystem` 事件队列塞真实的按下/抬起。
15. **地图选择等待期间切楼层会软锁**：点手册底部胶片条（`WitchBookItemButton`）会把
    `OnChoiceMapAreaAsync` 的 await 挂断，加载界面永久卡在可见状态（run105/106）。选择态必须禁用切层。
16. **自动化脚本的「点屏幕中央」会抢先完成地图选择**：run115 里数字键选娱乐室却走了玄关大厅分支，
    原因就是脚本每 3 秒点中央真点中了默认高亮的地点。检测到选择态后必须立刻停止点击。

## 如何复现分析

- 剧本/文本：先跑 `probe\`（文本抽取与场景树），产物只留统计数据，**不留台词原文入库**。
- 运行时：`BepInEx\interop\` 由游戏首次启动生成；接口签名从 interop dll 读，
  行为从 `BepInEx\LogOutput.log` 与 probe 事件日志确认。
- 结论写进 [`无障碍可行性验证.md`](无障碍可行性验证.md)；每一处补丁都要在源码注释里
  写清依据（哪个类型/方法、怎么验证的）。

## 合规说明

- 本补丁为第三方非官方项目，与游戏开发/发行方无关；**仅限正版玩家**。
- 不随仓库分发任何游戏资源、反编译产物、剧本导出（见 `.gitignore` 与 `tools\check-staged.ps1`）。
- 随发布包分发的第三方组件与许可证见 `mod\licenses\THIRD-PARTY-NOTICES.txt`。
- 细节见 A11yFramework README 的「IP 与合规」一节。

## 许可

- 本补丁自身源码：MIT，见 [`LICENSE`](LICENSE)（与 A11yFramework 保持一致）。
- 平台层 / 契约层文件归 A11yFramework，逐字节复用、未修改。
- 第三方组件：各自许可证，全文在 `mod\licenses\`。
