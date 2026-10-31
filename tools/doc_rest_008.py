import io

def rd(p):
    return io.open(p, encoding='utf-8-sig').read()

def wr(p, s):
    io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)

base = r'D:\DSHWorkSpace\manosaba_a11y'

# ---------- 1) 版本串 ----------
for rel in ['mod/package/安装说明.md', '测试问卷.md', '无障碍可行性验证.md']:
    p = base + '\\' + rel.replace('/', '\\')
    s = rd(p)
    n = s.count('v0.0.0.7')
    s = s.replace('v0.0.0.7', 'v0.0.0.8')
    wr(p, s)
    print('ver', rel.encode('ascii', 'backslashreplace').decode(), n)

# ---------- 2) 安装说明：地图选择 ----------
p = base + r'\mod\package\安装说明.md'
s = rd(p)
sec = '''

## 地图上选择地点（v0.0.0.8 起）

审判和剧情里经常会出现「现在要去哪里」的地图（魔女手册的「地图」页，底部有 1F / 2F / 地下1F）。
以前这一步读屏玩家做不了，现在可以：

- 地图一出现，插件会立刻把**游戏允许你去的地点**从头到尾念一遍，例如
  「地图上可前往 8 个地点：在1F：1、会客厅。2、玄关大厅。3、食堂。4、医务室。5、淋浴房。
  6、往湖方向。在2F：7、娱乐室。在地下1F：8、牢房。按数字键选择。」
  名单直接来自游戏剧本，**不会把去不了的地方念成能去**。
- 直接按 `1`-`9` 选对应编号即可过去；这个界面里按错楼层不会出问题，插件会自动帮你点到正确楼层的那个地点。
- 名单超过 9 个地点时，用 `PageUp` / `PageDown` 翻页（插件会先念「第 2 页」这类提示再报新页的地点）。
- 正在选地点的时候，`Esc` 不会关掉手册（会念「请先选择一个地点。」）——因为这一关需要先做出选择，
  关掉会让剧情卡住。

顺带一提：**不在选择状态**、只是自己翻手册看地图时，`PageUp` / `PageDown` 仍然可以用来切换楼层查看。
'''
assert '地图上选择地点' not in s
s = s.rstrip('\n') + sec
wr(p, s)
print('install ok')

# ---------- 3) 常见问题：地图 ----------
p = base + r'\mod\package\常见问题.md'
s = rd(p)
faq = '''

## 走到地图页，但不知道能去哪里 / 念出来的地点都点不了

- v0.0.0.8 起：只要这一屏是**游戏要求你选地点**，插件一出现就会念「地图上可前往 N 个地点」并给出编号，
  按 `1`-`9` 直接过去（`PageUp`/`PageDown` 翻页）。
- 如果没有听到「地图上可前往…」，说明这一屏只是**你自己打开手册看地图**（不是剧情在等你选），
  这时候听到的是「1F：会客厅、玄关大厅、…」这样的浏览播报，不需要做选择。
- 如果念了地点、按数字键却没反应：先用方向键或 `Tab` 之外的组合键（`Alt`+`Shift`+`F1`）确认插件还活着
  （能听到导航播报）；仍不行请把 `BepInEx\\LogOutput.log` 末尾 200 行发出来，日志里会有
  `MAPCHOICE 扫描 … restr=名单(N) choiceMode=True` 这样的行可以判断插件有没有认出选择态。
- 千万不要在这一屏用 `Esc` 关手册：真的在等你选地点时，关掉会让剧情永远等不到结果。
  插件已经帮你拦住了（会念「请先选择一个地点。」）。
'''
assert '不知道能去哪里' not in s
s = s.rstrip('\n') + faq
wr(p, s)
print('faq ok')

# ---------- 4) 测试问卷：地图 ----------
p = base + r'\测试问卷.md'
s = rd(p)
q = '''

## 地图选择（v0.0.0.8 新增）

出现「要去哪里」的地图时（第 1 章 Adv25 之后会遇到）：

- [ ] 地图一出现就听到「地图上可前往 N 个地点」，并逐个念出地点名（含 2F / 地下1F 的地点）
- [ ] 按数字键 1 选第 1 个地点，剧情走到该地点对应的场景
- [ ] 选一个 2F 或地下1F 的地点（编号在「在2F：」「在地下1F：」后面），剧情也能正确走到
- [ ] 这一屏按 `Esc` 不会关掉手册，而是听到「请先选择一个地点。」
- [ ] （可选）地点超过 9 个时 `PageUp` / `PageDown` 能翻页
- [ ] （可选）只是自己翻手册看地图时，`PageUp` / `PageDown` 能切换楼层
'''
assert '地图选择（v0.0.0.8 新增）' not in s
s = s.rstrip('\n') + q
wr(p, s)
print('questionnaire ok')

# ---------- 5) 无障碍可行性验证：地图 ----------
p = base + r'\无障碍可行性验证.md'
s = rd(p)
v = '''

## 地图页选择地点（v0.0.0.8）

**问题**：`@choiceMapArea` 要求玩家在地图上点一个「可前往的地点」，但读屏用户看不到高亮，
平台层的导航也点不到（手册打开时被全屏 CanvasGroup 遮罩挡住）。

**结论**：可做，且已实机验证。

- 允许名单的权威来源是 `WitchTrials.Views.MapPage._restrictionAreas`
  （`IReadOnlyCollection<MapAreaKind>`；interop 的接口没有 `Count`，要
  `TryCast<Il2CppSystem.Collections.Generic.List<MapAreaKind>>()` 才取得出条目）。
- 判定「可点」= 在该名单里 + `activeInHierarchy` + 有 `Button` + `interactable` + 名字非空。
  **不能用 `CanvasGroup.alpha`**（实测恒为 0）。
- 选中必须走**真实输入注入**：`onClick.Invoke()` / `ExecuteEvents.pointerClickHandler` 都「成功」返回，
  但 `OnChoiceMapAreaAsync` 的 await 不返回、地图页不关（run117/118）。改成
  `RectTransformUtility.WorldToScreenPoint` + `InputInject.Click`（`InputSystem.QueueStateEvent`）后打通。
- 跨楼层地点**本身可以点**（run112 直接点 2F 娱乐室进 `Adv32`）；但在选择等待期间去点手册底部楼层卡
  会挂断 await、加载界面永久卡死（run105/106）——所以选择态禁用切层，并把允许名单里的地点一次全念出来。

**证据索引**
- `probe\\logs\\run122-summary.txt`：`restr=名单(8)`、播报原文、`数字键 3 -> 食堂`、`chosenAreaId=String:MessHall`
- `probe\\logs\\run11x-summary.txt`（run110/112/115）：跨楼层点选、软锁复现、点中央抢占
- `tests\\reports\\map-choice.md`：自动化用例 `map.choice`
'''
assert '地图页选择地点（v0.0.0.8）' not in s
s = s.rstrip('\n') + v
wr(p, s)
print('feasibility ok')

# ---------- 6) framework-notes：§18 ----------
p = base + r'\docs\framework-notes.md'
s = rd(p)
n = '''

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
'''
assert '第十一轮：地图页选择地点' not in s
s = s.rstrip('\n') + n
wr(p, s)
print('framework-notes ok')
