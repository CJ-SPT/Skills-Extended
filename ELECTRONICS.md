# Hacking: local PDA hacking build

At the main menu, open the game console and run `hacking 2 25 42`.
Arguments are difficulty (1 Standard, 2 Secure, 3 Hardened), Hacking level
(0–51), and seed. `hacking` uses Secure, level 0, seed 1. Practice uses the
same simulation and interface as a raid, without doors, inventory requirements,
failure counts, or XP. Escape closes practice; a finished board offers another seed.

In raid, carry a **Modified PDA** and approach an eligible keycard reader.
**Hack with PDA** displays its difficulty and remaining attempts, or the unmet
requirement. There is no skill-level gate. The raid keeps running; movement,
damage, death, disconnect, leaving range, and aborting consume an attempt.
Three failed or aborted attempts lock out hacking on that door for the raid.
Normal keycard use remains available. Another player's legitimate unlock cancels
the current hack without a penalty. A relocked door may be hacked again.

Click adjacent accessible nodes, collect utilities, and destroy the system core.
Defenses block their neighboring nodes. Attacks land before retaliation; destroyed
targets cannot retaliate. Clues remain visible and refresh when a utility is collected
or a cache is opened. They show the shortest distance along connections to a remaining
core, uncollected utility or unopened cache; defenses do not count, and 5+ means
at least five links. Use buttons or keys 1–4 for
utilities; targeted utilities then require a revealed defense/core. Invalid clicks,
hovering, and selecting a target utility cost no turn. Self Repair restores 8
coherence on three turns, Shield absorbs two retaliations, Kernel Rot halves
remaining target coherence, and Secondary Vector deals 20 damage on three turns.
Victory stops all subsequent turn effects. Colors are supplemented by symbols,
numbers, labels, and tooltips.

The reusable PDA is crafted at Workbench 2 in two hours before normal modifiers:
one Broken GPhone, two Printed circuit boards, two Bundles of wires, two Capacitors,
one Portable Powerbank, and a reusable Screwdriver. Special slots and carried
containers qualify; stash ownership does not. There is no trader stock, flea sale,
battery drain, or added loot distribution.

## Configuration and persistence

In the BepInEx configuration menu (F12), open **Skills Extended > Hacking >
Hacking volume (%)**. The local 0–100 slider controls all PDA hacking audio,
including ambience, low-coherence alerts and result sounds. Zero mutes it; the
default 100 preserves the existing mix. Changes apply immediately, including
during menu practice and a live attempt, with short fades to avoid clicks.
The setting is saved in the plugin's BepInEx configuration and also respects the
game's volume settings. Each Fika player controls their own volume.

Edit Hacking in the web skill editor at `/skills-extended/hacking`, or the `Hacking` section of
`SPT_Runtime/user/mods/SkillsExtended/Resources/Configs/SkillsConfig.json`.
Older configurations receive initialized defaults. Restart/reload follows the
mod's existing configuration lifecycle; changing settings mid-raid is not a supported
way to rebalance an active puzzle. Turning `Enabled` off retains owned PDAs and
saved progress, while hiding raid interactions and disabling progression effects.

| Setting | Default | Meaning |
| --- | ---: | --- |
| AttemptsPerDoor | 3 | Shared failures/aborts per door per raid |
| BaseCoherence / CoherencePerLevel | 60 / 1 | Starting coherence |
| BaseStrength / LevelsPerStrength | 20 / 5 | Strength = base + floor(level / interval) |
| UtilitySlots / EliteUtilitySlots | 3 / 4 | Inventory capacity; elite is level 51 |
| FailureXpRatio | 0.2 | Coherence-loss XP after at least 3 hidden nodes explored |
| PdaReferenceValue | 150000 | Handbook/reference rouble value |
| DefaultDifficulty | 2 | Fallback for eligible readers |

`Tiers` contains the node, defense, utility, cache, core coherence/strength, and
success-XP defaults: Standard 19/3/2/1/40/10/10, Secure 31/6/3/2/70/15/15,
Hardened 43/9/4/2/100/20/20. These are initial balance values, not playtest results.
The effective level is captured at attempt start. No later skill/buff change alters it.

`DoorDifficulties` maps `mapId/doorId` to 1–3 and takes precedence over
`KeycardDifficulties`, which maps keycard template IDs to 1–3. `ExcludedDoors`
uses `mapId/doorId`; `ExcludedKeycards` uses template IDs. Exclusions always win.
Labs Yellow defaults to Standard; Blue, Green, Red, Black, and Violet to Hardened.
Entry cards and the scripted saferoom extraction card are excluded. Native power,
accessibility, reader-side, and skill requirements still apply. Mechanical locks
are not electronic readers. Keypad-door subclasses and readers without a keycard
ID are supported; readers without a keycard difficulty override use the default.

Hacking uses reserved numeric skill ID **200**, distinct from Lock Picking.
The skill identifier and configuration key are `Hacking`; buff identifiers are
`HackingCoherence`, `HackingStrength`, and `HackingUtilitySlots`, retaining numeric
IDs 1028, 1029, and 1030 respectively. SPT's server enum extension declares
`Hacking = 200` through `user/patchers/com.cj.skillsextended/EnumExtensions.json`.
Profile saves and responses use `"Id": "Hacking"` through SPT's standard serializer.
Existing numeric `200` and string `"200"` IDs still load with their progress intact;
the next normal save writes the named string. Stock skill names are unchanged.
The client prepatcher supplies both `JsonEnumName("Hacking")` and `EnumMember(Value = "Hacking")`
so EFT's enum converter and the standard raid-save `StringEnumConverter` agree.
The offline serialization check uses the game's actual converter list and skill
descriptors through SPT's raid-end request model, then verifies progress on reload.
Run `Tests/SkillsExtended.ElectronicsTests/Run-SerializationChecks.ps1` to build and
check an isolated copy of the server assembly with the shipped enum declaration applied.
Missing progress is initialized once at zero. Existing progress and other skills
are preserved. Success XP is awarded once per player/door/raid; qualifying failure
XP is also awarded at most once. Both use the normal skill-action pipeline with
factor 1. Aborts and menu practice grant no XP.

## Local assets and reproducible build

This local version uses the EVE hacking textures and sounds from Corey's installed
copy, as explicitly requested. It reuses `pda.bundle`; no new 3D device was needed.
The texture and sound import scripts read EVE's resource index/cache without
changing the installation. EVE source material is separate from the CC0/ISC/MIT
fallback artwork and notices. See `Tools/ElectronicsAssets/README.md` for the build.

The Unity bundle contains the prefab, static TMP font, sprites, and 12 decoded
audio cues/loops. The game plugin supplies controllers at runtime. Headless hosts
run only the shared model. The host owns reservations, action ordering, failure
counts and results; all players need the matching core and Fika add-on versions.

## Live acceptance checklist

- Menu: run `hacking 1 0 1`, `hacking 2 25 42`, and `hacking 3 51 7`.
  Check grid alignment, labels, tooltips, hotkeys, sounds, ending/retrying, and cursor
  restoration at 1080p, 1440p, and ultrawide resolutions.
  Inspect diagonal connections and moving pulses for jagged edges, and the gauge
  symbol, bars and small text for clarity at your actual display resolution.
  Check the gauge at full, low and partial coherence: bars should clear the frame,
  and the last lit segment should fade rather than be sliced across its middle.
  Check node reveal flips/rings, traveling path pulses, blocked/unblocked routes,
  damage/repair flashes, and the result ripple. Rapid clicks, abort and retry must
  leave no stuck effects or displaced nodes. Animations do not advance turns.
- Inventory: craft the PDA, put it in a special slot and then a carried container;
  verify stash-only ownership fails. Check the displayed name/value and flea restriction.
- Doors: test a normal powered/unpowered reader and Labs rooms. Verify the wrong
  side, missing PDA, exclusions, and normal keycards after three failed hacks.
- Lifecycle: abort by Escape, movement, damage, range, death and disconnect. Each
  consumes exactly one attempt; inspect the same door after a new raid to see reset.
  Win, wait for native relocking, and hack again without duplicate success XP.
- Persistence: earn Hacking XP, finish/save the raid, reload the profile, and
  confirm progress. Disable/re-enable Hacking and confirm that progress survives.
- Fika: two players start on the same door; only one wins the reservation. Test
  legitimate unlocking by the other player, hacker disconnect/reconnect, shared
  lockout counts, exactly one XP award, and a headless host.

Offline simulation, serializer, regression, compilation, package, and Unity layout
checks do not establish live door, input, sound-mix, or multiplayer acceptance.
