import io
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\SelfTest.cs"
s = io.open(p, encoding="utf-8-sig").read()
s = s.replace("""            int missing = -1;
            try { missing = A11yHost.Missing(); } catch (Exception) { }
            Check("wiring.\u5951\u7ea6\u5fc5\u586b\u9879\u9f50\u5168", missing == 0, "missing=" + missing);""",
"""            string[] missing = null;
            try { missing = A11yHost.Missing(); } catch (Exception) { }
            string missText = missing == null ? "null" : string.Join(",", missing);
            Check("wiring.\u5951\u7ea6\u5fc5\u586b\u9879\u9f50\u5168", missing != null && missing.Length == 0, "missing=" + missText);""", 1)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("fixed Missing() usage")
