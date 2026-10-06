using System;

namespace SkillsExtended.LockPicking;

/// <summary>Sensory presentation only; never feeds back into picking simulation.</summary>
public static class PickPresentation
{
    public const float CueLifetime = .30f;
    public static float Unit(float value) => PinLockEngine.Finite(value) ? Math.Max(0, Math.Min(1, value)) : 0;

    // Physical plug travel: working movement is small; only unlocking reaches a quarter turn.
    public static float RotationDegrees(float rotation) => Unit(rotation) * 90;

    public static bool Fresh(PickSnapshot state, PickCue cue) =>
        state.ElapsedSeconds - cue.Time <= CueLifetime;

    public static bool TrueSet(PickSnapshot state, int pin) =>
        state.SetPinStates != null && state.SetPinStates.Length == state.Pins
        && pin >= 0 && pin < state.Pins && state.SetPinStates[pin]
        && (state.Tension || state.Outcome == PickOutcome.Unlocked);

    public static int SetCount(PickSnapshot state)
    {
        var count = 0;
        for (var pin = 0; pin < state.Pins; pin++)
            if (TrueSet(state, pin)) count++;
        return count;
    }
}
