import io

p = r'D:\DSHWorkSpace\manosaba_a11y\tests\Suite.ps1'
s = io.open(p, encoding='utf-8-sig').read()

old = """            $ann = @{ Ok = $false; Line = '' }
            for ($i = 1; $i -le 20; $i++) {
                $ann = Wait-Log -Pattern 'MAPCHOICE 播报 \\d+ 个地点' -TimeoutSec 12
                if ($ann.Ok) { break }
                $null = Click-Rel $Game.Hwnd 0.06 0.06
            }"""
assert old in s, 'loop anchor'

new = """            $ann = @{ Ok = $false; Line = '' }
            $answered = $false
            for ($i = 1; $i -le 26; $i++) {
                $ann = Wait-Log -Pattern 'MAPCHOICE 播报 \\d+ 个地点' -TimeoutSec 12
                if ($ann.Ok) { break }
                # Adv18 有一个 3 选 1 的选项挡在路上（截图 map-choice.png 实证：三张纸卷
                # 「回答：逃往招待所/逃往监牢/总之先逃吧」）。左上角点击推不动选项，
                # 必须点选项按钮本身；第 1 个选项按钮中心 ≈ 相对 (0.5, 0.365)。
                if (-not $answered -and @(Get-LogMatches '共 3 个选项').Count -gt 0) {
                    $null = Click-Rel $Game.Hwnd 0.5 0.365
                    $answered = $true
                    Start-Sleep -Seconds 2
                }
                $null = Click-Rel $Game.Hwnd 0.06 0.06
            }"""

s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('suite map.choice option-click added')
