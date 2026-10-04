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
    }
}
