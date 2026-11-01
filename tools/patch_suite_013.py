import io
p = r'D:\DSHWorkSpace\manosaba_a11y\tests\Suite.ps1'
s = io.open(p, encoding='utf-8-sig').read()

anchor = '''    # ---------------------------------------------------------------- 整页界面层'''
assert anchor in s
case = '''    # ---------------------------------------------------------------- 性能模块
    @{
        # 低性能模式不需要点鼠标：插件内部按 TEST_LOWPERF=1 在 40s 自动开启、75s 自动关闭，
        # 我们只验三件事——开启了、改的确实是 URP/TrueShadow、关闭后全部还原。
        Id = 'perf.lowmode'; Title = '低性能模式（开启 → 画面降级 → 关闭还原）'; Group = '模块/性能'
        Env = @{ MANOSABA_A11Y_DIAG = '1'; MANOSABA_A11Y_PERFDIAG = '1'; MANOSABA_A11Y_TEST_LOWPERF = '1' }
        Timeout = 180
        Body = {
            param($Game, $Result)
            $ready = Wait-Log -Pattern 'loaded v[0-9.]+ \\(IL2CPP' -TimeoutSec 60
            Add-Check $Result '插件加载' $ready.Ok $ready.Line

            $on = Wait-Log -Pattern 'LOWPERF 已开启' -TimeoutSec 90
            Add-Check $Result '低性能模式已开启' $on.Ok $on.Line
            $urp = @(Get-LogMatches 'LOWPERF URP: renderScale=0\\.80')
            Add-Check $Result 'URP 画质已降级' ($urp.Count -ge 1) ("命中 " + $urp.Count)
            $shadow = @(Get-LogMatches 'LOWPERF TrueShadow: 关闭 \\d+ 个')
            Add-Check $Result '柔边阴影已关闭' ($shadow.Count -ge 1) ("命中 " + $shadow.Count)

            $off = Wait-Log -Pattern 'LOWPERF 已关闭' -TimeoutSec 90
            Add-Check $Result '低性能模式已关闭' $off.Ok $off.Line
            $back = @(Get-LogMatches 'LOWPERF URP: 已还原画质设置')
            Add-Check $Result '画质设置已还原' ($back.Count -ge 1) ("命中 " + $back.Count)
            $restoreShadow = @(Get-LogMatches 'LOWPERF TrueShadow: 还原 \\d+ 个')
            Add-Check $Result '柔边阴影已还原' ($restoreShadow.Count -ge 1) ("命中 " + $restoreShadow.Count)

            $err = @(Get-LogMatches 'LOWPERF 开启失败|LOWPERF 关闭失败')
            Add-Check $Result '没有开启/关闭失败（反向）' ($err.Count -eq 0) ("命中 " + $err.Count)

            if ($on.Ok) { $Result.Evidence += $on.Line.Trim() }
            if ($off.Ok) { $Result.Evidence += $off.Line.Trim() }
        }
    },

'''
s = s.replace(anchor, case + anchor, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('suite case added')
