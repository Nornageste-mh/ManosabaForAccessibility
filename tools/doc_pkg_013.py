import io

# ---------- README 进度表 ----------
p = r'D:\DSHWorkSpace\manosaba_a11y\README.md'
s = io.open(p, encoding='utf-8').read()
anchor = '| 语音后端选择与运行中切换（`F7`） | ✅ 实机验证（`争渡读屏` ↔ `SAPI`） |\n'
assert anchor in s
row = '| 低性能模式（`Alt`+`Shift`+`F2`，默认关闭） | ✅ 实机验证（开启/还原、柔边阴影 579 个、低帧率提醒）；画面降级在本机无收益 |\n'
s = s.replace(anchor, anchor + row, 1)
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('readme row ok')

# ---------- 包内 安装说明 ----------
p2 = r'D:\DSHWorkSpace\manosaba_a11y\mod\package\安装说明.md'
t = io.open(p2, encoding='utf-8-sig').read()
t = t.replace('0.0.0.12', '0.0.0.13')
# 快捷键表补一行
key = '| `F7` | 切换语音后端'
if key in t:
    i = t.index(key)
    lineEnd = t.index('\n', i) + 1
    t = t[:lineEnd] + '| `Alt`+`Shift`+`F2` | 开启 / 关闭低性能模式（默认关闭） |\n' + t[lineEnd:]
# 新增一节：低性能模式
anchor3 = '## '
# 追加在文末更安全
t = t.rstrip() + '''

## 低性能模式（v0.0.0.13 起，默认关闭）

游戏卡的时候可以打开它：按 `Alt`+`Shift`+`F2`，或者在
`BepInEx\\config\\manosaba.a11y.reader.cfg` 的「性能」组里打开「低性能模式」。

它会做两件事：

1. **降低补丁自己的每帧开销**（无视觉代价，恒开）——实测每帧从 10~13 毫秒降到 2.5~5 毫秒；
2. **按你的配置降低画面**，三档可以单独关：
   - 「降低 URP 画质」：渲染缩放 0.8、关抗锯齿、关阴影、关额外灯光（画面会变糊、立体感变弱）；
   - 「关闭柔边阴影（TrueShadow）」：本作有 800 多个柔边阴影，关掉后文字/立绘的阴影会消失，
     对比度会变，看不清就关掉这一项；
   - 「降低渲染分辨率」：窗口分辨率降到 75%（**默认关闭**，它会改变窗口大小）。

关闭低性能模式（再按一次组合键）会把所有改过的设置还原；插件卸载时也会还原。

**它会提醒你，但不会替你做决定**：如果连续半分钟平均低于 15 帧，会念一句
「当前帧率较低，约 N 帧。按 Alt+Shift+F2 可以开启低性能模式。」，开关始终由你决定。

**诚实说明**：在测试机上（兆芯 KX-U6780A + 集显）画面降级**没有测出帧率提升**——
那台机器是主线程瓶颈，不是显卡瓶颈。所以低性能模式真正的收益来自第 1 项；
第 2 项是给显卡弱的机器准备的，可以用配置逐项关掉。
'''
io.open(p2, 'w', encoding='utf-8-sig', newline='\n').write(t)
print('install doc ok')

# ---------- 包内 常见问题 ----------
p3 = r'D:\DSHWorkSpace\manosaba_a11y\mod\package\常见问题.md'
q = io.open(p3, encoding='utf-8-sig').read()
q = q.replace('0.0.0.12', '0.0.0.13')
q = q.rstrip() + '''

### 游戏很卡，有没有省电/省显卡的模式？（v0.0.0.13 起）

有，按 `Alt`+`Shift`+`F2` 打开「低性能模式」（默认关闭，也可以在 cfg 的「性能」组里打开）。
它会先把**补丁自己的**每帧开销压下去（无视觉代价），再按你的配置降低画面：
URP 画质、柔边阴影、渲染分辨率三档可以单独关。关掉模式会把设置全部还原。

两点说明：
- 帧率低于 15 帧时它会念一句提醒，但**不会自动切换**；
- 画面降级在测试机上没有测出提升（那台机器是主线程瓶颈），所以如果画面变差了、
  帧率没变好，直接把「降低 URP 画质」「关闭柔边阴影」关掉即可。
'''
io.open(p3, 'w', encoding='utf-8-sig', newline='\n').write(q)
print('faq ok')
