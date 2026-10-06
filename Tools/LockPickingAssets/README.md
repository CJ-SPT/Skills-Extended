# Lock-picking artwork

The purchased Warren Marshall Mini Pack supplies two lock housings, two cylinders,
four picks and a screwdriver. `Build-Assets.py` reads its Unity FBX/PNG entries
directly from the source ZIP. It triangulates the meshes, preserves the indexed
normals and UVs, and converts authoring centimetres to metres. It requires Python 3
and no third-party modules or running Unity editor.

```powershell
python Tools/LockPickingAssets/Build-Assets.py 'C:\path\to\MiniPack_Lockpick.zip'
```

This produces `Client/SkillsExtended.Client.Skills/Assets/LockPicking.assets` and
an ignored `artifacts/lockpicking/source-receipt.json`. The resource ZIP has stable
entry order and timestamps. Its SHA-256 is recorded in the runtime art notice.
It is embedded in the client DLL; no vendor project, FBX, or loose atlas is packaged.
The purchased assets remain subject to the seller's license, separate from this
repository's software license. Do not redistribute the source pack.

At runtime Unity reconstructs the meshes, reverses handedness and triangle winding,
recalculates tangents, and uses the game's existing Standard shader. The RGB normal
atlas is loaded as linear data with opaque alpha, compatible with Standard's RG/AG
normal decoding. The metallic/smoothness atlas is also linear. A separate camera
renders the isolated models to a UI texture; closing disposes the scene and target.

`Preview-Art.py` needs NumPy and Pillow. It creates offline textured-mesh composition
previews in `artifacts/lockpicking` at 1080p, 1440p and ultrawide. These approximate
the runtime composition; they do not validate Unity materials, lighting or controls.

```powershell
python Tools/LockPickingAssets/Preview-Art.py
```

Before running the preview, export the side-view triangles from the production
cutaway through the offline test harness:

```powershell
dotnet run --project Tests/SkillsExtended.LockPickingTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false -- artifacts/lockpicking/cutaway-preview
```

The preview uses those triangles beneath the 1000 x 310 front view and also
produces `cutaway-states.png` for 3–5 pins in active, unlocked and broken states.
Full layouts cover 1080p, 1440p and ultrawide. Text rasterization and front-view
lighting remain approximate; the cutaway's geometry comes from the runtime code.

## Mechanical reference and partial cutaway

The procedural side view is a generic pin-tumbler partial cutaway, informed by:

- [Schlage cylinder guide, pages 3.2 and 7.1–7.3](https://www.schlage.com/content/dam/sch-us/documents/pdf/installation-manuals/P513-325.pdf): the rotating plug, fixed shell, common shear interface, bullet-shaped bottom pins, flat-ended top pins and spring seats.
- [SPARROWS spool cutaway](https://www.sparrowslockpicks.com/products/cut-away-lock-spool-pins): solid brass around milled inspection windows and flanged spool profiles. Its manufacturer photographs were inspected as references; none are embedded or redistributed.
- [Matt Blaze's photographed pin-tumbler notes](https://www.mattblaze.org/papers/notes/picking/): the pick pivots at the front keyway ward, torque binds drivers at the plug/shell interface, and spool feedback involves a small reverse plug rotation.

The user chose to preserve hidden heights. The intact metal band therefore covers
the pin/driver junctions and catching edges around the shear line. Only pin tips,
upper driver portions and spring travel are exposed. The omitted pin lengths are
illustrative, not generated from the secret bitting; no visible seam claims to be
the setting point. A continuous ward surface replaces isolated per-pin supports.
The end elevation shows a moving plug and warded keyway inside a fixed shell.
It and the front artwork use the simulation's physical rotation, rather than
turning a false set into a magnified 30-degree rotation.

The pick remains rigid and levers on a fixed entrance ward. At steep near-pin
angles its handle leaves the inspection window and is clipped, not stretched.
Spring coil count and wire width remain fixed while pitch changes with travel.
This is an explanatory game rendering, not a dimensioned manufacturer replica or
a new physical lock simulation. Hidden targets remain concealed; confirmed sets
are shown from authoritative snapshot flags in both normal play and coaching.
Pins now seat on upward contact instead of requiring a timed hold, and continued
lift can still overset them. Fine control changes lift speed and pressure steps.

## Mechanical audio

`Build-Audio.py` cuts 18 natural-pitch excerpts from **Tiny metal/lockpicking** by
cappellacciocrew ([Freesound](https://freesound.org/people/cappellacciocrew/sounds/639086/),
CC0). This version uses the original 44.1 kHz stereo 16-bit WAV supplied by Corey.
The explicit segment manifest records the source URL, hash,
cue roles, exact intervals and peak limits. NumPy and SoundFile are required.

```powershell
python Tools/LockPickingAssets/Build-Audio.py 'C:\path\to\639086__cappellacciocrew__tiny-metallockpicking.wav'
```

The builder checks the source hash and produces the embedded `LockPicking.audio`,
plus an ignored receipt, individual WAVs and `Tiny-metal-audition.wav` under
`artifacts/lockpicking/audio-review`. The source hash guards against accidentally using a preview or a different edit.
Update the manifest and shipped audio notice when intentionally changing sources.

Edits are limited to downmixing, DC removal, linear level adjustment (at most 2x),
and short edge fades. All clips remain at their original pitch and speed. Runtime
uses non-repeating variants for movement, pin setting, tension, release, strain
and unlocking; breaking uses a separate transient. Movement waits for feedback
and the current movement clip to finish. No idle movement loop or synthetic cues
are mixed in. The game UI mixer applies its volume once.

The audition file groups cues in manifest order, separated by 350 ms silence; it
is not a gameplay capture. Offline checks verify levels, bank integrity and event
routing. In-game audibility and subjective balance still require a live playtest.
The composition preview cannot establish Unity material or visual acceptance.
