import io
src = io.open(r'D:\DSHWorkSpace\manosaba_a11y\probe\run120-trialline.ps1', encoding='utf-8-sig').read()

# 头部注释换成低性能模式说明
head_end = src.index('param(')
body_start = head_end
new_head = '''# run126-lowperf.ps1 —— 低性能模式实机取证（0.0.0.13）
#
# 目的：验证「低性能模式」四档真的能开能还原，并量一下开启前后的帧率。
#   1) 读第 1 格存档进正片（不跳剧本，保持正常播放）；
#   2) 插件侧 MANOSABA_A11Y_TEST_LOWPERF=1 → 40s 自动 Enable、75s 自动 Disable；
#   3) MANOSABA_A11Y_PERFDIAG=1 → 每 10s 一行 PERF 帧率与各模块耗时；
#   4) 汇总写 probe\\logs\\<Tag>-summary.txt（LOWPERF / PERF 行）。
param(
    [int]$WaitSeconds = 200,
    [string]$Tag = 'run126',
    [string]$Locale = 'zh-Hans'
)
'''
src = new_head + src[body_start:]

# 环境变量区：改成本轮需要的
old_env_start = src.index("$env:MANOSABA_A11Y_DIAG = '1'")
old_env_end = src.index('\n', src.index('$env:MANOSABA_A11Y_LOCALE = $Locale')) + 1
new_env = '''$env:MANOSABA_A11Y_DIAG = '1'
$env:MANOSABA_A11Y_LOCALE = $Locale
$env:MANOSABA_A11Y_PERFDIAG = '1'
$env:MANOSABA_A11Y_TEST_LOWPERF = '1'
# 其余测试钩子一律清空（避免相互干扰）
foreach ($n in @('MANOSABA_A11Y_TEST','MANOSABA_A11Y_JUMP','MANOSABA_A11Y_JUMP_LINE','MANOSABA_A11Y_TEST_NAV',
                 'MANOSABA_A11Y_TEST_REPEAT','MANOSABA_A11Y_PROBE_UI','MANOSABA_A11Y_EVDIAG',
                 'MANOSABA_A11Y_TEST_EVCMD','MANOSABA_A11Y_TEST_EVFORCE','MANOSABA_A11Y_TEST_EVPICK',
                 'MANOSABA_A11Y_TEST_EVYES','MANOSABA_A11Y_TEST_EVMODE','MANOSABA_A11Y_TEST_MAPCHOICE',
                 'MANOSABA_A11Y_TEST_MAPFLOOR','MANOSABA_A11Y_TEST_MAPPICK_KIND','MANOSABA_A11Y_MAPDIAG',
                 'MANOSABA_A11Y_TEST_ESC','MANOSABA_A11Y_TEST_KEYS','MANOSABA_A11Y_TEST_SCRIPTJUMP',
                 'MANOSABA_A11Y_TEST_CHOICE','MANOSABA_A11Y_KEYDIAG')) {
    Set-Item -Path ("env:" + $n) -Value '' -ErrorAction SilentlyContinue
}
'''
src = src[:old_env_start] + new_env + src[old_env_end:]
io.open(r'D:\DSHWorkSpace\manosaba_a11y\probe\run126-lowperf.ps1', 'w', encoding='utf-8-sig', newline='\r\n').write(src)
print('written, lines=', src.count('\n'))
