# Skills Extended

Skills Extended expands skill progression in **SPT** with new activities, additional skill effects, and configurable bonuses. Pick mechanical locks, bypass electronic door security, and track hidden signal caches using a Modified PDA.

**Core version:** 3.1.0 · **SPT target:** 4.1.x · **Optional Fika integration:** 1.2.0

## Features

| Skill | What it adds |
| --- | --- |
| Lock Picking | A pin-and-tension minigame for supported mechanical doors, with a front view and animated side cutaway. |
| Hacking | A PDA-based hacking minigame for supported electronic and keycard doors. |
| Signals Intelligence | Tune transmissions, record bearings, and locate hidden supply caches with the Modified PDA. |
| NATO and Eastern Weapon Proficiency | Improve handling of the configured weapon families. |
| First Aid | Improve medkit use speed and reduce the resource cost of treating wounds. |
| Field Medicine | Improve injector effects and duration. |
| Prone Movement | Improve movement speed and reduce sound while prone. |
| Silent Ops | Improve melee speed, reduce door sound, and lower suppressor prices. |
| Shadow Connections | Improve Scav cooldowns, Cultist Circle returns, and configured Cultist Scav chance. |
| USEC Negotiations and BEAR Raw Power | Faction-specific trading and quest reward bonuses. |

The mod also provides configurable effects for existing physical skills, a web configuration editor, and an administrator profile skill-level editor. Available bonuses and progression depend on the server's configuration.

## Installation

1. Download the core ZIP from [Releases](https://github.com/CJ-SPT/Skills-Extended/releases).
2. Close the game and server before updating an existing installation.
3. Extract the ZIP into your **SPT installation folder**, merging its `BepInEx` and `SPT_Runtime` folders with the existing folders. Do not place the entire archive inside a single plugins or mods directory.
4. Start the SPT server, then launch the game normally.

Install the complete package. It contains client plugins, a client patcher, server files, and a server enum declaration needed for the added skills.

### Fika

Install the core package **and** the matching Fika integration ZIP on the host and every client, including a headless host when used. The integration requires Fika **2.2.4 or newer** and is an add-on, not a replacement for the core package.

Use the same Skills Extended versions across the session: **core 3.1.0 + integration 1.2.0**. The host controls minigame outcomes, lock state, and shared progression decisions.

## Getting started

### Lock picking

Craft a **Lockpick set at Workbench level 1** and carry it to a supported locked door. Each set has five uses; breaking a pick consumes one use.

| Control | Action |
| --- | --- |
| Mouse left / right | Change depth while the pick is lowered. |
| Mouse up / down | Lift or lower the pick. |
| Hold **A** by default | Apply tension. The binding is configurable. |
| Release tension | Drop the pins and reset an overset. |
| **Escape** | Leave the minigame. |

Feel for binding pins and remain in the setting window for **0.30 seconds**. Lower the pick before moving to another depth. The side cutaway shows tool movement and completed pins without revealing target heights or binding order.

Every configured tier can be attempted at any skill level. Higher skill improves control and resilience. Excess force causes lasting pick wear during the raid; cancelling does not consume a use, but does not repair that wear. Breaking a pick never permanently breaks the door or disables its key.

For practice, enter `lockpicking 2 0 1` in the game console: tier 2, skill level 0, seed 1. Practice does not affect items, XP, or doors. Press **R** after completion to retry.

### Hacking and Signals Intelligence

Craft a **Modified PDA at Workbench level 2**. It is reusable and supports both activities.

- Carry it to supported electronic doors to access the hacking interaction.
- In a supported raid, use the PDA's **Open receiver** inventory action to tune signals and collect bearings. Move between readings to narrow down a cache's location, then pair the PDA with the cache to unlock it.

Door availability, attempts, signal placements, rewards, and skill bonuses are controlled by the server configuration.

## Configuration

While the server is running, open its Skills Extended web page. With the default local server address, this is [https://127.0.0.1:6969/skills-extended/](https://127.0.0.1:6969/skills-extended/). Use your server's address and port if different.

Sign in with an **SPT administrator account** to edit skill settings, door tables, pin difficulty, or profile skill levels. Save your changes and restart the game client to load the updated configuration.

Use the in-game BepInEx configuration menu for client preferences such as lock-picking sensitivity, tension binding, sound volume, and reduced motion. Existing custom difficulty values remain in effect when defaults change.

For Fika, select authorized profiles under **Fika client editor access** on the Skills Extended configuration page and save. The default empty list permits no client editor access. Listed players can enable Developer tools and open the in-game editor during a PMC raid, including shared raids. Authorization is checked on the server when opening, reading, and saving; removing a profile blocks further operations immediately. Headless clients and Scavs cannot use the editor. Authored changes apply to future raids or after the normal client configuration reload.

## Support

Report problems through [GitHub Issues](https://github.com/CJ-SPT/Skills-Extended/issues). Include your SPT version, Skills Extended version, whether you use Fika, relevant logs, and steps to reproduce the problem. A screenshot or short clip helps with minigame display issues.

## Building from source

The solution uses the .NET 10 SDK and references assemblies from an SPT installation. The default layout expects this repository under `<SPT>/Development/Skills-Extended`; a full solution build also needs the Fika reference assembly.

**Generate the prepatched game assembly before building the full solution.** The client projects reference `BepInEx/DumpedAssemblies/EscapeFromTarkov/Assembly-CSharp.dll`, which must contain the fields, buffs, and skill IDs added by the Skills Extended prepatcher. The original assembly under `EscapeFromTarkov_Data/Managed` is not a substitute.

### Prepare the build references

1. Start with a working SPT installation and install a compatible core release of Skills Extended. This provides `BepInEx/plugins/SkillsExtended/SkillsExtended.Client.API.dll`, which the prepatcher reads when adding the skill manager field. Building the prepatcher alone does not supply that API DLL; Debug mode still reads it during patching.
2. With the game closed, build **only the prepatcher** from the repository root:

   ```powershell
   dotnet build Client/SkillsExtended.Client.Prepatch/SkillsExtended.Client.Prepatch.csproj -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
   ```

3. Copy `Client/SkillsExtended.Client.Prepatch/bin/Release/netstandard2.1/SkillsExtended.Client.Prepatch.dll` into `<SPT>/BepInEx/patchers/`, replacing the installed copy. Keep the installed API DLL in place.
4. In `<SPT>/BepInEx/config/BepInEx.cfg`, set the following values in the existing `[Preloader]` section:

   ```ini
   [Preloader]
   DumpAssemblies = true
   LoadDumpedAssemblies = false
   BreakBeforeLoadAssemblies = false
   ```

5. Start the server and launch the game normally once. Confirm that the Skills Extended prepatcher logs `Patching Complete!` and that `<SPT>/BepInEx/DumpedAssemblies/EscapeFromTarkov/Assembly-CSharp.dll` has been regenerated. Resolve any prepatcher errors before using the dump as a build reference.
6. Close the game before continuing. Close the server as well before replacing installed server files.

Keep the original managed game assemblies untouched. Repeat this process after changing the prepatcher or updating the game assemblies; an old dump may be missing members required by the current source.

### Build and package

Once the patched reference exists, build the full solution without copying files into the installed game:

```powershell
dotnet build 'Skills Extended.sln' -c Release -p:DeploySkillsExtended=false
```

The full solution build produces core and Fika ZIPs under `artifacts/packages/Release`. If Fika is not installed in the referenced SPT directory, supply its assembly with `-p:FikaAssemblyPath="D:/path/to/Fika.Core.dll"`.

Missing custom skill or buff members usually indicate an absent or stale prepatched reference. Check the dump and prepatcher log before changing assembly references. The server's enum declaration is a separate part of the complete installation; generating the client dump does not replace it.

Documentation and notices stay in the repository and are excluded from distributions. See [Tests/README.md](Tests/README.md) for offline checks; in-game and multiplayer behavior still need live validation.

## License

See [license.md](license.md) for the repository license. Third-party assets retain their own licenses.
