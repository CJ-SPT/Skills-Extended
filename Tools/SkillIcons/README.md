# Hacking skill icon

## Original built-in skill artwork

`NativeIcons.json` maps the 27 built-in config pages to sprites exported from the installed
`EscapeFromTarkov_Data/resources.assets`. `Extract-Native.py` reads that file with UnityPy 1.25.3
and exports unchanged sprites as `Skill_<configuration key>.png`, with dimensions, sprite object IDs,
and SHA-256 hashes in an extraction receipt. Source game files are never modified.
The outputs are used by the sidebar, overview cards, and page headers.
Aim Drills uses the game's weapon-drawing artwork, as assigned by its native skill icon table.
Recoil Control has no large portrait in that table, so its page uses the game's recoil bonus sprite.

The selected artwork is the rugged PDA option, with its external cable and round
connector removed. It was generated and edited using the built-in image-generation
tool, then copied without further image transformations to:

- `Client/SkillsExtended.Client.Skills/Resources/Images/HackingSkillIcon.png`
- `Server/SkillsExtended.Server/wwwroot/icons/Skill_Hacking.png`

The client loads and caches the PNG independently of the hacking UI bundle.
The skill name, configuration key, and enum identifier are **Hacking**.
Skill ID 200 and buff IDs 1028–1030 remain unchanged; the buff identifiers are
`HackingCoherence`, `HackingStrength`, and `HackingUtilitySlots`.

## Final edit prompt

Use case: precise-object-edit. Edit this selected square PDA skill icon for an
Escape from Tarkov mod. Remove the entire external cable on the left side and its
large round metal connector at the bottom left. Remove every visible piece of
external cable/wire/connector outside the PDA. Fill their former area naturally
with the same dark textured fabric/background. Preserve the PDA itself exactly:
same rugged chipped olive-black casing, amber screen graphs, buttons, diagonal
orientation, size and position, photographic lighting, worn materials, and square
framing. Do not replace the PDA with a circuit board. The final image is the PDA
alone on the same dark background. No added lettering, logos, UI badges or borders.
The icon will represent the skill named Hacking but do not write any text into
the picture.
