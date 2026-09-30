using SkillsExtended.Config.Skills;
using SkillsExtended.Signals;

internal static class SignalsGeometryChecks
{
    public static void Verify()
    {
        var checks = 0;
        void Check(bool condition, string label)
        {
            checks++;
            if (!condition)
                throw new Exception("Signals geometry: " + label);
        }
        var unbounded = new SignalReading[]
        {
            new()
            {
                Position = new(),
                Bearing = 0,
                Uncertainty = 12,
            },
            new()
            {
                Position = new() { X = 125 },
                Bearing = 340,
                Uncertainty = 12,
            },
        };
        Check(
            !SignalTriangulation.TryFix(unbounded, 125, out _, out _),
            "crossing centre rays with unbounded uncertainty cannot establish a fix"
        );
        Check(
            !SignalTriangulation.TryFix(new[] { unbounded[0] }, 125, out _, out _),
            "a single bearing cannot establish a fix"
        );
        Check(
            !SignalTriangulation.TryFix(
                new SignalReading[] { null, unbounded[0] },
                125,
                out _,
                out _
            ),
            "invalid reading rejected"
        );
        var random = new Random(314159);
        for (var sample = 0; sample < 10000; sample++)
        {
            var manifest = new SignalManifest
            {
                Config = new SignalsIntelligenceData(),
                Seed = (uint)sample,
                Placement = new()
                {
                    Position = new() { X = 100, Z = 100 },
                },
            };
            var level =
                sample % 3 == 0 ? 0
                : sample % 3 == 1 ? 25
                : 51;
            SignalReading Reading()
            {
                var angle = random.NextDouble() * Math.PI * 2;
                var distance = 30 + random.NextDouble() * 1500;
                var position = new SignalPoint
                {
                    X = 100 + (float)(Math.Sin(angle) * distance),
                    Z = 100 + (float)(Math.Cos(angle) * distance),
                };
                return new()
                {
                    Position = position,
                    Bearing = SignalsModel.ObservedBearing(manifest, position, level),
                    Uncertainty = SignalsModel.Uncertainty(manifest.Config, level),
                };
            }
            var a = Reading();
            var b = Reading();
            var readings = new List<SignalReading> { a, b };
            for (var count = 2; count <= 6; count++)
            {
                if (count > 2)
                    readings.Add(Reading());
                var found =
                    count == 2
                        ? SignalsModel.Intersect(a, b, 125, out var estimate, out var radius)
                        : SignalTriangulation.TryFix(readings, 125, out estimate, out radius);
                if (!found)
                    continue;
                var error = SignalPoint.Distance(estimate, manifest.Placement.Position);
                Check(
                    error + 2.99f <= radius,
                    $"cache and pairing approach contained: sample={sample}, level={level}, readings={count}, error={error}, radius={radius}"
                );
                foreach (var reading in readings)
                    Check(
                        SignalPoint.Distance(estimate, reading.Position) < .01f
                            || Math.Abs(
                                SignalsModel.Delta(
                                    SignalsModel.Bearing(reading.Position, estimate),
                                    reading.Bearing
                                )
                            )
                                <= reading.Uncertainty + .01f,
                        "estimate agrees with every retained uncertainty band"
                    );
                var state = new SignalSnapshot
                {
                    Ready = true,
                    HasFix = true,
                    Estimate = estimate,
                    Radius = radius,
                };
                var previous = float.MaxValue;
                for (var step = 0; step <= 10; step++)
                {
                    var position = new SignalPoint
                    {
                        X = estimate.X + (100 - estimate.X) * step / 10,
                        Z = estimate.Z + (100 - estimate.Z) * step / 10,
                    };
                    var interval = SignalsModel.ProximityInterval(manifest, state, position);
                    Check(
                        interval > 0 && interval <= previous + .0001f,
                        "beeps stay active and speed up from estimate to physical cache"
                    );
                    previous = interval;
                }
                Check(Math.Abs(previous - .18f) < .001f, "arrival cadence at physical cache");
            }
        }
        // The real beacon may lie at the extreme edge of every uncertainty band.
        foreach (var uncertainty in new[] { 1f, 4f, 12f, 30f })
        foreach (var rotation in new[] { 0f, 89f, 179f, 269f, 359f })
        {
            var target = new SignalPoint { X = -1800, Z = 2200 };
            var readings = Enumerable
                .Range(0, 4)
                .Select(i =>
                {
                    var bearing = rotation + i * 90;
                    var position = new SignalPoint
                    {
                        X = target.X - 500 * (float)Math.Sin(bearing * Math.PI / 180),
                        Z = target.Z - 500 * (float)Math.Cos(bearing * Math.PI / 180),
                    };
                    return new SignalReading
                    {
                        Position = position,
                        Bearing = SignalsModel.Wrap(
                            SignalsModel.Bearing(position, target)
                                + (i % 2 == 0 ? uncertainty : -uncertainty)
                        ),
                        Uncertainty = uncertainty,
                    };
                })
                .ToArray();
            Check(
                SignalTriangulation.TryFix(readings, 125, out var estimate, out var radius)
                    && SignalPoint.Distance(estimate, target) + 2.99f <= radius,
                "extreme noise, configured uncertainty and north wrap retain the cache"
            );
        }
        Console.WriteLine(
            $"Signals geometry: {checks} containment, multi-bearing and approach checks passed."
        );
    }
}
