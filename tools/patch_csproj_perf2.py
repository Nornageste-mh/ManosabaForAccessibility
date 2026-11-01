import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/ManosabaA11y.csproj"
s = io.open(p, encoding="utf-8-sig").read()
add = """    <Reference Include="UnityEngine.ParticleSystemModule">
      <HintPath>$(InteropDir)\\UnityEngine.ParticleSystemModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
"""
anchor = '    <Reference Include="LeTai.TrueShadow">'
assert s.count(anchor) == 1
s = s.replace(anchor, add + anchor)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("particle module ref added")
