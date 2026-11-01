import io
p = r"D:\DSHWorkSpace\manosaba_a11y\tools\check-staged.ps1"
s = io.open(p, encoding="utf-8-sig").read()
old = """    $staged = @(git -c core.quotepath=false diff --cached --name-only)"""
new = """    # 只检查**新增/修改**的文件；删除条目（D）也要从 diff --cached 里出来，
    # 但“删掉一份游戏原文”恰恰是我们想要的动作，不能算违规。
    $staged = @(git -c core.quotepath=false diff --cached --name-status |
                Where-Object { $_ -notmatch '^D' } |
                ForEach-Object { ($_ -split "`t")[-1] })"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("check-staged now ignores deletions")
