import io, re
p = r"D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\Patches.cs"
s = io.open(p, encoding="utf-8-sig").read()
orig = s

# 1) \u5220\u6389\u7ed3\u6784\u6027\u65e0\u6548\u7684\u9762\u677f\u7ea7 AddMessage \u8865\u4e01\uff08\u4e24\u4e2a\u5177\u4f53\u9762\u677f\u90fd override \u4e14\u4e0d\u8c03 base\uff09
start = s.index("        [HarmonyPatch(typeof(Naninovel.UI.UITextPrinterPanel), \"AddMessage\")]")
end = s.index("        [HarmonyPatch(typeof(Naninovel.UITextPrinter), \"AddMessage\")]")
removed = s[start:end]
assert "PanelAddMessage" in removed or "class Patch_PanelAddMessage" in removed, removed[:400]
s = s[:start] + s[end:]

# 2) Type.GetType \u9759\u9ed8\u964d\u7ea7 \u2192 AccessTools.TypeByName + LogError
s = s.replace('try { panel = Type.GetType("WitchTrials.Views.WitchTrialsTextPrinterPanel, Assembly-CSharp"); }',
              'try { panel = AccessTools.TypeByName("WitchTrials.Views.WitchTrialsTextPrinterPanel") ?? Type.GetType("WitchTrials.Views.WitchTrialsTextPrinterPanel, Assembly-CSharp"); }', 1)
s = s.replace('try { t = Type.GetType("GigaCreation.NaninovelExtender.Ui.LoadingUiExtended, GigaCreation.NaninovelExtender.UI"); }',
              'try { t = AccessTools.TypeByName("GigaCreation.NaninovelExtender.Ui.LoadingUiExtended") ?? Type.GetType("GigaCreation.NaninovelExtender.Ui.LoadingUiExtended, GigaCreation.NaninovelExtender.UI"); }', 1)
s = s.replace('try { panel = Type.GetType("WitchTrials.Views.DebateTextPrinterPanel, Assembly-CSharp"); }',
              'try { panel = AccessTools.TypeByName("WitchTrials.Views.DebateTextPrinterPanel") ?? Type.GetType("WitchTrials.Views.DebateTextPrinterPanel, Assembly-CSharp"); }', 1)

io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("Patches: removed panel AddMessage patch, TypeByName fallbacks")
