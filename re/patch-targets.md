# A11Y 补丁点清单（Naninovel 核心 + GigaCreation 扩展层）

本作：`D:\Steam\steamapps\common\manosaba_game`（Unity 6000.0.48f1 / IL2CPP / Naninovel / Addressables / zh-Hans / 全语音）。
本清单范围：**Elringus.Naninovel.Runtime.dll、Naninovel.Common.dll、GigaCreation.NaninovelExtender.\***。`WitchTrials.*` 游戏层由 witchtrials-re（`re\witchtrials-ui.md`）负责，本清单只在“链路交界”处登记。

证据约定：
- 「实查」= 本作 `BepInEx\interop` 的 `ilspycmd -t` 反编译签名（原始 dump：`re\raw\naninovel\*.cs`、`re\raw\giga\*.cs`；签名摘录：`re\decompiled\signatures\*.txt`）。
- 「参考」= `D:\DSHWorkSpace\work\decompiled\`（同代 Naninovel 的 Mono 构建，有方法体；只能当行为参考，不是本作证据）。
- 「待探针」= 必须运行时插件确认，未确认前不得写进最终补丁。

---

## 0. 运行时实测状态（probe2 / probe3 / probe4，2026-10-28；本作真机）

证据：`re\logs\re-probe-run-probe4-191710.log`（103 行 RE_*）、`re\logs\re-probe-run-probe3-190340.log`、`re\logs\re-probe-run-probe2-185840.log`；探针源码 `re\probe\NaninovelReProbe\ReProbePlugin.cs`；运行脚本 `re\probe\run_re_probe.ps1`；解析见 `re\naninovel-api.md` 第 9 节。

| 挂点 | 实测结果 | 结论 |
|---|---|---|
| `TextPrinterManager.Print` prefix（struct 参数） | RE_PATCH_OK；命中 1 次/句：`printerId='Normal' append=False speed=1`，`message.Text` 中文已本地化 | **可用，主钩子** |
| `TextPrinterManager.Print` 的 `message.Text.parts[i]` | 实测拿到 `id='0101Adv05_Narrative001'`、`spot=Act01_Chapter01/Act01_Chapter01_Adv05:11`、`plain=False`、`text=[]` | **lineId/剧本行号权威来源**（剧透闸门、语音对齐都用它） |
| `RevealableText.set_Text` postfix | RE_PATCH_OK；每句命中 4 次，其中 3 次为空串（面板初始化/重置），1 次正文（probe4：`setTextCalls=4 setTextEmpty=3`） | **可用，但必须过滤空串 + 按实例去重** |
| `UITextPrinterPanel.AddMessage` prefix | RE_PATCH_OK 但 ADV 打印时命中 **0** 次 | 不作主钩子 |
| `WitchTrialsTextPrinterPanel.SetMessageAuthor` prefix | RE_PATCH_OK；命中 4 次/句 | 游戏 ADV 面板的真作者钩子（witchtrials-re） |
| `DebateTextPrinterPanel.SetText` prefix | **probe3/probe4 两次独立进程均 `HarmonyException: IL Compile Error`** | **确认不可挂**；辩论改用 `AddMessage`/`SetMessages` |
| `DebateTextPrinterPanel.AddMessage` prefix | RE_PATCH_OK（两轮均未真正进辩论，命中 0） | **可挂但尚未实测触发** |
| `WitchTrials.Views.PrintDebateText.DebatePrinterId` | probe4 实读：`'Debate'`（public static 字段，`RE_DEBATE_PRINTER_ID`） | **判定辩论行：`printerId=='Debate'`**（无需拿 manager 单例） |
| `ChoiceHandlerPanel.AddChoiceButton` prefix | RE_PATCH_OK（两轮均未到达选项行，命中 0） | **可挂但尚未实测触发** |
| `IScriptPlayer` 事件 `Action<bool>/Action<Script>` | `RE_SUB_OK` + `RE_EVENT OnWaitingForInput/OnAutoPlay/OnPlay/OnStop` 实收 | **可用** |
| `TitleNewGameButton` 自动进正片 | `FindObjectsOfType`=0；游戏标题是自绘 UI（`Button_NewGame`），点击只重播 `System/System_Title` | 不可靠，**改 LoadAndPlay** |
| `ScriptPlayerExtensions.LoadAndPlay(player, path, 0)` | 实测成功进入 ADV 正片（probe4 #2：`RE_EVENT OnPlay script=Act01_Chapter01/Act01_Chapter01_Adv05`、`playing=True`、`printCalls=1`）；但 `LoadAndPlay(Trial01)` 未能让辩论脚本执行（trial 状态门控） | **无人值守进 ADV 主路径**（裸 `Play` 抛 `not loaded`） |
| 旁白行 author 形态 | probe4 用 `PrintedMessage.author` **字段**读法：旁白时 author 对象**非 null**，但 `id='<null>' label=''`；`PrintedMessage.Author`（Nullable getter）在旁白时抛 NRE | 旁白判定 = `author==null \|\| string.IsNullOrEmpty(author.Id)`；**读字段，勿读 `Author` 属性** |
| lineId 来源 | `PrintedMessage.Text` 不含 `\|#lineId\|`、不含 `Name:` 前缀；`LocalizableText.parts[i]` 是 `LocalizableTextPart{ string id; PlaybackSpot spot; string text; bool PlainText; }` | **lineId 读 `Text.parts[i].id` + `.spot`** |
| autoplay/skip 自动推进 | probe4：`OnAutoPlay True` 生效，但进 ADV 后 2 分钟 `playedIndex/printCalls` 不变（`waitingInput=False`）；`SetSkipEnabled(true)` 后 `skip=False` 不推进 | **不可依赖连播**；钩子命中验证要按正常剧情推进或分段 LoadAndPlay |
| 场景常量 | probe4：`printerPanels=2` 恒定存在（`NormalPrinter`/`DebatePrinter` 均 `active=True`）；面板 `GetType()` 在采样间变化（`WitchTrialsTextPrinterPanel`→`RevealableTextPrinterPanel`→`UITextPrinterPanel`） | 「有台词」不能靠面板 active/类型；用 `Print` 事件 + 面板 `msgs`/`text`；处理时 `FindObjectsOfType` 现取不缓存 |

---

## A. 台词打印（标准 ADV，`@print` / 普通对话）

| 用途 | 类型全名 | 方法签名（含参数与返回类型） | 选择理由 | 风险/失败模式 | 证据 |
|---|---|---|---|---|---|
| 拿已渲染的当前台词文本（主钩子） | `Naninovel.UI.RevealableText` | `void set_Text(string value)`（`public virtual new`；native token `set_Text_Public_Virtual_New_set_Void_String_0`） | 文本经本地化 + ruby 处理后逐字写入，是最终显示值；参考作已实测可 Harmony 挂 | ① 打印机复用时也会被调用（场景/读档/预载）→ 必须面板可见 + 新鲜期双闸门；② 文本含 TMP/富文本与 ruby 标记，需清洗；③ native detour 需本作探针确认 | 实查 `sig\RevealableText.txt`；参考 `Plugin.cs`（noexistence） |
| 拿“新行开始/打印完成”权威事件 | `Naninovel.TextPrinterManager` | `UniTask Print(string printerId, PrintedMessage message, bool append = false, float speed = 1f, AsyncToken token = null)`（`public virtual new`；token `Print_Public_Virtual_New_UniTask_String_PrintedMessage_Boolean_Single_AsyncToken_0`） | 标准 @print 的唯一咽喉（`PrintText.Print → PrinterManager.Print`），可在同点拿到 printerId + message | ① async 方法：postfix 在首个 await 就返回，不能当“打印完成”；② struct 参数 `PrintedMessage` 的 Harmony prefix 绑定**已实测可绑定**（probe3/probe4，形参 `(string, PrintedMessage, bool, float)`）；③ 辩论行是否经此路径待正常剧情验证（见 B，probe4 已实测 `DebatePrinterId='Debate'`） | 实查 `sig\TextPrinterManager.txt`；调用链见参考 `Naninovel.Commands\PrintText.cs:260`、`Naninovel\TextPrinterManager.cs:96-113`；probe4 `RE_PRINT` |
| 旧的打印事件（不可用，登记以免踩坑） | `Naninovel.TextPrinterManager` / `ITextPrinterManager` | `event Il2CppSystem.Action<PrintMessageArgs> OnPrintStarted/OnPrintFinished` | 语义上最理想（行起点/行结束） | **无法订阅**：`DelegateSupport.ConvertDelegate` 对 `PrintMessageArgs`（non-blittable struct）直接抛 `ArgumentException`；只能 Harmony patch | 实查字段 + env-probe run3 报错（lead m00258） |
| 说话人/显示名 | `Naninovel.UI.RevealableTextPrinterPanel` | `MessageAuthor Author { get; }`（native `get_Author_Protected_Virtual_New_get_MessageAuthor_0`）、`void SetMessageAuthor(MessageAuthor author)`；`AuthorChangedEvent : UnityEvent<string> onAuthorChanged` | `Author.Id` = 角色 ID（CharacterNames.txt 的 key 口径）；`Label` = 显示名；`onAuthorChanged` 是 `UnityEvent<string>`（blittable，可 `AddListener`） | ① 原语是 protected，interop 包装为 public 可调，但需 TryCast 到本作子类；② **旁白判定用 `PrintedMessage.author` 字段**：probe4 实测旁白时字段对象非 null 但 `Id=null`/`Label` 空（`Author` 的 Nullable getter 才抛 NRE）→ `author==null \|\| string.IsNullOrEmpty(author.Id)`；③ `Label` 可能是模板未加载 | 实查 `sig\RevealableTextPrinterPanel.txt`；probe3/probe4 |
| 显示名兜底 | `Naninovel.ICharacterManager` | `string GetAuthorName(string characterId)` | Naninovel 官方名字解析；中文名表在 `textassets\ui\zh-Hans\CharacterNames.txt` | 未加载/未知角色返回空 | 实查 `sig\ICharacterManager.txt` |
| 正文兜底（面板级） | `Naninovel.UI.UITextPrinterPanel` | `void SetMessages(IReadOnlyList<PrintedMessage>)` / `void AddMessage(PrintedMessage)` / `void AppendText(LocalizableText)` / `UniTask RevealMessages(float delay, AsyncToken token)` / `string FormatMessage(PrintedMessage message)` | 不依赖 RevealableText 的面板入口；`FormatMessage` 可直接得“这一条最终会显示什么” | **probe3 实测基类 `AddMessage` 在 ADV 打印时命中 0 次 → 不作主钩子**；本作 ADV 面板 `WitchTrials.Views.WitchTrialsTextPrinterPanel`（Giga 扩展子类）只覆写 `AuthorNamePanel`/`SetMessageAuthor` | 实查 `sig\UITextPrinterPanel.txt`；probe3 |
| 打印机 actor 级兜底 | `Naninovel.UITextPrinter` | `virtual void AddMessage(PrintedMessage)` / `virtual void AppendText(LocalizableText)` / `virtual UniTask Reveal(float delay, AsyncToken token = null)` | actor 层入口，覆盖所有 printer（含 `WitchTrials.Views.DebateTextPrinter`，它未覆写） | struct 参数已证可绑定（Print 同理）；面板可能再覆写内容 | 实查 `sig\UITextPrinter.txt`、`sig\DebateTextPrinter.txt`；probe3 |
| 等待点击/整行播完 | `Naninovel.IScriptPlayer` | `bool WaitingForInput { get; }`；`event Action<bool> OnWaitingForInput` | `Action<bool>` blittable，可安全订阅；判断“现在按任意键继续”的 A11Y 播报 | 事件需 `ConvertDelegate<Action<bool>>`（安全）但 UI 侧还有输入拦截 | 实查 `sig\IScriptPlayer.txt` |
| 语音在播/自动播放状态 | `Naninovel.IScriptPlayer` | `bool AutoPlayActive { get; }`、`event Action<bool> OnAutoPlay`；`bool SkipActive`、`event Action<bool> OnSkip` | 同上，状态只读 | 跳过时打印链会被加速/截断 | 实查 |

---

## B. 辩论打印（`@printDebate`，2003 处，本作核心）

| 用途 | 类型全名 | 方法签名 | 选择理由 | 风险/失败模式 | 证据 |
|---|---|---|---|---|---|
| 辩论台词文本 | `WitchTrials.Views.DebateTextPrinterPanel`（witchtrials-re） | `SetText(string)` / `AddText(string)` / `AddMessage(PrintedMessage)` / `SetMessages(IReadOnlyList<PrintedMessage>)` / `RevealMessages(float, AsyncToken)` | 辩论面板自身持有最终文本 | 归 witchtrials-re；本清单只登记 Naninovel 侧交界 | 实查（`re\raw\as\DebateTextPrinterPanel.cs`） |
| 辩论命令入口 | `WitchTrials.Views.PrintDebateText`（命令） | `static string DebatePrinterId`；async `Execute`（内部 await `UniTask<ITextPrinterActor>` + `UniTask`）；内嵌 `_Print(printer, token)` | 2003 处对话都走它；可在 prefix 拿参数（`pos/roll/label`）做“行开始”权威 | ① **probe4 实读 `DebatePrinterId='Debate'`**（public static 字段）→ 判定辩论行优先用 `printerId=='Debate'`；`LoadAndPlay(Trial01)` 未能让辩论脚本执行（trial 状态门控），是否经 `TextPrinterManager.Print` 仍待正常剧情验证；② 文本含自定义标记/正则（`s_pattern1/2`、`s_linkIdRegex`），需与 ADV 同一清洗管线 | 实查 `re\raw\as\PrintDebateText.cs`；probe4 `RE_DEBATE_PRINTER_ID` |
| 辩论打印机 actor | `WitchTrials.Views.DebateTextPrinter : Naninovel.UITextPrinter` | `ctor(string id, TextPrinterMetadata meta)`；`DebateTextAnimationKind AnimationKind` | 未覆写 AddMessage/AppendText/Reveal → 基类钩子覆盖它 | 面板层仍可能覆写 | 实查 `re\raw\as\DebateTextPrinter.cs` |
| 判定“是否处于辩论” | `WitchTrials.Models.IWitchTrialsNaninovelManager` | `bool IsInDebate { get; }`、`bool IsPresentEvidenceMode { get; }`、`IObservable<bool> OnDebateSkippableChangedAsObservable()` | 官方状态源，比猜动画/面板更稳 | 无静态单例：实例由 `WitchTrialsNaninovelPresenter`（`WitchTrials.Presenters`，MonoBehaviour）序列化持有 → 需 `FindObjectOfType` 或从持有它的组件取；**待探针** | 实查 `re\raw\as\IWitchTrialsNaninovelManager.cs`、`WitchTrialsNaninovelPresenter.cs` |
| 轻量替代判定 | `WitchTrials.Views.PrintDebateText` | `static string DebatePrinterId` | 打印时 `printerId == DebatePrinterId` 即辩论行（无需 manager 实例） | 需在 Print prefix 拿到 printerId（待探针） | 实查 |

> 结论：**Naninovel 层不提供辩论专用 API**。若探针证明 printDebate 也调用 `TextPrinterManager.Print`，A 组钩子即可统一覆盖；否则辩论文本必须由 witchtrials-re 的面板钩子提供，A 组只负责普通对话/旁白。

---

## C. 选项（choice）

| 用途 | 类型全名 | 方法签名 | 选择理由 | 风险/失败模式 | 证据 |
|---|---|---|---|---|---|
| 拿当前选项列表 | `Naninovel.UIChoiceHandler` | `List<ChoiceState> Choices { get; }`、`ChoiceHandlerPanel HandlerPanel { get; }` | 实现类直接暴露选项与面板；每帧扫一次即可，无需 patch | `UIChoiceHandler` 是 MonoBehaviourActor，需 `FindObjectOfType`；可能多实例（ADV/辩论） | 实查 `sig\UIChoiceHandler.txt` |
| 程序化选择（键盘激活） | `Naninovel.UIChoiceHandler` | `void HandleChoice(string id)`（public virtual，等价点击） | 比模拟点击稳，直接走 Goto/Gosub/AutoPlay 逻辑 | 需保证 id 来自当前 `Choices`；重复调用可能重复推栈 | 实查 |
| 选项按钮内容 | `Naninovel.UI.ChoiceHandlerButton` | `ChoiceState ChoiceState { get; }`；`event Action<string> OnSummaryTextChanged`；`UnityEvent<string> onSummaryTextChanged`；`void Initialize(ChoiceState)`；`void OnButtonClick()` | `OnSummaryTextChanged`（string）可安全订阅；`OnButtonClick()` 可程序化触发 | 本作子类 `AdvChoiceHandlerButton`/`TrialChoiceHandlerButton` 可能覆写 → 优先用基类实例（按钮仍是基类引用） | 实查 `sig\ChoiceHandlerButton.txt` |
| 选项面板 | `Naninovel.UI.ChoiceHandlerPanel` | `RectTransform ButtonsContainer { get; }`（public）；`List<ChoiceHandlerButton> ChoiceButtons { get; }`（**protected**）；`void AddChoiceButton(ChoiceState)`；`void RemoveAllChoiceButtons()`；`event Action<ChoiceState> OnChoice`（**struct payload，不能订阅**） | `ButtonsContainer` 可枚举子按钮；`ChoiceButtons` 虽 protected 但 interop 包装可调 | ① 键盘焦点要自己维护（uGUI `EventSystem`）；② 选项文本在 `ChoiceState.Summary`（LocalizableText，zh-Hans 加载时点待探针） | 实查 `sig\ChoiceHandlerPanel.txt` |
| 选项按钮焦点 | `UnityEngine.UI.Selectable` / `UnityEngine.EventSystems.EventSystem` | `void Selectable.Select()`；`void EventSystem.SetSelectedGameObject(GameObject selected)`（另有 `(GameObject, BaseEventData)` 重载） | 标准 uGUI 键盘焦点，无需碰 Naninovel | 手柄/键盘导航映射由游戏 InputSystem 决定；可能被 `IInputManager.ProcessInput` 拦 | 实查（`UnityEngine.UI.dll` dump 于 `re\raw\naninovel\EventSystem.cs`/`UI.Selectable.cs`） |
| 选项状态结构 | `Naninovel.ChoiceState` | 字段/属性：`string id`、`PlaybackSpot hostedAt`、`bool nested`、`LocalizableText summary`、`bool locked`、`string buttonPath`、`Vector2 buttonPosition`、`bool overwriteButtonPosition`、`string onSelectScript`、`bool autoPlay` | 文本、锁定态、选择后脚本都在这里 | struct；`Summary.ToString()` 依赖本地化加载 | 实查 `sig\ChoiceState.txt` |

---

## D. 剧本播放 / 状态（判断“当前台词/命令/是否等待/自动/跳过”）

| 用途 | 类型全名 | 方法签名 | 选择理由 | 风险/失败模式 | 证据 |
|---|---|---|---|---|---|
| 全部播放状态 | `Naninovel.IScriptPlayer` | 属性：`bool Playing/Completing/SkipActive/AutoPlayActive/WaitingForInput`、`PlayerSkipMode SkipMode`、`Script PlayedScript`、`Command PlayedCommand`、`IReadOnlyCollection<Command> ExecutingCommands`、`PlaybackSpot PlaybackSpot`、`ScriptPlaylist Playlist`、`int PlayedIndex/PlayedCommandsCount`、`Stack<PlaybackSpot> GosubReturnSpots` | 一个服务读全部状态，A11Y 的主状态机 | `GetService` 时机早会 null；`Command` 是基类，取具体命令要 TryCast | 实查 `sig\IScriptPlayer.txt` |
| 播放/停止/重播 | 同上 | `void Play(string scriptPath, int playlistIndex = 0)`、`void Resume(Nullable<int>)`、`void Stop()`、`UniTask<bool> Rewind(int lineIndex)`、`UniTask Complete(Func<UniTask>)` | 重复朗读/回退需要 | `Play` 是“从头”，重播当前行用 `Rewind(PlayedIndex)` 或 UI 层 Backlog | 实查 |
| 状态事件（可安全订阅） | 同上 | `event Action<Script> OnPlay/OnStop`；`event Action<Command> OnCommandExecutionStart/OnCommandExecutionFinish`；`event Action<bool> OnSkip/OnAutoPlay/OnWaitingForInput` | `Action<bool>/<Script>/<Command>` 均 blittable → 可 `ConvertDelegate` | 事件在打印之前/之后触发顺序需探针确认（行开始最适合播报） | 实查；noexistence 用 `Engine.GetService<IInputManager>()` 等同类调用成功 |
| 跳过/自动开关 | 同上 | `SetSkipEnabled(bool)`、`SetAutoPlayEnabled(bool)`、`SetWaitingForInputEnabled(bool)` | A11Y 热键控制 | 跳过会截断语音/文本事件 | 实查 |
| 剧透闸门 | 同上 + `Naninovel.Script` | `PlayedScript.Path`（`Script : ScriptableObject`，属性 `Path/TextMap/Playlist/Lines`）；`PlaybackSpot` 字段 `string scriptPath; int lineIndex; int inlineIndex`，属性 `ScriptPath/LineIndex/LineNumber/InlineIndex/Valid` | 精确到“第几个脚本第几行”，可白名单/剧透抑制 | `ScriptLine` 内容读取需 `Script.Lines`/`TextMap`（本地化键映射） | 实查 `sig\Script.txt`、`sig\PlaybackSpot.txt` |
| 脚本资源 | `Naninovel.IScriptManager` | `IResourceLoader<Script> ScriptLoader`、`IResourceLoader<Script> ExternalScriptLoader`、`int TotalCommandsCount` | 预载/查找外部脚本 | 少见场景用 | 实查 `sig\IScriptManager.txt` |

---

## E. 存档读档 / 标题 / 设置 / 控制栏（键盘可玩）

| 用途 | 类型全名 | 方法签名 | 选择理由 | 风险/失败模式 | 证据 |
|---|---|---|---|---|---|
| 标题菜单 | `Naninovel.UI.TitleMenu : CustomUI` | `override void Awake()`；`override UniTask ChangeVisibility(bool visible, Nullable<float> duration = null, AsyncToken token = null)`；`UniTask PlayTitleScript(AsyncToken token)`；字段 `IScriptPlayer player; IScriptManager scripts; string titleScriptPath` | 面板出现时朗读标题项、建立焦点顺序 | 本作 `WitchTrials.Views.TitleUi` 可能接管（witchtrials-re） | 实查 `sig\TitleMenu.txt` |
| 存档/读档菜单 | `Naninovel.UI.SaveLoadMenu : CustomUI` | `GlobalState LastSaveWasQuick`；`HandleLoadSlotClicked/HandleSaveSlotClicked/HandleDeleteSlotClicked/HandleDeleteQuickLoadSlotClicked`（async void）；`ChangeVisibility(...)` | 菜单可见性/槽位操作播报 | 本作 `SaveLoadUi` 扩展（witchtrials-re）；槽位是 `PageableGridPanel` 自定义 grid | 实查 `sig\SaveLoadMenu.txt` |
| 设置菜单 | `Naninovel.UI.GameSettingsMenu : CustomUI` | `Toggle[] Tabs { get; }`；`List<TMP_Dropdown> dropdowns`；`int tabIndex`；`override UniTask Initialize()`；`UniTask SaveSettingsAndHide()`；`void HandleCancelInput()`；`void HandleTabInput(float value)` | 键盘 tab/开关/取消的标准入口 | Toggle/Dropdown 需 uGUI 键盘支持；本作 `OptionsUi` 可能包一层（witchtrials-re） | 实查 `sig\GameSettingsMenu.txt` |
| 控制栏 | `Naninovel.UI.ControlPanel` | 字段 `GameObject buttons; GameObject legend;`；`void HandleInputModeChanged(InputMode)`；具体按钮类 `ControlPanelAutoPlayButton/…Skip…Settings…Save…QuickSave…Load…QuickLoad…Log…Hide…Title…Tips` | 控制栏按钮全部是 Naninovel 标准控件，可统一挂键盘焦点/朗读 | 按钮在 HUD 上，焦点顺序需自定义 | 实查（类型清单 + `sig\ControlPanel.txt`） |
| 面板可见性事件 | 任意 `Naninovel.UI.IManagedUI` | `bool Visible`；`event Action<bool> OnVisibilityChanged`；`UniTask ChangeVisibility(bool, Nullable<float>, AsyncToken)`；`void SetFont(TMP_FontAsset)`；`void SetFontSize(int)` | `Action<bool>` 可安全订阅 → 面板出现自动播报标题 | 面板实例获取（`IUIManager.GetUI<T>` vs `FindObjectOfType`）待探针 | 实查 `sig\IManagedUI.txt` |
| UI 管理器 | `Naninovel.IUIManager` | `T GetUI<T>() where T:class`；`IManagedUI GetUI(string)`；`bool HasUI<T>()/HasUI(string)`；`UniTask<IManagedUI> AddUI(GameObject, string, string)`；`bool RemoveUI(IManagedUI)`；`void GetManagedUIs(ICollection<IManagedUI>)`；`void SetUIVisibleWithToggle(bool, bool)`；模态 UI 方法族；`TMP_FontAsset GetFontAsset(string)` | 统一 UI 访问/字体放大 | 泛型 `GetUI<T>` 需 T 为非接口?（签名 `where T:class`）；接口 UI 可能拿不到 | 实查 `sig\IUIManager.txt` |

---

## F. 语音（重复朗读 + 重播）

| 用途 | 类型全名 | 方法签名 | 选择理由 | 风险/失败模式 | 证据 |
|---|---|---|---|---|---|
| 播/停语音 | `Naninovel.IAudioManager` | `UniTask PlayVoice(string path, float volume = 1f, string group = null, string authorId = null, AsyncToken token = null)`；`void StopVoice()` | 重播当前行语音 | 需要知道 voice path（来自打印链/label/GetPlayedVoice） | 实查 `sig\IAudioManager.txt` |
| 语音是否在播 | 同上 | `bool IsVoicePlaying(string path)`；`string GetPlayedVoice()`；`IAudioTrack GetVoiceTrack(string path)` | 轮询检测结束 | `GetPlayedVoice()` 形态（空串/null）待探针 | 实查 |
| 语音结束事件 | `Naninovel.IAudioTrack` | `bool Playing`；`event Action OnPlay/OnStop`；`void Play()/Stop()`；`UniTask Fade(float, float, AsyncToken)` | `Action` 无参 → 可安全订阅，得到“语音播完” | track 可能为空（未创建/已释放）；路径变化后需重取 | 实查 `sig\IAudioTrack.txt` |
| 音量/作者音量 | `Naninovel.IAudioManager` | `float GetAuthorVolume(string authorId)`；`void SetAuthorVolume(string, float)`；`VoiceVolume`；`string VoiceLocale` | A11Y 音量调节/角色音量 | 直接改会写入设置存档 | 实查 |
| Giga 扩展 | `GigaCreation.NaninovelExtender.Audio.AudioManagerExtended` | `override UniTask PlayVoice(string path, float volume = 1f, string group = null, string authorId = null, AsyncToken token = null)`；`override void StopVoice()`；`void PlayResponseSfx(ResponseKind)` | 本作实际注入的 audio manager 可能是它（扩展了响应音效） | patch 接口虚方法还是子类方法取决于实际服务类型（待探针） | 实查 `re\raw\giga\AudioManagerExtended.cs` |

---

## G. Toast / 系统横幅（`@toast` 78 处）

| 用途 | 类型全名 | 方法签名 | 选择理由 | 风险/失败模式 | 证据 |
|---|---|---|---|---|---|
| toast 文本播报 | `Naninovel.UI.ToastAppearance` | `void SetText(string text)`；`TextChangedEvent onTextChanged`（`UnityEvent<string>`）；`UnityEvent onSelected/onDeselected`；`void SetSelected(bool)` | `UnityEvent<string>` 可直接 `AddListener`（string blittable） | toast 出现/消失时机、多次 toast 覆盖 | 实查 `sig\ToastAppearance.txt` |
| toast 显示入口 | `Naninovel.UI.IToastUI` | `void Show(LocalizableText text, string appearance = null, Il2CppSystem.Nullable<float> duration = null)` | 可用于自测/主动播报 | 找到实现实例（`IUIManager.GetUI<IToastUI>` 或 FindObjectOfType）待探针 | 实查 `sig\IToastUI.txt` |
| toast 实现 | `Naninovel.UI.ToastUI : CustomUI` | `TrySelectAppearance(string appearance, out ToastAppearance result)`；字段 `defaultAppearance/defaultDuration/appearances/hideTimer` | 定位当前 appearance 实例 | — | 实查 `sig\ToastUI.txt` |

---

## H. 本作特有 / 边界（明确“不存在”的项）

| 用途 | 类型全名 | 方法签名 | 选择理由 | 风险/失败模式 | 证据 |
|---|---|---|---|---|---|
| QTE / 限时按钮 | —— | **Naninovel 层不存在**：`Elringus.Naninovel.Runtime.dll` 无 `QTEUI`/`Qte` 类型（全类型列表已查）；只有 `Naninovel.Timer`、`Commands.Wait` | 避免把参考作 QTE 方案硬套 | 本作限时/倒计时由 `WitchTrials.Models.Timer` + `WitchTrials.Views.SetTrialTimer`/`Timer` 驱动 → witchtrials-re | 实查（`ilspycmd -l c` 全类型） |
| 特殊演出/辩论 UI | `WitchTrials.Views.*`（DebateUi、DebateTextPrinterPanel、TrialChoiceHandlerPanel、WitchBook*…） | 见 `re\witchtrials-ui.md` | 本作 2003 处辩论台词的主战场 | 不在本清单范围；本清单只保证 Naninovel 层钩子可复用 | witchtrials-re 实查 |
| 自定义 printer 扩展 | `GigaCreation.NaninovelExtender.UI.RevealableTextPrinterPanelModified` | `override void SetWaitForInputIndicatorVisible(bool)`；`override void SetFont(TMP_FontAsset)`；`override string GetActiveAppearance()`；`override void SetActiveAppearance(string)`；`void HandleAutoModeChanged(bool)`；`void HandleSkipModeChanged(bool)`；字段 `_naniScriptPlayer` | 本作 ADV 面板的基类（`WitchTrialsTextPrinterPanel` 之上还有游戏子类） | 覆写链：`WitchTrials.* → Giga...Modified → Naninovel.UI.RevealableTextPrinterPanel`，patch 基类可能被子类覆写绕过 | 实查 `sig\RevealableTextPrinterPanelModified.txt` |
| 自定义选项面板 | `GigaCreation.NaninovelExtender.UI.ChoiceHandlerPanelModified` | `override UniTask Initialize()`；`override ChoiceHandlerButton LoadCustomButtonPrefab(string path)`；字段 `_customChoiceButtonData`（`Il2CppReferenceArray<CustomChoiceButtonData>`）、`_customButtons`（`IDictionary<string, ChoiceHandlerButton>`） | 自定义按钮 prefab 映射；`TrialChoiceHandlerPanel` 继承它 | 自定义按钮可能不是 `ChoiceHandlerButton` 子类？实际是（映射 value 类型） | 实查 `sig\ChoiceHandlerPanelModified.txt` |
| 输入检查/交互拦截 | `GigaCreation.NaninovelExtender.Common.NaninovelInputChecker` / `NaninovelInteractionBlocker` / `IClickThroughPanelExtended` | 见 `re\raw\giga\` dump | “点击继续”被游戏额外拦截时会影响 A11Y 模拟点击 | 归 Giga 层，需探针确认拦截时机 | 实查（类型存在） |
| 第三方 UI 依赖 | `MPUIKIT.MPImage`（`BepInEx\interop\MPUIKit.dll`，`class MPImage : Image`）；`LeTai.TrueShadow.TrueShadow`（`: UIBehaviour`） | `MPImage` 仅覆写材质/网格；`TrueShadow` 为视觉阴影 | 明确**不需要**为它们写适配：按钮仍是 uGUI `Button/Selectable`，键盘焦点逻辑不变；无 UI Polyon（interop 无对应 DLL） | 若误按第三方框架设计输入/焦点会走空 | 实查 `re\raw\ui\MPUIKIT.MPImage.cs`、`re\raw\ui\LeTai.TrueShadow.TrueShadow.cs` |

---

## I. 推荐最小可行补丁集（MVS，已按 probe3/probe4 实测收敛）

1. `TextPrinterManager.Print` **prefix**（已实测可用）→ 结构化 `printerId` + `PrintedMessage`（`Text`、`author` 字段、`Text.parts[i].id/.spot`）；它是唯一能同时拿到「说话人 + 纯文本 + 行 id + 播放器（ADV/辩论）」的点。**probe4 已实测 parts 取到 `id='0101Adv05_Narrative001'`、`spot=Act01_Chapter01/Act01_Chapter01_Adv05:11`**。
2. `RevealableText.set_Text` postfix（已实测可用）→ 兜底拿最终渲染串；**必须过滤空串**（实测每句 3 次空串）+ 按实例去重 + 面板可见/新鲜期闸门 + `GetComponentInParent` 定归属（面板实例会被替换，勿缓存）。
3. 订阅 `IScriptPlayer.OnWaitingForInput/OnAutoPlay/OnSkip`（`Action<bool>`，已实测可订阅）→ “按任意键继续 / 自动播放 / 跳过”状态播报。
4. `UIChoiceHandler` 每帧扫描 `Choices` → 选项播报；键盘：`HandleChoice(id)` 激活 + `EventSystem.SetSelectedGameObject` 焦点；`ChoiceHandlerPanel.AddChoiceButton` prefix 只作补充。
5. 订阅 `IManagedUI.OnVisibilityChanged`（`Action<bool>`）+ `ToastAppearance.onTextChanged`（`UnityEvent<string>`）→ 面板/横幅播报。
6. 语音：`IAudioManager.GetPlayedVoice()` + `IAudioTrack.OnStop` → 播完提示/自动重复朗读；`PlayVoice` 重播（`GetVoiceTrack`/`GetPlayedVoice` 形态待 probe5 验证）。
7. 剧透闸门：`IScriptPlayer.PlayedScript.Path` + `PlaybackSpot`（scriptPath/lineIndex/inlineIndex），配合 `Parts[i].Spot` 精确到行；**不要**用打印文本正则反推行 id。
8. 自动化测试/进入正片：`ScriptPlayerExtensions.LoadAndPlay(player, "Act01_Chapter01/Act01_Chapter01_Adv05", 0)`（已实测进 ADV 并触发打印），不要依赖标题按钮（实测只重播 `System/System_Title`）；**不要指望 autoplay 连播**（probe4 实测停在首句不前进）。

（辩论/魔女手册/QTE 等游戏层内容由 witchtrials-re 的面板钩子补上；`@printDebate` 是否经 `TextPrinterManager.Print` 仍待正常剧情推进验证——probe4 已确认辩论 printer id 静态值是 `'Debate'`，见 B 组。）
