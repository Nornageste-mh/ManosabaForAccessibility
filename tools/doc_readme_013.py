import io
p = r'D:\DSHWorkSpace\manosaba_a11y\README.md'
s = io.open(p, encoding='utf-8').read()

# 1) 把误插在表格中间的引用块移到表格之后
mid = '''
> `0.0.0.11` 起默认重读键是 `=`（Backspace 会与本作自带的“返回上一句”冲突）；
> 审判议论的台词默认不自动朗读（带配音），但每句都进重读缓冲，按 `=` 一样能听。
'''
assert mid in s
s = s.replace(mid, '', 1)

# 2) 表格里补一行（放在 F7 之后）
anchor = '| `F7` | 切换语音后端（自动 → Tolk → NVDA → 争渡 → SAPI），并念出新的后端名 |\n'
assert anchor in s
s = s.replace(anchor, anchor + '| `Alt`+`Shift`+`F2` | 开启 / 关闭**低性能模式**（默认关闭；见下「低性能模式」） |\n', 1)

# 3) 表格之后补回引用块 + 低性能模式说明
anchor2 = '**为什么导航不是 Tab 了**'
assert anchor2 in s
note = '''> `0.0.0.11` 起默认重读键是 `=`（Backspace 会与本作自带的“返回上一句”冲突）；
> 审判议论的台词默认不自动朗读（带配音），但每句都进重读缓冲，按 `=` 一样能听。

**低性能模式（`0.0.0.13` 起，默认关闭）**：按 `Alt`+`Shift`+`F2` 或打开配置
「性能 / 低性能模式」。它会做两件事——**降低插件自己的每帧开销**（无视觉代价），
以及按你的配置降低画面（URP 画质 / 柔边阴影 / 分辨率，三档可单独关）。
实测结论：这台测试机是**主线程**瓶颈，画面降级没有测出帧率提升；真正省下来的
是插件自己的 10~13ms/帧。关掉模式会把所有改过的设置还原。

'''
s = s.replace(anchor2, note + anchor2, 1)

# 4) 进度表标题版本
s = s.replace('## 当前进度（v0.0.0.9）', '## 当前进度（v0.0.0.13）', 1)

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('readme patched')
