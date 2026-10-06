using SkillsExtended.LockPicking;

// Test-only player model. This class receives snapshots and elapsed input time only.
// It cannot inspect an engine, lock definition, coaching, or the fixture's seed.
internal sealed class PublicPickingController
{
    private readonly PickCueReader _cues = new();
    private readonly bool[] _heardCatch = new bool[5];
    private float _lift, _pressure = .35f, _notchClock;
    private bool _lowering = true, _advance, _recovering;
    private int _pin;

    public (float Depth, float Lift, float Pressure) Tick(PickSnapshot state, float dt)
    {
        foreach (var cue in _cues.Read(state))
        {
            if (cue.Pin < 0 || cue.Pin >= state.Pins) continue;
            if (cue.Sound == PickSound.Catch) _heardCatch[cue.Pin] = true;
            if (cue.Sound is PickSound.Drop or PickSound.LostSet) _heardCatch[cue.Pin] = false;
            if (cue.Pin != _pin) continue;
            if (cue.Sound is PickSound.Catch or PickSound.Seat)
            { _lowering = true; _advance = true; }
            if (cue.Sound == PickSound.LostSet)
            { _lowering = true; _advance = true; _recovering = true; }
        }
        if (!_lowering && (PickPresentation.TrueSet(state, _pin) || state.Feedback == PickFeedback.Strain))
        { _lowering = true; _advance = true; }

        if (_lowering && state.Lift < .005f && _advance)
        {
            for (var i = 0; i < state.Pins; i++)
            {
                _pin = (_pin + 1) % state.Pins;
                if (!PickPresentation.TrueSet(state, _pin)) break;
            }
            _advance = _recovering = false;
        }
        // A learned response to an encountered catch, not a per-pin solution.
        var pressure = _heardCatch[_pin] || _recovering ? .20f : .35f;
        _notchClock += dt;
        if (_notchClock >= .05f)
        {
            _notchClock -= .05f;
            if (Math.Abs(_pressure - pressure) > .004f)
                _pressure = PickInput.Pressure(_pressure, Math.Sign(pressure - _pressure), true);
        }
        if (_lowering && !_advance && state.Selected == _pin && state.Lift < .005f
            && Math.Abs(state.TensionStrength - pressure) < .006f)
            _lowering = false;

        // Probe at 20% of total travel per second. Return through the same public input shaper.
        var mouse = (_lowering ? -2f : .20f) * dt / (.035f * .25f);
        _lift = PickInput.Lift(_lift, state.Lift, mouse, 1, fine: true);
        return ((_pin + .5f) / state.Pins, _lift, _pressure);
    }
}
