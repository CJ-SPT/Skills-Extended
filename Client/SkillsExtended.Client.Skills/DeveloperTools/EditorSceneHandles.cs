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
    internal EditorSceneHandles()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute; style.left = style.top = style.right = style.bottom = 0;
        generateVisualContent += Draw;
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
            var colors = new[] { new Color(.95f, .35f, .3f), new Color(.35f, .95f, .45f), new Color(.35f, .6f, 1) };
            for (var i = 0; i < 3; i++)
            {
                painter.strokeColor = colors[i]; painter.BeginPath(); painter.MoveTo(Center); painter.LineTo(Ends[i]);
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
    internal static float AxisDistance(Ray ray, Vector3 origin, Vector3 axis)
    {
        var b = Vector3.Dot(axis, ray.direction); var w = origin - ray.origin;
        var denominator = 1 - b * b;
        return denominator < .0001f ? 0 : (b * Vector3.Dot(ray.direction, w) - Vector3.Dot(axis, w)) / denominator;
    }
}
