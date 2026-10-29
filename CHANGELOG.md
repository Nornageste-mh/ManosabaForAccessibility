# 更新日志

> 结构沿用 A11yFramework 的约定：顶部固定「版本方案」「版本清单」两节，
> 每条写清「现象 → 后果 → 怎么修」。**没有实机复测的条目必须标注「尚未实机复测」**，
> 不许把「编译通过」写成「已验证」。

## 版本方案

- 版本号是**纯数字四段**（`System.Version`）：`0.1.0.0`。带字母的版本号会让
  BepInEx **静默跳过整个插件**（日志只有一行 Warning）。
- `Plugin.cs` 的 `public const string Version` 必须与 `ManosabaA11y.csproj` 的
  `<Version>` 完全一致；`mod\build.ps1` 的第三道守卫会校验，不一致就中断。
- 语义：`0.x` = 仍在补完逐作层；`1.0` = 全流程（对话 / 选项 / 审判辩论 / 魔女手册 /
  系统菜单）都经过实机验收。
- 发布 tag 写 `v<版本>`，例如 `v0.1.0.0`。

## 版本清单

### v0.1.0.0（首个可安装版本）

**已实机验证（2026-10-29，Unity 6000.0.48f1 / IL2CPP / BepInEx 6.0.0-be.788 / NVDA）**

- 新增：BepInEx 6 插件骨架，`BasePlugin` + `PluginBehaviour`（BepInEx 6 的插件基类
  不是 MonoBehaviour，没有 `Update`，每帧逻辑必须挂在自建 MonoBehaviour 上）。
- 新增：**对话朗读**。挂点 `Naninovel.UI.RevealableText.set_Text`（Postfix）。
  实测日志：`Reader.say[reveal] raw=——这里是囚牢。 => ——这里是囚牢`，
  连续 23~31 句文本链稳定、无异常。
- 新增：**说话人朗读**。`WitchTrialsTextPrinterPanel.SetMessageAuthor(MessageAuthor)`
  与 `RevealableTextPrinterPanel.SetAuthorNameText(string)` 两个 Postfix 互补：
  前者实测 `id/label` 恒为空（本作不走 Naninovel 的 actor 元数据），
  后者给出的名字框显示串才是有效值。实测输出 `樱羽艾玛：——不对！`；旁白不带说话人。
- 新增：文本清洗 `TextProc`。基于 440 个 zh-Hans 剧本文件的实际标签统计：
  `<br>` 25693 次、`<link>` 1192 次、`<color>` 130 次、`<b>` 72 次、`<ruby>` 50 次。
  规则：剥 `|#行号|` → 剥 `作者:` 前缀 → `<br>` 转停顿 → ruby「正文（旁注）」→
  link 只留文字 → 其余标签删除 → 方块字符读「方块」→ 空白/标点归一。
- 新增：界面导航 / 选项 / 审判辩论的补丁位（`ChoiceHandlerPanel.AddChoiceButton`、
  `DebateTextPrinterPanel.AddMessage` / `SetMessages`），已编译、已确认挂载成功，
  **但尚未在实机上读到选项与辩论文本**（见下「未验证」）。

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
2. **`<br>` 无条件换成逗号**。
   - 现象：`原来是做梦。她才刚安下心来——<br>眼前的光景就逐渐清晰了起来。`
     被念成「……安下心来——，眼前……」。
   - 怎么修：换成命名方法 `BrEvaluator`，前一字符是句读/破折号/引号收尾时换行直接省略，
     否则补「，」。命名方法而非 lambda：net6.0 + Il2Cppmscorlib 下 lambda 会触发
     `CS0656 缺少 System.Runtime.CompilerServices.NullableAttribute..ctor`。

**未验证 / 已知缺口（想帮忙的话优先测这些）**

- 选项朗读与数字键选择：补丁已挂 `ChoiceHandlerPanel.AddChoiceButton`，代码走
  「每帧扫容器、内容变化才播报」，**尚未跑到有选项的剧情位置实测**（尚未实机复测）。
- 审判辩论（`@printDebate`，全作 2003 条）：`DebateTextPrinterPanel.SetText(string)`
  **挂不上**（Harmony `IL Compile Error (unknown location)`，两个独立探针都复现），
  改挂 `AddMessage(PrintedMessage)` / `SetMessages(IReadOnlyList<PrintedMessage>)`；
  **尚未跑到辩论章节实测**（尚未复测）。
- 魔女手册 / 系统菜单 / 标题菜单的键盘导航：`UiNav.Game.cs` 目前是通用规则，
  **未按本作对象名做别名表**；标题菜单按钮不是 `UnityEngine.UI.Button`（本作自定义 UI），
  逐控件名字来源需用场景树实查后再补（尚未实机复测）。
- 游戏内文本语言：本作标题画面默认日文（`StreamingAssets\config.json` 里
  `available_text_lang` = `ja` / `zh-Hans`）。补丁新增配置「文本语言」，
  用 `ILocalizationManager.SelectLocale` 切到 zh-Hans；**默认「跟随游戏」**，
  日文环境下的玩家需要自己切成「简体中文」（尚未实机复测切换效果）。
- 有配音时是否跳过朗读：本作语音 26083 条、覆盖率对白 98.2%，但
  **暂未做「有配音就只放语音」**（读屏玩家需要中文文本，日配中文不同步）。
  配置项保留但行为是全部朗读。
