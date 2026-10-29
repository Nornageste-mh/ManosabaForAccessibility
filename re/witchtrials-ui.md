# WitchTrials.Views UI 层逆向报告（A11Y 读屏补丁目标）

> 目标游戏：《魔法少女ノ魔女裁判》(Steam appid 3101040)，Unity 6000.0.48f1 + IL2CPP，Naninovel VN 引擎 + GigaCreation 扩展层 + 本作自定义层 `WitchTrials.*`。
> 本文只描述**类型 / 方法 / 字段 / 结构**，不含任何游戏内台词文本。
> 负责范围：`Assembly-CSharp.dll` 中的 `WitchTrials.Views` / `WitchTrials.Models` / `WitchTrials.Presenters` / `WitchTrials.Debugging`，以及必需的最小 Naninovel / GigaCreation 基类。Naninovel 本体 API 另见 `re/naninovel-api.md`（他人文档）。

## 0. 证据来源与证据等级

**产物位置**

| 内容 | 路径 |
|---|---|
| 全量反编译工程（`ilspycmd -p`，241 个 .cs） | `re/decompiled/Assembly-CSharp-proj/WitchTrials.{Views,Models,Presenters,Debugging}/` |
| GigaCreation 6 个 dll 全量反编译 | `re/decompiled/Giga-proj/<dll 名>/` |
| Naninovel 关键基类单类型反编译 | `re/decompiled/Naninovel-types/` |
| 每类型签名清单（摘要格式，含 IL2CPP 代理签名） | `re/decompiled/signatures/witchtrials/*.txt`（143 个）等 |
| 类型全表 | `re/decompiled/witchtrials-types.txt` |
| 签名提取脚本（可重跑） | `re/tools/extract_sig.py` |

**证据等级（本文所有结论标注等级，禁止当成"可运行代码"）**

- **E1 直接实查**：类型/基类/字段名/字段类型/方法名/方法参数类型，均出现在反编译输出中，可直接引用。
- **E2 结构推断**：由 async 状态机（`_Xxx_d__N`）的字段、闭包（`__c`/`__c__DisplayClassN_M`）签名、字段组合推断出的控制流；不保证细节（顺序、条件、数值）。
- **E3 命名约定推断**：类名 → 剧本命令别名、`XxxVarName` 静态字符串 → 变量名等；**必须探针或剧本资源实查确认**。

**关键限制（必须先读）**

1. interop dll 是 IL2CPP 包装桩：方法体一律是 `il2cpp_runtime_invoke`，**无法从 dll 恢复原始逻辑**。所谓"反编译代码片段"只能是签名 + 状态机结构，本文不编造函数体。
2. `ilspycmd` 输出中自定义特性（attribute）**几乎全部被剥掉**：全库实测 `CommandAlias` / `ParameterAlias` 命中数为 **0**（只剩 `[ObfuscatedName(...)]` 与 `[System.Serializable]`）。→ **`@命令别名` 与 `参数别名`（pos/roll/label 之类）无法静态证实**，只能由类名按 Naninovel 约定推断（E3）。
3. 游戏安装目录内**没有明码 Naninovel 剧本**（`D:\Steam\steamapps\common\manosaba_game\` 顶层仅 `BepInEx/ D3D12/ dotnet/ manosaba_Data/`，无 `*.nani`、无 StreamingAssets 剧本文本）；剧本应在 Addressables bundle 内，属于 `textassets` 队友的范围。
4. IL2CPP 互操作陷阱（写代码时）：`someTransform is RectTransform` 恒 false、`component as Toggle` 恒 null → 必须 `TryCast<T>()`；`Il2CppSystem.Collections.Generic.List<T>` 不是 .NET `List<T>`；字符串走 `Il2CppSystem.String`（反编译里显示为 `string` 但 Harmony 补丁方法签名要按 interop 类型写）。

---

## 1. 辩论 / 审判文本打印链（必查项 1）

### 1.1 链路总览（E1）

```
剧本 @printDebate（别名 E3）
  └─ WitchTrials.Views.PrintDebateText : Naninovel.Commands.PrintText        ← 命令层
       │  AssignedPrinterId 覆写 → 固定打印机 id（static DebatePrinterId）
       │  ModifyDebatePrinter(IDebateTextPrinterActor) 写入 AnimationKind
       └─ printer actor = WitchTrials.Views.DebateTextPrinter : Naninovel.UI.UITextPrinter   ← 演员层（空壳）
            └─ panel = WitchTrials.Views.DebateTextPrinterPanel : Naninovel.UI.UITextPrinterPanel  ← 面板层（真正的 UI）
                 │  SetText(string) / AddText(string) / AppendText(LocalizableText) / AddMessage(PrintedMessage)
                 │  Il2CppReferenceArray<DebateText> _labels  ← 多行标签池
                 └─ 文本承载 = WitchTrials.Views.DebateText : Naninovel.UI.NaninovelTMProText : TextMeshProUGUI
```

**普通对话（非辩论）走另一条链**，见 1.6；两条链的 panel 不同，补丁点也不同。

### 1.2 命令层 `WitchTrials.Views.PrintDebateText : Naninovel.Commands.PrintText`（E1）

```csharp
// 自身参数（E1；DecimalListParameter = 逗号分隔浮点列表）
public unsafe DecimalListParameter ScenePosition;   // 屏幕位置，列表
public unsafe DecimalParameter     Roll;            // 旋转
public unsafe StringParameter      AnimationKind;   // 入场动画种类（对应 DebateTextAnimationKind）

// 静态成员（E1）
public static string DebatePrinterId;          // 固定打印机 id
public static string DebateWaitTimeVarName;    // 文本停留时长变量名
public static string EnableDebugSkipVarName;   // 调试跳过开关变量名
public static Regex  s_pattern1, s_pattern2, s_linkIdRegex;
public static bool   IsEnabledDebugSkip { get; }   // 读变量判断

// 覆写（E1）
public override string AssignedPrinterId { get; }                    // → 固定打到辩论打印机
public override UniTask Execute(AsyncToken token = null);
public override Il2CppSystem.IDisposable CompleteOnContinue(ref AsyncToken token);
public override UniTask Print(ITextPrinterActor printer, AsyncToken token);
public override bool   ShouldPlayAutoVoice();
public override UniTask WaitAutoVoice(AsyncToken token);

// 自有（E1）
public void  ModifyDebatePrinter(IDebateTextPrinterActor printer);   // 把 AnimationKind 传给 actor
public UniTask WaitForDebate(float time, AsyncToken token = null);   // 辩论行专用等待
public float CalculateWaitTime();
public static float CalculateWaitTimeFromMessage(string message);    // 按字数算停留时长
```

状态机（E2）：`_Execute_d__19`（含 `__c__DisplayClass19_0{ printer; _Execute_b__0(AsyncToken) }`）、`_Print_d__21`、`_WaitForDebate_d__25{ time; token; float _elapsedTime_5__2 }` → `WaitForDebate` 是**每帧累加 `_elapsedTime` 的自实现计时循环**，不是单纯 `Delay`（`EnableDebugSkip` 时可能提前结束，E3）。

> A11Y 含义：辩论行的"停留时长"= 字数换算，**读屏语速与之无关**；想让读屏读完再继续必须自己接管，不能依赖游戏计时。

### 1.3 演员层 `WitchTrials.Views.DebateTextPrinter : Naninovel.UI.UITextPrinter`（E1）

整个类型只有一个成员：

```csharp
public virtual DebateTextAnimationKind AnimationKind { get; set; }   // 入场动画方向
```

`DebateTextAnimationKind` = `Right, Up, Left, Down`（E1，枚举实查）。

### 1.4 面板层 `WitchTrials.Views.DebateTextPrinterPanel : Naninovel.UI.UITextPrinterPanel`（E1）

```csharp
public unsafe FontMaterialChanger _fontMaterialChanger;
public unsafe AutoLayoutSupporter _autoLayoutSupporter;
public unsafe HorizontalOrVerticalLayoutGroup _layoutGroup;
public unsafe Il2CppReferenceArray<DebateText> _labels;            // ← 多行文本池（固定数组）
public unsafe Il2CppReferenceArray<LabelSpaceData> _labelSpaceData;
public unsafe StringBuilder _builder;
public unsafe ILocalizationProvider _localizationProvider;
public unsafe ICustomVariableManager _naniVarManager;
public unsafe IDictionary<LocaleKind, float> _labelSpaces;
public unsafe CancellationTokenSource _revealCts;
public static string EnableDebugSkipVarName;
public static bool   IsEnabledDebugSkip { get; }
public override float  RevealProgress { get; }
public override string Appearance { get; set; }

public override void   Awake();
public override void   OnDestroy();
public override UniTask Initialize();
public override void   SetMessages(IReadOnlyList<PrintedMessage> messages);
public override void   AddMessage(PrintedMessage message);
public override void   AppendText(Naninovel.LocalizableText text);
public override UniTask RevealMessages(float delay, AsyncToken token);
public override void   SetWaitForInputIndicatorVisible(bool visible);
public override void   SetFont(TMP_FontAsset font);

public void   SetText(string text);       // ← 整段替换
public void   AddText(string text);       // ← 追加一行
public static UniTask WatAndRevealAsync(DebateText label, float delay, CancellationToken ct);
public static UniTask Gosub(string id);   // ← 点击 <link> 后执行的剧本跳转
```

嵌套：`LabelSpaceData { LocaleKind _localeKind; float _space; }`（按语言的排距）。

**`<link="...">` 点击行为（E1+E2）**：`Initialize` 里的两个静态闭包
`__c._Initialize_b__26_0(DebateText label) → IObservable<string>` 与 `__c._Initialize_b__26_1(string id) → void`
→ 结构为「对每个 label 订阅 `label.OnLinkClickedAsObservable()`，回调里 `Gosub(id)`」（E2，闭包签名 + `static Gosub(string)` 双向印证）。
即：文本中的 link id 就是剧本 Gosub 目标（多为证据提示段落）。

**逐字/逐行（E2）**：辩论链**没有** `RevealableText`（那是普通对话链），逐字由 `WatAndRevealAsync(DebateText label, delay, ct)` 按 label（行）做揭示动画，状态机 `_WatAndRevealAsync_d__35` 携带 `DebateText label` / `float delay` / `CancellationToken ct`；`_revealCts` 用于中途取消。→ 辩论文本的"进度"粒度是**行**，不是字符。

### 1.5 文本承载 `WitchTrials.Views.DebateText : Naninovel.UI.NaninovelTMProText`（E1）

```csharp
// DebateText 自有
public unsafe Il2CppSystem.ISubject<string> _onLinkClickedSubject;
public unsafe IInputManager _inputManager;
public unsafe int _selectedLink;
public unsafe IDisposable _disposable;
public override void OnDestroy();
public override void OnLinkClicked(TMP_LinkInfo linkInfo);
public override string ProcessText(string text);
public override string ProcessRubyTags(string text);
public void ModifyLinkedTextColor(Vector2 position, Camera renderCamera);
public Il2CppSystem.IObservable<string> OnLinkClickedAsObservable();   // ← 链接点击事件流
// 闭包 __c__DisplayClass8_0 { Camera renderCamera; void _ProcessText_b__0(InputAction.CallbackContext); }
```

基类 `Naninovel.UI.NaninovelTMProText : TextMeshProUGUI`（E1）提供：

```csharp
public string unprocessedText;                     // ← 未处理原文（带标签）
public override string text { get; set; }          // 处理后文本
public string linkTemplate;  public LinkClickedEvent onLinkClicked;
public StringUnityEvent onTipClicked;  public string tipTemplate;
public virtual void OnLinkClicked(TMP_LinkInfo linkInfo);
public virtual void OnTipClicked(string tipId);
public virtual string ProcessText(string text);
public virtual string ProcessLinkTags/ProcessRubyTags/ProcessEventTags/ProcessExpressionTags/ProcessTipTags(string s);
public virtual bool IsRuby(int charIndex);
public IReadOnlyCollection<string> PopEventsAtChar(int index);
```

> **读屏注意**：`DebateText.text` / `.unprocessedText` 可能带 `<link=...>`、`<ruby=...>`、`<tip=...>`、事件标签；朗读前必须剥标签或走 `ProcessText` 之后再清洗（否则屏幕阅读器会读标签名）。`<ruby>` 注音在 `ProcessRubyTags` 里被合并，朗读要不要保留注音需产品决策。

### 1.6 普通对话链（非辩论）——读屏首选（E1）

```
WitchTrials.Views.WitchTrialsTextPrinterPanel : GigaCreation.NaninovelExtender.Ui.RevealableTextPrinterPanelModified
   : Naninovel.UI.RevealableTextPrinterPanel : Naninovel.UI.UITextPrinterPanel : Naninovel.UI.CustomUI
      └─ IRevealableText RevealableText = Naninovel.UI.RevealableText : Naninovel.UI.NaninovelTMProText
```

`RevealableTextPrinterPanel` 现成钩子（E1）：

```csharp
public UnityEvent onRevealStarted;         // ← 开始逐字
public UnityEvent onRevealFinished;        // ← 逐字结束（可朗读整句）
public virtual void HandleCharRevealed(char character, AsyncToken token);   // ← 逐字！每字符一次
public virtual UniTask RevealMessages(float delay, AsyncToken token);
public string FormatMessage(PrintedMessage message, bool adjacent);         // ← 纯净文本（推荐）
public void SetMessages(IReadOnlyList<PrintedMessage> messages);
public void SetMessageAuthor(MessageAuthor author);
public MessageAuthor Author { get; }  public CharacterMetadata AuthorMeta { get; }
public IRevealableText RevealableText { get; }  public List<PrintedMessage> Messages { get; }
```

`RevealableText : NaninovelTMProText`（E1）：`RevealNextChars(int count, float duration, AsyncToken)`、`LastRevealedCharIndex`、`GetLastRevealedChar()`、`RevealProgress`、`SetRevealProgress(float)`、`TMPText`、`RevealState`、`RequestConfirmation/ResolveConfirmation`。

`WitchTrialsTextPrinterPanel` 自身（E1）：字段 `_authorTextSettings`、`_changeScaleByAppearances`、`_authorPlateCanvasGroup`、`_authorTextBuilder`、`_changeScaleTargets`；属性 `AuthorNamePanel`、`AuthorPlateCanvasGroup`；覆写 `Initialize()`、`SetMessageAuthor(MessageAuthor)`、`SetActiveAppearance(string)`。
父类 `RevealableTextPrinterPanelModified`（E1）：`_skipModeIndicatorCanvasGroup`、`_fontMaterialChanger`、`_naniScriptPlayer`、`_activeAppearance`；`SetWaitForInputIndicatorVisible`、`SetFont`、`GetActiveAppearance`、`SetActiveAppearance`、`HandleAutoModeChanged`、`HandleSkipModeChanged`。

> **结论**：普通台词朗读用 `onRevealFinished`（整句）或 `HandleCharRevealed`（逐字）+ `FormatMessage` 取文本；辩论台词没有这两者，见 1.4。

### 1.7 辩论状态中枢：HP / 计时 / 模式（E1）

**`WitchTrials.Models.IWitchTrialsNaninovelManager`（接口，实查）**

```csharp
bool IsInDebate { get; }                 // ← 当前是否辩论
bool IsPresentEvidenceMode { get; }      // ← 当前是否"提示证据"模式
IObservable<TimeSpan> OnRemainingTimeChangedAsObservable();   // ← 倒计时（可直接订阅朗读/提醒）
IObservable<bool>     OnDebateSkippableChangedAsObservable(); // ← 是否可跳过
IObservable<Unit>     OnEnterTitleScreenAsObservable();
void SetTitleScreenShown(bool shown);
void ChangeTrialMode(bool trialMode);
void ChangeDebateMode(bool debateMode);
void ChangeTrialPauseMode(bool paused);
void SetTrialTimer(...);
```

实现 `WitchTrials.Views?`（实为 `WitchTrials.Models.WitchTrialsNaninovelManager : Naninovel.NaninovelManager`）字段：`MaxHpVarName`、`PerspectiveStageOffsetX/Y/ZVarName`、`VoicePitchName`、`_isInTitleScreen`、`_isTrialMode`、`_canFastForwardDebate`、`_trialTimer`、`_perspectiveStageManager`、`_naniAudioManager`、`_naniScriptPlayer`、`_naniVarManager`、`_masterData`、`_pauseRequestCount`、`_originalSkipTimeScale`；方法 `StartTrial/StopTrial/StartDebate/StopDebate/PauseTrial/UnpauseTrial/PauseOrUnpauseVoicePlayer/OnSkipModeChangedDuringDebate/ForceRemainingTimeToNearlyZero`。

**`WitchTrials.Models.Timer`**（纯 C# 类，非 MonoBehaviour）：`_useRealtime`、`_remainingTime`、`_runningCts`、`Speed`；属性 `RemainingTime`、`Speed`、`TimeSinceStartup`；方法 `Dispose`、`ForceRemainingTimeToNearlyZero`、`SetTime`、`StartAsync`。

**`IMasterData`**：`DefaultHp`、`SkipTimeScaleDuringDebate`、`VoicePitchOnSkip`。

**HUD `WitchTrials.Views.DebateUi : Naninovel.UI.CustomUI`（E1）**

```csharp
public static string MaxHpVarName;          // 最大 HP 变量名
public static string TimeIsUpLabelName;     // 超时标签名
public static string SubroutineScriptPath;
public unsafe TextMeshProUGUI _label;       // ← HP/计时显示用的 TMP
public unsafe SkipDebateButton _skipDebateButton;
public unsafe StringBuilder _builder;
public unsafe IWitchTrialsNaninovelManager _witchTrialsNaninovelManager;
public unsafe IScriptPlayer _naniScriptPlayer;
public unsafe ICustomVariableManager _naniVarManager;
public override UniTask Initialize();
public override UniTask ChangeVisibility(bool visible, Il2CppSystem.Nullable<float> duration = null, AsyncToken asyncToken = null);
public void OnVisibilityChangedByCustomUi(bool visible);
public void OnCurrentHpChanged(float currentHp);        // ← HP 变化回调（挂它朗读）
public string TimeSpanToText(Il2CppSystem.TimeSpan timeSpan);
public void AppendSpriteTag(char c);
```

闭包（E2）：`_OnVisibilityChangedByCustomUi_b__18_0(TimeSpan)`、`_b__18_1(bool canSkip)`、`_b__18_2(Unit)`、`_b__18_3(Unit)` → 可见时订阅「剩余时间 / 可否跳过」并刷新 `_label`。
**HP 的数值只出现在自定义变量里**（`MaxHpVarName` + `IDebateUi`），`TrialStateManager` 不含 HP（E1 反证：其字段只有 `PerspectiveStageKind`、`CameraState`、`ScreenState`…）。

**跳过按钮 `SkipDebateButton : MonoBehaviour`（E1）**：`_canvasGroup`、`_selectable`（`Selectable`）、`_iconImage`、`_iconShadow`、`_storedColor`、`_storedShadowSize`；`Present()`、`Dismiss()`、`OnPointerDownAsObservable()`、`OnPointerUpAsObservable()`、`SetIconShadowEnabled(bool)`。→ 它是"按住跳过"，键盘可达性需自行提供。

**异议演出 `ObjectionCutIn : GigaCreation...SpawnableWithAudio`（E1）**：`_playableDirector`、`_objectLibrary`、`_currentIndex`、`_cts`；`SetSpawnParameters(IReadOnlyList<string>, bool)`、`AwaitSpawn(AsyncToken)`、`AwaitDestroy(AsyncToken)`。**纯演出，无文本**。
其命令 `GosubToObjectionCutIn : AudioCommand`（E1）参数：`CutInKind: StringParameter`、`Index: IntegerParameter`、`VoicePath: StringParameter`、`Volume: DecimalParameter`、`GroupPath: StringParameter`、`AuthorId: StringParameter`；静态常量 `SubroutineScriptPath`、`ShowObjectionCutInLabelName`、`ObjectionSpawnPathVarName`、`ObjectionIndexVarName`；属性 `HasVoice`；覆写 `PreloadResources()`、`ReleaseResources()`、`Execute(AsyncToken)`。

### 1.8 辩论相关命令（E1，详见第 5 节）

| 类型 | 基类 | 参数 | 说明 |
|---|---|---|---|
| `ChangeDebateMode` | `Command` | `Enable: BooleanParameter` | 进出辩论模式 |
| `ChangeTrialMode` | `Command` | `Enable: BooleanParameter` | 进出审判模式 |
| `ChangeTrialPauseMode` | `Command` | `Enable: BooleanParameter`、`ProcessTimer: BooleanParameter` | 暂停（可连计时一起暂停） |
| `SetTrialTimer` | `Command` | `Time: DecimalParameter` | 设置剩余时间 |
| `Damage` | `Command` | **无参数（实查为空）** | 扣血；数值来源未知 → **待探针** |
| `CancelGosub` | `Command` | 无参数 | 中断当前 gosub（可用于取消辩论子流程） |

---

## 2. 审判选项（必查项 2）

### 2.1 类链（E1）

```
WitchTrials.Views.TrialChoiceHandlerPanel : GigaCreation.NaninovelExtender.Ui.ChoiceHandlerPanelModified
   : Naninovel.UI.ChoiceHandlerPanel : Naninovel.UI.CustomUI          ← 前两级都是空壳（无自有成员）
WitchTrials.Views.TrialChoiceHandlerButton : Naninovel.UI.ChoiceHandlerButton  ← 空壳
WitchTrials.Views.AdvChoiceHandlerButton  : Naninovel.UI.ChoiceHandlerButton    ← 有扩展逻辑
```

> 空壳 = 反编译里除基类与构造外**没有任何自有字段/方法**（E1 反证）。说明本作选项的差异全在 prefab 配置（`_customChoiceButtonData` / `_customButtons`）。

### 2.2 基类 API（E1，Naninovel 标准）

`Naninovel.UI.ChoiceHandlerPanel`：

```csharp
public Il2CppSystem.Action<ChoiceState> OnChoice;             // ← 玩家选中事件（ChoiceState 参数）
public List<ChoiceHandlerButton> ChoiceButtons { get; }
public RectTransform buttonsContainer;
public ChoiceHandlerButton defaultButtonPrefab;
public bool focusChoiceButtons;                                 // ← 是否自动聚焦（键盘关键）
public virtual void AddChoiceButton(ChoiceState choice);        // ← 每加一个选项调用一次（读选项文本的首选钩子）
public virtual void RemoveChoiceButton(string id);
public void RemoveAllChoiceButtons();  public void RemoveAllChoiceButtonsDelayed();
public virtual bool ShouldFocusAddedButton(ChoiceHandlerButton button);
public virtual void FocusAddedButton(ChoiceHandlerButton button);
public virtual ChoiceHandlerButton LoadCustomButtonPrefab(string path);
public override UniTask ChangeVisibility(bool visible, Il2CppSystem.Nullable<float> duration = null, AsyncToken asyncToken = null);
```

`Naninovel.UI.ChoiceHandlerButton : Naninovel.UI.ScriptableButton`：

```csharp
public ChoiceState ChoiceState { get; }        // ← 选项数据（含 summary/locked）
public Action<string> OnSummaryTextChanged;    public Action<bool> OnLock;
public SummaryTextChangedEvent onSummaryTextChanged;  public OnLockEvent onLock;
public virtual void Initialize(ChoiceState choiceState);
public virtual void HandleLockChanged(bool locked);    // ← 锁定/禁用状态（禁用原因朗读）
```

`Naninovel.ChoiceState`（`sealed class`，Il2Cpp 值类型包装，E1 实查）：

```csharp
public string id;  public PlaybackSpot hostedAt;  public bool nested;
public LocalizableText summary;        // ← 选项文本（本地化对象，不是 string！）
public bool locked;                    // ← 是否锁定
public string buttonPath;  public Vector2 buttonPosition;  public bool overwriteButtonPosition;
public string onSelectScript;          // ← 选中后执行的剧本（含剧透，勿朗读）
public bool autoPlay;
// 同名属性：Id / HostedAt / Nested / Summary / Locked / ButtonPath / ButtonPosition / OverwriteButtonPosition / OnSelectScript / AutoPlay
```

> **坑**：`ChoiceState.Summary` 是 `Naninovel.LocalizableText`，**没有直接 string**；朗读前需要走 Naninovel 的本地化求值（`ILocalizationManager` / `LocalizableText` 求值 API）→ **待探针确认取值路径**（见第 8 节）。

### 2.3 本作扩展 `AdvChoiceHandlerButton`（E1）

```csharp
public unsafe CanvasGroup _criticalChoiceHintCanvasGroup;   // ← "关键选项提示"的显隐（alpha）
public unsafe string _sfxPath;
public unsafe IAudioManager _naniAudioManager;
public unsafe IWitchTrialsChoiceHandlerManager _witchTrialsChoiceHandlerManager;
public override void OnEnable();  public override void OnDisable();  public override void OnButtonClick();
public override void Initialize(ChoiceState choiceState);
public void ChangeCriticalChoiceHintVisibility(bool enable);
```

`WitchTrials.Models.WitchTrialsChoiceHandlerManager : Naninovel.ChoiceHandlerManager`（E1）：`bool CriticalChoiceHintEnabled { get; }`、`event OnCriticalChoiceHintChanged`；其配置类型 `WitchTrialsChoiceHandlersConfiguration`、嵌套 `SettingsWitchTrials`。
开关 UI 在 `OptionsUi._criticalChoiceHintToggleGroup`（E1）→ 玩家可以在设置里关掉"关键选项提示"。

### 2.4 选项相关命令（E1）

| 类型 | 基类 | 参数/静态 | A11Y 结论 |
|---|---|---|---|
| `StopForChoice` | `Naninovel.Commands.Stop` | 静态 `Il2CppStringArray s_alwaysHandleChoiceIds`、`List<PlaybackSpot> s_chosenChoiceStates`；参数 `Clear: BooleanParameter`；`Execute`、`static ResetStaticFields()`；闭包 `_Execute_b__4_0(IChoiceHandlerActor)→bool`、`_Execute_b__4_1(LocalizableTextPart)→bool` | 停在选项处等待；`s_alwaysHandleChoiceIds` = 需特殊处理的选项 id 白名单（E3 命名推断）；`Clear` 可能清空已选记录（E2）。**读屏应在 `ChoiceHandlerPanel.AddChoiceButton` 处播报，而不是在此命令** |
| `ChoiceEvidence` | **`ShowWitchBookUi`** | 静态 `ChosenEvidenceIdVarName`；参数 `CorrectEvidenceId: StringParameter`；`Execute`、闭包 `_Execute_b__2_0(WitchBookCategory)→IEnumerable<IdVersionPair>`、`_Execute_b__2_1(IdVersionPair)→string` | 打开手册选证据。**`CorrectEvidenceId` 是正确答案 → 剧透红线，任何日志/朗读都不得触碰** |
| `ChoiceMapArea` | **`ShowWitchBookUi`** | 静态 `ChosenAreaIdVarName`；参数 `MapAreaNames: StringListParameter`、`FlagVariableName: StringParameter`；`GetRestrictionAreas()` | 打开地图选区域；`GetRestrictionAreas` 决定哪些区域可选 |
| `RecordCorrectChoice` | `Naninovel.Commands.SetCustomVariableAsFlag` | 静态 `PlayedCorrectChoiceVarName` | **记录"玩家是否选对过"** 的布尔变量（E3）。只能做**剧透闸门**：仅在玩家自己已选错/已看过结果后才允许朗读提示；不得用它提前朗读"正确/错误" |
| `ClearWitchBook` / `UpdateWitchBook` | `Command` | 见第 5 节 | 手册状态维护 |

### 2.5 A11Y 落地建议（本节结论）

1. 选项朗读：`Prefix/Postfix` `ChoiceHandlerPanel.AddChoiceButton(ChoiceState)` → 取 `Summmary`（待确认求值）→ 加入"可读选项列表"；选项被移除时（`RemoveChoiceButton`）同步移除。
2. 键盘焦点：Naninovel 已内置 `focusChoiceButtons` / `FocusAddedButton` / `ShouldFocusAddedButton`；读屏应监听 `ChoiceHandlerButton.onSummaryTextChanged` + 自建焦点播报（`ButtonExtendedBase.OnHighlightingChanged`，见 4.6）。
3. 锁定选项：`ChoiceHandlerButton.HandleLockChanged(bool)` → 播报"该选项当前不可选"。
4. 证据/地图选择：`WitchBookScreen.ChoiceEvidenceAsync` / `ChoiceAreaAsync`（见 3.6）是"选完返回"的入口，播报"请选择证据/请选择地点"，绝不读 `CorrectEvidenceId`。

---

## 3. 魔女手册 WitchBook（必查项 3）

### 3.1 入口与顶层（E1）

```
HUD 按钮：WitchTrials.Views.WitchBookButtonUi : Naninovel.UI.CustomUI
   fields: Button _button; string _openSfxPath; IAudioManager _naniAudioManager; IUIManager _naniUiManager; IWitchBookUi _witchBookUi
   methods: Start() + _Start_b__11_0(...)   // Start 里订阅按钮点击 → 打开手册
剧本命令：WitchTrials.Views.ShowWitchBookUi : Command
   fields: static string EnableDebugSkipVarName; StringListParameter CategoryNames; BooleanParameter Back; BooleanParameter Global;
           IWitchTrialsNaninovelManager _witchTrialsNaninovelManager; ICustomVariableManager _naniVarManager; IUIManager _naniUiManager;
           IWitchBookUi _witchBookUi
   methods: Execute(AsyncToken); ShowWitchBook(...)
UI 外壳：WitchTrials.Views.WitchBookUi : Naninovel.UI.CustomUI      ← IUIManager 注册的 CustomUI
   fields: WitchBookScreen _witchBookScreen; FontMaterialChanger _fontMaterialChanger
   methods: Initialize(); SetPresentationMode(...); SetRestrictionCategories(...); SetRestrictionMapAreas(...);
            ChoiceEvidenceAsync(...); ChoiceAreaAsync(...); GetActiveEvidences(...); UpdateVersion(...); ClearState(...);
            SetFont(...); SerializeState(GameStateMap); DeserializeState(GameStateMap); // ← 自定义存档状态（E1）
屏幕：WitchTrials.Views.WitchBookScreen : GigaCreation.Essentials.UI.NaninovelScreen
```

`IWitchBookUi`（接口，E1）：`SetPresentationMode`、`SetRestrictionCategories`、`SetRestrictionMapAreas`、`GetActiveEvidences`、`ChoiceEvidenceAsync`、`ChoiceAreaAsync`、`UpdateVersion`、`ClearState`。
`IWitchBookButtonUi`（接口，E1）：**无成员**（空接口，纯标记）。
`WitchBookUi.GameState`（嵌套类，E1 实查）：`Il2CppStructArray<WitchBookCategory> RestrictionCategories`、`Il2CppStructArray<MapAreaKind> RestrictionMapAreas` —— 即存档只保存「哪些类别/区域被限制」，**不保存当前页签与滚动位置**（读屏恢复需记住 UI 侧状态，不能依赖存档）。
`WitchBookPresentationMode`（枚举，E1）：`Local, Global`。
`WitchBookCategory`（枚举，E1）：`Clue, Profile, Map, Rule, Note`（共 5 类；`Clue` 用于证据提示模式，页面 `CluePage`）。

### 3.2 `WitchBookScreen` 结构（E1）

```csharp
public unsafe WitchBookItemButton _itemButtonPrefab;      // 条目按钮 prefab
public unsafe CanvasGroup _closeButtonCanvasGroup;
public unsafe WitchBookItemList _itemList;                // 条目列表（分页）
public unsafe string _closeSfxPath;
public unsafe PageDataCategoryItemMap[] _pageDataMap;     // ← 类别 → 页面/条目 映射
public unsafe DialogData _confirmToChoiceEvidenceDialogData;
public unsafe WitchTrialsTabToggle _tab;                  // ← 页签（单个 Toggle 组？属性 Tab）
public unsafe IInputManager _inputManager;  public unsafe IDialogProvider _dialogProvider;
public unsafe IWitchTrialsNaninovelManager _witchTrialsNaninovelManager;
public unsafe IAudioManager _naniAudioManager;
public unsafe Component _currentPage;  public unsafe PageDataItem _pageData;
public unsafe Component _cancelEventBlocker;  public unsafe CompositeDisposable _disposables;
public WitchBookPresentationMode PresentationMode { get; }
public IReadOnlyList<WitchBookCategory> RestrictionCategories { get; }
public IReadOnlyList<MapAreaKind> RestrictionAreas { get; }

public UniTask InitializeAsync(CancellationToken ct = null);
public override void BeginToPresent();  public override void EndToPresent();
public override void BeginToDismiss();  public override void EndToDismiss();
public void InitializePages();
public void ChangePage(WitchBookCategory category);              // ← 切页（播报页名的钩子）
public override void PlayCancelSfx();
public IReadOnlyCollection<IdVersionPair> GetActiveEvidences(WitchBookCategory category);
public void UpdateVersion(WitchBookCategory category, string id, int version);
public void ClearState(WitchBookCategory category);
public UniTask<string> ChoiceEvidenceAsync(IReadOnlyList<WitchBookCategory> categories, CancellationToken ct = null);
public UniTask<string> OnPresentEvidenceAsync(IReadOnlyList<WitchBookCategory> categories, CancellationToken ct = null);
public UniTask<MapAreaKind> ChoiceAreaAsync(CancellationToken ct = null);
public void Dispose();
// 嵌套 PageDataCategoryItemMap / PageDataItem；__c__DisplayClass42_0._InitializePages_b__0(bool isOn) → 页签 onValueChanged 绑定
```

### 3.3 页面基类（E1）

```csharp
public class WitchBookPageBase : MonoBehaviour
  public Canvas _canvas;  public GraphicRaycaster _graphicRaycaster;
  public int MaxShownItemListCount;  public WitchBookDataAsset _witchBookDataAsset;
  public WitchBookItemButton[] _itemButtonPool;  public WitchTrialsButton _presentEvidenceButton;
  public VersionedState _state;  public AddressableAssetLoader _addressableAssetLoader;
  public IInputManager _inputManager;  public ILocalizationProvider _localizationProvider;
  public IStateManager _naniStateManager;
  public WitchBookItemList _itemList;  public WitchBookItemButton _itemButtonPrefab;
  public WitchBookItemButton[] _itemButtons;  public Dictionary<..> _loadedDataItemMap, _displayableItemMaps;
  public HashSet<string> _itemIds;  public string _currentItemId;
  public virtual void Reset();  public virtual void Dispose();
  public virtual UniTask InitializeAsync(WitchBookItemList itemList, WitchBookItemButton itemButtonPrefab, CancellationToken ct = null);
  public virtual void InitializeItems();
  public virtual void Show(bool asPresentEvidenceMode);   // ← 页面显隐（保留证据模式）
  public virtual void Hide();
  public virtual void UpdateVersion(string id, int version);
  public virtual void ClearState();
  public virtual void InformAt(int idx);                  // ← 当前条目索引变化（键盘移动时播报！）
  public virtual IReadOnlyCollection<IdVersionPair> GetActiveEvidences();
  public virtual UniTask<string> OnPresentEvidenceAsync(CancellationToken ct = null);
public class WitchBookPageBase<TItem, TState> : WitchBookPageBase where TState : VersionedState, new()
  public override UniTask InitializeAsync(...);  public virtual UniTask<IList<VersionedItem<TItem>>> LoadDataAsync(CancellationToken ct = null);
  public void SerializeState(GameStateMap);  public UniTask DeserializeState(GameStateMap);
  public bool ItemIdExists(string id);  public bool ValidateAs(IdVersionPair pair);
  public Il2CppReferenceArray<IdVersionPair> GetDisplayableIdVersionPairs();
  public void ShowItemList();  public void HideItemList();
  public void ControlPresentEvidenceUi(bool isModeEnabled);   // ← 证据提示模式下按钮的启用/禁用
  public virtual void SetupItemButton(WitchBookItemButton button, VersionedItem<TItem> map);
  public virtual void RefreshPageContent(VersionedItem<TItem> map);
```

### 3.4 五个具体页面：字段与**文本来源**（E1）

| 页面 | 基类 | 文本/数据字段 | 文本来源 |
|---|---|---|---|
| `CluePage` | `WitchBookPageBase<ClueDataItem, ClueState>` | `TMP_Text _subjectLabel`、`TMP_Text _descriptionLabel`、`_thumbnail`、`_localizedTextData` | `ClueDataItem{Name, Description}`（Models）经 `LoadDataAsync` → `RefreshPageContent` |
| `NotePage` | `<NoteDataItem, NoteState>` | `static Regex s_tagPattern`、`TMP_Text _titleLabel`、`TMP_Text _descriptionLabel`、`_localizedTextData` | `NoteDataItem{Title, Description}`；`s_tagPattern` 说明描述里含自定义标签需剥 |
| `RulePage` | `<RuleDataItem, RuleState>` | `_managedTextProvider`、`TMP_Text _titleNumLabel`、`TMP_Text _subtitleLabel`、`TMP_Text _descriptionLabel`、`ITextManager _naniTextManager`、`_numberings`；方法 `GetTitleText` | `RuleDataItem{Numbering, Subtitle, Description}`；标题由 `GetTitleText()` 拼 |
| `MapPage` | `<MapDataItem, MapState>` | `Il2CppReferenceArray<MapFloor> _floors`、`_localizedTextData`、`_restrictionAreas`、`_displayableAreas`；方法 `InitializeAreas`、`OnChoiceMapAreaAsync` | `MapDataItem{ButtonText}` + 楼层/区域结构 |
| `ProfilePage` | `<ProfileDataItem, ProfileState>` | `TMP_Text _authorLabel`、`TMP_Text _descriptionLabel`、`_thumbnail`、`_authorTextSettings`、`ICharacterManager _naniCharacterManager`、`_authorTextBuilder` | `ProfileDataItem{Description}` + 角色名 |

（数据模型字段为 `WitchTrials.Models` 实查：`ClueDataItem{Name,Description}`、`NoteDataItem{Title,Description}`、`RuleDataItem{Numbering,Subtitle,Description}`、`MapDataItem{ButtonText}`、`ProfileDataItem{Description}`。）

### 3.5 条目与列表（E1）

```csharp
WitchTrials.Views.WitchBookItemButton : MonoBehaviour
  public RectTransform _rectTransform;  public CanvasGroup _canvasGroup;  public Button _button;
  public Image[] _backgrounds;  public RawImage _rawImage;  public TMP_Text _label;
  public AddressableAssetLoader _addressableAssetLoader;  public Texture2D _defaultTexture;  public CancellationTokenSource _cts;
  public void SetupWithText(string text, int backgroundIndex);   // ← 条目文本落点
  public void SetupWithAddressableTexture(string address);
  public void StateLoading();  public void SetTexture(Texture2D tex);  public void ClearContent();
  public Il2CppSystem.IObservable<Unit> OnChoiceAsObservable();  // ← 点击事件流

WitchTrials.Views.WitchBookItemList : MonoBehaviour
  public RectTransform _contentRoot;  public AutoLayoutSupporter _autoLayoutSupporter;
  public ScrollRect? _pagination (Pagination);  public int _itemInterval (ItemInterval);
  public void Reset();  public UniTask ExecuteRebuildingAsync();

WitchTrials.Views.WitchBookItemDescription : MonoBehaviour { TextMeshProUGUI _label; AutoLayoutSupporter; void SetText(string); }
WitchTrials.Views.WitchBookItemSubjectLabel : MonoBehaviour { TextMeshProUGUI _label; int _fontSizeDiff; float _initialSpace; void SetText(string); }
WitchTrials.Views.WitchBookItemThumbnail : MonoBehaviour { CanvasGroup _canvasGroup; RawImage _rawImage; ... void Setup(...); }
```

> **读屏关键**：条目文本可从 `SetupWithText(string text, int backgroundIndex)` 的 `text` 直接拿到（**不需要**解析 TMP）；`OnChoiceAsObservable()` 提供点击/选择事件；`InformAt(int idx)` 提供"当前第几个条目"。

### 3.6 页签 / 按钮 / 日志（E1）

```csharp
WitchTrials.Views.WitchTrialsTabToggle : GigaCreation.Essentials.Toggleables.SlideToggle (: UnityEngine.UI.Toggle)
  public Canvas _canvas;  public GraphicRaycaster _graphicRaycaster;
  public CanvasGroup _defaultLabel, _highlightedLabel;  public Image _inactiveBase, _activeBase;
  public override void OnHighlightingChanged();  public void OnValueChanged(bool isOn);   // ← SlideToggle 提供 OnValueChangedAsObservable
  public void Show(); public void Hide();
WitchTrials.Views.WitchTrialsButton : GigaCreation.Essentials.UI.RebuildableButton (: MonoBehaviour ! 不是 UI.Button)
  public Canvas _canvas;  public GraphicRaycaster _graphicRaycaster;  public Vector2 _defaultSize;  public Vector2 DefaultSize { get; }
  public void RebuildElements();  public void GetReferences();  public void Show(); public void Hide();
  // RebuildableButton 内部真正的按钮是 UnityEngine.UI.Button _uiButton
WitchTrials.Views.WitchTrialsButtonLabel : RebuildableLabel   // Color InitialHtmlRgb/BodyHtmlRgb/DefaultLabelColor；ModifyEmphasizedText(...)
WitchTrials.Views.WitchTrialsLogUi : GigaCreation.NaninovelExtender.Ui.BacklogPanelModified (: Naninovel.UI.BacklogPanel)
  public AuthorTextBuilder.Settings _authorTextSettings;  public AuthorTextBuilder _authorTextBuilder;
  public override UniTask Initialize();  public override void SpawnMessage(BacklogMessage message);   // ← 每条日志落地
WitchTrials.Views.WitchTrialsLogMessageUi : Naninovel.BacklogMessageUI
  public CanvasGroup _authorPanelCanvasGroup;  public TMP_Text _authorLabel;  public string _authorId;
  public override string AuthorId;  public override GameObject AuthorPanel;
  public override void SetAuthor(string authorId);  public void ModifyAuthorPanel(AuthorTextBuilder builder);
```

日志基类实查（E1）：`Naninovel.UI.BacklogPanel.Messages: List<BacklogMessage>`；`Naninovel.UI.BacklogMessage{ LocalizableText text; string author; PlaybackSpot spot; string[] voice; }`（属性 `Text/Authored/AuthorId/HasRollback/RollbackSpot/Voice`）；`Naninovel.BacklogMessageUI.Text: LocalizableText`、`onMessageChanged`、`playVoiceButton`、`rollbackButton`。→ **历史日志朗读**：读 `BacklogPanel.Messages[i].Text`（本地化求值）+ `.AuthorId`；新日志：hook `SpawnMessage(BacklogMessage)`。

### 3.7 开合动画机制（必查结论，E1+E2）

| 层级 | 机制 | 证据 |
|---|---|---|
| 手册整屏 | `NaninovelScreen.Present/Dismiss`（UniTask）+ `BeginToPresent/EndToPresent/BeginToDismiss/EndToDismiss` 生命周期回调 + `_customUI` 显隐 + `SetCanvasesEnabled` / `SetGraphicRaycastersEnabled` / `EnableCancelEvent` / `DisableCancelEvent` / `Cancel` / `IsCancelEventBlocked` | E1（`NaninovelScreen : MonoBehaviour` 实查） |
| 页面 | `WitchBookPageBase.Show(bool asPresentEvidenceMode)` / `Hide()`（直接方法，非 tween 参数） | E1 |
| 条目列表 | `WitchBookItemList.ExecuteRebuildingAsync()` + `_pagination` | E1 |
| 条目显隐/缩略图 | `CanvasGroup`（`WitchBookItemButton._canvasGroup`、`WitchBookItemThumbnail._canvasGroup`）→ **alpha 驱动** | E1 字段 |
| 页签高亮 | `WitchTrialsTabToggle` 的 `CanvasGroup _defaultLabel/_highlightedLabel` 切换 + `Image _inactiveBase/_activeBase` 换图 → **alpha + sprite 切换** | E1 字段 |
| 关闭按钮 | `WitchBookScreen._closeButtonCanvasGroup` → alpha | E1 字段 |

结论：**手册顶层不是简单的 `SetActive`**——它走 `NaninovelScreen` 的 Present/Dismiss 状态机（带 UnityEvent 回调），页面/条目才用 `SetActive` + `CanvasGroup.alpha`。读屏若靠轮询 `activeInHierarchy` 判断"手册是否打开"会漏（子页面 Canvas 常开）；应改用 `WitchBookScreen.BeginToPresent/EndToPresent` 或 `WitchBookUi` 的 `HandleVisibilityChanged` / `SerializeState`。

### 3.8 地图（E1）

```csharp
MapFloor : MonoBehaviour  { Canvas _canvas; GraphicRaycaster _graphicRaycaster; MapArea[] _mapAreas; IdVersionPair _idAndVersion; MapArea[] Areas { get; }
                            Reset(); Show(); Hide(); bool IsMatch(...) }
MapArea  : MonoBehaviour  { MapAreaFocusButton _focusButton; MapAreaLabel _areaLabel; MapAreaKind _areaKind; MapAreaKind AreaKind { get; }
                            Reset(); Show(); Hide(); ChangeFocusButtonActivity(bool); UniTask OnClickAsync(...); ModifyInEditor() }
MapAreaFocusButton : ButtonExtendedBase (: Button) { CanvasGroup _canvasGroup; Awake(); Enable(); Disable(); OnHighlightingChanged(); OnEnablingChanged() }
MapAreaLabel : MonoBehaviour { CanvasGroup _canvasGroup; TextMeshProUGUI _label; Image[] _labelPlateImages; Show(); Hide(); }
MapAreaModifier : MonoBehaviour { MapArea[] _mapAreas; float[] _labelMaxWidths; bool _showFocusButton; }
MapAreaKind 枚举（E1 实查 29 值）：EntranceHall, CourtroomHallway, MessHall, Kitchen, Courtroom, Parlor, Infirmary, StorageRoom, Restroom, Sunroom, Showers, Courtyard, PrisonEntrance, FrontGuesthouse, ChamberSalamander, ChamberUndine, ChamberGnome, Warehouse, Wall, Lake, FlowerFields, SecondFloorHallway, Library, RecreationRoom, UnknownArea, BasementHallway, SolitaryConfinement, Incinerator, NoahStudio
```

> 地图区域按钮 = `MapAreaFocusButton`（继承 `ButtonExtendedBase`，有 `OnHighlightingChanged/OnEnablingChanged`），区域名 = `MapAreaLabel._label`（`TextMeshProUGUI`，靠 `_canvasGroup` alpha 显隐）。朗读区域名优先取本地化文本或 `MapAreaKind` 枚举名（需本地化映射，**待探针**）。

### 3.9 手册相关命令（E1）

| 类型 | 基类 | 参数 | 用途 |
|---|---|---|---|
| `ShowWitchBookUi` | `Command` | `CategoryNames: StringListParameter`、`Back: BooleanParameter`、`Global: BooleanParameter` + 静态 `EnableDebugSkipVarName` | 打开手册（可指定类别） |
| `UpdateWitchBook` | `Command` | `Id: StringParameter`、`Category: StringParameter`、`Version: IntegerParameter` | 解锁/更新条目 |
| `ClearWitchBook` | `Command` | `CategoryNames: StringListParameter` | 清空手册状态 |
| `GosubResetWitchBook` | `Naninovel.Commands.Gosub` | `Version: IntegerParameter`、`IsDebugDirectJump: BooleanParameter`；静态 `Dictionary<WitchBookCategory, IdVersionPair[]> s_prevEvidences`；方法 `TryGetScriptPathAndLabel`、`IsWitchBookStateUpdated`、`IsCategoryUpdated`、`ExportScriptFileAsync`、`ResetStaticFields` | 重置并跳到剧本标签（重开章节场景用） |
| `ChoiceEvidence` / `ChoiceMapArea` | `ShowWitchBookUi` | 见 2.4 | 证据/区域选择 |

---

## 4. 标题与系统 UI（必查项 4）

### 4.1 `TitleUi : Naninovel.UI.CustomUI`（E1）

```csharp
public static string StartGameLabelName, QuitGameLabelName, GameProgressVarName, NextScenarioVarName;
public unsafe Button _loadGameButton, _newGameButton, _galleryButton, _optionsButton, _exitButton;
public unsafe GoogleForm _googleForm;
public unsafe DialogData _confirmToQuitGameDialogData;
public unsafe Ease _easingTypeOnShow, _easingTypeOnHide;
public unsafe string _startGameSfxPath;  public unsafe Tweener _fadeTweener;
public unsafe CompositeDisposable _disposables;
public unsafe IInputManager _inputManager;  public unsafe IDialogProvider _dialogProvider;
public unsafe IWitchTrialsNaninovelManager _witchTrialsNaninovelManager;
public unsafe ICustomVariableManager _naniVarManager;
public unsafe IAudioManager _naniAudioManager;  public unsafe IScriptPlayer _naniScriptPlayer;  public unsafe IUIManager _naniUiManager;
public unsafe SaveLoadUi _saveLoadUi;  public unsafe GalleryUi _galleryUi;  public unsafe OptionsUi _optionsUi;
public unsafe GameObject _visibleSelf;  public bool Visible { get; }
public bool CanLoadGame { get; }                  // ← 是否有存档可读（"继续游戏"可用性）
public override UniTask ChangeVisibility(bool visible, Il2CppSystem.Nullable<float> duration = null, AsyncToken asyncToken = null);
public void SetVisibility(bool visible, ...);  public void OnBeginToPresentEvent();
public void Activate();  public void Deactivate();
public UniTask LoadGameAsync();  public void StartGame();  public UniTask QuitGameAsync();
```

> 标题菜单：5 个 `UnityEngine.UI.Button` + 二段确认对话框。`CanLoadGame` 控制"继续游戏"是否可用。`_visibleSelf` 说明**根对象常驻**，靠 `_fadeTweener` + 子对象切换显隐（E2）。

### 4.2 `ControlPanel : MonoBehaviour`（暂停菜单，E1）

```csharp
public static Sprite s_blink;
public string _returnToTitleScript;
public CanvasGroup _menuCanvasGroup;  public CanvasGroup _homeCanvasGroup;
public Button _logButton, _saveButton, _loadButton, _optionsButton, _titleButton;
public Button _openButton;  public CanvasGroup _openButtonCanvasGroup;
public Button _closeButton;  public CanvasGroup _closeButtonCanvasGroup;
public HintDisplay _hintDisplay;  public Animator _naviAnimator;  public Button _rayBlocker;
public DialogData _returnToTitleDialogData;  public string _openSfxPath, _closeSfxPath;
public IInputManager _inputManager;  public IDialogProvider _dialogProvider;  public IInputManager _naniInputManager;
public IAudioManager _naniAudioManager;  public IUIManager _naniUiManager;  public IScriptPlayer _naniScriptPlayer;
public WitchTrialsLogUi _logUi;  public SaveLoadUi _saveLoadUi;  public OptionsUi _optionsUi;  public bool _isOpened;
public void Start();  public void OnEnable();  public void OnDisable();
public UniTask ReturnToTitleAsync(CancellationToken ct = null);
public void Open();  public void Close();  public void ToggleVisibility();  public void OnShow();
```

`_rayBlocker` 是 `Button`（拦截点击）；`_naviAnimator` 是导航角色动画（E2）。`_Start_b__47_0..8` 共 9 个闭包 → Start 里给 5 个按钮 + hint/log 等订阅点击（E2）。

### 4.3 `OptionsUi : CustomUI`（设置，E1）

```csharp
// 消息/选项页
LabeledSlider _messageSpeedSlider, _autoDelaySlider;
ToggleGroup _skipModeToggleGroup, _toggleSkipModeToggleGroup, _criticalChoiceHintToggleGroup;
Dropdown _languageDropdown;  RebuildableButton _resetMessageOptionsButton;
// 画面页
ToggleGroup _screenModeToggleGroup;  Dropdown _resolutionDropdown, _frameRateDropdown;
ToggleGroup _postProcessingToggleGroup;  RebuildableButton _resetGraphicOptionsButton;
// 声音页
LabeledSlider _masterVolumeSlider, _bgmVolumeSlider, _sfxVolumeSlider, _voiceVolumeSlider;
RebuildableButton _resetAudioOptionsButton;
// 分页
RebuildableButton[] _pages;  int _unavailableResolutionOptionCount;
public override UniTask Initialize();  public override void HandleVisibilityChanged(bool visible);
public void ActivateUi();  public void Dispose();
public void ApplyOptionValuesToUi();                 // ← 值 → UI 的同步点（朗读设置项）
public UniTask ChangeLocaleAsync(...);  public void Save();  public UniTask SaveAsync();
```

> 设置项没有任何 TMP 标签字段（标签在 `LabeledSlider` 等 Giga 组件内）→ 朗读设置项需要读 `LabeledSlider` 自己的 label（Giga 类型，未展开）→ **待补**（见第 8 节）。

### 4.4 `SaveLoadUi : CustomUI`（存读档，E1）

```csharp
public unsafe NaninovelScreen _naninovelScreen;  public unsafe SaveLoadPanel[] _panels;
public float _fadeOutDurationOnLoad, _fadeInDurationOnLoad;  public Color _fadeColorOnLoad;
public int _saveGameTimeout;  public float _loadDelay;  public string _autoSaveTabName;
public string _saveSfxPath, _loadSfxPath;
public DialogData _overwriteSaveSlotDialogData, _deleteSaveSlotDialogData;
public IInputManager _inputManager;  public IOverlayFader _overlayFader;  public IDialogProvider _dialogProvider;
public IWitchTrialsNaninovelManager _witchTrialsNaninovelManager;  public IStateManager _naniStateManager;
public IAudioManager _naniAudioManager;  public IUIManager _naniUiManager;
public SaveLoadMode _presentationMode;  public bool _autoLoadMode;
public int _previousPageIndex, _currentPageIndex, _loadingSlotCount;
public string[] _autoSaveSlotNames;
public void Awake();  public override UniTask Initialize();
public SaveLoadMode GetLastLoadMode();
public void SetPresentationMode(...);
public void HandleSaveSlotClicked(...);  public UniTask HandleSaveSlotClickedAsync(...);
public void HandleLoadSlotClicked(...);  public UniTask HandleLoadSlotClickedAsync(...);
public void HandleDeleteSlotClicked(...); public UniTask HandleDeleteSlotClickedAsync(...);
public UniTask LoadSaveSlotAsync(...);  public bool IsAutoSaveSlot(...);  public int GetSlotIdFromIndex(...);
public void OnShow();  public void OnHide();  public void OnEndToDismiss();
public void PrePageChanged(...);  public override GameObject FindFocusObject();
```

存档槽：`WitchTrials.Views.WitchTrialsGameStateSlot : GigaCreation.Essentials.UI.GameStateSlotExtended (: GameStateSlot)`
字段 `_chapterData`、`_localizationProvider`、`_currentChapter`、`_localizedChapterNames`；方法 `Awake`、`SetEmptyState`、`SetNonEmptyState`、`BuildSubTitleText`、`HandleLocaleChanged`、`GetLocalizedSubTitle`。
基类 `GameStateSlotExtended`（E1）：`CanvasGroup _thumbnailCanvasGroup/_loadingIconCanvasGroup`、`Transform _loadingIconTransform`、`TMP_Text _noDataLabel/_subTitleLabel`、`float _fadeDuration/_rollDuration`、`string _autoSaveSlotPrefix`、`FadeMotion _fadeMotion`、`RollMotion _rollMotion`、`bool _isAutoSaveSlot`；`SetEmptyState`、`SetNonEmptyState`、`BuildSlotNumberText`、`BuildSubTitleText`、`SetSubTitleText`、`OnBeginToLoad`、`OnEndToLoad`、`SetRoll`、`CancelAllMotions`。
→ **空槽/有槽 = `SetEmptyState/SetNonEmptyState` + CanvasGroup alpha**（不是 SetActive）；槽文本 = `_subTitleLabel`。

### 4.5 其余系统 UI（E1）

| 类型 | 基类/关键点 | A11Y 要点 |
|---|---|---|
| `GalleryUi` | `Naninovel.UI.CGGalleryPanel`；`_cgSourcePaths`、`IResourceProviderManager _naniResourceProviderManager`、`ILocalizationManager _naniLocalizationManager`、`IUnlockableManager _naniUnlockableManager`、`IStateManager _naniStateManager`；`UpdateCgSourcePaths`、`UnlockAllStills`、`UnlockAllStillsAsync`、`LocateAsync` | 图廊 = 网格 `StillGrid : CGGalleryGrid`、槽 `GalleryStillGridSlot : CGGalleryGridSlot`（`_cgTextures`、`LoadCGTextures`、`HandleItemUpdated`）、查看器 `GalleryStillViewerPanel : CGViewerPanel`（`Show/Hide/ShowNextTexture`）→ 键盘翻页需自建 |
| `SoundTestUi` | `CustomUI`；`TMP_Text _titleLabel/_kindLabel/_timeLabel`、`CanvasGroup _playerSliderCanvasGroup`、`Slider _playerSlider`、`_fillImage/_handleImage`、`_introColor/_loopColor`；`ModifyPlayer`、`ResetPlayer`、`UniTask UpdatePlayerAsync`、`SetValueWithoutNotify`、`GetTimeSpan` | 曲名/类型/时间都是 TMP → 可直接朗读；播放进度靠 `UpdatePlayerAsync` 轮询 |
| `ExecutionButtonUi` | `CustomUI`，只有字段 `ExecutionButtonScreen _executionButtonScreen` | — |
| `ExecutionButtonScreen` | `MonoBehaviour`；`static string ExecutionConfirmedLabelName`；`Button _button`、`CanvasGroup _checkIcon`、`_blood`、`float _scaleOnPressed/_scaleDuration/_increaseSpeed/_decreaseSpeed/_delayAfterFullFill`、`string _chargingSfx/_completeSfx`、`CompositeDisposable _disposables`、`bool _pressed`、`CancellationTokenSource _cts`；`Awake/Ready/Dispose/PressAsync/ReleaseAsync/OnFullFillAsync` | **"长按处刑"型交互**（按住 → 蓄满 → 确认），键盘/读屏必须提供替代（例：Enter 长按或开关） |
| `CreditsUI` | `CustomUI`；`CreditsDirectorBase[] _creditsDirectors`、`_currentDirector`、`_cts`；`Initialize`、`PreloadResourcesAsync`、`PlayAsync`、`Stop` | 演职员表自动滚动（`CreditRoll`/`CreditRollVerticalScroll`/`CreditRollSpecialThanks`/`CreditsDirectorAct1/2`）→ 读屏意义低，**不建议朗读**（大量文本、非交互） |
| `HintDisplay` | `MonoBehaviour`；`_hintMessages`、`TextMeshProUGUI _messageLabel`、`CanvasGroup _onMenuCanvasGroup/_enabledCanvasGroup`、`_hintExistedNotice`、`_hintApp`、`Button _screenButton/_closeButton/_wordPrinterButton/_witchButton`、`_waitOnChara`、`_waitSkip`、`CharacterWait[] _charaWaits`、`Dictionary<char,float> _characterWaitDictionary`、`_requestMessage`、`_selectedButtonKind`、`_cts`、`OnClose`；`OnEnable/OnDestroy/OnShow/Present/RequestNextMessage/Close/Dismiss/Initialize/MainLoopAsync/ShowTextAsync/WaitContinueOrBreakAsync/OnClickAsObservable` | 提示面板：`ShowTextAsync(string request, CancellationToken)` 是**唯一文本入口**（读它即可朗读）；`WaitContinueOrBreakAsync` 等待玩家继续 → 键盘回调 |
| `AutoToggleUi` | `CustomUI`；`Toggle _toggle`；`OnEnable/OnDisable/Initialize/HandleAutoModeChanged` | 自动播放开关；状态在 Toggle |
| `LoadingIndicatorRolling` | `GigaCreation.Essentials.UI.LoadingIndicatorBase`（`MonoBehaviour`，`CanvasGroup _canvasGroup`、`SetOpacity/OnShow/OnHide`）；`_transform`、`_duration`、`_ease`、`_delay`、`_cts`；`ShowAsync`、`SetRoll` | 加载指示：`OnShow/OnHide` → 可播报"加载中" |
| `AutoSave` / `QuickSave` / `QuickLoad` / `ReturnToTitle` / `ProcessBadEnd` / `SetBadId` / `ShowLoadUi` / `ShowCreditsUi` / `HideCreditsUi` | 均为命令（见第 5 节） | 存档/读档/回标题/坏结局流程入口 |

### 4.6 可用/不可用状态与焦点体系（对读屏是**核心**，E1）

- **标准做法**：`UnityEngine.UI.Selectable.interactable` + `CanvasGroup.alpha`。
- Giga 提供的过渡组件（E1 实查）：
  - `CanvasGroupButton : Button`，字段 `_normal/_highlighted/_disabled: CanvasGroup`、`_ignoreParentDisabling`；`DoStateTransition`、`OnStateChanged` → **禁用=alpha 关掉某组**。
  - `TweeningButton : ButtonExtendedBase`，`_scaleUpWhenHighlighted`、`_fadeOutWhenDisabled`、`_scaleUtil`、`_fadeUtil`；`OnHighlightingChanged`、`OnEnablingChanged`。
  - `ButtonExtendedBase : Button`，`_disposables`、`_selectableUtil`、`SelectableUtil`；`Initialize`、`DoStateTransition`、`OnHighlightingChanged`、`OnEnablingChanged`。
  - `MapAreaFocusButton : ButtonExtendedBase`，`_canvasGroup`；`Enable()/Disable()`。
- **Naninovel `CustomUI` 自带焦点体系**（E1，所有本作 CustomUI 都继承它）：

```csharp
public List<SelectableData> selectables;        // SelectableData { Selectable Selectable; Navigation Navigation; }
public IReadOnlyList<SelectableData> Selectables { get; }
public override GameObject FocusObject { get; } // 当前应聚焦对象
public virtual bool IsFocusInside();
public virtual GameObject FindFocusObject();
public bool hideOnLoad / hideInThumbnail / saveVisibilityState / blockInputWhenVisible / adaptToInputMode / modalUI;
public string modalGroup;
public UniTask Initialize();
public override void HandleVisibilityChanged(bool visible);
public void BindInput(string path, Action action, Il2CppSystem.Nullable<BindInputOptions> options);
public void BindInput(string path, Action<float> action, Il2CppSystem.Nullable<BindInputChangeOptions> options);
```

→ **A11Y 结论**：读屏不需要自己实现焦点系统；应复用 `CustomUI.Selectables` / `FocusObject` / `IsFocusInside`，并在 `ButtonExtendedBase.OnHighlightingChanged`（或 `Selectable` 的 `Select` 事件）上播报。`BindInput(path, action)` 是 Naninovel 的输入绑定入口，可接键盘快捷键（需确认 action path 名，见 `WitchTrialsActions`）。

---

## 5. 自定义 Naninovel 命令一览（必查项 5）

> **别名不可静态恢复**（第 0 节限制 2）。下表"推断别名"全部为 **E3**：按 Naninovel 约定由类名推出，**必须在剧本资源或运行时确认**后才可在文档/代码里使用。

### 5.1 文本/辩论

| 类型 | 基类 | 参数（E1） | 推断别名(E3) |
|---|---|---|---|
| `PrintDebateText` | `PrintText` | `ScenePosition: DecimalListParameter`、`Roll: DecimalParameter`、`AnimationKind: StringParameter`（+ 继承 `Text/PrinterId/AuthorLabel/RevealSpeed/ResetPrinter/WaitForInput/Wait…`） | `printDebate` |
| `PrintTextModified` | `PrintText` | 无自有参数；仅覆写 `ShouldPlayAutoVoice()` | `printTextModified` |
| `PlaySfxModified` | `PlaySfx` | 无自有参数；覆写 `Play`、`PlayOrModifyTrack` | `playSfxModified` |
| `UpdateAudioPlayerUi` | `Command` | `TrackName: StringParameter`；`ISoundTestUi _soundTestUi` | `updateAudioPlayerUi` |

### 5.2 模式/审判/计时

| 类型 | 基类 | 参数 | 推断别名 |
|---|---|---|---|
| `ChangeDebateMode` | `Command` | `Enable: BooleanParameter` | `changeDebateMode` |
| `ChangeTrialMode` | `Command` | `Enable: BooleanParameter` | `changeTrialMode` |
| `ChangeTrialPauseMode` | `Command` | `Enable: BooleanParameter`、`ProcessTimer: BooleanParameter` | `changeTrialPauseMode` |
| `SetTrialTimer` | `Command` | `Time: DecimalParameter` | `setTrialTimer` |
| `Damage` | `Command` | （实查无参数） | `damage` |
| `ChangeCourtFireFxActivity` | `Command` | `Enable`(bool 字段) | `changeCourtFireFxActivity` |

### 5.3 选项/手册

| 类型 | 基类 | 参数 | 推断别名 |
|---|---|---|---|
| `StopForChoice` | `Stop` | `Clear: BooleanParameter` | `stopForChoice` |
| `RecordCorrectChoice` | `SetCustomVariableAsFlag` | （flag 值来自基类；静态 `PlayedCorrectChoiceVarName`） | `recordCorrectChoice` |
| `ChoiceEvidence` | `ShowWitchBookUi` | `CorrectEvidenceId: StringParameter`（**剧透！**） | `choiceEvidence` |
| `ChoiceMapArea` | `ShowWitchBookUi` | `MapAreaNames: StringListParameter`、`FlagVariableName: StringParameter` | `choiceMapArea` |
| `ShowWitchBookUi` | `Command` | `CategoryNames: StringListParameter`、`Back: BooleanParameter`、`Global: BooleanParameter` | `showWitchBook` |
| `UpdateWitchBook` | `Command` | `Id: StringParameter`、`Category: StringParameter`、`Version: IntegerParameter` | `updateWitchBook` |
| `ClearWitchBook` | `Command` | `CategoryNames: StringListParameter` | `clearWitchBook` |
| `GosubResetWitchBook` | `Gosub` | `Version: IntegerParameter`、`IsDebugDirectJump: BooleanParameter` | `gosubResetWitchBook` |

### 5.4 站位/舞台/演出（纯表现，A11Y 一般忽略）

| 类型 | 基类 | 参数 |
|---|---|---|
| `GosubToModifyPerspectiveStageCamera` | `Command` | `Index: IntegerParameter`、`LookCharacterId: StringParameter`、`Composition: StringParameter`、`RollDirection: StringParameter`、`ZoomLevel: IntegerParameter`、`LabelNameParams: StringListParameter` |
| `GosubToSplitPerspectiveStageScreens` | `Command` | 同上（`ZoomLevel` 无） |
| `ModifyPerspectiveStageCamera` | `Command` | `Index`、`Height`、`Rotation`、`Distance`、`Duration`、`EaseName`、`Wait` |
| `ShakePerspectiveStageCamera` | `Command` | `Index`、`Force`、`Frequency`、`DampingRatio`、`Randomly`、`Duration`、`Wait` |
| `LoadPerspectiveStage` / `UnloadPerspectiveStage` | `Command` | `Id`、`Wait` / 无 |
| `ShowPerspectiveStageScreen` | `Command` | `Index`、`Duration`、`Wait` |
| `HidePerspectiveStageScreen` | `Command` | `Index`、`Duration`、`Reset`、`Wait` |
| `SplitPerspectiveStageScreen` | `Command` | `Index`、`ScenePositionX`、`Roll`、`SplitDirection` |
| `GosubToObjectionCutIn` | `AudioCommand` | `CutInKind`、`Index`、`VoicePath`、`Volume`、`GroupPath`、`AuthorId` |
| `CancelGosub` | `Command` | 无 |
| `EmoteFall` | `AnimateActorExtended` | `_scenePositionX`、`_scenePositionY`、`_rotation`、`_duration`、`_easing` |
| `EmoteJump` / `EmoteJumpDouble` / `EmoteNod` / `EmoteShake` | `AnimateActorExtended` | 位置/时长/缓动（`EmoteShake` 为 X + duration） |

### 5.5 存档/流程

| 类型 | 基类 | 参数 |
|---|---|---|
| `AutoSave` | `Command` | `DelayDuration: DecimalParameter`、`Wait: BooleanParameter` |
| `QuickSave` | `Command` | `SlotNumber: IntegerParameter`、`Wait: BooleanParameter`（静态 `LastQuickSaveSlotNumberVarName`） |
| `QuickLoad` | `Command` | `SlotNumber: IntegerParameter`、`Wait: BooleanParameter`（方法 `TryGetSlotNumber`） |
| `ShowLoadUi` | `Command` | `AutoSave: BooleanParameter`、`Wait: BooleanParameter` |
| `ReturnToTitle` | `Stop` | `FadeDuration: DecimalParameter`、`MinDelayDuration: DecimalParameter`、`Wait: BooleanParameter` |
| `ProcessBadEnd` | `SetCustomVariableAsFlag` | `BadEndScriptName`、`PlayedBadEndVarName`、`CurrentBadEndIdVarName`、`BadEndPartKind: StringParameter` |
| `SetBadId` | `Command` | `CommonBadEndId: IntegerParameter`（静态 `CommonBadEndIdVarName`） |
| `ShowCreditsUi` | `Command` | `ActNumber: IntegerParameter`、`Wait: BooleanParameter` |
| `HideCreditsUi` | `Command` | 无 |

### 5.6 调试（`WitchTrials.Debugging`，正式版是否可用待探针）

| 类型 | 基类 | 参数 |
|---|---|---|
| `BlockInteraction` | `Command` | `Duration: DecimalParameter`、`Wait: BooleanParameter`（`Execute`/`BlockAsync`） |
| `ShowLoadingUiForDebug` | `Command` | `LoadingMode: StringParameter`、`Duration`、`HaltInput`、`Wait` |

`DebugCommandButtons : MonoBehaviour`（调试面板按钮，字段 `EnableDebugSkipVarName/AllowContinueDebateVarName/_togglePauseButton/_slowDownButton/_speedUpButton/_forceTimeUpButton/_unlockAllStillsButton/_toggleDebugSkipButton/...`）→ **`_forceTimeUpButton` / `_unlockAllStillsButton` 是剧透/作弊入口，A11Y 模组不应暴露**。

---

## 6. 与 Naninovel 标准 UI 的关系

| 本作类型 | 标准基类 | 关系 |
|---|---|---|
| `WitchTrialsTextPrinterPanel` | `RevealableTextPrinterPanel`（经 Giga `RevealableTextPrinterPanelModified`） | 普通台词打印机，**标准机制完整保留** → 优先用标准钩子 |
| `DebateTextPrinter` / `DebateTextPrinterPanel` | `UITextPrinter` / `UITextPrinterPanel` | 辩论专用旁路：自建多行 label 池，**没有** RevealableText 的逐字/UnityEvent |
| `TrialChoiceHandlerPanel` / `TrialChoiceHandlerButton` | `ChoiceHandlerPanel` / `ChoiceHandlerButton`（经 Giga `ChoiceHandlerPanelModified`） | 完全走标准选项系统，只加 prefab 映射表 |
| `WitchTrialsLogUi` / `WitchTrialsLogMessageUi` | `BacklogPanel`（Giga 改）/ `BacklogMessageUI` | 标准日志 + 作者名扩展 |
| `WitchBookUi` | `CustomUI` | 标准 UI 生命周期 + **自定义存档状态**（`SerializeState/DeserializeState`） |
| `WitchBookScreen` | Giga `NaninovelScreen` | 自定义"屏幕"抽象：Present/Dismiss 状态机 + UnityEvent |
| `TitleUi` / `OptionsUi` / `SaveLoadUi` / `SoundTestUi` / `AutoToggleUi` / `ExecutionButtonUi` / `CreditsUI` | `CustomUI` | 全部走 Naninovel UI 管理器（`IUIManager.GetUI<T>()`） |
| `GalleryUi` | `CGGalleryPanel` | 标准 CG 图廊 |

**取实例的正确姿势（待探针验证）**：`Engine.GetService<IUIManager>()` + `GetUI<T>()`（Naninovel 惯例），或 hook `Initialize()` 缓存。**不建议**用 `FindObjectOfType`（IL2CPP 下开销大且不可靠）。

---

## 7. Harmony 补丁点推荐表

> 时机列：`Pre`=Prefix、`Post`=Postfix、`Sub`=订阅事件（非补丁）。**参数类型按 interop 写法**（字符串为 `Il2CppSystem.String`，数组为 `Il2CppReferenceArray<T>`，`List` 为 `Il2CppSystem.Collections.Generic.List<T>`）。优先级 P0=读屏必需、P1=重要、P2=可选。

| # | Hook 目标（完整类型 + 方法） | 时机 | 参数（interop） | 用途 | 优先级 |
|---|---|---|---|---|---|
| 1 | `WitchTrials.Views.DebateTextPrinterPanel.AddText` | Post | `Il2CppSystem.String text` | 辩论加行 → 朗读整行（**首选**） | P0 |
| 2 | `WitchTrials.Views.DebateTextPrinterPanel.SetText` | Post | `Il2CppSystem.String text` | 辩论整段替换 → 清空缓冲并朗读 | P0 |
| 3 | `WitchTrials.Views.DebateTextPrinterPanel.SetMessages` | Post | `IReadOnlyList<PrintedMessage>` | 辩论重置 → 清空朗读缓冲 | P1 |
| 4 | `WitchTrials.Views.DebateTextPrinterPanel.AddMessage` | Post | `PrintedMessage` | 结构化消息（若走标准路径） | P1 |
| 5 | `WitchTrials.Views.DebateText.OnLinkClicked` | Pre/Post | `TMP_LinkInfo` | 点击证据链接 → 播报跳转目标（可阻止误触） | P1 |
| 6 | `WitchTrials.Views.DebateTextPrinterPanel.Gosub` | Pre | `Il2CppSystem.String id` | 拦截 link → Gosub（记录证据跳转，勿朗读剧透内容） | P2 |
| 7 | `Naninovel.UI.RevealableTextPrinterPanel.HandleCharRevealed` | Post | `char character`, `AsyncToken token` | 普通台词逐字事件 | P0 |
| 8 | `Naninovel.UI.RevealableTextPrinterPanel.RevealMessages` | Post | `float delay`, `AsyncToken token` | 整句揭示开始/结束（配合 `onRevealFinished`） | P0 |
| 9 | `Naninovel.UI.RevealableTextPrinterPanel.FormatMessage` | Post | `PrintedMessage message`, `bool adjacent` | 取纯净文本（剥标签后的） | P0 |
| 10 | `Naninovel.UI.ChoiceHandlerPanel.AddChoiceButton` | Post | `ChoiceState choice` | 新选项 → 朗读选项文本 + 计数 | P0 |
| 11 | `Naninovel.UI.ChoiceHandlerPanel.RemoveChoiceButton` / `RemoveAllChoiceButtons` | Post | `Il2CppSystem.String id` | 选项消失 → 同步读屏缓冲 | P1 |
| 12 | `Naninovel.UI.ChoiceHandlerPanel.FocusAddedButton` / `ShouldFocusAddedButton` | Post | `ChoiceHandlerButton button` | 焦点同步（键盘可达性） | P0 |
| 13 | `Naninovel.UI.ChoiceHandlerButton.Initialize` | Post | `ChoiceState choiceState` | 按钮 ↔ ChoiceState 映射缓存 | P1 |
| 14 | `Naninovel.UI.ChoiceHandlerButton.HandleLockChanged` | Post | `bool locked` | 播报"不可选" | P1 |
| 15 | `WitchTrials.Views.AdvChoiceHandlerButton.Initialize` / `ChangeCriticalChoiceHintVisibility` | Post | `ChoiceState` / `bool enable` | 关键选项提示状态（可读屏播报"这是个关键选项"） | P1 |
| 16 | `WitchTrials.Views.StopForChoice.Execute` | Post | `AsyncToken token` | 播报"请选择"（进入选择态） | P0 |
| 17 | `WitchTrials.Views.ChoiceEvidence.Execute` | **不 Hook**（只做进入/退出标记） | — | 绝不读 `CorrectEvidenceId`（剧透红线） | — |
| 18 | `WitchTrials.Views.DebateUi.OnCurrentHpChanged` | Post | `float currentHp` | HP 变化播报 | P0 |
| 19 | `WitchTrials.Views.DebateUi.ChangeVisibility` | Post | `bool`, `Nullable<float>`, `AsyncToken` | 辩论 HUD 显隐 | P1 |
| 20 | `WitchTrials.Views.DebateUi.TimeSpanToText` | Post | `TimeSpan` | 取计时文本（复用其格式化） | P1 |
| 21 | `WitchTrials.Models.Timer.ForceRemainingTimeToNearlyZero` | Pre | — | 超时瞬间提示（读屏播报"时间到"） | P1 |
| 22 | `WitchTrials.Views.WitchBookScreen.BeginToPresent` / `EndToPresent` / `BeginToDismiss` / `EndToDismiss` | Post | — | 手册开合播报 + 焦点切换 | P0 |
| 23 | `WitchTrials.Views.WitchBookScreen.ChangePage` | Post | `WitchBookCategory category` | 页签切换播报（"线索页/档案页/地图/规则/笔记"） | P0 |
| 24 | `WitchTrials.Views.WitchBookPageBase.Show` / `Hide` | Post | `bool asPresentEvidenceMode` / — | 页面级显隐 + 列表就绪 | P1 |
| 25 | `WitchTrials.Views.WitchBookPageBase.InformAt` | Post | `int idx` | 当前条目索引（键盘上下移动播报） | P0 |
| 26 | `WitchTrials.Views.WitchBookItemButton.SetupWithText` | Post | `Il2CppSystem.String text`, `int backgroundIndex` | 缓存条目文本（读屏内容来源） | P0 |
| 27 | `WitchTrials.Views.WitchBookItemButton.OnChoiceAsObservable` | Sub | — | 订阅条目点击（返回 `IObservable<Unit>`） | P1 |
| 28 | `WitchTrials.Views.WitchBookPageBase.OnPresentEvidenceAsync` / `WitchBookScreen.ChoiceEvidenceAsync` | Pre/Post | `IReadOnlyList<WitchBookCategory>`, `CancellationToken` | "请选择证据"流程 + 返回结果（**结果 id 只用于确认，不预读对错**） | P1 |
| 29 | `WitchTrials.Views.WitchBookScreen.ChoiceAreaAsync` | Post | `CancellationToken` | "请选择地点"返回 `MapAreaKind` | P1 |
| 30 | `WitchTrials.Views.MapAreaFocusButton.OnHighlightingChanged` / `Enable` / `Disable` | Post | — / — / — | 地图区域焦点播报 | P1 |
| 31 | `WitchTrials.Views.TitleUi.ChangeVisibility` / `Activate` / `Deactivate` | Post | `bool, Nullable<float>, AsyncToken` | 标题菜单进入/退出播报 + 焦点 | P0 |
| 32 | `WitchTrials.Views.TitleUi.get_CanLoadGame` | Post | `ref bool __result` | 播报"可继续/无存档" | P1 |
| 33 | `WitchTrials.Views.ControlPanel.Open` / `Close` / `ToggleVisibility` / `OnShow` | Post | — | 暂停菜单播报 + 焦点 | P0 |
| 34 | `WitchTrials.Views.HintDisplay.ShowTextAsync` | Post | `Il2CppSystem.String request`, `CancellationToken` | 提示文本 → 朗读（**唯一入口**） | P0 |
| 35 | `WitchTrials.Views.HintDisplay.Present` / `RequestNextMessage` / `Close` | Post | — | 提示条生命周期 | P1 |
| 36 | `WitchTrials.Views.HintDisplay.WaitContinueOrBreakAsync` | Post | `CancellationToken` → `UniTask<bool>` | 等待继续（键盘 Enter 推进） | P1 |
| 37 | `WitchTrials.Views.WitchTrialsLogUi.SpawnMessage` | Post | `BacklogMessage message` | 新日志朗读；历史读 `BacklogPanel.Messages` | P1 |
| 38 | `WitchTrials.Views.SaveLoadUi.HandleSaveSlotClickedAsync` / `LoadSaveSlotAsync` | Post/Pre | 见签名 | 存档槽操作播报 | P1 |
| 39 | `GigaCreation.Essentials.UI.GameStateSlotExtended.SetEmptyState` / `SetNonEmptyState` | Post | 见签名 | 槽位可用性（空/有档）→ 播报 | P1 |
| 40 | `WitchTrials.Views.OptionsUi.ApplyOptionValuesToUi` | Post | — | 设置项朗读（值 → 文本） | P1 |
| 41 | `WitchTrials.Views.OptionsUi.ActivateUi` | Post | — | 设置页进入 + 焦点 | P1 |
| 42 | `WitchTrials.Views.ExecutionButtonScreen.PressAsync` / `ReleaseAsync` / `OnFullFillAsync` | Post | 见签名 | 长按处刑 → 键盘替代 + 进度播报 | P0 |
| 43 | `WitchTrials.Views.SkipDebateButton.Present` / `Dismiss` | Post | — | "可跳过辩论"播报（含键盘替代） | P1 |
| 44 | `WitchTrials.Views.DebateTextPrinterPanel.RevealMessages` | Post | `float delay`, `AsyncToken` | 辩论逐行揭示完成 → 判断"整段读完" | P2 |
| 45 | `WitchTrials.Views.SoundTestUi.UpdatePlayerAsync` | Post | — | 曲名/时间变化播报 | P2 |
| 46 | `WitchTrials.Views.LoadingIndicatorRolling.ShowAsync` / `OnHide` | Post | — | "加载中"播报 | P2 |

**通用注意**：`*Async(AsyncToken token = null)` 是 `Naninovel.UniTask`，Prefix 里不要 `await`；Postfix 拿结果/字段即可。`Il2CppSystem.Nullable<T>` 参数在 Harmony 里按其 interop 类型声明。

---

## 8. 风险与「待探针验证」清单

### 8.1 已验证的硬风险（写代码前必须处理）

| # | 风险 | 影响 | 缓解 |
|---|---|---|---|
| R1 | 辩论文本**不走** `RevealableText`，无逐字/UnityEvent | 若按普通对话方式 hook（`HandleCharRevealed`）会**完全读不到辩论文本** | 必须单独 hook `DebateTextPrinterPanel.AddText/SetText` |
| R2 | `DebateTextPrinterPanel` 用**固定长度 label 数组** `_labels` + `_labelSpaces` | 长台词会跨 label；`AddText` 顺序与显示行可能不一致 | 以 `SetMessages`/`SetText` 为整段重建边界，`AddText` 追加 |
| R3 | 辩论行"停留时长"= 字数换算（`CalculateWaitTimeFromMessage`），与读屏语速无关 | 朗读被打断/跳行 | 需要接管推进（阻止自动前进或排队播报） |
| R4 | `ChoiceState.Summary` 是 `LocalizableText` 不是 `string` | 直接 `.ToString()` 可能得到非本地化/带标签文本 | 用 Naninovel 本地化求值（**待探针**） |
| R5 | `ChoiceEvidence.CorrectEvidenceId` 是正确答案 | **剧透** | 任何日志/播报都不得读该字段；`RecordCorrectChoice` 只作闸门 |
| R6 | `ChoiceState.onSelectScript` 是选中后执行的剧本路径 | 可能含剧透 | 不朗读、不写日志（或只记 id 哈希） |
| R7 | `DebateText.text` / `.unprocessedText` 含 `<link>`/`<ruby>`/`<tip>`/事件标签 | 读屏会读标签名/注音错乱 | 朗读前剥标签；`ProcessText` 后再清洗 |
| R8 | 手册顶层走 `NaninovelScreen` 状态机，非 `SetActive` | 轮询显隐状态会误判 | 用 `BeginToPresent/EndToPresent` 或 `HandleVisibilityChanged` |
| R9 | `WitchTrialsButton` (`_presentEvidenceButton`) 是 `MonoBehaviour` 包装，**不是** `UI.Button` | `TryCast<Button>` 得 null，键盘焦点拿不到 | 取其 `_uiButton` 字段或直接操作 prefab 内 `Button` |
| R10 | `ExecutionButtonScreen` 是**长按蓄力**交互 | 键盘/读屏用户无法完成 | 必须提供替代输入（可配置） |
| R11 | 命令别名/参数别名在 interop 中**被剥离**（实测 `CommandAlias` 命中 0） | 无法确知剧本里 `@xxx` 的真实写法 | 剧本资源实查（textassets 队友）或运行时反射 `Command` 注册表 |
| R12 | `Damage` 命令**无任何参数**（实查） | 伤害数值来源不明（可能固定值/变量） | 探针：辩论中观察 HP 变化量 |
| R13 | `RecordCorrectChoice` / `ProcessBadEnd` 基于自定义变量 flag | 变量可被玩家/模组读取 → 提前暴露结果 | 只做闸门：玩家已选过之后再允许提示 |
| R14 | 大量状态是 `CanvasGroup.alpha`/`Tween` 驱动 | 轮询 `activeSelf` 判断"可见/可用"会错 | 订阅具体回调（表 7 的 Hook 点） |

### 8.2 待探针验证（运行时才能确认）

1. **命令别名**：`PrintDebateText` → `printDebate`？`ShowWitchBookUi` → `showWitchBook`？`GosubToObjectionCutIn` → ?（用 `Naninovel.Command.CommandAliasAttribute` 反射或 `IScriptPlayer` 解析结果确认）。
2. **参数别名**：`ScenePosition` → `pos`？`Roll` → `roll`？`AnimationKind` → `anim`？（同上）
3. **`ChoiceState.Summary` 的求值路径**：读屏应调用哪个 API 才能得到显示用文本（`LocalizableText` 求值）。
4. **`DebateText.label.text` 返回的是否已剥标签**（`ProcessText` 覆写行为）。
5. **单例获取方式**：`Engine.GetService<IUIManager>().GetUI<DebateTextPrinterPanel>()` 是否可用；`DebateTextPrinter` actor 的 `Panel` 关联方式。
6. ~~`HintDisplay.OnClose` 的事件类型~~ → **已解决**：`Il2CppSystem.Action OnClose { get; }`（无参，E1）。
7. ~~`WitchBookItemList._pagination` 类型~~ → **已解决**：`GigaCreation.Essentials...Pagination _pagination { get; }`（`Pagination` 组件，E1）；其分页 API 仍需看 GigaEssentials 源码/探针。
8. **`BacklogPanelModified` 覆写的 `SpawnMessage` 是否改变消息内容**（本作只加作者名，E3）。
9. **`MapAreaKind` → 本地化区域名的映射表**（`MapPage._localizedTextData` 结构）。
10. **`OptionsUi` 各设置项的标签来源**（`LabeledSlider`/`ToggleGroup` 在 GigaEssentials 内，标签可能是本地化 key）。
11. **`Damage` 的实际扣血量**与 HP 变量（`DebateUi.MaxHpVarName`）的读写方式。
12. ~~`WitchBookUi.SerializeState` 的 GameState 结构~~ → **已解决**：只有 `RestrictionCategories: Il2CppStructArray<WitchBookCategory>` 与 `RestrictionMapAreas: Il2CppStructArray<MapAreaKind>`（E1）。
13. **`s_alwaysHandleChoiceIds` 的语义**（`StopForChoice` 中白名单用途）。
14. **TMP 富文本剥离规则**：`<link>`、`<ruby>`、`<tip>`、`<event>`、`<expr>` 五类（`NaninovelTMProText` 的五个 `ProcessXxxTags`）在朗读时的取舍。

### 8.3 未找到 / 需补充的类型

- `Naninovel.BacklogMessageUI` 的**完整成员**：`ilspycmd -t Naninovel.UI.BacklogMessageUI` 只输出 45 字节（类型名不符；正确全名为 `Naninovel.BacklogMessageUI`，已补反编译到 `re/decompiled/Naninovel-types/`），但成员较多未逐条收录 → 若要读屏回放日志需补。
- `LabeledSlider` / `ToggleGroup` / `RebuildableToggle`（GigaEssentials）未展开签名 → `OptionsUi` 朗读方案待补。
- `Pagination`（GigaEssentials，`WitchBookItemList._pagination` 的类型）未展开。
- （`ProcessBadEnd.BadEndPartKind` 不是枚举，而是 `StringParameter` 字段名，E1 实查已确认。）

---

## 9. 附录

### 9.1 复现命令

```powershell
# 全量反编译 Assembly-CSharp.dll（241 个 cs）
ilspycmd -p -o re\decompiled\Assembly-CSharp-proj <interop>\Assembly-CSharp.dll
# 列出全部类型
ilspycmd -l c <interop>\Assembly-CSharp.dll > re\decompiled\witchtrials-types.txt
# 单类型（Naninovel 基类）
ilspycmd -t Naninovel.UI.RevealableTextPrinterPanel <interop>\Elringus.Naninovel.Runtime.dll
# 生成每类型签名清单 + 索引
python re\tools\extract_sig.py re\decompiled\Assembly-CSharp-proj re\decompiled\signatures\witchtrials --index re\decompiled\signatures\witchtrials.index.txt
```

### 9.2 签名清单目录（`re/decompiled/signatures/`）

| 目录 | 文件数 | 内容 |
|---|---|---|
| `witchtrials/` | 143 | `WitchTrials.Views` 全部类型（**本文所有 E1 证据的落地文件**） |
| `witchtrials-models/` | 68 | `WitchTrials.Models`（状态中枢/数据模型/接口） |
| `witchtrials-presenters/` | 5 | `WitchTrials.Presenters` |
| `witchtrials-debugging/` | 17 | `WitchTrials.Debugging` |
| `giga-ui/` `giga-common/` `giga-ess-ui/` `giga-actors/` `giga-spawn/` | 21/24/42/30/5 | GigaCreation 6 个 dll |
| `naninovel-subset/` | 14 | 仅本报告用到的 Naninovel 基类（**Naninovel 全量归他人**） |

签名格式：首行 `[il2cpp] dll=... ns=... name=...`，次行 `[source] Xxx.cs`，正文为 2 空格缩进（成员）/4+ 空格（嵌套类型成员）。IL2CPP 类型映射见 `re/decompiled/signatures/README.md`。

### 9.3 版本

- interop 生成于 BepInEx 6.0.0-be.788 环境，`Assembly-CSharp.dll` 1244160 B；ilspycmd 9.1.0.7988。
- 本报告基于**静态**证据；第 8.2 节条目全部需要运行时探针（另一队友的 probe 工作）。
