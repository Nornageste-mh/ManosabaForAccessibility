import io
p = "D:/DSHWorkSpace/manosaba_a11y/tests/Suite.ps1"
s = io.open(p, encoding="utf-8-sig").read()
old = """                $db = Advance-Until $Game -Pattern 'Reader\\.say\\[debate:add\\][^\\r\\n]*' -MinCount 3 -TimeoutSec 240
                $hit = if ($db.Ok) { @($db.Matches) } else { $null }
                Add-Check $Result '\u8fa9\u8bba\u53f0\u8bcd\u8d70 debate \u901a\u9053 \u22653 \u53e5' ($null -ne $hit) ("\u53e5\u6570 " + ($hit | Measure-Object).Count)
                if ($hit) {
                    $clean = @($hit | Where-Object { $_ -match '=> ' })
                    Add-Check $Result '\u8fa9\u8bae\u6587\u672c\u5df2\u6e05\u6d17\uff08\u6709 => \u8f93\u51fa\uff09' ($clean.Count -ge 1) ("\u6e05\u6d17 " + $clean.Count)
                    $Result.Evidence += ($hit | Select-Object -First 3 | ForEach-Object { $_.Trim() })
                }"""
new = """                # 0.0.0.11\uff1a\u8bae\u8bba\u53f0\u8bcd\u9ed8\u8ba4**\u4e0d\u6717\u8bfb**\uff08\u5e26\u914d\u97f3\uff09\uff0c\u53ea\u8fdb\u91cd\u8bfb\u7f13\u51b2\uff0c
                #   \u6240\u4ee5\u65ad\u8a00\u8981\u770b `Reader.skipped[debate]`\uff08\u800c\u4e0d\u662f `Reader.say[debate:add]`\uff09\uff1b
                #   \u540c\u65f6\u53cd\u5411\u65ad\u8a00\uff1a\u4e0d\u5141\u8bb8\u51fa\u73b0 `Reader.say[debate` \u2014\u2014 \u90a3\u610f\u5473\u7740\u8bae\u8bba\u53f0\u8bcd\u88ab
                #   \u81ea\u52a8\u6717\u8bfb\u4e86\uff0c\u4e0e\u8bed\u97f3\u53e0\u5728\u4e00\u8d77\u3002
                $db = Advance-Until $Game -Pattern 'Reader\\.skipped\\[debate\\][^\\r\\n]*' -MinCount 3 -TimeoutSec 240
                $hit = if ($db.Ok) { @($db.Matches) } else { $null }
                Add-Check $Result '\u8bae\u8bba\u53f0\u8bcd\u8fdb\u91cd\u8bfb\u7f13\u51b2\u4e14\u4e0d\u6717\u8bfb \u22653 \u53e5' ($null -ne $hit) ("\u53e5\u6570 " + ($hit | Measure-Object).Count)
                $spoke = @(Get-LogMatches 'Reader\\.say\\[debate')
                Add-Check $Result '\u8bae\u8bba\u53f0\u8bcd\u6ca1\u6709\u88ab\u81ea\u52a8\u6717\u8bfb\uff08\u53cd\u5411\uff09' ($spoke.Count -eq 0) ("\u547d\u4e2d " + $spoke.Count)
                if ($hit) {
                    $clean = @($hit | Where-Object { $_ -match '\u53ea\u8fdb\u91cd\u8bfb\u7f13\u51b2: \\S' })
                    Add-Check $Result '\u8bae\u8bba\u6587\u672c\u5df2\u6e05\u6d17\uff08\u6709\u6587\u672c\u5185\u5bb9\uff09' ($clean.Count -ge 1) ("\u6e05\u6d17 " + $clean.Count)
                    $Result.Evidence += ($hit | Select-Object -First 3 | ForEach-Object { $_.Trim() })
                }"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("trial.debate case updated for silent debate")
