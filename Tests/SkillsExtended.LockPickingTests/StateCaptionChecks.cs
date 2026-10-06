using SkillsExtended.Config.Skills;
using SkillsExtended.LockPicking;

internal static class StateCaptionChecks
{
    public static void Run(Action<bool, string> check)
    {
        var definition = new PinLockDefinition
        {
            Heights = [.5f, .5f, .5f], Order = [0, 1, 2],
            Types = [PinType.Standard, PinType.Standard, PinType.Spool], Catches = [[], [], [.25f]],
        };
        var engine = new PinLockEngine(definition, new LockPickingData(), 1, 0);
        void Advance(float depth, float lift, float pressure, int frames = 90)
        {
            for (var frame = 0; frame < frames; frame++)
                engine.Advance(1f / 60, depth, lift, pressure > 0, pressure);
        }
        var text = new PickFeedbackPresenter();
        Advance(0, .5f, .35f);
        Advance(0, 1, .35f, 20);
        var overset = engine.Snapshot();
        check(overset.SelectedPinState == PinState.Overset && !overset.SetPinStates[0]
            && overset.Cues.Count(c => c.Sound == PickSound.Overset) == 1, "Overset snapshot and one-shot event report the actual state");
        text.Update(overset, 1, 1);
        check(text.Detail.Contains("Pin 1 overset") && text.Detail.Contains("lower the pick"),
            "Overset explanation occupies the set-notification line even during resistance");
        text.Update(overset, 1, 4);
        check(text.Detail.Contains("overset"), "Overset remains readable after the event caption expires");
        Advance(0, 1, .35f, 30);
        text.Update(engine.Snapshot(), 1, 4.1f);
        check(text.Detail.Contains("overset") && text.Detail.Contains("damaging"),
            "Active wear warning retains the overset explanation");
        for (var i = 0; i < 120 && engine.Snapshot().SelectedPinState == PinState.Overset; i++)
            Advance(0, 0, .2f, 1);
        var recovered = engine.Snapshot();
        check(recovered.SelectedPinState == PinState.Unsettled
            && recovered.Cues.Count(c => c.Sound == PickSound.Recovered) == 1, "Lowering at controlled pressure emits one recovery event");
        text.Update(recovered, 0, 5);
        check(text.Detail == "Pin 1 overset cleared.", "Recovery supersedes the older overset caption");

        // Apply tension with the pick already too high: no preceding confirmed set exists.
        engine = new PinLockEngine(definition, new LockPickingData(), 1, 0);
        Advance(0, 1, 0, 120);
        Advance(0, 1, .35f, 5);
        check(engine.Snapshot().SelectedPinState == PinState.Overset
            && engine.Snapshot().Cues.Any(c => c.Sound == PickSound.Overset)
            && !engine.Snapshot().Cues.Any(c => c.Sound == PickSound.LostSet),
            "Direct oversetting is announced even without an earlier set");

        engine = new PinLockEngine(definition, new LockPickingData(), 1, 0);
        Advance(0, .5f, .35f);
        Advance(.5f, 0, .35f);
        Advance(.5f, .5f, .35f);
        Advance(.9f, 0, .35f);
        Advance(.9f, .25f, .35f);
        var falseSet = engine.Snapshot();
        check(falseSet.FalseSet && falseSet.SelectedPinState == PinState.Caught
            && falseSet.Cues.Count(c => c.Sound == PickSound.FalseSet) == 1, "Whole-lock false set differs from an individual catch");
        var restored = Newtonsoft.Json.JsonConvert.DeserializeObject<PickSnapshot>(Newtonsoft.Json.JsonConvert.SerializeObject(falseSet));
        restored.Cues = [];
        text.Reset();
        text.Update(restored, .25f, 10);
        check(text.Detail.StartsWith("False set"), "Recovered snapshot restores a persistent false-set indication without replaying an event");
        text.Update(restored, .25f, 13);
        check(text.Detail.StartsWith("False set"), "False-set indication does not expire while the lock remains in it");
        restored.Cues = [new PickCue { Sequence = ++restored.Cue, Sound = PickSound.AdjustTension, Time = restored.ElapsedSeconds }];
        text.Update(restored, .25f, 13.1f);
        check(text.Detail.StartsWith("False set"), "Routine pressure captions cannot hide a persistent false set");
        Advance(.9f, .25f, .35f);
        check(engine.Snapshot().Cues.Count(c => c.Sound == PickSound.FalseSet) == 1, "Steady false set does not flood the event queue");
        Advance(.9f, .26f, .2f, 15);
        check(engine.Snapshot().SelectedPinState == PinState.Caught && engine.Snapshot().FalseSet,
            "A caught spool remains a false set until its catch clears");
        Advance(.9f, .35f, .2f, 12);
        var cleared = engine.Snapshot();
        check(!cleared.FalseSet && cleared.Cues.Any(c => c.Sound == PickSound.CatchCleared),
            "Clearing the spool exits the false set and emits a catch-clear event");
        text.Update(cleared, .35f, 14);
        check(!text.Detail.StartsWith("False set"), "Counter-rotation and cleared-catch feedback replace false-set text");

        // A missed exit event must not leave the last false-set notification on screen.
        text.Reset();
        restored.Cues = [new PickCue { Sequence = restored.Cue + 1, Sound = PickSound.FalseSet, Time = restored.ElapsedSeconds }];
        restored.Cue++;
        text.Update(restored, .25f, 20);
        restored.FalseSet = false;
        restored.SelectedPinState = PinState.Unsettled;
        restored.Cues = [];
        text.Update(restored, .25f, 20.1f);
        check(!text.Detail.StartsWith("False set"), "Snapshot correction clears stale false-set caption before its timer expires");
        Advance(.9f, 0, 0, 1);
        var release = engine.Snapshot();
        text.Update(release, 0, 21);
        check(!release.FalseSet && release.SelectedPinState == PinState.Unsettled && text.Detail.Contains("released"),
            "Full release clears mechanical state and restores release feedback");

        // Serration catches alone must never be labeled as the whole lock's false set.
        definition.Types = [PinType.Serrated, PinType.Standard, PinType.Standard];
        definition.Catches = [[.2f, .3f], [], []];
        engine = new PinLockEngine(definition, new LockPickingData(), 1, 0);
        Advance(0, .2f, .35f);
        text.Reset();
        text.Update(engine.Snapshot(), .2f, 30);
        check(!engine.Snapshot().FalseSet && text.Detail.Contains("caught") && !text.Detail.StartsWith("False set"),
            "Serration catch is explicitly caught, not falsely described as a whole-lock false set");
        Advance(0, .34f, .2f, 12);
        text.Update(engine.Snapshot(), .34f, 30.2f);
        check(text.Detail.Contains("catch cleared"), "Working past an individual catch is explained");
        text.Reset();
        text.Update(new PinLockEngine(definition, new LockPickingData(), 1, 0).Snapshot(), 0, 31);
        check(!text.Detail.Contains("overset") && !text.Detail.Contains("False set") && !text.Detail.Contains("caught"),
            "Retry clears prior state notifications");
    }
}
