# Signals assets

`SignalsSkillIcon.svg` is the authored vector source for the receiver icon.
The checked-in 128px PNG uses the same antenna/radio motif.

The cache uses **Military Hard Case** by **Alpen wolf**, Fab listing
`76f33491-c282-487d-9b70-407ea18eb425`, under the Fab Standard License. The
`ammo_box_01` prefab/material is the olive variant; 02 is tan and 03 is black.
Obtain `ammo_boxesi_2021328f1.unitypackage` through the listing with your own
license. Raw source files stay local to CJ-SDK and are excluded from its Git
checkout. Only the compiled olive case and its notices ship with the mod.

The new prefab is Y-up, with a separate rear-hinged lid. The client does not
apply the old toolbox's -90-degree rotation. Its native inventory template stays
`5909d50c86f774659e6aaebe`, preserving reward packing and Fika inventory behavior.
Placement and the marker height use the new model's actual bounds.

`Stage-Case.py` extracts only the olive prefab/material, mesh, and three 4K
textures, preserving GUIDs and recording source hashes. `SignalsCaseVisualBuilder`
compiles the visual prefab using CJ-SDK / Unity **2022.3.43f1**, adds collision,
checks its geometry/materials, and renders closed/open previews.

`Extract-Case.py` obtains the installed EFT container components and sounds from
a locally exported native library. It relocates both texture/mesh and audio
streamed resources. `Graft-Case.py` attaches the native LootableContainer to the
new lid and native ballistic components to the case collision meshes. It remaps
object references and script types, embeds opening/closing audio, and validates
the final bundle by reopening it. The native library is only an authoring input;
Campaigns is not a runtime dependency.

With UnityPy **1.25.3** available in `artifacts/signals-python`:

```powershell
Tools/SignalsAssets/Build-Case.ps1 `
  -Package 'C:\Users\Corey\Downloads\ammo_boxesi_2021328f1.unitypackage' `
  -NativeContainers '<local-native-containers.bundle>' `
  -RunUnityBatch
```

Omit `-RunUnityBatch` to stage authoring inputs without launching an editor.
The batch build requires exclusive access to CJ-SDK, runs hidden, and does not
start/stop Tarkov or SPT or stop an existing editor. Output goes to
`artifacts/signals-case`; only successful builds replace client Resources.

Keep `source-receipt.json`, `visual-validation.json`, the final bundle validation,
and the previews as offline evidence. Inspect real lighting, native opening and
looting, all configured placement areas, and Fika host/peer behavior in-game;
offline checks cannot establish that live acceptance.
