import io
p = r'D:\DSHWorkSpace\manosaba_a11y\.gitignore'
s = io.open(p, encoding='utf-8-sig').read()
add = """
# 游戏原始资源与反编译产物一律不入库（IP 红线：见 README「我们不做什么」）
evidence/raw/
re/raw/
re/lead/
re/lead2/
*.bin
"""
if 'evidence/raw/' not in s:
    s = s.rstrip() + '\n' + add
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('gitignore updated')
