import io
p = r'D:\DSHWorkSpace\manosaba_a11y\CHANGELOG.md'
s = io.open(p, encoding='utf-8-sig').read()
assert '当前 `0.0.0.12`' in s
s = s.replace('当前 `0.0.0.12`', '当前 `0.0.0.13`', 1)

anchor = '## 版本清单'
i = s.index(anchor) + len(anchor)
entry = '''

### v0.0.0.13（低性能模式：插件侧降开销 + 可选画面降级）

**现象**：用户提出「这台机器上游戏很卡，希望有一个低性能模式让游戏更流畅」，并
接受先探查再决定的流程（`m03246`）。

**先量了三件事**（`PerfProbe`，`MANOSABA_A11Y_PERFDIAG=1`，测试机 兆芯 KX-U6780A + ZX C-960）：
1. 帧率 1.8~14 FPS、帧时间 71~570ms，`focused=True`；
2. 场景里 803 个柔边阴影（TrueShadow）、77 个 Canvas；URP `renderScale=1.0 msaa=4 shadowDist=150 addLights=8`；
3. **插件自己每帧 10~13ms**（`ev≈5.7ms` + `screen≈3~10ms` + `uinav≈0.7ms`）——
   这部分是纯浪费，和画面无关。

**分阶段实验（`MANOSABA_A11Y_PERFTEST=1`）**：第 70 秒把 URP 压到最省（renderScale 0.8、
关 MSAA/主光/软阴影、shadowDist 0、addLights 0），第 140 秒再关掉 581 个 TrueShadow →
**前后帧率都在 ≈8 FPS 的噪声区间**。结论：这台机器是**主线程瓶颈，不是显卡瓶颈**，
画面降级在这里没有收益。所以低性能模式不能只做画面，必须先把插件自己的开销压下去。

**怎么修**：
- 插件侧限频（`0.0.0.12` 已先落地，无视觉代价）：`Evidence` 的门控判断改成每 0.4 秒算一次、
  `ScreenAssist` 空闲时 1.5 秒扫一次 → `ev` 5.7ms→1.1~1.9ms、`screen` 3~10ms→1.1~3.2ms。
- 新增 `LowPerf.cs`（默认关闭，配置「性能」组）：
  - `Alt`+`Shift`+`F2` 或配置「低性能模式」开关；开启/关闭都念一句。
  - 四档，可单独关：**降低 URP 画质**（renderScale 0.8 / MSAA 关 / 主光与软阴影关 /
    shadowDist 0 / cascade 1 / addLights 0）、**关柔边阴影 TrueShadow**、
    **降低渲染分辨率**（默认关闭：会改窗口大小）、**插件侧降开销**（恒开，无视觉代价）。
  - 关闭模式或插件卸载时**还原所有改过的设置**（保存原始值 + 逐个还原被关掉的组件）。
  - 低帧率只**提醒**不自动切：连续 30 秒平均低于 15 帧时念
    「当前帧率较低，约 N 帧。按 Alt+Shift+F2 可以开启低性能模式。」（10 分钟内最多一次）。
- 为什么默认关闭、且不自动切：改画面属于替玩家做决策，读屏用户尤其依赖固定的
  窗口大小/对比度；所以只提供入口 + 提醒。

**实机验证**（`probe\\run126-lowperf.ps1 -Tag lowperf-live2`，`TEST_LOWPERF=1` 40s 开、75s 关）：
```
[a11y] TEST LOWPERF: 开启 (t=40.1)
LOWPERF URP: renderScale=0.80 msaa=1 mainShadow=False shadowDist=0
LOWPERF TrueShadow: 关闭 579 个柔边阴影组件
LOWPERF 已开启（自动化测试）：urp=True noShadow=True lowRes=False
[a11y] TEST LOWPERF: 关闭 (t=75.0)
LOWPERF URP: 已还原画质设置
LOWPERF TrueShadow: 还原 579 个柔边阴影组件
LOWPERF 已关闭（自动化测试）：设置已还原
```
同轮还抓到提醒链路：`LOWPERF 低帧率提醒: fps=10.1 未开启低性能模式` →
`[朗读] 当前帧率较低，约 10 帧。按 Alt+Shift+F2 可以开启低性能模式。`
开启前后帧率：开启段 14.4/14.3/14.3 FPS、关闭后 12.4/10.9 FPS——**本机噪声太大，
不能声称画面降级带来提升**；诚实的说法是「收益主要来自插件侧限频」。

**单元断言**：52 → **54** 条（`lowperf.默认关闭`、`lowperf.开关键可解析`），
`SELFTEST SUMMARY total=54 pass=54 fail=0`。

**尚未实机复测**：组合键 `Alt+Shift+F2` 的真实按键链路（本机自动化键盘注入被系统挡掉，
只由 `Hotkey.Parse` 单元断言覆盖）；降低渲染分辨率档（默认关闭，本轮未开）；
不同显卡机器上画面降级的收益。
'''
s = s[:i] + entry + s[i:]
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('changelog ok')
