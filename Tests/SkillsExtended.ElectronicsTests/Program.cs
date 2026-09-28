using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Mono.Cecil;
using Newtonsoft.Json;
using SkillsExtended.Config;
using SkillsExtended.Config.Skills;
using SkillsExtended.Electronics;
using SkillsExtended.Patches;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Path = System.IO.Path;

var checks = 0;
void Check(bool condition, string name)
{
    checks++;
    if (!condition)
    {
        throw new Exception(name);
    }
}

string Json(object o) => JsonSerializer.Serialize(o);
var config = new ElectronicsData();

// Exercise the production client reader with the installed game's Newtonsoft assembly.
var clientConfig = SkillsExtended.Helpers.ConfigurationJson.Deserialize<SkillsConfig>(
    Json(new SkillsConfig())
);
Console.WriteLine(
    $"Client config: {clientConfig.Electronics.Tiers.Count} tiers, {clientConfig.Electronics.ExcludedKeycards.Count} excluded keycards."
);
Check(
    clientConfig.Electronics.Tiers.Count == 3,
    "client config must replace initialized tiers, not append them"
);
clientConfig.Electronics.Validate();
Check(
    clientConfig.Electronics.ExcludedKeycards.Count == 3,
    "client config must not duplicate exclusions"
);
Check(
    Json(clientConfig) == Json(new SkillsConfig()),
    "complete server configuration survives client roundtrip unchanged"
);
var configured = new SkillsConfig();
configured.Electronics.Tiers[1].SuccessXp = 17;
configured.Electronics.KeycardDifficulties.Clear();
configured.Electronics.ExcludedKeycards.Clear();
var customClient = SkillsExtended.Helpers.ConfigurationJson.Deserialize<SkillsConfig>(
    Json(configured)
);
customClient.Electronics.Validate();
Check(
    customClient.Electronics.Tier(2).SuccessXp == 17,
    "custom tiers survive client loading exactly once"
);
Check(
    customClient.Electronics.KeycardDifficulties.Count == 0
        && customClient.Electronics.ExcludedKeycards.Count == 0,
    "explicit empty overrides replace defaults"
);
var legacyClient = SkillsExtended.Helpers.ConfigurationJson.Deserialize<SkillsConfig>("{}");
legacyClient.Electronics.Validate();
Check(legacyClient.Electronics.Tiers.Count == 3, "missing section retains client defaults");
var partialClient = SkillsExtended.Helpers.ConfigurationJson.Deserialize<SkillsConfig>(
    "{\"Electronics\":{\"Enabled\":false}}"
);
partialClient.Electronics.Validate();
Check(
    !partialClient.Electronics.Enabled && partialClient.Electronics.Tiers.Count == 3,
    "partial sections retain omitted defaults and enabled flag"
);
if (args.Contains("--installed-config"))
{
    var installedPath = Path.GetFullPath(
        "../../SPT_Runtime/user/mods/SkillsExtended/Resources/Configs/SkillsConfig.json"
    );
    var installed = SkillsExtended.Helpers.ConfigurationJson.Deserialize<SkillsConfig>(
        File.ReadAllText(installedPath)
    );
    installed.Electronics.Validate();
    Console.WriteLine("Installed configuration passed client startup validation (read only).");
}

for (var tier = 1; tier <= 3; tier++)
{
    foreach (var level in new[] { 0, 25, 51 })
    {
        for (uint seed = 0; seed < 120; seed++)
        {
            var b = HackingEngine.StartAttempt(config, tier, level, seed);
            Check(
                Json(b) == Json(HackingEngine.StartAttempt(config, tier, level, seed)),
                "deterministic seed"
            );
            Check(
                b.Nodes.Count == config.Tier(tier).Nodes
                    && b.Nodes.Count(n => n.Kind == NodeKind.Core) == 1,
                "board counts"
            );
            Check(
                b.Nodes.Count(n => n.Revealed) == 1 && b.Nodes.Count(n => n.Cleared) == 1,
                "single entrance"
            );
            Check(
                b.Nodes.Count(n => b.CanSelect(n.Id)) >= 2
                    && b.Nodes.Where(n => b.CanSelect(n.Id)).All(n => n.Kind == NodeKind.Empty),
                "safe initial routes"
            );
            Check(
                b.MaximumCoherence == 60 + level
                    && b.Strength == 20 + level / 5
                    && b.Slots == (level == 51 ? 4 : 3),
                "level snapshot"
            );
            var reached = new HashSet<int> { 0 };
            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                foreach (var n in b.Nodes[queue.Dequeue()].Neighbors)
                {
                    if (reached.Add(n))
                    {
                        queue.Enqueue(n);
                    }
                }
            }

            Check(reached.Count == b.Nodes.Count, "connected board");
            Check(
                b.Nodes.All(n => n.Neighbors.All(i => b.Nodes[i].Neighbors.Contains(n.Id))),
                "bidirectional edges"
            );
            Check(
                tier > 1
                    || !b.Nodes.Any(n =>
                        n.Kind == NodeKind.Restoration || n.Kind == NodeKind.Suppressor
                    ),
                "standard defenses"
            );
            Check(tier > 2 || !b.Nodes.Any(n => n.Kind == NodeKind.Suppressor), "secure defenses");
            Check(
                b.Nodes.Where(n => n.IsUtility)
                    .All(n =>
                        tier == 3
                        || (tier == 2 ? n.Kind != NodeKind.Vector : n.Kind == NodeKind.SelfRepair)
                    ),
                "utility availability"
            );
            var before = Json(b);
            Check(
                !b.SelectNode(-1) && !b.UseUtility(99) && Json(b) == before,
                "invalid actions are inert"
            );
            // Independent all-pairs oracle; clues must remain visible and follow remaining targets.
            var distances = new int[b.Nodes.Count, b.Nodes.Count];
            for (var i = 0; i < b.Nodes.Count; i++)
            {
                for (var j = 0; j < b.Nodes.Count; j++)
                {
                    distances[i, j] =
                        i == j ? 0
                        : b.Nodes[i].Neighbors.Contains(j) ? 1
                        : 1000;
                }
            }

            for (var k = 0; k < b.Nodes.Count; k++)
            {
                for (var i = 0; i < b.Nodes.Count; i++)
                {
                    for (var j = 0; j < b.Nodes.Count; j++)
                    {
                        distances[i, j] = Math.Min(
                            distances[i, j],
                            distances[i, k] + distances[k, j]
                        );
                    }
                }
            }

            var entrance = b.Nodes.Single(n => n.Revealed).Id;
            for (var step = 0; step < 150 && b.Status == HackStatus.Active; step++)
            {
                var legal = b.Nodes.Where(n => b.CanSelect(n.Id)).ToArray();
                var n =
                    legal.FirstOrDefault(n => !n.Revealed)
                    ?? legal.FirstOrDefault(n =>
                        n.Kind == NodeKind.Cache || n.IsUtility && b.Utilities.Count < b.Slots
                    )
                    ?? legal.FirstOrDefault(n => n.IsDefense || n.Kind == NodeKind.Core);
                if (n == null)
                {
                    break;
                }

                b.SelectNode(n.Id);
                if (b.Status == HackStatus.Active)
                {
                    var targets = b
                        .Nodes.Where(t =>
                            !t.Cleared
                            && (t.Kind == NodeKind.Core || t.IsUtility || t.Kind == NodeKind.Cache)
                        )
                        .ToArray();
                    foreach (
                        var clue in b.Nodes.Where(t =>
                            t.Id != entrance && t.Revealed && t.Kind == NodeKind.Empty
                        )
                    )
                    {
                        Check(
                            clue.Clue == Math.Min(5, targets.Min(t => distances[clue.Id, t.Id])),
                            "visible clue matches current remaining targets"
                        );
                    }

                    Check(b.Nodes[entrance].Clue == 0, "entrance marker preserved");
                }

                Check(b.Coherence >= 0 && b.Coherence <= b.MaximumCoherence, "coherence bounds");
            }
        }
    }
}

Console.WriteLine("Seed/graph/progression sweep passed: 1,080 boards.");
HackBoard Fixture(params NodeKind[] kinds)
{
    var b = new HackBoard
    {
        Coherence = 60,
        MaximumCoherence = 60,
        BaseStrength = 20,
        Slots = 3,
    };
    b.Nodes.Add(
        new HackNode
        {
            Id = 0,
            Revealed = true,
            Cleared = true,
        }
    );
    foreach (var kind in kinds)
    {
        var n = new HackNode
        {
            Id = b.Nodes.Count,
            Kind = kind,
            Revealed = true,
            Neighbors = new() { 0 },
        };
        HackingEngine.SetStats(n);
        if (kind == NodeKind.Core)
        {
            n.Coherence = n.MaximumCoherence = 40;
            n.Strength = 10;
        }

        b.Nodes[0].Neighbors.Add(n.Id);
        b.Nodes.Add(n);
    }

    return b;
}

{
    var b = Fixture(NodeKind.Antivirus);
    b.Nodes[1].Coherence = 20;
    Check(
        b.SelectNode(1) && b.Coherence == 60 && b.Nodes[1].Cleared,
        "dead target cannot retaliate"
    );
    b = Fixture(NodeKind.Antivirus);
    b.Coherence = 20;
    Check(
        b.SelectNode(1) && b.Status == HackStatus.Lost && b.Coherence == 0,
        "exact lethal retaliation"
    );
    b = Fixture(NodeKind.Core, NodeKind.Restoration);
    b.Nodes[1].Coherence = 20;
    b.Coherence = 1;
    b.RepairTurns = 3;
    Check(
        b.SelectNode(1) && b.Status == HackStatus.Won && b.Coherence == 1 && b.RepairTurns == 3,
        "victory ends turn processing"
    );
    b = Fixture(NodeKind.Firewall, NodeKind.Restoration);
    b.Nodes[1].Coherence = 55;
    Check(b.SelectNode(1) && b.Nodes[1].Coherence == 45, "restoration repairs after attack");
    b.Nodes[1].Coherence = 59;
    b.Utilities.Add(NodeKind.Shield);
    b.UseUtility(0);
    Check(b.Nodes[1].Coherence == 60, "restoration cap");
    b = Fixture(NodeKind.Suppressor, NodeKind.Suppressor, NodeKind.Suppressor, NodeKind.Suppressor);
    Check(b.Strength == 5, "suppression floor");
    b.Nodes[1].Cleared = b.Nodes[2].Cleared = b.Nodes[3].Cleared = true;
    Check(b.Strength == 15, "suppression removed on destruction");
    b = Fixture(NodeKind.Firewall);
    b.Utilities.Add(NodeKind.Shield);
    b.UseUtility(0);
    b.SelectNode(1);
    b.SelectNode(1);
    Check(b.Coherence == 60 && b.ShieldCharges == 0, "shield two retaliations");
    b = Fixture(NodeKind.Firewall);
    b.Coherence = 30;
    b.Utilities.Add(NodeKind.SelfRepair);
    b.UseUtility(0);
    Check(b.Coherence == 38 && b.RepairTurns == 2, "repair activation tick");
    b.Utilities.Add(NodeKind.Shield);
    b.UseUtility(0);
    b.Utilities.Add(NodeKind.KernelRot);
    b.UseUtility(0, 1);
    Check(b.Coherence == 54 && b.RepairTurns == 0, "three repair ticks");
    b = Fixture(NodeKind.Core);
    b.Nodes[1].Coherence = 3;
    b.Utilities.Add(NodeKind.KernelRot);
    b.UseUtility(0, 1);
    Check(b.Nodes[1].Coherence == 1 && b.Coherence == 60, "rot halves without retaliation");
    b = Fixture(NodeKind.Firewall);
    b.Utilities.Add(NodeKind.Vector);
    b.UseUtility(0, 1);
    Check(b.Nodes[1].Coherence == 40 && b.Nodes[1].VectorTurns == 2, "vector first tick");
    b.Utilities.Add(NodeKind.Shield);
    b.UseUtility(0);
    b.Utilities.Add(NodeKind.SelfRepair);
    b.UseUtility(0);
    Check(b.Nodes[1].Cleared, "vector full damage over three actions");
    b = Fixture(NodeKind.SelfRepair);
    b.Utilities.AddRange(new[] { NodeKind.Shield, NodeKind.KernelRot, NodeKind.Vector });
    var before = Json(b);
    Check(!b.SelectNode(1) && before == Json(b), "full slots do not consume turn or utility");
    b = Fixture(NodeKind.Firewall, NodeKind.Empty);
    b.Nodes[2].Revealed = false;
    b.Nodes[2].Neighbors.Add(1);
    b.Nodes[1].Neighbors.Add(2);
    Check(!b.CanSelect(2) && b.CanSelect(1), "defense blocks neighboring hidden node");
    b.Nodes[1].Coherence = 1;
    b.SelectNode(1);
    Check(b.CanSelect(2), "destroying defense restores route");
    b = Fixture(NodeKind.Cache);
    b.Nodes[1].CacheContent = NodeKind.Antivirus;
    b.SelectNode(1);
    Check(
        b.Nodes[1].Kind == NodeKind.Antivirus && b.Nodes[1].Coherence == 30 && b.Coherence == 60,
        "fixed cache content reveals without attack"
    );
    b = Fixture(NodeKind.Empty, NodeKind.Core);
    b.Nodes[1].Revealed = false;
    b.Nodes[1].Neighbors.Add(2);
    b.Nodes[2].Neighbors.Add(1);
    b.SelectNode(1);
    Check(b.Nodes[1].Clue == 1, "exact nearest useful distance");
    Check(b.Abort() && !b.Abort() && !b.SelectNode(2), "abort is terminal/idempotent");
}

Console.WriteLine("Combat, utilities, clues and terminal boundaries passed.");
{
    // A clue has a one-link utility/cache and a core two links away.
    HackBoard ClueFixture(NodeKind kind)
    {
        var b = Fixture(NodeKind.Empty, kind, NodeKind.Core);
        b.Nodes[1].Revealed = false;
        b.Nodes[1].Neighbors.Add(2);
        b.Nodes[2].Neighbors.Add(1);
        b.Nodes[3].Revealed = false;
        b.SelectNode(1);
        Check(b.Nodes[1].Clue == 1, "fixture initially points to adjacent useful target");
        return b;
    }

    var b = ClueFixture(NodeKind.SelfRepair);
    b.SelectNode(2);
    Check(b.Nodes[1].Clue == 2, "collected utility no longer leaves stale one");
    Check(
        JsonSerializer.Deserialize<HackBoard>(Json(b)).Nodes[1].Clue == 2,
        "refreshed clue serialized for remote client"
    );
    b = ClueFixture(NodeKind.Cache);
    b.Nodes[2].CacheContent = NodeKind.Antivirus;
    b.SelectNode(2);
    Check(b.Nodes[1].Clue == 2, "cache turned defense no longer counted");
    b = ClueFixture(NodeKind.Cache);
    b.Nodes[2].CacheContent = NodeKind.SelfRepair;
    b.SelectNode(2);
    Check(b.Nodes[1].Clue == 1, "cache turned utility remains a valid target");
    b.SelectNode(2);
    Check(b.Nodes[1].Clue == 2, "cache utility collection refreshes clue");
    b = ClueFixture(NodeKind.SelfRepair);
    b.Utilities.AddRange(new[] { NodeKind.Shield, NodeKind.Shield, NodeKind.Shield });
    Check(!b.SelectNode(2) && b.Nodes[1].Clue == 1, "rejected collection retains valid clue");
}

Console.WriteLine("Persistent clues refresh after utility collection and cache opening.");
{
    var observer = new HackingPresentation();
    var b = Fixture(NodeKind.Firewall, NodeKind.Empty);
    b.Nodes[1].Revealed = b.Nodes[2].Revealed = false;
    b.Nodes[1].Neighbors.Add(2);
    b.Nodes[2].Neighbors.Add(1);
    Check(observer.Observe(b).Nodes.Count == 0, "initial snapshot does not replay actions");
    b.SelectNode(1);
    var unchanged = Json(b);
    var changes = observer.Observe(b);
    Check(Json(b) == unchanged, "presentation never mutates authority");
    Check(
        changes.Nodes.Any(n => n.Node == 1 && n.Motion == NodeMotion.Reveal),
        "mutable local board reveal observed"
    );
    Check(
        changes.Nodes.Any(n => n.Node == 2 && n.Motion == NodeMotion.Blocked),
        "defense blocks route animation"
    );
    b.SelectNode(1);
    Check(observer.Observe(b).Nodes.Any(n => n.Motion == NodeMotion.Damage), "damage transition");
    b.SelectNode(1);
    observer.Observe(b);
    b.SelectNode(1);
    changes = observer.Observe(b);
    Check(
        changes.Nodes.Any(n => n.Node == 1 && n.Motion == NodeMotion.Destroy),
        "defense destruction transition"
    );
    Check(
        changes.Nodes.Any(n => n.Node == 2 && n.Motion == NodeMotion.Available),
        "unblocked frontier transition"
    );
    Check(
        changes.Paths.Any(p => p.From == 1 && p.To == 2 && !p.Claimed),
        "unblocked path travels outward"
    );
    Check(
        changes.Paths.Any(p => p.From == 0 && p.To == 1 && p.Claimed),
        "claimed path travels inward"
    );
    Check(
        observer.Observe(JsonSerializer.Deserialize<HackBoard>(Json(b))).Paths.Count == 0,
        "duplicate serialized snapshot is inert"
    );
    b.SelectNode(-1);
    Check(observer.Observe(b).Nodes.Count == 0, "invalid click creates no transition");
    b.SelectNode(2);
    Check(observer.Observe(b).Nodes.Any(n => n.Motion == NodeMotion.Reveal), "empty node reveal");
    observer.Reset();
    b = Fixture(NodeKind.Cache);
    b.Nodes[1].CacheContent = NodeKind.Antivirus;
    observer.Observe(b);
    b.SelectNode(1);
    Check(
        observer.Observe(b).Nodes.Any(n => n.Motion == NodeMotion.Reveal),
        "cache content flips into view"
    );
    observer.Reset();
    b = Fixture(NodeKind.Core);
    b.Nodes[1].Coherence = 20;
    observer.Observe(b);
    b.SelectNode(1);
    Check(observer.Observe(b).Won && !observer.Observe(b).Won, "victory ripple once");
    observer.Reset();
    b = Fixture(NodeKind.Antivirus);
    b.Coherence = 20;
    observer.Observe(b);
    b.SelectNode(1);
    Check(observer.Observe(b).Lost && !observer.Observe(b).Lost, "failure ripple once");
}

Console.WriteLine("Presentation transitions and replay suppression passed.");
{
    var host = new HackingAuthority(config, 42);
    HackReply Start(string actor = "p", string door = "d") =>
        host.Process(
            new()
            {
                Raid = host.Raid,
                Actor = actor,
                Door = door,
                Operation = "start",
            },
            0,
            2,
            null
        );
    HackRequest Action(HackReply r, string operation) =>
        new()
        {
            Raid = host.Raid,
            Actor = r.Actor,
            Door = r.Door,
            Attempt = r.Attempt,
            Sequence = r.Sequence + 1,
            Operation = operation,
        };
    var r = Start();
    Check(
        r.Board != null && Start("other").Error != null && Start("p", "other").Error != null,
        "reservation exclusivity"
    );
    var bad = Action(r, "abort");
    bad.Actor = "spoof";
    Check(host.Process(bad, 0, 2, null).Error != null && host.Active.Count == 1, "actor binding");
    bad = Action(r, "abort");
    bad.Sequence = 2;
    Check(host.Process(bad, 0, 2, null).Error != null, "sequence binding");
    var abort = Action(r, "abort");
    var ended = host.Process(abort, 0, 2, null);
    Check(ended.Xp == 0 && ended.Failures["d"] == 1, "abort consumes one attempt no XP");
    host.Process(abort, 0, 2, null);
    Check(host.Failures["d"] == 1, "duplicate no repeat penalty");
    for (var i = 0; i < 2; i++)
    {
        Start();
        host.End("d", true);
    }

    Check(host.Failures["d"] == 3 && Start().Board == null, "third failure lockout");
    Check(
        host.Process(new() { Actor = "new", Operation = "sync" }, 0, 2, null).Failures["d"] == 3,
        "reconnect snapshot"
    );
    r = Start("p", "cancel");
    host.End("cancel", false);
    Check(!host.Failures.ContainsKey("cancel"), "external unlock no penalty");
    r = Start("p", "init");
    host.Process(Action(r, "ui-error"), 0, 2, null);
    Check(!host.Failures.ContainsKey("init"), "initialization error no penalty");
    r = Start("p", "queued");
    var inFlight = Action(r, "node");
    inFlight.Node = r.Board.Nodes.First(n => r.Board.CanSelect(n.Id)).Id;
    var queuedAbort = Action(r, "abort");
    var progressed = host.Process(inFlight, 0, 2, null);
    var closed = host.Process(queuedAbort, 0, 2, null);
    Check(
        closed.Board.Status == HackStatus.Aborted
            && closed.Sequence == 2
            && host.Failures["queued"] == 1,
        "abort while action is in flight"
    );
    Check(
        closed.Revision > progressed.Revision && host.End("queued", true) == null,
        "terminal revision monotonic and cancellation idempotent"
    );
    for (var i = 0; i < 2; i++)
    {
        r = Start("p", "success");
        var s = host.Active["success"];
        s.Board = Fixture(NodeKind.Core);
        s.Board.Difficulty = 2;
        s.Board.Nodes[1].Coherence = 20;
        var req = Action(r, "node");
        req.Node = 1;
        ended = host.Process(req, 51, 3, null);
        Check(
            ended.Unlock && ended.Xp == (i == 0 ? 15 : 0),
            "once success XP despite relock and midattempt level changes"
        );
        Check(!host.Process(req, 0, 2, null).Unlock, "duplicate cannot unlock twice");
    }

    for (var i = 0; i < 2; i++)
    {
        r = Start("p", "failure");
        var s = host.Active["failure"];
        s.Board = Fixture(NodeKind.Antivirus);
        s.Board.Coherence = 20;
        s.Board.Difficulty = 2;
        s.Board.Explored = 3;
        var req = Action(r, "node");
        req.Node = 1;
        ended = host.Process(req, 0, 2, null);
        Check(ended.Xp == (i == 0 ? 3 : 0), "failure XP once after exploration threshold");
    }

    var fresh = new HackingAuthority(config, 42);
    Check(fresh.Failures.Count == 0 && fresh.Raid != host.Raid, "raid reset");
    Check(
        fresh
            .Process(
                new()
                {
                    Raid = host.Raid,
                    Actor = "p",
                    Door = "d",
                    Operation = "start",
                },
                0,
                2,
                null
            )
            .Error != null,
        "stale raid rejected"
    );
}

Console.WriteLine("Authority reservations, replay rejection, XP and reconnect state passed.");
var profile = new PmcData
{
    Skills = new()
    {
        Common = new List<CommonSkill>
        {
            new() { Id = SkillTypes.Strength, Progress = 123 },
        },
    },
};
ElectronicsProfile.Ensure(profile);
ElectronicsProfile.Ensure(profile);
Check(profile.Skills.Common.Count() == 2, "profile initialized once");
var electronics = profile.Skills.Common.Single(s => (int)s.Id == 200);
electronics.Progress = 321;
ElectronicsProfile.Ensure(profile);
Check(electronics.Progress == 321, "existing progress preserved");
var roundtrip = JsonSerializer.Deserialize<PmcData>(Json(profile));
Check(
    roundtrip.Skills.Common.Single(s => (int)s.Id == 200).Progress == 321
        && roundtrip.Skills.Common.Single(s => s.Id == SkillTypes.Strength).Progress == 123,
    "actual server model roundtrip"
);
var oldConfig = JsonSerializer.Deserialize<SkillsConfig>("{}");
Check(
    oldConfig.Electronics.Enabled && oldConfig.Electronics.Tiers.Count == 3,
    "old configs receive defaults"
);
oldConfig.Electronics.Enabled = false;
ElectronicsProfile.Ensure(roundtrip);
Check(
    roundtrip.Skills.Common.Single(s => (int)s.Id == 200).Progress == 321,
    "disable preserves saved progress"
);
if (args.Contains("--write-config"))
{
    var path = Path.GetFullPath("Server/SkillsExtended.Server/Resources/Configs/SkillsConfig.json");
    var document = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path));
    document["Electronics"] = System.Text.Json.Nodes.JsonNode.Parse(Json(config));
    File.WriteAllText(
        path,
        document.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n"
    );
}

if (args.Contains("--write-preview"))
{
    Directory.CreateDirectory("artifacts/electronics-ui");
    var preview = HackingEngine.StartAttempt(config, 3, 25, 42);
    foreach (var n in preview.Nodes)
    {
        n.Revealed = true;
        if (n.Kind == NodeKind.Empty)
        {
            n.Cleared = true;
            n.Clue = 2;
        }
    }

    preview.Utilities.AddRange(new[] { NodeKind.Vector, NodeKind.Shield, NodeKind.KernelRot });
    File.WriteAllText("artifacts/electronics-ui/preview-board.json", Json(preview));
    var motion = HackingEngine.StartAttempt(config, 3, 25, 42);
    for (var i = 0; i < 7; i++)
    {
        var node = motion.Nodes.FirstOrDefault(n =>
            !n.Revealed && n.Kind == NodeKind.Empty && motion.CanSelect(n.Id)
        );
        if (node == null)
        {
            break;
        }

        motion.SelectNode(node.Id);
    }

    var clicked = motion
        .Nodes.Where(n => !n.Revealed && n.Kind == NodeKind.Empty && motion.CanSelect(n.Id))
        .OrderByDescending(n =>
            n.Neighbors.Count(id => !motion.Nodes[id].Revealed && !motion.CanSelect(id))
        )
        .First()
        .Id;
    var before = JsonSerializer.Deserialize<HackBoard>(Json(motion));
    var observer = new HackingPresentation();
    observer.Observe(motion);
    motion.SelectNode(clicked);
    var changes = observer.Observe(motion);
    Check(
        changes.Paths.Any(p => p.Claimed) && changes.Paths.Any(p => !p.Claimed),
        "motion preview exercises claimed and newly opened paths"
    );
    File.WriteAllText(
        "artifacts/electronics-ui/animation-preview.json",
        Json(
            new
            {
                Before = before,
                After = motion,
                Clicked = clicked,
                BeforeAvailable = before
                    .Nodes.Where(n => before.CanSelect(n.Id))
                    .Select(n => n.Id)
                    .ToArray(),
                AfterAvailable = motion
                    .Nodes.Where(n => motion.CanSelect(n.Id))
                    .Select(n => n.Id)
                    .ToArray(),
                Nodes = changes.Nodes.Select(n => new { n.Node, Motion = (int)n.Motion }).ToArray(),
                Paths = changes
                    .Paths.Select(p => new
                    {
                        p.From,
                        p.To,
                        p.Claimed,
                    })
                    .ToArray(),
            }
        )
    );
}

Console.WriteLine($"PASS: {checks:N0} checks.");
ClientSerialization.Verify();
