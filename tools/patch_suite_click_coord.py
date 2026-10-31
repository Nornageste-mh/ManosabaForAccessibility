import io

p = r'D:\DSHWorkSpace\manosaba_a11y\tests\Suite.ps1'
s = io.open(p, encoding='utf-8-sig').read()
old = '$null = Click-Rel $Game.Hwnd 0.06 0.06'
assert old in s, 'click anchor'
new = ('$null = Click-Rel $Game.Hwnd 0.5 0.06   '
       '# 顶部中间：不是按钮（(0.06,0.06) 是「图鉴」按钮，会把手册反复点开成死循环）')
s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('click coord changed')
