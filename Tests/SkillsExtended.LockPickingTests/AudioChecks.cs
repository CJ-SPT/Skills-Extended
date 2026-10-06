using SkillsExtended.LockPicking;
using SkillsExtended.Skills.LockPicking;
using UnityEngine;

internal static class AudioChecks
{
    public static void Run(Action<bool, string> check)
    {
        for (var raid = 0; raid < 4; raid++)
        {
            var owner = new GameObject();
            var audio = new LockPickingAudio(owner);
            var bank = AudioClip.Created.Where(c => c).ToArray();
            check(bank.Length == 18 && bank.All(c => c.Samples.Length > 0),
                "Each attempt loads all 18 real recordings without retaining previous banks");
            AudioClip.UnloadUnused();
            check(bank.All(c => c), "Unused-asset cleanup preserves the active recording bank");
            var state = new PickSnapshot { Outcome = PickOutcome.Active };
            Time.unscaledTime = 0;
            audio.Tick(state);
            Time.unscaledTime = 1;
            state.Lift = .5f;
            audio.Tick(state);
            check(owner.Sources[0].Plays == 1, "Movement recording plays after cleanup");
            state.Cue = 1;
            state.Cues = [new PickCue { Sequence = 1, Sound = PickSound.Click }];
            audio.Tick(state);
            check(owner.Sources[1].Plays == 1, "Pin feedback plays after cleanup");
            state.Outcome = PickOutcome.Unlocked;
            audio.Tick(state);
            check(owner.Sources[1].Plays == 2, "Terminal recording plays");
            audio.Dispose();
            audio.Dispose();
            check(bank.All(c => !c) && owner.Sources.All(s => !s),
                "Closing releases every native clip and source, including repeated disposal");
            AudioClip.UnloadUnused();
        }
        AudioClip.FailNextSetData = true;
        try
        {
            using var failed = new LockPickingAudio(new GameObject());
            check(false, "Failed audio upload should reject initialization");
        }
        catch (InvalidDataException)
        {
            check(AudioClip.Created.All(c => !c), "Failed preload releases its partial bank");
        }
        using (var retry = new LockPickingAudio(new GameObject()))
            check(AudioClip.Created.Count(c => c) == 18, "Preload can recover on the next attempt");
        check(AudioClip.Created.All(c => !c), "No recording remains allocated after all attempts");
        CheckMechanicalEvents(check);
    }

    private static void CheckMechanicalEvents(Action<bool, string> check)
    {
        var owner = new GameObject();
        using var audio = new LockPickingAudio(owner);
        var state = new PickSnapshot { Pins = 3 };
        Time.unscaledTime = 0;
        audio.Tick(state);
        var movement = owner.Sources[0];
        var feedback = owner.Sources[1];
        void Event(float time, params PickSound[] sounds)
        {
            Time.unscaledTime = state.ElapsedSeconds = time;
            state.Cues = sounds.Select(sound => new PickCue
            {
                Sequence = ++state.Cue, Sound = sound, Time = time, Pin = 1,
            }).ToArray();
            audio.Tick(state);
        }
        Event(.1f, PickSound.AdjustTension);
        check(feedback.clip.Name == "Tension01" && feedback.volume < .3f,
            "Pressure adjustment uses restrained recorded feedback");
        Event(.2f, PickSound.Catch);
        check(feedback.clip.Name == "Pin02", "A catch promptly interrupts lower-priority pressure audio");
        Event(.3f, PickSound.Seat);
        check(feedback.clip.Name == "Pin01" && feedback.pitch == 1,
            "Seating uses a distinct natural-pitch recording");
        var played = feedback.Plays;
        audio.Tick(state);
        check(feedback.Plays == played, "Repeated network snapshot cannot replay the seating sound");
        Event(.32f, PickSound.AdjustTension);
        check(feedback.Plays == played, "A pressure adjustment does not interrupt seating");
        Time.unscaledTime = state.ElapsedSeconds = .8f;
        feedback.Stop();
        audio.Tick(state);
        check(feedback.Plays == played, "Expired queued adjustment is discarded instead of playing late");

        Event(.9f, PickSound.Drop, PickSound.Drop, PickSound.Drop);
        check(feedback.Plays == played + 1 && feedback.clip.Name.StartsWith("Release"),
            "Several dropped stacks produce one synchronized release sound");
        feedback.Stop();
        Time.unscaledTime = 1;
        audio.Tick(state);
        check(feedback.Plays == played + 1, "Coalesced drops do not leave an audible backlog");
        Event(1.1f, PickSound.CounterRotation);
        check(feedback.clip.Name == "Tension01", "Backward wrench motion receives its mechanical cue");
        Event(1.3f, PickSound.Release);
        check(feedback.clip.Name.StartsWith("Release"), "Full release preempts earlier mechanical feedback");
        played = feedback.Plays;
        feedback.Stop();
        state.Cues = [new PickCue { Sequence = ++state.Cue, Sound = PickSound.Catch, Time = 0 }];
        Time.unscaledTime = state.ElapsedSeconds = 2;
        audio.Tick(state);
        check(feedback.Plays == played, "Old server cue history is silent even when first received now");

        SkillsExtended.Config.ConfigManager.LockPickingVolume.Value = 0;
        Event(2.2f, PickSound.Catch);
        check(feedback.volume == 0 && movement.volume == 0, "Muted audio still consumes cues at zero volume");
        SkillsExtended.Config.ConfigManager.LockPickingVolume.Value = 100;
        played = feedback.Plays;
        audio.Tick(state);
        check(feedback.Plays == played, "Unmuting does not replay consumed mechanical events");
        state.Outcome = PickOutcome.PickBroken;
        audio.Tick(state);
        check(feedback.clip.Name == "Break01", "Terminal break immediately replaces active feedback");
        state = new PickSnapshot { Pins = 3 };
        audio.Tick(state);
        check(!feedback.isPlaying && !movement.isPlaying, "Retry stops old recordings and clears cue history");
        Event(2.5f, PickSound.Seat);
        check(feedback.clip.Name == "Pin01", "Retry accepts the new attempt's restarted sequence");
        Event(2.6f, PickSound.LostSet);
        check(feedback.clip.Name.StartsWith("Release"), "Loss of a true set interrupts the earlier seating sound");
        played = feedback.Plays;
        audio.Tick(state);
        check(feedback.Plays == played, "Duplicate loss-of-set cues are not replayed");
    }
}
