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
    public uint Seed { get; set; }
    public float Frequency { get; set; }

    // Native flat item JSON stays opaque to the simulation and travels unchanged to peers.
    public string ItemsJson { get; set; } = "[]";
    public SignalsIntelligenceData Config { get; set; }
    public string Error { get; set; }
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

    public static bool Intersect(
        SignalReading a,
        SignalReading b,
        float separation,
        out SignalPoint point,
        out float radius
    )
    {
        point = null;
        radius = 0;
        var angle = Math.Abs(Delta(a.Bearing, b.Bearing));
        if (SignalPoint.Distance(a.Position, b.Position) < separation || angle < 15 || angle > 165)
            return false;
        var ax = Math.Sin(a.Bearing * Math.PI / 180);
        var az = Math.Cos(a.Bearing * Math.PI / 180);
        var bx = Math.Sin(b.Bearing * Math.PI / 180);
        var bz = Math.Cos(b.Bearing * Math.PI / 180);
        var dx = b.Position.X - a.Position.X;
        var dz = b.Position.Z - a.Position.Z;
        var cross = ax * bz - az * bx;
        var t = (dx * bz - dz * bx) / cross;
        var u = (dx * az - dz * ax) / cross;
        if (t < 0 || u < 0 || t > 4000 || u > 4000)
            return false;
        point = new SignalPoint
        {
            X = a.Position.X + (float)(ax * t),
            Z = a.Position.Z + (float)(az * t),
        };
        radius = Math.Max(
            12,
            (float)(
                (
                    t * Math.Tan(a.Uncertainty * Math.PI / 180)
                    + u * Math.Tan(b.Uncertainty * Math.PI / 180)
                ) / Math.Abs(cross)
            )
        );
        return true;
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
            State.Message = error ?? "Beacon unavailable.";
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
                $"Move at least {Manifest.Config.MinimumSeparation:0.#} metres from each of your previous readings.";
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
            State.Message = "Establish a fix and approach the case.";
            return;
        }
        if (r.Operation == "pair" && State.PairingActor != null && State.PairingActor != r.Actor)
        {
            State.Message = "Another receiver is pairing.";
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
            State.Message = "Stop moving before taking a reading.";
            return;
        }
        var dt = Math.Max(0, Math.Min(.35, now - op.Last));
        op.Last = now;
        var aligned =
            r.Operation == "scan"
                ? Math.Abs(r.Frequency - Manifest.Frequency)
                    <= SignalsModel.Tolerance(Manifest.Config, level)
                    && Math.Abs(
                        SignalsModel.Delta(
                            r.Bearing,
                            SignalsModel.ObservedBearing(Manifest, position, level)
                        )
                    ) <= 5
                : Math.Abs(SignalsModel.Delta(r.Phase, SignalsModel.PairPhase(Manifest.Seed, now)))
                    <= 12;
        op.Stable = aligned ? op.Stable + dt : 0;
        if (r.Operation == "pair")
            State.PairingActor = r.Actor;
        State.Message = aligned
            ? $"{(r.Operation == "scan" ? "Recording" : "Pairing")}: {op.Stable:0.0}s"
            : "Align the signal and hold steady.";
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
            State.Message = "Pairing complete. Signal cache unlocked.";
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
        foreach (var other in SignalsModel.PlottedReadings(State, level))
            if (
                SignalsModel.Intersect(
                    other,
                    reading,
                    Manifest.Config.MinimumSeparation,
                    out var estimate,
                    out var radius
                ) && (!State.HasFix || radius < State.Radius)
            )
            {
                State.HasFix = true;
                State.AccessCode = (Manifest.Seed % 1000000).ToString("D6");
                State.Estimate = estimate;
                State.Radius = radius;
            }
        State.Readings.Add(reading);
        // Shared history retains six; each receiver plots its owner's allowance.
        while (State.Readings.Count > 6)
            State.Readings.RemoveAt(0);
        _awards.TryGetValue(r.Actor, out var count);
        if (count < 2)
        {
            Award(r.Actor, Manifest.Config.BearingXp);
            _awards[r.Actor] = count + 1;
        }
        State.Message = State.HasFix
            ? "Fix established. Access code recovered. Search the plotted area."
            : $"Bearing recorded. Move {Manifest.Config.MinimumSeparation:0.#} metres or more and take a crossing bearing.";
    }

    private void Award(string actor, float xp)
    {
        State.EarnedXp.TryGetValue(actor, out var earned);
        State.EarnedXp[actor] = earned + xp;
    }
}
