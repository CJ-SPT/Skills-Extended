using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillsExtended.Skills.Signals;

// Use UI key events while the PDA owns input, independently of gameplay key polling.
internal sealed class SignalsKeyboard
{
    private readonly HashSet<KeyCode> _held = new();
    private readonly Action<KeyCode, float> _adjust;
    private readonly Action _record;
    private readonly Action _close;

    public SignalsKeyboard(Action<KeyCode, float> adjust, Action record, Action close)
    {
        _adjust = adjust;
        _record = record;
        _close = close;
    }

    public void Process(Event input)
    {
        if (input == null || (input.type != EventType.KeyDown && input.type != EventType.KeyUp))
            return;
        var key = input.keyCode;
        if (
            key != KeyCode.LeftArrow
            && key != KeyCode.RightArrow
            && key != KeyCode.UpArrow
            && key != KeyCode.DownArrow
            && key != KeyCode.Q
            && key != KeyCode.E
            && key != KeyCode.Return
            && key != KeyCode.KeypadEnter
            && key != KeyCode.Escape
        )
            return;
        var down = input.type == EventType.KeyDown;
        input.Use();
        if (!down)
        {
            _held.Remove(key);
            return;
        }
        if (!_held.Add(key))
            return; // OS repeats must not toggle recording repeatedly.
        if (key == KeyCode.Escape)
            _close();
        else if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
            _record();
        else
            _adjust(key, .05f); // A quick tap is visible even between rendered frames.
    }

    public void Advance(float elapsed)
    {
        if (float.IsNaN(elapsed) || elapsed <= 0)
            return;
        foreach (var key in _held)
            if (key != KeyCode.Return && key != KeyCode.KeypadEnter && key != KeyCode.Escape)
                _adjust(key, Math.Min(elapsed, .1f));
    }

    public void Clear() => _held.Clear();
}
