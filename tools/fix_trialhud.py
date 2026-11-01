import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Patches.cs"
s = io.open(p, encoding="utf-8-sig").read()
add = '''
        // ================= \u5ba1\u5224 HUD / \u5012\u8ba1\u65f6 / \u63d0\u4ea4\u9759\u97f3 =================
        // \u5168\u90e8\u7528 AccessTools \u6309\u540d\u5b57\u6302\uff1a\u8fd9\u4e9b\u7c7b\u5206\u5c5e UnityEngine / Assembly-CSharp /
        // Giga \u7684\u4e0d\u540c\u7a0b\u5e8f\u96c6\uff0c\u5199\u6b7b\u7c7b\u578b\u4f1a\u8ba9\u6ca1\u5f15\u7528\u5b83\u4eec\u7684\u7248\u672c\u76f4\u63a5\u7f16\u8bd1\u5931\u8d25\uff1b
        // \u540d\u5b57\u67e5\u4e0d\u5230\u53ea\u8bb0\u4e00\u6761\u65e5\u5fd7\uff0c\u4e0d\u5f71\u54cd\u5176\u5b83\u8865\u4e01\u3002
        // \u4e09\u8005\u8fd4\u56de\u503c\u5747\u4e3a void\uff08detour \u5b89\u5168\uff09\uff1b\u5012\u8ba1\u65f6\u90a3\u4e24\u4e2a\u9664\u5916\u7684 UniTask \u65b9\u6cd5
        // \uff08ChangeVisibility / Initialize\uff09**\u7edd\u4e0d\u80fd**\u6302\u2014\u2014\u8fd4\u56de\u975e blittable \u7ed3\u6784\u4f53\u4f1a\u628a await \u641e\u5d29\uff08\u89c1 10-29 \u4e8b\u6545\uff09\u3002
        internal static void ApplyTrialHud(Harmony harmony)
        {
            // 1) \u5012\u8ba1\u65f6\u8bbe\u7f6e
            Type t = FindType("WitchTrials.Models.Timer");
            if (t != null)
            {
                MethodInfo m = AccessTools.Method(t, "SetTime");
                if (m != null)
                {
                    try
                    {
                        harmony.Patch(m, postfix: new HarmonyMethod(typeof(Patches).GetMethod(
                            "TimerSetPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
                        Plugin.L.LogInfo("[a11y] patched " + t.Name + ".SetTime");
                    }
                    catch (Exception e) { Plugin.L.LogError("[a11y] \u6302 " + t.Name + ".SetTime \u5931\u8d25: " + e.Message); }
                }
                MethodInfo up = AccessTools.Method(t, "ForceRemainingTimeToNearlyZero");
                if (up != null)
                {
                    try
                    {
                        harmony.Patch(up, postfix: new HarmonyMethod(typeof(Patches).GetMethod(
                            "TimeUpPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
                        Plugin.L.LogInfo("[a11y] patched " + t.Name + ".ForceRemainingTimeToNearlyZero");
                    }
                    catch (Exception e) { Plugin.L.LogError("[a11y] \u6302 Timer \u5f52\u96f6\u5931\u8d25: " + e.Message); }
                }
            }

            // 2) HP \u53d8\u5316
            Type du = FindType("WitchTrials.Views.DebateUi");
            if (du != null)
            {
                MethodInfo m = AccessTools.Method(du, "OnCurrentHpChanged", new Type[] { typeof(float) });
                if (m != null)
                {
                    try
                    {
                        harmony.Patch(m, postfix: new HarmonyMethod(typeof(Patches).GetMethod(
                            "HpPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
                        Plugin.L.LogInfo("[a11y] patched " + du.Name + ".OnCurrentHpChanged");
                    }
                    catch (Exception e) { Plugin.L.LogError("[a11y] \u6302 HP \u5931\u8d25: " + e.Message); }
                }
            }

            // 3) \u5bfc\u822a\u6a21\u5f0f\u4e0b\u5c4f\u853d\u6e38\u620f\u81ea\u5df1\u7684\u63d0\u4ea4\u4e8b\u4ef6\uff1a
            //    \u5e73\u53f0\u5c42 UiNav \u8bf4\u660e\u4e86\u6302\u70b9\uff08\u89c1 UiNav.cs:257-259\uff09\uff0c\u4f46\u9010\u4f5c\u5c42\u4e00\u76f4\u6ca1\u63a5\u2014\u2014
            //    \u7ed3\u679c\u662f\u5bfc\u822a\u6a21\u5f0f\u4e0b\u6309\u56de\u8f66\uff0c\u65e2\u89e6\u53d1\u6211\u4eec\u7684\u201c\u786e\u8ba4\u201d\u53c8\u89e6\u53d1\u6e38\u620f\u7684\u63d0\u4ea4\u3002
            Type sim = FindType("UnityEngine.EventSystems.StandaloneInputModule");
            if (sim != null)
            {
                MethodInfo m = AccessTools.Method(sim, "SendSubmitEventToSelectedObject");
                if (m != null)
                {
                    try
                    {
                        harmony.Patch(m, postfix: new HarmonyMethod(typeof(Patches).GetMethod(
                            "MuteSubmitPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
                        Plugin.L.LogInfo("[a11y] patched " + sim.Name + ".SendSubmitEventToSelectedObject");
                    }
                    catch (Exception e) { Plugin.L.LogError("[a11y] \u6302\u63d0\u4ea4\u4e8b\u4ef6\u5931\u8d25: " + e.Message); }
                }
            }
        }

        private static Type FindType(string name)
        {
            try
            {
                Type t = AccessTools.TypeByName(name);
                if (t == null) Plugin.L.LogWarning("[a11y] \u627e\u4e0d\u5230\u7c7b " + name + "\uff08\u8be5\u529f\u80fd\u964d\u7ea7\uff09");
                return t;
            }
            catch (Exception e) { Plugin.L.LogWarning("[a11y] \u67e5\u7c7b " + name + " \u5f02\u5e38: " + e.Message); return null; }
        }

        private static void TimerSetPostfix([HarmonyArgument(0)] Il2CppSystem.TimeSpan span)
        {
            try { Reader.OnTrialTimerSet(span); }
            catch (Exception e) { A11yHost.Diag("Patches.TimerSet: " + e.Message); }
        }

        private static void TimeUpPostfix()
        {
            try { Reader.OnTrialTimeUp(); }
            catch (Exception e) { A11yHost.Diag("Patches.TimeUp: " + e.Message); }
        }

        private static void HpPostfix([HarmonyArgument(0)] float hp)
        {
            try { Reader.OnDebateHp(hp); }
            catch (Exception e) { A11yHost.Diag("Patches.Hp: " + e.Message); }
        }

        private static void MuteSubmitPostfix()
        {
            try
            {
                // \u8fd4\u56de\u503c void\uff0c\u6240\u4ee5\u4e0d\u80fd Prefix \u8fd4 false \u62e6\u622a\u3002\u5b9e\u9645\u505a\u6cd5\uff1a
                // \u53ea\u9760 UiNav \u81ea\u5df1\u7684 ShouldMuteUnitySubmit \u5728\u4e0b\u4e00\u5e27\u628a\u9009\u4e2d\u7269\u53d6\u6d88\u9009\u4e2d\uff0c
                // \u8fd9\u91cc\u53ea\u8bb0\u4e00\u6761\u8bca\u65ad\uff0c\u4fbf\u4e8e\u5b9a\u4f4d\u53cc\u89e6\u53d1\u3002
                if (A11yHost.ShouldMuteUnitySubmit != null && A11yHost.ShouldMuteUnitySubmit())
                    A11yHost.Diag("Patches.MuteSubmit: \u5bfc\u822a\u6a21\u5f0f\u4e0b\u6e38\u620f\u63d0\u4ea4\u4e8b\u4ef6\u88ab\u8bfb\u5c4f\u5c42\u63a5\u7ba1");
            }
            catch { }
        }
'''
# \u63d2\u5728 applyManual \u524d\u9762\uff08\u7c7b\u5c3e\u90e8\u5c31\u884c\uff09
idx = s.index("        internal static void ApplyManual(Harmony harmony)")
s = s[:idx] + add + "\n" + s[idx:]
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Patches.cs: trial hud + mute submit added")

# Plugin.cs: \u8c03\u7528 ApplyTrialHud
p2 = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Plugin.cs"
s2 = io.open(p2, encoding="utf-8-sig").read()
s2 = s2.replace("                Patches.ApplyLoadingUi(_harmony);",
                "                Patches.ApplyLoadingUi(_harmony);\n                // \u5ba1\u5224 HUD\uff08\u5012\u8ba1\u65f6 / HP\uff09\u4e0e\u63d0\u4ea4\u9759\u97f3\uff1a\u8fd9\u4e09\u7c7b\u4fe1\u606f\u5bfc\u822a\u626b\u4e0d\u5230\uff0c\u53ea\u80fd\u9760\u8865\u4e01\u3002\n                Patches.ApplyTrialHud(_harmony);", 1)
io.open(p2, "w", encoding="utf-8-sig", newline="\n").write(s2)
print("Plugin.cs: ApplyTrialHud wired")
