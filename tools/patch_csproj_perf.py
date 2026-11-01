import io
p = "D:/DSHWorkSpace/manosaba_a11y/mod/src/ManosabaA11y/ManosabaA11y.csproj"
s = io.open(p, encoding="utf-8-sig").read()
add = """    <Reference Include="LeTai.TrueShadow">
      <HintPath>$(InteropDir)\\LeTai.TrueShadow.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Unity.RenderPipelines.Core.Runtime">
      <HintPath>$(InteropDir)\\Unity.RenderPipelines.Core.Runtime.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Unity.RenderPipelines.Universal.Runtime">
      <HintPath>$(InteropDir)\\Unity.RenderPipelines.Universal.Runtime.dll</HintPath>
      <Private>false</Private>
    </Reference>
"""
anchor = '    <Reference Include="GigaCreation.NaninovelExtender.UI">'
assert s.count(anchor) == 1
s = s.replace(anchor, add + anchor)
io.open(p, "w", encoding="utf-8-sig", newline="\n").write(s)
print("csproj references added")
