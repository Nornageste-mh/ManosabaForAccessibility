import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()


def rep(old, new):
    global s
    if old not in s:
        raise SystemExit('NOT FOUND: ' + old[:80])
    s = s.replace(old, new, 1)


# 1) 楼层卡标签：换行/标签在楼层名里应该直接吃掉，不能变成「，」
rep('''        private static string ItemLabel(WitchTrials.Views.WitchBookItemButton it)
        {
            try
            {
                if (it._label != null) return TextProc.Clean(it._label.text);
            }
            catch { }
            return "";
        }''',
    '''        private static string ItemLabel(WitchTrials.Views.WitchBookItemButton it)
        {
            try
            {
                if (it._label == null) return "";
                // 楼层卡的文字是「地下<br>1F」这种带换行/标签的两行，
                // 直接交给 TextProc.Clean 会把 <br> 变成「，」→「地下，1F」（run106 实测）。
                // 楼层名要的是连读，所以先把标签和换行全部吃掉再做普通清洗。
                string s = it._label.text ?? "";
                s = System.Text.RegularExpressions.Regex.Replace(s, "<[^<>]{1,120}>", "");
                s = s.Replace("\\r", "").Replace("\\n", "");
                return TextProc.Clean(s);
            }
            catch { }
            return "";
        }''')

# 2) chosenAreaId / chosenAreaFlag 诊断：变量类型不同，不能一律读 Number
#    （run114 实测 chosenAreaId 是字符串，读 .Number 抛 Naninovel.Error: Custom variable is not a number.）
rep('''                string id = "-";
                string flag = "-";
                try { if (vars.VariableExists("chosenAreaId")) { Naninovel.CustomVariableValue v = vars.GetVariableValue("chosenAreaId"); id = v.Type + ":" + v.String + "/" + v.Number + "/" + v.Boolean; } }
                catch (Exception e) { id = "err:" + e.Message; }
                try { if (vars.VariableExists("chosenAreaFlag")) { Naninovel.CustomVariableValue v = vars.GetVariableValue("chosenAreaFlag"); flag = v.Type + ":" + v.String + "/" + v.Number + "/" + v.Boolean; } }
                catch (Exception e) { flag = "err:" + e.Message; }''',
    '''                string id = "-";
                string flag = "-";
                try { if (vars.VariableExists("chosenAreaId")) id = VarText(vars.GetVariableValue("chosenAreaId")); }
                catch (Exception e) { id = "err:" + e.Message; }
                try { if (vars.VariableExists("chosenAreaFlag")) flag = VarText(vars.GetVariableValue("chosenAreaFlag")); }
                catch (Exception e) { flag = "err:" + e.Message; }''')

# 3) 工具方法 VarText
rep('''        private static void DumpChosenVariables()''',
    '''        /// <summary>按变量自己的类型取值（字符串变量读 .Number 会抛 Naninovel.Error）。</summary>
        private static string VarText(Naninovel.CustomVariableValue v)
        {
            string type = "-";
            try { type = v.Type.ToString(); } catch { }
            try
            {
                if (type == "String") return type + ":" + v.String;
                if (type == "Number") return type + ":" + v.Number.ToString("0.###");
                if (type == "Boolean") return type + ":" + v.Boolean;
            }
            catch (Exception e) { return type + ":err:" + e.Message; }
            return type;
        }

        private static void DumpChosenVariables()''')

io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('patched ItemLabel + VarText')
