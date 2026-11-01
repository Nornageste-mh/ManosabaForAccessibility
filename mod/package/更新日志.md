# 更新日志

> 结构沿用 A11yFramework 的约定：顶部固定「版本方案」「版本清单」两节，
> 每条写清「现象 → 后果 → 怎么修」。**没有实机复测的条目必须标注「尚未实机复测」**，
> 不许把「编译通过」写成「已验证」。

## 版本方案

- 版本号是**纯数字四段**（`System.Version`）：当前 `0.0.0.10`。带字母的版本号会让
  BepInEx **静默跳过整个插件**（日志只有一行 Warning）。
- **升版克制**（按项目要求）：只有「玩家能感知的新功能」或「实机事故修复」才 +1 最后一段；
  纯文档、纯打包、纯内部重构一律不动版本号。`0.0.0.1 → 0.0.0.2` 这种小步走是正常节奏，
  不要为了「看起来有进展」跳版本。
- `Plugin.cs` 的 `public const string Version` 必须与 `ManosabaA11y.csproj` 的
  `<Version>` 完全一致；`mod\build.ps1` 的第三道守卫会校验，不一致就中断。
- 语义：`0.0.x` = 仍在补完逐作层；`0.x` = 主线流程（对话 / 选项 / 审判辩论 / 魔女手册 /
  系统菜单）都经过实机验收；`1.0` = 可面向普通玩家发布。
- 发布 tag 写 `v<版本>`，例如 `v0.0.0.1`。

## 版本清单

### v0.0.0.10（8 份审计报告回填：文本边界/说话人/交互与合规）

**起因**：上一版交付后派了 8 个只读子代理分头审计（文本清洗 / Harmony 补丁 / 界面导航 / 交互竞态 / L2 契约 / 配置 / 测试套件 / 合规）。
本版只收回“修完能定量验证”的那些，没修的列在末尾。

**怎么修的（逐条）**
1. 文本：`<br>` 左侧是「的/是/了/在/地/得…」时不再补逗号（语料里 1,353 处）；`<br>　` 造成的孤立空格删掉（
   3,342 处）；ruby 的拉丁旁注（Balloon / SALAMANDER…）只读正文；破折号后不再叠逗号；句末句号不再被剥。
2. 说话人：行 id 反推作者（`0101Trial01_Sherry001 → Sherry`），但**必须能查成中文名**才用——否则
   `…_Narrative001` 会被反推成作者 `Narrative`，旁白就被念成英文名。`TextProc.Speaker` 对未映射的纯西文 id 现在返回空串。
3. 补丁：删掉结构性无效的 `UITextPrinterPanel.AddMessage`（两个具体面板都 override）；`Type.GetType`
   静默降级改成 `AccessTools.TypeByName` + 失败打 LogError；新增倒计时 / HP / 提交事件三类 void 补丁。
4. 交互：地图选择**只派发一次**点击（先真实输入注入、失败才回退合成）——run117/118 的“选食堂走了医务室”就是两次点击造成的；
   注入前加坐标越界检查；证物层改成“先点后诵”（点击失败不再谎报“已选中”）；三层共用一个
   焦点门（`FocusGate`）：游戏窗口不在前台时不响应 1-9，避免玩家在别的程序里输数字就把选项点了。
5. 配置：`退出前二次确认` 默认对象名改成实机取证的 `Button_Exit`（原来默认留空→永不生效）；
   `沉默按键` 默认改空（`0` 不是合法键名，每次启动都报警）；删掉 4 个零读取开关；
   `MANOSABA_A11Y_TEST`/`DIAG` 不再把 `界面诊断日志` 永久写进玩家 cfg（改成纯内存）；加两条 cfg 迁移。
6. 安全：测试钩子总闸（没有 TEST/DIAG 环境变量时所有 `*_TEST_*` 一律不生效）。
7. 测试：`Skip` 过滤修复（原来被 `if ($Only.Count -gt 0)` 包着——Skip 永远不生效）；用例厂时看门狗；
   `Clear-A11yEnv` 补齐 9 个环境变量；单元断言 26 → **45** 条（新增文本边界 8 条 + L2 接线 6 条 + 配置默认值 2 条）。
8. 合规：把 `evidence/raw/`、`re/raw/`、`re/lead/` 从 git 索引移除（561 个文件）并加回忽略；
   `tools\check-staged.ps1` 的禁用路径/扩展名补上 `evidence/raw/`、`*.bin`、`*.nson`。

**实机验证**（`tests\Suite.ps1 -Only unit.selftest`）：`SELFTEST SUMMARY total=45 pass=45 fail=0`。

**本版未修（已已存在的审计清单，下一版继续）**：快进停止补念、手机消息/表情朗读（需先有数据源）、
警告级的界面导航细节（下拉框读 Item1、滑条值念两遍、组总览只列 6 组）、测试套件的 JUMP 生效校验与部署新鲜度校验。


### v0.0.0.9（审判「出示证据 / 选证物」：纯图标的证物卡也能听懂）

**现象**
审判到了要出示证据时，游戏会打开魔女手册并限制成只剩「证物」页，底部胶片条是一排
**纯图标**的证物卡（`WitchBookItemButton` 的 `_label` 是空串、子树里也没有 TMP 文本），
读屏用户既听不到有哪些证物，也没有任何可聚焦的文字。

**三层根因**
1. **`IsPresentEvidenceMode` 在真正的选证物界面里也是 `false`**（实机 ev1 日志：手册确实被
   限制成只剩证物页、`PresentEvidenceButton` 已亮，但 `mode=False`）。所以「盯着这个标志」
   永远不会触发。
2. 证物卡没有文字，名字只出现在「选中之后页面右侧的标题」里，必须点一下才知道叫什么。
3. 「出示证据」按钮挂在 **WorldSpace Canvas** 上，`canvas.worldCamera` 为 null，用
   `WorldToScreenPoint(null, …)` 算出来是负坐标（实机 `screen=-7,-2`），点击落到屏幕外，
   表现就是「念了『已出示证据』但游戏毫无反应」。

**怎么修**
1. 门控改成读游戏自己的限制：`WitchBookScreen.RestrictionCategories` 里**必须含 `Clue`**，
   且 `RestrictionAreas` 为空（地图选择时 areas 非空，交给 `MapChoice`，两边不抢）。
2. 名字枚举：一张一张真实点击证物卡 → 读页面标题 → 缓存到 `Dictionary<int,string>`
   （不缓存会每 0.5 秒重扫、反复枚举，实机 ev2 撞到过）。枚举结束回点第一张，恢复选中态。
3. 注入点击前先算屏幕坐标并做**边界检查**：越界只记日志、不点击（避免点到别的按钮）。
   WorldSpace 画布回退 `Camera.main` 取事件相机。
4. 交互按「不替玩家做决策」设计：**数字键 = 选中并播报名字**，
   **回车/空格 = 出示**（随后进游戏自己的确认框）；确认框照读文案与按钮，数字键选「返回/是」。
   我们**永不**自动回答「是」。

**实机证据（0.0.0.9，`probe\run124-evdialog.ps1`）**
- `EVIDENCE 名字枚举完成: 1=二阶堂希罗原本持有的钢笔`
- `EVIDENCE 播报: 请出示证据。可选的证物共 1 件：1、二阶堂希罗原本持有的钢笔。按数字键选择。`
- `EVIDENCE 数字键 1 -> 二阶堂希罗原本持有的钢笔` → `EVIDENCE 注入点击 name=Button screen=309,250 canvas=WorldSpace`
- `EVIDENCE 对话框: 确定要提交此项证据吗？。1、返回。2、是。按数字键选择。`
- `EVIDENCE 对话框数字键 2` → `EVIDENCE dump … idx=191` → **`idx=197`**（剧本真的过了
  `@choiceEvidence` 那一行，证据提交成功）。

**尚未实机复测**
- 多件证物的列表播报（本次这一处只有 1 件）。
- 确认框选「返回」之后的返回路径。
- 全部 61 个含 `@choiceEvidence` 的审判脚本只抽了 `Act01_Chapter01_Trial04` 第 327 行验证。
- 自动化用例目前仍默认跳过（见 `tests\README.md`）：要跨「3 选 1 → 剧情连开线索页」才到
  这一步，6 FPS 下点击不稳；靠 `MANOSABA_A11Y_JUMP_LINE` 跳行复现。

### v0.0.0.8（地图页选择地点：读游戏自己的允许名单 + 数字键 + 真实输入注入）

**现象**（用户反馈 + 截图）
- 审判/剧情里出现「要去哪里」的地图时，读屏用户既听不到哪些地点可去，也点不到。
- 旧版（0.0.0.7）只能按「当前楼层 + 名牌是否可见」猜：run97 实机把**命令没允许**的「图书室」
  也念了出来，选它游戏会自动落回默认项（玄关大厅）——等于给了一个假选项。

**根因（三层）**
1. 权威名单不在我们手里：`@choiceMapArea MapAreaNames:…` 的参数在运行时落到
   `WitchTrials.Views.MapPage._restrictionAreas`（`IReadOnlyCollection<MapAreaKind>`，
   interop 的接口没有 `Count`，要 `TryCast<List<MapAreaKind>>()` 才取得出条目）。
2. 跨楼层地点是**可点**的：run112 直接点 2F 的娱乐室，剧情正常进 `Adv32`；
   之前「只列当前楼层」是因为切楼层会出下文第 3 条的事故。
3. **合成点击游戏不认**：`onClick.Invoke()` 甚至 `ExecuteEvents.pointerClickHandler` 都派发了
   （诊断 `点击派发 ok=True`），`OnChoiceMapAreaAsync` 依然不返回、地图页一直不关（run117/118）。
   本作只启用新 Input System，必须往 `InputSystem` 事件队列里塞真实的鼠标按下/抬起。

**怎么修**
- `MapChoice.cs` 新增 `_allowedKinds`：从 `MapPage._restrictionAreas` 读名单，`live` 判据改成
  「在名单里 + 有按钮 + 可点 + 有名字」，读不到名单才退回旧判据（当前楼层 + 名牌可见）。
- 播报跨楼层分组，一次念全（用户 2026-10-31 选定「立刻念全」）：
  `地图上可前往 8 个地点：在1F：1、会客厅。… 在2F：7、娱乐室。在地下1F：8、牢房。按数字键选择。`
- 数字键 → `ClickByInput()`：`RectTransformUtility.WorldToScreenPoint` 换算屏幕坐标，
  再 `InputInject.Click(x,y)` 走 InputSystem 队列；合成派发保留作兜底。
- 名单超过 9 项时 `PageUp`/`PageDown` 在**名单内**翻页（用户 2026-10-31 选定），
  不在名单内时才是浏览态的楼层切换；选择等待期间禁用楼层切换（见事故 2）。
- `ScreenAssist.SimulateEscape()` 补上「正在选地点就不许关手册」的保护（和真实 Esc 一致）。

**实机证据**（`probe\logs
un11x-summary.txt`、`tests
eports\map*.md`）
- `MAPCHOICE 扫描 areas=33 page=ok dispLen=29 restr=名单(8) choiceMode=True`，
  8 条与剧本 `@choiceMapArea MapAreaNames:BasementHallway,Lake,Lounge,EntranceHall,Infirmary,MessHall,RecreationRoom,Showers` 完全一致。
- 数字键 3 → `chosenAreaId=String:MessHall` → `Adv31「艾玛她们为了调查，来到了食堂」`
- 数字键 7（2F）→ `chosenAreaId=String:RecreationRoom` → `Adv32「艾玛她们来到了娱乐室，看见正靠在沙发上的可可」`
- 数字键 8（地下1F）→ `chosenAreaId=String:BasementHallway` → `Adv26「决定前往最有可能找到证据的尸体目击现场」`

**事故（修之前踩到的）**
- 选择等待期间点手册底部楼层条 → `OnChoiceMapAreaAsync` 的 await 被挂断，加载界面永久卡死
  （run105/106，`HOOK loadingUi visible=True` 后再也不变 False）。→ 选择态禁用楼层切换。
- 自动化脚本在地图选择期间还每 3 秒点屏幕中央 → 真的点中地图地点、以它完成选择，
  于是「数字键选娱乐室」看起来走错分支（run115）。→ 检测到选择态立即停止点击。

**尚未实机复测**
- 名单超过 9 项时的翻页：当前剧本里最大 8 项，代码路径只有编译验证。
- 浏览态（非选择）用 `PageUp/PageDown` 翻楼层。

### v0.0.0.7（魔女手册/记录整页界面：自动播报 + Esc 关闭）

**现象**（用户 2026-10-31 反馈 + 三张截图）：进魔女手册「记录」页后**出不来**；「地图」页
按导航走不到地点，只会念「未命名开关」。日志实测：
`[画面外或点不到] …/WitchBookUI/Wrapper/CloseButton`、
`[画面外或点不到] …/Page_Maps/ItemButtonPool/WitchBookItemButton(Clone)`、
`[画面外或点不到] …/ItemList/Pagination/MovingArrows/ButtonNext`。

**后果**：读屏玩家一旦打开手册就可能被关在里面（只能 Alt+F4），地图页等于不可用。

**根因**：平台层的可达性判定（`UiNav.VisiblyClickable`：CanvasRect 相交 + 从控件中心打
UI 射线，只认打到自己/子物体）被**书本打开动画的全屏 CanvasGroup** 骗了——射线先打到那层
遮罩，于是真实的关闭按钮/地点按钮全被判成「点不到」。地图地点本身还不是 Selectable
（`WitchTrials.Views.MapAreaLabel` 只是 TMP 标签），平台层根本不扫。

**怎么修**（新增逐作层 `ScreenAssist.cs`，不动平台层）：
1. **自动播报**：`WitchBookUI` / `LogUI` 可见时，每 0.5s 扫一次，念「界面名，页签。内容：…」；
   内容只取**当前页**（`Page_<页签>` 子树），避免把别的页也念出来。
2. **地图页专用**：取当前楼层容器（`F1_1`/`F2_1`/`F2_2`/`B1_1`）下的 `MapAreaLabel._label`
   文字，念成「1F：会客厅、玄关大厅、…」。这些标签的 CanvasGroup 平时 alpha=0，
   所以这一条**故意不看可见性**（文字齐全，只是没显示）。
3. **Esc 关闭**：每帧查 Esc（本作 6 FPS，0.5s 节流会漏掉快速按键），找到
   `WitchBookUI/Wrapper/CloseButton` 直接 `onClick.Invoke()`，念「已关闭魔女手册。」。
4. 内容过滤：跳过 `Description` 这类本地化键名（`LooksLikeKey`）。

**实机证据（run86–run91，`probe\run86-book-assist.ps1`：读取→存档槽 1→手册→地图页）**：
- `ScreenAssist: 播报 魔女手册，线索页签。内容：二阶堂希罗原本持有的钢笔。`
- `ScreenAssist: 播报 魔女手册，地图页签。内容：1F：会客厅、玄关大厅、审判庭入口过道、食堂、厨房、审判庭、接客室、医务室、杂物处、女厕、日光房、淋浴房、中庭、监牢入口、招待所、火精之室、水精之室、地精之室、仓库、围墙、往湖方向、往花田方向。`
- `ScreenAssist: Esc 关闭 魔女手册（path=Naninovel<Runtime>/UI/WitchBookUI/Wrapper/CloseButton）`

**尚未实机复测**：真实键盘 Esc（本机自动化环境的键盘注入被系统挡掉，`SendInput` 返回 0；
关闭逻辑用 `MANOSABA_A11Y_TEST_ESC=1` 钩子验证）、`LogUI`（对话记录）页的实际念法。

**测试**：`tests\Suite.ps1` 新增 `book.assist` 用例（读取存档槽 1 → 点书本 → 断言播报 →
`TEST_ESC` 钩子断言关闭），并把整套用例的点击改成「相对坐标 + ClientToScreen」；全量
`full5` 跑出 **11/11 通过**（此前因坐标/键盘注入问题会集体红）。

### v0.0.0.6（开发者面板：查清发行版里没有，开关默认关）

**现象**：玩家希望用游戏内置开发者面板（Ctrl+Alt+Shift+F3）减少测试负担。

**实机查证（run76–run82，`MANOSABA_A11Y_DEBUGUI=1` 一次性侦察）**：
- `DebugOptionsBase.s_isInitialized=true`，但 `s_debugManager=null` —— 发行版里游戏自己**没有创建**调试管理器。
- `DebugPanel` 只有 **1 个 prefab 资产**（`scene=<prefab/无场景>`、`parent=Wrapper`、`activeInHierarchy=False`），
  从未被实例化；`EnableDebugModeButton` 与 `DebugCommandButtons` 同样只有 prefab / 未激活。
- 因此 Ctrl+Alt+Shift+F3 在**发行版**里没有面板可开；这是开发构建（Editor / Dev Build）的功能。

**做了什么**：
- 新增配置「其它 / 允许激活开发者选项」（**默认关闭**，按玩家要求）：打开后补建 `DebugManager(true)`、
  翻转 `IsDebugMode`，并把 `DebugPanel` prefab 实例化后挂到场景 Canvas 上（尽力而为）。
- 组合键读取改走 `Plugin.SpecDown`（Win32 `GetAsyncKeyState` 全局兜底 ∪ 新 Input System）：
  run77 起日志稳定出现 `DevPanel: IsDebugMode -> true/false`，说明**组合键本身可用**，
  只是发行版没有面板可显示（面板对象 activeInHierarchy 仍为 False / 画面上看不到）。

**结论**：开关保留给开发构建；发行版里按了也不会出现面板，默认关闭，避免玩家误以为游戏坏了。

### v0.0.0.5（功能键读取接上「新 Input System」+ 全局键预热）

**已实机验证（2026-10-29 18:11，run71：`say=56 voiced=34 重读=3`）**

- 修：**退格 / 沉默 / 导航组合键在某些启动方式下读不到**。
  - 现象：同样的部署，`重读` 有时能出、有时整轮一条都没有；KEYDIAG 里
    `reading=True available=True` 但 `back=False`。
  - 根因（两层）：
    1. `Hotkey.Down` 的全局兜底依赖 `Hotkey.GlobalHeld` 委托，而它只在
       `GlobalKeys.Reading` 被**第一次访问**时才接线；启动早期如果没有别的代码碰过
       `Reading`，前几帧（甚至整轮，如果 `TickUiNav` 没跑）功能键就是静默失效。
    2. 本作只启用新 Input System，平台层的 Win32 `GetAsyncKeyState` 是一路兜底，
       但自动化环境里 `keybd_event` 注入的键不一定进 async key state
       （同进程自测 `sawKey=False`），于是测试脚本永远验证不到这两个键。
  - 怎么修：
    - `Plugin.Load()` 里主动访问一次 `GlobalKeys.Reading`（触发平台层懒同步）并把
      `reading/available/backspaceHeld` 写进日志；
    - 新增 `InputSysKeys`（`mod\src\ManosabaA11y\InputSysKeys.cs`）：用游戏自己的
      `UnityEngine.InputSystem.Keyboard.current` 读「这一帧刚按下」，覆盖字母/数字/
      功能键/方向键/退格等配置里可能写的键；
    - `Plugin.SpecDown(spec)` = `Hotkey.Down(spec)` **或** `InputSysKeys` 同语义判断
      （修饰键也按配置严格匹配，没写的修饰键要求没按住，避免抢游戏按键）；
      退格/沉默/导航组合键都改走它。
  - 验证：run71 `重读: 樱羽艾玛：牢、牢房……！？为什么！？`（有配音行也能用 TTS 重读）、
    `重读: 艾玛早早地洗了澡，换了睡衣，早早地钻进了被窝里`；手工注入 Backspace 同样命中。
  - 备注：这一版**没有改平台层**，逐作层只做「多一路读取」，平台层的 `Hotkey`/`KeyEdge`
    依旧是主路径（真实玩家两条路都能读到，并集无副作用）。
### v0.0.0.4（魔女手册命名 + 审判实机验证 + 配音报名字改默认关）

**已实机验证（2026-10-29，Unity 6000.0.48f1 / IL2CPP / BepInEx 6.0.0-be.788）**

- 修：**魔女手册里一串「未命名开关/未命名按钮」**（玩家实机反馈「有很多未命名控件」）。
  - 取证：玩家日志里 44 条 `[UiNav] 未命名控件`，按路径分组 = 页签 `Tab_Maps`(17)、`Tab_Notes`(10)、
    `Tab_Rules`(3)、打开手册的 `WitchBookButtonUI/Wrapper/Button`(9)、`PresentEvidenceButton`(5)。
  - 根因：这些页签只有 `CanvasGroup`+`RebuildableLabel`，**TMP 文字挂在未激活的子物体上**，
    平台层的 `OwnTextOf` 只认激活文字 → 一个字都取不到；按钮本身对象名又太泛（`Button`/`Wrapper`）。
  - 改法（都在逐作层 `UiNav.Game.cs`，平台层未动）：
    1) `NameAlias` 补页签（线索/人物/地图/规定/记录）、出示证据、魔女手册、分页箭头（上一页/下一页）、
       `LoupeButton`（放大查看）；
    2) `PathAlias` 补**地图全部 29 个地点**（医务室/审判庭/图书室/…，中文名取自游戏自己的
       `CustomUI.txt` 的 `WitchBook.Map.*`，不是我们编的）与楼层容器（F1_1/F2_1/F2_2/B1_1）；
    3) `GameLabelOf` 增加「NameAlias 祖先」与 `InactiveTextOf`（读子树里**未激活**的 TMP 文字，
       长度 ≤24、过噪声过滤；滑条/下拉框跳过，否则会把「100%」当名字）；
    4) `GameGroupName` 改成**先看组根自己**：魔女手册的 5 个页签各自成组，之前一路往上找到
       `WitchBookUI` 会让 10 个组全念「魔女手册」。
  - 实测（run60/run61 UIPROBE `resolved=` 栏）：`Tab_Clues→线索`、`Tab_Profiles→人物`、
    `Focus(…/Page_Maps/F1_1/Infirmary)→医务室`、`Focus(…/B1_1/Incinerator)→焚烧炉`、
    `ButtonNext→下一页`、`WitchBookButtonUI/Wrapper/Button→魔女手册`、`DebateUI/…/SkipButton→跳过辩论`。
  - 实测（run61 导航播报原文）：`导航模式。第 1 / 10 组：记录页签，1 项。全部 10 组：【记录页签 1 项】；
    规定页签 1 项；地图页签 1 项；人物页签 1 项；魔女手册 14 项；线索页签 1 项。…记录，开关，关。1 / 1`。
- 改：**「有配音时也报说话人」改为默认关闭**（按要求）。现在有配音的行**完全安静**：
  不念正文、也不念名字；玩家要文本时按 `Backspace` 用 TTS 重读（这一条 v0.0.0.3 已实机验证）。
  旧配置文件不会自动迁移，装好后请确认 `BepInEx\config\manosaba.a11y.reader.cfg` 里
  `有配音时也报说话人 = false`。
- 验证：**魔女审判（审判/辩论）实机跑通**（run59，JUMP 到 `Act01_Chapter01/Act01_Chapter01_Trial01`，
  300 秒真实点击推进）：`say=16 voiced=3 debate=34`。
  - 辩论台词（`@printDebate`）逐句朗读，实测原文
    `Reader.say[debate:add] raw=我找到的证据……<br>那就是<br>死亡讯息！ => 我找到的证据……那就是，死亡讯息！`；
    带 `<link>`（异议触发词）的行只读文字、不读 id：`在这些地方都有用血画下的蝴蝶绘画！`。
  - 审判画面的 HUD：跳过按钮 ` DebateUI/Wrapper/SkipButton/Button → 跳过辩论`；
    倒计时与 HP 条**不是可交互控件**（是 Image/文字），导航读不到 —— 见「未验证/未实现」。
- 诊断留档：`MANOSABA_A11Y_PROBE_UI=1` 的 UIPROBE 转储改成「12 秒后开始、最多 12 次、每次 40 秒」，
  这样魔女手册、审判这些要走真实操作才能到达的画面也能被截到（run59 一次转储 1069 行）。
- 未验证 / 未实现（诚实记录）：
  - 「方向键逐项移动」在魔女手册里**没有在自动化下复测成功**：扩展键（方向键/PageDown）由脚本
    `keybd_event` 合成时命中率不稳定（标题菜单那次成功过，书里这次没有），**建议玩家自己按方向键确认**；
    代码路径与标题菜单完全相同（`KeyEdge.Pressed` → 全局 `GetAsyncKeyState`）。
  - 审判倒计时、HP 数值、关键选项提示（`CriticalChoiceHint`）**不会主动播报**：前者是 Image+文字、
    没有可交互控件可挂；后两者需要额外的主动播报机制，目前没做。
  - 证据出示（`ChoiceEvidence`）只加了剧透闸门约束、**未实机走到该分支**。

### v0.0.0.3（退格重读 + 配音闸门 + 隐藏控制台）

**已实机验证（2026-10-29，Unity 6000.0.48f1 / IL2CPP / BepInEx 6.0.0-be.788）**

- 新增：**退格重读上一句**（配置「朗读 / 重读按键」，默认 `Backspace`）。
  - 语义：只重读**剧情文字与选项播报**，不重读界面导航的「系统设置，按钮。2 / 5」
    ——所以逐作层自己维护了一条「剧情专用」最后一句（`Reader.Remember/LastLine`），
    没有直接用平台层的 `Repeat.Again()`（后者会把导航播报一起退回来）。
  - 有配音的行**也会**记进这条缓冲：语音错过了，退格把文本念一遍正是读屏用户要的。
  - 实测（run57/run58）：`重读: 唯有这句话不管怎样都说不出口`、
    `重读: 樱羽艾玛：牢、牢房……！？为什么！？`（后者是有配音的行，证明可被 TTS 重读）。
  - 首版失效原因（记录，避免重犯）：`Hotkey.Down` 对「认不出来的键名」静默返回 false，
    日志里什么都没有 → 现在启动时把三个功能键解析结果各写一行日志。
- 新增：**有配音的行不自动朗读**（配置「朗读 / 朗读无配音剧情」，默认开；
  「有配音时也报说话人」默认开）。判定依据是**行 id 的静态配音表**。
  - 行 id 来源：`PrintedMessage.Text.parts[i].id`。为此新增
    `Naninovel.UITextPrinter.AddMessage(PrintedMessage)` 与 `SetMessages(IReadOnlyList<PrintedMessage>)`
    两条补丁 —— 本作 ADV 路径**从不调用面板级** `UITextPrinterPanel.AddMessage`
    （之前挂的补丁一条日志都没有），拿不到 `PrintedMessage` 就没有行 id，
    配音闸门只能全放行（run56：`voiced=0`）。
  - 为什么这两条可以挂、而 `TextPrinterManager.Print` 不行：它们的返回类型是 `void`
    （ilspycmd 实查），Harmony 原生 detour 不需要搬非 blittable 返回值，
    不会重演「async 状态机 `MethodAccessException`、剧情永久卡死」的事故。
  - 配音表：`tools\gen_voicelist.py` 从 `textassets\voice-index.tsv`（26,083 条 clip）
    生成 `VoiceLineIds.cs`（gzip + base64 内嵌），启动解压成 `HashSet<string>`；
    解压失败退化为「全部朗读」并写诊断日志。
  - 实测（run58）：`Reader.voiced[reveal] id=0101Adv02_Ema001 => 有配音，跳过正文（只报名字）`
    共 22 条，同时 `[朗读] 樱羽艾玛` 只报名字；同一句按退格仍能念出文本。
- 新增：**隐藏 BepInEx 控制台窗口**（配置「其它 / 隐藏控制台窗口」，默认开）。
  - 只隐藏窗口，**不改 `BepInEx.cfg`**（不动别人的配置、玩家可能已装 BepInEx）。
  - **事故与修复**：第一版直接 `ShowWindow(GetConsoleWindow(), SW_HIDE)`，
    结果实测日志里「控制台 hwnd」与脚本抓到的**游戏主窗口 hwnd 相同**（16717180）
    —— 等于把游戏窗口隐藏了，10 次点击全部点空、`say=0`。
    现在先校验 `GetClassName == "ConsoleWindowClass"` + `GetWindowText` + 进程 id，
    非控制台类就跳过并记日志；run56/run57/run58 均为
    `控制台候选 … class=[ConsoleWindowClass] title=[BepInEx 6.0.0-be.788… - manosaba]` → `隐藏控制台窗口 … ok=True`。
- 新增：**沉默键**（配置「朗读 / 沉默按键」，默认 `0`）接线到 `Speech.Stop()`
  （之前只 Bind、没人读，等于摆设）。
- 补丁数从 10 条变 12 条（新增 printer 级 `AddMessage` / `SetMessages`）。

### v0.0.0.2（导航键改为组合键 + 语音后端切换热键）

**已实机验证（2026-10-29，Unity 6000.0.48f1 / IL2CPP / BepInEx 6.0.0-be.788）**

- 修：**标题画面出现后 Tab 失效**（用户实机反馈：「主界面还没加载出来时按 Tab 有反应，
  主界面加载后按 Tab 就没反应了」）。原因是游戏自己的标题菜单把 Tab 当菜单键吃掉了
  （Boot 阶段没人抢，所以表现为「有时能用」）。
  - 改法：导航开关键改成**组合键**（配置「界面导航 / 导航开关键」，默认 `Alt+Shift+F1`），
    Tab 保留为备用键（配置「同时允许 Tab」默认开）。实现走逐作层自己的钩子
    `A11yHost.NavTogglePressed`，**平台层一行没改**（平台层是不可改的复用层）。
  - 实测（run48，v0.0.0.2）：`导航开关键按下: Alt+Shift+F1` → `[朗读] 导航模式，标题画面，共 5 项。
    退出游戏，按钮。1 / 5` → 再按一次 → `[朗读] 已退出导航模式。`
  - 方向键逐项移动另测（run50）：`下拉 1.2s` → `[朗读] 系统设置，按钮。2 / 5`、
    `[朗读] 鉴赏模式，按钮。3 / 5`，`Home` → 回到 `[朗读] 退出游戏，按钮。1 / 5`。
- 新增：**运行中切换语音后端（默认 F7）**。动机是用户实机「听不到声音」：机器上同时装了
  NVDA 与争渡，`语音后端 = 自动` 按平台顺序挑中了 NVDA，而玩家平时用争渡 → 一句都听不到。
  - 行为：按一下循环 `自动 → Tolk → NVDA → 争渡 → SAPI`，并念出「语音后端：X」。
  - 实测（run48）：`语音后端: 争渡读屏（ZDSR）…状态=空闲（可用）`，
    按 F7 → `[朗读] 语音后端：SAPI` + `[a11y] 语音后端切换 -> SAPI`。
  - 启动播报同时改成带后端名：`魔法少女ノ魔女裁判 无障碍补丁已加载。语音后端 争渡读屏`
    —— 「没声音」时第一句日志/播报就能定位是不是后端挑错了。
- 新增：`MANOSABA_A11Y_KEYDIAG=1` 诊断开关（每 0.5 秒打印平台层键盘读取状态）。
  起因：自动化发方向键时导航没反应，需要区分「游戏没收到」还是「平台层读不到」。
  实测该开关直接指出 `reading=True available=True` 但 `down=False` —— 问题在测试脚本
  （Win32 `keybd_event` 对方向键这类扩展键必须带 `KEYEVENTF_EXTENDEDKEY`），不在补丁。
- 文档：`安装说明.md` 快捷键表更新（主键 `Alt`+`Shift`+`F1`、`F7` 换后端），
  并新增「为什么导航不是 Tab 了」「听不到声音？先按 F7」两节。

### v0.0.0.1（首个可安装版本；版本号按项目要求从 0.0.0.1 起算）

**已实机验证（2026-10-29，Unity 6000.0.48f1 / IL2CPP / BepInEx 6.0.0-be.788 / NVDA）**

- 新增：BepInEx 6 插件骨架，`BasePlugin` + `PluginBehaviour`（BepInEx 6 的插件基类
  不是 MonoBehaviour，没有 `Update`，每帧逻辑必须挂在自建 MonoBehaviour 上）。
- 新增：**对话朗读**。挂点 `Naninovel.UI.RevealableText.set_Text`（Postfix）。
  实测日志：`Reader.say[reveal] raw=——这里是囚牢。 => ——这里是囚牢`，
  单轮连续 82 句文本链稳定、无 `MethodAccessException`。
- 新增：**说话人朗读**。`WitchTrialsTextPrinterPanel.SetMessageAuthor(MessageAuthor)`
  与 `RevealableTextPrinterPanel.SetAuthorNameText(string)` 两个 Postfix 互补：
  前者实测 `id/label` 恒为空（本作不走 Naninovel 的 actor 元数据），
  后者给出的名字框显示串才是有效值。实测输出 `樱羽艾玛：——不对！`；旁白不带说话人。
- 新增：文本清洗 `TextProc`。基于 440 个 zh-Hans 剧本文件的实际标签统计：
  `<br>` 25693 次、`<link>` 1192 次、`<color>` 130 次、`<b>` 72 次、`<ruby>` 50 次。
  规则：剥 `|#行号|` → 剥 `作者:` 前缀 → `<br>` 转停顿 → ruby「正文（旁注）」→
  link 只留文字 → 其余标签删除 → 方块字符读「方块」→ 空白/标点归一。
- 新增：**选项朗读 + 数字键选择**（实机跑到第 1 章第 5 幕的选项）。
  实测日志：`选项容器 Content childCount=2` → `CHOICE 播报 2 项: 挡在蕾雅的身前|选择了旁观`
  → `CHOICE 数字键 1 -> 挡在蕾雅的身前` → 脚本按所选分支继续。
  整组读成「共 2 个选项。选项 1：……。选项 2：……。按数字键选择。」
- 新增：**审判辩论朗读**（实机 JUMP 到 `Act01_Chapter01/Act01_Chapter01_Trial01` 验证）。
  辩论走 `DebateTextPrinterPanel.AddText(string)`（`debate:add` 来源），实测 8 句连续、
  `<link>` 只读文字、`<br>` 转停顿：`看尸体的下面，还有旁边……在这些地方都有用血画下的蝴蝶绘画！`
  辩论**没有**逐字回调，所以整句到达即朗读；也**没有**可用的说话人（为防张冠李戴，辩论不报名字）。
- 新增：**加载播报**（照《不存在》的 `LoadingWatch` 做法）。本作点「新游戏」后有
  黑屏 + 右下角进度条的一段，读屏玩家此时完全没有线索。挂
  `GigaCreation.NaninovelExtender.Ui.LoadingUiExtended.HandleVisibilityChanged(bool)`
  的 Postfix（事件驱动；轮询查 `Naninovel.UI.LoadingPanel` 与按对象名找 `LoadingUI`
  都不可靠），可见时念「正在加载。」并**打断**当前朗读（时效性就是它的全部价值）。
  实测日志：`patched LoadingUiExtended.HandleVisibilityChanged` → `加载播报（LoadingUiExtended）`。
- 新增：**菜单/设置界面的中文名与当前值**。用一次性取证探针
  （`MANOSABA_A11Y_PROBE_UI=1`，实机转储 79 个 `Selectable` 的对象名与路径）建立
  `NameAlias`（`Button_Save` 保存 / `Button_Load` 读取存档 / `Button_Log` 对话记录 /
  `Button_Options` 系统设置 / `Button_Title` 返回标题 / `CloseButton` 关闭 /
  `OpenButton` 打开菜单 / `AutoToggle` 自动播放 / `PlayVoiceButton` 重播语音 / 页签 …）
  与 `PathAlias`（`MasterVolume` 主音量、`BgmVolume` 音乐音量、`MessageSpeed` 文字显示速度、
  `Language` 语言、`ResolutionDropDown` 分辨率 …）；滑块读百分比、下拉框读当前选项文字。
- 修：**旧输入 API 在本作整体不可用**。实机探针抓到原文异常：
  `You are trying to read Input using the UnityEngine.Input class, but you have switched
  active Input handling to Input System package in Player Settings.`
  → 平台层热键/导航若走裸 `Input` 会全部静默失效。改开 `A11yHost.UseGlobalKeys = true`
  （窗口无焦点时用 Win32 `GetAsyncKeyState` 兜底），选项数字键改走平台层 `KeyEdge`。
- 修：**标题菜单整排按钮念「Ver. 1.1.2」**。`TitleUI/Wrapper/Buttons/*` 这 5 个按钮
  子树里没有标签文字，平台层的「同行文字」退路取到了旁边的版本号
  （`TitleUI/Wrapper/VersionNumberLabel` = `Ver. 1.1.2`），于是整排都念版本号。
  修法用逐作钩子：`GameTextIsNoise` 把 `Ver.` 开头的文本判成噪声，
  `NameAlias` 补 `Button_NewGame` 新游戏 / `Button_LoadGame` 读取存档 /
  `Button_Gallery` 鉴赏模式 / `Button_Exit` 退出游戏，`GameGroupName` 把组名
  从兜底的「面板」改成「标题画面」。实测读出
  `导航模式，标题画面，共 5 项。退出游戏，按钮。1 / 5`。

- 新增：**文本语言（简体中文 / 日文）切换**。本作默认日文
  （`StreamingAssets\config.json` 的 `available_text_lang` = `ja` / `zh-Hans`），
  补丁用 `ILocalizationManager.SelectLocale(locale).Forget()` 切换，**已实机双向验证**：
  默认 `zh-Hans` 读中文（`——这里是囚牢。`），改成 `ja` 后读到日文
  （`Reader.say[reveal] raw=――ここは獄だ。`），证明真的换了 locale 而不是缓存。
- 修：**选项重复播报**。面板淡入淡出时 `UiVis.Visible` 会抖一帧 false，旧代码在
  `!live` 分支把去重键清零 → 同一组选项要念两遍（run32）。改法：`!live` 只清按钮列表、
  不清 `_announcedKey`，并加 1.5 秒宽限；`Reset()` 才彻底清空。
  实测 run34 起「一次选项只播报一次」。

**修掉的两个事故（都是实机打出来的，不是编译期能发现的）**

1. **`TextPrinterManager.Print` 补丁杀死 Naninovel 异步协程**。
   - 现象：装插件后剧情停在第一句，`playing=True / waiting=False`，任何输入都推不动。
   - 根因：`Naninovel.TextPrinterManager.Print(...)` 的返回类型是 `UniTask`（非 blittable
     结构体）。Harmony 的原生 detour 搬不动它的返回值，调用方的 async 状态机在
     `await` 处抛 `System.MethodAccessException: Attempt to access method
     'Naninovel.Async.IAwaiter.get_IsCompleted' on type 'Naninovel.UniTask' failed.`
     —— 命令永不结束，脚本就永远卡在那一行。
   - 怎么修：**删掉这个补丁**，文本改由 `RevealableText.set_Text`（返回 void）取。
   - 铁律：**不要 patch 返回 UniTask / Task / 其它非 blittable 结构体的 Naninovel 方法**。
     二分方法：`MANOSABA_A11Y_NOPATCH=print` 只关这一组补丁即可复现/排除。
2. **`<br>` 无条件换成逗号**（第二轮又补了一条：右侧是「的/了/着/过」等粘着语素时也不补逗号，
   否则辩论里会念出「用血画下的，蝴蝶绘画」）。
   - 现象：`原来是做梦。她才刚安下心来——<br>眼前的光景就逐渐清晰了起来。`
     被念成「……安下心来——，眼前……」。
   - 怎么修：换成命名方法 `BrEvaluator`，前一字符是句读/破折号/引号收尾时换行直接省略，
     否则补「，」。命名方法而非 lambda：net6.0 + Il2Cppmscorlib 下 lambda 会触发
     `CS0656 缺少 System.Runtime.CompilerServices.NullableAttribute..ctor`。

**未验证 / 已知缺口（想帮忙的话优先测这些）**

- 审判辩论的**说话人**：辩论界面没有名字框（`SetAuthorNameText` 不走辩论链），
  所以辩论句只念文本、不报名字 —— 这是有意的保守选择，避免把 A 的话安到 B 头上。
  另外 `DebateTextPrinterPanel.SetText(string)` **挂不上**（Harmony `IL Compile Error`），
  实机主路是 `AddText(string)`。
- 魔女手册（WitchBook）：页面用 `Present/Dismiss` 而非 `SetActive`，页签是
  `WitchTrialsTabToggle : SlideToggle`；朗读与导航**尚未实机复测**。
- 有配音时是否跳过朗读：本作语音 26083 条、对白覆盖率 98.2%，但
  **暂未做「有配音就只放语音」**（读屏玩家需要中文文本，日配中文不同步；
  且 `Naninovel.AudioManager` 没有「当前配音 clip」的公开入口，拿不到「这句有没有配音」）。
  配置项保留但行为是全部朗读。
- 焦点/全屏下的自动测试路径不可靠：本作只有约 6 FPS，Win32 点击常被吞，
  自动化只能当辅助；最终验收一律以「真人点击 + 日志」为准（见 `docs\framework-notes.md` §9）。
