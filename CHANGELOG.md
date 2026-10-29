# 更新日志

> 结构沿用 A11yFramework 的约定：顶部固定「版本方案」「版本清单」两节，
> 每条写清「现象 → 后果 → 怎么修」。**没有实机复测的条目必须标注「尚未实机复测」**，
> 不许把「编译通过」写成「已验证」。

## 版本方案

- 版本号是**纯数字四段**（`System.Version`）：当前 `0.0.0.1`。带字母的版本号会让
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
