import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/SelfTest.cs"
s = io.open(p, encoding="utf-8-sig").read()
old = """            CheckWiring();
            Summary();"""
new = """            CheckWiring();
            CheckScriptIndex();
            Summary();"""
assert s.count(old) == 1
s = s.replace(old, new)

anchor = """        // ---------------------------------------------------------------- TextProc"""
add = """        // ---------------------------------------------------------------- 剧本索引（剧情跳转菜单）
        /// <summary>
        /// 剧情跳转菜单的数据源是生成文件 ScriptIndex.cs（440 条剧本路径）。
        /// 它由 tools/gen_script_index.py 从 textassets 索引生成，**不含任何台词原文**。
        /// 这里只断言“表本身是好的”（数量/无重复/含已知条目），不依赖游戏运行时。
        /// </summary>
        private static void CheckScriptIndex()
        {
            Check("scriptindex.数量不少于 400", ScriptIndex.Count >= 400, "Count=" + ScriptIndex.Count);

            bool hasFirst = false;
            for (int i = 0; i < ScriptIndex.Count; i++)
                if (ScriptIndex.All[i] == "Act01_Chapter01/Act01_Chapter01_Adv01") { hasFirst = true; break; }
            Check("scriptindex.含已知剧本 Adv01", hasFirst, "");

            Check("scriptindex.含系统脚本", ScriptIndex.IndexOfPrefix("System/System_Title") >= 0,
                  "idx=" + ScriptIndex.IndexOfPrefix("System/System_Title"));

            var set = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            int dup = 0;
            for (int i = 0; i < ScriptIndex.Count; i++)
                if (!set.Add(ScriptIndex.All[i])) dup++;
            Check("scriptindex.无重复条目", dup == 0, "dup=" + dup);

            // 菜单靠下标移动，越界一定不能抛
            bool boundsOk = true;
            try
            {
                int idx = ScriptIndex.IndexOfPrefix("NoSuchScript_zzz");
                if (idx != -1) boundsOk = false;
            }
            catch { boundsOk = false; }
            Check("scriptindex.查不到返回 -1", boundsOk, "");
        }

        // ---------------------------------------------------------------- TextProc"""
assert s.count(anchor) == 1
s = s.replace(anchor, add)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("SelfTest patched")
