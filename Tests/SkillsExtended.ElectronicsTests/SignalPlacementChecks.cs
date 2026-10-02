using System.Numerics;
using System.Text.Json;
using SkillsExtended.Config.Skills;
using SkillsExtended.Signals;

internal static class SignalPlacementChecks
{
    public static void Verify()
    {
        var checks = 0;
        void Check(bool condition, string label)
        {
            if (!condition)
                throw new Exception("Signal placement: " + label);
            checks++;
        }
        var anchor = new SignalPlacement
        {
            Id = "first",
            Map = "woods",
            SearchRadius = 10,
        };
        var geometry = new SignalCaseGeometry
        {
            // Deliberately offset pivot and elongated footprint to catch hard-coded boxes.
            Body = new(new Vector3(.1f, .3f, 0), new Vector3(.4f, .2f, .15f)),
            Opening = new(new Vector3(.1f, .6f, 0), new Vector3(.4f, .5f, .2f)),
            Interaction = new(new Vector3(.1f, .35f, 0), new Vector3(.4f, .15f, .2f)),
            Approaches = [new Vector3(0, 0, -.85f)],
            ViewTarget = new Vector3(0, .4f, 0),
        };
        SignalPlacementReport Resolve(Scene scene, params SignalPlacement[] locations)
        {
            var report = new SignalPlacementReport();
            foreach (
                var attempt in SignalPlacementSearch.Search(
                    locations.Length == 0 ? [anchor] : locations,
                    42,
                    geometry,
                    scene
                ).Take(locations.Length == 0 ? 4 : int.MaxValue)
            )
                report.Add(attempt);
            return report;
        }
        void Reject(Scene scene, SignalPlacementFailure reason, string label)
        {
            var report = Resolve(scene);
            Check(
                report.Placement == null && report.Attempts == 4 && report.Rejections[reason] == 4,
                label
            );
        }

        var valid = Resolve(new());
        Check(
            valid.Attempts == 1 && valid.Placement != null,
            "first valid authored orientation wins"
        );
        Check(
            Math.Abs(valid.Placement.Position.Y + .08f) < .0001f,
            "ground offset accounts for prefab pivot"
        );
        Check(anchor.Position.Y == 0 && anchor.Yaw == 0, "authoring anchor remains unchanged");
        Reject(
            new() { NoNavigation = true },
            SignalPlacementFailure.Navigation,
            "missing navigation"
        );
        Reject(
            new() { Snap = new Vector3(1.01f, 0, 0) },
            SignalPlacementFailure.Navigation,
            "horizontal snap bounded"
        );
        Reject(
            new() { Snap = new Vector3(0, 1.01f, 0) },
            SignalPlacementFailure.Navigation,
            "vertical snap bounded"
        );
        Reject(new() { Floor = p => null }, SignalPlacementFailure.Support, "missing ground");
        // The centre is supported but one or more corners hang over a ledge at every yaw.
        Reject(
            new() { Floor = p => new Vector2(p.X, p.Z).Length() < .2f ? 0 : null },
            SignalPlacementFailure.Support,
            "ledge corners rejected"
        );
        Reject(
            new() { Floor = p => p.X + p.Z },
            SignalPlacementFailure.UnevenGround,
            "uneven footprint"
        );
        Reject(new() { Slope = 26 }, SignalPlacementFailure.Slope, "steep ground");
        Reject(
            new() { Block = _ => SignalPlacementFailure.Solid },
            SignalPlacementFailure.Solid,
            "embedded body"
        );
        Reject(
            new()
            {
                Block = v =>
                    v.Center.Y + v.Extents.Y > .8f
                        ? SignalPlacementFailure.Solid
                        : SignalPlacementFailure.None,
            },
            SignalPlacementFailure.Solid,
            "lid overhead clearance"
        );
        Reject(
            new() { Block = _ => SignalPlacementFailure.Hazard },
            SignalPlacementFailure.Hazard,
            "hazard volume"
        );
        Reject(
            new() { Block = _ => SignalPlacementFailure.Interaction },
            SignalPlacementFailure.Interaction,
            "nearby door or loot"
        );
        Reject(
            new()
            {
                Block = v =>
                    v.Extents.Y > .8f ? SignalPlacementFailure.Solid : SignalPlacementFailure.None,
            },
            SignalPlacementFailure.Solid,
            "standing approach obstructed"
        );
        Reject(
            new() { CanSee = false },
            SignalPlacementFailure.Approach,
            "wall blocks interaction"
        );
        Reject(
            new() { Connected = false },
            SignalPlacementFailure.Route,
            "isolated navigation island"
        );
        var blockedSnap = new Scene { NavigationMap = p => new Vector3(0, p.Y, 0) };
        Reject(
            blockedSnap,
            SignalPlacementFailure.Approach,
            "standing position cannot snap inside future case"
        );

        var rotated = Resolve(
            new() { Floor = p => Math.Abs(p.X) <= .2f || Math.Abs(p.X) >= .7f ? 0 : null }
        );
        Check(
            rotated.Placement?.Yaw == 90 && rotated.Attempts == 2,
            "rotated footprint fits narrow support"
        );
        var exactAnchor = SignalPlacementSearch.Copy(anchor);
        exactAnchor.SearchRadius = 0;
        exactAnchor.Position = new() { X = 11.125f, Y = 8.75f, Z = -19.5f };
        exactAnchor.Yaw = -721.25f;
        var exact = new SignalPlacementReport();
        foreach (var attempt in SignalPlacementSearch.Search([exactAnchor, anchor], 42, null, null))
            exact.Add(attempt);
        Check(
            exact.Attempts == 1 && exact.Rejections.Count == 0
                && exact.Placement?.Position.X == exactAnchor.Position.X
                && exact.Placement.Position.Y == exactAnchor.Position.Y
                && exact.Placement.Position.Z == exactAnchor.Position.Z
                && exact.Placement.Yaw == exactAnchor.Yaw,
            "zero radius preserves exact XYZ and raw yaw without scene or geometry"
        );
        exact.Placement.Position.Y += 10;
        Check(exactAnchor.Position.Y == 8.75f, "exact placement is detached from saved coordinates");
        var mixed = Resolve(new() { NoNavigation = true }, anchor, exactAnchor);
        Check(mixed.Attempts == 133 && mixed.Placement.Position.Y == exactAnchor.Position.Y
            && mixed.Placement.Yaw == exactAnchor.Yaw
            && mixed.Rejections[SignalPlacementFailure.Navigation] == 132,
            "failed search area falls back to unchanged exact transform");
        var area = SignalPlacementSearch.Copy(anchor);
        area.SearchRadius = 10;
        var samples = SignalPlacementSearch.Samples(area, 42).ToArray();
        Check(samples.Length == 33 && samples[0] == Vector3.Zero, "centre plus 32 samples");
        Check(
            samples.Distinct().Count() == 33 && samples.All(p => p.Length() <= 10),
            "distinct samples inside radius"
        );
        Check(
            samples.SequenceEqual(SignalPlacementSearch.Samples(area, 42)),
            "samples repeat for same seed"
        );
        Check(
            !samples.SequenceEqual(SignalPlacementSearch.Samples(area, 43)),
            "seed varies samples"
        );
        Check(
            SignalPlacementSearch.Samples(exactAnchor, 42).Count() == 1,
            "exact mode uses only centre"
        );
        var localFallback = Resolve(
            new()
            {
                Block = v =>
                    Math.Abs(v.Center.X) < .6f && Math.Abs(v.Center.Z) < .6f
                        ? SignalPlacementFailure.Solid
                        : SignalPlacementFailure.None,
            },
            area
        );
        Check(
            localFallback.Placement != null && localFallback.Attempts > 4,
            "blocked centre searches nearby ground"
        );
        Check(
            SignalPoint.Distance(localFallback.Placement.Position, area.Position) <= 10,
            "resolved point stays in area"
        );
        var edge = new Scene
        {
            Snap = new Vector3(.9f, 0, 0),
            Block = _ => SignalPlacementFailure.Solid,
        };
        var edgeReport = Resolve(edge, area);
        Check(
            edgeReport.Rejections.ContainsKey(SignalPlacementFailure.OutsideArea),
            "navigation cannot push candidate outside area"
        );

        var other = new SignalPlacement
        {
            Id = "second",
            Map = "woods",
            SearchRadius = 10,
            Position = new() { X = 20 },
        };
        var fallback = Resolve(
            new()
            {
                Block = v =>
                    v.Center.X < 10 ? SignalPlacementFailure.Solid : SignalPlacementFailure.None,
            },
            anchor,
            other
        );
        Check(
            fallback.Placement?.Id == "second"
                && fallback.Attempts == 133
                && fallback.Locations.Count == 2,
            "exhaust first area then fall back"
        );
        var exhausted = Resolve(new() { NoNavigation = true }, area, other);
        Check(
            exhausted.Placement == null && exhausted.Attempts == 264,
            "all candidates exhausted within attempt limit"
        );
        var empty = new SignalPlacementReport();
        foreach (var attempt in SignalPlacementSearch.Search([], 0, geometry, new Scene()))
            empty.Add(attempt);
        Check(empty.Attempts == 0 && empty.Placement == null, "empty areas fail closed");
        var locations = SignalsDefaults.Placements();
        var order = SignalPlacementSearch.Order(locations, 42);
        Check(
            order
                .Select(p => p.Id)
                .SequenceEqual(
                    SignalPlacementSearch
                        .Order(locations.AsEnumerable().Reverse(), 42)
                        .Select(p => p.Id)
                ),
            "stable ordering independent of input order"
        );
        Check(
            !order
                .Select(p => p.Id)
                .SequenceEqual(SignalPlacementSearch.Order(locations, 43).Select(p => p.Id)),
            "seed changes area order"
        );
        order[0].Position.X += 100;
        Check(
            order[0].Position.X != locations.Single(p => p.Id == order[0].Id).Position.X,
            "ordered candidates are deep copies"
        );

        using var cancelled = new CancellationTokenSource();
        using var search = SignalPlacementSearch
            .Search([area], 42, geometry, new Scene { NoNavigation = true }, cancelled.Token)
            .GetEnumerator();
        Check(search.MoveNext(), "search begins incrementally");
        cancelled.Cancel();
        try
        {
            search.MoveNext();
            throw new Exception("Cancellation ignored");
        }
        catch (OperationCanceledException)
        {
            checks++;
        }
        using var duringAttempt = new CancellationTokenSource();
        var sceneCancel = new Scene
        {
            Block = _ =>
            {
                duringAttempt.Cancel();
                return SignalPlacementFailure.None;
            },
        };
        try
        {
            SignalPlacementSearch
                .Search([anchor], 42, geometry, sceneCancel, duringAttempt.Token)
                .ToArray();
            throw new Exception("Partial success published after cancellation");
        }
        catch (OperationCanceledException)
        {
            checks++;
        }

        var manifest = new SignalManifest
        {
            Raid = "raid",
            RootId = "root",
            ContainerId = "case",
            InteractionNetId = 17,
            Seed = 42,
            Frequency = 96,
            ItemsJson = "[\"fixed-rewards\"]",
            PlacementCandidates = [anchor, other],
            Config = new() { Placements = [], Loot = [] },
        };
        var unresolvedPeer = manifest.ForPeer();
        Check(
            !unresolvedPeer.PlacementResolved
                && unresolvedPeer.Placement == null
                && unresolvedPeer.PlacementCandidates.Count == 0,
            "unresolved catalog stays on authority"
        );
        manifest.Config.Placements = locations;
        manifest.Config.Loot = SignalsDefaults.Loot();
        manifest.Error = "asset unavailable";
        var errorPeer = manifest.ForPeer();
        Check(
            errorPeer.Config.Placements.Count == 0
                && errorPeer.Config.Loot.Count == 0
                && errorPeer.PlacementCandidates.Count == 0
                && manifest.Config.Placements.Count == 24,
            "error snapshots also strip catalogs without changing authority config"
        );
        manifest.Error = null;
        manifest.Placement = fallback.Placement;
        manifest.PlacementResolved = true;
        var peer = manifest.ForPeer();
        Check(
            peer.PlacementCandidates.Count == 0 && peer.HasSamePlacement(manifest),
            "peer receives exact resolved transform and identity"
        );
        var wire = JsonSerializer.Serialize(peer);
        var restored = JsonSerializer.Deserialize<SignalManifest>(wire);
        Check(restored.HasSamePlacement(manifest), "server JSON preserves resolved state");
        var client = SkillsExtended.Helpers.ConfigurationJson.Deserialize<SignalManifest>(wire);
        Check(
            client.HasSamePlacement(manifest) && client.PlacementCandidates.Count == 0,
            "native client JSON preserves host placement"
        );
        Check(
            peer.ItemsJson == manifest.ItemsJson && peer.Seed == 42 && peer.Frequency == 96,
            "placement fallback leaves reward and signal identity intact"
        );
        Check(
            manifest.ForPeer().HasSamePlacement(manifest),
            "repeated sync and reconnect keep placement"
        );
        restored.Placement.Position.X += 1;
        Check(!restored.HasSamePlacement(manifest), "relocation rejected");
        restored = manifest.ForPeer();
        restored.InteractionNetId++;
        Check(!restored.HasSamePlacement(manifest), "changed native identity rejected");
        Check(
            !unresolvedPeer.HasSamePlacement(manifest),
            "unresolved snapshot cannot overwrite resolved placement"
        );
        peer.Placement.Position.Y += 3;
        Check(
            peer.Placement.Position.Y != manifest.Placement.Position.Y,
            "peer snapshot transform cannot mutate authority"
        );

        foreach (var radius in new[] { 0f, 10, 25 })
        {
            var config = new SignalsIntelligenceData
            {
                Placements =
                [
                    new()
                    {
                        Id = "area",
                        Map = "woods",
                        SearchRadius = radius,
                    },
                ],
            };
            config.Validate();
            checks++;
        }
        foreach (var radius in new[] { -.01f, 25.01f, float.NaN, float.PositiveInfinity })
        {
            var config = new SignalsIntelligenceData
            {
                Placements =
                [
                    new()
                    {
                        Id = "area",
                        Map = "woods",
                        SearchRadius = radius,
                    },
                ],
            };
            try
            {
                config.Validate();
                throw new Exception("Invalid radius accepted");
            }
            catch (ArgumentException)
            {
                checks++;
            }
        }
        var legacy = SkillsExtended.Helpers.ConfigurationJson.Deserialize<SignalPlacement>(
            "{\"Id\":\"legacy\",\"Map\":\"woods\"}"
        );
        Check(legacy.SearchRadius == 10, "missing radius defaults to ten metres");
        Console.WriteLine(
            $"Signals placement: {checks} search, geometry, cancellation, snapshot and configuration assertions passed."
        );
    }

    private sealed class Scene : ISignalPlacementScene
    {
        public bool NoNavigation;
        public Vector3 Snap;
        public Func<Vector3, Vector3> NavigationMap;
        public Func<Vector3, float?> Floor = _ => 0;
        public float Slope;
        public Func<SignalVolume, SignalPlacementFailure> Block = _ => SignalPlacementFailure.None;
        public bool CanSee = true;
        public bool Connected = true;

        public bool Navigation(Vector3 near, out Vector3 position)
        {
            position = NavigationMap?.Invoke(near) ?? near + Snap;
            return !NoNavigation;
        }

        public bool Ground(Vector3 near, out Vector3 point, out float slope)
        {
            var y = Floor(near);
            point = new Vector3(near.X, y ?? 0, near.Z);
            slope = Slope;
            return y.HasValue;
        }

        public SignalPlacementFailure Clearance(SignalVolume volume) => Block(volume);

        public bool Visible(Vector3 eye, Vector3 target) => CanSee;

        public bool Route(Vector3 approach) => Connected;
    }
}
