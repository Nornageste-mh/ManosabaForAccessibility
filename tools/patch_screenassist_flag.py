import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/ScreenAssist.cs"
s = io.open(p, encoding="utf-8-sig").read()
# 1) 字段
s = s.replace("        private static float _nextScanAt;",
"""        private static float _nextScanAt;
        private static bool _foundLastScan;""", 1)
# 2) 找到/没找到时维护标记
s = s.replace("""                Transform found = FindVisibleScreen(out string title);
                if (found == null)
                {""",
"""                Transform found = FindVisibleScreen(out string title);
                _foundLastScan = found != null;
                if (found == null)
                {""", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("ok")
