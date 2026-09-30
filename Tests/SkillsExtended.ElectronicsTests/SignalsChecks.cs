using System.Reflection;
using System.Text.Json.Nodes;
using SkillsExtended.Config;
using SkillsExtended.Config.Skills;
using SkillsExtended.Core;
using SkillsExtended.Core.Editing;
using SkillsExtended.Models;
using SkillsExtended.ServerSignals;
using SkillsExtended.Signals;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using Path = System.IO.Path;

internal static class SignalsChecks
{
    public static void Verify()
    {
        var checks = 0;
        void Check(bool condition, string label)
        {
            checks++;
            if (!condition)
                throw new Exception("Signals: " + label);
        }
        var c = new SignalsIntelligenceData();
        c.Validate();
        foreach (var map in new[] { "woods", "Woods", "WOODS", "bigmap", "Bigmap", "BIGMAP" })
        {
            Check(SignalsMaps.IsSupported(map), "native and configured map casing: " + map);
            Check(
                SignalsMaps.Same(map, map.ToLowerInvariant()),
                "host, peer and authoring map match: " + map
            );
        }
        Check(
            !SignalsMaps.IsSupported(null)
                && !SignalsMaps.IsSupported("factory4_day")
                && !SignalsMaps.Same(null, null),
            "missing and unsupported map IDs stay excluded"
        );
        Check(c.MinimumSeparation == 125, "default bearing separation is 125 metres");
        Check(
            c.Placements.Count == 24 && c.Placements.GroupBy(p => p.Map).All(g => g.Count() == 12),
            "24 unique map candidates"
        );
        foreach (var level in new[] { 0, 25, 51 })
        {
            var expected =
                level == 0 ? 12
                : level == 25 ? 8
                : 4;
            Check(SignalsModel.Uncertainty(c, level) == expected, "level precision");
            Check(
                Math.Abs(
                    SignalsModel.Tolerance(c, level) - .2f * (1 + .5f * Math.Min(level, 50) / 50f)
                ) < .0001,
                "level tuning"
            );
        }
        var invalid = new SignalsIntelligenceData { ReadingSeconds = float.NaN };
        try
        {
            invalid.Validate();
            throw new Exception("NaN accepted");
        }
        catch (ArgumentException)
        {
            checks++;
        }
        var a = new SignalManifest
        {
            Raid = "one",
            Seed = 42,
            Config = c,
            Frequency = 96,
            Placement = new()
            {
                Position = new() { X = 100, Z = 100 },
            },
        };
        var at = new SignalPoint();
        var observed = SignalsModel.ObservedBearing(a, at, 0);
        Check(
            SignalsModel
                .ScanAlignmentHint(a, at, 0, a.Frequency + 1, observed)
                .Contains("FREQUENCY"),
            "frequency misalignment names the visible frequency control"
        );
        Check(
            SignalsModel.ScanAlignmentHint(a, at, 0, a.Frequency, observed + 6).Contains("BEARING"),
            "bearing misalignment names the visible bearing control instead of phase"
        );
        Check(
            SignalsModel.ScanAlignmentHint(a, at, 0, a.Frequency, observed + 360) == null,
            "aligned bearing wraps through north"
        );
        Check(
            SignalsModel.ScanAlignmentHint(a, at, 0, a.Frequency, observed + 4.99f) == null,
            "scan hint matches the authority's five degree hold tolerance"
        );
        var zone = new SignalSnapshot
        {
            Ready = true,
            HasFix = true,
            Estimate = a.Placement.Position,
            Radius = 100,
        };
        var edgeInterval = SignalsModel.ProximityInterval(a, zone, new() { X = 200, Z = 100 });
        var middleInterval = SignalsModel.ProximityInterval(a, zone, new() { X = 150, Z = 100 });
        var caseInterval = SignalsModel.ProximityInterval(a, zone, a.Placement.Position);
        var listener = new SignalPoint { X = 100, Z = 90 };
        var towardPitch = SignalsModel.ProximityPitch(listener, a.Placement.Position, 0);
        var sidePitch = SignalsModel.ProximityPitch(listener, a.Placement.Position, 90);
        var awayPitch = SignalsModel.ProximityPitch(listener, a.Placement.Position, 180);
        Check(
            Math.Abs(towardPitch - 1.6f) < .001f
                && Math.Abs(sidePitch - 1.2f) < .001f
                && Math.Abs(awayPitch - .8f) < .001f,
            "pitch rises from behind through side to ahead"
        );
        Check(
            SignalsModel.ProximityPitch(listener, a.Placement.Position, -90) == sidePitch
                && SignalsModel.ProximityPitch(listener, a.Placement.Position, 360) == towardPitch,
            "direction cue is symmetric and wraps north"
        );
        Check(
            SignalsModel.ProximityPitch(new() { X = 90, Z = 100 }, a.Placement.Position, 90)
                == towardPitch,
            "east uses Unity player yaw convention"
        );
        Check(
            SignalsModel.ProximityPitch(
                new()
                {
                    X = 100,
                    Y = 50,
                    Z = 90,
                },
                a.Placement.Position,
                0
            ) == towardPitch,
            "heading pitch ignores height differences"
        );
        Check(
            SignalsModel.ProximityPitch(new() { X = 100, Z = 99.5f }, a.Placement.Position, 180)
                > awayPitch
                && SignalsModel.ProximityPitch(a.Placement.Position, a.Placement.Position, 180)
                    == towardPitch,
            "arrival tone stays stable when crossing the cache"
        );
        Check(
            SignalsModel.ProximityPitch(null, a.Placement.Position, 0) == 1
                && SignalsModel.ProximityPitch(listener, null, 0) == 1
                && SignalsModel.ProximityPitch(listener, a.Placement.Position, float.NaN) == 1,
            "invalid heading inputs return neutral pitch"
        );
        var lastPitch = towardPitch;
        for (var yaw = 1; yaw <= 180; yaw++)
        {
            var pitch = SignalsModel.ProximityPitch(listener, a.Placement.Position, yaw);
            Check(
                pitch <= lastPitch && pitch >= .7999f && pitch <= 1.6001f,
                "pitch changes smoothly with heading"
            );
            lastPitch = pitch;
        }
        Check(
            Math.Abs(edgeInterval - 1.5f) < .001f
                && Math.Abs(caseInterval - .18f) < .001f
                && edgeInterval > middleInterval
                && middleInterval > caseInterval,
            "proximity cadence gets faster from zone edge to cache"
        );
        Check(
            SignalsModel.ProximityInterval(a, zone, new() { X = 200.01f, Z = 100 }) == 0,
            "outside the plotted zone is silent"
        );
        zone.HasFix = false;
        Check(
            SignalsModel.ProximityInterval(a, zone, a.Placement.Position) == 0,
            "no proximity cue before fix"
        );
        zone.HasFix = true;
        zone.Unlocked = true;
        Check(
            SignalsModel.ProximityInterval(a, zone, a.Placement.Position) == 0,
            "unlock silences proximity cue"
        );
        zone.Unlocked = false;
        zone.Ready = false;
        Check(
            SignalsModel.ProximityInterval(a, zone, a.Placement.Position) == 0,
            "failed or pending case is silent"
        );
        zone.Ready = true;
        c.Enabled = false;
        Check(
            SignalsModel.ProximityInterval(a, zone, a.Placement.Position) == 0,
            "disabled feature is silent"
        );
        c.Enabled = true;
        a.Error = "placement failed";
        Check(
            SignalsModel.ProximityInterval(a, zone, a.Placement.Position) == 0,
            "case errors silence cue"
        );
        a.Error = null;
        zone.Estimate = new() { X = 140, Z = 100 };
        Check(
            SignalsModel.ProximityInterval(a, zone, new() { X = 180, Z = 100 })
                > SignalsModel.ProximityInterval(a, zone, a.Placement.Position),
            "cadence follows real cache distance rather than estimated center"
        );
        Check(SignalsModel.Noise(42, at) == SignalsModel.Noise(42, at), "repeatable noise");
        Check(
            SignalsModel.Strength(a, at, 0, 96, SignalsModel.ObservedBearing(a, at, 0)) > 0,
            "detectable at origin"
        );
        Check(SignalsModel.Strength(a, at, 0, 90, 0) == 0, "wrong frequency rejects reception");
        Check(
            !SignalsModel.Intersect(
                new() { Position = at, Bearing = 45 },
                new()
                {
                    Position = new() { X = 1 },
                    Bearing = 315,
                },
                40,
                out _,
                out _
            ),
            "baseline required"
        );
        Check(
            !SignalsModel.Intersect(
                new() { Position = at, Bearing = 0 },
                new()
                {
                    Position = new() { X = 100 },
                    Bearing = 1,
                },
                40,
                out _,
                out _
            ),
            "parallel readings rejected"
        );
        Check(
            SignalsModel.Intersect(
                new()
                {
                    Position = at,
                    Bearing = 45,
                    Uncertainty = 4,
                },
                new()
                {
                    Position = new() { X = 200 },
                    Bearing = 315,
                    Uncertainty = 4,
                },
                40,
                out var fix,
                out var radius
            )
                && Math.Abs(fix.X - 100) < .01
                && Math.Abs(fix.Z - 100) < .01
                && radius >= 12,
            "crossing geometry"
        );
        var authority = new SignalsAuthority(a);
        authority.Ready();
        var sequence = 0;
        double now = 0;
        void Request(
            string op,
            string actor,
            SignalPoint position,
            float frequency,
            float bearing,
            float phase,
            string error = null
        )
        {
            now += .1;
            authority.Process(
                new()
                {
                    Raid = "one",
                    Actor = actor,
                    Sequence = ++sequence,
                    Operation = op,
                    Frequency = frequency,
                    Bearing = bearing,
                    Phase = phase,
                },
                position,
                25,
                now,
                error,
                new[] { "a", "b" }
            );
        }
        void Scan(string actor, SignalPoint position)
        {
            for (var i = 0; i < 33; i++)
                Request(
                    "scan",
                    actor,
                    position,
                    96,
                    SignalsModel.ObservedBearing(a, position, 25),
                    0
                );
        }
        Scan("a", at);
        Check(
            authority.State.Readings.Count == 1 && authority.State.EarnedXp["a"] == 3,
            "first reading and XP"
        );
        Scan("a", at);
        Check(
            authority.State.Readings.Count == 1 && authority.State.EarnedXp["a"] == 3,
            "same position no reroll or XP"
        );
        Scan("a", new() { X = 124.99f });
        Check(
            authority.State.Readings.Count == 1
                && authority.State.EarnedXp["a"] == 3
                && authority.State.Message.Contains("125"),
            "under 125 metres rejects bearing and XP with useful feedback"
        );
        Scan("b", new() { X = 200 });
        Check(
            authority.State.HasFix
                && authority.State.Readings.Count == 2
                && authority.State.AccessCode == "000042",
            "cooperative triangulation"
        );
        Scan("a", new() { X = -125 });
        Check(
            authority.State.Readings.Count == 3 && authority.State.EarnedXp["a"] == 6,
            "exactly 125 metres accepts the next bearing"
        );
        Scan("a", new() { X = -250 });
        Check(
            authority.State.EarnedXp["a"] == 6 && authority.State.Readings.Count == 4,
            "bearing cap and plot memory"
        );
        Scan("a", new() { X = -375 });
        Scan("b", new() { X = -500 });
        Check(
            authority.State.Readings.Count == 6
                && SignalsModel.PlottedReadings(authority.State, 0).Count() == 4
                && SignalsModel.PlottedReadings(authority.State, 25).Count() == 4
                && SignalsModel.PlottedReadings(authority.State, 51).Count() == 6,
            "mixed-level receivers retain their own plot allowance"
        );
        Scan("a", new() { X = 1 });
        Check(
            authority.State.Readings.Count == 6 && authority.State.EarnedXp["a"] == 6,
            "separation checks older readings as well as the latest one"
        );
        var target = a.Placement.Position;
        Request("pair", "a", target, 96, 0, SignalsModel.PairPhase(a.Seed, now + .1));
        Check(authority.State.PairingActor == "a", "pairing reservation");
        Request("pair", "b", target, 96, 0, SignalsModel.PairPhase(a.Seed, now + .1));
        Check(authority.State.PairingActor == "a", "second player cannot steal reservation");
        authority.Cancel("a");
        Check(authority.State.PairingActor == null, "disconnect releases reservation");
        for (var i = 0; i < 30; i++)
            Request("pair", "b", target, 96, 0, SignalsModel.PairPhase(a.Seed, now + .1));
        Request("pair", "b", target, 96, 0, SignalsModel.PairPhase(a.Seed, now + .1) + 90);
        for (var i = 0; i < 25; i++)
            Request("pair", "b", target, 96, 0, SignalsModel.PairPhase(a.Seed, now + .1));
        Check(!authority.State.Unlocked, "misalignment resets progress");
        for (var i = 0; i < 35; i++)
            Request("pair", "b", target, 96, 0, SignalsModel.PairPhase(a.Seed, now + .1));
        Check(
            authority.State.Unlocked
                && authority.State.EarnedXp["a"] == 18
                && authority.State.EarnedXp["b"] == 18,
            "contributors receive completion once"
        );
        for (var i = 0; i < 60; i++)
            Request("pair", "b", target, 96, 0, SignalsModel.PairPhase(a.Seed, now + .1));
        Check(authority.State.EarnedXp["b"] == 18, "unlocked replay inert");
        var another = new SignalsAuthority(a);
        another.Ready();
        another.Process(
            new()
            {
                Raid = "wrong",
                Actor = "x",
                Sequence = 1,
                Operation = "scan",
            },
            at,
            0,
            1,
            null,
            new[] { "x" }
        );
        Check(another.State.Readings.Count == 0, "wrong raid rejected");
        another.Process(
            new()
            {
                Raid = "one",
                Actor = "x",
                Sequence = 1,
                Operation = "pair",
            },
            target,
            0,
            1,
            null,
            new[] { "x" }
        );
        Check(another.State.PairingActor == null, "accidental discovery cannot bypass fix");
        var timed = new SignalsAuthority(a);
        timed.Ready();
        for (var i = 1; i < 100; i++)
            timed.Process(
                new()
                {
                    Raid = "one",
                    Actor = "x",
                    Sequence = i,
                    Operation = "scan",
                    Frequency = 96,
                    Bearing = SignalsModel.ObservedBearing(a, at, 0),
                },
                at,
                0,
                0,
                null,
                new[] { "x" }
            );
        Check(timed.State.Readings.Count == 0, "packet spam cannot advance host time");
        timed.Process(
            new()
            {
                Raid = "one",
                Actor = "x",
                Sequence = 100,
                Operation = "scan",
                Frequency = 96,
                Bearing = SignalsModel.ObservedBearing(a, at, 0),
            },
            at,
            0,
            .1,
            "Carry a Modified PDA.",
            new[] { "x" }
        );
        Check(timed.State.Readings.Count == 0, "missing PDA interrupts operation");
        var before = timed.State.Revision;
        timed.Process(
            new()
            {
                Raid = "one",
                Actor = "x",
                Sequence = 100,
                Operation = "scan",
                Frequency = 96,
            },
            at,
            0,
            20,
            null,
            new[] { "x" }
        );
        Check(timed.State.Revision == before, "duplicate sequence is inert");

        var root = Environment.GetEnvironmentVariable("SKILLS_EFT_ROOT") ?? @"F:\SPT 4.1.x";
        var json = new JsonUtil([new SptJsonConverterRegistrator()]);
        var db = Path.Combine(root, "SPT_Runtime/SPT_Data/database/templates");
        var templates = Activator.CreateInstance<TemplateTable>() with
        {
            Items = json.Deserialize<Dictionary<MongoId, TemplateItem>>(
                File.ReadAllText(Path.Combine(db, "items.json"))
            )!,
            Handbook = json.Deserialize<HandbookBase>(
                File.ReadAllText(Path.Combine(db, "handbook.json"))
            )!,
        };
        var config = new ConfigController(null, []);
        var snapshot = new ConfigSnapshot(
            new SkillsConfig(),
            new ServerConfig { CheckForUpdates = false },
            "test"
        );
        typeof(ConfigController)
            .GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(config, snapshot);
        var service = new SignalsRaidService(config, templates, json);
        foreach (var map in new[] { "Woods", "Bigmap" })
        {
            service.Start("case-fixture", map, map, true);
            var mapManifest = service.Get("case-fixture");
            Check(
                mapManifest.Error == null
                    && mapManifest.PlacementCandidates.Count == 12
                    && mapManifest.PlacementCandidates.All(p => p.Map == map.ToLowerInvariant()),
                "raid creation accepts native map ID " + map
            );
        }
        service.Start("snapshot", "frozen", "woods", true);
        var frozen = service.Get("snapshot");
        Check(
            frozen.Placement == null
                && !frozen.PlacementResolved
                && frozen.PlacementCandidates.Count == 12
                && frozen.PlacementCandidates.All(p =>
                    p.Map == "woods" && p.Enabled && p.SearchRadius == 10
                ),
            "server freezes all enabled map areas for host resolution"
        );
        var frozenLocation = frozen.PlacementCandidates[0];
        var originalLocation = snapshot.Skills.SignalsIntelligence.Placements.Single(p =>
            p.Id == frozenLocation.Id
        );
        var originalX = originalLocation.Position.X;
        originalLocation.Position.X += 5;
        originalLocation.SearchRadius = 0;
        Check(
            frozenLocation.Position.X == originalX && frozenLocation.SearchRadius == 10,
            "raid placement catalog is isolated from edits"
        );
        originalLocation.Position.X = originalX;
        originalLocation.SearchRadius = 10;
        snapshot.Skills.SignalsIntelligence.BearingXp = 9;
        service.Start("snapshot", "frozen", "woods", true);
        Check(
            frozen.Config.BearingXp == 3 && ReferenceEquals(frozen, service.Get("snapshot")),
            "current raid keeps frozen rules"
        );
        service.Start("snapshot", "next", "woods", true);
        Check(service.Get("snapshot").Config.BearingXp == 9, "next raid uses edited rules");
        snapshot.Skills.SignalsIntelligence.BearingXp = 3;
        var prices = templates.Handbook.Items!.ToDictionary(
            i => i.Id.ToString(),
            i => Convert.ToDouble(i.Price)
        );
        for (var i = 0; i < 120; i++)
        {
            var raid = "fixture-" + i;
            service.Start("profile", raid, i % 2 == 0 ? "bigmap" : "woods", true);
            var manifest = service.Get("profile");
            Check(manifest.Error == null, "real database reward generation: " + manifest.Error);
            Check(
                manifest
                    .PlacementCandidates.Select(p => p.Id)
                    .SequenceEqual(
                        SignalPlacementSearch
                            .Order(manifest.PlacementCandidates, manifest.Seed)
                            .Select(p => p.Id)
                    ),
                "server candidate order derives from frozen raid seed"
            );
            var rewardBeforePlacement = manifest.ItemsJson;
            var idsBeforePlacement = (
                manifest.ContainerId,
                manifest.RootId,
                manifest.Seed,
                manifest.Frequency
            );
            manifest.Placement = SignalPlacementSearch.Copy(manifest.PlacementCandidates.Last());
            manifest.PlacementResolved = true;
            Check(
                manifest.ItemsJson == rewardBeforePlacement
                    && (manifest.ContainerId, manifest.RootId, manifest.Seed, manifest.Frequency)
                        == idsBeforePlacement,
                "choosing fallback placement does not regenerate rewards or identity"
            );
            var items = JsonNode.Parse(manifest.ItemsJson)!.AsArray();
            var value = items.Skip(1).Sum(item => prices[item!["_tpl"]!.GetValue<string>()]);
            Check(value >= 250000 && value <= 500000, "reward value bounds");
            Check(
                items.Select(n => n!["_id"]!.GetValue<string>()).Distinct().Count() == items.Count,
                "unique item IDs"
            );
            var occupied = new HashSet<(int, int)>();
            foreach (var item in items.Skip(1))
            {
                var template = templates.Items[new MongoId(item!["_tpl"]!.GetValue<string>())];
                var x = item["location"]!["x"]!.GetValue<int>();
                var y = item["location"]!["y"]!.GetValue<int>();
                for (var xx = x; xx < x + template.Properties.Width; xx++)
                for (var yy = y; yy < y + template.Properties.Height; yy++)
                    Check(occupied.Add((xx, yy)), "loot grid does not overlap");
            }
            service.Start("profile", raid, "woods", true);
            Check(
                ReferenceEquals(manifest, service.Get("profile")),
                "duplicate raid request reuses identical manifest"
            );
        }
        service.Start("profile", "scav", "woods", false);
        Check(service.Get("profile").Error != null, "scav raid excluded");
        service.Start("profile", "other", "factory4_day", true);
        Check(service.Get("profile").Error != null, "unsupported map excluded");
        snapshot.Skills.SignalsIntelligence.Enabled = false;
        service.Start("profile", "disabled", "woods", true);
        Check(service.Get("profile").Error != null, "disabled feature creates no cache");
        snapshot.Skills.SignalsIntelligence.Enabled = true;
        snapshot.Skills.SignalsIntelligence.Loot =
        [
            new() { Template = "ffffffffffffffffffffffff", Theme = "Missing" },
        ];
        service.Start("profile", "missing-loot", "woods", true);
        Check(service.Get("profile").Error != null, "missing loot templates suppress cache");
        snapshot.Skills.SignalsIntelligence.Placements.Clear();
        service.Start("profile", "no-locations", "woods", true);
        Check(service.Get("profile").Error != null, "empty placements suppress cache");
        Console.WriteLine(
            $"Signals: {checks} simulation, content, reward and lifecycle assertions passed."
        );
    }
}
