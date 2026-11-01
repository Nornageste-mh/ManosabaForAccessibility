import io
base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

# ---------- 1) Hotkey.Parse: \u652f\u6301 "=" \u4e3a KeyCode.Equals ----------
p = base + r"\Hotkey.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """                if (u == "PLUS" || t == "加号") { plusToken = true; keyName = "Plus"; continue; }"""
new = """                if (u == "PLUS" || t == "加号") { plusToken = true; keyName = "Plus"; continue; }

                // 等号（0.0.0.11）：玩家要求「审慎避让游戏自带键」，重读键默认从 Backspace 改成 `=`。
                // 为什么必须显式映射：`Enum.TryParse("=")` 不认这个符号，会报「Unity 不认识这个键名」；
                // 而 KeyCode.Equals 才是主键盘那个 `=` 键（小键盘回车旁边的不算）。
                if (t == "=" || t == "＝" || u == "EQUALS") { keyName = "Equals"; continue; }"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Hotkey: '=' supported")

# ---------- 2) InputSysKeys: KeyCode.Equals -> Key.Equals ----------
p = base + r"\InputSysKeys.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """            { KeyCode.Return, Key.Enter },"""
new = """            { KeyCode.Return, Key.Enter },
            { KeyCode.Equals, Key.Equals },"""
assert old in s
s = s.replace(old, new, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("InputSysKeys: Equals mapped")
