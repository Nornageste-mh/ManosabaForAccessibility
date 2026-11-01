import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\package\安装说明.md"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("v0.0.0.9", "v0.0.0.10")
# \u5220\u6389\u5bf9\u5df2\u5220\u9664\u5f00\u5173\u7684\u627f\u8bfa
s = s.replace("\u5feb\u8fdb\u505c\u6b62\u65f6\u8865\u5ff5\u5f53\u524d\u53e5", "\uff08\u5df2\u79fb\u9664\uff1a\u672c\u7248\u672a\u5b9e\u73b0\uff09")
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)

p2 = r"D:\DSHWorkSpace\manosaba_a11y\mod\package\常见问题.md"
t = io.open(p2, encoding="utf-8-sig").read()
t = t.replace("v0.0.0.9", "v0.0.0.10")
# \u4fee\u6b63\u201c\u9000\u51fa\u524d\u4e8c\u6b21\u786e\u8ba4\u9ed8\u8ba4\u5f00\u7740\u201d\u7684\u8bf4\u6cd5\uff08\u73b0\u5728\u771f\u7684\u751f\u6548\u4e86\uff09
t = t.replace("Exit@@", "Button_Exit")
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(t)
print("package docs updated")
