using System;
using System.Linq;
using SkillsExtended.Config.Skills;

namespace SkillsExtended.LockPicking;

public enum PickOutcome
{
    Active,
    Unlocked,
    PickBroken,
    Cancelled,
    Interrupted,
}

public enum PickFeedback
{
    Searching,
    Springy,
    Binding,
    Ready,
    Set,
    Overset,
    Strain,
    Reset,
    Unlocked,
    Broken,
}

public sealed class PinLockDefinition
{
    public float[] Heights { get; set; }
    public int[] Order { get; set; }

    public static PinLockDefinition Create(int pins, uint seed)
    {
        uint Next()
        {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            return seed;
        }
        if (seed == 0)
            seed = 1;
        var result = new PinLockDefinition
        {
            Heights = new float[pins],
            Order = Enumerable.Range(0, pins).ToArray(),
        };
        for (var i = 0; i < pins; i++)
            result.Heights[i] = .32f + Next() / (float)uint.MaxValue * .4f;
        for (var i = pins - 1; i > 0; i--)
        {
            var j = (int)(Next() % (uint)(i + 1));
            (result.Order[i], result.Order[j]) = (result.Order[j], result.Order[i]);
        }
        return result;
    }
}

// No hidden heights or binding order are exposed to the UI or network.
public sealed class PickSnapshot
{
    public PickOutcome Outcome { get; set; }
    public PickFeedback Feedback { get; set; }
    public int Pins { get; set; }
    public int Selected { get; set; }
    public int SetPins { get; set; }
    public bool[] SetPinStates { get; set; }
    public int Cue { get; set; }
    public float Lift { get; set; }
    public float Strain { get; set; }
    public float Wear { get; set; }
    public bool Tension { get; set; }
}

/// <summary>Fixed-step simulation. Elapsed time comes from the authority, never a network peer.</summary>
public sealed class PinLockEngine
{
    public const float StepSeconds = 1f / 60;
    private readonly PinLockDefinition _lock;
    private readonly bool[] _set;
    private readonly float _tolerance,
        _warning,
        _wearSeconds;
    private float _remainder,
        _lift,
        _strain,
        _settle;
    private int _selected,
        _count,
        _cue;
    private bool _tension,
        _overset;
    private PickFeedback _feedback;
    public PickOutcome Outcome { get; private set; }
    public float Wear { get; private set; }
    public bool MadeProgress { get; private set; }

    public PinLockEngine(
        PinLockDefinition definition,
        LockPickingData config,
        int difficulty,
        int skill,
        float wear = 0
    )
    {
        _lock = definition;
        _set = new bool[definition.Heights.Length];
        var tier = config.Tier(difficulty);
        skill = Math.Max(0, Math.Min(51, skill));
        var elite = skill == 51 ? config.ExpertControlElite / 100f : 0;
        _tolerance = Math.Min(
            .24f,
            tier.Tolerance * (1 + skill * config.PinTolerancePerLevel / 100f + elite)
        );
        _warning = tier.StrainWarningSeconds;
        _wearSeconds =
            config.PickWearSeconds * (1 + skill * config.PickResiliencePerLevel / 100f + elite);
        Wear = Math.Max(0, Math.Min(1, wear));
    }

    public void Advance(float seconds, float depth, float targetLift, bool tension)
    {
        if (
            Outcome != PickOutcome.Active
            || !Finite(seconds)
            || !Finite(depth)
            || !Finite(targetLift)
        )
            return;
        _remainder += Math.Max(0, Math.Min(.25f, seconds));
        while (_remainder + .000001f >= StepSeconds && Outcome == PickOutcome.Active)
        {
            _remainder -= StepSeconds;
            Tick(Math.Max(0, Math.Min(1, depth)), Math.Max(0, Math.Min(1, targetLift)), tension);
        }
    }

    private void Tick(float depth, float target, bool tension)
    {
        if (_tension && !tension)
        {
            Array.Clear(_set, 0, _set.Length);
            _count = 0;
            _overset = false;
            _settle = 0;
            Cue(PickFeedback.Reset);
        }
        _tension = tension;
        if (_lift < .08f)
        {
            var selected = Math.Min(_set.Length - 1, (int)(depth * _set.Length));
            if (selected != _selected)
            {
                _selected = selected;
                _settle = 0;
            }
        }
        var binding = _count < _set.Length && _lock.Order[_count] == _selected;
        var resistance =
            !tension || _set[_selected] ? 1f
            : binding ? _lock.Heights[_selected] + _tolerance + .025f
            : .18f;
        var wanted = Math.Min(target, resistance);
        _lift += Math.Max(-StepSeconds * 1.5f, Math.Min(StepSeconds * .55f, wanted - _lift));
        var forcing = tension && target > resistance && _lift >= resistance - .015f;
        _strain =
            forcing || (tension && _overset && target > .1f)
                ? _strain + StepSeconds
                : Math.Max(0, _strain - StepSeconds * 3);
        if (_strain > _warning && tension && (forcing || (_overset && target > .1f)))
            Wear = Math.Min(1, Wear + StepSeconds / _wearSeconds);
        if (Wear >= 1)
        {
            Outcome = PickOutcome.PickBroken;
            Cue(PickFeedback.Broken);
            return;
        }
        if (!tension)
        {
            if (_feedback != PickFeedback.Reset)
                _feedback = PickFeedback.Searching;
            return;
        }
        if (_overset)
        {
            _feedback = PickFeedback.Overset;
            return;
        }
        if (_set[_selected])
        {
            _feedback = PickFeedback.Set;
            return;
        }
        if (binding && _lift > _lock.Heights[_selected] + _tolerance)
        {
            _overset = true;
            Cue(PickFeedback.Overset);
            return;
        }
        if (
            binding
            && Math.Abs(_lift - _lock.Heights[_selected]) <= _tolerance
            && target <= _lock.Heights[_selected] + _tolerance
        )
        {
            if (_feedback != PickFeedback.Ready)
                Cue(PickFeedback.Ready);
            _settle += StepSeconds;
            if (_settle >= .3f)
            {
                _set[_selected] = true;
                _count++;
                MadeProgress = true;
                Cue(PickFeedback.Set);
                if (_count == _set.Length)
                {
                    Outcome = PickOutcome.Unlocked;
                    Cue(PickFeedback.Unlocked);
                }
            }
        }
        else
        {
            _settle = 0;
            var feedback =
                _strain > _warning * .5f ? PickFeedback.Strain
                : binding && _lift > .1f ? PickFeedback.Binding
                : PickFeedback.Springy;
            if (feedback != _feedback)
                Cue(feedback);
        }
    }

    private void Cue(PickFeedback feedback)
    {
        _feedback = feedback;
        _cue++;
    }

    public void End(bool interrupted)
    {
        if (Outcome == PickOutcome.Active)
            Outcome = interrupted ? PickOutcome.Interrupted : PickOutcome.Cancelled;
    }

    public PickSnapshot Snapshot() =>
        new()
        {
            Outcome = Outcome,
            Feedback = _feedback,
            Pins = _set.Length,
            Selected = _selected,
            SetPins = _count,
            SetPinStates = (bool[])_set.Clone(),
            Cue = _cue,
            Lift = _lift,
            Strain = Math.Min(1, _strain / _warning),
            Wear = Wear,
            Tension = _tension,
        };

    public static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
}
