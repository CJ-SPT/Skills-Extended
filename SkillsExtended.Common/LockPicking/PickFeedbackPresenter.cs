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
            Status = state.Outcome == PickOutcome.Unlocked ? "Lock released"
                : state.Outcome == PickOutcome.PickBroken ? "Pick broken — the key still works" : "Picking stopped";
            Detail = state.Outcome == PickOutcome.Unlocked ? "The plug turns freely."
                : state.Outcome == PickOutcome.PickBroken ? "The pick gave way under strain." : "";
            return;
        }

        var drops = new HashSet<int>();
        foreach (var cue in fresh)
            if (PickPresentation.Fresh(state, cue) && cue.Sound == PickSound.Drop)
                drops.Add(cue.Pin);
        foreach (var cue in fresh)
        {
            if (!PickPresentation.Fresh(state, cue)) continue;
            var pin = cue.Pin >= 0 && cue.Pin < state.Pins ? $"Pin {cue.Pin + 1}" : "A pin";
            switch (cue.Sound)
            {
                case PickSound.Release:
                    Show("Turning pressure released — pins lose support.", 90, now);
                    break;
                case PickSound.Drop:
                    Show(drops.Count > 1 ? $"{drops.Count} pins slipped down."
                        : $"{pin} slipped down.", 80, now);
                    break;
                case PickSound.Seat:
                    Show($"{pin} set — a firm click under the pick.", 75, now);
                    break;
                case PickSound.LostSet:
                    Show($"{pin} lost its set.", 85, now);
                    break;
                case PickSound.Overset:
                    Show($"{pin} overset — ease tension and lower the pick.", 85, now, cue.Sound, cue.Pin);
                    break;
                case PickSound.Recovered:
                    Show($"{pin} overset cleared.", 86, now);
                    break;
                case PickSound.CatchCleared:
                    Show($"{pin} catch cleared.", 70, now);
                    break;
                case PickSound.FalseSet:
                    Show("False set — the plug turned, but the lock is still locked.", 78, now, cue.Sound);
                    break;
                case PickSound.CounterRotation:
                    Show("Counter-rotation — the wrench turned back as the pin lifted.", 70, now);
                    break;
                case PickSound.Catch:
                    Show($"{pin} caught — a light tick, not a true set.", 65, now);
                    break;
                case PickSound.Tension:
                    Show("Turning pressure applied through the wrench.", 30, now);
                    break;
                case PickSound.AdjustTension:
                    Show("Adjusting turning pressure through the wrench.", 20, now);
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
        var pinName = $"Pin {state.Selected + 1}";
        var overset = state.Tension && state.SelectedPinState == PinState.Overset;
        var caught = state.Tension && state.SelectedPinState == PinState.Caught;
        Status = damage ? "Pick wearing under strain"
            : state.Feedback == PickFeedback.Strain ? "Resisting lift"
            : state.Feedback == PickFeedback.CounterRotation ? "Wrench pressing back"
            : lowering ? "Lowering the pick"
            : !state.Tension ? (state.Lift > .08f ? "Lifting without turning pressure" : "No turning pressure")
            : state.Feedback == PickFeedback.Binding ? $"Resistance under pin {state.Selected + 1}"
            : state.Lift <= .08f ? $"Pick beneath pin {state.Selected + 1}"
            : unsupported ? "Little spring pressure under the pick"
            : "Spring pressure under the pick";

        // Damage remains distinct from pressure and is reported only after measured wear loss.
        Detail = damage ? (overset ? $"{pinName} overset — forcing is damaging the pick."
                : "Continued force is damaging the pick — ease your lift.")
            : overset ? $"{pinName} overset — ease tension and lower the pick."
            : state.Feedback == PickFeedback.Strain ? (caught ? $"{pinName} caught — resisting lift. Ease tension."
                : "More lift is requested, but the pin is barely moving.")
            : now < _until && _priority >= 60 ? _event
            : state.Feedback == PickFeedback.CounterRotation ? "Counter-rotation — the pin is pushing back against the wrench."
            : state.FalseSet ? "False set — the plug turned, but the lock is still locked."
            : caught ? $"{pinName} caught — not a true set. Ease tension to work through it."
            : now < _until ? _event
            : state.Feedback == PickFeedback.Binding ? $"{pinName} binding — resistance under the pick."
            : unsupported ? "The upper pin remains raised as the lower pin moves away."
            : !state.Tension ? "The wrench is relaxed; pins are free to return."
            : $"Turning pressure held at {state.TensionStrength:P0}.";
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
