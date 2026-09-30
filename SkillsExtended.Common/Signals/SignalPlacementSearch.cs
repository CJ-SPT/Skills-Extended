using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace SkillsExtended.Signals;

public enum SignalPlacementFailure
{
    None,
    Navigation,
    OutsideArea,
    Support,
    Slope,
    UnevenGround,
    Solid,
    Interaction,
    Hazard,
    Approach,
    Route,
}

public readonly struct SignalVolume
{
    public readonly Vector3 Center;
    public readonly Vector3 Extents;
    public readonly float Yaw;

    public SignalVolume(Vector3 center, Vector3 extents, float yaw = 0)
    {
        Center = center;
        Extents = extents;
        Yaw = yaw;
    }

    public SignalVolume At(Vector3 position, float yaw) =>
        new(position + SignalPlacementSearch.Rotate(Center, yaw), Extents, yaw);
}

public sealed class SignalCaseGeometry
{
    public SignalVolume Body { get; set; }
    public SignalVolume Opening { get; set; }
    public SignalVolume Interaction { get; set; }
    public Vector3[] Approaches { get; set; }
    public Vector3 ViewTarget { get; set; }
}

// Only this adapter touches Unity. The same search and acceptance rules run in fixtures.
public interface ISignalPlacementScene
{
    bool Navigation(Vector3 near, out Vector3 position);
    bool Ground(Vector3 near, out Vector3 point, out float slope);
    SignalPlacementFailure Clearance(SignalVolume volume);
    bool Visible(Vector3 eye, Vector3 target);
    bool Route(Vector3 approach);
}

public sealed class SignalPlacementAttempt
{
    public string Location { get; set; }
    public SignalPlacementFailure Failure { get; set; }
    public SignalPlacement Placement { get; set; }
}

public sealed class SignalPlacementReport
{
    public int Attempts { get; private set; }
    public List<string> Locations { get; } = new();
    public Dictionary<SignalPlacementFailure, int> Rejections { get; } = new();
    public SignalPlacement Placement { get; private set; }

    public void Add(SignalPlacementAttempt attempt)
    {
        Attempts++;
        if (!Locations.Contains(attempt.Location))
            Locations.Add(attempt.Location);
        if (attempt.Failure == SignalPlacementFailure.None)
            Placement = attempt.Placement;
        else
            Rejections[attempt.Failure] = Rejections.TryGetValue(attempt.Failure, out var n)
                ? n + 1
                : 1;
    }

    public override string ToString() =>
        $"Locations [{string.Join(", ", Locations)}], {Attempts} attempts; "
        + string.Join(", ", Rejections.Select(p => $"{p.Key}={p.Value}"))
        + (
            Placement == null
                ? "; no safe placement."
                : $"; selected {Placement.Id} at ({Placement.Position.X:0.###}, {Placement.Position.Y:0.###}, {Placement.Position.Z:0.###}), yaw {Placement.Yaw:0.##}."
        );
}

public static class SignalPlacementSearch
{
    public static Vector3 Vector(SignalPoint p) => new(p.X, p.Y, p.Z);

    public static SignalPoint Point(Vector3 p) =>
        new()
        {
            X = p.X,
            Y = p.Y,
            Z = p.Z,
        };

    public static SignalPlacement Copy(SignalPlacement p) =>
        p == null
            ? null
            : new()
            {
                Id = p.Id,
                Map = p.Map,
                Name = p.Name,
                Enabled = p.Enabled,
                Position = Point(Vector(p.Position)),
                Yaw = p.Yaw,
                SearchRadius = p.SearchRadius,
            };

    // Explicit PRNG and ID hash keep ordering independent of runtime/string hash versions.
    private static uint Next(ref uint state)
    {
        state = unchecked(state * 1664525u + 1013904223u);
        return state;
    }

    public static List<SignalPlacement> Order(IEnumerable<SignalPlacement> locations, uint seed)
    {
        var list = locations.OrderBy(p => p.Id, StringComparer.Ordinal).Select(Copy).ToList();
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = (int)(Next(ref seed) % (uint)(i + 1));
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    public static IEnumerable<Vector3> Samples(SignalPlacement anchor, uint seed)
    {
        var origin = Vector(anchor.Position);
        yield return origin;
        if (anchor.SearchRadius == 0)
            yield break;
        foreach (var c in anchor.Id)
            seed = unchecked((seed ^ c) * 16777619u);
        var phase = Next(ref seed) / (double)uint.MaxValue * Math.PI * 2;
        for (var i = 0; i < 32; i++)
        {
            var radius = anchor.SearchRadius * Math.Sqrt((i + .5) / 32);
            var angle = phase + i * 2.399963229728653;
            yield return origin
                + new Vector3(
                    (float)(Math.Cos(angle) * radius),
                    0,
                    (float)(Math.Sin(angle) * radius)
                );
        }
    }

    public static Vector3 Rotate(Vector3 p, float yaw) =>
        Vector3.Transform(
            p,
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw * (float)Math.PI / 180)
        );

    private static float Horizontal(Vector3 a, Vector3 b) =>
        new Vector2(a.X - b.X, a.Z - b.Z).Length();

    private static bool SnapAllowed(Vector3 from, Vector3 to) =>
        Horizontal(from, to) <= 1.0001f && Math.Abs(from.Y - to.Y) <= 1.0001f;

    private static bool Intersects(SignalVolume a, SignalVolume b)
    {
        if (Math.Abs(a.Center.Y - b.Center.Y) >= a.Extents.Y + b.Extents.Y)
            return false;
        var ax = Rotate(Vector3.UnitX, a.Yaw);
        var az = Rotate(Vector3.UnitZ, a.Yaw);
        var bx = Rotate(Vector3.UnitX, b.Yaw);
        var bz = Rotate(Vector3.UnitZ, b.Yaw);
        foreach (var axis in new[] { ax, az, bx, bz })
        {
            var reach =
                a.Extents.X * Math.Abs(Vector3.Dot(ax, axis))
                + a.Extents.Z * Math.Abs(Vector3.Dot(az, axis))
                + b.Extents.X * Math.Abs(Vector3.Dot(bx, axis))
                + b.Extents.Z * Math.Abs(Vector3.Dot(bz, axis));
            if (Math.Abs(Vector3.Dot(a.Center - b.Center, axis)) >= reach)
                return false;
        }
        return true;
    }

    // Each MoveNext performs just one bounded candidate/yaw attempt. Callers can yield
    // between attempts; cancellation never returns a partial placement as successful.
    public static IEnumerable<SignalPlacementAttempt> Search(
        IEnumerable<SignalPlacement> orderedLocations,
        uint seed,
        SignalCaseGeometry geometry,
        ISignalPlacementScene scene,
        CancellationToken cancellation = default
    )
    {
        foreach (var anchor in orderedLocations)
        foreach (var sample in Samples(anchor, seed))
            for (var turn = 0; turn < 4; turn++)
            {
                cancellation.ThrowIfCancellationRequested();
                var yaw = SignalsModel.Wrap(anchor.Yaw + turn * 90);
                var failure = Evaluate(anchor, sample, yaw, geometry, scene, out var position);
                var resolved = failure == SignalPlacementFailure.None ? Copy(anchor) : null;
                if (resolved != null)
                {
                    resolved.Position = Point(position);
                    resolved.Yaw = yaw;
                    resolved.SearchRadius = 0;
                }
                cancellation.ThrowIfCancellationRequested();
                yield return new SignalPlacementAttempt
                {
                    Location = anchor.Id,
                    Failure = failure,
                    Placement = resolved,
                };
                if (resolved != null)
                    yield break;
            }
    }

    private static SignalPlacementFailure Evaluate(
        SignalPlacement anchor,
        Vector3 sample,
        float yaw,
        SignalCaseGeometry geometry,
        ISignalPlacementScene scene,
        out Vector3 position
    )
    {
        position = default;
        if (!scene.Navigation(sample, out var nav) || !SnapAllowed(sample, nav))
            return SignalPlacementFailure.Navigation;
        // Radius zero preserves authored X/Z, while allowing grounding and checking
        // nearby navigation. Nonzero areas allow only bounded navigation adjustment.
        if (anchor.SearchRadius == 0)
            nav = new Vector3(sample.X, nav.Y, sample.Z);
        if (Horizontal(nav, Vector(anchor.Position)) > anchor.SearchRadius + .0001f)
            return SignalPlacementFailure.OutsideArea;
        var body = geometry.Body;
        var low = float.PositiveInfinity;
        var high = float.NegativeInfinity;
        foreach (
            var offset in new[]
            {
                body.Center,
                body.Center + new Vector3(-body.Extents.X, 0, -body.Extents.Z),
                body.Center + new Vector3(-body.Extents.X, 0, body.Extents.Z),
                body.Center + new Vector3(body.Extents.X, 0, -body.Extents.Z),
                body.Center + new Vector3(body.Extents.X, 0, body.Extents.Z),
            }
        )
        {
            var probe = nav + Rotate(new Vector3(offset.X, 0, offset.Z), yaw);
            if (!scene.Ground(probe, out var floor, out var slope) || Math.Abs(floor.Y - nav.Y) > 1)
                return SignalPlacementFailure.Support;
            if (slope > 25)
                return SignalPlacementFailure.Slope;
            low = Math.Min(low, floor.Y);
            high = Math.Max(high, floor.Y);
        }
        if (high - low > .05001f)
            return SignalPlacementFailure.UnevenGround;
        position = new Vector3(nav.X, high + .02f - (body.Center.Y - body.Extents.Y), nav.Z);
        foreach (var volume in new[] { body, geometry.Opening, geometry.Interaction })
        {
            var failure = scene.Clearance(volume.At(position, yaw));
            if (failure != SignalPlacementFailure.None)
                return failure;
        }
        var approachFailure = SignalPlacementFailure.Approach;
        foreach (var offset in geometry.Approaches)
        {
            var desired = position + Rotate(offset, yaw);
            desired.Y = high;
            if (
                !scene.Navigation(desired, out var approach)
                || !SnapAllowed(desired, approach)
                || Horizontal(desired, approach) > .25f
                || Horizontal(approach, position) > 3
                || Math.Abs(approach.Y - position.Y) > 3
            )
                continue;
            // Native opening animations use the authored interaction offset. Check
            // that actual standing space, not a conveniently shifted navmesh point.
            approach.X = desired.X;
            approach.Z = desired.Z;
            if (
                !scene.Ground(approach, out var floor, out var slope)
                || slope > 25
                || Math.Abs(floor.Y - approach.Y) > .2f
            )
                continue;
            approach.Y = floor.Y;
            var standing = new SignalVolume(
                approach + new Vector3(0, .95f, 0),
                new Vector3(.3f, .9f, .3f)
            );
            // The candidate does not exist in the scene yet. Reserve its space so
            // navigation snapping cannot put the only usable approach inside it.
            if (
                Intersects(standing, body.At(position, yaw))
                || Intersects(standing, geometry.Opening.At(position, yaw))
            )
                continue;
            var clearance = scene.Clearance(standing);
            if (clearance != SignalPlacementFailure.None)
            {
                approachFailure = clearance;
                continue;
            }
            if (
                !scene.Visible(
                    approach + Vector3.UnitY * 1.5f,
                    position + Rotate(geometry.ViewTarget, yaw)
                )
            )
                continue;
            if (!scene.Route(approach))
            {
                approachFailure = SignalPlacementFailure.Route;
                continue;
            }
            return SignalPlacementFailure.None;
        }
        return approachFailure;
    }
}
