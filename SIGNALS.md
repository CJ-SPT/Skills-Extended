# Signals Intelligence — 3.1.0

Carry a Modified PDA in a supported PMC raid on Customs or Woods. Right-click it
and choose **Open receiver**, or press **Left Ctrl + P**. The shortcut and live
receiver volume are configurable under **Skills Extended / Signals Intelligence**
in the BepInEx configuration menu.

The receiver uses a fitted PDA housing with an inset screen, native Unity sliders
and buttons, frequency markings, numeric readouts, reception meter, and hunt-stage
indicators. Pairing replaces the bearing control with phase alignment. The bezel's
Exit button or Escape closes the receiver. Controls disable once the cache unlocks.

## Hunting a cache

1. Adjust frequency to the spectrum peak (slider or Left/Right arrows).
2. Sweep the antenna bearing for strongest reception (slider or Up/Down arrows).
3. Press Enter or **Record** and hold the tuning and bearing for three seconds.
   Press Enter or **Stop** to interrupt a reading.
4. Move at least 125 metres and take a crossing bearing. Nearly parallel bearings
   cannot establish a useful fix, even with three or four readings stored. Move
   sideways relative to the previous bearing to create a crossing angle. The
   stored count is memory usage, not a required number of readings. The plot stays
   north-up and follows you at its center, fitting previous readings and the search
   area as you travel. The white cross is you, green lines are stored bearings,
   the amber arrow is your current antenna setting, and the amber circle is the
   estimated search area. Alignment prompts name FREQUENCY or BEARING and its keys;
   waveform/PHASE alignment is only used when pairing at the case.
   Each green centre line is an approximate direction; its dim outer lines show
   the uncertainty band. Centre lines need not meet at a single point. The fix
   combines all retained shared bearings and encloses their overlapping bands,
   with three metres of approach room. Broad or unbounded overlap needs a better
   crossing bearing rather than implying a precise location. Later readings
   refine the shared fix instead of selecting an unrelated pair of lines.
5. Approach the physical signal case. Once a fix recovers its code, **Pair PDA**
   becomes available within three metres. Adjust the phase slider or Q/E until the
   two waveforms overlap, then hold alignment for five seconds with recording active.
6. After a fix, carrying the PDA inside the plotted search area produces a proximity
   beep, including while moving with the receiver closed. It speeds up from roughly
   one pulse every 1.5 seconds at the far edge to one every 0.18 seconds at the case.
   A higher pitch means you are facing the cache; a lower pitch means it is behind
   you. Turn toward the highest tone, then walk toward the faster pulses. Pitch uses
   your character's facing direction and settles to a high arrival tone within one
   metre. Tuning and the antenna slider do not affect this cue. Receiver volume
   controls it, including mute. Leaving the area, losing the PDA, dying, or unlocking
   the cache stops it. Headless hosts do not play audio.
7. Loot the unlocked case normally. Its beacon switches off for everyone.

Movement, damage, death, disconnect, and closing the screen interrupt the current
operation. Completed bearings remain. Misalignment resets the current hold;
there is no battery cost or permanent lockout. The raid does not pause.

Levels improve precision and tuning tolerance. Elite retains six plotted readings
instead of four. Accepted readings give 3 XP, at most twice per player/cache/raid;
completion gives 12 XP once to participating players still present in the raid.
Delivered XP totals are recorded under `BepInEx/config/skills-extended-signals-xp.json`
to suppress repeat awards after reconnecting, including a client process restart.
Receipts are reserved before applying XP; a crash between those operations can
lose that pending award. Transactional crash recovery is not yet guaranteed.
New readings must be at least 125 metres from each of that player's previous
readings; rejected readings grant no XP. The editor's Minimum Separation setting
controls both this limit and the triangulation baseline (125 metres by default).

## Practice and authoring

At the main menu, run `signals 0 42`, `signals 25 42`, or `signals 51 42` in the game
console. Use the simulated movement buttons to establish a fix. The pairing
practice button moves the simulated receiver to the case; it still requires a fix.
Practice changes no inventory, profile, or raid state and awards no XP.

In a loaded supported map:

- `signals_validate` searches each configured area and logs its attempted location,
  candidate count, rejection reasons, and resolved coordinates. Disabled areas are
  included so they can be checked before enabling them.
- `signals_preview bigmap-01` resolves that area with the same checks and displays
  the actual, unlootable case for 20 seconds. Its scripts and colliders are disabled.
- `signals_capture name` checks the PMC position and writes a disabled candidate
  JSON file under the plugin's `SignalsPlacements` directory. Captures use radius
  zero, preserving their X/Z coordinates. Review and enter them in the web editor.
  Capture stores a ground anchor, rather than the case prefab's offset pivot.

### How placement works

Coordinates now represent area anchors. Search Radius defaults to **10 metres**
(and accepts 0-25); zero preserves the authored X/Z while grounding the case.
Each raid freezes every enabled area on its map and uses the raid seed to order
those areas. After native loot loads, the host tests the centre, then 32 seeded
points spread across each area. Each point tries the authored rotation and its
three quarter-turn alternatives. The first valid placement wins; a failed area
falls back to the next without rerolling loot, frequency, or cache identity.

Validation derives the footprint, interaction bounds and lid sweep from the
bundled olive military hard case, authored in Unity's Y-up axes. It checks centre/corner support,
a maximum 5 cm support-height difference, slopes no steeper than 25 degrees,
and 2 cm clearance above the highest support. Navigation snapping is limited to
one metre horizontally and vertically and cannot escape the area. A usable
standing position must be within pairing range, have clear interaction sight,
and have a complete navigation path from a native PMC player spawn. Case and
approach volumes reject solid obstructions, doors, other loot, extraction zones,
and known hazards. Route segments also reject intersecting doors and hazard
volumes. No local player is required on headless hosts.

Search runs on Unity's main thread in small batches and cancels when the raid
ends. If all candidates fail, the host suppresses the hunt and logs a rejection
summary. Asset or inventory failures also suppress it. Authoring uses a fixed
seed for repeatable previews; a raid can choose a different point. Existing
signal-case geometry is ignored during repeat diagnostics.

The **24 shipped anchors remain unreviewed starting points**, spread across native
PMC approach positions and offset from spawn centres. They are not playtested
hiding spots. Physical and navigation checks cannot prove freedom from every
scripted hazard, dynamic map change, or inaccessible route; inspect them in-game.

## Configuration and compatibility

The web page `/skills-extended/signals-intelligence` edits receiver settings,
locations and weighted themed loot tables. Raid creation freezes configuration
and generates a single manifest with the map's candidate areas. Repeat requests
reuse it; contents are not generated by individual receivers or refreshed by
reopening the interface.

The initial loot target is 250,000–500,000 roubles of handbook value. Only explicit
barter/medical entries are eligible. Contents are packed into the native toolbox
grid and rejected when the table cannot meet the limits. This value is not a
guaranteed trader or flea sale price. Normal raid item and extraction rules apply.

Client and server reserve `SignalsIntelligence = 201`; buffs use 1031–1033.
Hacking remains 200. Old profiles receive zero progress once, existing progress
is preserved, and disabling Signals keeps owned PDAs and skill progress.

Fika uses the matching **1.2.0** add-on with core **3.1.0**. Host decisions control
readings, the shared fix, pairing reservations and XP. Cache IDs match on peers;
the host resolves placement once and supplies its exact position, rotation, and
native interaction network ID, registered alongside the container's item owner
before the loot-loading task completes.
Peers never search or snap the host placement independently. Only a resolved,
registered case becomes ready; repeated snapshots cannot relocate it.
Late receivers receive the native current inventory descriptor instead of the
original reward list. Inventory operations use native Fika handling. The feature
does not require Campaigns to be installed.

## Verification and remaining acceptance

Offline validation covers seeded area search, radius and snap bounds, fallback,
controlled scene-query fixtures, cancellation, peer placement serialization,
signal timing/geometry, duplicate requests and XP,
configuration, reward generation against the installed item database, skill
serialization through the actual game and server converters, web authorization,
the extracted native asset references, compilation and packages.

Corey performs live acceptance; no client/server/editor starts are automated:

- Review all 24 areas and their fallback results for grounded corners, lid clearance,
  usable approaches, access, concealment and proximity to extracts,
  scripted hazards, player spawns and valuable ordinary loot.
- Complete level 0/25/51 hunts on both maps and measure the 5–10 minute target.
- Verify layout at 1080p, 1440p and ultrawide; mouse, keyboard, mute, interruption,
  cursor restoration, and mutual exclusion with Hacking, including Hacking disabled.
- Open, search, move/rearrange/take loot, drop and extract items; check found-in-raid
  behavior and profile reload. Verify that all native locked-case actions are blocked.
- With a Fika host, two clients, and separately a headless host: combine bearings,
  contest pairing, loot concurrently, reconnect after partial looting, and verify
  no duplicate contents, stale lock state, or repeated XP.

Offline tests do not establish Unity presentation, physical placement, native
inventory synchronization, multiplayer reconnect behavior, or live gameplay acceptance.
