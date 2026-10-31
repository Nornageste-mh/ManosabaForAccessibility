import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs'
s = io.open(p, encoding='utf-8-sig').read()
old = """        public void Update()
        {
            _ticks++;
"""
new = """        public void Update()
        {
            _ticks++;

            // 地图页数字键选择要靠 InputSystem 事件队列注一次真实点击（run117/run118：
            // 合成指针事件游戏不认），所以 Tick 必须每帧都走，不能只在测试注入模式下开。
            try { InputInject.Tick(); } catch { }
"""
if old not in s:
    raise SystemExit('update head not found')
s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('plugin: InputInject.Tick unconditional')
