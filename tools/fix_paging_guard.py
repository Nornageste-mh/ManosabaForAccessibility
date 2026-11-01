import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """                bool navOwnsPaging = Plugin.CfgMenuNav != null && Plugin.CfgMenuNav.Value
                                     && Plugin.CfgStartupHint != null; // \u5bfc\u822a\u6a21\u5f0f\u662f\u5426\u5f00\u7740\u65e0\u6cd5\u76f4\u63a5\u8bfb\uff0c\u6545\u7528\u914d\u7f6e\u964d\u7ea7\u5224\u636e"""
new = """                // \u5bfc\u822a\u6a21\u5f0f\u72b6\u6001\u76f4\u63a5\u8bfb\u5e73\u53f0\u5c42\u7684 UiNav.Active\uff08internal\uff0c\u540c\u4e00\u7a0b\u5e8f\u96c6\uff09\u3002
                bool navOwnsPaging = UiNav.Active;"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("paging guard now uses UiNav.Active")
