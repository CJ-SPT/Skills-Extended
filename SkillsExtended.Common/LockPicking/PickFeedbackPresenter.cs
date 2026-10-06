using System.Collections.Generic;

namespace SkillsExtended.LockPicking;

/// <summary>Readable sensations from public observations, without coaching or secret geometry.</summary>
public sealed class PickFeedbackPresenter
{
    public const float EventSeconds = 1.6f;
    public string Status { get; private set; }
    public string Detail { get; private set; }
    private readonly PickCueReader _cues = new();
    private string _event;
    private PickSound? _eventCondition;
    private int _eventPin;
    private int _priority;
    private float _until, _wear, _damageUntil, _shownAt;
    private bool _observed;
    private PickOutcome _outcome;

    public void Reset()
    {
        _cues.Reset();
        _event = Status = Detail = null;
        _eventCondition = null;
        _priority = 0;
        _until = _wear = _damageUntil = _shownAt = 0;
        _observed = false;
    }

    public void Update(PickSnapshot state, float commandLift, float now)
    {
        if (_observed && _outcome != PickOutcome.Active && state.Outcome == PickOutcome.Active)
            Reset();
        if (_observed && state.Wear > _wear + .000001f)
            _damageUntil = now + .6f;
        _observed = true;
        _wear = state.Wear;
        _outcome = state.Outcome;
        var fresh = _cues.Read(state);
        if (state.Outcome != PickOutcome.Active)
        {
            _event = null;
            _until = _damageUntil = 0;
            Status = state.Outcome == PickOutcome.Unlocked ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.LockReleased")
                : state.Outcome == PickOutcome.PickBroken ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.PickBrokenTheKeyStillWorks") : LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.PickingStopped");
            Detail = state.Outcome == PickOutcome.Unlocked ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.ThePlugTurnsFreely")
                : state.Outcome == PickOutcome.PickBroken ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.ThePickGaveWayUnderStrain") : "";
            return;
        }

        var drops = new HashSet<int>();
        foreach (var cue in fresh)
            if (PickPresentation.Fresh(state, cue) && cue.Sound == PickSound.Drop)
                drops.Add(cue.Pin);
        foreach (var cue in fresh)
        {
            if (!PickPresentation.Fresh(state, cue)) continue;
            var pin = cue.Pin >= 0 && cue.Pin < state.Pins ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.Pin", cue.Pin + 1) : LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.APin");
            switch (cue.Sound)
            {
                case PickSound.Release:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.TurningPressureReleasedPinsLoseSupport"), 90, now);
                    break;
                case PickSound.Drop:
                    Show(drops.Count > 1 ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.PinsSlippedDown", drops.Count)
                        : LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.SlippedDown", pin), 80, now);
                    break;
                case PickSound.Seat:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.SetAFirmClickUnderThePick", pin), 75, now);
                    break;
                case PickSound.LostSet:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.LostItsSet", pin), 85, now);
                    break;
                case PickSound.Overset:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.OversetEaseTensionAndLowerThePick", pin), 85, now, cue.Sound, cue.Pin);
                    break;
                case PickSound.Recovered:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.OversetCleared", pin), 86, now);
                    break;
                case PickSound.CatchCleared:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.CatchCleared", pin), 70, now);
                    break;
                case PickSound.FalseSet:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.FalseSetThePlugTurnedButTheLockIs"), 78, now, cue.Sound);
                    break;
                case PickSound.CounterRotation:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.CounterRotationTheWrenchTurnedBackAsThePin"), 70, now);
                    break;
                case PickSound.Catch:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.CaughtALightTickNotATrueSet", pin), 65, now);
                    break;
                case PickSound.Tension:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.TurningPressureAppliedThroughTheWrench"), 30, now);
                    break;
                case PickSound.AdjustTension:
                    Show(LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.AdjustingTurningPressureThroughTheWrench"), 20, now);
                    break;
            }
        }

        // Snapshot recovery can clear a condition even when its brief exit cue was missed.
        if (_eventCondition == PickSound.FalseSet && !state.FalseSet
            || _eventCondition == PickSound.Overset && _eventPin == state.Selected
                && state.SelectedPinState != PinState.Overset)
            _until = 0;

        var lowering = PickPresentation.Unit(commandLift) + .025f < state.Lift;
        var motion = state.PinMotion != null && state.Selected >= 0 && state.Selected < state.PinMotion.Length
            ? state.PinMotion[state.Selected] : null;
        var unsupported = motion != null && motion.DriverLift > motion.KeyLift + .025f;
        var damage = now < _damageUntil;
        var pinName = LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.Pin", state.Selected + 1);
        var overset = state.Tension && state.SelectedPinState == PinState.Overset;
        var caught = state.Tension && state.SelectedPinState == PinState.Caught;
        Status = damage ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.PickWearingUnderStrain")
            : state.Feedback == PickFeedback.Strain ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.ResistingLift")
            : state.Feedback == PickFeedback.CounterRotation ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.WrenchPressingBack")
            : lowering ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.LoweringThePick")
            : !state.Tension ? (state.Lift > .08f ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.LiftingWithoutTurningPressure") : LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.NoTurningPressure"))
            : state.Feedback == PickFeedback.Binding ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.ResistanceUnderPin", state.Selected + 1)
            : state.Lift <= .08f ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.PickBeneathPin", state.Selected + 1)
            : unsupported ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.LittleSpringPressureUnderThePick")
            : LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.SpringPressureUnderThePick");

        // Damage remains distinct from pressure and is reported only after measured wear loss.
        Detail = damage ? (overset ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.OversetForcingIsDamagingThePick", pinName)
                : LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.ContinuedForceIsDamagingThePickEaseYourLift"))
            : overset ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.OversetEaseTensionAndLowerThePick", pinName)
            : state.Feedback == PickFeedback.Strain ? (caught ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.CaughtResistingLiftEaseTension", pinName)
                : LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.MoreLiftIsRequestedButThePinIsBarely"))
            : now < _until && _priority >= 60 ? _event
            : state.Feedback == PickFeedback.CounterRotation ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.CounterRotationThePinIsPushingBackAgainstThe")
            : state.FalseSet ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.FalseSetThePlugTurnedButTheLockIs")
            : caught ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.CaughtNotATrueSetEaseTensionToWork", pinName)
            : now < _until ? _event
            : state.Feedback == PickFeedback.Binding ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.BindingResistanceUnderThePick", pinName)
            : unsupported ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.TheUpperPinRemainsRaisedAsTheLowerPin")
            : !state.Tension ? LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.TheWrenchIsRelaxedPinsAreFreeToReturn")
            : LocalizedText.Get("SkillsExtended.PickFeedbackPresenter.TurningPressureHeldAt", state.TensionStrength);
    }

    private void Show(string text, int priority, float now, PickSound? condition = null, int pin = -1)
    {
        // Do not let repeated pressure adjustments bury a click or a loss of support.
        if (now < _until && priority < _priority && (priority < 60 || now <= _shownAt + .001f)) return;
        _event = text;
        _eventCondition = condition;
        _eventPin = pin;
        _priority = priority;
        _shownAt = now;
        _until = now + EventSeconds;
    }
}
