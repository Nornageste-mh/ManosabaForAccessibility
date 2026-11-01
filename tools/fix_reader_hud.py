import io
base = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y"

# ---------- Reader: \u5ba1\u5224 HUD / \u5012\u8ba1\u65f6 / HP ----------
p = base + r"\Reader.cs"
s = io.open(p, encoding="utf-8-sig").read()
anchor = "        /// <summary>\n        /// \u884c id \u2192 \u4f5c\u8005 id \u7684\u56de\u9000\u5339\u914d"
assert anchor in s
new = """        // ================= \u5ba1\u5224\u754c\u9762\u72b6\u6001\uff08\u5012\u8ba1\u65f6 / HP\uff09 =================
        // \u4e3a\u4ec0\u4e48\u5728\u8fd9\u91cc\uff1a\u8fd9\u4e24\u7c7b\u4fe1\u606f\u5728\u672c\u4f5c\u53ea\u5b58\u5728\u4e8e HUD \u7ec4\u4ef6\u7684\u5c5e\u6027\u91cc\uff0c
        // \u5bfc\u822a\u626b\u63cf\u770b\u4e0d\u5230\uff08\u4e0d\u662f Selectable\uff09\uff0c\u53ea\u80fd\u9760\u8865\u4e01\u53d6\u3002\u5168\u90e8\u505a\u9650\u9891\uff0c
        // \u5426\u5219 6 FPS \u4e0b\u4f1a\u628a\u8bfb\u5c4f\u6d17\u6bdb\u3002
        private static int _lastAnnouncedTimerSec = -1;
        private static int _lastAnnouncedHp = -1;
        private static bool _timeUpAnnounced;
        private static float _lastTrialAnnounceAt = -999f;

        /// <summary>\u5012\u8ba1\u65f6\u88ab\u8bbe\u7f6e\uff08\u6765\u81ea WitchTrials.Models.Timer.SetTime\uff09\u3002</summary>
        internal static void OnTrialTimerSet(Il2CppSystem.TimeSpan span)
        {
            try
            {
                double total = span.TotalSeconds;
                if (total < 0 || total > 36000) return;
                int sec = (int)System.Math.Ceiling(total);
                // \u53ea\u5728\u8de8\u8fc7 5 \u5206/\u4e24\u5206/\u4e00\u5206/30 \u79d2/10 \u79d2\u9608\u503c\u65f6\u8bf4\u4e00\u53e5
                int[] marks = new int[] { 300, 120, 60, 30, 10 };
                foreach (int m in marks)
                {
                    if (sec <= m && _lastAnnouncedTimerSec > m)
                    {
                        AnnounceTrial(m >= 60 ? ("\u5269\u4f59\u65f6\u95f4 " + (m / 60) + " \u5206\u949f\u3002") : ("\u5269\u4f59\u65f6\u95f4 " + m + " \u79d2\u3002"));
                        break;
                    }
                }
                _lastAnnouncedTimerSec = sec;
                if (sec > 10) _timeUpAnnounced = false;
            }
            catch (Exception e) { A11yHost.Diag("Reader.TimerSet: " + e.Message); }
        }

        /// <summary>\u5012\u8ba1\u65f6\u5f52\u96f6\uff08\u6765\u81ea Timer.ForceRemainingTimeToNearlyZero\uff09\u3002</summary>
        internal static void OnTrialTimeUp()
        {
            try
            {
                if (_timeUpAnnounced) return;
                _timeUpAnnounced = true;
                AnnounceTrial("\u65f6\u95f4\u5230\u4e86\u3002");
            }
            catch (Exception e) { A11yHost.Diag("Reader.TimeUp: " + e.Message); }
        }

        /// <summary>HP \u53d8\u5316\uff08\u6765\u81ea DebateUi.OnCurrentHpChanged\uff09\u3002</summary>
        internal static void OnDebateHp(float hp)
        {
            try
            {
                int v = (int)hp;
                if (v == _lastAnnouncedHp) return;
                bool first = _lastAnnouncedHp < 0;
                _lastAnnouncedHp = v;
                if (first) return;
                AnnounceTrial("\u5f53\u524d\u751f\u547d " + v + "\u3002");
            }
            catch (Exception e) { A11yHost.Diag("Reader.Hp: " + e.Message); }
        }

        private static void AnnounceTrial(string text)
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            if (now - _lastTrialAnnounceAt < 1.2f) return;
            _lastTrialAnnounceAt = now;
            Repeat.Say(text, true);
        }

"""
s = s.replace(anchor, new + anchor, 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Reader.cs: trial hud added")
