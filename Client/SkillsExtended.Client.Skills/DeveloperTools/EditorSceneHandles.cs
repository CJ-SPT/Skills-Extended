using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillsExtended.DeveloperTools;

internal sealed class EditorSceneHandles : VisualElement
{
    internal Vector2 Center;
    internal readonly Vector2[] Ends = new Vector2[3];
    internal bool Rotate;
    internal bool Active;
    private bool _paintedActive, _paintedRotate;
    private Vector2 _paintedCenter;
    private readonly Vector2[] _paintedEnds = new Vector2[3];
    private static readonly Color[] AxisColors =
        { new(.95f, .35f, .3f), new(.35f, .95f, .45f), new(.35f, .6f, 1) };
    internal EditorSceneHandles()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute; style.left = style.top = style.right = style.bottom = 0;
        generateVisualContent += Draw;
    }
    internal void RepaintIfChanged()
    {
        var changed = Active != _paintedActive;
        if (Active)
        {
            changed |= Rotate != _paintedRotate || !Center.Equals(_paintedCenter);
            if (!Rotate)
                for (var i = 0; i < 3; i++) changed |= !Ends[i].Equals(_paintedEnds[i]);
        }
        if (!changed) return;
        _paintedActive = Active; _paintedRotate = Rotate; _paintedCenter = Center;
        for (var i = 0; i < 3; i++) _paintedEnds[i] = Ends[i];
        MarkDirtyRepaint();
    }
    private void Draw(MeshGenerationContext ctx)
    {
        if (!Active) return;
        var painter = ctx.painter2D;
        painter.lineWidth = 3;
        if (Rotate)
        {
            painter.strokeColor = new Color(.85f, .75f, .35f);
            painter.BeginPath();
            for (var i = 0; i <= 64; i++)
            {
                var angle = i * Mathf.PI * 2 / 64;
                var p = Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 48;
                if (i == 0) painter.MoveTo(p); else painter.LineTo(p);
            }
            painter.Stroke();
        }
        else
        {
            for (var i = 0; i < 3; i++)
            {
                painter.strokeColor = AxisColors[i]; painter.BeginPath(); painter.MoveTo(Center); painter.LineTo(Ends[i]);
                painter.Stroke();
                var tangent = (Ends[i] - Center).normalized;
                var cross = new Vector2(-tangent.y, tangent.x);
                painter.BeginPath(); painter.MoveTo(Ends[i] - tangent * 9 + cross * 5);
                painter.LineTo(Ends[i]); painter.LineTo(Ends[i] - tangent * 9 - cross * 5); painter.Stroke();
            }
        }
    }
    internal int Pick(Vector2 pointer)
    {
        if (!Active) return -1;
        if (Rotate) return Math.Abs(Vector2.Distance(pointer, Center) - 48) < 12 ? 3 : -1;
        var best = 11f; var axis = -1;
        for (var i = 0; i < 3; i++)
        {
            var direction = Ends[i] - Center;
            if (direction.sqrMagnitude < 36) continue;
            var t = Mathf.Clamp01(Vector2.Dot(pointer - Center, direction) / direction.sqrMagnitude);
            if (t < .15f) continue;
            var distance = Vector2.Distance(pointer, Center + direction * t);
            if (distance < best) { axis = i; best = distance; }
        }
        return axis;
    }
    internal static Vector2 Project(Camera camera, VisualElement root, Vector3 position, out bool visible)
    {
        var pixel = camera.WorldToScreenPoint(position);
        visible = pixel.z > .01f;
        return RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(pixel.x, Screen.height - pixel.y));
    }
    internal static bool TryAxisDistance(Ray ray, Vector3 origin, Vector3 axis, out float distance)
    {
        return EditorHandleMath.TryAxisDistance(
            new(ray.origin.x, ray.origin.y, ray.origin.z), new(ray.direction.x, ray.direction.y, ray.direction.z),
            new(origin.x, origin.y, origin.z), new(axis.x, axis.y, axis.z), out distance);
    }
    internal static Vector3 Axis(int axis, float yaw)
    {
        var direction = EditorHandleMath.Axis(axis, yaw);
        return new Vector3(direction.X, direction.Y, direction.Z);
    }
    internal static float RotationDelta(Vector2 pivot, Vector2 previous, Vector2 current)
    {
        return EditorHandleMath.RotationDelta(new(pivot.x, pivot.y), new(previous.x, previous.y), new(current.x, current.y));
    }
}
