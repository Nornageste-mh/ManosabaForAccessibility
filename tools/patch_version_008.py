import io

base = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y'

# csproj
p = base + r'\ManosabaA11y.csproj'
s = io.open(p, encoding='utf-8-sig').read()
if '<Version>0.0.0.7</Version>' not in s:
    raise SystemExit('csproj version not found')
s = s.replace('<Version>0.0.0.7</Version>', '<Version>0.0.0.8</Version>', 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)

# Plugin.cs
p = base + r'\Plugin.cs'
s = io.open(p, encoding='utf-8-sig').read()
if 'Version = "0.0.0.7"' not in s:
    raise SystemExit('Plugin version not found')
s = s.replace('Version = "0.0.0.7"', 'Version = "0.0.0.8"', 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('version -> 0.0.0.8')
