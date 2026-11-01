import io
p = r"D:\DSHWorkSpace\manosaba_a11y\tests\Suite.ps1"
s = io.open(p, encoding="utf-8-sig").read()

# --- P0-5: Clear-A11yEnv \u8865\u9f50 5 \u4e2a\u6f0f\u6389\u7684\u952e ---
old = """    foreach ($k in @('MANOSABA_A11Y_DIAG','MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_SELFTEST','MANOSABA_A11Y_JUMP',
                     'MANOSABA_A11Y_LOCALE','MANOSABA_A11Y_TEST','MANOSABA_A11Y_TEST_NAV','MANOSABA_A11Y_TEST_PLAY',
                     'MANOSABA_A11Y_TEST_KEYS','MANOSABA_A11Y_TEST_REPEAT','MANOSABA_A11Y_TEST_ESC','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_DEBUGUI','MANOSABA_A11Y_INJECT',
                     'MANOSABA_A11Y_NOPATCH')) {"""
new = """    foreach ($k in @('MANOSABA_A11Y_DIAG','MANOSABA_A11Y_KEYDIAG','MANOSABA_A11Y_SELFTEST','MANOSABA_A11Y_JUMP',
                     'MANOSABA_A11Y_JUMP_LINE','MANOSABA_A11Y_LOCALE','MANOSABA_A11Y_TEST','MANOSABA_A11Y_TEST_NAV','MANOSABA_A11Y_TEST_PLAY',
                     'MANOSABA_A11Y_TEST_KEYS','MANOSABA_A11Y_TEST_REPEAT','MANOSABA_A11Y_TEST_ESC','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_DEBUGUI','MANOSABA_A11Y_INJECT',
                     'MANOSABA_A11Y_TEST_CHOICE','MANOSABA_A11Y_TEST_MAPCHOICE','MANOSABA_A11Y_TEST_MAPFLOOR','MANOSABA_A11Y_TEST_MAPPICK_KIND',
                     'MANOSABA_A11Y_TEST_EVMODE','MANOSABA_A11Y_TEST_EVCMD','MANOSABA_A11Y_TEST_EVFORCE','MANOSABA_A11Y_TEST_EVPICK','MANOSABA_A11Y_TEST_EVYES',
                     'MANOSABA_A11Y_EVDIAG','MANOSABA_A11Y_MAPDIAG','MANOSABA_A11Y_NOPATCH')) {"""
assert old in s
s = s.replace(old, new, 1)

# --- P0-1: Skip \u8fc7\u6ee4\u4e0d\u80fd\u88ab $Only \u5305\u4f4f ---
old2 = """$selected = $Cases
if ($Only.Count -gt 0) {
    $selected = @($Cases | Where-Object { $c = $_; @($Only | Where-Object { $c.Id -like "$_*" -or $c.Group -like "*$_*" }).Count -gt 0 })
    # Skip = $true \u7684\u7528\u4f8b\u4e0d\u8fdb\u9ed8\u8ba4\u6279\u6b21\uff08\u663e\u5f0f -Only \u6307\u5b9a\u65f6\u4ecd\u7136\u4f1a\u8dd1\uff0c\u65b9\u4fbf\u6392\u67e5\uff09\u3002
    if (-not $Only -or $Only.Length -eq 0) { $selected = @($selected | Where-Object { -not ($_.ContainsKey('Skip') -and $_.Skip) }) }
    if ($selected.Count -eq 0) { Write-Host "\u6ca1\u6709\u5339\u914d\u7684\u7528\u4f8b\uff1a$($Only -join ', ')"; exit 2 }
}"""
new2 = """$selected = $Cases
# Skip = $true \u7684\u7528\u4f8b\u4e0d\u8fdb\u9ed8\u8ba4\u6279\u6b21\uff08\u663e\u5f0f -Only \u6307\u5b9a\u65f6\u4ecd\u7136\u4f1a\u8dd1\uff0c\u65b9\u4fbf\u6392\u67e5\uff09\u3002
# \u2605 \u65e7\u5199\u6cd5\u628a\u8fd9\u884c\u653e\u5728 `if ($Only.Count -gt 0)` \u91cc\u9762\uff0c\u800c\u8be5\u6761\u4ef6\u53c8\u8981\u6c42 $Only \u4e3a\u7a7a
#   \u2014\u2014 \u6c38\u8fdc\u4e0d\u6210\u7acb\uff0c\u4e8e\u662f Skip \u5b8c\u5168\u5931\u6548\uff1a\u9ed8\u8ba4\u6279\u6b21\u628a evidence.present / map.choice
#   \u8fd9\u79cd\u4f9d\u8d56\u771f\u4eba\u70b9\u51fb\u7684\u7528\u4f8b\u4e5f\u62c9\u8d77\u6765\u8dd1\u4e86\u3002
if ($Only.Count -eq 0) {
    $selected = @($Cases | Where-Object { -not ($_.ContainsKey('Skip') -and $_.Skip) })
} else {
    $selected = @($Cases | Where-Object { $c = $_; @($Only | Where-Object { $c.Id -like "$_*" -or $c.Group -like "*$_*" }).Count -gt 0 })
    if ($selected.Count -eq 0) { Write-Host "\u6ca1\u6709\u5339\u914d\u7684\u7528\u4f8b\uff1a$($Only -join ', ')"; exit 2 }
}"""
assert old2 in s
s = s.replace(old2, new2, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Suite.ps1: env keys + skip filter fixed")
