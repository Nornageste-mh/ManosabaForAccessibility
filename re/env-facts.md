# 魔法少女ノ魔女裁判（manosaba）BepInEx 6 环境事实

> 维护者：env-probe（A11Y 模组团队）。所有结论都附证据；没有证据的地方明确标 "未验证"。
> 最近更新：2026-10-28

## 0. 一句话结论

**PASS**：BepInEx 6.0.0-be.788 + Il2CppInterop 1.5.3+PR#277 可以直接装在 Unity 6000.0.48f1 / IL2CPP
metadata 31.1 的 manosaba 上；interop 自动生成 157 个 dll；自写 BepInEx 6 IL2CPP 插件能被
Chainloader 加载、能绑 interop、能 `RegisterTypeInIl2Cpp` 注入 MonoBehaviour 并在 Unity 主循环里
持续运行。**不需要**更新 BepInEx、**不需要**手动跑 Cpp2IL。

## 1. 游戏与环境

| 项 | 值 | 证据 |
|---|---|---|
| Steam AppID | 3101040 | 任务书 / steam_appid.txt |
| 安装目录 | `D:\Steam\steamapps\common\manosaba_game` | — |
| 可执行文件 | `manosaba.exe`（672256 B） | `evidence/game-root-before-bepinex.txt` |
| Unity 版本 | `6000.0.48f1` | `LogOutput.log`: `[Info   :   BepInEx] Running under Unity 6000.0.48f1` |
| 运行时 | `[Info   :   BepInEx] Runtime version: 6.0.7` / `Runtime information: .NET 6.0.7` | 同上 |
| 后端 | IL2CPP（`GameAssembly.dll` 53917696 B / `baselib.dll`） | 目录清单 |
| IL2CPP metadata | magic `FAB11BAF`，version 31；Cpp2IL 判定 `31.1` | `[Info   :    Cpp2IL] [Library] Using actual IL2CPP Metadata version 31.1` |
| `global-metadata.dat` | 16087164 B | `manosaba_Data\il2cpp_data\Metadata\global-metadata.dat` |
| 应用信息 | productName=`manosaba`，Application.version=`1.1.2`，platform=`WindowsPlayer` | probe 插件日志 |
| Steam 客户端 | **在运行**（steam.exe PID 4816 起于 2026-10-27 17:11，另有多个 steamwebhelper） | `Get-Process steam` |

> 任务书原文说 "Steam 进程未运行"——**实测不成立**，Steam 客户端一直在跑。这不影响部署，但影响
> "是否需要 Steam" 的结论（见 G5）。

## 2. 部署内容（已写入游戏目录）

从 `D:\DSHWorkSpace\noexistence_a11y\mod\package\bepinex_be.zip`（34336405 B，233 个 entry）解压到
游戏根目录：

| 路径 | 说明 |
|---|---|
| `winhttp.dll`（26112 B） | doorstop 入口（BepInEx 6 用 winhttp 代理注入，**不是** winmm/version.dll） |
| `doorstop_config.ini`（1637 B） | doorstop 4.5.0 配置，默认 `enabled=true`、`target_assembly=BepInEx\core\BepInEx.Unity.IL2CPP.dll` |
| `.doorstop_version`（"4.5.0"） | — |
| `dotnet\` | 整个 .NET 6.0.7 运行时（BepInEx 自带，游戏不需要装 .NET） |
| `BepInEx\core\` | 38 个 dll（BepInEx.Core / BepInEx.Unity.IL2CPP / Il2CppInterop.* 等） |
| `BepInEx\plugins\` | 插件目录（ManosabaA11y.dll、ManosabaProbe.dll 等） |
| `BepInEx\config\` | 首次启动自动生成 `BepInEx.cfg` |
| `BepInEx\interop\` | **首次启动自动生成**，157 个 dll（见 `probe/interop-assemblies.txt`） |
| `BepInEx\unity-libs\` | **首次启动自动下载** `6000.0.48.zip`（2445772 B）并解出 Unity 6 模块 dll |
| `BepInEx\LogOutput.log` / `ErrorLog.log` | 运行日志 |
| `steam_appid.txt`（内容 `3101040`，无换行） | **仅为方便直接启动 exe；发布包必须排除** |

### 2.1 必须做的覆盖：Il2CppInterop.Runtime 用 PR#277 版本

zip 自带 `BepInEx\core\Il2CppInterop.Runtime.dll` 是上游 1.5.3（284672 B）。已用
`D:\DSHWorkSpace\noexistence_a11y\mod\package\Il2CppInterop.Runtime.dll`（287744 B，
ProductVersion `1.5.3+f0634efa9824a0c92ceab3dba9e6a21c249683a7` = PR#277）覆盖。
原因：Unity 6 / metadata 31 下 `Class::Init` 签名变化，上游 1.5.3 会在类型注入时签名耗尽。
**本次证据**：覆盖后 `[Info   :Il2CppInterop] Registered mono type ManosabaProbe.ProbeBehaviour in il2cpp domain`
+ `CLASSINJECT_OK`，说明 PR#277 生效。

### 2.2 未使用的参考资产

`D:\Steam\steamapps\common\The NOexistenceN of you AND me\BepInEx\unity-libs\2022.3.62.zip` 是 Unity
**2022** 的 base libs，与 Unity 6 不通用，**没有**拿来用；`6000.0.48.zip` 由 BepInEx 自动下载。

## 3. 启动与运行

### 3.1 启动命令

```
# 直接跑（推荐；Steam 客户端在后台即可）
D:\Steam\steamapps\common\manosaba_game\manosaba.exe

# 全团队标准方式（限时 + 自动收日志 + 自动杀进程）
pwsh -NoProfile -ExecutionPolicy Bypass -File D:\DSHWorkSpace\manosaba_a11y\probe\run_probe.ps1 `
     -TimeoutSec 600 -Tag mysession -Launch exe -PollSec 10
```

`run_probe.ps1` 参数：`-TimeoutSec`（默认 300）、`-GameDir`、`-LogOutDir`（默认
`D:\DSHWorkSpace\manosaba_a11y\probe\logs`）、`-Launch exe|steam`、`-Tag`、`-PollSec`、`-NoKill`。
行为：发现已有 `manosaba` 进程则 **exit 2 拒绝启动**（保证单实例）→ 删旧日志 → 启动 → 每隔
`-PollSec` 实时 tail 新日志 → 超时杀进程树（`taskkill /IM manosaba.exe /F` 兜底）→ 把
`LogOutput-<Tag>-<时间戳>.log`、`LogOutput-<Tag>.log`、`ErrorLog-<Tag>-*.log`、
`interop-list-<Tag>.txt` 复制到日志目录。

### 3.2 首次启动耗时（重要，别误判"没注入"）

首次启动要下载 unity-libs + Cpp2IL + 生成 interop，约 **110 秒**：

- Cpp2IL `[Info   :InteropManager] Cpp2IL finished in 00:00:31.4648882`（Processed 127775 OK）
- 之后 `Generating interop assemblies` 约 70 秒 → `[Info   :Il2CppInteropGen] Done!`

**在这 ~110 秒内 `BepInEx\LogOutput.log` 还不存在**，只有 `ErrorLog.log`（132 B，Steam 的
minidump 两行）和 `BepInEx\config\BepInEx.cfg`。等 `LogOutput.log` 出现才算插件阶段开始。
第二次及以后启动没有这一步，几秒内就进 `Chainloader startup complete`。

### 3.3 启动后日志的关键标志

```
[Message: Preloader] BepInEx 6.0.0-be.788 - manosaba ...
[Info   :   BepInEx] Running under Unity 6000.0.48f1
[Message:   BepInEx] Chainloader startup complete
```
有自写插件时还应看到（实测）：
```
[Info   :   BepInEx] 1 plugin to load
[Info   :   BepInEx] Loading [ManosabaProbe 0.1.0]
[Info   :ManosabaProbe] [ManosabaProbe] INTEROP_BINDING_OK
[Info   :Il2CppInterop] Registered mono type ManosabaProbe.ProbeBehaviour in il2cpp domain
[Info   :ManosabaProbe] [ManosabaProbe] CLASSINJECT_OK (RegisterTypeInIl2Cpp<ProbeBehaviour>)
[Info   :ManosabaProbe] [ManosabaProbe] == PLUGIN LOADED OK == probe behaviour 已挂到 ManosabaProbeHost
```

## 4. 构建工具链（插件侧）

- 机器上只有 **.NET SDK 8.0.404**（`D:\dotnet\sdk`），**没有 .NET 6 SDK**。
- 但 NuGet 全局缓存里已有 `microsoft.netcore.app.ref 6.0.36`，所以 `TargetFramework=net6.0`
  直接 `dotnet build` **可用**，**不需要** `FrameworkPathOverride`、**不需要**装 .NET 6 SDK。
- 插件工程不要用 `PackageReference` 引 BepInEx：全部用 `HintPath` 直接引游戏目录里的 dll
  （参考 `probe/plugin/ManosabaProbe/ManosabaProbe.csproj`）：
  - `BepInEx\core\`：BepInEx.Core、BepInEx.Unity.IL2CPP、BepInEx.Unity.Common、Il2CppInterop.Runtime
  - `BepInEx\interop\`：Il2Cppmscorlib、UnityEngine.CoreModule、UnityEngine.UIModule、UnityEngine.UI、
    Unity.TextMeshPro、Assembly-CSharp、Elringus.Naninovel.Runtime …（按需加）
- 构建：`dotnet build -c Release` → `bin\Release\<Name>.dll`（~19 KB）直接放进
  `BepInEx\plugins\`（可放子目录，BepInEx 会递归扫描）。

## 5. interop 产物（`BepInEx\interop\`）

157 个 dll（完整清单见 `probe/interop-assemblies.txt`）。A11Y 模组最相关的：

| dll | 大小 |
|---|---|
| `Assembly-CSharp.dll` | 1244160 |
| `Elringus.Naninovel.Runtime.dll` | 4736000 |
| `Naninovel.Common.dll` | 527360 |
| `Il2Cppmscorlib.dll` | 5808640 |
| `UnityEngine.CoreModule.dll` | 4509696 |
| `com.rlabrecque.steamworks.net.dll` | 1608192 |
| `Unity.TextMeshPro.dll` | 978432 |
| `UnityEngine.UI.dll` | 802816 |
| `UniTask.dll` / `UniRx.dll` / `Newtonsoft.Json.dll` / `Unity.InputSystem.dll` | 1881600 / 2100224 / 2042368 / 2724864 |

以及 15+ 个 `GigaCreation.*`（Essentials.Dialogs / Essentials.UI / NaninovelExtender.UI /
NaninovelExtender.ExtendedActors / Tools.Debugging.* 等）—— 游戏自定义框架大量扩展 Naninovel。

unstripping 统计（正常范围，不影响使用）：
`Restored 521 fields / Failed 13`、`Restored 17580 methods / Failed 1158`、
`method bodies: 10712 successful / 1478 failed`、171 potentially dead methods。

## 6. Steam 是否需要在线

- **实测**：Steam 客户端在运行 + 游戏根目录有 `steam_appid.txt=3101040` → 直接跑 exe 可正常启动，
  `ErrorLog.log` 只有两行正常输出：
  `Setting breakpad minidump AppID = 3101040` / `SteamInternal_SetMinidumpSteamID:  Caching Steam ID:  76561198668717305 [API loaded no]`。
- **未验证**：完全关掉 Steam 客户端后能否启动（无人值守场景不去杀用户的 Steam）。按给内部开发工具
  的常规做法，开发/取证都在 Steam 登录状态下跑，**不要**把"无 Steam 离线可用"当作结论。

## 7. 已知坑（G 编号，踩到就对照这里）

| 编号 | 现象 | 原因 / 对策 |
|---|---|---|
| **G1** | 首次启动后好几分钟都没有 `LogOutput.log`，以为没注入 | 正常：unity-libs 下载 + Cpp2IL + interop 生成约 110 s，此期间只有 `ErrorLog.log`。等，或看 `ErrorLog.log` 是否已有 Steam 两行（说明 doorstop 已进） |
| **G2** | Unity 6 上 `ClassInjector.RegisterTypeInIl2Cpp` 抛签名耗尽/Class::Init 异常 | `Il2CppInterop.Runtime.dll` 必须是 PR#277（287744 B）而非上游 1.5.3（284672 B）。见 §2.1 |
| **G3** | DLL 放进 `plugins\` 但日志只有 `0 plugins to load` | ① 插件目录放错（要在 `BepInEx\plugins\` 下，可递归）② dll 是 0 字节/依赖缺失（引用了游戏 interop 里没有的 dll）③ **`[BepInPlugin]` 版本号必须是纯数字四段**（如 `0.1.0.0`），否则 BepInEx 静默跳过；`1.0` / `1.0.0-beta` 都不行。排查手段：日志里搜 `Loading [`；再加一个只打印 `LogInterop` 的最小插件做对照 |
| **G4** | `transform is RectTransform` 永远 false | IL2CPP 下托管对象与 Unity 组件对象是两套包装，`is`/`as` 对 interop 包装类型不可靠。**必须** `var rt = t.TryCast<RectTransform>()`（`Il2CppObjectBase.TryCast<T>`），再判 `rt != null` |
| **G5** | 直接跑 exe 报 Steam 相关错误 | 先确保 Steam 客户端已登录并在跑；游戏根目录放 `steam_appid.txt`（内容 `3101040`）能显著提高成功率。**发布包必须排除该文件** |
| **G6** | 团队并发跑游戏互相抢实例 | 游戏同时只能一个实例。统一用 `probe/run_probe.ps1`（内含"发现已有进程则 exit 2 拒启"），跑完必杀；`taskkill /IM manosaba.exe /F` 兜底 |
| **G7** | 插件加载后 MonoBehaviour 不跑/日志乱码 | ① 注入前必须先 `RegisterTypeInIl2Cpp<T>()`；② `GameObject`/`DontDestroyOnLoad` 要在 `Load()`（主线程）里建；③ 事件订阅回调要保存托管委托引用，否则被 GC 回收；④ 源码文件用 UTF-8 保存、csproj 设 `<CodePage>65001</CodePage>` 避免中文日志乱码 |
| **G8** | interop 的 `IReadOnlyList<T>` 没有 `Count` | Il2CppInterop 只投影了接口自己声明的成员，`Count` 在 `IReadOnlyCollection` 上，没被投影。改判断非空或用索引器，必要时用 `Il2CppReferenceArray` 转换 |
| **G9** | 用 `-Launch steam` 方式启动 | `steam://rungameid/3101040` 依赖 Steam 客户端与库识别；无人值守优先 `-Launch exe` |
| **G10** | 想用 `ITextPrinterManager.add_OnPrintStarted(...)` 订阅台词，加载即抛 `System.ArgumentException: Delegate has parameter of type Naninovel.PrintMessageArgs (non-blittable struct) which is not supported` | **本作不可用，别再试 `DelegateSupport.ConvertDelegate`**（栈在 `Il2CppInterop.Runtime\DelegateSupport.cs:line 235`）。`PrintMessageArgs` 是 il2cpp ValueType，il2cpp 的委托桥只支持 blittable 参数。**唯一可行路线 = Harmony 补丁**（见 `re/probe-v3-findings.md`：9/9 目标全部 Patch_OK） |
| **G11** | 自动化脚本点 "New Game" 没反应：连续点 60 次、`PRINT #` 仍是 0，场景一直 `Boot` | 两个原因叠加：① **窗口不在前台**——Unity 只在窗口获得焦点时处理鼠标，后台进程 `SetForegroundWindow` 会被静默拒绝，必须先 `ShowWindow(SW_RESTORE)` + `AttachThreadInput(当前前台线程)` 再 `SetForegroundWindow` 并**校验 `GetForegroundWindow()` 是否真等于游戏 hwnd**；② **坐标**：1920x1080 下 "New Game" 可点区域中心 Lead 判读约 **(400,880)**，v1 用的 (450,950) 落在判定框边缘。已在 `probe/run5-automation.ps1` (v2) 里加"多候选坐标 + 焦点校验 + Enter/Space 兜底" |

## 8. 证据文件

| 内容 | 路径 |
|---|---|
| 部署前游戏根目录清单（516 条，可用来证明没改原始资源） | `evidence/game-root-before-bepinex.txt` |
| 裸启动（无插件）成功日志 | `probe/logs/LogOutput-run1-bare.log`（+ 带时间戳副本、ErrorLog） |
| 带插件启动成功日志 | `probe/logs/LogOutput-run2-probe.log`（+ 带时间戳副本） |
| interop dll 清单 | `probe/interop-assemblies.txt` |
| 探针插件源码 | `probe/plugin/ManosabaProbe/ProbePlugin.cs`、`ManosabaProbe.csproj` |
| 探针插件构建产物 | `probe/plugin/ManosabaProbe/bin/Release/ManosabaProbe.dll` |
| 场景树 dump（游戏生成，随 run 覆盖） | `BepInEx\ManosabaProbe\scene-tree-*.txt`；副本 `probe/logs/scene-tree.txt` |
| 可重复启动脚本 | `probe/run_probe.ps1` |
| 自动化驱动脚本（点 New Game 进 ADV + 连续推进；v2 含焦点校验与多候选坐标） | `probe/run5-automation.ps1` |
| run3 日志（v2 探针：Engine.Initialized / ITextPrinterManager / 第一条台词出现） | `probe/logs/LogOutput-run3-naninovel-20261028-182219.log` |
| run4 日志（v3 探针：GetService 四个管理器全部可用） | `probe/logs/LogOutput-run4-v3-*.log` |
| run5 日志（v4 Harmony 探针：9/9 PATCH_OK，但未进游戏故无 PRINT） | `probe/logs/LogOutput-run5-harmony-20261028-183438.log` |
| Harmony 探针结论（A/B/C 答复，含已确认/未确认分栏） | `re/probe-v3-findings.md` |

## 9. 实例占用记录（团队共用一台机器，唯一游戏实例）

| 时间 | 事件 |
|---|---|
| 2026-10-28 18:11:23 | run1-bare 启动（PID 9564） |
| 2026-10-28 18:16 前后 | run1 结束，进程被杀，日志已收 |
| 2026-10-28 18:17:18 | run2-probe 启动（PID 16604），验证插件加载 |
| **2026-10-28 18:21:18** | **run2 超时结束，实例释放**（18:22 确认 "NO manosaba process"） |
| 2026-10-28 18:2x | run3-naninovel 启动（v2 探针：Engine.Initialized / ITextPrinterManager / 场景树） |
| 2026-10-28 18:27:50 | run3 实例被主动 `Stop-Process`（PID 9260），日志已收 |
| 2026-10-28 18:28:2x | run4-v3 启动（v3 探针：四个管理器 GetService） |
| 2026-10-28 18:29:45 | run4 实例被主动 kill（PID 6888），日志已收 |
| 2026-10-28 18:34:38 | run5-harmony 启动（v4 Harmony 探针，PID 13620） |
| **2026-10-28 18:39:23** | **run5 结束（由 Lead kill），实例释放；18:4x 起 Lead 接管实例做 a11y 插件实机测试** |

> 占用规则：谁要跑，先 `Get-Process manosaba`；有空再起。跑完必须杀。长时间占用前在团队频道报一声。
