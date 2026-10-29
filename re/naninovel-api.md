# Naninovel API 速查 / Harmony 可挂点（本作实查版）

目标游戏：`D:\Steam\steamapps\common\manosaba_game`（Unity 6000.0.48f1，IL2CPP，Naninovel + Addressables，zh-Hans）
读者：写 A11Y 补丁层（Harmony + 运行时读取）的人；本文只讲 **Naninovel 核心 + GigaCreation 扩展层**，`WitchTrials.*` 游戏层见 `re\witchtrials-ui.md`（witchtrials-re）。

> 约定：凡标「已实查」= 来自本作 interop DLL 的 `ilspycmd` 反编译（类型/签名级证据）；凡标「参考实现」= 来自 `D:\DSHWorkSpace\work\decompiled\`（另一款 Mono 构建的 Naninovel 同样版本代源码，有方法体，但 **方法体不是本作证据**）；凡标「待探针验证」= 需要运行时插件确认，不能当结论用。

---

## 0. 证据与环境

| 项 | 路径/值 |
|---|---|
| interop 目录 | `D:\Steam\steamapps\common\manosaba_game\BepInEx\interop\` |
| 核心程序集 | `Elringus.Naninovel.Runtime.dll`（4,736,000 B）、`Naninovel.Common.dll`、`Assembly-CSharp.dll`、`GigaCreation.NaninovelExtender.*.dll`（18 个）、`UnityEngine.UI.dll`、`Unity.TextMeshPro.dll`、`Il2Cppmscorlib.dll` |
| BepInEx 版本 | 6.0.0-be.788 + Il2CppInterop 1.5.3+PR#277（见 `re\env-facts.md`） |
| 反编译命令 | `ilspycmd -t <类型全名> <dll>`（ilspycmd 9.1.0.7988，需 .NET 8） |
| 每类型原始 dump | `re\raw\naninovel\<Leaf>.cs`（Naninovel）、`re\raw\giga\`（GigaCreation）、`re\raw\as\`（Assembly-CSharp，仅做边界参考） |
| 签名摘录（交付物） | `re\decompiled\signatures\<Leaf>.txt`，索引 `index.txt`（`name | base=… | fields=… | methods=…`） |
| 全量反编译 | `re\decompiled\<Asm>\<Asm>.decompiled.cs` |
| 参考同代源码（有方法体） | `D:\DSHWorkSpace\work\decompiled\NaninovelRuntime\…`、`…\Naninovel.Commands\…`（SisterOtherParanoia 构建；因其同样存在 `PrintMessageArgs`，属于同一代 Naninovel API，可作行为参考） |

关键事实：interop DLL 的方法体全部是 `IL2CPP.il2cpp_runtime_invoke(...)` 桩，**没有游戏逻辑**；所以「谁调用了谁」只能靠：① 异步状态机字段/awaiter 类型（签名级）② 参考同代源码（行为级，需标注）③ 运行时探针（最终证据）。

---

## 1. 服务获取（一切读取的入口）

`Naninovel.Engine`（`public static class`，`Elringus.Naninovel.Runtime.dll`，已实查 `re\raw\naninovel\Engine.cs`）：

```csharp
public static class Engine {
    public static TService GetService<TService>();
    public static bool TryGetService<TService>(out TService service);
    public static TService GetServiceOrErr<TService>();
    public static TService FindService<TService>(Predicate<TService> filter);
    public static void FindAllServices<TService>(ICollection<TService> result);
    public static T FindService<TService, TState>(TState state, Func<TService, TState, bool> filter);
    public static TConfiguration GetConfiguration<TConfiguration>();
}
```

用法（IL2CPP 下可用，泛型调用走 `MethodInfoStoreGeneric_*`）：

```csharp
var printerMgr = Engine.GetService<ITextPrinterManager>();     // 打印管理器
var player     = Engine.GetService<IScriptPlayer>();           // 剧本播放器
var uiMgr      = Engine.GetService<IUIManager>();              // UI 管理器
var audioMgr   = Engine.GetService<IAudioManager>();           // 音频
var charMgr    = Engine.GetService<ICharacterManager>();       // 角色/显示名
var inputMgr   = Engine.GetService<IInputManager>();           // 输入
```

- **时机**：EN engine 在 Naninovel 初始化后才可 `GetService`；BepInEx 插件 `Awake()` 太早。本作由 `GigaCreation.NaninovelExtender.Core.NaninovelManager.InitializeAsync()` 驱动（`re\raw\giga\NaninovelManager.cs`），建议插件里延迟到首次 `Engine.TryGetService<IScriptPlayer>(out var p)` 成功再挂（或 patch 初始化流程后的第一个 `OnPlay`）。
- 失败模式：早期 `GetServiceOrErr` 抛异常；用 `TryGetService` 轮询到成功再启用 A11Y。
- 已有证据：参考作 noexistence 在运行时用同样调用成功（`D:\DSHWorkSpace\noexistence_a11y\mod\src\NoExistenceA11y\Plugin.cs`），但本作需探针确认。

---

## 2. 台词打印链（核心）

### 2.1 类型与签名（全部已实查，证据 = `re\raw\naninovel\*.cs` + `re\decompiled\signatures\*.txt`）

```csharp
public class ITextPrinterManager : Il2CppObjectBase {          // 接口（interop 渲染为 class）
    string DefaultPrinterId { get; }
    float  BaseRevealSpeed { get; }
    float  BaseAutoDelay   { get; }
    void add_OnPrintStarted(Il2CppSystem.Action<PrintMessageArgs> value);   // 事件
    void remove_OnPrintStarted(Il2CppSystem.Action<PrintMessageArgs> value);
    void add_OnPrintFinished(Il2CppSystem.Action<PrintMessageArgs> value);
    UniTask Print(string printerId, PrintedMessage message, bool append = false, float speed = 1f, AsyncToken token = null);
}

public class TextPrinterManager : OrthoActorManager<ITextPrinterActor, TextPrinterState, TextPrinterMetadata, TextPrintersConfiguration> {
    Il2CppSystem.Action<PrintMessageArgs> OnPrintStarted;   // 字段（事件）
    Il2CppSystem.Action<PrintMessageArgs> OnPrintFinished;
    public unsafe virtual UniTask Print(string printerId, PrintedMessage message, bool append = false, float speed = 1f, AsyncToken token = null);
    // native token: NativeMethodInfoPtr_Print_Public_Virtual_New_UniTask_String_PrintedMessage_Boolean_Single_AsyncToken_0
}

public sealed class PlaybackSpot : Il2CppSystem.ValueType { /* 见 §5 */ }

public sealed class PrintMessageArgs : Il2CppSystem.ValueType {   // 注意：结构体，含引用字段 → non-blittable
    ITextPrinterActor Printer; PrintedMessage Message; bool Append; float Speed;
}

public sealed class PrintedMessage : Il2CppSystem.ValueType {
    LocalizableText text; MessageAuthor author;          // 字段
    LocalizableText Text { get; }                        // 只读属性
    Il2CppSystem.Nullable<MessageAuthor> Author { get; }
}

public sealed class MessageAuthor : Il2CppSystem.ValueType {
    string Id; LocalizableText Label:
    // 字段 id/label + 属性 Id/Label；ctor (string id, LocalizableText label = null)
}

public sealed class LocalizableText : Il2CppSystem.ValueType {
    Il2CppReferenceArray<LocalizableTextPart> parts; IReadOnlyList<LocalizableTextPart> Parts; bool IsEmpty;
    static LocalizableText Empty;
    static implicit operator LocalizableText(string plainText);   // op_Implicit_Public_Static_LocalizableText_String_0
    static implicit operator string(LocalizableText text);        // op_Implicit_Public_Static_String_LocalizableText_0
    static LocalizableText FromPlainText(string);
    UniTask Load(object holder = null);
    void Hold(object); void Release(object);
    override string ToString();
}

public class ITextPrinterActor : Il2CppObjectBase {
    IReadOnlyList<PrintedMessage> Messages { get; }
    IReadOnlyList<MessageTemplate> Templates { get; }
    float RevealProgress { get; }
    void AddMessage(PrintedMessage);
    void AppendText(LocalizableText);
    UniTask Reveal(float delay, AsyncToken token = null);
}
```

面板/控件：

```csharp
public class UITextPrinterPanel : CustomUI {                  // 已实查 re\raw\naninovel\UITextPrinterPanel.cs
    string text; string authorLabel; RectTransform Content;
    float RevealProgress; string Appearance; Color TintColor;
    IReadOnlyCollection<GameObject> ContinueInputTriggers;
    List<PrintedMessage> Messages; List<MessageTemplate> Templates;
    public virtual void SetMessages(IReadOnlyList<PrintedMessage>);
    public virtual void AddMessage(PrintedMessage);
    public virtual void AppendText(LocalizableText);
    public virtual UniTask RevealMessages(float delay, AsyncToken token);
    public virtual void SetWaitForInputIndicatorVisible(bool);
    public string FormatMessage(PrintedMessage message);
}

public class RevealableTextPrinterPanel : UITextPrinterPanel {
    // native token: NativeMethodInfoPtr_get_Author_Protected_Virtual_New_get_MessageAuthor_0
    public unsafe virtual MessageAuthor Author;            // protected 原语，interop 包装为 public 可调用
    public unsafe virtual CharacterMetadata AuthorMeta;
    public unsafe virtual void SetMessageAuthor(MessageAuthor author);
    public virtual IRevealableText RevealableText { get; }
    public AuthorChangedEvent onAuthorChanged;             // 嵌套 class AuthorChangedEvent : UnityEvent<string>
    // 覆写：Initialize / SetMessages / AddMessage / AppendText / RevealMessages / WaitForContinueRevealConfirmation
}

public class UITextPrinter : MonoBehaviourActor<TextPrinterMetadata> {
    IReadOnlyList<PrintedMessage> Messages; IReadOnlyList<MessageTemplate> Templates;
    float RevealProgress; UITextPrinterPanel PrinterPanel;
    public virtual void AddMessage(PrintedMessage); public virtual void AppendText(LocalizableText);
    public virtual UniTask Reveal(float delay, AsyncToken token = null);
    public virtual void SetMessages(IReadOnlyList<PrintedMessage>); public virtual void SetTemplates(...);
}

public class RevealableText : NaninovelTMProText {
    // native token: NativeMethodInfoPtr_set_Text_Public_Virtual_New_set_Void_String_0
    public unsafe virtual string Text { get; set; }     // set_Text(string) —— 参考作已验证可挂
    float RevealProgress; IRevealInfo RevealInfo; RevealState RevealState;
    int LastCharIndex; int LastRevealedCharIndex; bool RevealRubyInstantly;
    bool RevealNextChars(int count, float duration, AsyncToken token); char GetLastRevealedChar();
}
```

### 2.2 标准 @print 路径（`PrintText` 命令）

参考同代实现（**行为参考**，`D:\DSHWorkSpace\work\decompiled\NaninovelRuntime`）：

- `Naninovel.Commands.PrinterCommand.GetOrAddPrinter()`：`await PrinterManager.GetOrAddActor(AssignedPrinterId ?? DefaultPrinterId)`（`Naninovel.Commands\PrinterCommand.cs`）
- `Naninovel.Commands.PrintText.Execute`（`Naninovel.Commands\PrintText.cs:95-98`）：`printer = await GetOrAddPrinter(token); await WaitOrForget(token => Print(printer, token), Wait, token);`
- `PrintText.Print(printer, token)`（同文件 :101-144）：`ResetText → ShowPrinter → SetDefaultPrinter → PlayAutoVoice → LoadDynamicTextResources → await Print(printedText, printer, token) → WaitEndOfFrame → WaitInput(printedText) → WaitAutoVoice`
- `PrintText.Print(PrintedMessage, ITextPrinterActor, token)`（同文件 :260）：**`return base.PrinterManager.Print(printer.Id, message, Append, AssignedRevealSpeed, token);`**
- `TextPrinterManager.Print`（`Naninovel\TextPrinterManager.cs:96-113`）：
  1. `printer = await GetOrAddActor(printerId)`
  2. `OnPrintStarted?.Invoke(new PrintMessageArgs(printer, message, append, speed))`
  3. `append ? printer.AppendText(message.Text) : printer.AddMessage(message)`
  4. `delay = RevealInstantly||SkipActive ? 0 : Lerp(MaxRevealDelay,0,BaseRevealSpeed*speed)`
  5. `await printer.Reveal(delay, token)`
  6. `OnPrintFinished?.Invoke(...)`

因此：**`TextPrinterManager.Print` 是标准 @print 的单一咽喉**，同时 `OnPrintStarted/OnPrintFinished` 在它里面同点触发。

### 2.3 本作特有：`@printDebate`（2003 处）与辩论打印器

- 命令类型：`WitchTrials.Views.PrintDebateText`（Assembly-CSharp，已实查 `re\raw\as\PrintDebateText.cs`，63,616 B）
  - `public static string DebatePrinterId`、`public static string DebateWaitTimeVarName`、`public static string EnableDebugSkipVarName`
  - 静态正则：`s_pattern1`、`s_pattern2`、`s_linkIdRegex`（文本预处理/掘り）
  - `Execute` 的异步状态机持有 `UniTask<ITextPrinterActor>.Awaiter` + `UniTask.Awaiter`（即“先取 printer actor，再 await 一次打印/Reveal”）
  - 内嵌 `_Print_d__` 状态机字段：`<>4__this`、`printer`（`ITextPrinterActor`）、`token`、`UniTask.Awaiter`
  - **签名级能证明它自己取 printer 并 await，但不能证明它是否走 `TextPrinterManager.Print`** → 必须探针
- 辩论 actor：`WitchTrials.Views.DebateTextPrinter : Naninovel.UITextPrinter`（已实查，4,564 B）
  - `ctor(string id, TextPrinterMetadata meta)`、`AnimationKind` 属性；**没有覆写 `AddMessage/AppendText/Reveal`** → 走基类 `UITextPrinter.AddMessage → PrinterPanel.AddMessage`
- 辩论面板：`WitchTrials.Views.DebateTextPrinterPanel : UITextPrinterPanel`（witchtrials-re 范围，已实查存在 `SetText(string)/AddText(string)/AddMessage/SetMessages/RevealMessages`）

结论（写给补丁层）：
1. 标准台词：patch `TextPrinterManager.Print`（prefix，拿 printerId）+ `RevealableText.set_Text`（postfix，拿已本地化文本）。
2. 辩论台词：**不要只依赖 `TextPrinterManager.Print`**。若探针确认 printDebate 也走 `Print` 则可复用；否则用 `UITextPrinter.AddMessage`/`UITextPrinterPanel.AddMessage` 或辩论面板自身方法（witchtrials-re）。
3. 完全路径无关的兜底内容钩子：`RevealableText.set_Text`（ADV 面板）；辩论面板若不走 RevealableText，则必须有面板级钩子。

### 2.4 稳定获取「当前台词 + 说话人 + 是否旁白」

推荐 **方案 A（推荐）**：

```
patch: Naninovel.UI.RevealableText.set_Text(string)   // postfix
  → 文本 = value（已本地化、含 TMP/富文本与 ruby 标记，需清洗）
  → 面板 = __instance.GetComponentInParent<RevealableTextPrinterPanel>()（本作 ADV 面板是 GigaCreation 扩展子类，可再 TryCast）
  → 说话人ID   = panel.Author.Id            （MessageAuthor.Id，空/无 → 旁白）
  → 说话人显示名 = panel.Author.Label.ToString()  或 Engine.GetService<ICharacterManager>().GetAuthorName(id)
  → 可见性闸门 = panel.Visible && __instance.isActiveAndEnabled && 处于打印新鲜期
```

- 用 `panel.Author` 的 **protected getter**：interop 已把它包装成 `public unsafe virtual MessageAuthor Author`，可直接调用（IL2CPP il2cpp_runtime_invoke 不做访问检查）。这是本作实查的签名，**不是猜的**。
- `panel.onAuthorChanged`（`UnityEvent<string>`）可 `AddListener(Action<string>)` —— `string` 是 blittable，`DelegateSupport.ConvertDelegate` 可用（对比打印事件的失败，见 §9）。
- 旁白判定：`Author.Id` 为空/null 即旁白；也可用 `ICharacterManager.GetAuthorName(id)` 返回空判断。**待探针确认**具体空值形态（null vs ""）。

**方案 B**：patch `TextPrinterManager.Print` 的 prefix，拿 `(printerId, message, append, speed)` 作为“新行起点/权威事件”，再与 `set_Text` 的内容配对（noexistence 的 Pending 队列思路）。风险：`message` 是 struct 参数，Harmony 对 struct 参数的支持在本作 **待探针验证**；且 `Print` 是 async，postfix 会在首个 await 处就返回（**postfix 早于 `OnPrintFinished` 与 Reveal 完成，不能当“打印完成”用**）。

**双闸门（强推，参考 noexistence 血泪）**：`set_Text` 在打印机复用时也会被调用（场景加载、读档、预载、结局面板隐藏后仍写文本）。必须同时满足：
- ① 5 秒内存在一次 `Print`/打印起点（若 B 可用）或 `panel.Visible==true`；
- ② 面板自身 CanvasGroup 可见（遍历 `panel.transform` 上 CanvasGroup alpha/`panel.gameObject.activeInHierarchy`）；
- ③ 文本指纹去重 + 同一面板实例 ID 记 lastText，只播增量。

### 2.5 `LocalizableText` 转字符串

- `LocalizableText.ToString()`（参考同代：`return string.Join(" ", Parts);`）→ 依赖 `Parts` 已被本地化加载。
- `LocalizableText.implicit operator string` 已实查存在（`op_Implicit_Public_Static_String_LocalizableText_0`），C# 里 `(string)l10n` 可用 → 但**是否等价于“已本地化 zh-Hans”取决于是否已 `Load`**：打印流程中 `PrintText` 在打印前做本地化加载（参考 `PrintText.cs:129 `LoadDynamicTextResources`），因此 `set_Text` 时刻文本已是最终本地化文本；而直接读 `ChoiceState.Summary` / `MessageAuthor.Label` 时需确认已加载（**待探针**）。
- 打印时抓到的“原始值”最好用 `RevealableText.Text`（TMP 已合成，含 ruby 标记）再做清洗；`PrintedMessage.Text` 可能仍是未渲染的 LocalizableText。

---

## 3. 选项（choice）

### 3.1 链路与签名（已实查）

- 命令：`Naninovel.Commands.AddChoice : Command`（对应 `@choice`，本作 1184 处）
  - 参数：`LocalizableTextParameter ChoiceSummary`、`BooleanParameter Lock`、`StringParameter ButtonPath`、`DecimalListParameter ButtonPosition`、`StringParameter HandlerId`、`NamedStringParameter GotoPath/GosubPath`、`StringParameter SetExpression`、`BooleanParameter AutoPlay`
  - `Execute` 异步状态机持 `UniTask<IDisposable>.Awaiter` + `UniTask<IChoiceHandlerActor>.Awaiter`；`GetOrAddHandler() → IChoiceHandlerActor`
- `IChoiceHandlerManager`（只有 3 个成员，**AddChoice 不在 manager 上**）：
  ```csharp
  IResourceLoader<GameObject> ChoiceButtonLoader; 
  void PushPickedChoice(PlaybackSpot hostedAt, PlaybackSpot continueAt);
  PlaybackSpot PopPickedChoice(PlaybackSpot hostedAt);
  ```
- `IChoiceHandlerActor`：
  ```csharp
  IReadOnlyList<ChoiceState> Choices { get; }
  void AddChoice(ChoiceState choice); void RemoveChoice(string id); void HandleChoice(string id);
  ```
- 实现 actor：`Naninovel.UIChoiceHandler : MonoBehaviourActor<ChoiceHandlerMetadata>`（已实查）
  ```csharp
  public unsafe virtual ChoiceHandlerPanel HandlerPanel { get; }       // ← 拿到面板
  public unsafe virtual List<ChoiceState> Choices { get; }
  public unsafe virtual void AddChoice(ChoiceState choice);
  public unsafe virtual void RemoveChoice(string id);
  public unsafe virtual void HandleChoice(string id);                  // ← 程序化选择！
  public unsafe virtual void HandleChoice(ChoiceState choice);
  public unsafe virtual void AddChoiceToBacklog(ChoiceState state);
  public unsafe virtual void ClearChoices();
  ```
- 面板：`Naninovel.UI.ChoiceHandlerPanel : CustomUI`（已实查）
  ```csharp
  public unsafe Il2CppSystem.Action<ChoiceState> OnChoice;   // 事件（add_OnChoice/remove_OnChoice，payload=struct → 不能 ConvertDelegate 订阅）
  protected virtual List<ChoiceHandlerButton> ChoiceButtons { get; }   // native: get_ChoiceButtons_Protected_Virtual_New_...
  public unsafe virtual RectTransform ButtonsContainer { get; }        // public
  public unsafe virtual ChoiceHandlerButton DefaultButtonPrefab { get; }
  protected virtual bool FocusChoiceButtons { get; }
  public unsafe virtual void AddChoiceButton(ChoiceState choice);
  public unsafe virtual void RemoveChoiceButton(string id);
  public unsafe virtual void RemoveAllChoiceButtons();
  public unsafe virtual void RemoveAllChoiceButtonsDelayed();
  public unsafe virtual ChoiceHandlerButton LoadCustomButtonPrefab(string path);
  public unsafe virtual void FocusAddedButton(ChoiceHandlerButton button);
  public unsafe virtual bool ShouldFocusAddedButton(ChoiceHandlerButton button);
  public List<ChoiceState> Buttons;            // GameState 字段
  ```
- 按钮：`Naninovel.UI.ChoiceHandlerButton : ScriptableButton`（`ScriptableButton : ScriptableUIControl<Button>`）
  ```csharp
  public unsafe ChoiceState ChoiceState { get; }                       // get_ChoiceState_Public_get_...
  public unsafe Il2CppSystem.Action<string> OnSummaryTextChanged;      // 事件，string → 可订阅！
  public unsafe Il2CppSystem.Action<bool> OnLock;                      // 事件，bool → 可订阅
  public SummaryTextChangedEvent onSummaryTextChanged;                 // UnityEvent<string>
  public OnLockEvent onLock;                                           // UnityEvent<bool>
  public unsafe virtual void Initialize(ChoiceState choiceState);
  public unsafe virtual void OnButtonClick();                          // ← 等价点击，可直接调用
  public unsafe void HandleLockChanged(bool locked);
  ```
- `ChoiceState`（struct，已实查）：`string id`、`PlaybackSpot hostedAt`、`bool nested`、`LocalizableText summary`、`bool locked`、`string buttonPath`、`Vector2 buttonPosition`、`bool overwriteButtonPosition`、`string onSelectScript`、`bool autoPlay` + 同名属性；`Id/HostedAt/Nested/Summary/Locked/ButtonPath/ButtonPosition/OverwriteButtonPosition/OnSelectScript/AutoPlay`

### 3.2 键盘可玩方案（推荐，均无需 struct 参数 patch）

1. 每帧/事件扫描：`var handler = UnityEngine.Object.FindObjectOfType<Naninovel.UIChoiceHandler>();`
   - `handler == null || !handler.Visible || handler.Choices.Count == 0` → 无选项
   - 选项文本：`handler.Choices[i].Summary.ToString()`（`LocalizableText.ToString()`；若为空则取按钮 `onSummaryTextChanged` 值）
2. 选中/激活：`handler.HandleChoice(handler.Choices[i].Id)` —— **public virtual，等价点击**；或调用对应 `ChoiceHandlerButton.OnButtonClick()`。
3. 焦点高亮：`EventSystem.current.SetSelectedGameObject(button.gameObject)`（`UnityEngine.UI.dll`：`public unsafe void SetSelectedGameObject(GameObject selected, BaseEventData pointer)` / `(GameObject)` 两个重载；`Selectable.Select()` 亦存在）。
4. 内容变更订阅（可选的省 CPU 方案）：每个按钮 `AddListener` 到 `OnSummaryTextChanged`（`Action<string>`，blittable 安全）。
5. 风险：`OnChoice` / `OnPrintStarted` 这类 **struct payload 事件不能** 用 `DelegateSupport.ConvertDelegate`（§9 已实测报错）；不要设计成依赖它们。

---

## 4. 剧本播放（IScriptPlayer）

已实查（`re\raw\naninovel\ScriptPlayer.cs` / `sig`）：

```csharp
public class IScriptPlayer : Il2CppObjectBase {
    bool Playing, Completing, SkipActive, AutoPlayActive, WaitingForInput;   // ← 关键状态
    PlayerSkipMode SkipMode;
    Script PlayedScript; Command PlayedCommand;
    IReadOnlyCollection<Command> ExecutingCommands;
    PlaybackSpot PlaybackSpot;
    ScriptPlaylist Playlist; int PlayedIndex; int PlayedCommandsCount;
    Stack<PlaybackSpot> GosubReturnSpots;
    event Action<Script> OnPlay, OnStop;
    event Action<Command> OnCommandExecutionStart, OnCommandExecutionFinish;
    event Action<bool> OnSkip, OnAutoPlay, OnWaitingForInput;   // ← blittable，可安全订阅
    void Play(string scriptPath, int playlistIndex = 0);
    void Resume(Il2CppSystem.Nullable<int> playlistIndex = null);
    void Stop();
    UniTask<bool> Rewind(int lineIndex);
    bool HasPlayed(string scriptPath, Il2CppSystem.Nullable<int> playlistIndex = null);
    void SetSkipEnabled(bool); void SetAutoPlayEnabled(bool); void SetWaitingForInputEnabled(bool);
    void AddPreExecutionTask(Func<Command, UniTask>); void RemovePreExecutionTask(Func<Command, UniTask>);
    void AddPostExecutionTask(Func<Command, UniTask>); void RemovePostExecutionTask(Func<Command, UniTask>);
    UniTask Complete(Func<UniTask> onComplete = null);
}
```

- 「是否等待玩家点击/继续」= `WaitingForInput`（或订阅 `OnWaitingForInput`，`Action<bool>` 安全）；配合 `ITextPrinterActor.RevealProgress==1` 判断整行已显示。
- 「自动播放中」= `AutoPlayActive`（订阅 `OnAutoPlay`）；「跳过中」= `SkipActive`（订阅 `OnSkip`）。
- 当前脚本 = `PlayedScript.Path`（`Script : ScriptableObject`：`string path`、`ScriptTextMap textMap`、`ScriptPlaylist playlist`、`Il2CppReferenceArray<ScriptLine> lines` + 属性 `Path/TextMap/Playlist/Lines`；静态 `FromText/FromTransient/Create`）。
- 当前行 = `PlaybackSpot`（struct）：`static PlaybackSpot Invalid`、字段 `string scriptPath; int lineIndex; int inlineIndex;`、属性 `ScriptPath/LineIndex/LineNumber/InlineIndex/Valid` + `ToString()`。
- 剧透闸门：`PlayedScript.Path`（脚本名/路径）+ `PlaybackSpot.LineIndex`/`LineNumber`；两者都是安全可读值（`PlayedScript` 为引用类型，`PlaybackSpot` 为返回值 struct —— 读属性后取字段即可，不做委托）。

---

## 5. 语音

已实查 `Naninovel.IAudioManager`：

```csharp
UniTask PlayVoice(string path, float volume = 1f, string group = null, string authorId = null, AsyncToken token = null);
void StopVoice();
bool IsVoicePlaying(string path);
string GetPlayedVoice();
IAudioTrack GetVoiceTrack(string path);
IAudioTrack GetAudioTrack(string path);
bool IsBgmPlaying(string path); bool IsSfxPlaying(string path);
void GetPlayedBgm(ICollection<string>); void GetPlayedSfx(ICollection<string>);
float GetAuthorVolume(string authorId); void SetAuthorVolume(string, float);
float MasterVolume/BgmVolume/SfxVolume/VoiceVolume; string VoiceLocale;
```

`IAudioTrack`（已实查）：

```csharp
bool Playing, Loop, Mute; float Volume;
event Action OnPlay, OnStop;      // ← Action（无参）blittable，安全订阅
void Play(); void Stop();
UniTask Fade(float volume, float fadeTime, AsyncToken token = null);
```

- **没有“语音播放完成”事件在 IAudioManager 上**；两条路：
  - A：`var t = audioMgr.GetVoiceTrack(path); if (t != null) t.add_OnStop(...)` —— `Action` 无参，`ConvertDelegate` 安全（**待探针**：voice track 实际是否非空、path 形态）
  - B：轮询 `IsVoicePlaying(path)` / `GetPlayedVoice()`（简单但需知道 path；`GetPlayedVoice()` 返回正在播的 voice 路径，null/空 = 无语音）
- 重复朗读/重播：拿到本行 voice path（来源：打印链里的 `TextId`/label 或 `IAudioManager.GetPlayedVoice()`），`audioMgr.PlayVoice(path, 1f, null, authorId)`。
- GigaCreation 扩展：`GigaCreation.NaninovelExtender.Audio.AudioManagerExtended` 覆写 `PlayVoice`/`StopVoice` 并加 `PlayResponseSfx(ResponseKind)`；`IAudioManagerExtended` 是接口（本作已实查 `re\raw\giga\AudioManagerExtended.cs`）。补丁建议 patch 接口实现或 `AudioManagerExtended.PlayVoice` prefix 记录“当前行语音已请求”。

---

## 6. 标题 / 存档读档 / 设置 / 控制栏 / Toast

- `Naninovel.UI.TitleMenu : CustomUI`（已实查 `re\raw\naninovel\TitleMenu.cs`，28,305 B）：
  ```csharp
  IScriptPlayer player; IScriptManager scripts; string titleScriptPath;
  override void Awake(); override UniTask ChangeVisibility(bool visible, Nullable<float> duration = null, AsyncToken token = null);
  UniTask PlayTitleScript(AsyncToken token);
  ```
- `Naninovel.UI.SaveLoadMenu : CustomUI`（129,838 B）：含 `GlobalState LastSaveWasQuick`、`HandleLoadSlotClicked/HandleSaveSlotClicked/HandleDeleteSlotClicked` 等（async void 状态机签名）；`SaveLoadUi`/`TitleUi`/`OptionsUi` 游戏层由 witchtrials-re 负责。
- `Naninovel.UI.GameSettingsMenu : CustomUI`（已实查）：`Toggle[] tabs; List<TMP_Dropdown> dropdowns; int tabIndex; IStateManager state; ITextManager docs; override UniTask Initialize(); UniTask SaveSettingsAndHide(); void HandleCancelInput(); void HandleTabInput(float value);`
- `Naninovel.UI.ControlPanel`：`GameObject buttons; GameObject legend;` + 每个按钮类（`ControlPanelAutoPlayButton`、`ControlPanelSkipButton`、`ControlPanelSettingsButton`、`ControlPanelSaveButton`、`ControlPanelQuickSaveButton`、`ControlPanelLoadButton`、`ControlPanelQuickLoadButton`、`ControlPanelLogButton`、`ControlPanelHideButton`、`ControlPanelTitleButton`、`ControlPanelTipsButton`）——键盘可玩需要给这些按钮建立“焦点顺序 + 朗读 label”。
- `IManagedUI`（UI 公共接口，已实查）：`bool Visible; Camera RenderCamera; string ModalGroup; int SortingOrder; event Action<bool> OnVisibilityChanged; UniTask Initialize(); UniTask ChangeVisibility(bool, Nullable<float>, AsyncToken); void SetFont(TMP_FontAsset); void SetFontSize(int);` → `OnVisibilityChanged` 是 `Action<bool>`，可安全订阅，用来做“面板出现时优先播报面板标题/首个可交互项”。
- `IUIManager`：`T GetUI<T>() where T:class; IManagedUI GetUI(string); bool HasUI<T>(); bool HasUI(string); UniTask<IManagedUI> AddUI(GameObject, string name=null, string group=null); bool RemoveUI(IManagedUI); void GetManagedUIs(ICollection<IManagedUI>); void SetUIVisibleWithToggle(bool, bool allowToggle=true); ...; TMP_FontAsset GetFontAsset(string);`
- Toast（`@toast` 78 处 = Naninovel 内置）：
  ```csharp
  public class IToastUI : Il2CppObjectBase { void Show(LocalizableText text, string appearance = null, Il2CppSystem.Nullable<float> duration = null); }
  public class ToastUI : CustomUI { string defaultAppearance; float? defaultDuration; ...; bool TrySelectAppearance(string appearance, out ToastAppearance result); }
  public class ToastAppearance : MonoBehaviour { TextChangedEvent onTextChanged; UnityEvent onSelected, onDeselected; void SetText(string text); void SetSelected(bool selected); }
  public class TextChangedEvent : UnityEvent<string>;
  ```
  → 订阅 `ToastAppearance.onTextChanged`（`UnityEvent<string>`，blittable 安全）即可朗读 toast；或 patch `SetText(string)`。

### 6.5 第三方 UI 依赖（本作 interop 实查，影响补丁策略）

| 库 | 实查类型 | 结论 |
|---|---|---|
| MPUIKit | `MPUIKIT.MPImage : UnityEngine.UI.Image`（`BepInEx\interop\MPUIKit.dll`，`ilspycmd -t MPUIKIT.MPImage`） | 自定义按钮/面板图形只是 `Image` 子类（替换圆角/多边形材质），`Button`/`Selectable`/`EventSystem` 的键盘焦点与点击逻辑完全不受影响；A11Y 不需要、也不应去找 MPUIKit 的事件 |
| LeTai.TrueShadow | `LeTai.TrueShadow.TrueShadow : UnityEngine.UIBehaviour`（`LeTai.TrueShadow.dll`） | 纯视觉阴影（mesh effect），无交互、无文本；补丁忽略 |
| UI Polyon / 其它 | interop 目录无对应 DLL | 本作**不使用** UI Polyon |

→ 键盘可玩与朗读只需处理 Naninovel UI（`CustomUI`/`ScriptableButton`）+ uGUI `Button`/`Selectable`/`EventManager` + `TMP_Text`，无需第三方 UI 框架适配。证据：`D:\DSHWorkSpace\manosaba_a11y\re\raw\ui\MPUIKIT.MPImage.cs`、`...\LeTai.TrueShadow.TrueShadow.cs`。

---

## 7. Harmony 可挂点（按可靠性排序，回答 lead 的 5 问）

| # | 挂点 | 类型全名 | 方法签名 | 证据 | 结论/备注 |
|---|---|---|---|---|---|
| 1 | 台词文本（最可靠） | `Naninovel.UI.RevealableText` | `void set_Text(string value)`（`Public_Virtual_New`） | 已实查 `sig\RevealableText.txt`；参考作实测可挂 | postfix 拿 `__instance.Text`；`value` 为已合成文本；需面板可见 + 新鲜期闸门 |
| 2 | 打印起点/权威行事件 | `Naninovel.TextPrinterManager` | `UniTask Print(string printerId, PrintedMessage message, bool append = false, float speed = 1f, AsyncToken token = null)` | 已实查（token `Print_Public_Virtual_New_UniTask_String_PrintedMessage_Boolean_Single_AsyncToken_0`） | **prefix** 拿 printerId/message；postfix 在首个 await 就返回（不可当打印完成）；**probe3 实测 prefix 可挂且 struct 参数可绑定**（`RE_PRINT #1 printerId='Normal' append=False speed=1 text=[...]`） |
| 3 | 标准打印的旧式事件 | `Naninovel.TextPrinterManager` | `event Action<PrintMessageArgs> OnPrintStarted / OnPrintFinished` | 已实查字段 + 参考同代 body 在 Print 内触发 | **不能用 `DelegateSupport.ConvertDelegate` 订阅**（env-probe run3 实测 `ArgumentException: Delegate has parameter of type Naninovel.PrintMessageArgs (non-blittable struct)...`）→ 只能 Harmony |
| 4 | 打印机 actor 内容 | `Naninovel.UITextPrinter` | `virtual void AddMessage(PrintedMessage)` / `virtual void AppendText(LocalizableText)` / `virtual UniTask Reveal(float, AsyncToken)` | 已实查 | 覆盖主/辩论 printer actor（`DebateTextPrinter : UITextPrinter` 未覆写）；probe3 实测 `UITextPrinterPanel.AddMessage`（面板层）命中 0 次 → 别在面板基类 AddMessage 上挂主钩子 |
| 5 | 面板内容（ADV 子类会覆写） | `Naninovel.UI.UITextPrinterPanel` | `virtual void SetMessages(IReadOnlyList<PrintedMessage>) / AddMessage(PrintedMessage) / AppendText(LocalizableText) / UniTask RevealMessages(float, AsyncToken)` | 已实查 | 本作 ADV 面板 `WitchTrialsTextPrinterPanel` **未覆写** AddMessage/SetMessages（只覆写 AuthorNamePanel/SetMessageAuthor），但基类 AddMessage 实测 0 命中 → 走 `SetMessageAuthor`/`set_Text` |
| 6 | 选项按钮生成 | `Naninovel.UI.ChoiceHandlerPanel` | `virtual void AddChoiceButton(ChoiceState choice)` | 已实查 | struct 参数；postfix 里用 `Traverse`/扫描 `ButtonsContainer` 找新按钮；或改挂 #7 |
| 7 | 选项按钮自身（推荐） | `Naninovel.UI.ChoiceHandlerButton` | `virtual void Initialize(ChoiceState choiceState)` | 已实查 | postfix 读 `__instance.ChoiceState`（**不需要绑定 struct 参数**）→ 最稳 |
| 8 | 选项对话事件 | `Naninovel.UI.ChoiceHandlerPanel` | `event Action<ChoiceState> OnChoice` | 已实查（`add_OnChoice_Public_add_Void_Action_1_ChoiceState_0`，同名 backing 字段 `OnChoice`） | **字段+add/remove = C# event**；payload 非 blittable → 不能订阅，需 Harmony |
| 9 | 剧本状态 | `Naninovel.ScriptPlayer` | `OnPlay/OnStop(→ Script)`、`OnWaitingForInput/OnAutoPlay/OnSkip(→bool)`、`OnCommandExecutionStart/Finish(→Command)` | 已实查 `IScriptPlayer` | **`Action<bool>`/`Action<Script>`/`Action<Command>` 均 blittable → 可用 `ConvertDelegate` 订阅**（推荐，省 patch） |
| 10 | 面板可见 | 任意 `IManagedUI` | `event Action<bool> OnVisibilityChanged` | 已实查 | 可订阅；面板实例由 `IUIManager.GetUI<T>()` 或 `FindObjectOfType` 拿 |
| 11 | 语音结束 | `Naninovel.IAudioTrack` | `event Action OnPlay / OnStop` | 已实查 | 无参 Action 可订阅；`GetVoiceTrack(path)` 可能为空（待探针） |
| 12 | Toast | `Naninovel.UI.ToastAppearance` | `void SetText(string)` + `UnityEvent<string> onTextChanged` | 已实查 | 事件可订阅或 patch `SetText` |
| 13 | 按键继续/推进 | `Naninovel.IInputManager` / 打印面板 `ContinueInputTriggers` | `bool ProcessInput; IInputSampler GetSampler(string); void AddBlockingUI(IManagedUI, params string[])` | 已实查 | 读 `WaitingForInput`+`ProcessInput` 判断“当前等待点击”；自动化推进时调 `IScriptPlayer` 或模拟采样器 |

### 7.1 `Print` 的调用链证据（回答 Q1）

- 标准 @print：`PrintText` 命令 → `PrintText.Print(printedText, printer, token)` → `base.PrinterManager.Print(printer.Id, message, Append, AssignedRevealSpeed, token)`（**参考同代源码** `Naninovel.Commands\PrintText.cs:260`；本作签名已实查同样存在该方法）。
- `TextPrinterManager.Print` 内部触发 `OnPrintStarted` → `printer.AddMessage/AppendText` → `await printer.Reveal` → `OnPrintFinished`（参考同代 `Naninovel\TextPrinterManager.cs:96-113`）。
- `RevealableText.set_Text` 由面板的 reveal 流程（`TextRevealer`/`StringBuilder`）逐字写入（参考 `RevealableTextPrinterPanel` 的 `builder`/`revealer` 字段与 `RevealMessages`；本作签名已实查这些成员），因此 set_Text 是 **内容/时序双重可靠** 的观测点。
- 本作辩论 `@printDebate`：命令 `WitchTrials.Views.PrintDebateText` 自己拿 `ITextPrinterActor`（状态机有 `UniTask<ITextPrinterActor>.Awaiter`）并有内嵌 `_Print`（`printer` 字段 + `UniTask.Awaiter`）；**是否调用 `ITextPrinterManager.Print` 无法从签名断定 → 待探针**。若探针显示 `Print` 未被辩论行触发，则辩论内容钩子必须用 #4/#5/辩论面板（witchtrials-re）。

---

## 8. IL2CPP 可调用性（本作实测/已知）

1. **委托转换（最关键）**：`DelegateSupport.ConvertDelegate<T>(handler)` 只接受 **参数全 blittable** 的委托。`PrintMessageArgs`（含引用字段的结构体）会抛：
   ```
   System.ArgumentException: Delegate has parameter of type Naninovel.PrintMessageArgs (non-blittable struct) which is not supported
   ```
   （env-probe run3 实测，lead 转述）。同理 **`Action<ChoiceState>`（OnChoice）也不能订阅**。可安全订阅的：`Action`、`Action<bool>`、`Action<string>`、`Action<Script>`、`Action<Command>`、`Action<IEngineService>` 等引用/基础类型参数。
2. **struct 返回值/属性**：interop wrapper 已处理装箱（`il2cpp_object_unbox`），直接从 C# 读 `MessageAuthor.Id`、`ChoiceState.Summary`、`PlaybackSpot.LineIndex` 等 OK。
3. **protected 成员**：interop 将成员统一生成为 `public unsafe`，可调用（无运行时访问检查）；原生 token 名里仍写着 `Protected`（例：`get_Author_Protected_Virtual_New_get_MessageAuthor_0`）。
4. **接口**：interop 把接口渲染为 `public class X : Il2CppObjectBase`；拿到实现类实例后直接调用成员即可；需要 `Il2CppObjectBase.TryCast<T>()` 做接口/子类转换（env-facts G4 坑：`RectTransform` 等要用 TryCast 而非 C# cast）。
5. **属性 setter 挂点**：`set_Text` 是 `Public_Virtual_New`，Harmony 按方法名 patch 即可（参考作已验证）；本作 `RevealableText` 在 `Elringus.Naninovel.Runtime.dll`，HarmonyX 的 IL2CPP detour 对 native 方法生效（**待本作探针最终确认**，probe v3 计划）。
6. `UnityEngine.UI.Button`（`UnityEngine.UI.dll`）有 `onClick`（`ButtonClickedEvent : UnityEvent`）；`Selectable.Select()`、`EventSystem.SetSelectedGameObject(GameObject)` / `(GameObject, BaseEventData)`、`EventSystem.current` / `currentSelectedGameObject` 均已实查 → 键盘焦点/激活可用标准 uGUI 路径。
7. 对象查找：`UnityEngine.Object.FindObjectOfType<T>()` / `FindObjectsOfType<T>()`（Unity 6 仍存在，可能有新 API `FindObjectsByType`）；`GameObject.GetComponentInParent<T>()` / `GetComponent<T>()` 标准可用。

---

## 9. 探针结论（probe2/probe3/probe4 实机，2026-10-28）

证据文件：`re\logs\re-probe-run-probe4-191710.log`（103 行 RE_*，含 Parts/lineId 证据）、`re\logs\re-probe-run-probe3-190340.log`（89 行）、`re\logs\LogOutput-probe3.log`、`re\logs\re-probe-journal-probe3.log`；probe2 证据 `re\logs\re-probe-run-probe2-185840.log`。探针源码 `re\probe\NaninovelReProbe\ReProbePlugin.cs`；运行脚本 `re\probe\run_re_probe.ps1`（`-Tag probe4`）。

| # | 待验项 | 结论 | 证据 |
|---|---|---|---|
| 1 | `TextPrinterManager.Print` prefix 能否挂 / struct 参数绑定 | **能挂、能绑定**。prefix 形参 `(string printerId, PrintedMessage message, bool append, float speed)` 实测拿到 `printerId='Normal' append=False speed=1` | `RE_PATCH_OK TextPrinterManager.Print ... +prefix`、`RE_PRINT #1 printerId='Normal' ... text=[...]` |
| 2 | `RevealableText.set_Text` 本作可 detour | **可**。postfix 每句命中 4 次；**其中 3 次是空串**（面板初始化/重置），1 次正文 → 必须过滤空串 + 按实例去重。probe4 计数：`setTextCalls=4 setTextEmpty=3` | `RE_PATCH_OK RevealableText.set_Text ... +postfix`、`RE_SETTEXT #1..#4`、`RE_SUMMARY ... setTextEmpty=3` |
| 3 | `@printDebate` 是否触发 `TextPrinterManager.Print` | **仍未跑通**（probe4 用 `LoadAndPlay("Act01_Chapter01/Act01_Chapter01_Trial01")` 载入辩论脚本，但脚本未真正执行：无任何 `RE_PRINT`，随后回到 `System/System_Title`；怀疑 Trial01 受 trial 状态/变量门控，必须从剧情正常推进进入）。已实查辩论 printer id 静态值：`PrintDebateText.DebatePrinterId='Debate'` → 判定辩论行只需 `printerId=='Debate'`（或 `DEBATE_PRINTER_ID` 日志） | `RE_DEBATE_PRINTER_ID static PrintDebateText.DebatePrinterId='Debate'`、`RE_LOADPLAY_OK #1 ... Trial01` 后无 `RE_PRINT`、`RE_EVENT OnPlay script=System/System_Title` |
| 4 | 旁白行 author 形态 | **`PrintedMessage.author` 字段是“非 null 的空 author”**（probe4 用字段读法不再 NRE）：`author={id='<null>' label=''}`。`PrintedMessage.Author` 的 `Nullable` getter 在旁白时抛 NRE（probe3），**不要用**。旁白判定 = `author == null || string.IsNullOrEmpty(author.Id)`；说话人行取 `author.Id`（= CharacterNames 的 key，如 `Ema`）+ `author.Label`（本地化显示名） | `RE_PRINT #1 author={id='<null>' label=''} text=[...]`；probe3 `author={ERR:NullReferenceException}` |
| 5 | `LocalizableText` 本地化时点 / lineId 获取 | `set_Text` 与 `Print` 的 `message.Text` 都已是 **zh-Hans 最终文本** 且不含 `|#lineId|`/`Name:` 前缀；**lineId 从 `message.Text.parts[i].id` 拿**，probe4 实证：`id='0101Adv05_Narrative001'`、`spot=Act01_Chapter01/Act01_Chapter01_Adv05:11`、`plain=False`、`text=[]`（part.text 在模板化文本里为空，正文以 `ToString()` 合并后为准） | `RE_PRINT #1 text=[沾满鲜血的会客厅内，<br>少女们陷入了恐慌。]`、`RE_PRINT_PARTS id#1 count=1 [0]{id='0101Adv05_Narrative001' plain=False spot=Act01_Chapter01/Act01_Chapter01_Adv05:11 text=[]}` |
| 6 | `ConvertDelegate` 订阅 | `Action<bool>`/`Action<Script>` **可订阅**并实收回调（`RE_SUB_OK` + `RE_EVENT OnWaitingForInput/OnAutoPlay/OnPlay/OnStop`）；`PrintMessageArgs`/`ChoiceState` 仍不可 | `RE_SUB_OK ...`、`RE_EVENT OnPlay script=System/System_Title` |
| 7 | `IScriptPlayer.Play(path)` 能否直接进正片 | **不能**：抛 `Naninovel.Error: Failed to get '...' resource of type 'Naninovel.Script': not loaded.`；必须 `ScriptPlayerExtensions.LoadAndPlay(player, path, 0)`（先加载再播，probe3/probe4 均能进 ADV 并触发打印） | `RE_PLAY_FALLBACK_FAIL`（probe2）、`RE_LOADPLAY_OK`+`RE_EVENT OnPlay script=Act01_Chapter01/Act01_Chapter01_Adv05`（probe4） |
| 8 | 标题是否 Naninovel `TitleMenu`/`TitleNewGameButton` | **不是**：`FindObjectsOfType<TitleNewGameButton>`=0；标题脚本 `System/System_Title`；游戏自绘标题按钮 `Button_NewGame`（uGUI Button）`onClick.Invoke()` 只重播 System_Title、不进正片 | `RE_TITLE_SCAN titleNewGameButtons=0`、`RE_UI_CLICK_OK name='Button_NewGame'` |
| 9 | `DebateTextPrinterPanel.SetText` 可否挂 | **不能**：probe3/probe4 两次独立进程均 `HarmonyException: IL Compile Error`；辩论内容钩子改用 `AddMessage`/`SetMessages`（witchtrials-re） | `RE_PATCH_FAIL hit:DebateTextPrinterPanel.SetText` |
| 10 | `Engine.GetService` 初始化时机 | 首帧后轮询即可：`t=6.9s` 拿到 `ITextPrinterManager`；Harmony 必须在此之后安装（早装会 AccessViolation） | `RE_ENGINE_READY t=6.9`、probe1 崩溃栈 |
| 11 | 无人值守 autoplay 推进 | **不可靠**：`SetAutoPlayEnabled(true)` 生效（`RE_EVENT OnAutoPlay True`），但进 ADV 后脚本停在首个 `@print` 处不前进（2 分钟 `playedIndex`/`printCalls` 不变，`waitingInput=False`）；`SetSkipEnabled(true)` 后 `skip=False` 且不推进。→ 探针/自动化不要依赖 autoplay 连播；READER 本身也不该依赖 | probe4 `RE_STATE ... printCalls=1`（t=109→240 恒定）、`RE_SKIP_ON/RE_SKIP_OFF`、`RE_SUMMARY ... choiceAddHits=0` |
| 12 | `@choice` 钩子是否命中 | probe4 因脚本不推进未到达选项行（`choiceLogs=0 choiceAddHits=0`）；`ChoiceHandlerPanel.AddChoiceButton` 补丁可挂、`OnChoice(Action<ChoiceState>)` 不可订阅的静态结论不变 | `RE_SUMMARY ... choiceLogs=0` |

**probe4 额外确认（可直接用于实现）**：
- 面板类型不稳定：同一 `name='NormalPrinter'` 在采样间先后显示 `WitchTrialsTextPrinterPanel` / `RevealableTextPrinterPanel` / `UITextPrinterPanel`；`DebatePrinter` 面板采样为 `DebateTextPrinterPanel` / `UITextPrinterPanel`。→ 只按 `name`（actor id）与 `GetComponentInParent<RevealableTextPrinterPanel>` 判定，不按 `GetType()`。
- 场景里 `printerPanels=2` 恒定存在（`NormalPrinter` + `DebatePrinter`，都 `active=True`），所以**面板 active 不能当“有台词”信号**；用 `Print` 事件 + `msgs`/`text` 判空。
- `RE_HIT_ADV_SETMESSAGEAUTHOR` 每句命中 4 次（作者清空/重设节律），`RE_HIT_MODIFIED_WAIT False` 在每句末尾 1 次（等待点击指示器隐藏）→ “当前行展示完毕/等待点击”可用 `RevealableTextPrinterPanelModified.SetWaitForInputIndicatorVisible(true)` 检测（补丁可挂，见 #patch 表）。

**关键新发现（lineId 权威来源）**：`PrintedMessage.Text` 是 `LocalizableText`，其 `Parts[i]` 为 `Naninovel.LocalizableTextPart`（实查成员：`string id`、`PlaybackSpot spot`、`string text`、`bool PlainText`；另有 `LocalizableText.Parts`、`ToString()=string.Join(" ",Parts)`）。打印文本**不含** `|#lineId|` 也不含 `Ema:` 前缀（说话人在 `MessageAuthor`），因此语音/剧透按行 id 对齐时请读 `Parts[i].Id` + `.Spot`，不要正则文本。`MessageTemplate.template`（如 `{author}: {text}`）可辅助格式还原。probe4 已实测该路径（`id='0101Adv05_Narrative001'`，`spot` 含脚本路径与行号）。

**面板实例会换**：同一 actor `'NormalPrinter'` 的运行时类型在两次采样间从 `WitchTrialsTextPrinterPanel` 变为 `RevealableTextPrinterPanel` → A11Y 不要缓存 panel 实例；每次用 `FindObjectsOfType<RevealableTextPrinterPanel>(true)` + `activeInHierarchy` 现取，归属用 `set_Text` 的 `GetComponentInParent`。

**ADV 路径覆盖链实查**：`WitchTrials.Views.WitchTrialsTextPrinterPanel : GigaCreation.NaninovelExtender.Ui.RevealableTextPrinterPanelModified`；该面板只 override `AuthorNamePanel`/`SetMessageAuthor`，**不 override AddMessage/SetMessages**；但 `Naninovel.UI.UITextPrinterPanel.AddMessage` prefix 在 ADV 打印时命中 **0 次** → 该基类方法不是本作 ADV 的真入口，A11Y 不要以它为主钩子（真入口 = `Print` + `set_Text` + `SetMessageAuthor`）。

---

## 10. 与参考作 noexistence_a11y 的差异（必须改）

| 参考作 | 本作 | 影响 |
|---|---|---|
| `PrintTextArgs` + `OnPrintTextStarted` | `PrintMessageArgs` + `OnPrintStarted`（且 **不能 ConvertDelegate**） | 事件订阅方案作废 → 改 Harmony `Print` prefix |
| `ITextPrinterManager.add_OnPrintTextStarted` | 同上，且 `PrintMessageArgs` 非 blittable | 只能 patch |
| 打印文本钩子 `RevealableText.set_Text` | 同（本作 `RevealableText` 存在于 runtime） | 可沿用，但需重做面板/角色判定 |
| `UITextPrinterPanel.FormatMessage` 已有 | 本作也存在（已实查） | 可用于“这一条最终会显示什么”的校验 |
| QTE = `Naninovel.UI.QTEUI` | **本作无 Naninovel QTE 类型**（已 grep 全类型）；限时来自 `WitchTrials.Models.Timer`/`SetTrialTimer` | QTE 归 witchtrials-re；Naninovel 层无对应 patch 点 |
