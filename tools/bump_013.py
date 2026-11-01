import io
p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\ManosabaA11y.csproj'
s = io.open(p, encoding='utf-8').read()
assert '<Version>0.0.0.12</Version>' in s
s = s.replace('<Version>0.0.0.12</Version>', '<Version>0.0.0.13</Version>', 1)
io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print('csproj version -> 0.0.0.13')
