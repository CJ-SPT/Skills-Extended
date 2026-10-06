using System;
using System.Collections.Generic;
using System.Linq;
using SkillsExtended.Config.Skills;

namespace SkillsExtended.Signals;

public static class SignalsIds
{
    public const int Skill = 201;
    public const int Accuracy = 1031;
    public const int Tuning = 1032;
    public const int Memory = 1033;
    public const string Prefix = "skills-signal-";
}

public class SignalPoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public static float Distance(SignalPoint a, SignalPoint b) =>
        (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));

    public bool IsFinite => Finite(X) && Finite(Y) && Finite(Z);

    public static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
}

public class SignalPlacement
{
    public string Id { get; set; } = "";
    public string Map { get; set; } = "";
    public string Name { get; set; } = "";
    public SignalPoint Position { get; set; } = new();
    public float Yaw { get; set; }
    public float SearchRadius { get; set; } = 10;
    public bool Enabled { get; set; } = true;
}

public class SignalLootEntry
{
    public string Template { get; set; } = "";
    public string Theme { get; set; } = "Technical";
    public int Weight { get; set; } = 1;
}

public class SignalManifest
{
    public string Raid { get; set; } = "";
    public string ContainerId { get; set; } = "";
    public int InteractionNetId { get; set; }
    public string RootId { get; set; } = "";
    public string ContainerTemplate { get; set; } = "";
    public SignalPlacement Placement { get; set; }
    public List<SignalPlacement> PlacementCandidates { get; set; } = new();
    public bool PlacementResolved { get; set; }
    public uint Seed { get; set; }
    public float Frequency { get; set; }

    // Native flat item JSON stays opaque to the simulation and travels unchanged to peers.
    public string ItemsJson { get; set; } = "[]";
    public SignalsIntelligenceData Config { get; set; }
    public string Error { get; set; }

    public bool HasSamePlacement(SignalManifest other) =>
        PlacementResolved
        && other?.PlacementResolved == true
        && Placement?.Position != null
        && other.Placement?.Position != null
        && ContainerId == other.ContainerId
        && RootId == other.RootId
        && InteractionNetId == other.InteractionNetId
        && Placement.Id == other.Placement.Id
        && Placement.Map == other.Placement.Map
        && Placement.Yaw == other.Placement.Yaw
        && Placement.Position.X == other.Placement.Position.X
        && Placement.Position.Y == other.Placement.Position.Y
        && Placement.Position.Z == other.Placement.Position.Z;

    // Do not publish the authority's search catalog or its mutable working manifest.
    public SignalManifest ForPeer() =>
        new()
        {
            Raid = Raid,
            ContainerId = ContainerId,
            InteractionNetId = InteractionNetId,
            RootId = RootId,
            ContainerTemplate = ContainerTemplate,
            Placement = PlacementResolved ? SignalPlacementSearch.Copy(Placement) : null,
            PlacementResolved = PlacementResolved,
            Seed = Seed,
            Frequency = Frequency,
            ItemsJson = ItemsJson,
            Config = Config?.RulesOnly(),
            Error = Error,
        };
}

public class SignalReading
{
    public string Actor { get; set; }
    public SignalPoint Position { get; set; }
    public float Bearing { get; set; }
    public float Uncertainty { get; set; }
}

public class SignalSnapshot
{
    public string Raid { get; set; }
    public long Revision { get; set; }
    public bool Ready { get; set; }
    public bool Unlocked { get; set; }
    public bool HasFix { get; set; }
    public string AccessCode { get; set; }
    public SignalPoint Estimate { get; set; }
    public float Radius { get; set; }
    public List<SignalReading> Readings { get; set; } = new();
    public Dictionary<string, float> EarnedXp { get; set; } = new();
    public Dictionary<string, int> LastSequences { get; set; } = new();
    public string PairingActor { get; set; }
    public string Message { get; set; }
}

public class SignalRequest
{
    public string Raid { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Operation { get; set; } = "sync";
    public int Sequence { get; set; }
    public float Frequency { get; set; }
    public float Bearing { get; set; }
    public float Phase { get; set; }
}

public static class SignalsModel
{
    public static IEnumerable<SignalReading> PlottedReadings(SignalSnapshot state, int level) =>
        state.Readings.Skip(Math.Max(0, state.Readings.Count - (level >= 51 ? 6 : 4)));

    public static float Wrap(float angle) => (angle % 360 + 360) % 360;

    public static float Delta(float a, float b) => Wrap(a - b + 180) - 180;

    public static float Bearing(SignalPoint from, SignalPoint to) =>
        Wrap((float)(Math.Atan2(to.X - from.X, to.Z - from.Z) * 180 / Math.PI));

    public static float Uncertainty(SignalsIntelligenceData c, int level) =>
        c.BaseUncertainty
        + (c.MinimumUncertainty - c.BaseUncertainty) * Math.Min(50, Math.Max(0, level)) / 50f;

    public static float Tolerance(SignalsIntelligenceData c, int level) =>
        c.TuningTolerance * (1 + c.TuningBonus * Math.Min(50, Math.Max(0, level)) / 50f);

    public static float Noise(uint seed, SignalPoint p)
    {
        unchecked
        {
            var hash =
                seed
                ^ (uint)(int)Math.Floor(p.X / 20) * 73856093u
                ^ (uint)(int)Math.Floor(p.Z / 20) * 19349663u;
            hash ^= hash >> 16;
            hash *= 2246822519u;
            hash ^= hash >> 13;
            return (hash % 10001) / 5000f - 1;
        }
    }

    public static float ObservedBearing(SignalManifest m, SignalPoint p, int level) =>
        Wrap(Bearing(p, m.Placement.Position) + Noise(m.Seed, p) * Uncertainty(m.Config, level));

    public static float Strength(
        SignalManifest m,
        SignalPoint p,
        int level,
        float frequency,
        float direction
    )
    {
        var tuned = Math.Max(
            0,
            1 - Math.Abs(frequency - m.Frequency) / (Tolerance(m.Config, level) * 5)
        );
        var aimed =
            .08f
            + .92f
                * (float)
                    Math.Pow(
                        Math.Max(
                            0,
                            Math.Cos(Delta(direction, ObservedBearing(m, p, level)) * Math.PI / 180)
                        ),
                        6
                    );
        return tuned
            * aimed
            * (.25f + .75f / (1 + SignalPoint.Distance(p, m.Placement.Position) / 350));
    }

    public static float PairPhase(uint seed, double time) =>
        Wrap(seed % 360 + (float)Math.Sin(time * .7) * 35 + (float)Math.Sin(time * .23) * 20);

    public static string ScanAlignmentHint(
        SignalManifest manifest,
        SignalPoint position,
        int level,
        float frequency,
        float bearing
    )
    {
        if (Math.Abs(frequency - manifest.Frequency) > Tolerance(manifest.Config, level))
            return "SkillsExtended.SignalsModel.TuneFrequencyToThePeakLeftRight";
        if (Math.Abs(Delta(bearing, ObservedBearing(manifest, position, level))) > 5)
            return "SkillsExtended.SignalsModel.SweepBearingForStrongestReceptionUpDown";
        return null;
    }

    // Zero means silent. Zone membership uses the same horizontal circle as the plot;
    // cadence uses distance to the physical cache, independently of tuning or direction.
    public static float ProximityInterval(
        SignalManifest manifest,
        SignalSnapshot state,
        SignalPoint position
    )
    {
        if (
            manifest?.Config?.Enabled != true
            || manifest.Error != null
            || manifest.Placement?.Position == null
            || state?.Ready != true
            || !state.HasFix
            || state.Unlocked
            || state.Estimate == null
            || position?.IsFinite != true
            || !SignalPoint.Finite(state.Radius)
            || state.Radius <= 0
            || SignalPoint.Distance(position, state.Estimate) > state.Radius
        )
            return 0;
        var target = manifest.Placement.Position;
        var horizontal = SignalPoint.Distance(position, target);
        var vertical = position.Y - target.Y;
        var distance = (float)Math.Sqrt(horizontal * horizontal + vertical * vertical);
        var range = Math.Max(1, state.Radius + SignalPoint.Distance(state.Estimate, target));
        return .18f + 1.32f * Math.Min(1, distance / range);
    }

    // Pitch supplies heading while pulse cadence supplies distance. Call only when
    // ProximityInterval is active, so this never reveals the cache before a fix.
    public static float ProximityPitch(SignalPoint position, SignalPoint target, float facing)
    {
        if (position?.IsFinite != true || target?.IsFinite != true || !SignalPoint.Finite(facing))
            return 1;
        var distance = SignalPoint.Distance(position, target);
        if (!SignalPoint.Finite(distance))
            return 1;
        var alignment = (float)Math.Cos(Delta(facing, Bearing(position, target)) * Math.PI / 180);
        // Within one metre, settle toward the arrival tone instead of flipping
        // high/low as the player walks across the case's horizontal position.
        return 1.6f - .4f * (1 - alignment) * Math.Min(1, distance);
    }

    public static bool Intersect(
        SignalReading a,
        SignalReading b,
        float separation,
        out SignalPoint point,
        out float radius
    )
    {
        return SignalTriangulation.TryFix(new[] { a, b }, separation, out point, out radius);
    }
}

/// <summary>Monotonic host time and real player positions are supplied by the game adapter.</summary>
public sealed class SignalsAuthority
{
    private sealed class Operation
    {
        public string Kind;
        public SignalPoint Origin;
        public double Last;
        public double Stable;
    }

    public SignalManifest Manifest { get; }
    public SignalSnapshot State { get; }
    private readonly Dictionary<string, Operation> _active = new();
    private readonly Dictionary<string, int> _sequences = new();
    private readonly Dictionary<string, List<SignalPoint>> _positions = new();
    private readonly Dictionary<string, int> _awards = new();
    private readonly HashSet<string> _contributors = new();
    private readonly HashSet<string> _completed = new();

    public SignalsAuthority(SignalManifest manifest)
    {
        manifest.Config.Validate();
        Manifest = manifest;
        State = new SignalSnapshot { Raid = manifest.Raid };
    }

    public void Ready()
    {
        State.Ready = true;
        State.Revision++;
    }

    public void Cancel(string actor)
    {
        _active.Remove(actor);
        if (State.PairingActor == actor)
            State.PairingActor = null;
        State.Revision++;
    }

    public void Expire(double now)
    {
        foreach (
            var actor in _active.Where(x => now - x.Value.Last > 1.5).Select(x => x.Key).ToArray()
        )
            Cancel(actor);
    }

    public void Process(
        SignalRequest r,
        SignalPoint position,
        int level,
        double now,
        string error,
        IEnumerable<string> connected
    )
    {
        if (r.Raid != State.Raid || string.IsNullOrEmpty(r.Actor) || r.Sequence <= 0)
            return;
        if (_sequences.TryGetValue(r.Actor, out var previous) && r.Sequence <= previous)
            return;
        _sequences[r.Actor] = r.Sequence;
        State.LastSequences[r.Actor] = r.Sequence;
        State.Message = null;
        if (r.Operation == "cancel")
        {
            Cancel(r.Actor);
            return;
        }
        if (
            !State.Ready
            || State.Unlocked
            || error != null
            || position == null
            || !position.IsFinite
        )
        {
            Cancel(r.Actor);
            State.Message = error ?? "SkillsExtended.SignalsModel.BeaconUnavailable";
            return;
        }
        if (
            !SignalPoint.Finite(r.Frequency)
            || !SignalPoint.Finite(r.Bearing)
            || !SignalPoint.Finite(r.Phase)
        )
        {
            Cancel(r.Actor);
            return;
        }
        if (r.Operation != "scan" && r.Operation != "pair")
            return;
        if (
            r.Operation == "scan"
            && _positions.TryGetValue(r.Actor, out var recorded)
            && recorded.Any(p =>
                SignalPoint.Distance(p, position) < Manifest.Config.MinimumSeparation
            )
        )
        {
            Cancel(r.Actor);
            State.Message =
                LocalizedText.Message("SkillsExtended.SignalsModel.MoveAtLeastMetresFromEachOfYourPrevious", Manifest.Config.MinimumSeparation);
            return;
        }
        if (
            r.Operation == "pair"
            && (
                !State.HasFix
                || SignalPoint.Distance(position, Manifest.Placement.Position) > 3
                || Math.Abs(position.Y - Manifest.Placement.Position.Y) > 3
            )
        )
        {
            Cancel(r.Actor);
            State.Message = "SkillsExtended.SignalsModel.EstablishAFixAndApproachTheCase";
            return;
        }
        if (r.Operation == "pair" && State.PairingActor != null && State.PairingActor != r.Actor)
        {
            State.Message = "SkillsExtended.SignalsModel.AnotherReceiverIsPairing";
            return;
        }
        if (
            !_active.TryGetValue(r.Actor, out var op)
            || op.Kind != r.Operation
            || now - op.Last > 1.5
        )
        {
            Cancel(r.Actor);
            _active[r.Actor] = op = new Operation
            {
                Kind = r.Operation,
                Origin = position,
                Last = now,
            };
        }
        if (
            SignalPoint.Distance(op.Origin, position) > .3f
            || Math.Abs(op.Origin.Y - position.Y) > .3f
        )
        {
            Cancel(r.Actor);
            State.Message = "SkillsExtended.SignalsModel.StopMovingBeforeTakingAReading";
            return;
        }
        var dt = Math.Max(0, Math.Min(.35, now - op.Last));
        op.Last = now;
        var alignmentHint =
            r.Operation == "scan"
                ? SignalsModel.ScanAlignmentHint(Manifest, position, level, r.Frequency, r.Bearing)
            : Math.Abs(SignalsModel.Delta(r.Phase, SignalsModel.PairPhase(Manifest.Seed, now)))
            <= 12
                ? null
            : "SkillsExtended.SignalsModel.MatchTheWaveformsWithPhaseQEThenHold";
        var aligned = alignmentHint == null;
        op.Stable = aligned ? op.Stable + dt : 0;
        if (r.Operation == "pair")
            State.PairingActor = r.Actor;
        State.Message = aligned
            ? LocalizedText.Message("SkillsExtended.SignalsModel.Progress", (r.Operation == "scan" ? "SkillsExtended.SignalsModel.Recording" : "SkillsExtended.SignalsModel.Pairing"), op.Stable)
            : alignmentHint;
        if (
            op.Stable
            < (
                r.Operation == "scan"
                    ? Manifest.Config.ReadingSeconds
                    : Manifest.Config.PairingSeconds
            )
        )
        {
            State.Revision++;
            return;
        }
        Cancel(r.Actor);
        if (r.Operation == "pair")
        {
            _contributors.Add(r.Actor);
            State.Unlocked = true;
            foreach (var actor in connected.Where(_contributors.Contains).Distinct())
                if (_completed.Add(actor))
                    Award(actor, Manifest.Config.CompletionXp);
            State.Message = "SkillsExtended.SignalsModel.PairingCompleteSignalCacheUnlocked";
            return;
        }
        if (!_positions.TryGetValue(r.Actor, out var positions))
            _positions[r.Actor] = positions = new();
        positions.Add(position);
        _contributors.Add(r.Actor);
        var reading = new SignalReading
        {
            Actor = r.Actor,
            Position = position,
            Bearing = SignalsModel.ObservedBearing(Manifest, position, level),
            Uncertainty = SignalsModel.Uncertainty(Manifest.Config, level),
        };
        State.Readings.Add(reading);
        // Shared history retains six; each receiver plots its owner's allowance.
        while (State.Readings.Count > 6)
            State.Readings.RemoveAt(0);
        if (
            SignalTriangulation.TryFix(
                State.Readings,
                Manifest.Config.MinimumSeparation,
                out var estimate,
                out var radius
            )
        )
        {
            State.HasFix = true;
            State.AccessCode = (Manifest.Seed % 1000000).ToString("D6");
            State.Estimate = estimate;
            State.Radius = radius;
        }
        _awards.TryGetValue(r.Actor, out var count);
        if (count < 2)
        {
            Award(r.Actor, Manifest.Config.BearingXp);
            _awards[r.Actor] = count + 1;
        }
        State.Message =
            State.HasFix ? "SkillsExtended.SignalsModel.FixEstablishedAccessCodeRecoveredSearchThePlottedArea"
            : State.Readings.Count >= 2
                ? LocalizedText.Message("SkillsExtended.SignalsModel.NoCrossingFixYetMoveSidewaysAtLeastMetres", Manifest.Config.MinimumSeparation)
            : LocalizedText.Message("SkillsExtended.SignalsModel.BearingRecordedMoveMetresOrMoreAndTakeA", Manifest.Config.MinimumSeparation);
    }

    private void Award(string actor, float xp)
    {
        State.EarnedXp.TryGetValue(actor, out var earned);
        State.EarnedXp[actor] = earned + xp;
    }
}
