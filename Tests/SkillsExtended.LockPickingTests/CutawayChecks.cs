using System.Text.Json;
using SkillsExtended.LockPicking;
using SkillsExtended.Skills.LockPicking;
using UnityEngine.UI;

internal static class CutawayChecks
{
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
                SetPinStates = Enumerable
                    .Range(0, pins)
                    .Select(p => outcome == PickOutcome.Unlocked || p < selected)
                    .ToArray(),
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
        graphic.Render(State([true]), .016f, true);
        check(
            Geometry(graphic.CaptureMesh()) == unknown,
            "Wrong-length flags do not invent completed pins"
        );
        graphic.Render(State([false, false, false]), .016f, true);
        check(Geometry(graphic.CaptureMesh()) == unknown, "Unknown flags use neutral pins");
        graphic.Render(State([true, false, false]), .016f, true);
        check(
            Geometry(graphic.CaptureMesh()) != unknown,
            "Completed flags visibly change the diagram"
        );
        var released = State([true, true, true]);
        released.Tension = false;
        graphic.Render(released, .016f, true);
        var release = Geometry(graphic.CaptureMesh());
        released.SetPinStates = [false, false, false];
        graphic.Render(released, .016f, true);
        check(
            Geometry(graphic.CaptureMesh()) == release,
            "Release removes completion marks immediately"
        );
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
    }
}
