import io
p = "D:/DSHWorkSpace/manosaba_a11y/tests/Suite.ps1"
lines = io.open(p, encoding="utf-8-sig").read().split("\n")
start = next(i for i, l in enumerate(lines) if "Reader\\.say\\[debate:add\\]" in l)
end = next(i for i, l in enumerate(lines) if i > start and "Shot $Game.Hwnd 'trial-debate'" in l)
new = [
"                # 0.0.0.11\uff1a\u8bae\u8bba\u53f0\u8bcd\u9ed8\u8ba4**\u4e0d\u6717\u8bfb**\uff08\u5e26\u914d\u97f3\uff09\uff0c\u53ea\u8fdb\u91cd\u8bfb\u7f13\u51b2\uff0c",
"                #   \u6240\u4ee5\u65ad\u8a00\u8981\u770b `Reader.skipped[debate]`\uff08\u800c\u4e0d\u662f `Reader.say[debate:add]`\uff09\u3002",
"                $db = Advance-Until $Game -Pattern 'Reader\\.skipped\\[debate\\][^\\r\\n]*' -MinCount 3 -TimeoutSec 240",
"                $hit = if ($db.Ok) { @($db.Matches) } else { $null }",
"                Add-Check $Result '\u8bae\u8bba\u53f0\u8bcd\u8fdb\u91cd\u8bfb\u7f13\u51b2\u4e14\u4e0d\u6717\u8bfb \u22653 \u53e5' ($null -ne $hit) (\"\u53e5\u6570 \" + ($hit | Measure-Object).Count)",
"                $spoke = @(Get-LogMatches 'Reader\\.say\\[debate')",
"                Add-Check $Result '\u8bae\u8bba\u53f0\u8bcd\u6ca1\u6709\u88ab\u81ea\u52a8\u6717\u8bfb\uff08\u53cd\u5411\uff09' ($spoke.Count -eq 0) (\"\u547d\u4e2d \" + $spoke.Count)",
"                if ($hit) {",
"                    $Result.Evidence += ($hit | Select-Object -First 3 | ForEach-Object { $_.Trim() })",
"                }",
]
out = lines[:start] + new + lines[end:]
io.open(p, "w", encoding="utf-8-sig", newline="\n").write("\n".join(out))
print("replaced lines", start, "..", end)
