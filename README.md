# Skills Extended

Skills Extended expands skill progression in **SPT** with new activities, additional skill effects, and configurable bonuses. Pick mechanical locks, bypass electronic door security, and track hidden signal caches using a Modified PDA.

**Core version:** 3.1.1 · **SPT target:** 4.1.x · **Optional Fika integration:** 1.2.2

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

Use matching Skills Extended core and Fika integration builds across the session. The host controls minigame outcomes, lock state, and shared progression decisions. Mechanical-state and confirmed-set feedback use lockpicking protocol 7: update core and Fika on every host and joining player together; incompatible picking sessions are refused.

## Getting started

### Lock picking

Craft a **Lockpick set at Workbench level 1** and carry it to a supported locked door. Each set has five uses; breaking a pick consumes one use.

| Control | Action |
| --- | --- |
| Mouse left / right | Change depth while the pick is lowered. |
| Mouse up / down | Lift or lower the pick. |
| Hold **A** by default | Apply tension. Enable **Lock Picking → Toggle tension** to press once for on and again for off. The binding is configurable. |
| Mouse wheel / **E** / **Q** while tension is applied | Increase or decrease pressure. E/Q can be rebound. |
| Hold **Left Shift** | Fine control: quarter-speed lift and 1% pressure steps instead of 5%. Depth speed stays unchanged. Rebind in client settings. |
| Release tension | Drop all pins and reset the lock. |
| **Escape** | Leave the minigame. |

Feel for resistance and lower the pick before changing depth. A binding pin seats immediately when lifted to its setting position after clearing its catches. Stop at the firm click and green SET badge; continued lift can overset it. Reversing the mouse clears excess upward input so you can lower promptly. Spool pins can produce a false set: the cylinder turns slightly but stays locked. Ease tension while lifting to allow counter-rotation. Serrated pins produce intermediate clicks; a catch alone does not confirm a true set. Too little pressure or counter-rotation can drop previously set pins.

The partial cutaway shows observed pin-tip movement, upper driver profiles, spring compression, pressure, and tool condition. Solid metal covers the pin junctions around the shear line to preserve hidden setting heights. The pick levers against the keyway entrance, and the end view shows the plug turning inside a fixed shell. The applied-pressure gauge and wrench show your torque; the adjacent lift/input bars show when requested movement meets resistance. Retained driver tops stay raised when you move away, and fall when they lose support. Light catch ticks and firmer seating clicks reinforce the movement. Green SET badges and the TRUE SET count confirm achieved progress in all attempts; they clear when a pin drops or becomes overset. Target heights and the next binding pin remain hidden without coaching. Amber marks actual strain, not high pressure alone. Muted audio and reduced motion retain informative text, positions, and gauges.

Coaching adds pin-type guidance, target bands, and recommended-pin arrows in practice and automatically in raids for lock tiers 1–3 while your Lock Picking skill level is 10 or lower (before temporary buffs). Difficult locks can take over a minute; there is no countdown.

Every configured tier can be attempted at any skill level. Higher skill improves control and resilience. Excess force causes lasting pick wear during the raid; cancelling does not consume a use, but does not repair that wear. Breaking a pick never permanently breaks the door or disables its key.

For practice, open **Character → Skills** outside a raid and click **Practice** beside Lock Picking in the list or below its icon in the grid. Choose a difficulty and start; practice defaults to the displayed skill level. **Customize skill level** changes only the simulation. No equipment is required, and practice does not affect items, XP, or doors. Press **R** after completion to retry the same lock. Practice uses the same security mechanics as raids.

Practice is also available from the hideout's Skills screen. Coaching gives one next action at a time and marks the recommended pin with a blue arrow. Lift and tension gauges show blue target bands, white actual-value markers, and a gold commanded-lift marker; the progress bar counts confirmed sets. Follow the numbered instruction when a catch or dropped pin changes the next step. Recovery guidance uses 20–24% tension; normal guidance uses 25–35%. Keep tension applied while adjusting it, since releasing it resets the lock. These aids do not move the pick or change the simulation.

In Lock Picking setup, uncheck **Coaching hints and target guides** for normal raid-style feedback without coaching instructions, target gauges, or next-pin arrows. Confirmed-set badges and counts remain visible. Coaching defaults to on; your choice stays in effect for retries and new attempts from that setup panel. Closing a mini-game returns to setup, where **Start Practice** generates a fresh scenario; **Back** returns to Skills. Console commands remain available for repeatable seeds, for example `lockpicking 2 0 1` (tier 2, skill level 0, seed 1).

The lockpicking cutaway identifies the pin currently above the pick as **Standard**, **Spool**, or **Serrated**, including with coaching disabled. A separate tip below the live feedback explains how to handle that pin type and updates as you move the pick. In practice and raids, the header shows the seed, mod version, and effective skill level beside the tier. Include these in bug-report screenshots; replay with `lockpicking <tier> <skill> <seed>` using the same version and lockpicking configuration. Negative seed values are valid. Retrying the same lock preserves its seed; the seed reproduces the initial lock layout, not previous inputs or accumulated pick wear.

Sensory text describes turning pressure, spring contact, resistance, lowering the pick, and retained movement. The same feedback line that confirms a set also identifies false sets, overset or caught pins, counter-rotation, cleared catches, and recovered oversets. False-set and selected-pin overset indications persist while those conditions remain; an individual catch is not mislabeled as a whole-lock false set. Drop captions combine simultaneous drops and remain visible alongside coaching. Damage text appears only after the pick actually loses condition. These captions remain available with muted audio and reduced motion.

Server administrators can turn off **Lock Picking → Enable in-raid coaching** (`LockPicking.EnableRaidCoaching`) to hide raid target guidance and pin-type tips. Pin-type labels, confirmed sets, and mechanical feedback remain available, and practice keeps its own coaching controls. This setting defaults to `true`; target guidance still requires lock tiers 1–3 and an unbuffed skill level of 10 or lower.

### Hacking and Signals Intelligence

Craft a **Modified PDA at Workbench level 2**. It is reusable and supports both activities.

- Carry it to supported electronic doors to access the hacking interaction.
- In a supported raid, use the PDA's **Open receiver** inventory action to tune signals and collect bearings. Move between readings to narrow down a cache's location, then pair the PDA with the cache to unlock it.

Door availability, attempts, signal placements, rewards, and skill bonuses are controlled by the server configuration.

**Signals Intelligence → Show blue cache arrow** (`SignalsIntelligence.ShowCacheArrow`) controls the arrow above the physical cache for all players. It defaults to `true`; turning it off leaves the cache, receiver, pairing, and proximity audio available. Save either assistance setting in the server editor and restart connected clients before the next raid.

To learn either activity without a PDA, use its **Practice** button on **Character → Skills** outside raids. Hacking offers difficulties 1–3; Signals uses a simulated receiver and pairing scenario. Both default to the displayed skill level and allow a practice-only level adjustment. Disabled or locked skills have no practice button. Practice never creates world caches or awards XP.

## Configuration

Illustrated [mini-game guides](https://127.0.0.1:6969/skills-extended/guides) cover Lockpicking, Hacking, and Signals Intelligence. No sign-in is required. Use your server's address and port if different.

While the server is running, open its Skills Extended web page. With the default local server address, this is [https://127.0.0.1:6969/skills-extended/](https://127.0.0.1:6969/skills-extended/). Use your server's address and port if different.

Sign in with an **SPT administrator account** to edit skill settings, door tables, pin difficulty, or profile skill levels. Save your changes and restart the game client to load the updated configuration.

The editor has **46 skill pages**, including the remaining implemented mental, weapon, combat, practical, and hideout skills. Every skill page includes its individual leveling multiplier and original game artwork. Built-in bonus fields start with game defaults and show a **Reset to default** button. Server-backed defaults come from the loaded globals before Skills Extended overrides, including other mods; hardcoded client bonuses use the installed game's rules. Viewing defaults does not create saved overrides. Resetting or clearing an edited field restores its default. Percentage fields use percentage points (0.5 means 0.5%). Native elite unlocks and nonlinear rules remain unchanged. Unimplemented skills such as Memory and Night Operations have no bonus pages.

Built-in bonus overrides are saved under `NativeSkills.Overrides` in `SkillsConfig.json`; older configurations default to an empty set. Server-side bonuses and hideout settings apply after saving. Restart all connected clients to load raid bonuses, with matching client and common assemblies on players and headless hosts.

Use the in-game BepInEx configuration menu for client preferences such as lock-picking sensitivity, tension and pressure bindings, sound volume, and reduced motion. Existing custom difficulty values remain in effect when defaults change.

The administrator **Leveling speed** page controls every player skill, including vanilla and extended skills. Its global skill multiplier combines with each individual skill multiplier: 2× globally and 1.5× Endurance gives 3× incoming Endurance XP. Individual weapon mastery has its own independent multiplier. Each control accepts 0–100; 1× preserves current speed and 0 disables the affected gameplay gains. Existing fatigue, bonuses, XP tables, and skill caps still apply. Dormant or disabled skills stay inactive; quest skill rewards and manual profile edits retain their stated values.

These settings are stored under `LevelingSpeed` in `Resources/Configs/SkillsConfig.json`. Missing settings in older configuration files default to 1×. Server-awarded gameplay gains (crafting, consumption, upgrades, Scav case actions, gym workouts, repairs, insurance, and item examination) use saved settings immediately. Quest and prestige rewards retain their stated values. Restart connected game clients to load raid and mastery settings. Use matching updated Skills Extended files on Fika hosts, headless hosts, and every player.

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


### Client translations

In-game practice, lockpicking, hacking, Signals, and developer-editor UI text uses `SkillsExtended.*` keys from `Server/SkillsExtended.Server/Resources/Locales/en.json`. Add translations using the same keys in the existing language file in that directory (for example `ru.json`). The server fills missing entries with English before applying language overrides. The client resolves keys through EFT's `Localized()` lookup, so it uses the player's selected game language. F12 option names and descriptions remain unchanged.

Preserve numbered placeholders and their format suffixes, such as `{0}`, `{1:0.0}`, and `{2:P0}`; they may move within a translated sentence. Preserve intentional newlines, control names, and rich-text markup. Existing skill and item locale keys remain available. Text baked into artwork, diagnostic logs, early loader failures, and console-command metadata are not part of the in-game text catalog. Custom administrator-authored names and third-party diagnostic details are displayed as supplied.

For new client text, add an English key and use `LocalizedText.Get("SkillsExtended.Feature.Message", arguments...)` at the display boundary. Shared authority code must send a key or `LocalizedText.Message(key, arguments...)`; receiving UI calls `LocalizedText.Resolve(message)` so the host's language is never baked into replicated text. The shared assembly embeds the same English file for startup, offline tools, missing keys, and invalid translation-format fallback. Keep host, headless, and player binaries matched when updating. Restart the server to load edited locale files, then reconnect the client and reopen the affected screen.

`Tests/SkillsExtended.ElectronicsTests/Run-SerializationChecks.ps1` includes locale-key, placeholder, fallback, language-switch, and authority-message checks. Visual fit and font coverage for each translated language still require an in-game check.
