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
var config = new LockPickingData
{
    Enabled = true,
    InspectLockXpRatio = .15f,
    FailureLockXpRatio = .25f,
    XpTable = new() { ["1"] = 4 },
};
config.Validate();
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
Check(overset.Snapshot().Feedback == PickFeedback.Overset, "Excess lift oversets the binding pin");
Advance(overset, 0, 0, false, 1);
Check(
    overset.Snapshot().SetPins == 0 && overset.Snapshot().Strain == 0,
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
Check(
    again.State.Wear == worn && worn > 0 && again.State.SetPins == 0,
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
        Check(game.Snapshot().SetPins == 1, "Failure XP test makes real pin progress");
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
    "Network snapshots contain no solution"
);
for (var tier = 1; tier <= 5; tier++)
{
    var definition = PinLockDefinition.Create(config.Tier(tier).Pins, 71);
    var engine = new PinLockEngine(definition, config, tier, 0);
    var empty = engine.Snapshot();
    Check(
        empty.SetPinStates.Length == empty.Pins && empty.SetPinStates.All(p => !p),
        "New attempts expose unset flags"
    );
    var pin = definition.Order[0];
    Advance(engine, (pin + .5f) / empty.Pins, 0, true, 1);
    Advance(engine, (pin + .5f) / empty.Pins, definition.Heights[pin], true, 2);
    var set = engine.Snapshot();
    Check(
        set.SetPinStates[pin] && set.SetPinStates.Count(p => p) == set.SetPins,
        "Flags identify the actual completed pin"
    );
    var json = JsonSerializer.Serialize(set);
    var restored = JsonSerializer.Deserialize<PickSnapshot>(json);
    var packet = Newtonsoft.Json.JsonConvert.SerializeObject(new PickReply { State = set });
    var received = Newtonsoft.Json.JsonConvert.DeserializeObject<PickReply>(packet);
    Check(
        received.State.SetPinStates.SequenceEqual(set.SetPinStates),
        "Fika JSON preserves individual completion flags"
    );
    Check(
        restored.SetPinStates.SequenceEqual(set.SetPinStates),
        "Completed flags survive JSON transport"
    );
    Check(
        !json.Contains("Heights") && !json.Contains("Order") && !json.Contains("Tolerance"),
        "Cutaway state contains no secret solution"
    );
    set.SetPinStates[pin] = false;
    Check(engine.Snapshot().SetPinStates[pin], "Snapshot consumers cannot mutate engine flags");
    var retained = engine.Snapshot();
    Advance(engine, 0, 0, false, 1);
    Check(
        engine.Snapshot().SetPinStates.All(p => !p),
        "Tension release clears every completed flag"
    );
    Check(
        retained.SetPinStates[pin] && empty.SetPinStates.All(p => !p),
        "Earlier snapshots retain their own flags"
    );
    Solve(engine, definition, 60);
    Check(engine.Snapshot().SetPinStates.All(p => p), "Unlock exposes all completed pins");
    Check(
        new PinLockEngine(definition, config, tier, 0).Snapshot().SetPinStates.All(p => !p),
        "Practice retry resets completion flags"
    );
}
Check(
    JsonSerializer.Deserialize<PickSnapshot>("{\"Pins\":3,\"SetPins\":1}").SetPinStates == null,
    "Older snapshots leave individual pin state unknown"
);

// Balance checks exercise actual setting behavior, including interrupted dwell time.
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
    Check(tighter.Snapshot().SetPins == 0, "The old outer sweet spot no longer auto-sets a pin");
    Advance(tighter, 0, .5f, true, 2);
    Check(tighter.Snapshot().SetPins == 1, "The center still sets normally with tighter tolerance");
    var custom = new LockPickingData();
    custom.Tier(tier).Tolerance = initialTolerances[tier - 1];
    var customized = new PinLockEngine(definition, custom, tier, 0);
    Advance(customized, 0, edge, true, 2);
    Check(customized.Snapshot().SetPins == 1, "Explicit custom tolerance remains authoritative");
}
var timedDefinition = new PinLockDefinition { Heights = [.5f, .5f, .5f], Order = [0, 1, 2] };
foreach (var fps in new[] { 30, 60, 144 })
{
    var timed = new PinLockEngine(timedDefinition, config, 1, 0);
    Advance(timed, 0, .5f, false, 1, fps);
    Advance(timed, 0, .5f, true, .24f, fps);
    Check(
        timed.Snapshot().SetPins == 0,
        "The former 0.20-second hold is insufficient at every frame rate"
    );
    Advance(timed, 0, .5f, true, .1f, fps);
    Check(
        timed.Snapshot().SetPins == 1,
        "A continuous 0.30-second hold still sets at every frame rate"
    );
}
var interruptedHold = new PinLockEngine(timedDefinition, config, 1, 0);
Advance(interruptedHold, 0, .5f, false, 1);
for (var step = 0; step < 14; step++)
    interruptedHold.Advance(PinLockEngine.StepSeconds, 0, .5f, true);
for (var step = 0; step < 10; step++)
    interruptedHold.Advance(PinLockEngine.StepSeconds, 0, 0, true);
Check(
    interruptedHold.Snapshot().SetPins == 0,
    "Leaving the window before the hold finishes does not set"
);
for (var step = 0; step < 120 && interruptedHold.Snapshot().Feedback != PickFeedback.Ready; step++)
    interruptedHold.Advance(PinLockEngine.StepSeconds, 0, .5f, true);
Check(
    interruptedHold.Snapshot().Feedback == PickFeedback.Ready,
    "Returning to the window restores ready feedback"
);
for (var step = 0; step < 13; step++)
    interruptedHold.Advance(PinLockEngine.StepSeconds, 0, .5f, true);
Check(
    interruptedHold.Snapshot().SetPins == 0,
    "Separate visits cannot accumulate partial hold time"
);
interruptedHold.Advance(PinLockEngine.StepSeconds, 0, .5f, false);
for (var step = 0; step < 14; step++)
    interruptedHold.Advance(PinLockEngine.StepSeconds, 0, .5f, true);
Check(interruptedHold.Snapshot().SetPins == 0, "Releasing tension also resets partial hold time");
for (var step = 0; step < 6; step++)
    interruptedHold.Advance(PinLockEngine.StepSeconds, 0, .5f, true);
Check(
    interruptedHold.Snapshot().SetPins == 1,
    "The pin remains recoverable after interrupted holds"
);
CutawayChecks.Run(Check, args.FirstOrDefault());
Console.WriteLine($"Lock-picking: {checks} checks passed.");
