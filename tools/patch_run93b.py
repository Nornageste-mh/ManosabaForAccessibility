import io

p = r'D:\DSHWorkSpace\manosaba_a11y\probe\run93-mapreal.ps1'
s = io.open(p, encoding='utf-8-sig').read()

# 选完之后不要立刻结束：留时间看剧情有没有继续走（分辨「选对了继续」vs「选错了卡住」）
s = s.replace(
    "if (-not $pickOk -and (Text-Has 'MAPCHOICE 数字键 1 ->')) { $pickOk = $true; Write-Host '[step5] 数字键选择已触发'; Shot $h 'map-picked'; break }",
    "if (-not $pickOk -and (Text-Has 'MAPCHOICE 数字键 1 ->')) { $pickOk = $true; $pickAt = Get-Date; Write-Host '[step5] 数字键选择已触发'; Shot $h 'map-picked' }")

# 选完之后再等 30 秒，看剧情是否继续推进
s = s.replace(
    "$el = [int]((Get-Date) - $started).TotalSeconds",
    "if ($pickOk -and ((Get-Date) - $pickAt).TotalSeconds -gt 30) { Write-Host '[step6] 选择后 30s，收尾'; break }\n    $el = [int]((Get-Date) - $started).TotalSeconds")

# 汇总里补「选择之后的朗读」证据
if '---- 选择后剧情 ----' not in s:
    s = s.replace(
        "$lines += \"---- 异常（最后 5 条）----\"",
        "$pickLines = @([regex]::Matches($t, 'Reader\\.(?:say|voiced)\\[[^\\r\\n]*') | ForEach-Object { $_.Value })\n"
        "$lines += \"---- 选择后剧情（最后 8 条朗读）----\"; $lines += ($pickLines | Select-Object -Last 8)\n"
        "$lines += \"---- 异常（最后 5 条）----\"")

if '$pickAt' not in s.split('\n')[0]:
    s = s.replace("$jumpOk = $false; $choiceOk = $false; $pickOk = $false",
                  "$jumpOk = $false; $choiceOk = $false; $pickOk = $false; $pickAt = Get-Date")

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('patched run93')
