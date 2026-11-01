import io
p = r'D:\DSHWorkSpace\manosaba_a11y\tests\README.md'
s = io.open(p, encoding='utf-8-sig').read()

# 单元断言数量 26 -> 54（两处）
s = s.replace('26 条断言', '54 条断言', 1)
s = s.replace('| 26 条全 PASS，`fail=0` |', '| 54 条全 PASS，`fail=0` |', 1)

# 用例表补 perf.lowmode 行
anchor = '| `map.choice` | 界面/地图 | 地图页选择地点（默认批次跳过） |'
i = s.index(anchor)
lineEnd = s.index('\n', i) + 1
row = '| `perf.lowmode` | 模块/性能 | 低性能模式 | `TEST_LOWPERF=1` 40s 开、75s 关；`LOWPERF URP: renderScale=0.80`；`关闭 579 个柔边阴影组件`；关闭后 `已还原画质设置` + `还原 579 个`；无开启/关闭失败 |\n'
s = s[:lineEnd] + row + s[lineEnd:]

# 覆盖范围行补「低性能模式」
s = s.replace('标题导航 / 对话 / 说话人 / 配音闸门 / 退格重读 / 选项 / 辩论 / 加载 / 语言',
              '标题导航 / 对话 / 说话人 / 配音闸门 / 退格重读 / 选项 / 辩论 / 加载 / 语言 / 整页界面 / 低性能模式', 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('tests README ok')
