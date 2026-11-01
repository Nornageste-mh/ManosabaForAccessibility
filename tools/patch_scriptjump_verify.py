import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/ScriptJump.cs"
s = io.open(p, encoding="utf-8-sig").read()

# 1) 跳转后 3 秒回读剧本状态（证明跳转真的生效，而不是只发了调用）
old = """                _active = false;
                A11yHost.Diag("SCRIPTJUMP 跳转 path=" + path + " line=" + line);"""
new = """                _active = false;
                _verifyAt = Time.realtimeSinceStartup + 3f;
                _verifyPath = path;
                A11yHost.Diag("SCRIPTJUMP 跳转 path=" + path + " line=" + line);"""
assert s.count(old) == 1
s = s.replace(old, new)

# 2) 字段
old2 = """        private static float _lastKeyAt = -10f;"""
new2 = """        private static float _lastKeyAt = -10f;
        private static float _verifyAt = -1f;
        private static string _verifyPath = "";"""
assert s.count(old2) == 1
s = s.replace(old2, new2)

# 3) Update 开头做一次「跳转后回读」
old3 = """                if (Plugin.CfgAllowScriptJump == null || !Plugin.CfgAllowScriptJump.Value) return;"""
new3 = """                // 跳转后回读：LoadAndPlayAtLine 是异步的，只有回读剧本路径才能证明真的到了。
                if (_verifyAt > 0f && Time.realtimeSinceStartup >= _verifyAt)
                {
                    _verifyAt = -1f;
                    VerifyJump(_verifyPath);
                }

                if (Plugin.CfgAllowScriptJump == null || !Plugin.CfgAllowScriptJump.Value) return;"""
assert s.count(old3) == 1
s = s.replace(old3, new3)

# 4) VerifyJump 方法
old4 = """        private static int CurrentIndex()"""
new4 = """        /// <summary>跳转 3 秒后回读玩家剧本状态 —— 这是「跳转真的生效」的唯一硬证据。</summary>
        private static void VerifyJump(string wantPath)
        {
            try
            {
                IScriptPlayer sp;
                if (!TryGetPlayer(out sp)) return;
                string cur = sp.PlayedScript != null ? (sp.PlayedScript.Path ?? "") : "";
                A11yHost.Diag("SCRIPTJUMP 跳转后: want=" + wantPath + " got=" + cur
                              + " playing=" + sp.Playing + " idx=" + sp.PlayedIndex);
                if (!string.Equals(cur, wantPath, StringComparison.OrdinalIgnoreCase))
                    Repeat.Say("注意：跳转后的剧本是 " + cur + "，不是 " + wantPath + "。", true);
            }
            catch (Exception e) { A11yHost.Diag("ScriptJump: 跳转回读失败 " + e.Message); }
        }

        private static int CurrentIndex()"""
assert s.count(old4) == 1
s = s.replace(old4, new4)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("ScriptJump patched with post-jump verify")
