# Electronics asset authoring

Use CJ-SDK with Unity **2022.3.43f1**. All authoring changes stay under
`Assets/Mods/SkillsExtended.Assets`. The prefab uses UGUI and TextMesh Pro, with
no serialized game controller. `ElectronicsUiVisuals.cs` is shared with the plugin
so the offline layout preview uses the same symbols and positioning.

This revision is the user-authorized local EVE asset build. The original CC0 Kenney
and Lucide source inputs are retained for provenance, but the import step replaces
the relevant sprites and audio with locally installed EVE material. Do not describe
the EVE files as CC0 or covered by the Lucide license. No raw EVE bank or decoder is
included in the mod's runtime packages.

Install/extract the standalone Windows x64 `vgmstream-cli.exe` from
https://github.com/vgmstream/vgmstream/releases/tag/r2117 into a tools directory.
The tested extraction is `artifacts/eve-reference/vgmstream`. Retain that tool's
included license files. It converts Wwise Vorbis WEM to PCM WAV. Unity preserves
the PCM samples and original sample rate, with synchronous preload and no second
lossy compression. Bundle validation compares every sample with the source WAV
and writes `audio-validation.txt`; runtime caches the clips before interaction.

From the repository root:

```powershell
Tools/ElectronicsAssets/Build-Local.ps1 -Decoder 'artifacts/eve-reference/vgmstream/vgmstream-cli.exe'
dotnet build 'Skills Extended.sln' -c Release -p:DeploySkillsExtended=false
```

The script stages free authoring inputs, imports 34 EVE textures and 12 audio
cues/loops, generates a seeded preview fixture, launches Unity in hidden offline
batch mode, reloads and validates the bundle, renders three resolutions, and copies
the validated bundle into client Resources. It does not start Tarkov or SPT.
It must have exclusive access to the SDK project; it does not terminate editors.

Manual import steps are `Stage-Assets.ps1`, then `Import-EveAssets.ps1`, then
`python Import-EveSounds.py --decoder <path>`. Always import EVE after staging,
because staging restores the fallback artwork. Texture hashes and sound IDs/hashes
are written to `Inputs/eve-source-receipt.json` and `Inputs/eve-audio-receipt.json`
inside the SDK. Audio decoding also writes `artifacts/eve-reference/audio/manifest.json`.
The importer requires a locally cached Interface bank and matching soundbank
metadata (TQ, or SISI only when its Interface bank identity matches TQ).

The decoder extracts 24 named hacking clips for inspection; these 12 are selected:

| Action | EVE clip |
| --- | --- |
| Open | hack_startup1 |
| Reveal | hack_click1 |
| Attack | hack_click5 |
| Open cache | hack_click6 |
| Utility | hack_click4 |
| Shield enabled / spent | hack_shieldTurnOn / hack_shieldOFF |
| Win / loss | hack_Win2 / hack_Loose |
| Abort | hack_shutdown1 |
| Background / low coherence | hack_bgloop1 / coheren_low_hack_loop |

The click-to-action mapping is this mod's selection, not a claim of reconstructing
EVE's original Wwise event graph. Full streamed media is used instead of the short
prefetch payload stored in the bank. Both loops stop when the puzzle closes;
terminal cues may finish afterward. Loops are intentionally quiet under raid audio.
Feedback is limited to two voices with short crossfades instead of accumulating
`PlayOneShot` tails. Background and low-coherence loops also fade on transitions.
All sources use the game's common UI mixer group and its normal raid attenuation.

The background grid uses its measured 86-pixel horizontal pitch and 53-pixel row
spacing. The background transform and nodes share the same scale, origin, and row
spacing. This preserves alignment on all board sizes and screen aspect ratios.
Node text is deliberately smaller than the first preview.

Connections and animated beams use `ElectronicsLine.shader`: a 1.5-unit stroke
with screen-pixel edge coverage, including in overlay canvases without MSAA.
`ElectronicsHudArtwork.cs` reproducibly draws the gauge's TerraGroup emblem
and frame at four pixels per UI unit. These are authored replacements for
the enlarged low-resolution gauge sprites; original EVE inputs remain unchanged.
The gauge icon is 352x264 and frame 736x568. Sprite imports preserve
non-power-of-two dimensions without compression. The static font uses a 2048-pixel
atlas sampled at 90 points. EVE's soft glows remain in the other original sprites.
The centered TerraGroup mark is a pale monochrome geometric redraw of the divided
diamond (three filled triangles and one outlined triangle), without the wordmark.
Reference links are retained in `Resources/Notices/TerraGroup-Logo.txt`.
`sharpness-validation.txt` checks a rendered diagonal without MSAA and bundle reload;
`line-coverage.png` records that isolated render.

`ElectronicsGauge.shader` draws 17 complete segments with screen-pixel edge
coverage and transparent padding. Bars sit inside a matching frame curve with
4.5 UI units of clearance. Coherence fades the last segment instead of cropping
through it. Each view owns its two material instances and releases them on close.
The offline builder renders 0, 1, 25, 50, 75 and 100 percent coherence at all three
resolutions, verifies empty/full bounds and monotonic fill, and writes
`gauge-validation.txt` plus `gauge-<resolution>-<percent>.png`.

`ElectronicsBoardFx.cs` is shared between the plugin and the offline Unity preview.
It animates node flips, expanding rings, hit/repair feedback, directional path
pulses and victory/failure ripples. The EVE ring/glow/linebleed textures supply the
artwork; motion and timing are authored here rather than extracted EVE code.
Nodes keep their grid positions. Effects use unscaled time, never advance turns,
and never delay accepting a valid action. The observer copies model state so
local mutation and duplicate Fika snapshots cannot erase or replay transitions.
Temporary images are pooled and bounded; retry destroys the previous effect layer.
Raid results display for up to 0.95 seconds, with native unlock/XP already applied.

The build also renders `motion/frame-000.png` through `frame-031.png` using a
real seeded before/after action. `motion-validation.txt` verifies changing frames,
grid positions, settled transforms, rapid effects, cleanup and input transparency.

Outputs: `artifacts/electronics-ui/electronics_ui.bundle`, `validation.txt`, build
log, and `layout-1920x1080.png`, `layout-2560x1440.png`, `layout-3440x1440.png`.
The layout fixture exposes contents for inspection; normal play starts hidden.
