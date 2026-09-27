# Skills Extended regression checks

Run from the repository root:

```powershell
dotnet run --project Tests/SkillsExtended.Regression -c Release -p:DeploySkillsExtended=false
dotnet build 'Skills Extended.sln' -c Release -p:DeploySkillsExtended=false -p:FikaAssemblyPath='C:\path\to\Fika.Core.dll'
```

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
door unlocking/breaking with another player and a headless host.
