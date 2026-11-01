import io
p = "D:/DSHWorkSpace/manosaba_a11y/docs/framework-notes.md"
s = io.open(p, encoding="utf-8-sig").read().rstrip() + """

## 22. 性能取证：为什么「低性能模式」要先测量（0.0.0.12 之后）

**测量工具**：`mod\\src\\ManosabaA11y\\PerfProbe.cs`，`MANOSABA_A11Y_PERFDIAG=1` 打开。
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
"""
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("notes §22 written")
