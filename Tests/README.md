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
dotnet run --project Tests/SkillsExtended.ElectronicsTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
dotnet run --project Tests/SkillsExtended.LockPickingTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
dotnet run --project Tests/SkillsExtended.Regression -c Release -p:DeploySkillsExtended=false
dotnet build 'Skills Extended.sln' -c Release -p:DeploySkillsExtended=false -p:FikaAssemblyPath='C:\path\to\Fika.Core.dll'
```

The Electronics suite covers seeded graphs at levels 0/25/51, defenses, utilities,
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
That suite also covers Fika peer/profile binding, remote sync and inspection,
actor spoof rejection, disconnect/reconnect cleanup, and the installed Fika
handshake callback signature. Run it from the repository root with Fika installed.
