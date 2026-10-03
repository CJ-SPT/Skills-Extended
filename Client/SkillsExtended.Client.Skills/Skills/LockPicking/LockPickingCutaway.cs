using SkillsExtended.LockPicking;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.LockPicking;

/// <summary>An illustrative mechanism driven only by the public, authoritative snapshot.</summary>
internal sealed class LockPickingCutaway : MaskableGraphic
{
    private static readonly Color Back = new(.055f, .064f, .068f);
    private static readonly Color Housing = new(.12f, .14f, .15f);
    private static readonly Color Edge = new(.28f, .32f, .33f);
    private static readonly Color Metal = new(.57f, .60f, .57f);
    private static readonly Color Shine = new(.78f, .80f, .75f);
    private static readonly Color Set = new(.55f, .70f, .57f);
    private static readonly Color Warning = new(.88f, .65f, .33f);
    private PickSnapshot _state;
    private PickCoaching _coaching;
    private float _rotation;
    private float _lift,
        _pickX,
        _retraction;
    private bool _initialized;
    private int _pinCount;

    public static float PinX(int pin, int count) => 20 + 260f * pin / (count - 1);

    public void ResetAnimation() => _initialized = false;

    public void Render(PickSnapshot state, float deltaTime, bool reducedMotion, PickCoaching coaching = null)
    {
        if (state == null || state.Pins < 3 || state.Pins > 5)
        {
            _state = null;
            _initialized = false;
            SetVerticesDirty();
            return;
        }
        var x = PinX(Mathf.Clamp(state.Selected, 0, state.Pins - 1), state.Pins);
        var lift = PinLockEngine.Finite(state.Lift) ? Mathf.Clamp01(state.Lift) : 0;
        var retract = state.Outcome == PickOutcome.Unlocked ? 1f : 0;
        var reset =
            !_initialized
            || _pinCount != state.Pins
            || (
                _state != null
                && _state.Outcome != PickOutcome.Active
                && state.Outcome == PickOutcome.Active
            );
        var blend = reset || reducedMotion ? 1 : 1 - Mathf.Exp(-16 * Mathf.Max(0, deltaTime));
        _pickX = Mathf.Lerp(_pickX, x, blend);
        _lift = Mathf.Lerp(_lift, lift, blend);
        _retraction = Mathf.Lerp(_retraction, retract, blend);
        if (System.Math.Abs(_lift - lift) < .0001f) _lift = lift;
        if (System.Math.Abs(_pickX - x) < .0001f) _pickX = x;
        if (System.Math.Abs(_retraction - retract) < .0001f) _retraction = retract;
        _pinCount = state.Pins;
        _state = state;
        _coaching = coaching;
        _rotation = Mathf.Lerp(_rotation, state.CylinderRotation, blend);
        _initialized = true;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect(vh, -500, -95, 1000, 190, Back);
        Line(vh, new(-500, 94), new(500, 94), Edge);
        if (_state == null)
            return;
        var selected = Mathf.Clamp(_state.Selected, 0, _state.Pins - 1);
        var known = _coaching?.SetPinStates != null && _coaching.SetPinStates.Length == _state.Pins;
        var warning =
            _state.Outcome == PickOutcome.Active
            && (
                _state.Feedback == PickFeedback.Strain
                || _state.Strain > .5f
            );
        var tension = _state.Tension && _state.Outcome == PickOutcome.Active;

        // A compact cut cylinder leaves room for a real insertion stroke outside its mouth.
        // All pin lengths are illustrative, independent of the hidden bitting and target heights.
        Rounded(vh, -49, -78, 408, 141, 17, new Color(.023f, .028f, .026f), Housing);
        Rounded(
            vh,
            -43,
            -73,
            395,
            88,
            16,
            new Color(.24f, .20f, .12f),
            new Color(.68f, .61f, .39f)
        );
        Rounded(
            vh,
            -35,
            -66,
            377,
            74,
            11,
            new Color(.38f, .32f, .19f),
            new Color(.73f, .67f, .45f)
        );
        Rounded(
            vh,
            -28,
            -61,
            355,
            63,
            7,
            new Color(.08f, .079f, .063f),
            new Color(.025f, .029f, .025f)
        );
        // Fixed upper shell and separate exposed spring/driver bores.
        Rounded(vh, -39, 7, 383, 54, 5, new Color(.39f, .34f, .21f), new Color(.64f, .59f, .40f));
        Rect(vh, -34, 57, 373, 4, new Color(.78f, .74f, .55f));
        // Open keyway: the lowered pin tips sit 51 units above its floor (formerly 28).
        Rect(vh, -48, -60, 25, 49, Back);
        Rect(vh, -42, -11, 12, 22, Metal);
        Rect(vh, -40, -11, 3, 22, Shine);
        Line(vh, new(-25, -61), new(319, -61), new Color(.61f, .55f, .36f), 1.2f);
        Ellipse(vh, 335, -31, 8, 27, new Color(.36f, .31f, .18f));
        Ellipse(vh, 333, -30, 5, 23, new Color(.55f, .49f, .30f));

        // Neutral rotation gauge: readable with sound or decorative vibration disabled.
        Ellipse(vh, 411, -30, 24, 24, Edge);
        var angle = _rotation * Mathf.PI * .5f;
        Line(vh, new(411, -30), new(411 + Mathf.Sin(angle) * 55, -30 + Mathf.Cos(angle) * 55), Shine, 3);
        for (var pin = 0; pin < _state.Pins; pin++)
        {
            var x = PinX(pin, _state.Pins);
            var completed =
                known
                && _coaching.SetPinStates[pin]
                && (_state.Tension || _state.Outcome == PickOutcome.Unlocked);
            var bottom = completed ? 8 : -10 + (pin == selected ? _lift * 16 : 0);
            var top = bottom + 38;
            Rect(vh, x - 13, 8, 26, 48, new Color(.045f, .048f, .037f));
            Rect(vh, x - 15, 9, 2, 47, new Color(.80f, .74f, .50f));
            Rect(vh, x + 13, 9, 2, 47, new Color(.25f, .21f, .13f));
            var spring = new Vector2(x, top + 1);
            for (var turn = 1; turn <= 60; turn++)
            {
                var phase = turn * Mathf.PI / 6;
                var next = new Vector2(
                    x + Mathf.Sin(phase) * 7,
                    Mathf.Lerp(top + 1, 54, turn / 60f) + (Mathf.Cos(phase) - 1) * .45f
                );
                Line(vh, spring, next, Color.Lerp(Edge, Shine, (Mathf.Cos(phase) + 1) * .5f), 1.4f);
                spring = next;
            }
            // Distinct rounded key pin and steel driver, with a visible contact joint.
            PinBody(vh, x, bottom, 18, false);
            var type = _state.PinTypes != null && _state.PinTypes.Length == _state.Pins
                ? _state.PinTypes[pin] : PinType.Standard;
            DriverPin(vh, x, bottom + 18, type);
            if (pin == selected)
            {
                var outline = warning ? Warning : Shine;
                Brackets(vh, x - 19, -14, 38, 70, outline);
                Line(vh, new(x - 4, -71), new(x, -67), outline, 1.5f);
                Line(vh, new(x, -67), new(x + 4, -71), outline, 1.5f);
            }
            if (completed)
            {
                Line(vh, new(x + 18, 0), new(x + 22, -4), Set, 2);
                Line(vh, new(x + 22, -4), new(x + 28, 5), Set, 2);
            }
        }

        if (_retraction < .99f)
        {
            var x = Mathf.Lerp(_pickX, -50, _retraction);
            var lift = _lift * (1 - _retraction);
            Vector2 Point(float px, float py) => PickPoint(new Vector2(px, py), x, lift);
            // Every vertex uses the same rigid transform: fixed shaft, hook and grip lengths.
            Line(vh, Point(-438, -18), Point(-383, -18), Edge, 13);
            Line(vh, Point(-438, -16), Point(-383, -16), Metal, 3);
            Ellipse(vh, Point(-438, -18).x, Point(-438, -18).y, 6.5f, 6.5f, Edge);
            for (var rib = 0; rib < 7; rib++)
                Line(vh, Point(-434 + rib * 7, -23), Point(-434 + rib * 7, -13), Housing);
            SteelLine(vh, Point(-389, -18), Point(-26, -18), 4);
            if (_state.Outcome != PickOutcome.PickBroken)
            {
                var previous = Point(-26, -18);
                for (var segment = 1; segment <= 12; segment++)
                {
                    var t = segment / 12f;
                    var next = Point(-26 + 26 * t, -18 + 16.5f * t * t * (3 - 2 * t));
                    SteelLine(vh, previous, next, 3);
                    previous = next;
                }
            }
            else
                Line(vh, Point(-29, -21), Point(-24, -15), Warning, 2);
        }
        if (_state.Outcome != PickOutcome.Unlocked)
        {
            var tint = tension ? Warning : Metal;
            Line(vh, new(-134, -77), new(-66, -77), tint, 4);
            Line(vh, new(-66, -77), new(-66, tension ? -52 : -56), tint, 4);
            Line(vh, new(-66, tension ? -52 : -56), new(-4, tension ? -52 : -56), tint, 4);
        }
    }

    // Rigid translation and slight rotation about the tip, never per-endpoint stretching.
    internal static Vector2 PickPoint(Vector2 local, float tipX, float lift)
    {
        var angle = lift * .065f;
        var c = Mathf.Cos(angle);
        var s = Mathf.Sin(angle);
        return new Vector2(
            tipX + local.x * c - local.y * s,
            -10 + lift * 16 + local.x * s + local.y * c
        );
    }

    private static void DriverPin(VertexHelper vh, float x, float bottom, PinType type)
    {
        if (type != PinType.Spool && type != PinType.Serrated)
        {
            PinBody(vh, x, bottom, 20, true);
            return;
        }
        // Fixed illustrative profiles: never draw the secret catch locations or target heights.
        var dark = new Color(.20f, .23f, .23f);
        var light = new Color(.80f, .84f, .82f);
        for (var row = 0; row < 20; row++)
        {
            var radius = type == PinType.Spool
                ? (row >= 4 && row < 16 ? 4f : 8f)
                : (row == 4 || row == 5 || row == 9 || row == 10 || row == 14 || row == 15 ? 5f : 8f);
            for (var band = 0; band < 16; band++)
            {
                var lighting = Mathf.Pow(Mathf.Max(0, Mathf.Cos((band / 15f - .40f) * Mathf.PI)), .7f);
                Rect(vh, x - radius + band * radius / 8, bottom + row,
                    radius / 8, 1, Color.Lerp(dark, light, lighting));
            }
        }
        Ellipse(vh, x, bottom + 20, 8, 1, light);
    }

    private static void PinBody(VertexHelper vh, float x, float bottom, float height, bool steel)
    {
        var dark = steel ? new Color(.20f, .23f, .23f) : new Color(.28f, .22f, .10f);
        var light = steel ? new Color(.80f, .84f, .82f) : new Color(.85f, .75f, .46f);
        Ellipse(vh, x, bottom + 3, 8, 3, dark);
        for (var band = 0; band < 16; band++)
        {
            var lighting = Mathf.Pow(Mathf.Max(0, Mathf.Cos((band / 15f - .40f) * Mathf.PI)), .7f);
            Rect(vh, x - 8 + band, bottom + 3, 1, height - 4, Color.Lerp(dark, light, lighting));
        }
        Ellipse(vh, x, bottom + height - 1, 8, 2, light);
    }

    private static void Rect(
        VertexHelper vh,
        float x,
        float y,
        float width,
        float height,
        Color tint
    )
    {
        var i = vh.currentVertCount;
        vh.AddVert(new Vector2(x, y), tint, Vector2.zero);
        vh.AddVert(new Vector2(x, y + height), tint, Vector2.zero);
        vh.AddVert(new Vector2(x + width, y + height), tint, Vector2.zero);
        vh.AddVert(new Vector2(x + width, y), tint, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }

    private static void Rounded(
        VertexHelper vh,
        float x,
        float y,
        float w,
        float h,
        float radius,
        Color low,
        Color high
    )
    {
        // Narrow shaded bands follow the rounded edge, giving the cut metal a bevel and depth.
        float Inset(float height)
        {
            var edge = Mathf.Min(height, h - height);
            return edge >= radius
                ? 0
                : radius
                    - Mathf.Sqrt(Mathf.Max(0, radius * radius - (radius - edge) * (radius - edge)));
        }
        for (var band = 0; band < 32; band++)
        {
            var a = h * band / 32;
            var b = h * (band + 1) / 32;
            var ca = Inset(a);
            var cb = Inset(b);
            var tint = Color.Lerp(low, high, (band + .5f) / 32);
            var i = vh.currentVertCount;
            vh.AddVert(new Vector2(x + ca, y + a), tint, Vector2.zero);
            vh.AddVert(new Vector2(x + cb, y + b), tint, Vector2.zero);
            vh.AddVert(new Vector2(x + w - cb, y + b), tint, Vector2.zero);
            vh.AddVert(new Vector2(x + w - ca, y + a), tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    private static void Ellipse(VertexHelper vh, float x, float y, float rx, float ry, Color tint)
    {
        var i = vh.currentVertCount;
        vh.AddVert(new Vector2(x, y), tint, Vector2.zero);
        for (var segment = 0; segment <= 24; segment++)
        {
            var angle = -segment * Mathf.PI / 12;
            vh.AddVert(
                new Vector2(x + Mathf.Cos(angle) * rx, y + Mathf.Sin(angle) * ry),
                tint,
                Vector2.zero
            );
            if (segment > 0)
                vh.AddTriangle(i, i + segment, i + segment + 1);
        }
    }

    private static void SteelLine(VertexHelper vh, Vector2 a, Vector2 b, float width)
    {
        Line(vh, a, b, Edge, width + 1);
        Line(vh, a, b, Metal, width);
        Line(vh, a + new Vector2(0, .5f), b + new Vector2(0, .5f), Shine, 1);
    }

    private static void Brackets(VertexHelper vh, float x, float y, float w, float h, Color tint)
    {
        for (var corner = 0; corner < 4; corner++)
        {
            var left = corner < 2;
            var bottom = corner % 2 == 0;
            var point = new Vector2(left ? x : x + w, bottom ? y : y + h);
            Line(vh, point, point + new Vector2(left ? 8 : -8, 0), tint, 1.5f);
            Line(vh, point, point + new Vector2(0, bottom ? 8 : -8), tint, 1.5f);
        }
    }

    private static void Line(VertexHelper vh, Vector2 a, Vector2 b, Color tint, float width = 1)
    {
        var direction = (b - a).normalized;
        var offset = new Vector2(-direction.y, direction.x) * (width / 2);
        var i = vh.currentVertCount;
        vh.AddVert(a - offset, tint, Vector2.zero);
        vh.AddVert(a + offset, tint, Vector2.zero);
        vh.AddVert(b + offset, tint, Vector2.zero);
        vh.AddVert(b - offset, tint, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }
}
