import io

p = r'D:\DSHWorkSpace\manosaba_a11y\CHANGELOG.md'
s = io.open(p, encoding='utf-8-sig').read()

s = s.replace('当前 `0.0.0.7`', '当前 `0.0.0.8`', 1)

anchor = '## 版本清单\n\n'
if anchor not in s:
    raise SystemExit('anchor not found')

entry = '''## 版本清单

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

**实机证据**（`probe\logs\run11x-summary.txt`、`tests\reports\map*.md`）
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

'''

s = s.replace(anchor, entry, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('changelog updated')
