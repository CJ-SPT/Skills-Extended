# Lock Picking 2.0

Version 3.2.0 replaces the angle-based minigame with hidden pins and tension control.
The keyhole close-up uses Warren Marshall's purchased Mini Pack: Lockpick Kit.
The presentation correction keeps both tools in front of the lock throughout their
motion, gives the art and controls separate regions, and uses local softbox
reflections, softened normal intensity and full-resolution filtered textures.
Mechanical recordings are preloaded from embedded PCM data, with separate
movement and result voices; probing and tension changes now have audible cues.

The front view sits above a metal-colored side cutaway in both practice and raids.
The compact brass cylinder, separate key pins and steel drivers, shaded coils and steel pick
show authoritative depth, lift, tension and individual completed pins. An outline and pointer mark the selected pin; green checks mark set pins,
and amber indicates strain. Identical pin shapes and illustrative raised positions
do not expose target heights, binding order or a calibrated shear line. Release
clears completion marks immediately; retry resets animation, unlock retracts the
pick and break removes its tip. Reduced motion uses direct positioning.
The pick has a fixed-length grip, shaft and hook: insertion translates the whole
tool, and lift rotates it slightly without stretching. The grip remains outside
the cylinder throughout its stroke. A deeper keyway leaves 51 layout units below
resting pin tips, compared with 28 in the initial diagram. Pin stacks are cosmetic
representations, never the lock's secret bitting or target heights.

Snapshots include a copied `SetPinStates` array through the existing Fika JSON
reply. Missing or wrong-length flags display neutral completion state, retaining
the existing total count. No input, reward, difficulty or audio changes are made
by the cutaway. It uses UI geometry with no extra render camera or vendor assets.

## Controls and progression

- Mouse left/right selects depth when the pick is lowered; mouse up/down lifts it.
- Hold the existing configurable tension key (default A), feel for a binding pin,
  and stay within its setting window continuously for 0.30 seconds. Leaving the
  window or releasing tension resets that hold. Lower the pick before changing depth.
- Releasing tension drops all pins and clears an overset. Escape leaves freely.
- Excess force produces strain and permanent wear for that pick during this raid.
  A break consumes one use of the existing five-use set and starts the next pick
  undamaged. It never breaks the door or prevents the correct key from working.
- The same door keeps its solution throughout the raid. Cancelling resets pins,
  not tool wear. Wear follows the item between doors and players during that raid.
- All configured mechanical tiers can be attempted at skill zero. Defaults are
  3/3/4/4/5 pins; higher tiers have tighter setting windows and shorter warnings.
  With the side cutaway, default tolerances are 0.096/0.080/0.068/0.056/0.044
  (20% narrower than the initial 2.0 values), with a 0.30-second hold instead of
  0.20 seconds. Pick wear, strain warnings and skill bonuses are unchanged.
  Existing explicitly configured tolerances remain authoritative; apply these
  values through the web editor to retune an existing custom configuration.
  Skill improves setting tolerance and resilience. Elite grants a further control
  bonus, but picks can still break. Familiar completion targets 15–30 seconds;
  this timing still needs live playtesting.
- Inspect XP and success XP each pay once per player/door/raid. Failure XP requires
  setting a pin and a broken pick, and is also capped once per player/door/raid.

`lockpicking 2 0 1` in the game console opens practice at tier 2, skill 0, seed 1.
Practice does not change inventory, XP, or doors. R retries after success or break.
Use the in-game configuration menu for sensitivity, volume, reduced motion, and
the existing tension binding. The web Lock Picking page edits bonuses, wear,
five pin tiers, map door tables and XP.

Existing skill, buff and lockpick item IDs remain unchanged. Old angle/attempt
configuration fields remain readable for saved configurations but do not control
the new simulation and are hidden from the editor. Door levels outside 1–5 clamp
to the nearest puzzle tier while retaining their configured XP-table key.

## Authority and interruption

The host generates each door's solution and advances its fixed-step simulation.
Clients send only depth, lift and tension, with ordered sequence numbers; replies
never include secret pin heights or order. One actor can reserve a door at a time,
and one actor can run one attempt. Tool use and XP are applied once per result.
The host retains its own tool-use ledger to reject stale depleted inventories.

Escape, focus loss, death, damage, leaving range, moving, disappearing tools,
native door-state changes, disconnects and scene teardown release the UI/session.
The tension binding takes priority over its overlapping movement/stance binding
while held. Native inventory/weapon actions are blocked during picking. Prior
input flags, event-system settings and cursor state are restored on close.
Disconnected clients time out after three seconds without accepted input.

Fika integration 1.3.0 requires core 3.2.0. Install this same pairing on the host
and all peers, including headless hosts. The headless
host runs the simulation without loading UI/art. Standard door synchronization
remains responsible for doors already unlocked before a late join.

## Offline checks and remaining acceptance

```powershell
dotnet run --project Tests/SkillsExtended.LockPickingTests -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
dotnet run --project Tests/SkillsExtended.WebRegression -c Release -p:DeploySkillsExtended=false -p:PackageSkillsExtended=false
dotnet build 'Skills Extended.sln' -c Release -p:DeploySkillsExtended=false
```

The pin suite covers seeded solutions at all five tiers, skill 0/25/51 and
30/60/144 FPS, overset recovery, resilience, irreversible wear, once-only use/XP,
depletion, cancellation, reservations, stale input, timeouts and hidden solutions.
It also checks completion flags and JSON transport, copied-state isolation,
cutaway geometry bounds for 3–5 pins, missing flags, reset/retry, smoothing and
reduced motion. The UI geometry is compiled against offline stubs; these checks
do not establish Unity lifecycle or multiplayer acceptance.
The web suite exercises actual tier-editing handlers, validation, discard and
filesystem round trips. Previews are offline compositions, not Unity captures.

Live acceptance remains required before treating this as a release-ready build:

1. Practice at all tiers: verify Unity materials, framing, tool movement, audio,
   mouse edge behavior, sensitivity and reduced motion at 1080p, 1440p, ultrawide.
   Check the side cutaway at first/last depth, maximum lift, tension release,
   success, break and retry, and verify closing removes both views and labels.
2. In raid, pick and inspect supported doors, cancel/reopen, force a break, exhaust
   a set, switch tools/doors, and confirm correct keys always work. Check the
   intended 15–30 second pace and skill/elite benefit without guaranteed success.
3. Interrupt with damage, death, Escape, focus loss, movement and raid teardown.
   Verify camera, weapons, movement, cursor and inventory recover correctly.
   Repeat alongside Old Tarkov Movement, Hacking and Signals.
4. With two Fika players and a headless host, compete for one door, unlock for both
   peers, break exactly one use, reconnect, and late join. Verify synchronized
   inventory persistence, XP, door state and reservation cleanup.
5. Use the live administrator web editor to save tiers, reload configuration, and
   confirm the restarted client uses the values. Offline tests do not establish
   live browser-circuit or game/profile persistence acceptance.

Building with deployment disabled creates archives only. It does not install,
launch, stop, or restart the game, server, or editor.
