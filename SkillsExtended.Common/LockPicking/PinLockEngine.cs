using System;
using System.Collections.Generic;
using System.Linq;
using SkillsExtended.Config.Skills;

namespace SkillsExtended.LockPicking;

public enum PickOutcome { Active, Unlocked, PickBroken, Cancelled, Interrupted }
public enum PickFeedback { Searching, Springy, Binding, Strain, Unlocked, Broken, Click, CounterRotation }
public enum PinType { Standard, Spool, Serrated }
public enum PinState { Unsettled, Caught, Set, Overset }
public enum PickSound { Click, Release, Tension, Strain, Unlock, Break, Catch, Seat, Drop, CounterRotation, AdjustTension, LostSet, FalseSet, Overset, Recovered, CatchCleared }
public sealed class PickCue
{
    public int Sequence { get; set; }
    public PickSound Sound { get; set; }
    public int Pin { get; set; } = -1;
    public float Time { get; set; }
}

// Observed displacement only: neither target geometry nor logical pin states.
public sealed class PickPinMotion
{
    public float KeyLift { get; set; }
    public float DriverLift { get; set; }
}

public sealed class PinLockDefinition
{
    public uint? Seed { get; set; }
    public float[] Heights { get; set; }
    public int[] Order { get; set; }
    public PinType[] Types { get; set; }
    public float[][] Catches { get; set; }

    public static PinLockDefinition Create(int pins, uint seed) => Create(
        new LockPickingTier { Pins = pins, SpoolPins = 0, SerratedPins = 0 }, seed);

    public static PinLockDefinition Create(LockPickingTier tier, uint seed)
    {
        uint Next()
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            return seed;
        }
        if (seed == 0) seed = 1;
        var pins = tier.Pins;
        var result = new PinLockDefinition
        {
            Seed = seed,
            Heights = new float[pins], Order = Enumerable.Range(0, pins).ToArray(),
            Types = new PinType[pins], Catches = new float[pins][],
        };
        for (var i = 0; i < pins; i++)
            result.Heights[i] = .32f + Next() / (float)uint.MaxValue * .4f;
        for (var i = pins - 1; i > 0; i--)
        {
            var j = (int)(Next() % (uint)(i + 1));
            (result.Order[i], result.Order[j]) = (result.Order[j], result.Order[i]);
        }
        // Shuffle type positions independently of binding priorities.
        var positions = Enumerable.Range(0, pins).ToArray();
        for (var i = pins - 1; i > 0; i--)
        {
            var j = (int)(Next() % (uint)(i + 1));
            (positions[i], positions[j]) = (positions[j], positions[i]);
        }
        for (var i = 0; i < tier.SpoolPins; i++) result.Types[positions[i]] = PinType.Spool;
        for (var i = tier.SpoolPins; i < tier.SpoolPins + tier.SerratedPins; i++)
            result.Types[positions[i]] = PinType.Serrated;
        for (var i = 0; i < pins; i++)
        {
            var n = result.Types[i] == PinType.Spool ? 1
                : result.Types[i] == PinType.Serrated ? tier.SerrationCatches : 0;
            result.Catches[i] = new float[n];
            for (var c = 0; c < n; c++)
                result.Catches[i][c] = result.Heights[i] * (.25f + .45f * (c + 1) / (n + 1));
        }
        return result;
    }
}

// Observed feedback plus reproduction metadata; no explicit setting geometry.
public sealed class PickSnapshot
{
    public uint? Seed { get; set; }
    public int SkillLevel { get; set; }
    public PickOutcome Outcome { get; set; }
    public PickFeedback Feedback { get; set; }
    public int Pins { get; set; }
    public int Selected { get; set; }
    public PinType[] PinTypes { get; set; }
    public int Cue { get; set; }
    public PickCue[] Cues { get; set; }
    public float Lift { get; set; }
    public float Strain { get; set; }
    public float Wear { get; set; }
    public bool Tension { get; set; }
    public float TensionStrength { get; set; }
    public float CylinderRotation { get; set; }
    public float ElapsedSeconds { get; set; }
    public PickPinMotion[] PinMotion { get; set; }
    public bool[] SetPinStates { get; set; }
    public PinState SelectedPinState { get; set; }
    public bool FalseSet { get; set; }
}

public sealed class PickCoaching
{
    public int BindingPin { get; set; } = -1;
    public float LiftMin { get; set; }
    public float LiftMax { get; set; }
    public float PressureMin { get; set; }
    public float PressureMax { get; set; }
    public PinType Type { get; set; }
    public PinState State { get; set; }
    public int SetPins { get; set; }
}

// Retained cue history bridges short events between 20 Hz network snapshots.
public sealed class PickCueReader
{
    private int _last;
    public void Reset(int sequence = 0) => _last = sequence;
    public PickCue[] Read(PickSnapshot state)
    {
        var fresh = (state.Cues ?? Array.Empty<PickCue>()).Where(c => c.Sequence > _last)
            .OrderBy(c => c.Sequence).ToArray();
        _last = Math.Max(_last, state.Cue);
        return fresh;
    }
}

/// <summary>Fixed-step authoritative simulation with deterministic pin support and recovery.</summary>
public sealed class PinLockEngine
{
    public const float StepSeconds = 1f / 60;
    public const float MoveLiftLimit = .08f;
    public const float ClearPressureMin = .08f, ClearPressureMax = .24f;
    public const float CatchClearance = .025f;
    private readonly PinLockDefinition _lock;
    private readonly PinState[] _states;
    private readonly int[] _passed;
    private readonly float[] _support;
    private readonly float[] _driverLift;
    private readonly Queue<PickCue> _cues = new();
    private readonly float _tolerance, _warning, _wearSeconds;
    private readonly int _skill;
    private float _remainder, _lift, _strain, _pressure, _rotation;
    private int _selected, _cue;
    private bool _held;
    private bool _counterMoving, _falseSet;
    private float _time, _lastDesiredPressure;
    private PickFeedback _feedback;
    public PickOutcome Outcome { get; private set; }
    public float Wear { get; private set; }
    public bool MadeProgress { get; private set; }

    public PinLockEngine(PinLockDefinition definition, LockPickingData config, int difficulty,
        int skill, float wear = 0)
    {
        _lock = definition;
        _states = new PinState[definition.Heights.Length];
        _passed = new int[_states.Length];
        _support = new float[_states.Length];
        _driverLift = new float[_states.Length];
        var tier = config.Tier(difficulty);
        skill = Math.Max(0, Math.Min(51, skill));
        _skill = skill;
        var elite = skill == 51 ? config.ExpertControlElite / 100f : 0;
        _tolerance = Math.Min(.24f, tier.Tolerance * (1 + skill * config.PinTolerancePerLevel / 100f + elite));
        _warning = tier.StrainWarningSeconds;
        _wearSeconds = config.PickWearSeconds * (1 + skill * config.PickResiliencePerLevel / 100f + elite);
        Wear = Math.Max(0, Math.Min(1, wear));
    }

    public void Advance(float seconds, float depth, float targetLift, bool tension,
        float tensionStrength = .35f)
    {
        if (Outcome != PickOutcome.Active || !Finite(seconds) || !Finite(depth)
            || !Finite(targetLift) || !Finite(tensionStrength)) return;
        _remainder += Math.Max(0, Math.Min(.25f, seconds));
        while (_remainder + .000001f >= StepSeconds && Outcome == PickOutcome.Active)
        {
            _remainder -= StepSeconds;
            Tick(Clamp(depth), Clamp(targetLift), tension ? Clamp(tensionStrength) : 0);
        }
    }

    private float[] Catches(int pin) => _lock.Catches?[pin] ?? Array.Empty<float>();
    private PinType Type(int pin) => _lock.Types?[pin] ?? PinType.Standard;
    private int Binding()
    {
        // Unsettled stacks prevent a false set; caught stacks bind after these clear.
        foreach (var pin in _lock.Order)
            if (_states[pin] == PinState.Overset || _states[pin] == PinState.Unsettled) return pin;
        foreach (var pin in _lock.Order)
            if (_states[pin] == PinState.Caught) return pin;
        return -1;
    }

    private void Drop(int pin)
    {
        _states[pin] = PinState.Unsettled;
        _passed[pin] = 0;
        _driverLift[pin] = 0;
        Emit(PickSound.Drop, pin);
    }

    private void Tick(float depth, float target, float desiredPressure)
    {
        _time += StepSeconds;
        var held = desiredPressure > 0;
        if (_held && !held)
        {
            for (var i = 0; i < _states.Length; i++)
                if (_states[i] != PinState.Unsettled) Drop(i);
            Array.Clear(_states, 0, _states.Length);
            Array.Clear(_passed, 0, _passed.Length);
            Emit(PickSound.Release);
        }
        else if (!_held && held) Emit(PickSound.Tension);
        else if (held && Math.Abs(desiredPressure - _lastDesiredPressure) >= .005f)
            Emit(PickSound.AdjustTension);
        _lastDesiredPressure = desiredPressure;
        _held = held;
        _pressure = held ? Move(_pressure, desiredPressure, StepSeconds * 2) : 0;
        if (_lift < MoveLiftLimit)
        {
            var pin = Math.Min(_states.Length - 1, (int)(depth * _states.Length));
            _selected = pin;
        }
        // Very light pressure loses support deterministically, not by a chance roll.
        for (var i = 0; i < _states.Length; i++)
            if (_states[i] == PinState.Set && _pressure < .10f && held && target < .08f)
            { Drop(i); }
        var binding = held && _pressure >= .08f && Binding() == _selected;
        var state = _states[_selected];
        var catches = Catches(_selected);
        var caught = state == PinState.Caught;
        var canClear = _pressure >= ClearPressureMin && _pressure <= ClearPressureMax;
        var counter = caught && Type(_selected) == PinType.Spool && target > _lift + .015f;
        var height = _lock.Heights[_selected];
        var resistance = !held ? 1f : state == PinState.Set ? height + _tolerance + .025f
            : state == PinState.Overset ? _lift
            : caught && !canClear ? catches[_passed[_selected]]
            : binding ? height + _tolerance + .025f : .18f;
        // Heavy torque adds friction, making forced movement slower and more costly.
        var speed = .55f / (1 + Math.Max(0, _pressure - .35f) * 3);
        _lift = Move(_lift, Math.Min(target, resistance), StepSeconds * (target < _lift ? 1.5f : speed));
        var forcing = held && target > resistance + .01f && _lift >= resistance - .015f;
        _strain = forcing ? _strain + StepSeconds : Math.Max(0, _strain - StepSeconds * 3);
        if (forcing && _strain > _warning)
            Wear = Math.Min(1, Wear + StepSeconds / _wearSeconds * (1 + Math.Max(0, _pressure - .35f)));
        if (Wear >= 1) { ObserveMotion(); Outcome = PickOutcome.PickBroken; Emit(PickSound.Break); return; }
        _feedback = !held ? PickFeedback.Searching : forcing ? PickFeedback.Strain
            : counter ? PickFeedback.CounterRotation
            : binding && _lift > .1f ? PickFeedback.Binding : PickFeedback.Springy;

        if (held && state == PinState.Overset && canClear && target < height - _tolerance
            && _lift <= height + _tolerance)
        {
            Drop(_selected);
            Emit(PickSound.Recovered, _selected);
            state = PinState.Unsettled;
        }
        if (held && (binding || state == PinState.Set) && _lift > height + _tolerance)
        {
            if (state == PinState.Set) Emit(PickSound.LostSet, _selected);
            if (state != PinState.Overset) Emit(PickSound.Overset, _selected);
            _states[_selected] = PinState.Overset;
        }
        else if (held && caught && canClear && target > catches[_passed[_selected]] + CatchClearance)
        {
            _passed[_selected]++;
            _states[_selected] = PinState.Unsettled;
            Emit(PickSound.CatchCleared, _selected);
        }
        else if (held && binding && state == PinState.Unsettled
            && _passed[_selected] < catches.Length && _lift >= catches[_passed[_selected]])
        {
            _lift = catches[_passed[_selected]];
            _states[_selected] = PinState.Caught;
            Emit(PickSound.Catch, _selected);
            _feedback = PickFeedback.Click;
        }
        else if (held && binding && state == PinState.Unsettled
            && Math.Abs(_lift - height) <= _tolerance && target >= _lift
            && _passed[_selected] == catches.Length)
        {
            // Seat on actual contact, even if the player is still requesting more lift.
            // Further movement remains possible and can overset this pin on later steps.
            _states[_selected] = PinState.Set;
            _support[_selected] = _rotation;
            MadeProgress = true;
            Emit(PickSound.Seat, _selected);
            _feedback = PickFeedback.Click;
        }

        var complete = true;
        var falseSet = true;
        for (var i = 0; i < _states.Length; i++)
        {
            if (_states[i] == PinState.Set) continue;
            complete = false;
            if (_states[i] != PinState.Caught || Type(i) != PinType.Spool) falseSet = false;
        }
        falseSet &= !complete;
        var rotationTarget = complete ? 1f : falseSet ? .10f : held ? .015f : 0;
        if (counter && canClear) rotationTarget = .015f;
        // In a false set the plug's ledges support ordinary stacks at different depths.
        // Returning past a shallow ledge at low torque releases that stack predictably.
        if (falseSet)
            for (var i = 0; i < _states.Length; i++)
                if (_states[i] == PinState.Set)
                    _support[i] = Math.Max(_support[i], Math.Min(.09f,
                        .03f + Array.IndexOf(_lock.Order, i) * .025f));
        var oldRotation = _rotation;
        _rotation = Move(_rotation, rotationTarget, StepSeconds * .4f);
        var movingBack = counter && held && _rotation < oldRotation;
        if (movingBack && !_counterMoving) Emit(PickSound.CounterRotation, _selected);
        _counterMoving = movingBack;
        if (_rotation < oldRotation && _pressure < .18f && held)
            for (var i = 0; i < _states.Length; i++)
                if (i != _selected && _states[i] == PinState.Set && _support[i] > _rotation + .025f)
                { Drop(i); }
        ObserveMotion();
        if (complete) { _rotation = 1; Outcome = PickOutcome.Unlocked; Emit(PickSound.Unlock); }
        // Describe the resulting state, after any support loss, without changing mechanics.
        var currentFalseSet = held && Outcome == PickOutcome.Active;
        var hasSpoolCatch = false;
        for (var pin = 0; pin < _states.Length; pin++)
        {
            if (_states[pin] == PinState.Caught && Type(pin) == PinType.Spool) hasSpoolCatch = true;
            else if (_states[pin] != PinState.Set) currentFalseSet = false;
        }
        currentFalseSet &= hasSpoolCatch;
        if (currentFalseSet && !_falseSet) Emit(PickSound.FalseSet);
        _falseSet = currentFalseSet;
    }

    private void ObserveMotion()
    {
        for (var i = 0; i < _states.Length; i++)
        {
            var contact = i == _selected ? _lift : 0;
            _driverLift[i] = _states[i] == PinState.Unsettled
                ? contact : Math.Max(_driverLift[i], contact);
        }
    }

    private void Emit(PickSound sound, int pin = -1)
    {
        _cues.Enqueue(new PickCue { Sequence = ++_cue, Sound = sound, Pin = pin, Time = _time });
        while (_cues.Count > 32) _cues.Dequeue();
    }
    public void End(bool interrupted)
    {
        if (Outcome == PickOutcome.Active) Outcome = interrupted ? PickOutcome.Interrupted : PickOutcome.Cancelled;
    }
    public PickCoaching Coaching()
    {
        var state = _states[_selected];
        var height = _lock.Heights[_selected];
        var min = Math.Max(0, height - _tolerance);
        var max = Math.Min(1, height + _tolerance);
        if (state == PinState.Overset) { min = 0; max = Math.Max(0, height - _tolerance - .02f); }
        else if (state == PinState.Caught)
        {
            min = Math.Min(max, Catches(_selected)[_passed[_selected]] + CatchClearance + .01f);
            max = Math.Min(max, Math.Max(min, height));
        }
        var recovery = state == PinState.Caught || state == PinState.Overset;
        return new PickCoaching
        {
            BindingPin = Binding(), LiftMin = min, LiftMax = max,
            PressureMin = recovery ? .20f : .25f, PressureMax = recovery ? ClearPressureMax : .35f,
            Type = Type(_selected), State = state,
            SetPins = _states.Count(s => s == PinState.Set),
        };
    }
    public PickSnapshot Snapshot() => new()
    {
        Seed = _lock.Seed, SkillLevel = _skill,
        Outcome = Outcome, Feedback = _feedback, Pins = _states.Length, Selected = _selected,
        PinTypes = _lock.Types == null ? new PinType[_states.Length] : (PinType[])_lock.Types.Clone(),
        Cue = _cue, Cues = _cues.Select(c => new PickCue { Sequence = c.Sequence, Sound = c.Sound, Pin = c.Pin, Time = c.Time }).ToArray(),
        Lift = _lift, Strain = Math.Min(1, _strain / _warning), Wear = Wear,
        Tension = _held, TensionStrength = _pressure, CylinderRotation = _rotation,
        ElapsedSeconds = _time,
        SetPinStates = _states.Select(s => s == PinState.Set).ToArray(),
        SelectedPinState = _states[_selected],
        FalseSet = Outcome == PickOutcome.Active && _falseSet,
        PinMotion = _driverLift.Select((driver, pin) => new PickPinMotion
        {
            KeyLift = pin == _selected ? _lift : 0, DriverLift = driver,
        }).ToArray(),
    };
    private static float Clamp(float v) => Math.Max(0, Math.Min(1, v));
    private static float Move(float from, float to, float step) => from + Math.Max(-step, Math.Min(step, to - from));
    public static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
}
