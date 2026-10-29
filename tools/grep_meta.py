import re,sys
p=r"D:\Steam\steamapps\common\manosaba_game\manosaba_Data\il2cpp_data\Metadata\global-metadata.dat"
data=open(p,'rb').read()
print("size",len(data))
def find(pat,n=40):
    out=[]
    for m in re.finditer(re.escape(pat.encode()),data):
        out.append(m.start())
    print(f"--- {pat}: {len(out)} hits")
    for off in out[:n]:
        # extract surrounding printable ascii
        s=max(0,off-60); e=min(len(data),off+120)
        chunk=data[s:e]
        txt=''.join(chr(c) if 32<=c<127 else '.' for c in chunk)
        print(hex(off), txt)
for pat in ["Elringus.Naninovel","GigaCreation","ITextPrinterManager","IChoiceHandlerManager","IScriptPlayer","IAudioManager","TitleMenu","SaveLoadMenu","SettingsMenu","ChoiceButton","UITextPrinter"]:
    find(pat)
