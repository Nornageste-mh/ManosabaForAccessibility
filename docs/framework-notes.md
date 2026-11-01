# Framework 施工笔记（ManosabaA11y / fw-skeleton 交付）

> 给 Lead 与后续逐作开发用。**只留可执行事实**，不抄 Framework 文档原文。
> 仓库：`D:\DSHWorkSpace\Framework`（只读，未做任何修改）。
> 本工程：`D:\DSHWorkSpace\manosaba_a11y`（`mod\` 是骨架，docs/probe/re 由全队共用）。
> 生成时间：2026-10-28，P2 骨架阶段。

---

## 1. 三层：必须包含 / 禁止修改

| 层 | 文件（本工程 `mod\src\ManosabaA11y\`） | 规则 |
|---|---|---|
| **L1 平台层** | `Speech.cs` `Nvda.cs` `Sapi.cs` `Zdsr.cs` `Hotkey.cs` `KeyEdge.cs` `GlobalKeys.cs` `UiNav.cs` `Repeat.cs` | **逐字节复用** Framework `platform\`。唯一允许的差异：`namespace A11yFramework` → `namespace ManosabaA11y`。**禁止改逻辑、禁止加 public API**。 |
| **L2 契约层** | `A11yHost.cs` | 逐字节复用 `contract\A11yHost.cs`（同上只换 namespace）。逐作**只填字段**，不改文件。 |
| **L3 逐作层** | `Plugin.cs`（含 `Patches` 类）、`TextProc.cs`、`Choices.cs`、`UiNav.Game.cs` | 全部可改。必须遵守：**唯一朗读出口是 `Repeat`**（禁止直接 `Speech.Speak`，`tools\lint_repeat.ps1` 会拦）；**不猜游戏 API**，每处补丁注释写依据。 |

L1/L2 硬清单校验：`tools\compile_check.ps1`（拷贝 L1+L2 到 `.compilecheck`、编译 + 静态断言 L1 八件齐全、`lint_repeat.ps1`）。

### ⚠ 已知上游缺口（新作必踩）

**`tools/new_game.py` 的 L1 列表漏了 `GlobalKeys.cs`**（第 141-142 行只有 8 个），
但 `KeyEdge.cs` 直接调用 `GlobalKeys.Reading/Held/SnapshotWords/FillOtherKeySnapshot/
AnyNewOtherKeyHeld/ResetPressed` → 新生成骨架必然 CS0103。
**已手工从 `Framework\platform\GlobalKeys.cs` 补入**（仅替换 namespace，行尾统一 CRLF，sha256 前 8 位 `6FB098E2` 源文件）。

### 生成器其它缺口（本工程已手工补齐）

1. `new_game.py` 不生成 `Choices.cs`，但模板 `Plugin.cs` 的 `TickUiNav` 调 `Choices.Update()` → 已建占位。
2. 模板 `UiNav.Game.cs.template` 是**裸 class 体**（无 using/namespace/class）→ 已包进
   `namespace ManosabaA11y { internal static partial class UiNav { ... } }`。
3. 模板 `Plugin.cs.template` 是 **Mono 版**（`BaseUnityPlugin` + Awake/Update）→ IL2CPP 已改成
   `BepInEx.Unity.IL2CPP.BasePlugin` + `PluginBehaviour : MonoBehaviour`。
4. 模板 `build.ps1/release.ps1` 是 **BepInEx 5 版** → 已改成 BepInEx 6 IL2CPP（见 §3）。
5. 目标目录非空（队友已有 docs/probe/re）时 `new_game.py` 拒写 → 先生成到 `.skelgen\` 再展平合并；
   `Copy-Item -Recurse` 到已存在目录会嵌套成 `mod\mod`，必须逐项拷。

---

## 2. IL2CPP 路线 csproj 关键属性

文件：`mod\src\ManosabaA11y\ManosabaA11y.csproj`（`net6.0`）

```
ManosabaGameDir  (Condition 为空 → 默认 D:\Steam\steamapps\common\manosaba_game)   ← build.ps1 -p: 覆盖
BepInExCoreDir = $(ManosabaGameDir)\BepInEx\core
InteropDir     = $(ManosabaGameDir)\BepInEx\interop
AssemblyName/RootNamespace = ManosabaA11y ; Version = 0.0.0.1
TargetFramework = net6.0 ; AllowUnsafeBlocks = true ; CodePage = 65001
LangVersion = 9.0 ; Nullable disable ; ImplicitUsings disable
AppendTargetFrameworkToOutputPath = false ; GenerateDocumentationFile = false
NoWarn = CS0169;CS0414;CS0649;CS0436;CS0437;CS1701;CS1702
可复现三行：Deterministic / IncludeSourceRevisionInInformationalVersion=false / EnableSourceLink=false
```

引用（全部 `<Private>false</Private>`）：
- `BepInEx\core\`：`BepInEx.Core` `BepInEx.Unity.IL2CPP` `BepInEx.Unity.Common` `0Harmony` `Il2CppInterop.Runtime`
- `BepInEx\interop\`：`Assembly-CSharp` `Il2Cppmscorlib` `UnityEngine.CoreModule` `UnityEngine.InputLegacyModule` `UnityEngine.InputModule` `UnityEngine.UIModule` `UnityEngine.TextRenderingModule` `UnityEngine.IMGUIModule` `UnityEngine.UI` `Unity.TextMeshPro`

注意：
- **不要用通配符引 interop**：生成失败会留 0 字节 dll，`HintPath` 指向它只会报「不是有效的程序集」。
  build.ps1 第 1 步会先查 5 个关键 dll 存在且非 0 字节。
- `UnityEngine.EventSystems` 没有独立 dll，类型在 `UnityEngine.UI.dll` 里。
- 本机 interop 现状：`BepInEx\interop\` 160 个文件；`Elringus.Naninovel.Runtime`
  (4736000)、`Naninovel.Common`(527360)、`GigaCreation.NaninovelExtender.ExtendedActors`(165888) 均已非 0 字节。

### net6.0 编译环境（本机实测可行，不需要装 .NET 6）

- 本机只有 SDK **8.0.404**（`D:\dotnet\dotnet.exe`）+ 8.0 运行时；`D:\dotnet\packs` 里没有 6.0 的 ref pack。
- **可行原因**：SDK 8 会把 `net6.0` 的引用程序集当 NuGet 包还原，本机
  `%USERPROFILE%\.nuget\packages\microsoft.netcore.app.ref\6.0.36` 已存在 ⇒ 离线可编。
- **不需要** `FrameworkPathOverride`，**不需要**借用游戏 `dotnet\` 运行时目录（那是运行期，不是编译期）。
- 冷机（没有 6.0.36 缓存）时会联网从 nuget.org 还原 `Microsoft.NETCore.App.Ref 6.0.x`。

命令（三条守卫见 §3）：
```powershell
cd D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y
dotnet build -c Release -v minimal -nowarn:MSB3277 -p:ManosabaGameDir="D:\Steam\steamapps\common\manosaba_game"
```

---

## 3. 脚本：参数与流程

### `mod\build.ps1`（6 步）
参数：`-GameDir`(默认游戏目录) `-BepInExVersion 6.0.0-be.788` `-BepInExZip` `-Il2CppInteropDll`
`-NvdaClientUrl` `-CacheDir $env:A11Y_CACHE` **`-NoDeploy`**。
1. 查 `manosaba.exe` + interop 完整性（5 个关键 dll 存在且非 0 字节）+ BepInEx 6 core 存在。
2. 取 BepInEx 6 基础包（顺序：`-BepInExZip` → `$CacheDir\package\bepinex_be.zip`
   → 本机 `D:\DSHWorkSpace\noexistence_a11y\mod\package\bepinex_be.zip` → 官方 URL 下载）→ 解压 `bep_dl\`。
3. NVDA x64 客户端（同上：缓存 → 本机已有 → 官方 URL）→ `nvda_dl\x64\`。
4. 守卫 + 编译：① `BepInPlugin` 版本可 `System.Version` 解析（本工程从 `const string Version` 取）
   ② csproj `<Version>` 与之一致 ③ `dotnet build ... -p:ManosabaGameDir="$GameDir"`。
5. 部署（**`-NoDeploy` 跳过**）：→ `<GameDir>\BepInEx\plugins\manosaba_a11y\`，
   拷 `ManosabaA11y.dll` + `nvdaControllerClient.dll`；**游戏在跑则轮询等退出（上限 180 秒），超时放弃**。
6. 组装 `package\`：保留自产 `安装说明.md`/`常见问题.md` → 铺 BepInEx 6 基础包 →
   叠修复版 `Il2CppInterop.Runtime.dll`（**按 SHA256 内容判断**，见假设 A2）→ 插件 dll +
   nvda 到 `BepInEx\plugins\manosaba_a11y\` 和包根 → 拷 `licenses\` → 红线自检
   （包内不得有 `BepInEx\interop`、`BepInEx\unity-libs`、`BepInEx\config\manosaba.a11y.reader.cfg`）。

### `mod\release.ps1`（4 步）
参数：`-GameDir`、`-SkipBuild`。1/4 调 `build.ps1 -NoDeploy`；2/4 校验 12 项必需文件 +
`*.md ≥ 2` + 禁止项；3/4 `tools\makezip.ps1` 打 `dist\ManosabaA11y-<版本>.zip` +
`ZipFile` 自检「无 `\` 条目」；4/4 打印发布清单（tag `v<版本>`）。

### `mod\tools\makezip.ps1`
`-Source <dir> -Zip <file>`。**禁止 Compress-Archive**（PowerShell 5.1 会写反斜杠条目）。
`.ps1` 必须 **UTF-8 带 BOM**（PS 5.1 读无 BOM 脚本按 GBK 解码 → 中文语法错误）。

---

## 4. 发布包：必须 / 禁止

**必须**：`winhttp.dll`、`doorstop_config.ini`、`.doorstop_version`、
`BepInEx\core\{BepInEx.Core,BepInEx.Unity.IL2CPP,BepInEx.Unity.Common,0Harmony,Il2CppInterop.Runtime}.dll`、
`dotnet\`（BepInEx 6 自带运行时）、`BepInEx\plugins\manosaba_a11y\ManosabaA11y.dll`、
`nvdaControllerClient.dll`（包根 + 插件目录各一份）、`licenses\`（含 `THIRD-PARTY-NOTICES.txt`）、
包根 `安装说明.md` + `常见问题.md`。

**禁止**：`BepInEx\interop\`（机器相关）、`BepInEx\unity-libs\`、
`BepInEx\config\<guid>.cfg`（用户数据）、probe dll、任何游戏资源/反编译产物。
版本号纯数字四段；tag `v<版本>`；**不**放进 git（`.gitignore` 已排除 dist/download/package zip）。

---

## 5. 文档模板清单（`Framework\docs\templates\` + 生成位置）

| 模板 | 生成到 | 说明 |
|---|---|---|
| `无障碍可行性验证.md` | 仓库根 | P0，章节：目标游戏 / 剧情文本存放 / 剧本统计（行类型、配音覆盖、分支、说话人）/ 运行时架构与补丁点（本作特有麻烦）/ 语气与可覆盖范围（**明确不做什么**）/ 复现方式 |
| `测试问卷.md` | 仓库根 | P4 十节；第 8 节「**回归：不装 / 关掉补丁游戏正常**」硬要求；未实测写「尚未实机复测」 |
| `安装说明.md` | `mod\package\` | 逐作重写（路径/键位/IL2CPP 首次启动慢） |
| `常见问题.md` | `mod\package\` | 跨作复用（已补 IL2CPP 两条：interop 生成慢、0 字节 dll） |
| `无障碍补丁流水线.md` | `docs\` | 全流程 P0-P8，不逐作改 |
| `README.md` 骨架 | 仓库根 | 章节：说明/安装/快捷键/已知问题/构建/目录结构/模组做了什么/踩坑/如何复现分析/合规/许可 |

`mod\licenses\`：24 个许可证文本 + **`THIRD-PARTY-NOTICES.txt`**（由
`Framework\licenses\THIRD-PARTY-NOTICES.example-noexistence-il2cpp.txt` 改写，
只换游戏名/包名/文件名；随包组件 = BepInEx 6 + Doorstop + HarmonyX + Mono.Cecil + MonoMod +
NVDA Client + Il2CppInterop(LGPL-3.0，**修改版**需说明)。**ZDSRAPI 专有、不随附**）。

---

## 6. 构建验证（本机实测，2026-10-28）

环境：Windows / .NET SDK 8.0.404 / 游戏已装 BepInEx 6.0.0-be.788 且 interop 已生成（160 文件）。

```powershell
# ① 只编译（快速回归）
cd D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y
dotnet build -c Release -v minimal -nowarn:MSB3277
# 输出：ManosabaA11y -> ...\bin\Release\ManosabaA11y.dll
#       已成功生成。 0 个警告 0 个错误   (exit 0, ~6s, 首次 restore ~0.3s)

# ② 完整构建（编译 + 部署 + 组装 package）
cd D:\DSHWorkSpace\manosaba_a11y\mod
pwsh -NoProfile -File .\build.ps1
# 1/6 interop + core 就绪 → 2/6 基础包复用 noexistence 的 bep zip
# → 3/6 NVDA dll 复用本机 → 4/6 版本守卫通过 → 编译 0 警告 0 错误
# → 5/6 部署 BepInEx\plugins\manosaba_a11y（等待运行中的游戏退出后成功）
# → 6/6 package 组装完成 (258 文件)   exit 0

# ③ 发布（校验 + 打 zip）
pwsh -NoProfile -File .\release.ps1
# 必需文件 12 项齐全 / 说明文档 2 份 / 未混入 interop|unity-libs|config|probe
# zip: mod\dist\ManosabaA11y-0.0.0.1.zip  34.17 MB  265 条目  无反斜杠   exit 0
```

产物：`mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll`（69120 字节，sha256 `1FA40C0C…`）、
`<GameDir>\BepInEx\plugins\manosaba_a11y\{ManosabaA11y.dll, nvdaControllerClient.dll}`（已核对与源同步）、
`mod\package\`、`mod\dist\ManosabaA11y-0.0.0.1.zip`。

**L1/L2 逐字节机械校验**（python 比较原始字节，只把两边的 namespace 归一到同一 token）：
10 个文件（Hotkey/KeyEdge/Nvda/Repeat/Sapi/Speech/UiNav/Zdsr/GlobalKeys/A11yHost）
**内容完全一致，唯一差异是 namespace 长度（13→12，因此文件短 1 字节）**，CRLF 行数也一致。
注：GlobalKeys.cs 是手工补拷的，补入时写成了 LF；已统一回 CRLF 再校验通过。

Framework 仓库未改动：`git status --porcelain` 为空。

**未验证**（诚实记录）：没有启动游戏，因此「插件真被 BepInEx 加载、启动播报出声」尚未实机复测
（游戏实例归 env-probe / Lead / naninovel-re 使用；部署时游戏正在运行、脚本按设计等待其退出）。

---

## 7. 本工程相对模板的改动（IL2CPP 适配，全部在 L3）

1. `Plugin.cs`：`BaseUnityPlugin` → `BasePlugin`；`Awake` → `override Load()`；`Update` →
   `class PluginBehaviour : MonoBehaviour` + `AddComponent<PluginBehaviour>()`（BepInEx 6 的
   `BasePlugin` 没有 Awake/Update）；`Speech.Init` 延到 Behaviour 首帧（参考实现同做法，避免
   Load 阶段初始化 NVDA/COM 失败拖死加载链）；首帧 `Repeat.Say("…无障碍补丁已加载")`。
2. `UiNav.Game.cs`：包进 partial class；`Resources.FindObjectsOfTypeAll(_nameMgrType)` →
   `...Il2CppType.From(_nameMgrType)`（IL2CPP 收 `Il2CppSystem.Type`）；`found[i] as Component` →
   `As<Component>()`（内部 `TryCast<T>()`，因为 IL2CPP 的 `as` 对代理类型恒 null，陷阱 I1）。
3. `Choices.cs`：新增占位（生成器缺口）；`Patches` 为空类（**P3 前不猜 API**）。

### 假设与决策（无人值守，待 Lead 复核）

- **A1** interop 引用取「core 5 + interop 10 具体 dll」，不用通配符；理由见 §2 0 字节 dll。
- **A2** 修复版 `Il2CppInterop.Runtime.dll` 按 **SHA256 内容**比对后叠进包：基础包自带
  `1.5.3+dbda1cb…`（284672 B），游戏目录与 noexistence 包里是 `1.5.3+f0634efa…`（287744 B，
  同为 PR#277 修复版）。编译引用游戏目录那份 ⇒ 包必须一致，否则「编时装时不同」极难排查。
  可用 `-Il2CppInteropDll` 覆盖；找不到不同内容时回退基础包并打印提示。
- **A3** `A11yHost.UseGlobalKeys = false`：本作窗口是否也拿不到键盘焦点**未实测**，先保守。
  若实机焦点异常，改一行即可（Plugin.Load 里）。
- **A4** 版本号 `0.0.0.1`；`CfgQuitNames` 默认 `"TODO"`、`CfgExcludeNames` 默认空 ——
  退出按钮名与开发者工具面板名**必须 P3 实查后填**，不许猜。
- **A5** 生成器临时目录 `D:\DSHWorkSpace\manosaba_a11y\.skelgen\` 为合并用中间产物，交付前应删除；
  Framework 仓库未改动。

---

## 8. 待办（交给后续逐作）

- P3：TextProc 规则 → 剧情朗读（Naninovel 打印事件，经 `Repeat`）→ 选项 + 数字键/限时 →
  UiNav 别名表与排除项 → 本作特有（手机界面等）。
- 实机验证：插件加载日志、启动播报、interop 生成稳定性、窗口焦点（决定 A3）。
- `docs\无障碍可行性验证.md` / `docs\测试问卷.md` 的逐作统计要等 text-extract 的数值，
  **不要替它编数字**。

---

## 9. 实机阶段回填（Lead，2026-10-29）

### 9.1 已推翻的旧假设

- **A3 已改**：实机证明窗口焦点可以拿到（`focused=True`），`A11yHost.UseGlobalKeys` 保持 `false` 仍是对的，
  因为补丁不需要全局热键就能收到键盘（游戏在前台时 Naninovel 自己轮询输入）。
- **A4 已改**：`CfgQuitNames` 默认值从 `"TODO"` 改为空串，并在说明里写清「本作退出按钮对象名尚未取证」；
  留 `"TODO"` 会把占位符塞进玩家配置。

### 9.2 新增铁律（本作实机打出来的，建议回流 Framework 文档 §7 陷阱表）

1. **不要 patch 返回 `UniTask` / `Task` / 其它非 blittable 结构体的 Naninovel 方法。**
   `TextPrinterManager.Print` 一挂，`PrintText.Print` 的 `await` 就抛
   `MethodAccessException: ... 'Naninovel.Async.IAwaiter.get_IsCompleted'`，
   命令永不结束 → 剧情卡在当前行、任何输入都推不动（但文本仍被 Prefix 念出来，极易误判成输入问题）。
   二分开关：`MANOSABA_A11Y_NOPATCH=print`。
2. **做「无补丁对照组」时，插件必须挪出游戏目录。**
   BepInEx 递归扫描 `BepInEx\plugins` 的子目录，挪进 `plugins\_stash\` 仍会被加载，
   会得出「无补丁也卡」的错误结论（本项目为此浪费了一轮排查）。
3. **自动化点击不能当验收路径。** 本作 6 FPS + 新输入系统（`InputSystem.QueueStateEvent` 可注入但
   进正片成功率不稳），所有结论都以「真人点击 + 日志核对」为准。

### 9.3 交付状态

- 对话/旁白朗读 + 说话人：**实机通过**（31 句连续、无异常）。
- 选项 / 辩论 / 系统菜单导航：补丁已挂，**尚未跑到对应剧情位置实测**，见 `CHANGELOG.md`。

## 10. 第二轮回填（0.0.0.1，参考《不存在》仓库后的补充）

### 10.1 输入后端：Unity 6 + 只启用 Input System 的作

- 实机探针原文：`You are trying to read Input using the UnityEngine.Input class, but you have
  switched active Input handling to Input System package in Player Settings.`
  → 平台层所有裸 `Input.GetKey*`（热键、导航、选项数字键）在本作会**整体抛异常或静默失效**，
  必须 `A11yHost.UseGlobalKeys = true`（`GlobalKeys` 用 Win32 `GetAsyncKeyState` 兜底），
  逐作层按键也走平台层 `KeyEdge`。**新作接入第一步就应该探这个**（`MANOSABA_A11Y_PROBE_UI=1`
  里试一次 `Input.GetKey` 即可）。
- 本作实测约 6 FPS（`Time.deltaTime≈0.165`），按键必须**按住 ≥0.4s** 才能被逐帧采样命中；
  90ms 的模拟按键会整个落在两帧之间。
- 自动点击进正片成功率不稳（`InputSystem.QueueStateEvent<MouseState>` 可注入，但标题就绪时机难卡），
  验收仍以真人点击 + 日志为准。

### 10.2 加载播报：轮询查不到就挂可见性回调

- 本作加载界面是 **Giga 扩展**：`GigaCreation.NaninovelExtender.Ui.LoadingUiExtended : CustomUI`，
  实机对象名 `LoadingUI`，方法 `HandleVisibilityChanged(bool)`。挂它的 Postfix 最稳；
  `Naninovel.UI.LoadingPanel` 轮询与 `Resources.FindObjectsOfTypeAll<Transform>()` 按名扫描
  在本作都拿不到（对象不在默认层级/未激活）。
- 参考作用 3 秒去抖；本作一次「新游戏」会连续显隐 4 轮，3 秒窗内全播了 → 放宽到 8 秒，
  并保留「必须打断」的语义（`Repeat.Say("正在加载。", true)`）。

### 10.3 选项链与去重

- `ChoiceHandlerPanel.AddChoiceButton` 只登记面板；**文字必须等按钮 TMP 写完再读**
  （每帧扫容器、内容变化才播报），且容器第 0 个子物体不一定是第一个选项。
- 面板淡入淡出会抖出一帧 `Visible=false`：旧实现在 `!live` 分支把去重键清零 → 同一组选项念两遍。
  正确做法：`!live` 只清按钮列表、不清 `_announcedKey`，再加 1.5 秒宽限。
- 数字键选择走 `KeyEdge.Pressed`（同样受 10.1 影响）。

### 10.4 版本策略（本项目）

- 当前 **0.0.0.1**；只有「玩家能感知的新功能」或「实机事故修复」才 +1 最后一段，
  纯文档/纯打包/纯内部重构不动版本号。

### 10.5 交付状态（第二轮）

- 对话/旁白 + 说话人：**实机通过**（单轮 82 句、0 异常）。
- 选项播报 + 数字键选择：**实机通过**（第 1 章第 5 幕，2 个选项，选中后走对应分支）。
- 加载播报：**实机通过**（`LoadingUiExtended` 钩子）。
- 菜单/设置中文名与当前值：别名表按探针取证建立（79 个 `Selectable`），**尚未逐项实机复测**。
- 审判辩论：**实机通过**（Trial01，8 句连续，`Reader.say[debate:add]`）。\n- 魔女手册 / 文本语言切换：**尚未实机复测**。

## 11. 第三轮：标题菜单导航的可重复验证

- 物理按键在自动化里**不可重复**：同样的 `Send-Key 0x09`（按住 420ms）在 run41 能进入导航、
  在 run42/run43/run45 一次都没进去（同一条标题截图、同一份日志只有 36 行）。
  结论与 §9 一致：自动化只能当辅助，不能当验收依据。
- 想稳定验证「导航朗读」这类纯 UI 链路，用**逐作钩子合成输入**，而不是抢焦点发键：
  `MANOSABA_A11Y_TEST_NAV=1` → `A11yHost.NavTogglePressed = SyntheticNavPressed`
  （45s 后每 25s 返回一次 true）。这条路只改本进程的 UI 状态，不碰游戏逻辑，
  run44 稳定复现「进入 → 播报 → 退出 → 播报」。
- 顺带打掉的两个标签坑（都是逐作钩子能修、平台层不该修的）：
  - `GameTextIsNoise`：`TitleUI/Wrapper/VersionNumberLabel` 的 `Ver. 1.1.2` 会被
    「同行文字」退路拿去当按钮标签，整排按钮全念版本号。
  - `GameGroupName`：组根叫 `Wrapper`，不映射就只能兜底「面板」。

## 12. 第四轮：导航开关键被游戏吃掉 + 语音后端切换（0.0.0.2）

- **用户实机反馈**：标题画面出现后按 `Tab` 完全没反应；加载阶段按 `Tab` 有反应。
  说明游戏自己的标题菜单在抢 `Tab`（Naninovel 的菜单键 / UI Selectable 导航）。
  - 平台层不能改，走逐作钩子 `A11yHost.NavTogglePressed`：主键改成 `Alt+Shift+F1`
    （`Hotkey.Down`），`Tab` 降为备用键（`KeyEdge.Pressed`），两个都在配置里可改。
  - 教训：**平台层的默认键位一定会遇到「游戏自己也用这个键」**。逐作层必须留一个
    「换键 + 保留旧键」的口子，且默认值要挑游戏不会碰的组合键。
- **「没声音」≠「没生效」**：用户的日志里 15 条 `[UiNav] 导航开关`、18 条 `[朗读]`
  证明导航与朗读都在跑，只是听不到。根因是 `语音后端 = 自动` 在同装 NVDA + 争渡的机器上
  按平台顺序挑中 NVDA。修法两条，都不改平台层：
  1. 启动播报带后端名（`… 无障碍补丁已加载。语音后端 争渡读屏`）；
  2. `F7` 运行中循环切换后端并念出新名字（`Speech.CycleAvailable()` 原本没有任何热键接线）。
- **键盘链路诊断开关**：`MANOSABA_A11Y_KEYDIAG=1` 每 0.5 秒打印
  `reading/available/up/down/enter/tab/f1/alt/shift` 的 `GlobalKeys` 读数。
  这次它一次就定位了「方向键没反应」的真凶：**补丁没问题，是测试脚本的问题** ——
  `keybd_event` 对方向键/Home/End 这类**扩展键**必须带 `KEYEVENTF_EXTENDEDKEY (0x0001)`，
  否则系统按小键盘键处理，`GetAsyncKeyState(VK_DOWN)` 恒为 0（实测 `0x0000`）。
  修正后 run50 实测：`系统设置，按钮。2 / 5` → `鉴赏模式，按钮。3 / 5` → `Home` 回第 1 项。
  - 一般化教训：**「按键没生效」要先用一个独立读数确认按键有没有到达进程**，
    再去怀疑业务逻辑；否则容易在正确的代码上反复改。

## 13. 第五轮：退格重读 / 配音闸门 / 隐藏控制台（0.0.0.3）

用户要求（原话）：「我们的按下退格朗读上一句(仅包含剧情文字和选项)呢？」
「移除/隐藏控制台窗口」「有语音的部分不朗读(可被TTS重复朗读)也是我们每一个仓库的标配」。

**结论一：文本主钩子换成 actor 级 `UITextPrinter.AddMessage`。**
本作 ADV 路径**从不调用** `Naninovel.UI.UITextPrinterPanel.AddMessage` ——
面板级补丁挂了、`patchedMethods` 也算上了，但实机 `Reader.say[reveal] id=` 全是空
（run56：`voiced=0`）。真正被调的是 `Naninovel.UITextPrinter.AddMessage(PrintedMessage)`
（ilspycmd 实查返回 `void`），行 id 只能从 `message.Text.parts[i].id` 拿。

**一般化铁律（比 0.0.0.1 那条更细）：**
- patch 之前先看**返回类型**：`UniTask`/`Task`/非 blittable 结构体一律不挂；
  `void` + 简单参数（`string`/`bool`/托管引用）安全。
- 「挂上了」≠「会被调用」：`patchedMethods` 只说明 detour 装好了，
  必须再用一条实机日志证明命中次数 > 0。本作有两条补丁就是「装着但永不触发」。

**结论二：退格重读不能直接复用平台层 `Repeat.Again()`。**
平台 `Repeat` 是所有朗读（含界面导航「系统设置，按钮。2 / 5」）的公共出口，
用户要的是「仅剧情文字和选项」→ 逐作层自己维护 `Reader.Remember/LastLine`，
导航播报只走 `Repeat.Say` 不入这条缓冲。有配音的行也要 `Remember`（语音错过了，
退格把文本念一遍正是读屏用户要的）。

**结论三：功能键必须把「解析结果」写进日志。**
`Hotkey.Down("Backspace")` 对认不出的键名**静默返回 false**，症状是「按了没反应、
日志一片空白」——这正是 0.0.0.3 第一次实机退格失效的排查成本（最后发现是
测试脚本只按了 90ms、被 6 FPS 吞掉，但当时无法从日志区分）。
现在 `Load()` 会打「重读按键=Backspace -> KeyCode.Backspace」等三行。

**结论四：隐藏控制台要先校验窗口类名。**
`GetConsoleWindow()` 在本作实测返回的句柄与游戏主窗口**相同**（run55：两者都是 16717180），
直接 `ShowWindow(SW_HIDE)` 会把游戏窗口一起藏掉，表现为「点了 10 次没有任何反应」。
现在必须 `GetClassName == "ConsoleWindowClass"` + `GetWindowThreadProcessId` 才动手。

**实测证据（0.0.0.3）：**
- run57（Adv01）：`say=42 voiced=8 重读=2`，`Reader.voiced[reveal] id=0101Adv02_Ema001`
  → `[朗读] 樱羽艾玛`，`重读: 唯有这句话不管怎样都说不出口`。
- run58（Adv02）：`say=33 voiced=22 重读=3`，`重读: 樱羽艾玛：牢、牢房……！？为什么！？`
  —— 有配音的行可以被退格用 TTS 重读，满足用户「可被TTS重复朗读」的要求。
- 两轮均为 `隐藏控制台窗口 hwnd=… ok=True`，且游戏窗口未被误隐藏（6 次点击进正片）。

**仍未做（诚实记录）：** 魔女手册朗读/页签、存读档界面、关键选项「重要选项」提示、
快进经过的行能否重读 —— 都还没实机复测。

## 14. 第七轮：未命名控件（魔女手册）+ 审判实机 + 配音报名字默认关（0.0.0.4）

**用户反馈的三件事**（m03529）：「有很多未命名控件，用 REA 继续研究」「魔女审判暂时还没有测试」
「有配音文字朗读角色名需要默认关闭」。

### 14.1 未命名控件的根因不是"没取名字"，是"取不到激活文字"

- 取证：用户自己那轮日志里 44 条 `[UiNav] 未命名控件`，全部集中在魔女手册：
  `Tab_Maps` 17、`Tab_Notes` 10、`WitchBookButtonUI/Wrapper/Button` 9、
  `Page_Clues/.../PresentEvidenceButton/Button` 5、`Tab_Rules` 3。
- 平台 `ResolveLabel` 的第一顺位是 `OwnTextOf`（只看**激活**的 `TMP_Text`）。而本作页签标签
  挂在 `WitchTrialsTabToggle._defaultLabel/_highlightedLabel`（`CanvasGroup`）下，
  未选中的页签文字是未激活子物体 —— 于是平台层拿到空串，落到类型名。
- 改法（逐作层内，未动平台）：`GameLabelOf` 再降一级 `InactiveTextOf(transform, 3)`，
  用 `GetComponentsInChildren<TMP_Text>(true)` 取第一段 ≤24 字、非噪声的文字；
  **Slider/Dropdown 必须跳过**，否则会把「100%」「简体中文」这种当前值当名字。
- 交叉验证：`UiProbe` 给每个 `UIPROBE item` 打 `resolved='…'`，run60 显示
  `Tab_Clues→线索`、`Focus(Page_Maps/F1_1/Infirmary)→医务室`、`ButtonNext→下一页`。

### 14.2 组名要"先看组根自己"，否则十个页签组全叫「魔女手册」

第一版 `GameGroupName` 从组根向上找 `GroupAlias`，结果 10 个页签组都命中祖先 `WitchBookUI`。
改成：先看组根自己（`GroupAlias`，或以 `Tab_` 开头则 `NameAlias + "页签"`），再向上。
run61 播报：`导航模式。第 1 / 10 组：记录页签，1 项。全部 10 组：【记录页签 1 项】；规定页签 1 项；…`。

### 14.3 审判（魔女审判）实机结论

- 辩论文本走 `DebateTextPrinterPanel.AddText(string)`，**没有 `PrintedMessage`**，所以辩论行
  没有行 id → 配音闸门不生效（辩论台词本来也几乎都有配音，实际影响小）。
- 辩论不报说话人：界面没有可靠名字来源，宁可只读正文，不张冠李戴。
- 审判倒计时与 HP 条不是 `Selectable`，导航扫不到 —— 目前**不主动播报**（属于"可感知性缺口"，
  不是 bug；要做得另挂 `WitchTrialsNaninovelManager.OnRemainingTimeChangedAsObservable`，
  但它是 `IObservable` 的 IL2CPP 转换，成本高且容易引入噪声，本轮未做）。
- 「跳过辩论」按钮有名字：`DebateUI/Wrapper/SkipButton/Button → 跳过辩论`（run59 UIPROBE）。

### 14.4 方向键逐项移动仍然没能在自动化下复测

`keybd_event` 合成方向键/PageDown/HOME 这类扩展键需要 `KEYEVENTF_EXTENDEDKEY(0x0001)`，
即使加了标志，6 FPS 下 420ms 按住仍常常落在两帧之间；KEYDIAG 232 行里只稳定看到
`alt=True shift=True`，`up/down` 恒 False。结论：**这条路只能玩家自己试**，
代码路径与已实测的标题菜单完全一致（`KeyEdge.Pressed` → `GetAsyncKeyState`）。

### 14.5 "有配音时也报说话人"改默认关

用户要求「有配音文字朗读角色名需要默认关闭」——默认值改 `false`（`CfgAnnounceOnVoiced`）。
注意 BepInEx 的 `ConfigEntry` **不会迁移老配置**：玩家已有的 cfg 里仍是 `true`，
文档必须写「升级后自己确认这一行」。

## 15. 第八轮：开发者面板（Ctrl+Alt+Shift+F3）——发行版根本没有（0.0.0.6）

玩家希望用游戏内置开发者面板减少测试负担。结论：**发行版里不存在可用面板**。

- `DebugOptionsBase.s_isInitialized == true`，但 `s_debugManager == null`：游戏在发行构建里
  不创建调试管理器（`DebugManager(bool initialMode)` 从未被 new 出来）。
- `Resources.FindObjectsOfTypeAll<Ui.DebugPanel>()` 只拿到 1 个 **prefab 资产**
  （`gameObject.scene.IsValid() == false`、`parent=Wrapper`、`activeInHierarchy=false`），
  面板里的 `DebugPanel._contents=[Content]`、`_showButton=ShowButton`、`_hideButton=HideButton`
  都是 prefab 内部引用；`EnableDebugModeButton` 同理（`_debugManager` 也是 null）。
- 场景根上那个叫 `Debug` 的对象（`Transform + 5 components`）实测 **没有任何子物体**，
  不是面板挂载点。
- 组合键读取本身可以打通：`Plugin.SpecDown("Ctrl+Alt+Shift+F3")`（Win32 `GetAsyncKeyState` 兜底）
  在 run77–run82 稳定打印 `DevPanel: IsDebugMode -> true/false`；但无论补建管理器、翻转
  `IsDebugMode`、还是把 prefab 实例化挂到场景 Canvas（`MovieUI`，sortingOrder=9999），
  画面上都不出现面板 —— 因为这只 prefab 依赖开发构建里的完整层级/画布。
- 因此：配置「允许激活开发者选项」保留但**默认关闭**（玩家明确要求），发行版按了也不会
  出面板；文档里写明「这是开发构建功能」。

**教训**：看到「游戏自带调试工具 + 有 prefab」不等于发行版能用；先查
`DebugOptionsBase.s_debugManager` 是否被创建、prefab 是否真的实例化（`scene.IsValid()`），
再决定要不要投入做入口。别把「实例化 prefab」当成万能钥匙——UI 层级缺画布时它照样不可见。

## 16. 第九轮：自动化测试套件（不随发布包）

用户需求（m04662）：「剧本百万级，手动完整测试不现实，希望只测试不同界面/剧情阶段/场景/模块
是否符合预期。」据此建立两层测试：

1. **单元层（插件内进程自测）**：`mod\src\ManosabaA11y\SelfTest.cs`，`MANOSABA_A11Y_SELFTEST=1`
   时启动 30 帧后跑 22 条断言（TextProc 清洗/行 id/指纹、SpeakerNames、VoiceLineIds、Hotkey 解析），
   输出 `SELFTEST PASS/FAIL <名字>` 与 `SELFTEST SUMMARY total=.. pass=.. fail=..`。
   为什么放插件里：IL2CPP 下这些类型进程外引用不到，用真实运行时 + 真实数据跑才可信。
2. **集成层（每个用例一次游戏生命周期）**：`tests\Suite.ps1` + `tests\lib\Harness.ps1`，
   真点标题「新游戏」进正片 → `MANOSABA_A11Y_JUMP` 跳到目标剧本 → `Advance-Until` 边点边等 →
   断言日志证据行 → 出 `tests\reports\<Tag>-report.md` / `-result.json` / `shots\*.png`；
   退出码 = 失败数。

四条新教训（都已写进 Harness 注释）：

- **`MainWindowHandle` 会拿到 BepInEx 控制台窗口**（同进程）→ 必须 `EnumWindows` + 排除
  `ConsoleWindowClass`，否则点击全打在控制台、用例集体假失败。
- **`LogOutput.log` 不能直接 `ReadAllText`**（BepInEx 句柄不允许默认共享）→ 必须
  `[IO.File]::Open(..., FileShare.ReadWrite|Delete)`。第一版「游戏在写、套件读空」就是这个。
- **单元素数组会被 PowerShell 拆成标量**：`Get-LogMatches` 只命中一条时 `$Matches[0]` 变成取第一个
  字符（证据行显示成 `C`）→ 一律 `@()` 包住。
- `pwsh -File ... -Only a,b` 传进来是**一个**字符串，要自己按逗号拆。

其它落地细节：

- 用例级临时改 cfg：`Cfg = @{ '有配音时也报说话人' = 'true' }`，runner 启动前改、跑完还原
  （`Config.Bind` 在启动时读，进程内改 cfg 不生效）。
- 测试**不需要先开 Steam**（用户指正）：游戏目录有 `steam_appid.txt`，直接 `manosaba.exe`。
- 新增功能时顺手加一行带前缀的诊断日志，就等于给测试留了观测点 —— 这是本项目可测性的做法。

`tests\README.md` 里有用例清单、运行命令、新增用例模板与已知局限（不验证真实音频输出、
时序强场景只做证据行级断言、坐标依赖 1920×1080）。

## 17. 第十轮：整页界面——平台层判「点不到」时逐作层怎么办（0.0.0.7）

用户反馈（m05392）：魔女手册「记录」页**进去出不来**；「地图」页导航走不到地点。

根因不在我们的钩子，而在**平台层的可达性判定**：`UiNav.VisiblyClickable` 用「控件中心打一条
UI 射线、只认打到自己/子物体」来判断能不能点。手册打开时有一层全屏 CanvasGroup 盖在上面
（玩家实际能点，因为它只吃不拦），射线先打到遮罩 → `CloseButton`、地图按钮全判
`[画面外或点不到]`。另外地图地点 `WitchTrials.Views.MapAreaLabel` 只是 TMP 标签，
不是 `Selectable`，平台层根本不会扫。

平台层按「三条铁律」不能改，所以逐作层补一个 `ScreenAssist.cs`：

- **整页自动播报**：0.5s 扫一次「可见的界面根」（`WitchBookUI` / `LogUI`），换根或换页签
  才念，格式「界面名，页签。内容：…」。内容按 `Page_<页签>` 子树过滤，避免念到别的页。
- **地图页专用取法**：当前楼层容器（`F1_1`/`F2_1`/`F2_2`/`B1_1`）下的
  `MapAreaLabel._label`，**故意不看 CanvasGroup alpha** —— 实测 33 个标签全是
  `active=True visible=False`，alpha 为 0 只是没悬停，文字是齐的。
- **`Esc` 关闭**：每帧查一次（本作 6 FPS，0.5s 节流会漏按键），找到 `CloseButton` 直接
  `onClick.Invoke()`，念「已关闭魔女手册。」。
- 给自动化留钩子：`MANOSABA_A11Y_TEST_ESC=1` 走 `SimulateEscape()`（纯逻辑，不冒充按键）。

三条可复用的教训：

1. **「玩家能点，但平台判定点不到」是逐作层的活**：不要为了这个改平台层，也不要让玩家
   依赖导航——自己给一个直达入口（自动播报 + 专用关闭键）。
2. **可见性判定要按场景分开写**：同一页里有的文字靠 alpha 隐藏（`MapAreaLabel`，要忽略
   alpha）、有的靠 `SetActive`（楼层容器，要看 active），一刀切一定会念错或漏念。
3. **本机键盘注入不可用**（`SendInput` 返回 0）：所以按键类功能必须留进程内钩子，
   否则自动化永远只能验证「逻辑」，不能验证「按键→逻辑」这一段。
4. **客户区坐标 ≠ 屏幕坐标**：`SetCursorPos` 要屏幕坐标，改成相对点击时必须
   `GetClientRect` + `ClientToScreen`，否则窗口不在 (0,0) 时整套用例静默点空
   （本次 full1–full3 集体红就是这么来的）。同时给用例加「点一次→看日志→不行再点」
   的重试，别指望一次点中。
5. **`Wait-Log` 返回的是正则命中片段**，不是整行：断言要吃整行就得把 pattern 写成
   `'前缀[^\r\n]*'`，否则证据只剩前缀（`ScreenAssist: 播报 魔女手册` 看不到页签）。

## 18. 第十一轮：地图页选择地点（0.0.0.8）

本轮要解决的问题是「剧情让玩家在地图上选一个地点，读屏用户做不了」。四条经验：

1. **优先读游戏自己的数据，别自己猜规则**。一开始按「当前楼层 + 名牌 alpha」猜，run97 就把命令没允许的
   「图书室」念了出来，选它游戏会静默落回默认项。改读 `MapPage._restrictionAreas` 后，8 条与剧本
   `@choiceMapArea MapAreaNames:…` 完全一致。凡是「游戏已经知道答案」的判定，都先找它的字段/属性。
2. **interop 的 `IReadOnlyCollection<T>` / `IReadOnlyList<T>` 没有 `Count`**：与 `PrintedMessage` 同一个坑，
   要 `TryCast<Il2CppSystem.Collections.Generic.List<T>>()` 再取 `Count`/索引。
3. **合成 UI 事件不等于真实输入**。`onClick.Invoke()`、`ExecuteEvents.pointerClickHandler` 都返回成功，
   但本作（只启用新 Input System）不认，`OnChoiceMapAreaAsync` 永远不返回。必须
   `InputSystem.QueueStateEvent<MouseState>` 注入按下 + 抬起（`InputInject.Click`，且每帧 `Tick()` 补抬起）。
   这条经验同样适用于以后任何「点了没反应」的按钮。
4. **等待玩家输入的界面不要乱动**：选择等待期间点手册底部楼层卡会把 await 挂断，加载界面永久卡死。
   凡是「游戏在 await 我们的操作」的状态，只允许做「完成这次操作」的动作，其它快捷键一律禁用或拦截
   （本轮把 `Esc` 与楼层切换都拦了）。

自动化方面：`MANOSABA_A11Y_TEST_MAPCHOICE=<序号>` 与 `MANOSABA_A11Y_TEST_ESC=1` 让用例能稳定走到地图选择态
（本机键盘/鼠标注入在自动化里不可靠，但插件内部的注入点击是可复现的）。用例 `map.choice` 断言四件事：
认出了选择态 + 播报含跨楼层分组 + `chosenAreaId` 写成 `String:RecreationRoom` + 剧情念到「娱乐室」。

## 19. 第十二轮：审判「出示证据 / 选证物」（0.0.0.10）

**怎么到达那一步**（这是本轮最大的收获）：不要想办法造存档，直接跳行。
`Naninovel.ScriptPlayerExtensions.LoadAndPlayAtLine(sp, path, lineIndex, 0)` 会把剧本真正落到
指定行；行号可以自己算出来 —— `Script.lines` 是 **public 字段**
`Il2CppReferenceArray<ScriptLine>`（`Lines` 属性返回的 `IReadOnlyList` 在 interop 里不是 `List`，
`TryCast` 不成功），每条命令行是 `CommandScriptLine`，其 `command` 字段是唯一一个命令：
```csharp
var lines = script.lines;                      // Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Naninovel.ScriptLine>
var csl = lines[i].TryCast<Naninovel.CommandScriptLine>();
if (csl?.command.TryCast<WitchTrials.Views.ChoiceEvidence>() != null) { /* 就是这一行 */ }
```
本例 `Act01_Chapter01_Trial04` → 第 **327** 行（688 行里只有 1 处）。然后用
`MANOSABA_A11Y_JUMP_LINE="Act01_Chapter01/Act01_Chapter01_Trial04|327"` 复现，五秒内到位。

**四条教训**
1. **`IsPresentEvidenceMode` 是假的**：真正的选证物界面里它也是 `false`。判据要读游戏自己的限制
   状态（手册可见 + `RestrictionCategories` 非空 + `Page_Clues` 激活），别信名字好听的布尔量。
2. **纯图标卡片的文字只能「点一下再看」**：`WitchBookItemButton` 的 `_label` 空、子树无 TMP，
   名字出现在选中后的页面标题里。点完必须**缓存**（否则每 0.5 秒重扫会反复枚举，日志刷屏）。
3. **WorldSpace 画布的事件相机要回退 `Camera.main`**：`WorldToScreenPoint(null, …)` 会算出负坐标
   （实测 `screen=-7,-2`），点击落到屏幕外，界面毫无反馈 —— 看起来像「按钮没生效」。
   注入前一定做屏幕范围检查，并把算出来的坐标写进日志（截图可能全黑，日志是唯一落点证据）。
4. **门控条件别写死成一种**：先用「类别里有 Clue」判，结果真正的选证物反而被挡掉（ev12 卡住）；
   改成看 `Page_Clues` 是否激活才稳。

**验收证据**：`probe\run124-evdialog.ps1` → 播报「请出示证据。可选的证物共 1 件：1、…」→
`数字键 1` → `注入点击 name=Button screen=309,250 canvas=WorldSpace` → 对话框播报 →
`对话框数字键 2` → `EVIDENCE dump … idx=191` → **`idx=197`**。

**交互设计（评审结论）**：数字键只负责选中，**出示由玩家按回车**，确认框的「是」永远玩家自己按 ——
无障碍层不允许替玩家做游戏内决策，也不允许扩大可达范围。

## 20. 第十三轮：8 份审计报告回填（0.0.0.10）

交付 0.0.0.9 之后派了 8 个**只读**子代理分头审计（文本清洗 / Harmony 覆盖 / 界面导航 / 三层交互竞态 /
L2 契约 / 配置项 / 测试套件 / 合规）。**只读审计 + 主代理统一改**这个流程很值得复用：子代理各自
只看一个面，能给出 file:line 级的证据，而不会互相踩文件。

**这一轮最值钱的四个发现**（都不是“功能没做完”，而是“做完了但悄悄不生效”）：
1. **配置项 Bind 了没人读**：4 个开关（限时选择时长 / 朗读手机消息 / 朗读手机表情 / 快进补念）
   全仓 0 读取点。Bind 成功 ≠ 生效，必须给「每个 Cfg 键至少一个读取点」加断言。
2. **`ConfigEntry.Value = true` 会落盘**：测试开关把玩家的 `界面诊断日志` 永久改成 true，
   而诊断日志含台词原文 —— 等于把玩家推去剧透。改成只设 `A11yHost.DiagOn`（纯内存）。
3. **测试套件的 Skip 是死代码**：`if ($Only.Count -gt 0) { … if (-not $Only …) { 过滤 Skip } }` ——
   条件自相矛盾，于是依赖真人点击的两个用例每次都跑（实测 582 秒）。这类“看起来在防护、
   实际永远不成立”的守卫，比没有守卫更危险。
4. **git 里躺着 89 MB 剧本文本**：`evidence/raw/**` 444 个 `.bin`（含台词原文）与反编译源码
   一直是被 `git add -A` 带进去的。`.gitignore` 只管“以后”，不管“已经进去的”，
   所以红线要靠 `git rm --cached` + `check-staged.ps1` 的禁用路径/扩展名双保险。

**四条可复用的判据**
- `TextProc` 类修复必须跑**全量语料回归**（440 个剧本 / 3.4 万块）：只看单个样例会漏掉
  1,353 处「的，」与 3,342 处「<br>　」缩进空格。
- 交互层（选项 / 地图 / 证物）必须**共用一把焦点门**：本作只启用新 Input System，全局读键
  兜底不看焦点，玩家在别的程序里敲数字就会把选项点了。
- 「先念后点」一律改成「先点后念 + 校验返回值」：注入点击会因坐标越界/WorldSpace 相机失败，
  这时不能说“已选中”。
- 审计报告里的条目要**分成“能定量验证”与“需要真人复测”两类**：这一版只收回前者，
  后者如实列在 CHANGELOG 的“本版未修”里。

## 21. 第十四轮：审判台词不自动朗读 + 重读键避让（0.0.0.11）

**两件事都是玩家实测反馈，而不是自己想出来的**：①审判论战的语音与 TTS 叠在一起；
②重读键 `Backspace` 与游戏自带的“返回上一句”冲突。两件事都不会在日志里报错，只能靠玩家反馈。

**为什么不能用现有配音闸门解决审判台词**：议论路径走的是
`DebateTextPrinterPanel.AddText(string)`，拿不到 `PrintedMessage`，也就没有行 id；
`VoiceLineIds`、`LineSpeakers` 这两张表全都依赖行 id。所以只能按**来源**
（`src.StartsWith("debate")`）闸门，而不能按行 id。

**关键顺序**：先 `Remember(line)`（进重读缓冲），再判议论闸门——防止“不朗读”顺手把
重读缓冲也省了（那就变成“既不读也重读不了”，比不做还差）。

**快捷键问题的经验**：`Enum.TryParse` 不认 `=`、也不认 `＝`。玩家把键名写成一个
看起来正常的符号时，得到的是“这个键永远不触发”，而不是一个错误。解法：
在 `Hotkey.Parse` 里做显式符号映射（`=` / `＝` / `Equals` → `KeyCode.Equals`），并在 `SelfTest` 里钉住。

**实机证据**：`Reader.skipped[debate] 只进重读缓冲: 我找到的证据……那就是死亡讯息！`；
`trial.debate` 用例 3 句命中、反向断言“`Reader.say[debate` 命中 0”；
启动行 `[a11y] 重读按键== -> KeyCode.Equals`。

## 21. 第十四轮：开发者选项改成「剧情跳转」（0.0.0.12）

**为什么改**：0.0.0.6 已证发行版里游戏自带的调试面板打不开；玩家要求「开发者选项 = 跳到指定剧情」。
跳转 API（`ScriptPlayerExtensions.LoadAndPlayAtLine`）在 0.0.0.9 已经实机验证过，
这轮只是把它变成**玩家可操作的菜单**。

**三条经验**
1. **能跳 ≠ 跳过去会继续播**：如果跳转时画面正停在选项（`ChoiceHandlerPanel` 有按钮），
   剧本状态会切到目标脚本（回读 `sp.PlayedScript.Path` 证实是目标脚本、`playing=True`），
   但台词不会立刻打印——游戏的等待输入状态还挂着。开发功能不替玩家做选择，
   做法是**提醒一句**：「画面刚才停在选项上，跳转后如果没声音，先按数字键选一项。」
2. **回读才是证据**：`LoadAndPlayAtLine` 是异步的，发出去不等于生效。
   `ScriptJump.VerifyJump` 在 3 秒后回读 `sp.PlayedScript.Path` / `Playing` / `PlayedIndex`
   并写进日志（`SCRIPTJUMP 跳转后: want=… got=… playing=True idx=0`），
   这样「跳转成功」是可验证的，而不是靠感觉。
3. **菜单自己要用数字键，就必须让选择层让开**：`FocusGate.CanReadChoiceKeys()` 在
   `ScriptJump.Active` 时返回 false，否则 1-9 会被选项/地图/证物层同时读走。

**数据源**：`mod\src\ManosabaA11y\ScriptIndex.cs` 由 `tools\gen_script_index.py` 从
`textassets` 索引生成（440 条**剧本路径**，不含台词原文），单元断言
`scriptindex.数量不少于 400` / `无重复条目` / `查不到返回 -1` 守住表本身。

## 22. 性能取证：为什么「低性能模式」要先测量（0.0.0.12 之后）

**测量工具**：`mod\src\ManosabaA11y\PerfProbe.cs`，`MANOSABA_A11Y_PERFDIAG=1` 打开。
输出三类行：`PERF static/device/camera`、`PERF 10s fps=… frameMs=… costs=uinav/reader/ev/screen`、
`PERF counts shadow/volume/particles/skinned/renderers/canvases`。
`MANOSABA_A11Y_PERFTEST=1` 会依次施加候选优化做对照（第 70 秒 URP 降级、第 140 秒关 TrueShadow）。

**实测结论（本机，ADV 场景）**
1. 帧率 **1.8~14 FPS**、帧时间 **71~570ms**，波动极大；`focused=True`（不是窗口失焦导致的掉帧）。
2. **改画面设置没有可测收益**：把 `renderScale 1.0→0.8`、`MSAA 4→关`、主光阴影关、
   软阴影关、`shadowDistance 150→0`、`cascade 4→1`、`additionalLights 8→0`、再关掉
   581 个 `TrueShadow` 组件，前后帧率都在同一噪声区间（≈8 FPS 均值）。→ **这台机器是主线程瓶颈，不是 GPU**。
3. 场景里 `TrueShadow` 组件有 **805 个**、`Volume` 0 个、粒子 0 个、渲染器 193、Canvas 77。
   所以「2D 视觉小说没什么可优化」这个直觉是错的：它是 UI/阴影堆出来的负担，但不在 GPU 上。
4. **我们自己的开销是可测的、也是可降的**：改造前 `ev≈5.7ms`（`BookIsEvidenceMode()` 每帧
   `Resources.FindObjectsOfTypeAll<WitchBookUi>()`）+ `screen≈3~10ms` + `uinav≈0.7ms` ≈ **10~13ms/帧**；
   把 Evidence 门控缓存到 0.4 秒、ScreenAssist 找不到界面时把扫描间隔放到 1.5 秒之后，
   **掉到 ev≈1.1~1.9ms、screen≈1.1~3.2ms**，合计 **2.5~5ms/帧**。

**两条可复用判据**
- 任何 `Resources.FindObjectsOfTypeAll<T>()` 都不许放在每帧路径上；用 `ScanGate.Ready(name, interval)`
  限频到 0.4~2 秒（人的感知分辨率远低于帧率）。
- 做「性能模式」前先量三件事：机器是 CPU 瓶颈还是 GPU 瓶颈、场景里到底有什么贵东西、
  **补丁自己占了多少帧预算**。否则很容易做出一个「关阴影关成马赛克、帧率一点没变」的功能。

## 23. 第十四轮：低性能模式（0.0.0.13）

**先量，再动手**。用户提「低性能模式」时，第一件事不是写降画质代码，而是量三样：
① 帧率/帧时间（`PERF 10s fps=… frameMs=…`）；② 场景里的昂贵对象（`shadow=803 canvases=77`）；
③ **补丁自己占多少帧预算**（`costs=uinav=… reader=… ev=… screen=…`）。
量完才发现：这台机器 8 FPS 且 `ev≈5.7ms screen≈3~10ms` —— 补丁自己就吃掉 10~13ms。

**分阶段实验证明画面不是瓶颈**：把 URP 压到最省、关掉 581 个 TrueShadow，
帧率仍在 ≈8 FPS 的噪声区间 → 主线程瓶颈。这条结论直接决定了产品形态：
「低性能模式」的主收益必须来自插件侧限频，画面降级只能是可选的附加档，
而且要允许逐项关掉（否则等于用画质换不到性能）。

**两件事可以带走**：
1. 任何 `Resources.FindObjectsOfTypeAll<T>()` 都不许放每帧路径（`ScanGate.Ready(name, interval)`
   是最小解法）；本作里它一次就是 5~10ms。
2. 「改画面设置」属于替玩家做决策：默认关、只提醒不自动切、关掉必须逐项还原
   （保存原始值 + 记录被关掉的组件实例，卸载时也还原）。

**验收**：`probe\run126-lowperf.ps1`（40s 开 / 75s 关）+ 套件用例 `perf.lowmode`。

## 24. 第十五轮：低性能模式从「一档」改成「四档循环」（0.0.0.14）

**用户反馈**（0.0.13 刚交付时）："优化似乎很有限，看看是否可以更激进？当前我们的状态是保守。
按下那个快捷键可以在关闭/保守/普通/激进之间循环切换。"

**为什么档位比复选框好**：玩家说不清自己机器该降多少，也没耐心逐项试。
一条阶梯 + 一个循环键，按一次听一句「低性能模式：普通。」就知道在哪一级。

**每档加什么**（`LowPerf.Apply`）：
| 档位 | 手段 |
| --- | --- |
| 保守 | URP：renderScale 0.8 / MSAA 1 / 关主光与软阴影 / shadowDist 0 / cascade 1 / 附加灯 0；关 TrueShadow |
| 普通 | 再：renderScale 0.65、`QualitySettings.SetQualityLevel(0)`、纹理半分辨率、抗锯齿关、阴影整体关、LOD 0.7、像素灯 0、关实时反射 |
| 激进 | 再：renderScale 0.5、纹理 1/4、关各向异性、关 VSync + `targetFrameRate=30`、停粒子、LOD 0.5 |

**实测（本机 兆芯 KX-U6780A + 核显，标题画面，每档 30 秒取 10 秒均值）**：

| 档位 | 10 秒均值 | 相对关闭 |
| --- | --- | --- |
| 关闭 | ≈10.5 FPS（12.1 / 8.1 / 10.6 / 11.0） | — |
| 保守 | ≈14.4 FPS（14.2 / 14.4 / 14.6） | **+37%** |
| 普通 | ≈17.3 FPS（17.3 / 17.2 / 17.4） | +65% |
| 激进 | ≈20.5 FPS（20.5 / 20.4） | **+95%** |

结论修正（重要）：0.0.13 时说「画面降级没有收益」是**错的**——那次是在 ADV 场景里测的，
而 ADV 场景 `shadow=803 particles=0`、主要成本在别处；**标题/演出场景画面成本高得多**，
此时「保守」一档就从 10.5 → 14.4。所以文档里要写清「收益取决于场景」，
不要再用一次实验的结论概括全游戏。

**三条工程纪律**
1. 换档 = `Restore()` 全量还原 → `Apply(newMode)`。逐项撤销容易漏（尤其从激进直接回关闭），
   全量还原会有一点卡顿，但保证「关掉就是干净的原样」。
2. 「降低窗口分辨率」**不进档位**：它改变窗口大小，读屏用户依赖固定窗口位置，
   所以单独一个默认关闭的开关，只有玩家自己打开才跟着生效。
3. 低帧率只提醒、绝不自动切档；插件卸载（`Shutdown()`）也要还原。

**验收**：`tests\reports\modes*-report.md` 的 `perf.lowmode`（四档阶梯：40s 保守 → 70s 普通 →
100s 激进 → 130s 关闭，12 条断言含三条「已还原」），以及 `probe\run126-lowperf.ps1`。
