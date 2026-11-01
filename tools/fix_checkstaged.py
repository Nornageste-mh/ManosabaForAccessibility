import io, re
p = r'D:\DSHWorkSpace\manosaba_a11y\tools\check-staged.ps1'
s = io.open(p, encoding='utf-8-sig').read()
old = "        'verify/',"
new = ("        'verify/',\n"
       "        'evidence/raw/',\n"
       "        're/raw/',\n"
       "        're/lead/',")
assert old in s
s = s.replace(old, new, 1)
s = s.replace("$forbiddenExt = @('.dll', '.zip', '.exe', '.pdb')",
              "$forbiddenExt = @('.dll', '.zip', '.exe', '.pdb', '.bin', '.nson', '.bundle', '.assets')", 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('check-staged updated')
