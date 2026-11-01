using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ManosabaA11y
{
    /// <summary>
    /// 纯逻辑单元自测（自动化测试套件的第一层）。
    ///
    /// 为什么放在插件里而不是单独的测试工程：
    ///   本作是 IL2CPP，`TextProc` / `SpeakerNames` / `VoiceLineIds` 这些类型在
    ///   进程外没法引用（interop 程序集只在游戏运行时可用）。放在插件里跑，
    ///   等于「用真实的运行时跑真实的数据」，比另建 net6.0 测试工程更可信。
    ///
    /// 断言方式：只看**不变量**，不写死具体台词（台词是游戏 IP，不能进仓库；
    ///   而且换语言/改文案会误报）。例如「清洗后不许残留 TMP 标签」「指纹忽略标点」
    ///   「有配音表里必须有这句」「快捷键解析必须认出修饰键」。
    ///
    /// 输出：`SELFTEST PASS/FAIL &lt;名字&gt;[: 细节]`，最后一行
    ///   `SELFTEST SUMMARY total=N pass=N fail=N`，由 `tests\Suite.ps1` 解析。
    /// 开关：环境变量 `MANOSABA_A11Y_SELFTEST=1`（默认关闭，不打扰玩家）。
    /// </summary>
    internal static class SelfTest
    {
        private static int _pass;
        private static int _fail;
        private static int _frames;
        private static bool _done;

        internal static void Tick()
        {
            if (_done) return;
            if (Environment.GetEnvironmentVariable("MANOSABA_A11Y_SELFTEST") != "1") return;

            // 等 30 帧：日志系统与 interop 都就绪（实测第 1 帧读静态字段偶发 NRE）。
            if (++_frames < 30) return;
            _done = true;
            try { Run(); }
            catch (Exception e)
            {
                Fail("selftest.异常", "自测本身崩了: " + e.GetType().Name + ": " + e.Message);
                Summary();
            }
        }

        private static void Pass(string name)
        {
            _pass++;
            Plugin.L.LogInfo("SELFTEST PASS " + name);
        }

        private static void Fail(string name, string detail)
        {
            _fail++;
            Plugin.L.LogWarning("SELFTEST FAIL " + name + (string.IsNullOrEmpty(detail) ? "" : " : " + detail));
        }

        private static void Check(string name, bool ok, string detail = "")
        {
            if (ok) Pass(name);
            else Fail(name, detail);
        }

        private static void Run()
        {
            TextProcTests();
            SpeakerTests();
            VoiceTests();
            HotkeyTests();
            // 2026-11-01 审计新增的两组：文本边界用例 + L2 接线/配置默认值
            CheckText();
            CheckWiring();
            CheckScriptIndex();
            CheckLowPerf();
            Summary();
        }

        // ---------------------------------------------------------------- 剧本索引（剧情跳转菜单）
        /// <summary>
        /// 剧情跳转菜单的数据源是生成文件 ScriptIndex.cs（440 条剧本路径）。
        /// 它由 tools/gen_script_index.py 从 textassets 索引生成，**不含任何台词原文**。
        /// 这里只断言“表本身是好的”（数量/无重复/含已知条目），不依赖游戏运行时。
        /// </summary>
        private static void CheckLowPerf()
        {
            LowPerf.Mode m0;
            bool modeParsed = LowPerf.TryParseMode(
                Plugin.CfgLowPerfMode != null ? Plugin.CfgLowPerfMode.Value : "", out m0);
            Check("lowperf.默认关闭",
                Plugin.CfgLowPerfMode != null && modeParsed && m0 == LowPerf.Mode.Off && !LowPerf.Active,
                "CfgLowPerfMode=" + (Plugin.CfgLowPerfMode != null ? Plugin.CfgLowPerfMode.Value : "null")
                + " parsed=" + modeParsed + " Active=" + LowPerf.Active);

            // 四档解析：配置里写中文/英文/数字都要认（玩家可能手改 cfg）
            LowPerf.Mode m1, m2, m3, m4, mBad;
            Check("lowperf.档位解析-保守", LowPerf.TryParseMode("保守", out m1) && m1 == LowPerf.Mode.Conservative, "保守");
            Check("lowperf.档位解析-普通", LowPerf.TryParseMode("normal", out m2) && m2 == LowPerf.Mode.Normal, "normal");
            Check("lowperf.档位解析-激进", LowPerf.TryParseMode("3", out m3) && m3 == LowPerf.Mode.Aggressive, "3");
            Check("lowperf.档位解析-关闭", LowPerf.TryParseMode("off", out m4) && m4 == LowPerf.Mode.Off, "off");
            Check("lowperf.档位解析-坏值不崩", !LowPerf.TryParseMode("乱写", out mBad), "乱写");

            bool parsed = false, ok = false;
            try
            {
                Hotkey.Spec spec;
                string why;
                parsed = Hotkey.Parse(Plugin.CfgLowPerfToggleKey.Value, out spec, out why);
                ok = parsed && spec.Key == KeyCode.F2 && spec.Alt && spec.Shift;
            }
            catch (Exception e) { ok = false; A11yHost.Diag("SelfTest lowperf: " + e.Message); }
            Check("lowperf.开关键可解析",
                ok,
                Plugin.CfgLowPerfToggleKey.Value + " parsed=" + parsed);
        }

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

        // ---------------------------------------------------------------- TextProc
        private static void TextProcTests()
        {
            // 1) 清洗后绝不允许残留 TMP 标签、行 id 标记
            string raw = "Ema: <color=#ff0000>你好</color><br>世界<ruby=\"せかい\">世界</ruby>|#0101Adv02_Ema001|";
            string clean = TextProc.Clean(raw);
            Check("textproc.无残留标签",
                  clean.IndexOf('<') < 0 && clean.IndexOf('>') < 0 && clean.IndexOf("|#") < 0,
                  "clean=" + clean);

            // 2) <br> 必须变成停顿（逗号或换行），不能原样留着
            string br = TextProc.Clean("对。<br>请大家仔细观察。");
            Check("textproc.br转停顿", br.IndexOf("<br>", StringComparison.Ordinal) < 0 && br.Length > 0, "br=" + br);

            // 3) 行 id 提取
            Check("textproc.行id提取",
                  TextProc.ExtractLineKey("; > Ema: |#0101Adv02_Ema001|") == "0101Adv02_Ema001");

            // 4) 作者前缀提取
            Check("textproc.作者提取", TextProc.ExtractAuthorId("Ema: 你好") == "Ema",
                  "实际=" + TextProc.ExtractAuthorId("Ema: 你好"));

            // 5) 注释行不该朗读
            Check("textproc.注释行不读", !TextProc.ShouldRead("; 日文原文"));
            Check("textproc.正常行要读", TextProc.ShouldRead("——这里是囚牢。"));

            // 6) 指纹忽略空白与标点（同一句经不同入口到达时必须能去重）
            Check("textproc.指纹去重",
                  TextProc.Fingerprint("你好，世界！") == TextProc.Fingerprint("你好 世界"),
                  "a=" + TextProc.Fingerprint("你好，世界！") + " b=" + TextProc.Fingerprint("你好 世界"));

            // 7) 纯标点/省略号视为噪声（不要念「点点点」）
            Check("textproc.纯标点算噪声", TextProc.IsTrivial("……") && !TextProc.IsTrivial("你好"));

            // 8) 方块替身（□■█）要念成「方块」，不能消失
            string blocks = TextProc.Clean("■■■");
            Check("textproc.方块处理", blocks.IndexOf("方", StringComparison.Ordinal) >= 0, "blocks=" + blocks);

            // 9) 空输入不崩
            Check("textproc.空输入", TextProc.Clean("") == "" && TextProc.Clean(null) == "");
        }

        // ------------------------------------------------------------- SpeakerNames
        private static void SpeakerTests()
        {
            Check("speaker.Ema映射", SpeakerNames.Resolve("Ema") == "樱羽艾玛",
                  "实际=" + SpeakerNames.Resolve("Ema"));
            Check("speaker.未知名不变", SpeakerNames.Resolve("NoSuchAuthorXyz") == "NoSuchAuthorXyz");
            Check("speaker.问号名念未知", TextProc.Speaker("? ? ?") == "未知",
                  "实际=" + TextProc.Speaker("? ? ?"));
        }

        // ------------------------------------------------------------- VoiceLineIds
        private static void VoiceTests()
        {
            // 26,083 条（textassets\voice-index.tsv），少于 2 万说明生成器或压缩坏了
            Check("voice.表规模", VoiceLineIds.Count > 20000, "Count=" + VoiceLineIds.Count);
            Check("voice.命中已知行", VoiceLineIds.Has("0101Adv02_Ema001"));
            Check("voice.不误判", !VoiceLineIds.Has("__not_a_real_line_id__"));

            // 大小写/空白不该误命中
            Check("voice.区分大小写", !VoiceLineIds.Has("0101adv02_ema001"));

            // --------------------------------------------------------- LineSpeakers
            // 24,492 条（对白行数，text-extract 统计一致）；少于 2 万说明生成器或压缩坏了
            Check("speaker.表规模", LineSpeakers.Count > 20000, "Count=" + LineSpeakers.Count);
            Check("speaker.命中已知行", LineSpeakers.AuthorOf("0101Adv02_Ema001") == "Ema",
                  "实际=" + LineSpeakers.AuthorOf("0101Adv02_Ema001"));
            Check("speaker.旁白无作者", LineSpeakers.AuthorOf("0101Adv01_Narrative001") == "",
                  "实际=" + LineSpeakers.AuthorOf("0101Adv01_Narrative001"));
            Check("speaker.不误判", LineSpeakers.AuthorOf("__not_a_real_line_id__") == "");
        }

        // ----------------------------------------------------------------- Hotkey
        private static void HotkeyTests()
        {
            Hotkey.Spec sp;
            string why;

            bool ok = Hotkey.Parse("Alt+Shift+F1", out sp, out why);
            Check("hotkey.组合键", ok && sp.Key == KeyCode.F1 && sp.Alt && sp.Shift && !sp.Ctrl,
                  "ok=" + ok + " why=" + why);

            ok = Hotkey.Parse("Ctrl+Alt+Shift+F3", out sp, out why);
            Check("hotkey.三修饰键", ok && sp.Key == KeyCode.F3 && sp.Ctrl && sp.Alt && sp.Shift,
                  "ok=" + ok + " why=" + why);

            ok = Hotkey.Parse("F7", out sp, out why);
            Check("hotkey.单键", ok && sp.Key == KeyCode.F7 && !sp.Ctrl && !sp.Alt && !sp.Shift,
                  "ok=" + ok + " why=" + why);

            ok = Hotkey.Parse("Backspace", out sp, out why);
            Check("hotkey.退格", ok && sp.Key == KeyCode.Backspace, "ok=" + ok + " why=" + why);

            // 0.0.0.11：重读键默认改成 `=`。Enum.TryParse 不认这个符号，
            //   所以必须有这条断言守着——否则玩家会得到一个“永远不触发”的重读键。
            ok = Hotkey.Parse("=", out sp, out why);
            Check("hotkey.等号", ok && sp.Key == KeyCode.Equals && !sp.Alt && !sp.Ctrl && !sp.Shift,
                  "ok=" + ok + " why=" + why);

            ok = Hotkey.Parse("Equals", out sp, out why);
            Check("hotkey.Equals别名", ok && sp.Key == KeyCode.Equals, "ok=" + ok + " why=" + why);

            // 认不出来的键必须「返回 false 且有原因」，不能静默当成功
            ok = Hotkey.Parse("Ctrl+NotAKeyXyz", out sp, out why);
            Check("hotkey.坏键报错", !ok && !string.IsNullOrEmpty(why), "ok=" + ok + " why=" + why);
        }

        private static void CheckText()
        {
            // ---- 2026-11-01 审计回填：这六条在修复前是红的 ----
            Check("textproc.br左侧助词不补逗号",
                  !TextProc.Clean("挂在墙上的<br>全新的制服").Contains("的，"),
                  TextProc.Clean("挂在墙上的<br>全新的制服"));
            Check("textproc.br后全角缩进不留空格",
                  !TextProc.Clean("墙上的<br>　全新").Contains(" "),
                  TextProc.Clean("墙上的<br>　全新"));
            Check("textproc.ruby拉丁旁注不念",
                  TextProc.Clean("<ruby=\"Balloon\">气球</ruby>") == "气球",
                  TextProc.Clean("<ruby=\"Balloon\">气球</ruby>"));
            Check("textproc.全角空格不留半角空格",
                  TextProc.Clean("甲　乙") == "甲乙",
                  TextProc.Clean("甲　乙"));
            Check("textproc.破折号后不叠逗号",
                  !TextProc.Clean("甲——，乙").Contains("—，"),
                  TextProc.Clean("甲——，乙"));
            Check("speaker.未映射英文id不念",
                  TextProc.Speaker("CreatureFreshName") == "",
                  TextProc.Speaker("CreatureFreshName"));

            // ---- 行 id 反推作者（审计 P1-2：1,988 个 @printDebate 行只能从 id 推）----
            Check("speaker.printDebate行可反推",
                  Reader.AuthorFromLineId("0101Trial01_Sherry001") == "Sherry",
                  Reader.AuthorFromLineId("0101Trial01_Sherry001"));
            Check("speaker.旁白id不产生作者",
                  Reader.AuthorFromLineId("0101Adv01_Narrative001") == "",
                  Reader.AuthorFromLineId("0101Adv01_Narrative001"));
            Check("speaker.反推结果能查成中文名",
                  TextProc.Speaker(Reader.AuthorFromLineId("0101Trial01_Sherry001")) == "橘雪莉",
                  TextProc.Speaker(Reader.AuthorFromLineId("0101Trial01_Sherry001")));
        }

        private static void CheckWiring()
        {
            // ---- L2 契约接线（审计 E：少填一个钩子就是静默失效）----
            string[] missing = null;
            try { missing = A11yHost.Missing(); } catch (Exception) { }
            string missText = missing == null ? "null" : string.Join(",", missing);
            Check("wiring.契约必填项齐全", missing != null && missing.Length == 0, "missing=" + missText);
            Check("wiring.groupPriority", A11yHost.GameGroupPriority != null);
            Check("wiring.groupName", A11yHost.GameGroupName != null);
            Check("wiring.labelOf", A11yHost.GameLabelOf != null);
            Check("wiring.textIsNoise", A11yHost.GameTextIsNoise != null);
            Check("wiring.muteSubmit", A11yHost.ShouldMuteUnitySubmit != null);
            Check("wiring.blockAdvance", A11yHost.BlockGameAdvance != null);
            Check("wiring.navToggle", A11yHost.NavTogglePressed != null);

            // ---- 配置默认值必须合法（审计 F：沉默按键原默认 "0" 是不合法键名）----
            Check("config.沉默按键留空或合法",
                  string.IsNullOrEmpty(Plugin.CfgSilenceKey == null ? "" : Plugin.CfgSilenceKey.Value));
            string quitNames = Plugin.CfgQuitNames == null ? "" : (Plugin.CfgQuitNames.Value ?? "");
            Check("config.退出确认默认已取证对象名",
                  quitNames.IndexOf("Button_Exit", StringComparison.OrdinalIgnoreCase) >= 0, quitNames);
        }

        private static void Summary()
        {
            Plugin.L.LogInfo("SELFTEST SUMMARY total=" + (_pass + _fail) + " pass=" + _pass + " fail=" + _fail);
        }
    }
}
