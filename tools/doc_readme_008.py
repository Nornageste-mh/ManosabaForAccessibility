import io

p = r'D:\DSHWorkSpace\manosaba_a11y\README.md'
s = io.open(p, encoding='utf-8-sig').read()

s = s.replace('## 当前进度（v0.0.0.7）', '## 当前进度（v0.0.0.8）', 1)

row = '| 魔女手册方向键逐项移动 | ⚠️ 代码路径与标题菜单相同，但自动化合成扩展键不稳定，**未复测**（请实机按方向键确认） |'
assert row in s, 'row'
new = row + (
    '\n| **地图页选择地点（`@choiceMapArea`）** | ✅ 实机验证（run122）：读游戏自己的允许名单，'
    '一次念全 8 个地点（含 2F/地下1F 跨层），数字键 3/7/8 分别走食堂/娱乐室/牢房分支；'
    '名单超 9 项时 `PageUp`/`PageDown` 翻页 |'
    '\n| 地图浏览态楼层切换（`PageUp`/`PageDown`） | ⚠️ 只编译验证：选择态禁用切层（会软锁），浏览态切层未单独复测 |'
)
s = s.replace(row, new, 1)

old = '| `PageUp` / `PageDown` | 切换面板组 |'
assert old in s, 'pageup'
s = s.replace(old, '| `PageUp` / `PageDown` | 切换面板组；**地图选择时**翻页（名单超过 9 项）；地图浏览时切楼层 |', 1)

anchor = '13. **`PrintWindow` 对 DXGI 翻转模型窗口会返回白图**'
assert anchor in s, 'pitfall13'
i = s.index(anchor)
j = s.index('\n\n', i)
add = (
    '\n\n14. **本作的按钮不能靠合成派发点**：`onClick.Invoke()` 与 `ExecuteEvents.pointerClickHandler`\n'
    '    都返回成功（诊断 `点击派发 ok=True`），但 `MapPage.OnChoiceMapAreaAsync` 的 await 一直不返回。\n'
    '    必须 `InputInject.Click(screenX, screenY)` 往 `InputSystem` 事件队列塞真实的按下/抬起。\n'
    '15. **地图选择等待期间切楼层会软锁**：点手册底部胶片条（`WitchBookItemButton`）会把\n'
    '    `OnChoiceMapAreaAsync` 的 await 挂断，加载界面永久卡在可见状态（run105/106）。选择态必须禁用切层。\n'
    '16. **自动化脚本的「点屏幕中央」会抢先完成地图选择**：run115 里数字键选娱乐室却走了玄关大厅分支，\n'
    '    原因就是脚本每 3 秒点中央真点中了默认高亮的地点。检测到选择态后必须立刻停止点击。'
)
s = s[:j] + add + s[j:]
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('README updated')
