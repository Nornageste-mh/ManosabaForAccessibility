import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/Evidence.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = "                if (Plugin.TestEvForce || IsMode || BookIsEvidenceMode()) UpdateChoice();"
new = """                // ★ 性能：BookIsEvidenceMode() 内部是 Resources.FindObjectsOfTypeAll<WitchBookUi>()，
                //   实测放在每帧路径上要 5.7ms（这台机器一帧才 80~370ms，等于吃掉 5%~7%）。
                //   界面显隐不可能一帧内变化到需要每帧知道，限频到 0.4 秒完全够，且不影响按键响应
                //   （按键判定在 UpdateChoice 里，仍每帧跑）。
                if (!IsMode && Plugin.TestEvForce == false && now >= _nextGateAt)
                {
                    _nextGateAt = now + 0.4f;
                    _gateCached = BookIsEvidenceMode();
                }
                if (Plugin.TestEvForce || IsMode || _gateCached) UpdateChoice();"""
assert s.count(old) == 1
s = s.replace(old, new)

# 字段
old2 = "        private static bool _warnedMgr;"
if s.count(old2) != 1:
    # 找别的锚点
    old2 = None
if old2:
    s = s.replace(old2, """        private static bool _warnedMgr;

        // 门控缓存（性能：见 Tick 里的注释）
        private static float _nextGateAt;
        private static bool _gateCached;""", 1)
else:
    raise SystemExit("anchor _warnedMgr not found")

io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Evidence gate cached")

# ScreenAssist：自适应扫描间隔
p2 = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/ScreenAssist.cs"
t = io.open(p2, encoding="utf-8-sig").read()
t = t.replace("        private const float ScanInterval = 0.5f;",
"""        private const float ScanInterval = 0.5f;

        // 性能（0.0.0.12 实测）：每次扫描要 find 全场景 Transform，花 3~10ms。
        // 找不到界面时（=绝大多数剧情时间）没必要 0.5 秒扫一次，放慢到 1.5 秒；
        // 一旦找到界面立刻回到 0.5 秒，保证界面里的响应速度。
        private const float ScanIntervalIdle = 1.5f;""", 1)
t = t.replace("""            _nextScanAt = Time.unscaledTime + ScanInterval;""",
"""            _nextScanAt = Time.unscaledTime + (_foundLastScan ? ScanInterval : ScanIntervalIdle);""", 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(t)
print("ScreenAssist adaptive interval")
