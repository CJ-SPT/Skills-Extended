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
            result.Instruction = state.Outcome == PickOutcome.Unlocked ? LocalizedText.Get("SkillsExtended.PickCoachingPresenter.LockReleased")
                : state.Outcome == PickOutcome.PickBroken ? LocalizedText.Get("SkillsExtended.PickCoachingPresenter.PickBrokenRetryToPracticeAgain") : LocalizedText.Get("SkillsExtended.PickCoachingPresenter.PracticeEnded");
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
                ? LocalizedText.Get("SkillsExtended.PickCoachingPresenter.LowerThePickFullyThenMoveToPin", pin)
                : LocalizedText.Get(result.RecommendedPin > state.Selected ? "SkillsExtended.PickCoachingPresenter.MoveRightToPin" : "SkillsExtended.PickCoachingPresenter.MoveLeftToPin", pin);
        }
        else if (!state.Tension)
            result.Instruction = LocalizedText.Get("SkillsExtended.PickCoachingPresenter.PinApplyTensionUsingTheControlBelow", pin);
        else if (state.TensionStrength < result.PressureMin - .001f || state.TensionStrength > result.PressureMax + .001f)
            result.Instruction = LocalizedText.Get(state.TensionStrength < result.PressureMin ? "SkillsExtended.PickCoachingPresenter.IncreaseTension" : "SkillsExtended.PickCoachingPresenter.EaseTension", pin, result.PressureMin, result.PressureMax);
        else if (coaching.State == PinState.Overset)
            result.Instruction = LocalizedText.Get("SkillsExtended.PickCoachingPresenter.PinOversetLowerThePickIntoTheLiftBand", pin);
        else if (coaching.State == PinState.Caught)
            result.Instruction = coaching.Type == PinType.Spool
                ? LocalizedText.Get("SkillsExtended.PickCoachingPresenter.PinSpoolCatchLiftThroughAllowCounterRotation", pin)
                : LocalizedText.Get("SkillsExtended.PickCoachingPresenter.PinSerrationCatchContinueLiftingThroughTheClick", pin);
        else if (commandLift > result.LiftMax)
            result.Instruction = LocalizedText.Get("SkillsExtended.PickCoachingPresenter.PinLowerYourInputIntoTheLiftBand", pin);
        else
            result.Instruction = LocalizedText.Get("SkillsExtended.PickCoachingPresenter.PinLiftIntoTheTargetBandStopAtThe", pin);
        return result;
    }
}
