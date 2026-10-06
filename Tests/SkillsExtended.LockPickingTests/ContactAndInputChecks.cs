using SkillsExtended.Config.Skills;
using SkillsExtended.LockPicking;

internal static class ContactAndInputChecks
{
    public static void Run(Action<bool, string> check)
    {
        var config = new LockPickingData();
        foreach (var fps in new[] { 30, 60, 144 })
        {
            var engine = new PinLockEngine(new PinLockDefinition { Heights = [.5f, .5f, .5f], Order = [0, 1, 2] }, config, 1, 0);
            while (!engine.Snapshot().SetPinStates[0] && engine.Snapshot().ElapsedSeconds < 2)
            {
                engine.Advance(1f / fps, 0, 1, true);
                var s = engine.Snapshot();
                if (s.Lift >= .5f - config.Tier(1).Tolerance)
                    check(s.SetPinStates[0], "Actual contact immediately seats despite commanded lift above the range");
            }
            var set = engine.Snapshot();
            check(set.Cues.Count(c => c.Sound == PickSound.Seat) == 1 && set.SetPinStates[0], "One seat cue per confirmed set");
            set.SetPinStates[0] = false;
            check(engine.Snapshot().SetPinStates[0], "Snapshot flag arrays cannot mutate the engine");
            for (var i = 0; i < fps; i++) engine.Advance(1f / fps, 0, 1, true);
            var overset = engine.Snapshot();
            check(!overset.SetPinStates[0] && overset.Cues.Count(c => c.Sound == PickSound.LostSet) == 1,
                "Continued lift oversets and emits one loss-of-set cue");
            check(overset.Wear > 0, "Continued forcing still wears the pick");
            var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<PickSnapshot>(Newtonsoft.Json.JsonConvert.SerializeObject(overset));
            check(PickPresentation.SetCount(restored) == 0 && restored.Cues.Any(c => c.Sound == PickSound.LostSet),
                "Snapshot recovery restores current progress and sequenced loss feedback");
            for (var i = 0; i < fps; i++) engine.Advance(1f / fps, 0, 0, true, .2f);
            check(!engine.Snapshot().SetPinStates[0] && engine.Coaching().State == PinState.Unsettled,
                "Overset recovery remains available at controlled pressure");

            foreach (var delay in new[] { 0f, .1f, .2f })
            {
                var probe = new PinLockEngine(new PinLockDefinition { Heights = [.5f, .5f, .5f], Order = [1, 0, 2] }, config, 1, 0);
                var deliveries = new Queue<(float Due, PickSnapshot State)>();
                var latest = probe.Snapshot();
                float command = 0, nextSnapshot = 0;
                for (var frame = 0; frame < fps * 2; frame++)
                {
                    var now = frame / (float)fps;
                    if (now >= nextSnapshot)
                    {
                        deliveries.Enqueue((now + delay, probe.Snapshot()));
                        nextSnapshot = now + .05f;
                    }
                    while (deliveries.TryPeek(out var queued) && queued.Due <= now) latest = deliveries.Dequeue().State;
                    command = PickInput.Lift(command, latest.Lift, 120f / fps, 1, true);
                    check(command <= latest.Lift + .050001f, "Input lead stays bounded with delayed 20Hz snapshots");
                    probe.Advance(1f / fps, 0, command, true);
                }
                check(probe.Wear > 0, "Bounded input does not make persistent forcing safe");
                var reversed = PickInput.Lift(command, latest.Lift, -.1f, 1, true);
                check(reversed < latest.Lift, "First reverse movement lowers the command instead of consuming queued lift");
            }
        }
        check(Math.Abs(PickInput.Lift(0, 0, 1, 1, true) * 4 - PickInput.Lift(0, 0, 1, 1, false)) < .00001f,
            "Fine control uses quarter-speed lift");
        check(Math.Abs(PickInput.Pressure(.35f, 1, true) - .36f) < .00001f
            && Math.Abs(PickInput.Pressure(.35f, -1, false) - .30f) < .00001f,
            "Fine and normal pressure use one and five percent steps");
        check(PickInput.Lift(.5f, float.NaN, float.NaN, 1, false) <= .05f
            && PickInput.Pressure(.99f, 100, true) == 1, "Input shaping clamps invalid and out-of-range values");
        var pressureOnly = new PinLockEngine(new PinLockDefinition { Heights = [.5f, .5f, .5f], Order = [0, 1, 2] }, config, 1, 0);
        for (var i = 0; i < 600; i++) pressureOnly.Advance(1f / 60, 0, 0, true, .9f);
        check(pressureOnly.Wear == 0 && pressureOnly.Snapshot().Strain == 0, "High pressure alone still causes no damage");
        pressureOnly.Advance(1f / 60, 0, 0, true, .91f);
        check(pressureOnly.Snapshot().Cues.Any(c => c.Sound == PickSound.AdjustTension), "Fine pressure steps produce adjustment cues");
    }
}
