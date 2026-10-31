import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()

old_id = '                try { if (vars.VariableExists("chosenAreaId")) id = vars.GetVariableValue("chosenAreaId").ToString(); }'
new_id = ('                try { if (vars.VariableExists("chosenAreaId")) { Naninovel.CustomVariableValue v = vars.GetVariableValue("chosenAreaId"); '
          'id = v.Type + ":" + v.String + "/" + v.Number + "/" + v.Boolean; } }')
old_flag = '                try { if (vars.VariableExists("chosenAreaFlag")) flag = vars.GetVariableValue("chosenAreaFlag").ToString(); }'
new_flag = ('                try { if (vars.VariableExists("chosenAreaFlag")) { Naninovel.CustomVariableValue v = vars.GetVariableValue("chosenAreaFlag"); '
            'flag = v.Type + ":" + v.String + "/" + v.Number + "/" + v.Boolean; } }')

if old_id not in s or old_flag not in s:
    raise SystemExit('variable diag lines not found')
s = s.replace(old_id, new_id, 1)
s = s.replace(old_flag, new_flag, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('variable diag prints typed value')
