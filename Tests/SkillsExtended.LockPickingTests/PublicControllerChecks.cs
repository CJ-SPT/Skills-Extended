using System.Text.Json;
using SkillsExtended.Config.Skills;
using SkillsExtended.LockPicking;

internal static class PublicControllerChecks
{
    public static void Run(Action<bool, string> check, string output)
    {
        var config = new LockPickingData();
        var results = new List<object>();
        float maxTime = 0, totalTime = 0, maxWear = 0;
        var totalDrops = 0;
        for (var tier = 4; tier <= 5; tier++)
        for (uint seed = 1; seed <= 20; seed++)
        foreach (var fps in new[] { 30, 60, 144 })
        foreach (var delay in new[] { 0f, .1f })
        {
            // Only the fixture owns the definition; the controller gets serialized public state.
            var engine = new PinLockEngine(PinLockDefinition.Create(config.Tier(tier), seed), config, tier, 0);
            var controller = new PublicPickingController();
            var events = new PickCueReader();
            var pending = new Queue<(float Due, PickSnapshot State)>();
            var latest = engine.Snapshot();
            float nextSnapshot = 0;
            var drops = 0;
            for (var frame = 0; frame < fps * 180 && engine.Outcome == PickOutcome.Active; frame++)
            {
                var now = frame / (float)fps;
                if (now + .00001f >= nextSnapshot)
                {
                    var packet = JsonSerializer.Serialize(engine.Snapshot());
                    pending.Enqueue((now + delay, JsonSerializer.Deserialize<PickSnapshot>(packet)));
                    nextSnapshot += .05f;
                }
                while (pending.TryPeek(out var queued) && queued.Due <= now) latest = pending.Dequeue().State;
                var input = controller.Tick(latest, 1f / fps);
                engine.Advance(1f / fps, input.Depth, input.Lift, true, input.Pressure);
                drops += events.Read(engine.Snapshot()).Count(c => c.Sound is PickSound.Drop or PickSound.LostSet);
            }
            var final = engine.Snapshot();
            check(final.Outcome == PickOutcome.Unlocked,
                $"Public-only controller: tier {tier}, seed {seed}, {fps}FPS, delay {delay}, outcome {final.Outcome}, sets {PickPresentation.SetCount(final)}, lift {final.Lift}, wear {final.Wear}");
            maxTime = Math.Max(maxTime, final.ElapsedSeconds);
            totalTime += final.ElapsedSeconds;
            maxWear = Math.Max(maxWear, final.Wear);
            totalDrops += drops;
            results.Add(new { tier, seed, fps, snapshotDelay = delay, seconds = final.ElapsedSeconds, drops, wear = final.Wear, outcome = final.Outcome.ToString() });
        }
        Console.WriteLine($"Public-only controller: {results.Count} runs solved; mean {totalTime / results.Count:F1}s, max {maxTime:F1}s; {totalDrops} losses; max wear {maxWear:P2}.");
        if (output != null)
        {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "public-controller-results.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
