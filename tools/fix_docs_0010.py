import io, re, os
base = r"D:\DSHWorkSpace\manosaba_a11y"
files = ["测试问卷.md", r"无障碍可行性验证.md", r"docs\framework-notes.md"]
for f in files:
    p = os.path.join(base, f)
    s = io.open(p, encoding="utf-8-sig").read()
    s2 = s.replace("0.0.0.9", "0.0.0.10")
    if s2 != s:
        io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s2)
        print("version bumped:", f)

# framework-notes \u00a720
p = os.path.join(base, r"docs\framework-notes.md")
s = io.open(p, encoding="utf-8-sig").read().rstrip()
s += """

## 20. 第十三轮：8 份审计报告回填（0.0.0.10）

交付 0.0.0.9 之后派了 8 个**只读**子代理分头审计（文本清洗 / Harmony 覆盖 / 界面导航 / 三层交互竞态 /
L2 契约 / 配置项 / 测试套件 / 合规）。**只读审计 + 主代理统一改**这个流程很值得复用：子代理各自
只看一个面，能给出 file:line 级的证据，而不会互相踩文件。

**这一轮最值钱的四个发现**（都不是“功能没做完”，而是“做完了但悄悄不生效”）：
1. **配置项 Bind 了没人读**：4 个开关（限时选择时长 / 朗读手机消息 / 朗读手机表情 / 快进补念）
   全仓 0 读取点。Bind 成功 ≠ 生效，必须给「每个 Cfg 键至少一个读取点」加断言。
2. **`ConfigEntry.Value = true` 会落盘**：测试开关把玩家的 `界面诊断日志` 永久改成 true，
   而诊断日志含台词原文 —— 等于把玩家推去剧透。改成只设 `A11yHost.DiagOn`（纯内存）。
3. **测试套件的 Skip 是死代码**：`if ($Only.Count -gt 0) { … if (-not $Only …) { 过滤 Skip } }` ——
   条件自相矛盾，于是依赖真人点击的两个用例每次都跑（实测 582 秒）。这类“看起来在防护、
   实际永远不成立”的守卫，比没有守卫更危险。
4. **git 里躺着 89 MB 剧本文本**：`evidence/raw/**` 444 个 `.bin`（含台词原文）与反编译源码
   一直是被 `git add -A` 带进去的。`.gitignore` 只管“以后”，不管“已经进去的”，
   所以红线要靠 `git rm --cached` + `check-staged.ps1` 的禁用路径/扩展名双保险。

**四条可复用的判据**
- `TextProc` 类修复必须跑**全量语料回归**（440 个剧本 / 3.4 万块）：只看单个样例会漏掉
  1,353 处「的，」与 3,342 处「<br>　」缩进空格。
- 交互层（选项 / 地图 / 证物）必须**共用一把焦点门**：本作只启用新 Input System，全局读键
  兜底不看焦点，玩家在别的程序里敲数字就会把选项点了。
- 「先念后点」一律改成「先点后念 + 校验返回值」：注入点击会因坐标越界/WorldSpace 相机失败，
  这时不能说“已选中”。
- 审计报告里的条目要**分成“能定量验证”与“需要真人复测”两类**：这一版只收回前者，
  后者如实列在 CHANGELOG 的“本版未修”里。
"""
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("framework-notes §20 added")
