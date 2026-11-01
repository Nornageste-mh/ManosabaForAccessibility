# ManosabaA11y

本仓库是**专门针对 Steam 游戏《魔法少女的魔女审判》（魔法少女ノ魔女裁判，Magical Girl Witch Trials，AppID 3101040）** 的屏幕阅读器
辅助补丁 —— 一个游戏内 **BepInEx 6 / IL2CPP 插件**（`ManosabaA11y.dll`），
台词与界面文字直接交给读屏 DLL（Tolk / NVDA Controller Client / 争渡读屏 ZDSR / Windows SAPI），
界面用键盘导航，通过向游戏自己的 Input System 投递真实输入事件来激活控件。

- 当前版本：**0.1.0.0**（见 `CHANGELOG.md`；这一版是第一个「可以长期放着用」的版本 ——
  控制台窗口从一开始就不创建，开局不再闪黑窗）

> ## 🎮 关于本作
>
> | | |
> |---|---|
> | 中文名 | **魔法少女的魔女审判**（日文原名 魔法少女ノ魔女裁判） |
> | 英文名 | Magical Girl Witch Trials |
> | 开发 | **Acacia**、**Re,AER** |
> | 发行 | **Re,AER**（中文地区：**CIRCLE LINE GAMES**） |
> | Steam | [AppID 3101040](https://store.steampowered.com/app/3101040/) |
> | 发售 | 2025 年 7 月 18 日 |
> | 引擎 | **Unity 6000.0.48f1**，IL2CPP，64 位；剧情引擎 Naninovel |
>
> **这个补丁是玩家自制的第三方工具，与 Acacia、Re,AER 没有任何关系，
> 也没有得到他们的授权或背书。** 游戏的著作权归 Acacia / Re,AER 所有，
> 本仓库只发布自己的补丁代码与文档，不含任何游戏资源。
>
> 脚注（哪些是本仓库亲自核实的）：
>
> - **开发 / 发行 / 发售日期**：由项目维护者在 Steam 商店页核实
>   （Steam 接口返回的开发者是 `Acacia`、`Re,AER`，发行是 `Re,AER`、`CIRCLE LINE GAMES`）
> - **引擎与位数**：本机核实 —— `manosaba.exe` 的版本资源里
>   `ProductVersion = 6000.0.48f1 (170d2541580d)`、`FileVersion = 6000.0.48.1510693`，
>   同目录有 53 MB 的 `GameAssembly.dll`（IL2CPP），进程是 64 位
> - **本仓库的结论都是在 Steam 正版上实测的**，未在其它版本或汉化补丁上验证过

---

> ## ⚠️ 郑重警告（请务必阅读）
>
> - 本项目是 **Vibe coding 产物**（AI 辅助生成），**非官方**，
>   与开发 **Acacia**、**Re,AER** **没有任何关系**。
> - **请务必支持正版：本辅助仅面向已在 Steam 购买《魔法少女的魔女审判》的玩家。**
>   **强烈要求每一位使用者通过 Steam 购买正版游戏。** 我们坚决反对任何形式的盗版、
>   破解、未授权传播；请勿将本工具用于协助获取或游玩盗版副本。
>   **这个补丁是让你能玩上这款游戏，不是让你不必买它** ——
>   Acacia 与 Re,AER 的劳动成果值得被正当地支持。
> - 本项目**不保证可用、不保证稳定**，代码可能存在各种问题（兼容性、稳定性、安全性等），
>   **无任何维护承诺**。使用风险自负，仅供个人学习/研究参考，请勿用于商业或分发牟利。
> - **建议在完全理解代码的前提下再使用**，自行承担一切后果。
> - 本仓库**不包含任何游戏资源**，只发布运行时补丁代码与文档。
> - **如您是 Acacia 或 Re,AER 的成员**，或本作的版权方，认为本仓库有任何不妥，
>   请联系我们，我们会立即配合调整或移除。

---

## 说明

本补丁的作用：让使用读屏软件的玩家，也能基本正常地游玩《魔法少女的魔女审判》。

- 游戏里的界面**不是 Win32 控件，是 Unity / UGUI 的 Canvas** —— 对读屏软件来说整个窗口是一片空白。
  补丁在游戏进程内挂 Harmony 钩子，从引擎的事件与对象里拿文本，
  并把「按一下」翻译成对目标控件的**真实输入**（走游戏自己的 Input System），
  所以**不需要读屏软件做任何特殊适配**。
- 朗读剧情：说话人 + 台词。**有配音的台词默认不自动朗读**（人声已经在读了），
  但按 `=` 仍能把这一句念出来。
- 朗读选项，并用数字键 `1`–`9` 选择（用的是**游戏自己给的选项列表**，不是猜的坐标）。
- 菜单、标题画面、魔女手册、对话记录可用方向键浏览、`Enter` 激活；`Esc` 关闭整页界面。
- 文本语言跟随**游戏的文本语言**（简体中文 / 日本語），也可以在配置里钉死一种。
- 语音后端按 `Tolk` → `NVDA` → `争渡读屏（ZDSR）` → `SAPI` 逐级降级；
  按 `F7` 可以在**当前可用**的后端之间循环切换（下次启动保持），详见
  [语音后端与切换快捷键](#语音后端与切换快捷键)。
- **不做什么**：不改游戏文件；不解包、不重打包游戏资源；不做 OCR（文本来自引擎事件与场景对象，不截图识别）；
  不朗读任何未经实机验证的推测内容；不上传任何数据。

**平台**：Windows x64。游戏是 64 位 IL2CPP 进程，所以 BepInEx、Il2CppInterop 与补齐的 .NET 6 运行时都是 64 位。

## 安装（仅限正版玩家）

安装 = **把发布包整体拷进有 `manosaba.exe` 的那一层**，不需要注入、不需要改任何游戏文件。
详细步骤（含「已经装过别的 BepInEx 模组怎么办」）见发布包里的 `安装说明.md`。

**为什么能这样装**：本作是 Unity 6 + IL2CPP，BepInEx 6 的注入靠 `winhttp.dll`（UnityDoorstop）
在游戏启动时先把 .NET 6 运行时（包里 `dotnet\`）拉起来，再由它加载 `BepInEx\plugins\` 下的插件。
补丁**没有改过游戏的任何文件**，卸载就是删文件（见下面的「还原」）。

**如果你本来就有 BepInEx 6，请先备份 `BepInEx\config\`** —— 包里那份 `BepInEx\config\BepInEx.cfg`
只用来关掉控制台，覆盖它顶多丢日志/控制台设置，但仍以先备份为准。

步骤：

1. **关掉游戏**（必须：文件被占用时拷贝会失败）。
2. 把发布包里的这些拷进游戏根目录（有 `manosaba.exe` 的那层）：

```
<游戏目录>\winhttp.dll
<游戏目录>\doorstop_config.ini
<游戏目录>\.doorstop_version
<游戏目录>\dotnet\                     ← 打包自带的 .NET 6 运行时（BepInEx 6 需要）
<游戏目录>\BepInEx\                    ← 核心 + 插件 + 配置
<游戏目录>\nvdaControllerClient.dll    ← NV Access 官方 x64 版（随发布包含）
<游戏目录>\安装说明.md 等文档
```

3. 启动游戏。**第一次会比较慢**：Il2CppInterop 要生成 `BepInEx\interop\`（160 个程序集），
   生成完再启动就快了；听到「无障碍补丁已加载」即成功。

**不要把 `BepInEx\interop\` 发给别人**（跟机器/游戏版本绑定）。若 `interop\` 里出现 0 字节 dll，
说明 interop 生成失败：删掉整个 `interop` 目录再启动一次，然后看 `BepInEx\LogOutput.log`。

发布包在 GitHub Releases 里（`ManosabaA11y-<版本>.zip`，约 35 MB —— 其中绝大部分是 BepInEx 6
与自带的 .NET 6 运行时），含 BepInEx 本体、插件与第三方许可证。**包里不含任何游戏文件。**

### 关于 BepInEx 控制台窗口

从 **0.1.0.0** 起，包里带一份 `BepInEx\config\BepInEx.cfg`，只固定了一个键：

```ini
[Logging.Console]
Enabled = false
```

所以控制台窗口**从一开始就不创建**，开局不会再闪一下黑窗（旧版是「建出来再 `ShowWindow` 隐藏」，
挡不住那一闪）。想看日志不需要控制台：`BepInEx\LogOutput.log` 一直在写。
如果你本来就有 `BepInEx.cfg`，**不要覆盖它**，手动把 `Enabled` 改成 `false` 即可。

## 还原

删掉安装时拷进去的东西即可。补丁不改动游戏文件、**不会留下任何残留**：

```
<游戏目录>\winhttp.dll
<游戏目录>\doorstop_config.ini
<游戏目录>\.doorstop_version
<游戏目录>\dotnet\                     ← 打包自带的 .NET 6 运行时
<游戏目录>\BepInEx\                    ← 若你原本没有 BepInEx（里面的 interop 缓存删掉无害）
<游戏目录>\nvdaControllerClient.dll
<游戏目录>\licenses\                   ← 第三方许可证，留着不碍事、删了不影响运行
<游戏目录>\安装说明.md 等文档
```

如果你**本来就有 BepInEx 6**（包里那份只是补全缺的文件），只删这三样：

```
<游戏目录>\BepInEx\plugins\manosaba_a11y\
<游戏目录>\BepInEx\config\manosaba.a11y.reader.cfg
<游戏目录>\BepInEx\config\BepInEx.cfg    ← 仅当它是安装时补的；想恢复控制台把 Enabled 改回 true
```

存档在 `C:\Users\<用户名>\AppData\LocalLow\Re,AER\manosaba\Saves_v1\`，补丁不读也不写存档内容。

## 主要快捷键

| 键 | 作用 |
|---|---|
| `1`–`9` | 选项 / 地点 / 证物：选择对应编号 |
| `0` | 立刻停止朗读（沉默键，不改变游戏状态） |
| `=` | 重读上一句剧情 / 选项（有配音的句子也会被念出来） |
| `Alt`+`Shift`+`F1` | 进入 / 退出界面导航模式（**主键**；`Tab` 在本作会被标题画面吃掉，见下） |
| `Tab` | 备用导航键（Boot/加载阶段有效；标题画面出现后会被游戏吃掉） |
| `↑` `↓` | 选择控件（念出名称与 `1 / N`） |
| `←` `→` | 调整滑条类控件 |
| `Home` / `End` | 首项 / 末项 |
| `PageUp` / `PageDown` | 切换面板组；地图名单超过 9 项时翻页；地图浏览态切楼层 |
| `Enter` / `Space` | 导航模式下激活当前控件（非导航模式下 = 推进剧情下一句） |
| `Esc` | 关闭魔女手册 / 对话记录这类整页界面 |
| `F7` | 循环切换语音后端（自动 → Tolk → NVDA → 争渡 → SAPI），念出结果；键位可在配置里改 |
| `Alt`+`Shift`+`F2` | 低性能模式五档循环：关闭 → 轻度 → 均衡 → 强力 → 极限 → 关闭（默认关闭；见下） |
| `Ctrl`+`Alt`+`Shift`+`F3` | 剧情跳转菜单（开发者功能，默认关闭，需先在 cfg 里打开「允许剧情跳转（开发者）」） |

游戏本体的原生键位（`Enter` / `Space` / 鼠标左键推进、`Esc` 菜单、`Ctrl` 快进）**不动**，补丁不去抢占。
补丁只在**界面导航模式**和**选项编号键**上接管输入。

> `0.0.0.11` 起默认重读键是 `=`（`Backspace` 会与本作自带的「返回上一句」冲突）；
> 审判议论的台词默认不自动朗读（带配音），但每句都进重读缓冲，按 `=` 一样能听。

**低性能模式（`0.0.0.16` 起是五档循环，默认关闭）**：按 `Alt`+`Shift`+`F2` 一轮一轮往上加，
听到「低性能模式：轻度 / 均衡 / 强力 / 极限」，再按一次回到「已关闭」（并把画面设置全部还原）。

| 档位 | 做了什么 | 实测（标题画面） |
| --- | --- | --- |
| 轻度 | URP 画质降级（渲染缩放 0.8、关 MSAA/主光阴影/软阴影/阴影距离）+ 关掉 800 多个柔边阴影 | ≈14.4 帧 |
| 均衡 | 再降渲染缩放 0.65、**窗口 0.85×**、质量等级最低、纹理半分辨率、关抗锯齿与阴影、LOD 0.7 | ≈17.3 帧 |
| 强力 | 再降渲染缩放 0.5、**窗口 0.70×**、纹理 1/4、关各向异性与实时反射、关 VSync 锁 30 帧、停粒子 | ≈20.4 帧 |
| 极限 | **窗口 0.50×** + 画面整个丢掉（相机不画、Renderer 停用、UI 刷黑，只剩黑窗口）+ 强制最低 LOD/单骨骼、关所有 URP Volume、相机后处理全关、**停所有 Animator**、锁 30 帧 | 比强力再高 |

（关闭档 ≈10.5 帧；这个表是测试机 兆芯 KX-U6780A + 核显、标题画面量的，收益取决于场景。）

**关于「CG/立绘/过场一出现就卡」（`0.1.0.0` 查证）**：这类卡顿是**主线程**造成的，
把画面降级（渲染缩放、关阴影、纹理 1/4、窗口缩小）几乎没用（实测 12.1 帧 vs 基线 13.5 帧）。
唯一沾边的是「界面动画」——但两轮测量互相矛盾（15.5 帧 / 8.1 帧），拿不到可靠结论，
所以只留了一个**默认关闭**的可选开关「性能 / 停止界面动画（强力档起）」，想试可以打开。
剩下的主要是游戏自己的逐字文本排版与 UI 重建，模组侧改不动。

**极限档为什么还能操作**：隐藏 UI 用的是「把每个 UI 图元的颜色刷黑」，不是关 Canvas/CanvasRenderer——
后两者会让 UGUI 的射线检测一起失效，点击就全废了（游戏软锁）。刷颜色不参与射线判定，
所以画面全黑时点击照样能推进剧情（`probe\logs\run127a-summary.txt`：黑屏期间点击 26 次，
朗读从 1 句涨到 13 句）。怕误触可以在配置里关掉「性能 / 允许极限档（画面全黑）」。

「降窗口分辨率」从 0.0.16 起**算进档位**（均衡 0.85× / 强力 0.70× / 极限 0.50×，都以原生分辨率为基准）。
不想让窗口尺寸变就关掉配置里的「性能 / 允许改变窗口分辨率」（默认开，关掉后其它手段照旧生效）。

**为什么导航不是 Tab 了**（2026-10-29 用户实机反馈）：本作标题画面出现后会把 `Tab`
当自己的菜单键吃掉，所以「主界面加载前按 Tab 有反应、加载后没反应」。主键因此改成
`Alt`+`Shift`+`F1`，`Tab` 保留作备用（配置「界面导航 / 同时允许 Tab」可关）。
两个键都能在 `BepInEx\config\manosaba.a11y.reader.cfg` 里改。

**听不到声音？先按 `F7`**：同时装了 NVDA 和争渡的机器，`语音后端 = 自动` 会按平台顺序
挑中 NVDA；如果你平时用争渡，就会一句都听不到。在游戏里按 `F7` 换到争渡即可，
启动播报也会念出当前后端名。

### 语音后端与切换快捷键

补丁启动时按 `Tolk` → `NVDA` → `争渡读屏（ZDSR）` → `SAPI` 的顺序找后端，
找到一个就用；任一级失败都只降级，不报错、不中止。

- **争渡排在 NVDA 之后、SAPI 之前**：原来用 Tolk/NVDA 的机器行为完全不变，
  只装争渡的机器才落到这一级（这类机器过去在三级链上大概率一片安静）。
- **争渡的可用性判据是 `GetSpeakState()` ∈ {3 正在朗读, 4 空闲}，不是 `Speak()` 的返回值** ——
  未授权时 `Speak()` 照样返回 0，只看它就会「插件自称正常、玩家一声不响」。
- **争渡的 dll 只用玩家自己安装的那份**：本补丁**不随包分发任何争渡 dll**，
  从争渡安装目录（注册表卸载项 / 默认安装目录）找；某一份 dll 版本对不上时如实判不可用，
  并给出人话理由 + 试过的路径（这段是念出来的，不会「装了争渡却一声不响」）。
- `F7` = 循环切换：**自动** → `Tolk` → `NVDA` → `争渡` → `SAPI` → 回到**自动**。
  **当前不可用的后端会被跳过**，并在播报里说明跳过了哪些；结果用**新后端**念出来；
  选「自动」时会连当前实际用的是谁一起念（例：`语音后端：自动（当前 SAPI）`）。
- 切换结果写回 `BepInEx\config\manosaba.a11y.reader.cfg`，下次启动保持：
  「朗读 / 语音后端」写死后端、「朗读 / 切换语音后端按键」改键位（默认 `F7`）。
- 配置里的后端**当时不可用时会自动回退**到自动链上的其它后端，不会静默变成「没有声音」。
- 修键位/后端不用重新编译：改完重开游戏生效。

## 当前进度（v0.1.0.0）

| 功能 | 状态 |
|---|---|
| 插件加载 / 启动播报（带语音后端名） | ✅ 实机验证 |
| 语音后端选择与运行中切换（`F7`） | ✅ 实机验证（`争渡读屏` ↔ `SAPI`） |
| 低性能模式（`Alt`+`Shift`+`F2`，默认关闭） | ✅ 实机验证（开启/还原、柔边阴影 579 个、低帧率提醒）；画面降级在本机无收益 |
| 对话朗读（含旁白与对白） | ✅ 实机验证（单轮连续 82 句无异常） |
| 说话人朗读（`樱羽艾玛：……`） | ✅ 实机验证（旁白不报名字） |
| 文本清洗（`<br>`/`<ruby>`/`<link>`/`<color>` 等） | ✅ 实机验证 |
| **有配音的行不自动朗读**（默认完全安静，`=` 可 TTS 重读） | ✅ 实机验证（run58：22 条配音行跳过，重读念回原文；v0.0.0.6 起默认连名字也不报） |
| **`=` 重读上一句剧情/选项** | ✅ 实机验证（run57/run58 用的是旧默认键 Backspace；0.0.0.11 起默认改为 `=`，已单元验证可解析）；导航播报不会被退回 |
| **BepInEx 控制台窗口** | ✅ **v0.1.0.0 起不创建**（随包 `BepInEx\config\BepInEx.cfg` 里 `[Logging.Console] Enabled = false`）——不是「建出来再藏」，所以开局连闪一下都没有；旧版靠 `ShowWindow(SW_HIDE)` 隐藏，实测必须先校验 `ConsoleWindowClass`，否则会把游戏窗口一起藏掉 |
| 选项朗读 / 数字键选择 | ✅ 实机验证（第 1 章第 5 幕，2 个选项，数字键选中后走对应分支） |
| 加载播报（点「新游戏」后的黑屏加载段） | ✅ 实机验证（`LoadingUiExtended` 可见性钩子） |
| 菜单 / 设置界面中文名与当前值 | ✅ 别名表按实机取证建立（探针转储 79 个 `Selectable`） |
| 标题菜单导航（组合键进入 / 组名 / 按钮名 / 方向键移动） | ✅ 实机验证：`导航模式，标题画面，共 5 项。退出游戏，按钮。1 / 5`；下拉键 → `系统设置，按钮。2 / 5`、`鉴赏模式，按钮。3 / 5`；`Home` → 回到第 1 项 |
| 审判辩论（`@printDebate`）朗读 | ✅ 实机验证（run59 完整审判段落：16 句朗读、34 条辩论日志；辩论不报名字） |
| 审判画面 HUD（跳过按钮） | ✅ `DebateUI/Wrapper/SkipButton/Button → 跳过辩论` |
| 审判倒计时 / HP（0.0.0.10 新增） | ⚠️ 已挂三个 void 补丁（`Timer.SetTime` / `Timer.ForceRemainingTimeToNearlyZero` / `DebateUi.OnCurrentHpChanged`），只在跨过 5分/2分/1分/30秒/10秒阈值时报一句；**未实机复测**（需打到审判战斗段） |
| **剧情跳转菜单（开发者）** | ✅ 实机验证（0.0.0.12）：`SCRIPTJUMP 打开菜单 index=17 共 440 个剧本` → 下移两项 → `SCRIPTJUMP 选中 index=19 path=Act01_Chapter01/Act01_Chapter01_Adv20` → `SCRIPTJUMP 跳转 path=…Adv20 line=0`；真实按键未复测（本机键盘注入被系统挡掉） |
| **魔女手册 / 对话记录整页界面** | ✅ 实机验证（run91）：打开即念「魔女手册，线索页签。内容：…」，地图页念「1F：会客厅、玄关大厅、…」；`Esc` 直接关闭并念「已关闭魔女手册。」（平台层射线被遮罩挡住，改由逐作层自己处理） |
| 魔女手册（图鉴）控件命名 | ✅ 实机验证（run61：10 个组 = 记录页签/规定页签/地图页签/人物页签/魔女手册 14 项/线索页签；地图 29 个地点全部念中文名） |
| 魔女手册方向键逐项移动 | ⚠️ 代码路径与标题菜单相同，但自动化合成扩展键不稳定，**未复测**（请实机按方向键确认） |
| **地图页选择地点（`@choiceMapArea`）** | ✅ 实机验证（run122）：读游戏自己的允许名单，一次念全 8 个地点（含 2F/地下1F 跨层），数字键 3/7/8 分别走食堂/娱乐室/牢房分支；名单超 9 项时 `PageUp`/`PageDown` 翻页 |
| **审判「出示证据 / 选证物」（`@choiceEvidence`）** | ✅ 实机验证（run124）：证物卡是纯图标，靠「点一张读标题」枚举出名字后播报「请出示证据。可选的证物共 1 件：1、二阶堂希罗原本持有的钢笔。按数字键选择。」；数字键=选中、**回车/空格=出示**（不替玩家按「是」），确认框照读文案与按钮；实测选「是」后剧本从 `idx=191` 走到 `idx=197` |
| 地图浏览态楼层切换（`PageUp`/`PageDown`） | ⚠️ 只编译验证：选择态禁用切层（会软锁），浏览态切层未单独复测 |
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
- **纯图标的卡片要自己补名字**：证物卡（`WitchBookItemButton`）既没有 `_label` 文字，子树里也没有
  TMP 文本，名字只出现在「选中之后页面右侧的标题」里 —— 只能点一张读一次，并且**必须缓存**
  （不缓存会每 0.5 秒重扫、反复枚举）。
- **WorldSpace 画布要回退 `Camera.main`**：`RectTransformUtility.WorldToScreenPoint(null, …)` 在
  WorldSpace 画布上算出来是负坐标（实测 `screen=-7,-2`），点击会落到屏幕外，而界面毫无反馈。
  注入点击前一定要做屏幕坐标边界检查。
- **`IsPresentEvidenceMode` 不可信**：真正的选证物界面里它也是 `false`。判据要读游戏自己的限制
  （`WitchBookScreen.RestrictionCategories` 含 `Clue` 且 `RestrictionAreas` 为空）。
- **无障碍层不能替玩家做决策**：数字键只负责「选中」，出示必须由玩家按回车触发；确认框里的
  「是」永远由玩家自己按键。
- `BepInEx` 会**递归扫描 `BepInEx\plugins` 的子目录**：把插件挪进 `plugins\_stash\`
  并不会禁用它，必须挪到游戏目录之外。做「无补丁对照组」时踩过这个坑。

## 自动化测试

剧本 34,836 个文本块 / 79 万字，人工全流程回放不可行；本项目改为**按界面/剧情阶段/场景/模块**做自动化验收：

```powershell
pwsh -File tests\Suite.ps1 -Only unit.selftest   # 单元层：纯逻辑 69 条断言（约 40 秒）
pwsh -File tests\Suite.ps1                       # 12 个默认用例：标题/对话/说话人/配音/退格/选项/辩论/加载/性能/整页界面/语言
```

- 套件共 14 个用例，其中 `evidence.present` / `map.choice` 标了 `Skip`（默认批次不跑，`-Only` 指定时才跑）。
- 单元层在插件进程内跑（`mod\src\ManosabaA11y\SelfTest.cs`，`MANOSABA_A11Y_SELFTEST=1`）：
  IL2CPP 下 `TextProc` 等类型进程外引用不到，放运行时里跑才可信。
- 集成层每个用例**独立启动一次游戏**，真点击进正片 → 跳目标剧本 → 断言日志证据行，
  产出 `tests\reports\<Tag>-report.md` + JSON + 截图；退出码 = 失败数。
- 测试**不需要先开 Steam**：游戏目录有 `steam_appid.txt`。
- 键盘类断言（`Esc`/重读）走插件内钩子 `MANOSABA_A11Y_TEST_ESC` / `MANOSABA_A11Y_TEST_REPEAT`：
  本机自动化环境的键盘注入被系统挡掉（`SendInput` 返回 0），鼠标注入可用。
- 细节、新增用例模板、已知局限见 [`tests/README.md`](tests/README.md)。

## 构建

```powershell
cd mod
.\build.ps1 -GameDir "D:\Steam\steamapps\common\manosaba_game"   # 编译 + 部署 + 组装 package\
.\build.ps1 -NoDeploy                                            # 只编译 + 组装包，不动游戏
.\release.ps1                                                    # 校验 + 打 dist\ManosabaA11y-<版本>.zip
```

前置条件：游戏已装 BepInEx 6.0.0-be.788 并**启动过一次**（否则没有 interop 可引用）；
本机 .NET SDK 8 即可编 net6.0（SDK 会从 NuGet 缓存还原 6.0 的引用程序集）。
首次构建要联网取 BepInEx 基础包与 NVDA Controller Client（可用 `-BepInExZip` / `-NvdaClientUrl` 指定本地缓存）。

构建脚本有三道守卫：**版本号必须纯数字四段**（`System.Version`，带字母会让 BepInEx 静默跳过整个插件）、
**csproj `<Version>` 必须与 `Plugin.cs` 的 `public const string Version` 一致**、编译失败即中止。
产物是 `ManosabaA11y.dll`（1,014,784 字节）；`release.ps1` 再校验发布红线（必需文件、不含 interop/源码/日志/探测产物）
并检查 zip 条目无反斜杠。

### 构建是可复现的

同一个源码、同一台机器、同一个检出路径下，构建产物是确定的 ——
**两次干净构建（各自先删掉 `obj\` 与 `bin\`）产出的 `ManosabaA11y.dll` 逐字节相同**：

| 项目 | 值 |
|---|---|
| 产物 | `mod\src\ManosabaA11y\bin\Release\ManosabaA11y.dll`（csproj 设了 `AppendTargetFrameworkToOutputPath=false`，没有 `net6.0\` 子目录） |
| A 次（先删 `obj`+`bin`） | 1,014,784 字节，SHA-256 `1471BB8A09C29A0097DCA68ABF7FBFC631C3AABD50575CE28711FAA3C3DEE1D2`，10.59 s |
| B 次（先删 `obj`+`bin`） | 1,014,784 字节，SHA-256 `1471BB8A09C29A0097DCA68ABF7FBFC631C3AABD50575CE28711FAA3C3DEE1D2`，6.71 s |
| A vs B | **逐字节相同**，差异 0 字节；伴随的 `ManosabaA11y.pdb`（98,844 B）与 `.deps.json` 也相同 |
| 与构建前产物 / 已部署副本 | 同一个 SHA-256 —— 本机共 3 次构建得到同一份二进制 |

能做到这一点是因为 csproj 里定了 `<Deterministic>true</Deterministic>` 与
`<IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>`
（不把 git 提交号塞进程序集版本，否则每次提交都会变）：
PE 的 `TimeDateStamp` 是 `0xE469FA50`（Roslyn 确定性哈希派生值，不是真实时间）。
所以你可以用上面这个 SHA-256 核对下载到的 `ManosabaA11y.dll` 是不是从这份源码编出来的。

**边界说明**：这个结论**以同一个检出路径为前提** —— DLL 里唯一内嵌的绝对路径是 CodeView
调试目录指向的 `D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\obj\Release\ManosabaA11y.pdb`（偏移 `0xF7418`），
换个目录或换台机器没实测过，不作承诺。要跨机器复现，把这份 PDB 路径写进
`Deterministic` 的 `<PathMap>` 即可。

### 目录结构

```
mod/
  build.ps1           构建：检查 interop → 取 BepInEx 6/NVDA 客户端 → 编译 → 部署 → 组装 package\
  release.ps1         校验发布红线 → makezip 打 zip → ZipFile 自检无反斜杠条目
  templates/BepInEx.cfg   随包的配置模板：只固定 [Logging.Console] Enabled = false
  src/ManosabaA11y/
    ManosabaA11y.csproj     net6.0 / AllowUnsafeBlocks / CodePage 65001 / Deterministic / ManosabaGameDir
    Plugin.cs               入口（BasePlugin）+ 全部配置项 + PluginBehaviour(MonoBehaviour) + 补丁装配
    A11yHost.cs             契约层（逐字节来自 Framework，不改）
    Speech.cs / Nvda.cs / Sapi.cs / Zdsr.cs      语音后端（平台层）
    Hotkey.cs / KeyEdge.cs / GlobalKeys.cs / InputSysKeys.cs   热键（Win32 GetAsyncKeyState + InputSystem）
    Repeat.cs               播报的唯一出口（L3 层不许直接调 Speech.Speak）
    UiNav.cs                界面导航扫描（平台层）
    UiVis.cs                可见性判定（沿父链查 CanvasGroup.alpha）
    TextProc.cs             文本清洗 / 说话人 / 该不该读（逐作）
    Reader.cs               朗读编排：pending 缓冲 + 去重 + 可见性 + 说话人前缀
    Patches.cs              Harmony 补丁点（每一处都注明实查依据）
    Choices.cs              选项面板：每帧扫容器、内容变化才播报、数字键选择
    Evidence.cs             审判「出示证据 / 选证物」：枚举证物名、确认框
    MapChoice.cs            地图页选择地点（读游戏自己的允许名单）
    ScreenAssist.cs         整页界面（魔女手册 / 对话记录）：播报内容 + Esc 关闭
    LoadingWatch.cs         加载界面播报
    Locale.cs               文本语言（跟随游戏 / 简体中文 / 日本語）
    SpeakerNames.cs         AuthorId → 中文名（CharacterNames.txt 实查）
    LineSpeakers.cs         逐行说话人表（实查）
    VoiceLineIds.cs         有配音的行 id 表（实查）
    ScriptIndex.cs          剧本索引（由剧本文件名生成）
    ScriptJump.cs           剧情跳转菜单（开发者，默认关闭）
    LowPerf.cs              低性能模式五档（渲染缩放 / 窗口 / 关阴影 / 极限档画面丢弃）
    PerfProbe.cs            性能探针（MANOSABA_A11Y_PERFDIAG=1）
    UiProbe.cs              界面取证探针（MANOSABA_A11Y_PROBE_UI=1）
    ScanGate.cs             扫描节流小工具
    UiNav.Game.cs           UiNav 的 partial 逐作区（别名表 + 输入框角色）
    InputInject.cs          仅测试用：InputSystem 状态事件注入（发布版不启用）
    TestDriver.cs           仅测试用：MANOSABA_A11Y_TEST=1 时自动走流程并 dump 状态
    SelfTest.cs             单元层自检（69 条断言）
  tools/makezip.ps1     打 zip（正斜杠条目）
  licenses/             第三方许可证 + THIRD-PARTY-NOTICES.txt（随发布包分发）
  package/              构建产物：直接拷进游戏根目录
tests/                  自动化测试套件（Suite.ps1 + lib\Harness.ps1 + 报告）
probe/                  运行时探针脚本与日志（日志不入库）
docs/                   设计笔记（framework-notes.md / 无障碍补丁流水线.md）
tools/                  仓库维护脚本（check-staged.ps1 等）
```

### 模组做了什么

| 功能 | 挂载点 |
|---|---|
| 插件加载 / 配置 / 每帧驱动 | `Plugin.Load()` + `AddComponent<PluginBehaviour>()`（BepInEx 6 的 BasePlugin 没有 Update） |
| 朗读出口 | `Repeat`（平台层唯一出口；L3 不许直接调 `Speech.Speak`） |
| 启动播报 | `PluginBehaviour.Update()` 首帧 `Speech.Init` 后 `Repeat.Say` |
| 对话 / 旁白文本 | `Naninovel.UI.RevealableText.set_Text` Postfix → `Reader.OnRevealText` |
| 行 id / 配音判定 | `Naninovel.UITextPrinter.AddMessage(PrintedMessage)` + `SetMessages(...)` Postfix（返回 `void`，可安全挂）→ `Text.parts[i].id` → `VoiceLineIds` |
| 说话人 | `WitchTrialsTextPrinterPanel.SetMessageAuthor(MessageAuthor)` + `RevealableTextPrinterPanel.SetAuthorNameText(string)` Postfix |
| 选项 | `Naninovel.UI.ChoiceHandlerPanel.AddChoiceButton(ChoiceState)` Postfix → `Choices.OnButtonAdded` |
| 审判辩论 | `DebateTextPrinterPanel.AddMessage(PrintedMessage)` / `SetMessages(IReadOnlyList<PrintedMessage>)` |
| 文本语言切换 | `ILocalizationManager.SelectLocale("zh-Hans")` |
| 界面导航 | 平台层 `UiNav` 每帧扫描（`TickUiNav` 钩子）+ `UiNav.Game.cs` 逐作规则 |

### 几条踩过的坑（改之前请先读）

- **BepInEx 6 的 `BasePlugin` 不是 MonoBehaviour**：没有 `Awake`/`Update`，每帧逻辑必须
  `AddComponent<一个 MonoBehaviour>()`（同 Mono 模板的 `BaseUnityPlugin` 写法在 IL2CPP 下编不过）。
- **永远不要 patch 返回 `UniTask`/`Task`/非 blittable 结构体的方法**（本作实测：
  `TextPrinterManager.Print` 的 Prefix 会搬坏返回值 → `PrintText.Print` 的 `await` 抛
  `MethodAccessException` → 剧情永久卡死、任何输入都推不动）。要文本就挂返回 `void`
  的 `UITextPrinter.AddMessage` / `RevealableText.set_Text`。
- **面板级 `UITextPrinterPanel.AddMessage` 在本作 ADV 路径不触发**（挂了也一条日志没有）：
  ADV 真入口是 actor 级的 `Naninovel.UITextPrinter.AddMessage`，行 id 只能从这里拿。
- **`Resources.FindObjectsOfTypeAll` 在 IL2CPP 收 `Il2CppSystem.Type`**，不是 `System.Type`，
  要 `Il2CppType.From(...)` 转；`as Component` 恒 null，要 `TryCast<Component>()`。
- **`ShowWindow(GetConsoleWindow(), SW_HIDE)` 必须先校验窗口类名**：实测本作里
  `GetConsoleWindow()` 返回的句柄可能就是游戏主窗口，直接隐藏会把游戏窗口一起藏掉。
  （0.1.0.0 起改成从配置层就不创建控制台，见「关于 BepInEx 控制台窗口」。）
- **`BepInPlugin` 版本号必须纯数字四段**（`System.Version`）；带字母会让 BepInEx
  **静默跳过整个插件**，日志只有一行 Warning。
- **不要用 `Compress-Archive` 打发布包**：Windows PowerShell 5.1 会写反斜杠条目，
  Linux/macOS 解压成一坨；一律走 `tools/makezip.ps1`。
- **发行版里没有可用的开发者面板**：`DebugOptionsBase.s_debugManager` 实测是 null
  （`s_isInitialized` 却是 true），`DebugPanel` / `EnableDebugModeButton` /
  `DebugCommandButtons` 只有内存里的 prefab 资产、从未实例化 → 发行版按
  `Ctrl+Alt+Shift+F3` 没有面板可开（这是开发构建功能）。
  **0.0.0.12 起改成「跳转到指定剧情」**（`ScriptJump.cs` + 生成的 `ScriptIndex.cs`）：
  组合键打开菜单，上下键换剧本、数字键输入起始行、回车跳转，走
  `ScriptPlayerExtensions.LoadAndPlayAtLine`（0.0.0.9 已实机验证过这条 API）。
  配置项相应改名「允许剧情跳转（开发者）」，仍默认关闭。
- **整页界面不要指望平台层的可达性判定**：魔女手册打开时有一层全屏 CanvasGroup 挡在
  关闭按钮上面，`UiNav.VisiblyClickable` 的 UI 射线会判「点不到」（玩家实际能点）——
  平台层按约定不能改，逐作层的做法是 `ScreenAssist.cs`：自己播报页面内容 + `Esc` 直接
  调 `CloseButton.onClick`。地图地点（`MapAreaLabel`）根本不是 `Selectable`，平台层
  不会扫，必须单独取。
- **本作的按钮不能靠合成派发点**：`onClick.Invoke()` 与 `ExecuteEvents.pointerClickHandler`
  都返回成功（诊断 `点击派发 ok=True`），但 `MapPage.OnChoiceMapAreaAsync` 的 await 一直不返回。
  必须 `InputInject.Click(screenX, screenY)` 往 `InputSystem` 事件队列塞真实的按下/抬起。
- **本机自动化环境的键盘注入不可靠**：`keybd_event`/`SendInput` 实测被系统挡掉
  （`SendInput` 返回 0），鼠标注入可用；所以验证 `Esc`/重读这类键位时，用插件内的
  `MANOSABA_A11Y_TEST_ESC` / `MANOSABA_A11Y_TEST_REPEAT` 钩子，真实按键留给玩家确认。
- **窗口客户区尺寸会变**（实测见过 1920×1080 与 1280×720 两种）：自动化脚本一律用
  **相对坐标**（`GetClientRect` 后按比例算），写死像素坐标会「点空」。
- **`PrintWindow` 对 DXGI 翻转模型窗口会返回白图**：截图改为 `GetWindowRect` +
  `CopyFromScreen`（见 `probe\cap-screen.ps1`）。
- **地图选择等待期间切楼层会软锁**：点手册底部胶片条（`WitchBookItemButton`）会把
  `OnChoiceMapAreaAsync` 的 await 挂断，加载界面永久卡在可见状态（run105/106）。选择态必须禁用切层。
- **自动化脚本的「点屏幕中央」会抢先完成地图选择**：run115 里数字键选娱乐室却走了玄关大厅分支，
  原因就是脚本每 3 秒点中央真点中了默认高亮的地点。检测到选择态后必须立刻停止点击。

## 如何复现分析

补丁的每一条结论都来自两类证据之一：**运行时探针**，或**对游戏自带文本出口的读取**。

1. **运行时探针**：`probe\` 下是一堆一次性脚本 —— 抽文本表、dump 场景树/控件矩形、
   抓 Harmony 挂点、记录事件序列；产物只留统计与对象名，**不留台词原文入库**。
   `PerfProbe.cs` / `UiProbe.cs` 是留在插件里的两个诊断开关
   （`MANOSABA_A11Y_PERFDIAG=1` / `MANOSABA_A11Y_PROBE_UI=1`），默认不开。
2. **游戏自带的文本出口**：能问游戏要的就问游戏要 ——
   `LocalizableText.parts[i].id`（行 id 与配音判定）、`CharacterNames.txt`（角色名）、
   `ILocalizationManager`（文本语言）、`WitchBookScreen.RestrictionCategories`（出示证据的限制条件）。
   这样拿到的永远是正确的用词，也不用自己造。
3. **interop 与日志**：接口签名从 `BepInEx\interop\`（游戏首次启动生成）读，
   行为从 `BepInEx\LogOutput.log` 与 probe 事件日志确认；每一处补丁都在源码注释里
   写清依据（哪个类型/方法、怎么验证的）。

### 分析笔记与中间产物（**故意不入库**）

- 游戏资源、解包产物、反汇编输出、探针转储、运行日志、截图，以及**任何剧情文本**：
  `.gitignore` 里逐类拉黑，见下面的「内容边界」。
- 含台词原文的会话日志与截图**故意不入库**；`probe\logs\` 与 `tests\reports\` 都不入库。
- `docs\` 只写**机制**（哪个对象、哪个字段、为什么这么选），不抄游戏文本。

---

## 内容边界

本仓库严格遵守一条线：**任何会影响开发商与发行商利益的内容都不入库**。

**入库的**：

- 补丁自己的源码、构建脚本、文档；
- 界面控件的**功能性名称**（`UiNav.Game.cs` 里的别名表）、角色名与行 id 表 ——
  用途是让读屏念出正确用词、判断这一行有没有配音，**不包含任何剧情内容**；
- **第三方组件的许可证文本** `mod\licenses\`（见下）。

**不入库的**（`.gitignore` 里逐类拉黑，误敲 `git add .` 也拦得住）：

- 游戏本体与资源：`GameAssembly.dll`、`global-metadata.dat`、`*.bundle` `*.assets` `*.resS` …
- **IL2CPP 反编译产物与 interop 程序集**：`re\decompiled\`、`BepInEx\interop\`、`*.dll` …
- 逆向与解包分析脚本的原始产物：`re\raw\`、`evidence\raw\`、`textassets\`、`*.bin` …
- 运行日志与探针输出（可能夹带剧情文本）：`probe\logs\`、`tests\reports\`、`LogOutput.log` …

日常诊断日志写在游戏目录外的本地路径，不进仓库。

### 做了什么、没做什么

说清楚，免得读者误会：

- **做了**：在正版游戏上跑运行时探针，观察游戏自己的对象与字段；
  用 BepInEx 官方的 interop 机制读 IL2CPP 程序集里的**类型与方法签名**（不含剧情文本）；
  从游戏自己的事件与对象里取台词与控件名。
- **没做**：没有破解、解密或绕过任何技术保护措施；
  没有修改、替换或再分发任何游戏文件；没有解包或重打包游戏资源；
  没有把任何剧情文本或截图带进本仓库。

### 第三方组件

| 组件 | 许可 | 是否随仓库 |
|---|---|---|
| BepInEx 6（`BepInEx\`、`winhttp.dll`、`dotnet\`） | LGPL-2.1 | 否（`*.dll` 被 `.gitignore` 拉黑）；**随发布包分发**，许可证见 `mod\licenses\` |
| Il2CppInterop（PR#277 修复版） | LGPL-3.0 | 否；随发布包分发 |
| HarmonyX | MIT | 否；随发布包分发 |
| UnityDoorstop（`winhttp.dll`） | LGPL-2.1 | 否；随发布包分发 |
| NVDA Controller Client（`nvdaControllerClient.dll`，NV Access 官方 x64 版） | LGPL-2.1 | 否；随发布包分发 |
| Tolk（可选，若系统里有） | LGPL | 否，运行时自取 |
| 争渡读屏 API（`ZDSRAPI.dll`） | 争渡读屏自有许可 | **否**；本补丁**不随包分发**，只用玩家自己安装的那份 |

许可正文随发布包放在 `licenses\`（含 `THIRD-PARTY-NOTICES.txt`）。**补丁不修改、不反编译这些第三方组件**，
它们的版权归各自权利人所有，也不含在本仓库的 MIT 许可里。

---

## 合规说明

**《魔法少女的魔女审判》的著作权属于 Acacia 与 Re,AER。** 本仓库不是他们的项目，
也没有得到他们的授权或背书。下面的说明只是「我们做了什么、没做什么」，
不构成对他们的任何主张。

- 在绝大部分国家和地区，为自己使用而修改你**合法拥有**的软件通常是允许的；
  但把**修改后的软件提供给第三方**，以及**绕过技术保护措施**，
  在绝大部分国家和地区都是**不被允许的**。各国规定不尽相同，请以你所在地的法律为准。
- 本补丁因此**不包含任何游戏资源**，只发布补丁代码与文档；
  发布包中不含游戏本体、不含任何游戏资源，也**不随包分发任何游戏文件**。
- 补丁**不绕过任何技术保护措施**，不修改、不替换、不再分发游戏文件。
  它只是让游戏在启动时多加载一个我们自己的插件（BepInEx 官方机制），
  并在游戏自己的界面里替玩家按键。
- 补丁里的台词来自**游戏运行时的文本事件**，界面控件名来自实机取证；
  其中不含任何剧本原文。含台词原文或截图的中间产物**不入库，也不随发布包分发**。
- **再次请求：请通过 Steam 购买正版。**
  Acacia 与 Re,AER 的劳动成果值得被正当地支持 ——
  本补丁存在的意义是让更多人**玩得上**，不是替代购买。

---

## 许可

本仓库的**补丁代码与文档**采用 [MIT 许可](LICENSE) 发布；
附加的署名要求、非官方声明与免责条款见 [NOTICE.md](NOTICE.md)。

平台层与契约层（`A11yHost.cs`、`Speech.cs`、`UiNav.cs` 等）来自
[A11yFramework](https://github.com/Nornageste-mh/Framework)，同样是 MIT，**逐字节复用、未修改**。

第三方依赖各自的许可见上表与 [NOTICE.md](NOTICE.md)。

简要说明：

- **可自由使用、修改、再分发**（含商用），但须标注出处 —— 本仓库地址与作者
  Nornageste-mh；若你做了修改，须注明「已修改」，不得让人误以为修改版出自原作者。
- **本项目为非官方第三方工具**：本补丁是非官方玩家自制工具，与游戏开发商、发行商 Acacia / Re,AER 无任何关系，未获其授权或背书。游戏著作权归 Acacia / Re,AER 所有，本仓库不含任何游戏资源。
- **按「现状」提供，不承担任何责任**：因使用本软件导致的游戏存档损坏、游戏崩溃、
  账号受限、或与游戏厂商及任何第三方产生的争议与索赔，作者与贡献者均不负责。
  是否使用请自行判断并自担风险。

第三方组件的许可与出处详见 [NOTICE.md](NOTICE.md) 与 `mod\licenses\THIRD-PARTY-NOTICES.txt`。
