using System.Text.Json;
using SkillsExtended.Config.Skills;
using SkillsExtended.LockPicking;

internal static class CoachingChecks
{
    public static void Run(Action<bool, string> check)
    {
        var config = new LockPickingData();
        // Follow only published practice guidance, with no access to hidden lock geometry.
        for (var tier = 1; tier <= 5; tier++)
        foreach (var skill in new[] { 0, 25, 51 })
        foreach (var fps in new[] { 30, 60, 144 })
        for (uint seed = 1; seed <= 12; seed++)
        {
            var definition = PinLockDefinition.Create(config.Tier(tier), seed);
            var game = new PinLockEngine(definition, config, tier, skill);
            var untouched = new PinLockEngine(definition, config, tier, skill);
            float command = 0;
            for (var frame = 0; frame < fps * 90 && game.Outcome == PickOutcome.Active; frame++)
            {
                var state = game.Snapshot();
                var guidance = PickCoachingPresenter.Present(state, game.Coaching(), command);
                var moving = state.Selected != guidance.RecommendedPin;
                command = moving ? 0 : (guidance.LiftMin + guidance.LiftMax) / 2;
                var depth = ((moving ? guidance.RecommendedPin : state.Selected) + .5f) / state.Pins;
                // Match the UI's five-percent pressure increments, including recovery at 20%.
                var pressure = MathF.Round((guidance.PressureMin + guidance.PressureMax) / .10f) * .05f;
                game.Advance(1f / fps, depth, command, true, pressure);
                untouched.Advance(1f / fps, depth, command, true, pressure);
            }
            check(game.Outcome == PickOutcome.Unlocked && game.Wear == 0,
                $"Guidance completes tier {tier}, seed {seed}, skill {skill}, FPS {fps} without forcing");
            check(JsonSerializer.Serialize(game.Snapshot()) == JsonSerializer.Serialize(untouched.Snapshot()),
                "Coaching reads and presentation do not mutate the simulation");
            var retry = new PinLockEngine(definition, config, tier, skill);
            check(retry.Coaching().HoldProgress == 0 && retry.Coaching().SetPins == 0,
                "Retry has fresh coaching progress");
        }

        var s = new PickSnapshot { Pins = 3, Selected = 0, Tension = true, TensionStrength = .3f };
        var c = new PickCoaching { BindingPin = 2, LiftMin = .4f, LiftMax = .6f, PressureMin = .25f, PressureMax = .35f };
        PickGuidance Guide(float command = 0) => PickCoachingPresenter.Present(s, c, command);
        check(Guide().Instruction.Contains("right to pin 3"), "Wrong selected pin gets a numbered direction");
        s.Lift = .3f;
        check(Guide().Instruction.StartsWith("Lower"), "Raised pick must lower before moving");
        s.Lift = 0;
        check(Guide(.3f).Instruction.StartsWith("Lower"), "Commanded lift must also lower before moving");
        c.BindingPin = 0; s.Tension = false;
        check(Guide().Instruction.Contains("apply tension"), "Tension-off coaching explains the next control");
        s.Tension = true; c.Ready = true;
        check(Guide(.5f).Instruction.Contains("hold steady"), "Setting window asks for a hold");
        check(Guide(.8f).Instruction.Contains("lower your input"), "Excess commanded lift overrides a transient ready state");
        c.State = PinState.Caught; c.PressureMin = .2f; c.PressureMax = .24f;
        check(Guide().Instruction.Contains("ease tension"), "Caught pin first requests recovery pressure");
        s.TensionStrength = .22f; c.Type = PinType.Spool;
        check(Guide().Instruction.Contains("counter-rotation"), "Spool recovery explains counter-rotation");
        c.Type = PinType.Serrated;
        check(Guide().Instruction.Contains("through the click"), "Serrated recovery explains intermediate clicks");
        c.State = PinState.Overset; c.BindingPin = 2;
        check(Guide().RecommendedPin == 0 && Guide().Instruction.Contains("overset"), "Selected overset takes priority over another binding pin");
        s.Outcome = PickOutcome.Unlocked;
        check(Guide().RecommendedPin == -1 && Guide().Instruction == "Lock released", "Completion supersedes active hints");
        s.Outcome = PickOutcome.PickBroken;
        check(Guide().Instruction.StartsWith("Pick broken"), "Breakage supersedes active hints");

        var simple = new PinLockDefinition { Heights = [.5f, .5f, .5f], Order = [0, 1, 2] };
        var hold = new PinLockEngine(simple, config, 1, 0);
        for (var frame = 0; frame < 100 && !hold.Coaching().Ready; frame++)
            hold.Advance(PinLockEngine.StepSeconds, 0, .5f, true);
        check(hold.Coaching().HoldProgress > 0 && hold.Coaching().HoldProgress < 1, "Hold exposes partial progress");
        hold.Advance(PinLockEngine.StepSeconds, 0, .8f, true);
        check(hold.Coaching().HoldProgress == 0, "Commanding above the setting window interrupts the hold");
        hold.Advance(PinLockEngine.StepSeconds, 0, 0, false);
        check(hold.Coaching().HoldProgress == 0, "Releasing tension clears hold progress");
        for (var frame = 0; frame < 100; frame++) hold.Advance(PinLockEngine.StepSeconds, 0, .5f, true);
        check(hold.Coaching().BindingPin == 1, "True set advances recommended binding pin");
        hold.Advance(PinLockEngine.StepSeconds, 0, 0, false);
        check(hold.Coaching().BindingPin == 0 && hold.Coaching().SetPins == 0, "Dropped sets recompute binding order");
        for (var frame = 0; frame < 120; frame++) hold.Advance(PinLockEngine.StepSeconds, 0, 1, true);
        check(hold.Coaching().State == PinState.Overset, "Recovery scenario starts with a real overset");
        for (var frame = 0; frame < 120 && hold.Coaching().State == PinState.Overset; frame++)
        {
            var recovery = PickCoachingPresenter.Present(hold.Snapshot(), hold.Coaching(), 1);
            hold.Advance(PinLockEngine.StepSeconds, 0, (recovery.LiftMin + recovery.LiftMax) / 2, true, .2f);
        }
        check(hold.Coaching().State == PinState.Unsettled, "Guided pressure and lift recover an actual overset");
        var snapshotNames = typeof(PickSnapshot).GetProperties().Select(p => p.Name).ToArray();
        foreach (var secret in new[] { "BindingPin", "LiftMin", "LiftMax", "PressureMin", "PressureMax", "HoldProgress", "SetPinStates" })
            check(!snapshotNames.Contains(secret), "Coaching targets remain outside public snapshots: " + secret);
    }
}
