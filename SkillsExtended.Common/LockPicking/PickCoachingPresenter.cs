namespace SkillsExtended.LockPicking;

public sealed class PickGuidance
{
    public string Instruction { get; set; }
    public int RecommendedPin { get; set; } = -1;
    public float LiftMin { get; set; }
    public float LiftMax { get; set; }
    public float PressureMin { get; set; }
    public float PressureMax { get; set; }
}

/// <summary>Picking advice only; never controls or advances the simulation.</summary>
public static class PickCoachingPresenter
{
    public static PickGuidance Present(PickSnapshot state, PickCoaching coaching, float commandLift)
    {
        var result = new PickGuidance
        {
            RecommendedPin = coaching.State == PinState.Overset ? state.Selected : coaching.BindingPin,
            LiftMin = coaching.LiftMin, LiftMax = coaching.LiftMax,
            PressureMin = coaching.PressureMin, PressureMax = coaching.PressureMax,
        };
        if (state.Outcome != PickOutcome.Active)
        {
            result.RecommendedPin = -1;
            result.Instruction = state.Outcome == PickOutcome.Unlocked ? "Lock released"
                : state.Outcome == PickOutcome.PickBroken ? "Pick broken — retry to practice again" : "Practice ended";
            return result;
        }
        var pin = result.RecommendedPin + 1;
        if (coaching.State != PinState.Overset && state.Selected != result.RecommendedPin)
        {
            result.LiftMin = 0;
            result.LiftMax = PinLockEngine.MoveLiftLimit / 2;
            result.PressureMin = .25f;
            result.PressureMax = .35f;
            result.Instruction = state.Lift >= PinLockEngine.MoveLiftLimit || commandLift >= PinLockEngine.MoveLiftLimit
                ? $"Lower the pick fully, then move to pin {pin}"
                : $"Move {(result.RecommendedPin > state.Selected ? "right" : "left")} to pin {pin}";
        }
        else if (!state.Tension)
            result.Instruction = $"Pin {pin} — apply tension using the control below";
        else if (state.TensionStrength < result.PressureMin - .001f || state.TensionStrength > result.PressureMax + .001f)
            result.Instruction = $"Pin {pin} — {(state.TensionStrength < result.PressureMin ? "increase" : "ease")} tension to {result.PressureMin:P0}–{result.PressureMax:P0}";
        else if (coaching.State == PinState.Overset)
            result.Instruction = $"Pin {pin} overset — lower the pick into the lift band";
        else if (coaching.State == PinState.Caught)
            result.Instruction = coaching.Type == PinType.Spool
                ? $"Pin {pin} spool catch — lift through; allow counter-rotation"
                : $"Pin {pin} serration catch — continue lifting through the click";
        else if (commandLift > result.LiftMax)
            result.Instruction = $"Pin {pin} — lower your input into the lift band";
        else
            result.Instruction = $"Pin {pin} — lift into the target band; stop at the set click";
        return result;
    }
}
