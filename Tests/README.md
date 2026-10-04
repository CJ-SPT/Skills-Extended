# Skills Extended regression checks

The shared in-game developer editor (cache placement and Hacking/Lock Picking door
rules) is documented in [the developer editor guide](../docs/developer-editor.md).
Build the full solution first, then run `Tests/SkillsExtended.DeveloperEditorTests`
with deployment and packaging disabled for its offline draft, geometry, session,
configuration, layout and compiled cleanup checks.

For configuration editing, profile tools, and offline page/layout checks, see
[the web regression suite](SkillsExtended.WebRegression/README.md).

Run from the repository root:

```powershell
dotnet run --project Tests/SkillsExtended.PracticeTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
dotnet run --project Tests/SkillsExtended.SignalsLoadingTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
dotnet run --project Tests/SkillsExtended.SignalsInventoryTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
& ./Tests/SkillsExtended.ElectronicsTests/Run-SerializationChecks.ps1
dotnet run --project Tests/SkillsExtended.LockPickingTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
dotnet run --project Tests/SkillsExtended.Regression -c Release -p:DeploySkillsExtended=false
dotnet build 'Skills Extended.sln' -c Release -p:DeploySkillsExtended=false -p:FikaAssemblyPath='C:\path\to\Fika.Core.dll'
```

Practice checks execute the production Skills-screen button bindings and input
patches against small Unity/EFT substitutes. They cover recycled list/icon views,
single listeners, unrelated/nested icons, visibility changes, grid-spacing cleanup,
and declared patch callbacks. Layout arithmetic uses the installed native Skills
prefab geometry at 1280x720, 1920x1080 and 3440x1440; it is not a rendered UI check.
The regression suite additionally covers practice skill mapping, displayed-level
defaults, difficulty/level bounds, disabled/locked/headless/raid restrictions,
menu/hideout eligibility, double launches, failed launches, teardown, and nested
setup/game input restoration.

Live acceptance: open Character > Skills from the menu and hideout, switch between
list and icon views, sort/filter, and open all three Practice buttons repeatedly.
Verify names, bars, levels, tooltips and footers remain readable at those resolutions.
Check fresh starts and existing retries, simulated-level controls, Escape returning
one layer at a time, no underlying clicks/navigation, and cleanup on leaving Skills
or changing scenes. Compare XP/inventory before and after; no doors or world caches
should change. Confirm practice is unavailable in raids and for disabled/locked
skills. These checks require a user-run client; the offline suites do not start it.

Signals loading checks execute the production completion routines and both native
hooks with external Unity/EFT substitutes. They cover solo SPT, regular Fika and
headless hosts, peers, duplicate calls, native failures, raid cancellation, disabled
hunts and unsupported maps. Serialization checks also inspect the installed
`Fika.Headless.dll` boundary when present; set `SKILLS_EFT_HEADLESS_ROOT` for an
installation outside the default sibling directory. Live loading and shared cache
interaction still require a manual Woods/Customs retest in each hosting mode.

Signals inventory checks use the installed game's actual descriptors and native
binary codec. They reproduce the abstract-component JSON failure, then roundtrip
nested resource, medical and key components, grid/slot identities and updated loot
state through the existing inventory string envelope. Every host and peer needs
matching client binaries for the native inventory payload. Set `SKILLS_EFT_ROOT`
to test a separate installation; `SignalsGameRoot` overrides build references.
Loading checks also verify repeated identical failed snapshots are diagnosed once
and that a changed inventory can recover. These checks do not start the game.

The Electronics script prepares an isolated server enum fixture under artifacts;
it does not modify installed assemblies. The suite covers seeded graphs at levels 0/25/51, defenses, utilities,
turn ordering, XP/attempt authority, profile initialization, and ID 200 round trips
through the installed client's enum converter and the actual server profile model.
It also exercises the production client configuration reader with the installed
Newtonsoft assembly: full, partial, legacy, customized and explicitly empty
collections must load without appending built-in defaults. Add `-- --installed-config`
to validate the installed configuration read-only.
Electronics still requires in-raid input, sound, outcome and multiplayer acceptance.

The solution uses the installed game's managed and dumped assemblies. Fika defaults
to `BepInEx/plugins/Fika/Fika.Core.dll`; use `FikaAssemblyPath` for a separate reference
copy when Fika is not installed. The Fika integration is only deployed when Fika is
installed, and all deployment targets honor `DeploySkillsExtended=false`.

The executable regression suite links the production patch source and configuration.
Small substitutes supply the external game/server APIs; no game, server, profile, or
network session is started. These checks validate patch behavior, not Harmony
installation or Unity/Fika gameplay. The full solution build separately checks the
real dependency APIs.

Coverage includes:

- Hacking cursor graphics, native cursor policy, UI event processing, and restoration
  of prior input state after close, repeated cleanup, and scene teardown.
- Keypad discovery through the native scene registry, Factory hacking starts,
  derived/keyless readers, late loading, destroyed targets, and peer unlock lookup.
- Cursor method discovery with overloaded methods and unrelated static/instance types.
- Electronics menu practice leaving player-dependent input flags untouched, raid
  capture/restoration, repeated cleanup, and player disappearance during teardown.
- Quest XP at levels 0, 1, 10 and 51, disabled skills and faction eligibility.
- Cultist Circle timers and disabled behavior without a sacrifice session.
- Quest cash patch registration and percentage units.
- Both traders' independent faction locks and switches, elite stacking, the discount
  ceiling, and untouched non-money barters.
- Disabled and elite scav cooldown behavior.
- All seven physical switches preserving the game's original constructor arrays,
  and separate Endurance stamina/breath settings.
- Field Medicine modifying personal settings while preserving shared templates,
  negative effects, and disabled behavior.
- First Aid applying all three wound discounts without compounding or leaking across
  owners, while retaining surgery costs and other effect properties.
- Prone volume preserving the original baseline and other actors' sound.
- Door volume for local, remote and unattributed interactions.
- Strength obstacle entry/exit, stale elite restrictions, and disabled/nonlocal actors.

## In-game acceptance still required

After restarting the client/server normally, verify actual quest payouts and timer
displays; medkit consumption and injector effects; prone/door audio; and bush/swamp
movement at ordinary and elite levels. Recheck disabled settings after the required
client restart. Attempt lockpicking while moving without Old Tarkov Movement and
confirm the normal warning appears. For Fika, verify plugin loading and synchronized
door unlocking and pick consumption with another player and a headless host.
Lock Picking 2.0 still requires live artwork, input, audio and Fika acceptance;
its offline pin and cutaway suite is in `SkillsExtended.LockPickingTests`.
Guided coaching checks follow the published target bands through deterministic
locks at every tier, multiple skill levels and frame rates, using the UI's 5%
pressure increments. They cover instruction priorities, actual overset recovery,
hold interruption, binding changes, retries, read-only coaching and snapshot
separation. Cutaway checks distinguish recommended, selected and true-set pins.
Live coaching acceptance: verify gauges and instructions at 16:9 and ultrawide,
rebound controls and toggle tension, security-pin recovery, retries, and coaching
off. The offline checks do not establish rendered readability or input feel.
That suite also covers Fika peer/profile binding, remote sync and inspection,
actor spoof rejection, disconnect/reconnect cleanup, and the installed Fika
handshake callback signature. Run it from the repository root with Fika installed.
