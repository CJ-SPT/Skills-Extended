using SkillsExtended.LockPicking;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.LockPicking;

/// <summary>An illustrative mechanism driven only by the public, authoritative snapshot.</summary>
internal sealed class LockPickingCutaway : MaskableGraphic
{
    internal const float ShearY = 8, RestTipY = -32, PinTravel = 24;
    internal const float MouthX = -38, FulcrumY = -50;
    private static readonly Color Back = new(.055f, .064f, .068f);
    private static readonly Color Housing = new(.12f, .14f, .15f);
    private static readonly Color Edge = new(.28f, .32f, .33f);
    private static readonly Color Metal = new(.57f, .60f, .57f);
    private static readonly Color Shine = new(.78f, .80f, .75f);
    private static readonly Color Set = new(.68f, .91f, .72f);
    private static readonly Color Warning = new(.88f, .65f, .33f);
    private PickSnapshot _state;
    private PickCoaching _coaching;
    private float _rotation;
    private float _lift,
        _pickX,
        _retraction;
    private bool _initialized;
    private int _pinCount;
    private readonly float[] _keys = new float[5], _drivers = new float[5], _seatPulse = new float[5];
    private readonly PickCueReader _cues = new();
    private float _pressure, _commandLift, _phase;
    private bool _reducedMotion;

    public static float PinX(int pin, int count) => 20 + 260f * pin / (count - 1);

    public void ResetAnimation() => _initialized = false;

    public void Render(PickSnapshot state, float deltaTime, bool reducedMotion, PickCoaching coaching = null, float commandLift = 0)
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
        if (reset)
        {
            _cues.Reset(state.Cue);
            System.Array.Clear(_seatPulse, 0, _seatPulse.Length);
            _phase = 0;
        }
        foreach (var cue in _cues.Read(state))
            if (PickPresentation.Fresh(state, cue) && cue.Pin >= 0 && cue.Pin < state.Pins
                && (cue.Sound == PickSound.Seat || cue.Sound == PickSound.Catch))
                _seatPulse[cue.Pin] = cue.Sound == PickSound.Seat ? 1 : .55f;
        _reducedMotion = reducedMotion;
        _phase += Mathf.Max(0, deltaTime) * 43;
        _commandLift = PickPresentation.Unit(commandLift);
        _pressure = Settle(_pressure, PickPresentation.Unit(state.TensionStrength), blend);
        for (var pin = 0; pin < state.Pins; pin++)
        {
            var motion = state.PinMotion != null && state.PinMotion.Length == state.Pins ? state.PinMotion[pin] : null;
            var key = motion == null ? (pin == state.Selected ? lift : 0) : PickPresentation.Unit(motion.KeyLift);
            var driver = motion == null ? key : Mathf.Max(key, PickPresentation.Unit(motion.DriverLift));
            _keys[pin] = Settle(_keys[pin], key, blend);
            _drivers[pin] = Settle(_drivers[pin], driver, blend);
            _seatPulse[pin] = reducedMotion ? 0 : Mathf.Max(0, _seatPulse[pin] - Mathf.Max(0, deltaTime) * 5);
        }
        _pickX = Mathf.Lerp(_pickX, x, blend);
        _lift = Mathf.Lerp(_lift, lift, blend);
        _retraction = Mathf.Lerp(_retraction, retract, blend);
        if (System.Math.Abs(_lift - lift) < .0001f) _lift = lift;
        if (System.Math.Abs(_pickX - x) < .0001f) _pickX = x;
        if (System.Math.Abs(_retraction - retract) < .0001f) _retraction = retract;
        _pinCount = state.Pins;
        _state = state;
        _coaching = coaching;
        _rotation = Settle(_rotation, PickPresentation.RotationDegrees(state.CylinderRotation), blend);
        _initialized = true;
        SetVerticesDirty();
    }

    private static float Settle(float current, float target, float blend)
    {
        var value = Mathf.Lerp(current, target, blend);
        return Mathf.Abs(value - target) < .0001f ? target : value;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect(vh, -500, -95, 1000, 190, Back);
        Line(vh, new(-500, 94), new(500, 94), Edge);
        if (_state == null)
            return;
        var selected = Mathf.Clamp(_state.Selected, 0, _state.Pins - 1);
        var warning =
            _state.Outcome == PickOutcome.Active
            && (
                _state.Feedback == PickFeedback.Strain
                || _state.Strain > .5f
            );
        var tension = _state.Tension && _state.Outcome == PickOutcome.Active;

        // Side elevation of a partial cutaway: fixed shell above, solid cylindrical plug
        // below. The uncut band around the shear line conceals bitting and pin interfaces.
        Rounded(vh, -49, -78, 408, 154, 17, new Color(.023f, .028f, .026f), Housing);
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
            51,
            7,
            new Color(.08f, .079f, .063f),
            new Color(.025f, .029f, .025f)
        );
        // Fixed upper shell and separate exposed spring/driver bores.
        Rounded(vh, -39, ShearY + 1, 383, 64, 5, new Color(.39f, .34f, .21f), new Color(.64f, .59f, .40f));
        Rect(vh, -34, 70, 373, 4, new Color(.78f, .74f, .55f));
        // Open keyway leaves clearance beneath the lowered pins and fixed-length pick.
        Rect(vh, -48, -60, 25, 49, Back);
        Rect(vh, -42, -11, 12, 22, Metal);
        Rect(vh, -40, -11, 3, 22, Shine);
        Line(vh, new(-25, -61), new(319, -61), new Color(.61f, .55f, .36f), 1.2f);
        Ellipse(vh, 335, -31, 8, 27, new Color(.36f, .31f, .18f));
        Ellipse(vh, 333, -30, 5, 23, new Color(.55f, .49f, .30f));
        // The far-side ward rises from the solid plug floor and meets the rear wall.
        // Its broad shaded face makes the resting surface read as metal, not a guide
        // line. The pick travels in the open channel in front of this rear shoulder.
        Rounded(vh, -28, -66, 362, RestTipY + 66, 3,
            new Color(.31f, .26f, .15f), new Color(.40f, .34f, .21f));
        Rounded(vh, -28, RestTipY - 3, 362, 3, 1,
            new Color(.40f, .34f, .21f), new Color(.49f, .43f, .28f));

        // End elevation: the shell stays fixed while the warded keyway turns with the plug.
        Ellipse(vh, 411, -25, 37, 37, Edge);
        Ellipse(vh, 411, -25, 33, 33, new Color(.48f, .43f, .29f));
        Ellipse(vh, 411, -25, 29, 29, Back);
        Ellipse(vh, 411, -25, 27, 27, new Color(.68f, .60f, .39f));
        var angle = _rotation * Mathf.PI / 180;
        Vector2 Plug(float x, float y) => new(411 + x * Mathf.Cos(angle) + y * Mathf.Sin(angle),
            -25 - x * Mathf.Sin(angle) + y * Mathf.Cos(angle));
        Line(vh, Plug(0, -17), Plug(0, 17), Back, 6);
        Line(vh, Plug(-4, 5), Plug(2, 5), Back, 5);
        Line(vh, Plug(-2, -5), Plug(4, -5), Back, 5);
        Line(vh, Plug(0, 21), Plug(0, 26), Shine, 2);
        Line(vh, new(411, 6), new(411, 12), Shine, 2);
        // Pressure is neutral; amber is reserved for actual forcing/strain.
        Rect(vh, -446, 19, 176, 6, Edge);
        Rect(vh, -446, 19, 176 * _pressure, 6, Shine);
        Line(vh, new(-446 + 176 * _pressure, 16), new(-446 + 176 * _pressure, 29), Shine, 2);
        // Adjacent lift tracks distinguish requested travel from the pin's actual response.
        Rect(vh, -222, -28, 4, 56, Edge);
        Rect(vh, -209, -28, 4, 56, Edge);
        Rect(vh, -222, -28, 4, 56 * _lift, Shine);
        Rect(vh, -209, -28, 4, 56 * _commandLift, warning ? Warning : Metal);
        for (var pin = 0; pin < _state.Pins; pin++)
        {
            var x = PinX(pin, _state.Pins);
            var recommended = _coaching != null && _state.Outcome == PickOutcome.Active
                && pin == (_coaching.State == PinState.Overset ? selected : _coaching.BindingPin);
            if (recommended)
            {
                var guide = new Color(.25f, .70f, .85f);
                Line(vh, new(x, 87), new(x, 66), guide, 3);
                Line(vh, new(x - 6, 72), new(x, 66), guide, 3);
                Line(vh, new(x + 6, 72), new(x, 66), guide, 3);
            }
            var bottom = RestTipY + _keys[pin] * PinTravel;
            var pulse = Mathf.Sin(_seatPulse[pin] * Mathf.PI) * 1.5f;
            // Only the exposed upper portion is visible. Hidden length is schematic and
            // never derived from secret target heights; no false "set height" is drawn.
            var driverBottom = -24 + _drivers[pin] * PinTravel + pulse;
            var top = driverBottom + 52;
            Rect(vh, x - 13, 25, 26, 44, new Color(.045f, .048f, .037f));
            Rect(vh, x - 15, 25, 2, 44, new Color(.80f, .74f, .50f));
            Rect(vh, x + 13, 25, 2, 44, new Color(.25f, .21f, .13f));
            Rect(vh, x - 10, RestTipY, 20, 58, new Color(.055f, .053f, .040f));
            var spring = new Vector2(x, top + 1);
            for (var turn = 1; turn <= 72; turn++)
            {
                var phase = turn * Mathf.PI / 6;
                var next = new Vector2(
                    x + Mathf.Sin(phase) * 5,
                    Mathf.Lerp(top + 1, 67, turn / 72f) + (Mathf.Cos(phase) - 1) * .35f
                );
                Line(vh, spring, next, Color.Lerp(Edge, Shine, (Mathf.Cos(phase) + 1) * .5f), 1.1f);
                spring = next;
            }
            Rect(vh, x - 6, 67, 12, 2, Metal);
            // Bullet-shaped key tip continues into the opaque plug wall, never an exposed
            // short cylinder suspended under a spring. Its hidden joint is not rendered.
            KeyPin(vh, x, bottom);
            var type = _state.PinTypes != null && _state.PinTypes.Length == _state.Pins
                ? _state.PinTypes[pin] : PinType.Standard;
            DriverPin(vh, x, driverBottom, type);
            if (_seatPulse[pin] > 0)
                Brackets(vh, x - 17, 29, 34, 35, Shine);
            if (pin == selected)
            {
                var outline = warning ? Warning : Shine;
                Brackets(vh, x - 19, -36, 38, 102, outline);
                Line(vh, new(x - 4, -71), new(x, -67), outline, 1.5f);
                Line(vh, new(x, -67), new(x + 4, -71), outline, 1.5f);
            }
        }

        // Real cylinders retain their pins inside continuous material. Preserve an uncut
        // wall across the shear interface rather than inventing supports beneath each pin.
        Rounded(vh, -39, ShearY + 1, 383, 17, 2, new Color(.39f, .34f, .21f), new Color(.64f, .59f, .40f));
        Rounded(vh, -35, -8, 377, 16, 2, new Color(.38f, .32f, .19f), new Color(.73f, .67f, .45f));
        Line(vh, new(-35, ShearY), new(342, ShearY), Back, 1.5f);
        Line(vh, new(-64, ShearY), new(-36, ShearY), Edge, 1);

        if (_retraction < .99f)
        {
            var x = Mathf.Lerp(_pickX, -50, _retraction);
            var lift = _lift * (1 - _retraction);
            // Tiny rigid rocking about the contact emphasizes strain without stretching
            // the shaft or driving the contact point through the pin.
            var shake = _reducedMotion || !warning ? 0 : Mathf.Sin(_phase) * PickPresentation.Unit(_state.Strain) * .001f;
            Vector2 Point(float px, float py) => PickPoint(new Vector2(
                px * Mathf.Cos(shake) - py * Mathf.Sin(shake),
                px * Mathf.Sin(shake) + py * Mathf.Cos(shake)), x, lift);
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
            var tint = warning ? Warning : tension ? Shine : Metal;
            Vector2 Wrench(float x, float y)
            {
                var a = (_pressure * 3 + Mathf.Min(_rotation, 9) * .2f) * Mathf.PI / 180;
                return new Vector2(-4 + x * Mathf.Cos(a) - y * Mathf.Sin(a),
                    -60 + x * Mathf.Sin(a) + y * Mathf.Cos(a));
            }
            Line(vh, Wrench(-130, -12), Wrench(-62, -12), tint, 4);
            Line(vh, Wrench(-62, -12), Wrench(-62, 0), tint, 4);
            Line(vh, Wrench(-62, 0), Wrench(0, 0), tint, 4);
        }
        // Confirmed-set badges sit with the pin numbers, outside the moving mechanism.
        // Draw last so a lowered pick cannot hide confirmed progress.
        for (var pin = 0; pin < _state.Pins; pin++)
        {
            if (!PickPresentation.TrueSet(_state, pin)) continue;
            var x = PinX(pin, _state.Pins);
            Rounded(vh, x - 30, -94, 60, 20, 3,
                new Color(.04f, .12f, .08f), new Color(.08f, .20f, .12f));
            Line(vh, new(x - 25, -75), new(x + 25, -75), Set, 1.5f);
        }
    }

    // A rigid hook levers against the entrance ward. Insertion changes leverage, not length.
    internal static Vector2 PickPoint(Vector2 local, float tipX, float lift)
    {
        var distance = Mathf.Max(20, tipX - MouthX);
        var rise = RestTipY - FulcrumY + lift * PinTravel;
        var angle = (float)(System.Math.Atan2(rise, distance)
            - System.Math.Asin(18 / System.Math.Sqrt(distance * distance + rise * rise)));
        var c = Mathf.Cos(angle);
        var s = Mathf.Sin(angle);
        return new Vector2(
            tipX + local.x * c - local.y * s,
            RestTipY + lift * PinTravel + local.x * s + local.y * c
        );
    }

    private static void KeyPin(VertexHelper vh, float x, float bottom)
    {
        for (var band = 0; band < 16; band++)
        {
            var shade = Mathf.Pow(Mathf.Max(0, Mathf.Cos((band / 15f - .40f) * Mathf.PI)), .7f);
            var tint = Color.Lerp(new Color(.28f, .22f, .10f), new Color(.85f, .75f, .46f), shade);
            var dx = band - 8;
            var bevel = Mathf.Abs(dx + .5f) * .75f;
            Rect(vh, x + dx, bottom + bevel, 1, 26 - bottom - bevel, tint);
        }
    }

    private static void DriverPin(VertexHelper vh, float x, float bottom, PinType type)
    {
        // Chamfered flat ends, spool flanges and narrow annular serrations. The actual
        // pin interfaces and catching edges remain behind the uncut shear-line wall.
        var dark = new Color(.20f, .23f, .23f);
        var light = new Color(.80f, .84f, .82f);
        for (var row = 0; row < 52; row++)
        {
            if (bottom + row + 1 <= 26) continue;
            var radius = type == PinType.Spool
                ? (row >= 8 && row < 44 ? 4f : 8f)
                : type == PinType.Serrated && row >= 10 && row < 44 && row % 8 < 2 ? 6f : 8f;
            for (var band = 0; band < 16; band++)
            {
                var lighting = Mathf.Pow(Mathf.Max(0, Mathf.Cos((band / 15f - .40f) * Mathf.PI)), .7f);
                var y = Mathf.Max(26, bottom + row);
                Rect(vh, x - radius + band * radius / 8, y,
                    radius / 8, bottom + row + 1 - y, Color.Lerp(dark, light, lighting));
            }
        }
        Ellipse(vh, x, bottom + 52, 8, 1, light);
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
        // The hand/grip can leave this small inspection window at steep leverage angles.
        if (x - rx < -500 || x + rx > 500 || y - ry < -95 || y + ry > 95) return;
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
        // Clip the visible portion of the rigid tool rather than shortening or bending it
        // to fit the panel when levering a pin near the keyway entrance.
        var delta = b - a;
        var enter = 0f;
        var exit = 1f;
        var half = width / 2;
        if (!Clip(-delta.x, a.x + 500 - half, ref enter, ref exit)
            || !Clip(delta.x, 500 - half - a.x, ref enter, ref exit)
            || !Clip(-delta.y, a.y + 95 - half, ref enter, ref exit)
            || !Clip(delta.y, 95 - half - a.y, ref enter, ref exit)) return;
        b = a + delta * exit;
        a += delta * enter;
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

    private static bool Clip(float direction, float distance, ref float enter, ref float exit)
    {
        if (Mathf.Abs(direction) < .000001f) return distance >= 0;
        var t = distance / direction;
        if (direction < 0)
        {
            if (t > exit) return false;
            enter = Mathf.Max(enter, t);
        }
        else
        {
            if (t < enter) return false;
            exit = Mathf.Min(exit, t);
        }
        return true;
    }
}
