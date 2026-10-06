using System.Text.Json;
using SkillsExtended.Config.Skills;
using SkillsExtended.LockPicking;

internal static class CoachingChecks
{
    public static void Run(Action<bool, string> check)
    {
        foreach (var enabled in new[] { true, false })
        for (var tier = 1; tier <= 5; tier++)
        foreach (var skill in new[] { 0, 5, 6, 10, 11, 51 })
        {
            var authority = new PickingAuthority(new LockPickingData { EnableRaidCoaching = enabled }, 123);
            var start = authority.Process(new PickRequest
            {
                ProtocolVersion = PickingProtocol.Version, Raid = authority.Raid,
                Actor = "learner", Door = "door", Tool = "pick", Operation = "start",
            }, skill, tier, 0, 10, null);
            var eligible = enabled && tier <= 3 && skill <= 10;
            check((start.Coaching != null) == eligible, $"Raid coaching boundary: enabled {enabled}, tier {tier}, skill {skill}");
            authority.Process(new PickRequest
            {
                ProtocolVersion = PickingProtocol.Version, Raid = authority.Raid,
                Actor = "learner", Door = "door", Attempt = start.Attempt,
                Operation = "input", Sequence = 1,
            }, skill, tier, 0, 10, null);
            var tick = authority.Advance(.05f).Single();
            var remote = Newtonsoft.Json.JsonConvert.DeserializeObject<PickReply>(
                Newtonsoft.Json.JsonConvert.SerializeObject(tick));
            check((remote.Coaching != null) == eligible, "Coaching eligibility survives Fika reply serialization");
            if (eligible)
                check(remote.Coaching.BindingPin == authority.Active["door"].Engine.Coaching().BindingPin
                    && remote.State.SetPinStates.Length == remote.State.Pins,
                    "Remote coaching reflects the authoritative engine");
            check(authority.End("door", false).Coaching == null, "Completed attempts stop raid coaching");
        }
        var buffed = new PickingAuthority(new LockPickingData(), 123);
        var buffedStart = buffed.Process(new PickRequest
        {
            ProtocolVersion = PickingProtocol.Version, Raid = buffed.Raid,
            Actor = "learner", Door = "door", Tool = "pick", Operation = "start",
        }, 13, 3, 0, 10, null, coachingSkill: 10);
        check(buffedStart.Coaching != null, "Temporary skill buffs do not remove beginner coaching");
        buffed.Process(new PickRequest
        {
            ProtocolVersion = PickingProtocol.Version, Raid = buffed.Raid,
            Actor = "learner", Door = "door", Attempt = buffedStart.Attempt,
            Operation = "input", Sequence = 1,
        }, 11, 3, 0, 10, null, coachingSkill: 11);
        check(buffed.Advance(.05f).Single().Coaching == null,
            "Reaching skill level 11 removes coaching from an active attempt");

        var config = new LockPickingData { EnableRaidCoaching = false };
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
            check(PickPresentation.SetCount(retry.Snapshot()) == 0,
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
        s.Tension = true;
        check(Guide(.5f).Instruction.Contains("stop at the set click"), "Coaching explains immediate seating");
        check(Guide(.8f).Instruction.Contains("lower your input"), "Coaching warns about excessive commanded lift");
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
        for (var frame = 0; frame < 100 && !hold.Snapshot().SetPinStates[0]; frame++)
            hold.Advance(PinLockEngine.StepSeconds, 0, .5f, true);
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
        foreach (var secret in new[] { "BindingPin", "LiftMin", "LiftMax", "PressureMin", "PressureMax", "HoldProgress", "Heights", "Order", "Catches", "State" })
            check(!snapshotNames.Contains(secret), "Coaching targets remain outside public snapshots: " + secret);
        check(snapshotNames.Contains("SetPinStates"), "Confirmed sets are visible without coaching");
    }
}
