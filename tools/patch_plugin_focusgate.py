import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/Plugin.cs"
s = io.open(p, encoding="utf-8-sig").read()

# 1) FocusGate：剧情跳转菜单打开时让开数字键（菜单自己要用 0-9 输入行号）
old = """                bool ok;
                try { ok = UnityEngine.Application.isFocused; }
                catch { ok = true; }"""
new = """                bool ok;
                try { ok = UnityEngine.Application.isFocused; }
                catch { ok = true; }
                // 剧情跳转菜单（开发者功能）打开时，0-9 是「输入起始行」，
                // 不允许同时被选项/地图/证物层读走。
                if (ok && ScriptJump.Active) ok = false;"""
assert s.count(old) == 1
s = s.replace(old, new)

# 2) TEST_SCRIPTJUMP 环境变量
old2 = """            string evCmd = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_EVCMD");"""
new2 = """            // 自动化：从插件内部驱动「剧情跳转」菜单（仅 MANOSABA_A11Y_TEST_SCRIPTJUMP=1）
            if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_SCRIPTJUMP") == "1")
            {
                TestScriptJump = true;
                L.LogInfo("[a11y] TEST SCRIPTJUMP: 45s 后自动打开跳转菜单并跳转（自动化用）");
            }

            string evCmd = Environment.GetEnvironmentVariable("MANOSABA_A11Y_TEST_EVCMD");"""
assert s.count(old2) == 1
s = s.replace(old2, new2)

# 3) 测试钩子总闸里要能放行 TestScriptJump（它在 hooksAllowed 之后读取，检查顺序）
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("ok")
