# 自动化测试套件（《魔法少女ノ魔女裁判》A11Y 模组）

> 剧本规模：**34,836 个文本块 / 793,608 字 / 34,611 个唯一 lineId**。
> 人工全流程回放、或者「把每一句都跑一遍」都不现实，所以这套测试只回答一个问题：
> **「每个界面 / 剧情阶段 / 场景 / 模块，是否还符合预期？」**

## 两层结构

| 层 | 位置 | 跑法 | 覆盖什么 | 耗时 |
| --- | --- | --- | --- | --- |
| **单元自测** | `mod\src\ManosabaA11y\SelfTest.cs` | 进程内，`MANOSABA_A11Y_SELFTEST=1` | 纯逻辑：文本清洗规则、行 id 提取、说话人表、配音表、快捷键解析。26 条断言 | ~14 秒 |
| **集成场景** | `tests\Suite.ps1` + `tests\lib\Harness.ps1` | 每个用例独立启动一次游戏，真点击进正片 → 跳转到目标剧本 → 断言日志证据 | 标题导航 / 对话 / 说话人 / 配音闸门 / 退格重读 / 选项 / 辩论 / 加载 / 语言 | 每个 60–180 秒 |

为什么单元层放在插件里而不是单独的测试工程：本作是 **IL2CPP**，`TextProc`、`SpeakerNames`、`VoiceLineIds` 依赖运行时 interop，进程外引用不到。放在插件里跑 = 「用真实运行时 + 真实游戏数据」跑逻辑，比另建 net6.0 工程更可信。

## 用例清单

| Id | 分组 | 覆盖 | 关键断言 |
| --- | --- | --- | --- |
| `unit.selftest` | 单元 | TextProc / SpeakerNames / VoiceLineIds / LineSpeakers / Hotkey | 26 条全 PASS，`fail=0` |
| `menu.title` | 界面/标题 | 标题菜单导航 | `Alt+Shift+F1` 能进/出导航；组名「标题画面」；共 5 项；按钮有中文名；不念 `Ver.` |
| `story.adv` | 剧情/ADV | 普通对话逐句朗读 | ≥5 句 `Reader.say[reveal]`；带 `id=0101Adv01_*`；朗读文本无残留标签 |
| `story.speaker` | 剧情/ADV | 说话人 | `HOOK print author id=[Ema]` 类证据；出现「角色名：台词」或配音行「只报名字」；`重读: 角色名：台词` |
| `story.voice_gate` | 剧情/配音 | 配音闸门 | ≥3 句 `Reader.voiced[...]`，且带行 id |
| `keys.backspace` | 按键/退格 | 退格重读 | 按 `Backspace` 出现 `重读: …`，内容是剧情文本 |
| `story.choice` | 剧情/选项 | 选项组 | `CHOICE 播报 …共 N 个选项…按数字键选择`；数字键 `1` 被读到 |
| `trial.debate` | 剧情/审判 | 辩论朗读 | ≥3 句 `Reader.say[debate:add]`，文本已清洗 |
| `screen.loading` | 界面/加载 | 加载播报 | 点「新游戏」出现 `加载播报（…）`；`HOOK loadingUi visible=True` |
| `locale.ja` | 模块/本地化 | 文本语言 | 指定 `MANOSABA_A11Y_LOCALE=ja` 后台词是日文假名 |
| `book.assist` | 界面/整页 | 魔女手册整页界面 | 读取存档槽 1 → 点书本 → `ScreenAssist: 播报 魔女手册，<页签>页签。内容：…`；`TEST_ESC` 钩子触发 `ScreenAssist: Esc 关闭 魔女手册` |
| `evidence.present` | 审判/出示证据 | 否（默认跳过） | 跳行直达 `@choiceEvidence`：证物名枚举 → 播报 → 选卡 → 出示 → 确认框 → 剧本越过该行 |
| `map.choice` | 界面/地图 | 地图页选择地点（默认批次跳过） | `MAPCHOICE 播报 \d+ 个地点` 且含 `在2F：`；`chosenAreaId=[String:RecreationRoom]`；台词念到「娱乐室」 |

> 整页界面用例用「读取存档」而不是「新游戏」：存档槽 1 直接落在第 1 章 Adv18，手册里已有内容，
> 打开就能取证（用户提示的办法）；新游戏要推进到有线索的位置太慢。
> 键盘类断言（`Esc` / `Backspace`）走插件内钩子 `MANOSABA_A11Y_TEST_ESC` / `MANOSABA_A11Y_TEST_REPEAT`，
> 因为本机自动化环境的键盘注入被系统挡掉（`SendInput` 返回 0），鼠标注入可用。

> `map.choice` 目前 `Skip = $true`：从存档槽 1 到地图选择态要跨过「3 选 1 选项 → 剧情连开 3 次线索页」，
> 本机 6 FPS + 鼠标点击不稳定会让它在线索页卡住（run map3–map7）。地图选择本身已由
> `probe\run93-mapreal.ps1`（run122）实机验收；用 `-Only map.choice` 仍可手动触发。

> 魔女手册（页签/29 个地点）在 v0.0.0.4 用一次性 `UIPROBE` 转储人工核对过；它的自动化用例需要「先开手册再等 UIPROBE」（约 3 分钟）。
> 快捷入口：`tests\Suite.ps1 -Only <id前缀>` 只跑单个用例。

## 运行

```powershell
# 全部用例（约 15–20 分钟，逐个启动游戏）
pwsh -File tests\Suite.ps1

# 只跑单元层（约 12 秒，改完 TextProc 之类的逻辑先跑这个）
pwsh -File tests\Suite.ps1 -Only unit.selftest

# 只跑剧情类
pwsh -File tests\Suite.ps1 -Only story,keys -Tag suite-story

# 调试：跑完不杀游戏，方便自己看画面
pwsh -File tests\Suite.ps1 -Only story.adv -KeepGame
```

输出：

- `tests\reports\<Tag>-report.md` —— 人读：每个用例一张断言表 + 证据行 + 截图路径
- `tests\reports\<Tag>-result.json` —— 机读（CI / 后续做趋势图）
- `tests\reports\shots\*.png` —— 用例结束时的窗口截图

退出码 = 失败用例数（0 = 全绿），可直接用于 CI 判红绿。

## 设计约束（都是踩过的坑）

1. **不依赖 Steam**：游戏目录里有 `steam_appid.txt`，直接 `manosaba.exe` 启动即可（用户 2026-10-30 指正）。
2. **一个用例一次游戏生命周期**：用例之间不共享状态，崩溃/卡死不影响下一个。
3. **6 FPS**：本作实测 `Time.deltaTime≈0.165`，按键必须按住 ≥400ms、点击之间至少 1.5 秒，否则整段输入落在两帧之间被吞。
4. **进正片只有一条路**：真点标题「新游戏」(client 438,871)。`ScriptPlayerExtensions.LoadAndPlay` 只推进脚本状态机、画面仍停在标题（run16 截图证据），不能当验收路径。
5. **日志要按共享读打开**：BepInEx 的 `LogOutput.log` 句柄不允许默认共享，直接 `ReadAllText` 会抛「文件正被另一个进程使用」——套件第一版就因为这个问题「游戏在写日志、套件一直读空」。`Harness.ps1` 里用 `FileShare.ReadWrite|Delete` 打开。
6. **断言只认证据行**：不比对像素、不 OCR；截图只作人工复核附件。
7. **失败先怀疑环境**：窗口没抢到前台、控制台窗口被当成游戏窗口（`MainWindowHandle` 会拿到 `ConsoleWindowClass`）、游戏还在引擎初始化 —— 这三类曾造成大量假失败。

## 新增一个用例

在 `tests\Suite.ps1` 的 `$Cases` 里加一条：

```powershell
@{
    Id = 'story.xxx'; Title = '一句话说明'; Group = '剧情/某某'
    Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_JUMP = 'Act01_Chapter01/Act01_Chapter01_Adv05' }
    Timeout = 240
    Body = {
        param($Game, $Result)
        $e = Enter-Story $Game
        Add-Check $Result '进入正片' $e.Ok "尝试 $($e.Attempts) 次"
        if ($e.Ok) {
            $hit = Advance-Until $Game -Pattern '你的证据行正则[^\r\n]*' -MinCount 1 -TimeoutSec 150
            Add-Check $Result '断言名' $hit.Ok ($hit.Matches -join ' | ')
            $Result.Evidence += @($hit.Matches | Select-Object -First 3)
            $Result.Shots += (Shot $Game.Hwnd 'story-xxx')
        }
    }
},
```

要点：

- 断言的数据来源必须是**已有日志行**（`Reader.say[...]`、`CHOICE 播报`、`HOOK …`、`UIPROBE …`）。想测新功能，先在 `mod\src\ManosabaA11y` 里加一行带前缀的诊断日志 —— 这也是本项目「可测性」的写法。
- 剧情类用例用 `Enter-Story` + `Advance-Until`，不要写「点 N 次再断言」。
- 单次用例控制在 3 分钟内，超时值宁大勿小。
- 新增脚本必须带 UTF-8 BOM（项目 `check-staged.ps1` 会拦）。

## 已知局限

- 只验证「可读到 / 可操作到 / 文本正确」，不验证**语音真的从扬声器出来**（那需要真实读屏 + 音频回路）。
- 时序强、依赖随机分支的场景（辩论中途的证据提交、UI 动画中间态）只做「证据行存在」级断言。
- 截图不参与断言，视觉回归需要人工看 `tests\reports\shots`。
- 测试脚本本身依赖本机坐标（1920×1080 全屏），换分辨率需要调整 `438,871` / `960,540` / `84,62` 这几个点。

> **为什么 `evidence.present` 默认跳过**：它要先「读档进正片」才轮到 `MANOSABA_A11Y_JUMP_LINE`
> 跳行（跳转只在剧本已在播 `Act*` 时生效），而读档→正片这段在本机 6 FPS 下点击不稳。
> 链条本身已用 `probe\run124-evdialog.ps1` 实机验收（ev14：`idx=191 → 197`）。
> 需要复查时显式跑：`pwsh -File Suite.ps1 -Only evidence.present -Tag ev-suite`。
