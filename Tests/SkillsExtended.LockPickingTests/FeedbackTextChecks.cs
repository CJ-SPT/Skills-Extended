using SkillsExtended.LockPicking;

internal static class FeedbackTextChecks
{
    public static void Run(Action<bool, string> check)
    {
        var text = new PickFeedbackPresenter();
        var state = new PickSnapshot { Pins = 3, ElapsedSeconds = 10 };
        void Cue(PickSound sound, int pin = -1)
        {
            state.Cues = [new PickCue { Sequence = ++state.Cue, Sound = sound, Pin = pin, Time = state.ElapsedSeconds }];
        }
        text.Update(state, 0, 10);
        check(text.Status == "No turning pressure" && text.Detail.Contains("relaxed"), "Idle explains the relaxed wrench");
        state.Lift = .2f;
        text.Update(state, .2f, 10.1f);
        check(text.Status == "Lifting without turning pressure", "Lift without torque has its own sensation");
        state.Tension = true;
        state.TensionStrength = .9f;
        state.Wear = .4f;
        text.Reset();
        text.Update(state, .2f, 10.2f);
        check(!text.Status.Contains("wear") && !text.Detail.Contains("damaging"),
            "High pressure and existing pick wear do not imply active damage");
        Cue(PickSound.Tension);
        text.Update(state, .2f, 10.3f);
        check(text.Detail.Contains("applied"), "Applying tension is described");
        Cue(PickSound.Catch, 1);
        text.Update(state, .2f, 10.4f);
        var caught = text.Detail;
        check(caught.Contains("Pin 2") && caught.Contains("light tick"), "Catch text identifies observed pin and sensation");
        Cue(PickSound.AdjustTension);
        text.Update(state, .2f, 11);
        check(text.Detail == caught, "Catch remains readable beyond 0.4s and outranks pressure adjustment");
        text.Update(state, .2f, 12.1f);
        check(text.Detail.Contains("held at"), "Repeated snapshot does not restart expired event captions");
        Cue(PickSound.Seat, 0);
        text.Update(state, .2f, 12.2f);
        check(text.Detail.Contains("firm click") && text.Detail.Contains("Pin 1 set"), "Seating confirms achieved progress");
        state.Feedback = PickFeedback.Strain;
        text.Update(state, .8f, 12.3f);
        check(text.Detail.Contains("barely moving"), "Current resistance outranks the recent set caption");
        state.Feedback = PickFeedback.Springy;
        Cue(PickSound.LostSet, 0);
        text.Update(state, .2f, 12.4f);
        check(text.Detail == "Pin 1 lost its set.", "Oversetting supersedes an earlier set caption");
        Cue(PickSound.Seat, 0);
        text.Update(state, .2f, 12.6f);
        check(text.Detail.StartsWith("Pin 1 set"), "A later restored set replaces the earlier loss caption");
        state.ElapsedSeconds += 2;
        Cue(PickSound.Catch, 2);
        state.Cues[0].Time -= 1;
        text.Update(state, .2f, 14.3f);
        check(!text.Detail.Contains("tick"), "Stale network cues do not become fresh captions");

        state.Cues = [
            new PickCue { Sequence = ++state.Cue, Sound = PickSound.Drop, Pin = 0, Time = state.ElapsedSeconds },
            new PickCue { Sequence = ++state.Cue, Sound = PickSound.Drop, Pin = 1, Time = state.ElapsedSeconds },
            new PickCue { Sequence = ++state.Cue, Sound = PickSound.CounterRotation, Pin = 2, Time = state.ElapsedSeconds },
        ];
        state.Feedback = PickFeedback.CounterRotation;
        text.Update(state, .4f, 14.4f);
        check(text.Status == "Wrench pressing back" && text.Detail == "2 pins slipped down.",
            "Simultaneous drops coalesce while the persistent status still explains wrench resistance");
        Cue(PickSound.Release);
        state.Tension = false;
        state.Feedback = PickFeedback.Searching;
        text.Update(state, 0, 14.5f);
        check(text.Detail.Contains("released"), "Release supersedes the preceding mechanical event");

        text.Reset();
        state.Cues = [];
        state.Tension = true;
        state.Feedback = PickFeedback.Strain;
        text.Update(state, .6f, 20);
        check(text.Status == "Resisting lift" && text.Detail.Contains("barely moving"), "Strain explains stalled movement before damage");
        state.Wear += .01f;
        text.Update(state, .6f, 20.1f);
        check(text.Status == "Pick wearing under strain" && text.Detail.Contains("damaging"), "Measured wear loss triggers the damage message");
        state.Feedback = PickFeedback.Springy;
        text.Update(state, 0, 20.8f);
        check(text.Status == "Lowering the pick" && !text.Detail.Contains("damaging"), "Damage message clears after wear stops");
        state.PinMotion = [new PickPinMotion { KeyLift = .2f, DriverLift = .5f }];
        text.Update(state, .2f, 21);
        check(text.Status.Contains("Little spring pressure") && text.Detail.Contains("remains raised"),
            "Retained motion explains light contact without claiming the pin is set");

        state.Outcome = PickOutcome.Unlocked;
        text.Update(state, .2f, 21.1f);
        check(text.Status == "Lock released" && text.Detail.Contains("freely"), "Terminal result replaces transient sensations");
        state = new PickSnapshot { Pins = 3 };
        Cue(PickSound.Catch, 0);
        text.Update(state, 0, 22);
        check(text.Detail.Contains("light tick"), "Retry after completion accepts restarted cue sequences");
        text.Reset();
        state.Cues = [];
        text.Update(state, 0, 22.1f);
        check(!text.Detail.Contains("tick"), "Explicit retry clears event and damage history");
    }
}
