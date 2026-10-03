# In-game developer editor

Enable **Developer tools → Enable developer editor** in the BepInEx configuration
manager. Load a PMC raid and press **Ctrl+F9**, or enter `skills_editor` in the
game console. `signals_editor` remains an alias. The shortcut is configurable.

The editor uses the same Unity UI Toolkit styling and controls as Campaigns and
ships its own UI bundle. Campaigns does not need to be installed.

**AI and raid time continue.** Hold right mouse to look and use WASD/QE to fly.
Shift multiplies speed by four; Ctrl reduces it to one quarter. Set camera speed
in the toolbar. Press F to frame a selection and Ctrl+Z/Ctrl+Y to undo/redo. Escape
cancels typing or an active placement/drag before closing. Closing retains drafts
for the same raid and does not save them. Drafts end with the raid.

Automatic garbage collection stays enabled during raids by default through
**Memory → Automatic garbage collection during raids**, including headless hosts.
This prevents temporary allocations accumulating when the game disables GC on
machines with at least 25 GB of RAM. The setting can be changed during a raid;
the editor always keeps collection enabled while open. The latest game-requested
mode is restored once both raid and editor collection ownership end.
Idle draft status is cached after edits/save/undo rather than rebuilding
placement comparisons and door sets every frame; unchanged handles do not redraw.

For Fika, an administrator must select your profile under **Fika client editor
access** on the Skills Extended web configuration page and save. The default
empty list denies access. Authorized hosts and joining players can edit during
shared raids; connecting peers does not close the editor. Permissions are checked
on every server read/save and periodically while open. Removing a profile blocks
further operations immediately and closes its editor at the next access check.
Headless clients remain excluded. No PDA is required. A disabled gameplay skill
does not disable its authoring tool. Standalone SPT access is unchanged.

## Signal caches

Available on every loaded raid map except Factory (day and night). Select Add and
click terrain or a static surface. New maps start with no locations; no locations
are generated automatically. A runtime hunt requires an enabled saved location
on that map. Existing Customs/Woods locations are preserved.
Signals does not initialize in the hideout: it creates no runtime, requests no
raid manifest or peer snapshot, and loads no cache assets there.
Select previews by their labels or case bodies. Use Move/Rotate and the visible
handles, or edit XYZ/yaw in the inspector. Duplicate and Delete operate on the
draft; undo/redo also covers inspector edits.
Move handles sit at the preview object's pivot and follow its local axes.
Drag around the rotation ring to change yaw about that same pivot.

New placements are enabled and exact (search radius zero). Zero radius spawns
the native prefab at the saved XYZ and yaw, without grounding, rotation fallback,
or geometry/navigation checks. The preview uses that same exact transform.
A radius greater than zero, up to 25 metres, enables bounded runtime placement
search; validation shows its resolved preview separately from the authored anchor.

Validate accepts exact placements directly and checks enabled search areas for
geometry/access rejections. Save repeats this process and preserves exact XYZ/yaw,
then writes only the current map's placements. Disabled drafts bypass geometry checks.
Previews have no usable interaction, rewards, XP, or synchronized multiplayer state.
Saved placements are used in subsequent raids; the active raid cache is unchanged.

## Doors

Available on every loaded map. Click a door/reader in the scene or search the loaded
door list by name, scene name, key, ID, or scene number (for example `#12`).
Rows have a consistent height, with locations and unique numbers to distinguish
repeated names. Numbers remain stable while filtering the loaded scene. Show
filters the list to all doors, editable doors, or doors with configured rules.
Rescan finds doors added after opening the tool.
Selecting a door shows its native state, key, eligibility, and configuration rules.
Only relevant skill sections are shown; existing unsupported overrides remain
visible for review and can be removed with Clear overrides. Identity and shared
map-table notes are under Details.

The shared browser, inspector and choice menus use Campaigns' themed scrollbars.
Choice menus stay within the scaled viewport, flip above fields near the bottom,
and support arrow keys/Enter. Escape or clicking outside dismisses the menu before
any scene edit; camera and scene shortcuts are suppressed while it is open.

Electronic readers support a per-door Hacking difficulty (1–3, or inherit key/default)
and a per-door exclusion. Global key exclusions remain authoritative and are shown
in the inspector. Hacking does not support mechanical or keyless doors.

Recognized mechanical locks support a Lock Picking tier (1–5), or Disabled to remove
their lookup entry. Global skill switches, key recognition, and normal runtime
interaction requirements still apply. Existing legacy tier entries remain unchanged
until explicitly edited; the inspector explains their effective tier. Factory
day/night and the Ground Zero variants retain their existing shared lock tables.
Unloaded saved rules remain listed and are preserved unless explicitly cleared.

Clear overrides removes only the selected door's map-specific rules. Save writes
both skills' current-map door tables together, preserving global key rules and other
maps. **Restart the game client manually to load saved door rules.** The editor does
not unlock, move, replace, or modify the native state of doors in the active raid.

## Saving and extension

Requests are bound to the authenticated profile's active PMC raid and map. Both
tools use the existing transactional configuration store and revision checks.
Edits from another tool, web editor, or disk produce a conflict; the draft is kept
and Reload is explicit. Dirty reloads require confirmation. Failed writes roll back
through the existing configuration store.

The client `IDeveloperEditorTool` interface separates tool draft, actions, inspector,
selection, and persistence from the shared camera/input/panel lifecycle. Add future
tools to the host's tool registry; each tool declares its map support and owns its
data. The shared server session service is independent of Signals gameplay.

## Offline and live acceptance

Build the full solution with deployment disabled, then run:

```powershell
dotnet run --project Tests/SkillsExtended.DeveloperEditorTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
dotnet run --project Tests/SkillsExtended.Regression -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
dotnet run --project Tests/SkillsExtended.WebRegression -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
./Tests/SkillsExtended.ElectronicsTests/Run-SerializationChecks.ps1
```

The editor suite covers detached drafts, history, stale validation, exact yaw,
exact XYZ preservation, layout dimensions, serialized client/server requests,
session admission/expiry, map isolation, conflicts, rollback, native map casing,
legacy door preservation, and compiled cleanup/dependency contracts. Existing
client fixtures exercise the reused native input and cursor restoration helpers.

Offline checks do not establish Unity rendering or in-raid interaction. After
manually restarting the server and game, verify UI/font rendering with Campaigns
also installed; 1080p, 1440p and ultrawide layout; pointer separation and typing;
free camera, case selection and handles; physical case opening/approach space;
draft retention and restoration on close/death/raid exit; Fika peer-join closure;
persisted cache spawning on a subsequent raid; and saved door eligibility after
the next client restart.
