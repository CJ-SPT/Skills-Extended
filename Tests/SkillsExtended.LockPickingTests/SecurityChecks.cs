using System.Text.Json;
using SkillsExtended.Config.Skills;
using SkillsExtended.LockPicking;

internal static class SecurityChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var config = new LockPickingData();
        void Advance(PinLockEngine e, float depth, float lift, float pressure, float seconds = 1, int fps = 60)
        {
            for (var n = 0; n < (int)(seconds * fps); n++)
                e.Advance(1f / fps, depth, lift, pressure > 0, pressure);
        }
        for (var tier = 1; tier <= 5; tier++)
        for (uint seed = 1; seed <= 40; seed++)
        foreach (var skill in new[] { 0, 25, 51 })
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var definition = PinLockDefinition.Create(config.Tier(tier), seed);
            var game = new PinLockEngine(definition, config, tier, skill);
            check(definition.Types.Count(t => t == PinType.Spool) == config.Tier(tier).SpoolPins
                && definition.Types.Count(t => t == PinType.Serrated) == config.Tier(tier).SerratedPins,
                "Generated security composition matches the tier");
            check(JsonSerializer.Serialize(definition) == JsonSerializer.Serialize(PinLockDefinition.Create(config.Tier(tier), seed)),
                "Security geometry is reproducible");
            check(game.Snapshot().PinTypes.SequenceEqual(definition.Types), "Visible profiles match the authoritative lock");
            var cloned = game.Snapshot();
            cloned.PinTypes[0] = (PinType)99;
            check(game.Snapshot().PinTypes[0] == definition.Types[0], "Visible profile snapshots cannot mutate the lock");
            // First encounter each pin at normal pressure, stopping on its first catch.
            foreach (var pin in definition.Order)
            {
                var depth = (pin + .5f) / definition.Order.Length;
                Advance(game, depth, 0, .35f, 1, fps);
                for (var frame = 0; frame < fps * 3 && game.Outcome == PickOutcome.Active; frame++)
                {
                    game.Advance(1f / fps, depth, definition.Heights[pin], true, .35f);
                    if (game.Coaching().State is PinState.Caught or PinState.Set) break;
                }
            }
            // Read catches, ease pressure, and revisit stacks until all have truly set.
            for (var pass = 0; pass < 5 && game.Outcome == PickOutcome.Active; pass++)
                foreach (var pin in definition.Order)
                {
                    if (game.Outcome != PickOutcome.Active) break;
                    var depth = (pin + .5f) / definition.Order.Length;
                    Advance(game, depth, 0, .20f, 1, fps);
                    if (game.Coaching().State == PinState.Set) continue;
                    Advance(game, depth, definition.Heights[pin], .20f, 3, fps);
                }
            check(game.Outcome == PickOutcome.Unlocked && game.Wear == 0,
                $"Security lock solvable without forcing: tier {tier}, seed {seed}, skill {skill}, FPS {fps}");
        }
        var spool = new PinLockDefinition
        {
            Heights = [.5f, .5f, .5f], Order = [0, 1, 2],
            Types = [PinType.Standard, PinType.Standard, PinType.Spool],
            Catches = [[], [], [.25f]],
        };
        PinLockEngine FalseSet()
        {
            var e = new PinLockEngine(spool, config, 1, 0);
            for (var pin = 0; pin < 2; pin++)
            {
                Advance(e, (pin + .5f) / 3, 0, .35f);
                Advance(e, (pin + .5f) / 3, .5f, .35f);
            }
            Advance(e, .9f, 0, .35f);
            Advance(e, .9f, .25f, .35f);
            return e;
        }
        var falseSet = FalseSet();
        check(falseSet.Coaching().State == PinState.Caught && falseSet.Coaching().SetPins == 2
            && falseSet.Snapshot().CylinderRotation > .08f && falseSet.Outcome == PickOutcome.Active,
            "Spool false set rotates the cylinder without unlocking");
        Advance(falseSet, .9f, .5f, .65f, .5f);
        check(falseSet.Coaching().State == PinState.Caught && falseSet.Snapshot().Strain > 0,
            "Heavy torque traps a spool shoulder and produces strain");
        var rotationBefore = falseSet.Snapshot().CylinderRotation;
        Advance(falseSet, .9f, .5f, .20f, .30f);
        check(falseSet.Snapshot().CylinderRotation < rotationBefore,
            "Easing tension permits visible counter-rotation");
        Advance(falseSet, .9f, .5f, .20f, 2);
        check(falseSet.Outcome == PickOutcome.Unlocked, "Spool clears with controlled pressure");

        var counterLoss = FalseSet();
        Advance(counterLoss, .9f, .5f, .15f, .6f);
        check(counterLoss.Coaching().SetPins == 1 && counterLoss.Outcome == PickOutcome.Active,
            "Low-torque counter-rotation drops a shallow supported pin while retaining another");
        var recovery = FalseSet();
        Advance(recovery, .9f, 0, .05f);
        check(recovery.Coaching().SetPins == 0 && recovery.Outcome == PickOutcome.Active,
            "Insufficient torque drops supported pins without damaging the lock");
        Advance(recovery, .9f, 0, 0);
        check(recovery.Coaching().State == PinState.Unsettled, "Full release resets catches");
        var overset = new PinLockEngine(spool, config, 1, 0);
        Advance(overset, .1f, .5f, .35f, 2);
        check(overset.Coaching().State == PinState.Set, "Standard pin can truly set");
        Advance(overset, .1f, 1, .35f, .5f);
        check(overset.Coaching().State == PinState.Overset, "Previously set pin can be overset");
        Advance(overset, .1f, 0, .20f);
        check(overset.Coaching().State == PinState.Unsettled, "Individual overset is recoverable without full release");
        var onlyCatch = new PinLockDefinition
        {
            Heights = [.5f, .5f, .5f], Order = [0, 1, 2],
            Types = [PinType.Serrated, PinType.Standard, PinType.Standard],
            Catches = [[.2f, .3f, .4f], [], []],
        };
        var serrated = new PinLockEngine(onlyCatch, config, 1, 0);
        Advance(serrated, .1f, .2f, .35f);
        check(serrated.Coaching().State == PinState.Caught && !serrated.MadeProgress,
            "A serration click is not true progress");
        Advance(serrated, .1f, .5f, .20f, 2);
        check(serrated.Coaching().State == PinState.Set
            && serrated.Snapshot().Cues.Count(c => c.Sound == PickSound.Click) == 4,
            "Three serration catches and a true set produce the same click family");
        var before = JsonSerializer.Serialize(serrated.Snapshot());
        serrated.Advance(.1f, .1f, .5f, true, float.NaN);
        check(before == JsonSerializer.Serialize(serrated.Snapshot()), "Nonfinite torque cannot mutate the engine");

        var custom = JsonSerializer.Deserialize<LockPickingTier>("{\"Level\":5,\"Pins\":3}");
        check(custom.SpoolPins + custom.SerratedPins < custom.Pins, "Missing security fields respect custom pin counts");
        var explicitZero = JsonSerializer.Deserialize<LockPickingTier>("{\"Level\":5,\"Pins\":5,\"SpoolPins\":0,\"SerratedPins\":0}");
        check(explicitZero.SpoolPins == 0 && explicitZero.SerratedPins == 0, "Explicit zeros preserve standard-only locks");
        var invalid = new LockPickingData();
        invalid.Tier(1).SpoolPins = 3;
        try { invalid.Validate(); check(false, "Security composition must leave a standard pin"); }
        catch (ArgumentException) { check(true, "Invalid composition rejected"); }

        uint catchSeed = 1;
        while (PinLockDefinition.Create(config.Tier(2), catchSeed).Types[
            PinLockDefinition.Create(config.Tier(2), catchSeed).Order[0]] != PinType.Spool) catchSeed++;
        var catchDefinition = PinLockDefinition.Create(config.Tier(2), catchSeed);
        var catchAuthority = new PickingAuthority(new LockPickingData
            { FailureLockXpRatio = .25f, XpTable = new() { ["2"] = 4 } }, catchSeed - 1);
        var catchRequest = new PickRequest { ProtocolVersion = PickingProtocol.Version,
            Raid = catchAuthority.Raid, Actor = "a", Door = "d", Tool = "t", Operation = "start" };
        catchAuthority.Process(catchRequest, 0, 2, 0, 5, null);
        var catchGame = catchAuthority.Active["d"].Engine;
        var firstPin = catchDefinition.Order[0];
        var catchDepth = (firstPin + .5f) / 3;
        Advance(catchGame, catchDepth, 0, .35f);
        Advance(catchGame, catchDepth, catchDefinition.Catches[firstPin][0], .35f);
        Advance(catchGame, catchDepth, 1, .65f, 10);
        var failedCatch = catchAuthority.Advance(.05f).Single();
        check(failedCatch.State.Outcome == PickOutcome.PickBroken && failedCatch.ToolUses == 1
            && failedCatch.Xp == 0, "Breaking after only a false catch consumes a use without failure XP");

        var authority = new PickingAuthority(config, 42);
        var legacy = new PickRequest { Actor = "a", Operation = "sync" };
        check(authority.Process(legacy, 0, 1, 0, 5, null).Error == PickingProtocol.UpdateMessage,
            "Legacy peers are rejected at synchronization");
        var request = new PickRequest { ProtocolVersion = PickingProtocol.Version, Actor = "a", Raid = authority.Raid,
            Door = "d", Tool = "t", Operation = "start" };
        var reply = authority.Process(request, 0, 2, 0, 5, null);
        request.Operation = "input"; request.Attempt = reply.Attempt; request.Sequence = 1;
        request.TensionStrength = float.PositiveInfinity;
        authority.Process(request, 0, 2, 0, 5, null);
        check(authority.Active["d"].Sequence == 0, "Nonfinite network pressure rejected");
        request.TensionStrength = 1.1f;
        authority.Process(request, 0, 2, 0, 5, null);
        check(authority.Active["d"].Sequence == 0, "Out-of-range network pressure rejected");
        request.TensionStrength = .20f;
        authority.Process(request, 0, 2, 0, 5, null);
        check(authority.Active["d"].TensionStrength == .20f, "Valid pressure remains host authoritative");
    }
}
