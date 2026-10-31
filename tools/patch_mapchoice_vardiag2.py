import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()

block = """                // 选完 1.5 秒后把游戏自己的 chosenAreaId / chosenAreaFlag 打出来：
                // 这是判断「我们的点击到底被游戏接受成了哪个地点」的唯一权威信号。
                if (_choiceDiagAt > 0f && Time.unscaledTime >= _choiceDiagAt)
                {
                    _choiceDiagAt = 0f;
                    DumpChosenVariables();
                }

"""
if block not in s:
    raise SystemExit('diag block not found')
s = s.replace(block, '', 1)

anchor = """                if (_entries.Count == 0) return;"""
if anchor not in s:
    raise SystemExit('entries early-return not found')
s = s.replace(anchor, block + anchor, 1)

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('moved chosenArea diag above early return')
