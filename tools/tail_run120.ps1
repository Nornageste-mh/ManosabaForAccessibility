# ---------------- 一边推进一边等「选证物」状态出现
# 目标是确认两件事：
#   1) JUMP_LINE 能不能真的把剧本落到 Trial04 的第 N 行（看 EVIDENCE dump 的 idx/script）；
#   2) 剧本走到 @choiceEvidence 时，IsPresentEvidenceMode 会不会变 true、界面长什么样。
$modeSeen = $false
$progress = @()
for ($i = 1; $i -le 70; $i++) {
    Focus-Game $h
    Click-Rel $h 0.5 0.5
    Start-Sleep -Milliseconds 1800
    if ($i % 5 -eq 0) {
        $dm = Log-Matches 'EVIDENCE dump[^\r\n]*'
        $lastDump = if ($dm.Count -gt 0) { $dm[$dm.Count - 1] } else { '<none>' }
        Write-Host "[t=$i] $lastDump"
        $progress += "[t=$i] $lastDump"
        if ($lastDump -match 'mode=True') { $modeSeen = $true; break }
    }
    # 万一弹出选项（三选一），按 1 继续
    if ($i % 10 -eq 0) { Send-Key $h 0x31 200 }
}
Shot $h (Join-Path $OutDir "$Tag-mode.png")
Write-Host "[info] modeSeen=$modeSeen，再观察 25s 让交互层跑起来…"
Start-Sleep -Seconds 25
Shot $h (Join-Path $OutDir "$Tag-mode2.png")

$lines = @()
$lines += "Tag=$Tag 时刻=$(Get-Date -Format 'HH:mm:ss')"
$lines += "JumpLine=$JumpLine"
$lines += "窗口 hwnd=$h"
$lines += "进入正片=$entered"
$lines += "选证物模式出现=$modeSeen"
$lines += "EVIDENCE 转储数=$((Log-Matches 'EVIDENCE dump').Count)"
$lines += "剧情朗读数=$((Log-Matches 'Reader\.say\[').Count)"
$lines += "---- 进度（最后 20 条）----"
$lines += ($progress | Select-Object -Last 20)
$lines += "---- EVIDENCE ----"
$lines += (Log-Matches 'EVIDENCE[^\r\n]*' | Select-Object -Last 20)
$lines += "---- JUMP ----"
$lines += (Log-Matches '\[a11y\] JUMP[^\r\n]*' | Select-Object -Last 6)
$out = Join-Path $OutDir "$Tag-summary.txt"
$lines | Set-Content -Path $out -Encoding UTF8
Write-Host "[done] 汇总: $out"
Get-Process manosaba -ErrorAction SilentlyContinue | Stop-Process -Force
