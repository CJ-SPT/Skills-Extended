using System.Text.Json;
using SkillsExtended.Config.Skills;
using SkillsExtended.LockPicking;

var checks = 0;
void Check(bool value, string message)
{
    checks++;
    if (!value)
        throw new Exception(message);
}
if (args.Contains("--audio-only"))
{
    AudioChecks.Run(Check);
    Console.WriteLine($"Lock-picking audio: {checks} checks passed (simulated Unity lifecycle).");
    return;
}
if (args.Contains("--public-only"))
{
    PublicControllerChecks.Run(Check, null);
    return;
}
var config = new LockPickingData
{
    Enabled = true,
    InspectLockXpRatio = .15f,
    FailureLockXpRatio = .25f,
    XpTable = new() { ["1"] = 4 },
};
config.Validate();

// Remote requests must use the profile bound by Fika's handshake, not a nickname
// or an actor supplied by a different connection.
var peers = new SkillsExtendedFika.PeerActorRegistry<object>();
var remote = new object();
var otherPeer = new object();
Check(!peers.Matches(remote, "profile-a"), "unbound peer rejected");
peers.Bind(remote, "profile-a");
peers.Bind(otherPeer, "profile-b");
Check(peers.Matches(remote, "profile-a"), "remote profile accepted");
Check(!peers.Matches(remote, "Player Nickname"), "nickname is not an actor");
Check(!peers.Matches(remote, "profile-b"), "another player's actor rejected");
Check(!peers.Matches(remote, ""), "empty actor rejected");
var remoteAuthority = new PickingAuthority(config, 1);
var sync = new PickRequest { ProtocolVersion = PickingProtocol.Version, Actor = "profile-a" };
var synced = peers.Matches(remote, sync.Actor)
    ? remoteAuthority.Process(sync, 0, 1, 0, 10, null)
    : null;
Check(synced?.Raid == remoteAuthority.Raid, "remote handshake receives authority raid");
var inspect = new PickRequest
{
    ProtocolVersion = PickingProtocol.Version,
    Actor = "profile-a",
    Raid = synced.Raid,
    Door = "door",
    Operation = "inspect",
};
Check(
    peers.Matches(remote, inspect.Actor)
        && remoteAuthority.Process(inspect, 0, 1, 0, 10, null).Error == null,
    "remote inspection accepted after sync"
);
peers.Remove(remote);
Check(peers.Actor(remote) == null, "disconnect removes actor");
Check(peers.Matches(otherPeer, "profile-b"), "disconnect preserves other connections");
var reconnect = new object();
Check(!peers.Matches(reconnect, "profile-a"), "new connection needs handshake");
peers.Bind(reconnect, "profile-a");
Check(peers.Matches(reconnect, "profile-a"), "reconnect can bind same profile");
peers.Clear();
Check(
    peers.Actor(otherPeer) == null && peers.Actor(reconnect) == null,
    "new manager clears previous raid"
);

// Compile-time checks cannot verify a private Harmony target. Check the installed
// Fika assembly's actual callback signature without starting the game.
using (
    var fika = Mono.Cecil.AssemblyDefinition.ReadAssembly(
        Path.GetFullPath("../../BepInEx/plugins/Fika/Fika.Core.dll")
    )
)
{
    var callback = fika
        .MainModule.GetType("Fika.Core.Networking.FikaServer")
        .Methods.Single(m => m.Name == "OnNetworkSettingsPacketReceived");
    Check(
        callback
            .Parameters.Select(p => p.ParameterType.FullName)
            .SequenceEqual(
                new[]
                {
                    "Fika.Core.Networking.Packets.Backend.NetworkSettingsPacket",
                    "Fika.Core.Networking.LiteNetLib.NetPeer",
                }
            ),
        "installed Fika handshake patch signature"
    );
    Check(
        callback.Parameters[0].Name == "packet" && callback.Parameters[1].Name == "peer",
        "installed Fika Harmony argument names"
    );
}
void Advance(
    PinLockEngine e,
    float depth,
    float lift,
    bool tension,
    float seconds = 1.8f,
    int fps = 60
)
{
    for (var i = 0; i < (int)Math.Round(seconds * fps); i++)
        e.Advance(1f / fps, depth, lift, tension);
}
void Solve(PinLockEngine e, PinLockDefinition definition, int fps)
{
    foreach (var pin in definition.Order)
    {
        Advance(e, (pin + .5f) / definition.Order.Length, 0, true, 1, fps);
        Advance(e, (pin + .5f) / definition.Order.Length, definition.Heights[pin], true, 2, fps);
    }
}
for (var tier = 1; tier <= 5; tier++)
for (uint seed = 1; seed <= 40; seed++)
    foreach (var skill in new[] { 0, 25, 51 })
    {
        var definition = PinLockDefinition.Create(config.Tier(tier).Pins, seed);
        Check(
            definition.Order.Distinct().Count() == definition.Heights.Length,
            "Every pin occurs once in the binding order"
        );
        Check(
            JsonSerializer.Serialize(definition)
                == JsonSerializer.Serialize(PinLockDefinition.Create(config.Tier(tier).Pins, seed)),
            "Seed is reproducible"
        );
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var game = new PinLockEngine(definition, config, tier, skill);
            Solve(game, definition, fps);
            Check(
                game.Outcome == PickOutcome.Unlocked && game.Wear == 0,
                $"Solvable tier {tier}, skill {skill}, fps {fps}, seed {seed}"
            );
        }
    }
var def = PinLockDefinition.Create(3, 43);
var first = def.Order[0];
var wrong = (first + 1) % 3;
var overset = new PinLockEngine(def, config, 1, 0);
Advance(overset, (first + .5f) / 3, 1, true, 2);
Check(overset.Coaching().State == PinState.Overset, "Excess lift oversets the binding pin");
Advance(overset, 0, 0, false, 1);
Check(
    overset.Coaching().SetPins == 0 && overset.Snapshot().Strain == 0,
    "Release clears pins and strain"
);
Solve(overset, def, 60);
Check(overset.Outcome == PickOutcome.Unlocked, "Overset lock remains recoverable");
var novice = new PinLockEngine(def, config, 1, 0);
var elite = new PinLockEngine(def, config, 1, 51);
Advance(novice, (wrong + .5f) / 3, 1, true, 2);
Advance(elite, (wrong + .5f) / 3, 1, true, 2);
Check(
    novice.Wear > elite.Wear && elite.Wear > 0,
    "Elite reduces wear without removing consequences"
);
Advance(elite, (wrong + .5f) / 3, 1, true, 10);
Check(elite.Outcome == PickOutcome.PickBroken, "Elite picks still break");
var before = novice.Wear;
Advance(novice, 0, 0, false, 2);
Check(novice.Wear == before, "Releasing tension never repairs accumulated wear");
var authority = new PickingAuthority(config, 42);
PickRequest Request(string operation, string actor = "a", string door = "d", string tool = "t") =>
    new()
    {
        ProtocolVersion = PickingProtocol.Version,
        Raid = authority.Raid,
        Actor = actor,
        Door = door,
        Tool = tool,
        Operation = operation,
    };
PickReply Process(PickRequest r, string error = null, int used = 0) =>
    authority.Process(r, 0, 1, used, 5, error);
var startRequest = Request("start");
var start = Process(startRequest);
Check(start.State.Outcome == PickOutcome.Active, "Session starts");
Check(start.State.Seed == 43 && start.State.SkillLevel == 0, "Raid reports its original generation seed and effective skill");
foreach (var reportedSeed in new uint[] { 0, 1, 2147483648, uint.MaxValue })
{
    var generated = PinLockDefinition.Create(config.Tier(1), reportedSeed);
    var metadata = new PinLockEngine(generated, config, 1, 25).Snapshot();
    var wire = Newtonsoft.Json.JsonConvert.DeserializeObject<PickSnapshot>(
        Newtonsoft.Json.JsonConvert.SerializeObject(metadata));
    var consoleSeed = unchecked((int)wire.Seed.Value);
    var reproduced = PinLockDefinition.Create(config.Tier(1), unchecked((uint)consoleSeed));
    Check(JsonSerializer.Serialize(generated) == JsonSerializer.Serialize(reproduced)
        && wire.SkillLevel == 25, "Displayed signed seed reproduces geometry after Fika serialization, including zero and high-bit seeds");
}
Check(Process(Request("start", "b")).Error != null, "Another actor cannot reserve the door");
Check(Process(Request("start", "a", "other")).Error != null, "One active session per actor");
Process(startRequest);
Check(authority.Active.Count == 1, "Duplicate start is idempotent");
var session = authority.Active["d"];
var hostile = Request("cancel", "b");
hostile.Attempt = start.Attempt;
Process(hostile);
Check(authority.Active.ContainsKey("d"), "Another actor cannot cancel the session");
var input = Request("input");
input.Attempt = start.Attempt;
input.Sequence = 2;
input.Depth = (wrong + .5f) / 3;
input.Lift = 1;
input.Tension = true;
Process(input);
input.Sequence = 1;
input.Lift = 0;
Process(input);
Check(session.Lift == 1 && session.Sequence == 2, "Stale inputs cannot overwrite current input");
input.Sequence = 3;
input.Lift = float.NaN;
Process(input);
Check(session.Sequence == 2, "Nonfinite input rejected");
for (var i = 0; i < 40; i++)
    authority.Advance(.05f);
var worn = session.Engine.Wear;
var cancel = Request("cancel");
cancel.Attempt = start.Attempt;
var cancelled = Process(cancel);
Check(
    cancelled.State.Outcome == PickOutcome.Cancelled
        && cancelled.ToolUses == 0
        && cancelled.Xp == 0,
    "Cancellation costs no use or XP"
);
var again = Process(Request("start"));
Check(again.State.Seed == start.State.Seed && cancelled.State.Seed == start.State.Seed,
    "Door retries and terminal replies retain the original seed");
Check(
    again.State.Wear == worn && worn > 0 && authority.Active["d"].Engine.Coaching().SetPins == 0,
    "Reopening preserves tool wear and resets pins"
);
Solve(authority.Active["d"].Engine, def, 60);
var success = authority.Advance(.05f).Single();
Check(
    success.State.Outcome == PickOutcome.Unlocked && success.Xp == 4 && success.ToolUses == 0,
    "Stable door solution and successful XP"
);
Check(authority.Advance(.05f).Count == 0, "Completed results are emitted once");
var other = Process(Request("start", door: "other"));
Check(other.State.Seed == 44 && other.State.Seed != again.State.Seed,
    "A different door receives its own generation seed");
Check(other.State.Wear == worn, "Tool wear follows the item to another door");
authority.End("other", true);
var inspected = Process(Request("inspect"));
Check(
    inspected.Xp == .6f && Process(Request("inspect")).Xp == 0,
    "Inspection XP once per actor per door"
);
Check(Process(Request("inspect", "b")).Xp == .6f, "Inspection XP is personal");
var timeout = Process(Request("start", door: "timeout"));
PickReply last = null;
for (var i = 0; i < 65; i++)
{
    var replies = authority.Advance(.05f);
    if (replies.Count > 0)
        last = replies[0];
}
Check(
    last.State.Outcome == PickOutcome.Interrupted && last.ToolUses == 0,
    "Lost client releases reservation without penalty"
);
Check(
    Process(Request("start", door: "blocked"), "Native requirements").Error != null,
    "Native eligibility failures refuse start"
);
var oldRaid = Request("start");
oldRaid.Raid = "old";
Check(Process(oldRaid).Error != null, "Previous raid requests are rejected");
Check(
    Process(Request("start", tool: "empty"), used: 5).Error != null,
    "Depleted tool cannot start"
);
var breaking = new PickingAuthority(config, 100);
var breakDefinition = PinLockDefinition.Create(3, 101);
for (var use = 1; use <= 5; use++)
{
    // The caller deliberately keeps reporting zero uses, as a delayed inventory peer could.
    var r = new PickRequest
    {
        ProtocolVersion = PickingProtocol.Version,
        Raid = breaking.Raid,
        Actor = "a",
        Door = "d",
        Tool = "t",
        Operation = "start",
    };
    var opened = breaking.Process(r, 0, 1, 0, 5, null);
    Check(
        opened.State.Outcome == PickOutcome.Active
            && opened.ToolUses == use - 1
            && opened.State.Wear == 0,
        "A fresh pick in the set starts undamaged using the authority's use count"
    );
    var game = breaking.Active["d"].Engine;
    var progressed = use > 1;
    if (progressed)
    {
        var pin = breakDefinition.Order[0];
        Advance(game, (pin + .5f) / 3, 0, true, 1);
        Advance(game, (pin + .5f) / 3, breakDefinition.Heights[pin], true, 2);
        Check(game.Coaching().SetPins == 1, "Failure XP test makes real pin progress");
    }
    var resistantPin = breakDefinition.Order[2];
    Advance(game, (resistantPin + .5f) / 3, 0, true, 1);
    Advance(game, (resistantPin + .5f) / 3, 1, true, 10);
    var broken = breaking.Advance(.05f).Single();
    Check(
        broken.State.Outcome == PickOutcome.PickBroken && broken.ToolUses == use,
        "Each break consumes exactly one use"
    );
    Check(broken.Xp == (use == 2 ? 1 : 0), "Failure XP requires progress and pays once per door");
    Check(breaking.Advance(.05f).Count == 0, "A completed break cannot be charged twice");
    var lateCancel = new PickRequest
    {
        ProtocolVersion = PickingProtocol.Version,
        Raid = breaking.Raid,
        Actor = "a",
        Door = "d",
        Attempt = opened.Attempt,
        Operation = "cancel",
    };
    Check(
        breaking.Process(lateCancel, 0, 1, 0, 5, null).State == null,
        "A late cancellation cannot repeat or undo a break"
    );
}
var depleted = new PickRequest
{
    ProtocolVersion = PickingProtocol.Version,
    Raid = breaking.Raid,
    Actor = "b",
    Door = "other",
    Tool = "t",
    Operation = "start",
};
Check(
    breaking.Process(depleted, 51, 1, 0, 5, null).Error != null,
    "A depleted set stays depleted across actors and doors despite stale peer inventory"
);
var legacy = JsonSerializer.Deserialize<LockPickingData>(
    "{\"Enabled\":true,\"SweetSpotRangeBase\":3.75,\"AttemptsBeforeBreak\":3}"
);
legacy.Validate();
Check(legacy.Tiers.Count == 5, "Legacy config supplies pin defaults");
var invalid = new LockPickingData { Tiers = new() };
try
{
    invalid.Validate();
    throw new Exception("Empty tiers accepted");
}
catch (ArgumentException)
{
    checks++;
}
Check(
    !JsonSerializer.Serialize(start).Contains("Heights")
        && !JsonSerializer.Serialize(start).Contains("Order"),
    "Network snapshots omit explicit solution geometry"
);
var snapshotProbe = new PinLockEngine(def, config, 1, 0);
Solve(snapshotProbe, def, 60);
var snapshotJson = JsonSerializer.Serialize(snapshotProbe.Snapshot());
Check(snapshotJson.Contains("SetPinStates")
    && !snapshotJson.Contains("Heights") && !snapshotJson.Contains("Order")
    && !snapshotJson.Contains("\"Types\"") && !snapshotJson.Contains("Catches"), "Raid snapshot confirms sets without explicit solution arrays");
var observed = snapshotProbe.Snapshot();
var packet = Newtonsoft.Json.JsonConvert.SerializeObject(new PickReply { State = observed });
var received = Newtonsoft.Json.JsonConvert.DeserializeObject<PickReply>(packet).State;
Check(received.Cue == observed.Cue && received.Cues.Length == observed.Cues.Length
    && received.CylinderRotation == observed.CylinderRotation
    && received.PinTypes.SequenceEqual(observed.PinTypes), "Fika JSON preserves sensory feedback");
var reader = new PickCueReader();
Check(reader.Read(received).Length > 0 && reader.Read(received).Length == 0, "Retained mechanical cues are delivered once");
var cueClone = snapshotProbe.Snapshot();
cueClone.Cues[0].Sequence = -1;
Check(snapshotProbe.Snapshot().Cues[0].Sequence > 0, "Snapshot consumers cannot mutate retained cues");

// Balance checks preserve existing default and custom setting tolerances.
var initialTolerances = new[] { .12f, .10f, .085f, .07f, .055f };
for (var tier = 1; tier <= 5; tier++)
{
    var count = config.Tier(tier).Pins;
    var definition = new PinLockDefinition
    {
        Heights = Enumerable.Repeat(.5f, count).ToArray(),
        Order = Enumerable.Range(0, count).ToArray(),
    };
    var edge = .5f - initialTolerances[tier - 1] * .9f;
    var tighter = new PinLockEngine(definition, config, tier, 0);
    Advance(tighter, 0, edge, true, 2);
    Check(tighter.Coaching().SetPins == 0, "The old outer sweet spot no longer auto-sets a pin");
    Advance(tighter, 0, .5f, true, 2);
    Check(tighter.Coaching().SetPins == 1, "The center still sets normally with tighter tolerance");
    var custom = new LockPickingData();
    custom.Tier(tier).Tolerance = initialTolerances[tier - 1];
    var customized = new PinLockEngine(definition, custom, tier, 0);
    Advance(customized, 0, edge, true, 2);
    Check(customized.Coaching().SetPins == 1, "Explicit custom tolerance remains authoritative");
}
ContactAndInputChecks.Run(Check);
SecurityChecks.Run(Check);
CoachingChecks.Run(Check);
AudioChecks.Run(Check);
CutawayChecks.Run(Check, args.FirstOrDefault());
FeedbackChecks.Run(Check, args.FirstOrDefault());
FeedbackTextChecks.Run(Check);
StateCaptionChecks.Run(Check);
PublicControllerChecks.Run(Check, args.FirstOrDefault());
Console.WriteLine($"Lock-picking: {checks} checks passed.");
