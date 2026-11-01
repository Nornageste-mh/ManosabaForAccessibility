using System;
using Il2CppInterop.Runtime.InteropTypes;
using Naninovel;
using UnityEngine;

namespace ManosabaA11y
{
    /// <summary>
    /// 开发者功能：**跳转到指定剧情**（默认关闭，组合键 Ctrl+Alt+Shift+F3）。
    ///
    /// 为什么是「跳转剧情」而不是「打开游戏内置开发者面板」：
    ///   v0.0.0.6 已实机查清：正式发行版里游戏**没有**创建调试管理器
    ///   （`DebugOptionsBase.s_debugManager == null`），`DebugPanel` 只是内存里的预制体、
    ///   从未被实例化，所以那条路在发行版里根本走不通。
    ///   而「跳到剧本某一行」我们已经在 0.0.0.9 用
    ///   `Naninovel.ScriptPlayerExtensions.LoadAndPlayAtLine` 验证过（当时靠环境变量
    ///   `MANOSABA_A11Y_JUMP_LINE` 跳到 `Trial04` 第 327 行，五秒内到位），
    ///   现在把它做成**玩家可操作的菜单**。
    ///
    /// 无障碍纪律：
    ///   · 这是**开发者功能**：会跳到没看过的剧情、破坏存档进度、可能剧透。
    ///     配置项「允许剧情跳转（开发者）」默认关闭，只有玩家主动打开才响应组合键。
    ///   · 不做任何自动跳转：没有定时器、没有「卡住就帮你跳」的逻辑。
    ///   · 播报走 <see cref="Repeat"/>（和导航播报同一条出口），不写进「可重读」的剧情缓冲，
    ///     避免把菜单提示当成剧情内容被重读出来。
    /// </summary>
    internal static class ScriptJump
    {
        private const string Hotkey = "Ctrl+Alt+Shift+F3";

        private static bool _active;
        private static int _index;
        private static int _line;
        private static string _lineBuf = "";
        private static float _lastKeyAt = -10f;
        private static float _verifyAt = -1f;
        private static string _verifyPath = "";
        private static bool _warnedNoPlayer;

        /// <summary>菜单是否打开（其它交互层据此让开数字键/方向键）。</summary>
        internal static bool Active => _active;

        internal static void Update()
        {
            try
            {
                // 跳转后回读：LoadAndPlayAtLine 是异步的，只有回读剧本路径才能证明真的到了。
                if (_verifyAt > 0f && Time.realtimeSinceStartup >= _verifyAt)
                {
                    _verifyAt = -1f;
                    VerifyJump(_verifyPath);
                }

                if (Plugin.CfgAllowScriptJump == null || !Plugin.CfgAllowScriptJump.Value) return;

                if (Plugin.SpecDown(Hotkey)) { Toggle(); return; }
                if (!_active) return;

                if (Pressed(KeyCode.Escape)) { Exit(); return; }
                if (Pressed(KeyCode.UpArrow)) { Move(-1); return; }
                if (Pressed(KeyCode.DownArrow)) { Move(+1); return; }
                if (Pressed(KeyCode.PageUp)) { Move(-10); return; }
                if (Pressed(KeyCode.PageDown)) { Move(10); return; }
                if (Pressed(KeyCode.Home)) { _index = 0; _lineBuf = ""; _line = 0; Announce(); return; }
                if (Pressed(KeyCode.End)) { _index = ScriptIndex.Count - 1; _lineBuf = ""; _line = 0; Announce(); return; }

                // 行号输入：直接敲数字（最多 5 位），回车按当前行跳转。
                if (Pressed(KeyCode.Backspace))
                {
                    _lineBuf = "";
                    _line = 0;
                    Repeat.Say("起始行清零。", true);
                    return;
                }
                for (int i = 0; i <= 9; i++)
                {
                    if (Pressed(KeyCode.Alpha0 + i) || Pressed(KeyCode.Keypad0 + i))
                    {
                        if (_lineBuf.Length < 5) _lineBuf += i.ToString();
                        _line = _lineBuf.Length == 0 ? 0 : int.Parse(_lineBuf);
                        Repeat.Say("起始行 " + _line + "。", true);
                        return;
                    }
                }

                if (Pressed(KeyCode.Return) || Pressed(KeyCode.KeypadEnter)) { Jump(); return; }
            }
            catch (Exception e) { A11yHost.Diag("ScriptJump: " + e.Message); }
        }

        // ==================================================================== 菜单

        private static void Toggle()
        {
            if (_active) { Exit(); return; }

            _active = true;
            _lineBuf = "";
            _line = 0;
            _index = CurrentIndex();
            A11yHost.Diag("SCRIPTJUMP 打开菜单 index=" + _index + " 共 " + ScriptIndex.Count + " 个剧本");
            Repeat.Say("剧情跳转。当前 " + CurrentName() + "，共 " + ScriptIndex.Count
                       + " 个剧本。上下键换剧本，翻页键跳十个，数字键输入起始行，回车跳转，Esc 退出。"
                       + "注意：这是开发者功能，会跳过未读剧情。", true);
        }

        private static void Exit()
        {
            _active = false;
            A11yHost.Diag("SCRIPTJUMP 关闭菜单");
            Repeat.Say("已退出剧情跳转。", true);
        }

        private static void Move(int delta)
        {
            int next = _index + delta;
            if (next < 0) next = 0;
            if (next >= ScriptIndex.Count) next = ScriptIndex.Count - 1;
            if (next == _index) { Repeat.Say("到头了。", true); return; }
            _index = next;
            Announce();
        }

        private static void Announce()
        {
            A11yHost.Diag("SCRIPTJUMP 选中 index=" + _index + " path=" + CurrentPath() + " line=" + _line);
            Repeat.Say(CurrentName() + "。第 " + (_index + 1) + " 个，共 " + ScriptIndex.Count + " 个。", true);
        }

        private static string CurrentPath()
        {
            if (_index < 0 || _index >= ScriptIndex.Count) return "";
            return ScriptIndex.All[_index];
        }

        /// <summary>把 `Act01_Chapter01/Act01_Chapter01_Adv01` 念成「第1章 Act01 第01 节 Adv01」太难听，
        /// 这里只把路径本身念出来（剧本名是玩家/开发者都认识的标识）。</summary>
        private static string CurrentName()
        {
            string p = CurrentPath();
            return p.Length == 0 ? "（无）" : p;
        }

        // ==================================================================== 跳转

        private static void Jump()
        {
            IScriptPlayer sp;
            if (!TryGetPlayer(out sp)) return;

            string path = CurrentPath();
            if (path.Length == 0) { Repeat.Say("没有选中剧本。", true); return; }

            int line = _line < 0 ? 0 : _line;
            try
            {
                Naninovel.UniTaskExtensions.Forget(
                    Naninovel.ScriptPlayerExtensions.LoadAndPlayAtLine(sp, path, line, 0));
                _active = false;
                // 跳转时若画面正停在选项上，游戏的选择状态还挂着，跳过去可能不立刻打印台词。
                // 这是开发功能，不替玩家做选择，只提醒一句。
                if (Choices.HasChoice)
                    Repeat.Say("注意：画面刚才停在选项上，跳转后如果没声音，先按数字键选一项。", true);
                _verifyAt = Time.realtimeSinceStartup + 3f;
                _verifyPath = path;
                A11yHost.Diag("SCRIPTJUMP 跳转 path=" + path + " line=" + line);
                Repeat.Say("已跳到 " + path + "，从第 " + line + " 行开始。", true);
            }
            catch (Exception e)
            {
                A11yHost.Diag("SCRIPTJUMP 跳转失败: " + e.Message);
                Repeat.Say("跳转失败：" + e.Message, true);
            }
        }

        /// <summary>跳转 3 秒后回读玩家剧本状态 —— 这是「跳转真的生效」的唯一硬证据。</summary>
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

        private static int CurrentIndex()
        {
            try
            {
                IScriptPlayer sp;
                if (!TryGetPlayer(out sp)) return 0;
                string cur = sp.PlayedScript != null ? (sp.PlayedScript.Path ?? "") : "";
                if (cur.Length > 0)
                {
                    for (int i = 0; i < ScriptIndex.Count; i++)
                        if (string.Equals(ScriptIndex.All[i], cur, StringComparison.OrdinalIgnoreCase)) return i;
                }
            }
            catch (Exception e) { A11yHost.Diag("ScriptJump: 当前剧本定位失败 " + e.Message); }
            return 0;
        }

        private static bool TryGetPlayer(out IScriptPlayer sp)
        {
            sp = null;
            try
            {
                if (!Engine.Initialized) return false;
                sp = Engine.GetService<IScriptPlayer>();
                if (sp == null)
                {
                    if (!_warnedNoPlayer)
                    {
                        _warnedNoPlayer = true;
                        A11yHost.Diag("ScriptJump: 拿不到 IScriptPlayer，跳转菜单暂不可用");
                    }
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                A11yHost.Diag("ScriptJump: " + e.Message);
                return false;
            }
        }

        // ==================================================================== 按键

        /// <summary>
        /// 按键读取：平台层 <see cref="KeyEdge"/> ∪「新 Input System」。
        /// 本作只启用新 Input System，裸 `Input.GetKeyDown` 会抛异常；两条路缺一条就会
        /// 出现「某些键有反应、某些键完全没反应」的怪现象。
        /// </summary>
        private static bool Pressed(KeyCode key)
        {
            bool down = false;
            try { down = KeyEdge.Pressed(key); } catch { down = false; }
            if (!down) { try { down = InputSysKeys.Pressed(key); } catch { down = false; } }
            if (!down) return false;
            // 同一帧只消费一次（KeyEdge 不消费式，多个分支会连环命中）
            float now = Time.unscaledTime;
            if (now - _lastKeyAt < 0.02f) return false;
            _lastKeyAt = now;
            return true;
        }

        // ==================================================================== 测试钩子

        // 只在 MANOSABA_A11Y_TEST_SCRIPTJUMP=1（且 TestMode/DiagOn）时使用：
        // 自动化环境里键盘注入进不了游戏，只能从插件内部驱动同一套菜单代码。
        private static float _testAt = -1f;
        private static int _testStep;

        internal static void TestTick()
        {
            if (!Plugin.TestScriptJump) return;
            float now = Time.realtimeSinceStartup;
            if (_testAt < 0f) _testAt = now + 45f;
            if (now < _testAt) return;
            _testAt = now + 3f;

            if (_testStep == 0) { Toggle(); _testStep = 1; A11yHost.Diag("SCRIPTJUMP test: 打开菜单"); }
            else if (_testStep == 1) { Move(2); _testStep = 2; A11yHost.Diag("SCRIPTJUMP test: 下移两项 -> " + CurrentPath()); }
            else if (_testStep == 2) { _line = 0; Jump(); _testStep = 3; A11yHost.Diag("SCRIPTJUMP test: 已发出跳转"); }
        }
    }
}
