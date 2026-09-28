# PDA material repair

The original Unity 2019 PDA bundle referenced shader object
`-9098473984068178372` in `CAB-1dc8d26be8722a766953ce9d8a444e8c`.
Neither reference resolves in the installed EFT 40743 shader bundle, causing a
solid magenta item preview.

`Repair-Shader.py` reconnects the material and its preload entry to the current
game's `p0/Reflective/Bumped Specular SMap` shader. Every property required by that
shader is already present in the PDA material. It discovers the shader's current
CAB and object ID from the supplied game bundle rather than assuming IDs are stable.

Install UnityPy 1.25.3 in your Python environment, then run from the repository root:

```powershell
python Tools/PdaAssets/Repair-Shader.py --shaders 'F:/SPT 4.1.x/EscapeFromTarkov_Data/StreamingAssets/Windows/shaders' --write
python Tools/PdaAssets/Repair-Shader.py --shaders 'F:/SPT 4.1.x/EscapeFromTarkov_Data/StreamingAssets/Windows/shaders'
dotnet build 'Skills Extended.sln' -c Release -p:DeploySkillsExtended=false
```

The repair validates its serialized output before replacing the source bundle.
Mesh, texture, prefab identity, collider, components, and material properties are
preserved. The game shader bundle is read only and is not included in the mod.
Repeated runs validate the repaired bundle without rewriting it.

Offline validation proves reference resolution and asset preservation. After
installing the updated package and restarting normally, inspect the PDA in-game
to confirm its appearance. No application is started or restarted by this tool.
