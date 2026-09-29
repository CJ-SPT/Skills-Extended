using SkillsExtended.Skills.Signals;
using UnityEngine;

internal static class SignalsKeyboardChecks
{
    public static void Run(Action<bool, string> check)
    {
        foreach (
            var key in new[]
            {
                KeyCode.LeftArrow,
                KeyCode.RightArrow,
                KeyCode.UpArrow,
                KeyCode.DownArrow,
                KeyCode.Q,
                KeyCode.E,
            }
        )
        {
            var motion = 0f;
            var keyboard = new SignalsKeyboard(
                (actual, amount) =>
                {
                    if (actual == key)
                        motion += amount;
                },
                () => { },
                () => { }
            );
            var down = new Event { type = EventType.KeyDown, keyCode = key };
            keyboard.Process(down);
            check(
                down.type == EventType.Used && motion > 0,
                "Each advertised adjustment key responds immediately and consumes its UI event"
            );
            var tap = motion;
            keyboard.Advance(.02f);
            check(
                motion > tap,
                "Holding a key continues adjusting without gameplay GetKey polling"
            );
            keyboard.Process(new Event { type = EventType.KeyUp, keyCode = key });
            var released = motion;
            keyboard.Advance(.02f);
            check(motion == released, "Key release stops adjustment");
            keyboard.Process(new Event { type = EventType.KeyDown, keyCode = key });
            keyboard.Clear();
            released = motion;
            keyboard.Advance(.02f);
            check(motion == released, "Focus loss or close clears held adjustments");
        }
        var records = 0;
        var closes = 0;
        var controls = new SignalsKeyboard((_, _) => { }, () => records++, () => closes++);
        for (var i = 0; i < 8; i++)
            controls.Process(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return });
        controls.Advance(1);
        check(records == 1, "Enter auto-repeat toggles recording only once");
        controls.Process(new Event { type = EventType.KeyUp, keyCode = KeyCode.Return });
        controls.Process(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return });
        check(records == 2, "A second Enter press toggles recording again");
        controls.Process(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape });
        check(closes == 1, "Escape closes the receiver");
        var unrelated = new Event { type = EventType.KeyDown, keyCode = KeyCode.W };
        controls.Process(unrelated);
        check(
            unrelated.type == EventType.KeyDown,
            "Movement keys remain available to interrupt the receiver"
        );
    }
}

namespace UnityEngine
{
    public enum KeyCode
    {
        LeftArrow,
        RightArrow,
        UpArrow,
        DownArrow,
        Q,
        E,
        Return,
        KeypadEnter,
        Escape,
        W,
    }

    public enum EventType
    {
        KeyDown,
        KeyUp,
        Used,
    }

    public class Event
    {
        public EventType type;
        public KeyCode keyCode;

        public void Use() => type = EventType.Used;
    }
}
