import io

# ---------- 测试问卷 ----------
p = r'D:\DSHWorkSpace\manosaba_a11y\测试问卷.md'
s = io.open(p, encoding='utf-8-sig').read()
s = s.replace('0.0.0.12', '0.0.0.13')
s = s.rstrip() + '''

## 6.2 低性能模式（v0.0.0.13 新增）

| 检查项 | 结果 |
|---|---|
| 按 `Alt`+`Shift`+`F2` 能听到「低性能模式已开启」/「已关闭」 | ☐ |
| 开启后画面明显变简（阴影消失/变糊） | ☐ |
| 关闭后画面恢复原样（阴影回来、不糊） | ☐ |
| 游戏卡的时候，确实比不开更跟手 | ☐ |
| 帧率很低时会念「当前帧率较低…按 Alt+Shift+F2 可以开启低性能模式」 | ☐ |
| 不喜欢某一档时，能只关掉它（如「关闭柔边阴影」） | ☐ |
'''
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('questionnaire ok')

# ---------- 无障碍可行性验证 ----------
p2 = r'D:\DSHWorkSpace\manosaba_a11y\无障碍可行性验证.md'
t = io.open(p2, encoding='utf-8-sig').read()
t = t.replace('0.0.0.12', '0.0.0.13')
t = t.rstrip() + '''

## 低性能模式（v0.0.0.13）

| 项 | 实测 |
|---|---|
| 入口 | `Alt`+`Shift`+`F2`（`Plugin.CfgLowPerfToggleKey`）+ cfg「性能 / 低性能模式」 |
| 插件侧降开销 | `Evidence` 门控 0.4s 缓存、`ScreenAssist` 空闲 1.5s 扫描 → `ev` 5.7→1.1~1.9ms、`screen` 3~10→1.1~3.2ms |
| 画面降级 | `LowPerf.DegradeUrp()`（renderScale 0.8 / MSAA 1 / 主光与软阴影关 / shadowDist 0 / cascade 1 / addLights 0）、`DisableTrueShadows()`（579 个）、`LowerResolution()`（默认关闭） |
| 还原 | 关闭模式与 `OnDestroy` 都调 `Restore()`；实测 `LOWPERF URP: 已还原画质设置` + `还原 579 个柔边阴影组件` |
| 低帧率提醒 | 30 秒窗口、<15 帧、模式未开 → 播报一句，10 分钟内最多一次（**不自动切**） |
| 证据 | `probe\\logs\\lowperf-live2-summary.txt`、`tests\\reports\\perf1-report.md`、`run126-lowperf.ps1` |
| 未覆盖 | 组合键真实按键（本机注入被系统挡，靠 `Hotkey.Parse` 单元断言）、降低渲染分辨率档、别的显卡上画面降级的收益 |
'''
io.open(p2, 'w', encoding='utf-8-sig', newline='\n').write(t)
print('a11y verification ok')

# ---------- framework-notes ----------
p3 = r'D:\DSHWorkSpace\manosaba_a11y\docs\framework-notes.md'
f = io.open(p3, encoding='utf-8-sig').read()
f = f.rstrip() + '''

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

**验收**：`probe\\run126-lowperf.ps1`（40s 开 / 75s 关）+ 套件用例 `perf.lowmode`。
'''
io.open(p3, 'w', encoding='utf-8-sig', newline='\n').write(f)
print('framework notes ok')
