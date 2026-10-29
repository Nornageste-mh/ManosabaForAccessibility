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
