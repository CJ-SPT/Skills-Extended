using System.Text.Json;
using SkillsExtended.LockPicking;
using SkillsExtended.Skills.LockPicking;
using UnityEngine.UI;

internal static class CutawayChecks
{
    private static string SurfaceAt(VertexHelper mesh, float x, float y)
    {
        // Last covering triangle is the opaque surface visible to the player.
        var result = "";
        foreach (var triangle in mesh.Triangles)
        {
            var a = mesh.Vertices[triangle[0]];
            var b = mesh.Vertices[triangle[1]];
            var c = mesh.Vertices[triangle[2]];
            var d = (b.Point.y - c.Point.y) * (a.Point.x - c.Point.x) + (c.Point.x - b.Point.x) * (a.Point.y - c.Point.y);
            if (Math.Abs(d) < .00001f) continue;
            var u = ((b.Point.y - c.Point.y) * (x - c.Point.x) + (c.Point.x - b.Point.x) * (y - c.Point.y)) / d;
            var v = ((c.Point.y - a.Point.y) * (x - c.Point.x) + (a.Point.x - c.Point.x) * (y - c.Point.y)) / d;
            if (u >= 0 && v >= 0 && u + v <= 1)
                result = JsonSerializer.Serialize(new[] { a.Tint.r, a.Tint.g, a.Tint.b, a.Tint.a });
        }
        return result;
    }
    private static string Geometry(VertexHelper mesh) =>
        JsonSerializer.Serialize(
            new
            {
                vertices = mesh.Vertices.Select(v =>
                    new[] { v.Point.x, v.Point.y, v.Tint.r, v.Tint.g, v.Tint.b, v.Tint.a }
                ),
                triangles = mesh.Triangles,
            }
        );

    public static void Run(Action<bool, string> check, string output)
    {
        if (output != null)
            Directory.CreateDirectory(output);
        var toolPoints = new[]
        {
            new UnityEngine.Vector2(-438, -18),
            new UnityEngine.Vector2(-389, -18),
            new UnityEngine.Vector2(-26, -18),
            new UnityEngine.Vector2(0, -1.5f),
        };
        static float Distance(UnityEngine.Vector2 a, UnityEngine.Vector2 b) =>
            MathF.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
        foreach (var pins in new[] { 3, 4, 5 })
        foreach (var selected in Enumerable.Range(0, pins))
        foreach (var lift in new[] { 0f, .25f, .5f, .75f, 1f })
        {
            var tip = LockPickingCutaway.PinX(selected, pins);
            for (var point = 1; point < toolPoints.Length; point++)
                check(
                    Math.Abs(
                        Distance(toolPoints[0], toolPoints[point])
                            - Distance(
                                LockPickingCutaway.PickPoint(toolPoints[0], tip, lift),
                                LockPickingCutaway.PickPoint(toolPoints[point], tip, lift)
                            )
                    ) < .001f,
                    "Pick grip, shaft and hook retain fixed dimensions throughout insertion and lift"
                );
            var grip = LockPickingCutaway.PickPoint(new UnityEngine.Vector2(-383, -18), tip, lift);
            check(grip.x < -43, "The handle remains outside the cylinder at maximum insertion");
            var shaftA = LockPickingCutaway.PickPoint(new UnityEngine.Vector2(-389, -18), tip, lift);
            var shaftB = LockPickingCutaway.PickPoint(new UnityEngine.Vector2(-26, -18), tip, lift);
            var mouthY = shaftA.y + (LockPickingCutaway.MouthX - shaftA.x) * (shaftB.y - shaftA.y) / (shaftB.x - shaftA.x);
            check(Math.Abs(mouthY - LockPickingCutaway.FulcrumY) < .001f,
                "The rigid shaft stays on the entrance ward while insertion changes leverage");
            var contact = LockPickingCutaway.PickPoint(UnityEngine.Vector2.zero, tip, lift);
            check(Math.Abs(contact.x - tip) < .001f
                && Math.Abs(contact.y - (LockPickingCutaway.RestTipY + lift * LockPickingCutaway.PinTravel)) < .001f,
                "The hook's upper contact stays beneath the lifted pin tip");
        }
        foreach (var pins in new[] { 3, 4, 5 })
        foreach (var selected in Enumerable.Range(0, pins))
        foreach (var lift in new[] { 0f, 1f })
        foreach (
            var outcome in new[]
            {
                PickOutcome.Active,
                PickOutcome.Unlocked,
                PickOutcome.PickBroken,
            }
        )
        foreach (var reduced in new[] { false, true })
        {
            var view = new LockPickingCutaway();
            var state = new PickSnapshot
            {
                Pins = pins,
                Selected = selected,
                Lift = lift,
                Tension = true,
                Outcome = outcome,
                Feedback = lift == 1 ? PickFeedback.Strain : PickFeedback.Searching,
                Strain = lift,
            };
            view.Render(state, .016f, reduced);
            var mesh = view.CaptureMesh();
            check(
                mesh.Vertices.All(v =>
                    float.IsFinite(v.Point.x)
                    && float.IsFinite(v.Point.y)
                    && v.Point.x >= -500
                    && v.Point.x <= 500
                    && v.Point.y >= -95
                    && v.Point.y <= 95
                ),
                "Cutaway mesh stays inside its panel"
            );
            check(
                mesh.Triangles.All(t => t.All(i => i >= 0 && i < mesh.Vertices.Count)),
                "Cutaway triangles reference valid vertices"
            );
            foreach (var size in new[] { (1920, 1080), (2560, 1440), (3440, 1440) })
            {
                var scale = Math.Min(size.Item1 / 1920f, size.Item2 / 1080f);
                check(
                    mesh.Vertices.All(v =>
                        Math.Abs(v.Point.x * scale) < size.Item1 / 2f
                        && Math.Abs((v.Point.y - 120) * scale) < size.Item2 / 2f
                    ),
                    "Scaled cutaway remains on screen"
                );
            }
            if (output != null && reduced && (selected == 0 || selected == pins - 1))
                File.WriteAllText(
                    Path.Combine(output, $"cutaway-{pins}-{selected}-{lift}-{outcome}.json"),
                    Geometry(mesh)
                );
        }
        var graphic = new LockPickingCutaway();
        PickSnapshot State(bool[] flags = null) =>
            new()
            {
                Pins = 3,
                Selected = 1,
                Lift = .5f,
                Tension = true,
                SetPinStates = flags,
            };
        graphic.Render(State(), .016f, true);
        var unknown = Geometry(graphic.CaptureMesh());
        graphic.Render(State([true, false, false]), .016f, true);
        var confirmed = Geometry(graphic.CaptureMesh());
        check(confirmed != unknown, "Normal play shows authoritative true sets without coaching");
        graphic.Render(State(), .016f, true);
        check(Geometry(graphic.CaptureMesh()) == unknown, "Missing snapshot flags do not invent true sets");
        var advice = new PickCoaching { BindingPin = 2 };
        graphic.Render(State([true, false, false]), .016f, true, advice);
        var recommended = Geometry(graphic.CaptureMesh());
        advice.BindingPin = 1;
        graphic.Render(State([true, false, false]), .016f, true, advice);
        check(Geometry(graphic.CaptureMesh()) != recommended, "Recommended pin marker moves independently of selected and set pins");
        graphic.Render(State([true, false, false]), .016f, true);
        check(Geometry(graphic.CaptureMesh()) == confirmed, "Coaching off removes guidance but retains confirmed sets");
        graphic.Render(State([true]), .016f, true);
        check(Geometry(graphic.CaptureMesh()) == unknown, "Malformed set flags are not displayed");
        var profiled = State();
        profiled.PinTypes = [PinType.Standard, PinType.Spool, PinType.Serrated];
        graphic.Render(profiled, .016f, true);
        var shapes = Geometry(graphic.CaptureMesh());
        if (output != null) File.WriteAllText(Path.Combine(output, "pin-types.json"), shapes);
        check(shapes != unknown, "Raid cutaway distinguishes visible security-pin shapes without coaching");
        profiled.PinTypes = [PinType.Standard, PinType.Serrated, PinType.Spool];
        graphic.Render(profiled, .016f, true);
        check(Geometry(graphic.CaptureMesh()) != shapes, "Spool waists and serrated grooves have different profiles");
        profiled.PinTypes = [PinType.Spool];
        graphic.Render(profiled, .016f, true);
        check(Geometry(graphic.CaptureMesh()) == unknown, "Malformed visible profiles fall back to neutral pins");
        var rotation = State();
        rotation.CylinderRotation = .1f;
        graphic.Render(rotation, .016f, true);
        check(Geometry(graphic.CaptureMesh()) != unknown, "False-set rotation is visible without audio or vibration");
        var moving = State();
        moving.Selected = 2;
        moving.Lift = 1;
        graphic.Render(moving, .016f, false);
        var smoothing = Geometry(graphic.CaptureMesh());
        graphic.Render(moving, .016f, true);
        var direct = Geometry(graphic.CaptureMesh());
        check(smoothing != direct, "Normal movement smooths while reduced motion updates directly");
        graphic.ResetAnimation();
        graphic.Render(moving, 0, false);
        check(
            Geometry(graphic.CaptureMesh()) == direct,
            "Retry discards stale animation positions"
        );
        var complete = State([true, true, true]);
        complete.Outcome = PickOutcome.Unlocked;
        graphic.Render(complete, .016f, false);
        for (var frame = 0; frame < 120; frame++)
            graphic.Render(complete, .016f, false);
        var finished = Geometry(graphic.CaptureMesh());
        graphic.Render(complete, .016f, true);
        check(
            Geometry(graphic.CaptureMesh()) == finished,
            "Unlock animation settles to the reduced-motion result"
        );
        graphic.Render(State(), .016f, false);
        check(
            Geometry(graphic.CaptureMesh()) == unknown,
            "New active attempt resets terminal animation"
        );
        foreach (var pins in new[] { 3, 4, 5 })
        {
            var badges = new LockPickingCutaway();
            var observed = new PickSnapshot { Pins = pins, Tension = true };
            var flags = Enumerable.Range(0, pins).Select(p => p % 2 == 0).ToArray();
            foreach (var selected in Enumerable.Range(0, pins))
            foreach (var lift in new[] { 0f, 1f })
            {
                observed.Selected = selected;
                observed.Lift = lift;
                observed.SetPinStates = null;
                badges.Render(observed, 0, true);
                var noBadges = badges.CaptureMesh();
                observed.SetPinStates = flags;
                badges.Render(observed, 0, true);
                var visibleBadges = badges.CaptureMesh();
                for (var pin = 0; pin < pins; pin++)
                    check((SurfaceAt(visibleBadges, LockPickingCutaway.PinX(pin, pins), -84)
                        != SurfaceAt(noBadges, LockPickingCutaway.PinX(pin, pins), -84)) == flags[pin],
                        "True-set badge remains visible at its label through insertion and lift; unconfirmed pins stay neutral");
                check(visibleBadges.Vertices.All(v => v.Point.y >= -95 && v.Point.y <= 95),
                    "Confirmed-set badge fits the existing compact panel");
            }
            if (output != null)
                File.WriteAllText(Path.Combine(output, $"true-set-badges-{pins}.json"), Geometry(badges.CaptureMesh()));
            flags[0] = false;
            badges.Render(observed, 0, true);
            var droppedBadge = SurfaceAt(badges.CaptureMesh(), LockPickingCutaway.PinX(0, pins), -84);
            observed.SetPinStates = null;
            badges.Render(observed, 0, true);
            check(droppedBadge == SurfaceAt(badges.CaptureMesh(), LockPickingCutaway.PinX(0, pins), -84),
                "Authoritative loss of true-set state immediately clears that pin's badge");
            observed.Tension = false;
            badges.Render(observed, 0, true);
            var releasedBadges = badges.CaptureMesh();
            observed.SetPinStates = flags;
            badges.Render(observed, 0, true);
            for (var pin = 0; pin < pins; pin++)
                check(SurfaceAt(badges.CaptureMesh(), LockPickingCutaway.PinX(pin, pins), -84)
                    == SurfaceAt(releasedBadges, LockPickingCutaway.PinX(pin, pins), -84),
                    "Full release removes true-set badges even before old snapshot flags clear");

            var partial = new LockPickingCutaway();
            var cover = new PickSnapshot { Pins = pins };
            partial.Render(cover, 0, true);
            var baseline = partial.CaptureMesh();
            foreach (var lift in new[] { 0f, .25f, .5f, .75f, 1f })
            {
                cover.Lift = lift;
                cover.PinTypes = Enumerable.Repeat(PinType.Spool, pins).ToArray();
                cover.PinMotion = Enumerable.Range(0, pins).Select(_ => new PickPinMotion { KeyLift = lift, DriverLift = 1 }).ToArray();
                partial.Render(cover, 0, true);
                var exposed = partial.CaptureMesh();
                for (var pin = 0; pin < pins; pin++)
                foreach (var y in new[] { LockPickingCutaway.ShearY - 4, LockPickingCutaway.ShearY + 5 })
                {
                    var x = LockPickingCutaway.PinX(pin, pins);
                    check(SurfaceAt(exposed, x, y) == SurfaceAt(baseline, x, y),
                        "Uncut plug/shell walls conceal the pin interfaces through every lift and retention state");
                }
            }
        }
    }
}
