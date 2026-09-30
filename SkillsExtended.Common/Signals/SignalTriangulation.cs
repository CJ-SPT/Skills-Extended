using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillsExtended.Signals;

public static class SignalTriangulation
{
    private sealed class Boundary
    {
        public double X;
        public double Z;
        public double Offset;

        public double Inside(double x, double z) => X * x + Z * z - Offset;
    }

    // Intersect the full uncertainty wedges, not just their centre rays. Every
    // feasible point lies in this convex region, including the physical beacon.
    // No hidden cache position participates in the estimate or its radius.
    public static bool TryFix(
        IReadOnlyList<SignalReading> readings,
        float separation,
        out SignalPoint estimate,
        out float radius
    )
    {
        estimate = null;
        radius = 0;
        if (
            readings == null
            || readings.Count < 2
            || !SignalPoint.Finite(separation)
            || separation < 0
            || readings.Any(r =>
                r?.Position?.IsFinite != true
                || !SignalPoint.Finite(r.Bearing)
                || !SignalPoint.Finite(r.Uncertainty)
                || r.Uncertainty < 0
                || r.Uncertainty >= 90
            )
        )
            return false;

        var crossing = false;
        for (var i = 0; i < readings.Count; i++)
        for (var j = i + 1; j < readings.Count; j++)
            crossing |= HasCrossing(readings[i], readings[j], separation);
        if (!crossing)
            return false;

        var boundaries = new List<Boundary>();
        foreach (var reading in readings)
        foreach (var side in new[] { -1, 1 })
        {
            var angle = (reading.Bearing + side * reading.Uncertainty) * Math.PI / 180;
            var x = -side * Math.Cos(angle);
            var z = side * Math.Sin(angle);
            boundaries.Add(
                new Boundary
                {
                    X = x,
                    Z = z,
                    Offset = x * reading.Position.X + z * reading.Position.Z,
                }
            );
        }

        // If the normals fit in a semicircle, the overlap extends to infinity.
        // A centre-ray crossing alone must not turn that into a small search fix.
        var angles = boundaries.Select(b => Math.Atan2(b.Z, b.X)).OrderBy(a => a).ToArray();
        for (var i = 0; i < angles.Length; i++)
        {
            var next = i + 1 < angles.Length ? angles[i + 1] : angles[0] + Math.PI * 2;
            if (next - angles[i] >= Math.PI - 1e-8)
                return false;
        }

        var vertices = new List<(double X, double Z)>();
        for (var i = 0; i < boundaries.Count; i++)
        for (var j = i + 1; j < boundaries.Count; j++)
        {
            var a = boundaries[i];
            var b = boundaries[j];
            var determinant = a.X * b.Z - a.Z * b.X;
            if (Math.Abs(determinant) < 1e-10)
                continue;
            var x = (a.Offset * b.Z - a.Z * b.Offset) / determinant;
            var z = (a.X * b.Offset - a.Offset * b.X) / determinant;
            if (boundaries.All(boundary => boundary.Inside(x, z) >= -.001))
                vertices.Add((x, z));
        }
        if (vertices.Count == 0)
            return false;

        estimate = new SignalPoint
        {
            X = (float)vertices.Average(v => v.X),
            Z = (float)vertices.Average(v => v.Z),
        };
        // Enclose every corner, with room to approach the case from any side
        // within pairing range. The same circle drives the plot and beeps.
        var center = estimate;
        radius = Math.Max(
            12,
            3
                + (float)
                    vertices.Max(v =>
                        Math.Sqrt(
                            (v.X - center.X) * (v.X - center.X)
                                + (v.Z - center.Z) * (v.Z - center.Z)
                        )
                    )
        );
        return estimate.IsFinite && SignalPoint.Finite(radius);
    }

    private static bool HasCrossing(SignalReading a, SignalReading b, float separation)
    {
        var angle = Math.Abs(SignalsModel.Delta(a.Bearing, b.Bearing));
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
        return t >= 0 && u >= 0 && t <= 4000 && u <= 4000;
    }
}
