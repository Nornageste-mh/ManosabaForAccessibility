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
| `0` | 限时选择里「立刻沉默」 |
| `Backspace` | 重读最近朗读过的一段 |
| `Alt`+`Shift`+`F1` | 进入 / 退出界面导航模式（**主键**；`Tab` 会失效，见下） |
| `Tab` | 备用导航键（Boot/加载阶段有效；标题画面出现后会被游戏吃掉） |
| `F7` | 切换语音后端（自动 → Tolk → NVDA → 争渡 → SAPI），并念出新的后端名 |
| `↑` `↓` | 上一项 / 下一项 |
| `←` `→` | 调整滑条 |
| `回车` / `空格` | 导航模式下激活控件；否则交给游戏推进剧情 |
| `Home` / `End` | 第一项 / 最后一项 |
| `PageUp` / `PageDown` | 切换面板组 |

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

## 当前进度（v0.0.0.2）

| 功能 | 状态 |
|---|---|
| 插件加载 / 启动播报（带语音后端名） | ✅ 实机验证 |
| 语音后端选择与运行中切换（`F7`） | ✅ 实机验证（`争渡读屏` ↔ `SAPI`） |
| 对话朗读（含旁白与对白） | ✅ 实机验证（单轮连续 82 句无异常） |
| 说话人朗读（`樱羽艾玛：……`） | ✅ 实机验证（旁白不报名字） |
| 文本清洗（`<br>`/`<ruby>`/`<link>`/`<color>` 等） | ✅ 实机验证 |
| 选项朗读 / 数字键选择 | ✅ 实机验证（第 1 章第 5 幕，2 个选项，数字键选中后走对应分支） |
| 加载播报（点「新游戏」后的黑屏加载段） | ✅ 实机验证（`LoadingUiExtended` 可见性钩子） |
| 菜单 / 设置界面中文名与当前值 | ✅ 别名表按实机取证建立（探针转储 79 个 `Selectable`） |
| 标题菜单导航（组合键进入 / 组名 / 按钮名 / 方向键移动） | ✅ 实机验证：`导航模式，标题画面，共 5 项。退出游戏，按钮。1 / 5`；下拉键 → `系统设置，按钮。2 / 5`、`鉴赏模式，按钮。3 / 5`；`Home` → 回到第 1 项 |
| 审判辩论（`@printDebate`）朗读 | ✅ 实机验证（Trial01，8 句连续；辩论不报名字） |
| 魔女手册键盘导航 | ⚠️ 通用规则可跑，**未实机复测** |
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
| 说话人 | `WitchTrialsTextPrinterPanel.SetMessageAuthor(MessageAuthor)` + `RevealableTextPrinterPanel.SetAuthorNameText(string)` Postfix |
| 选项（已挂，待实测） | `Naninovel.UI.ChoiceHandlerPanel.AddChoiceButton(ChoiceState)` Postfix → `Choices.OnButtonAdded` |
| 审判辩论（已挂，待实测） | `DebateTextPrinterPanel.AddMessage(PrintedMessage)` / `SetMessages(IReadOnlyList<PrintedMessage>)` |
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
