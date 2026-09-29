using System;
using System.Linq;
using SkillsExtended.Signals;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.Signals;

public sealed class SignalGraphic : MaskableGraphic
{
    public SignalsView View;
    public bool Spectrum;

    private void Line(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width = 1)
    {
        var n = (b - a).normalized;
        var p = new Vector2(-n.y, n.x) * width / 2;
        var i = vh.currentVertCount;
        vh.AddVert(a - p, color, Vector2.zero);
        vh.AddVert(a + p, color, Vector2.zero);
        vh.AddVert(b + p, color, Vector2.zero);
        vh.AddVert(b - p, color, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (!View || View.Manifest == null || View.State == null)
            return;
        var r = rectTransform.rect;
        var green = new Color(.2f, .85f, .73f);
        var amber = new Color(1, .7f, .3f);
        for (var x = r.xMin; x <= r.xMax; x += 40)
            Line(vh, new(x, r.yMin), new(x, r.yMax), new Color(.1f, .21f, .23f));
        for (var y = r.yMin; y <= r.yMax; y += 40)
            Line(vh, new(r.xMin, y), new(r.xMax, y), new Color(.1f, .21f, .23f));
        if (Spectrum)
        {
            var previous = Vector2.zero;
            var other = Vector2.zero;
            for (var i = 0; i <= 200; i++)
            {
                var x = r.xMin + r.width * i / 200;
                var value = View.Pairing
                    ? .5
                        + .35
                            * Math.Sin(
                                i * .13
                                    + SignalsModel.PairPhase(View.Manifest.Seed, View.Clock)
                                        * Math.PI
                                        / 180
                            )
                    : .12
                        + .7
                            * Math.Exp(
                                -Math.Pow(((88 + i * .1) - View.Manifest.Frequency) / .35, 2)
                            )
                        + .03 * Math.Sin(i * 1.7);
                if (View.State.Unlocked)
                    value = .08;
                var p = new Vector2(x, r.yMin + (float)value * r.height);
                if (i > 0)
                    Line(vh, previous, p, green, 2);
                previous = p;
                if (View.Pairing)
                {
                    var q = new Vector2(
                        x,
                        r.yMin
                            + (float)(.5 + .35 * Math.Sin(i * .13 + View.Phase * Math.PI / 180))
                                * r.height
                    );
                    if (i > 0)
                        Line(vh, other, q, amber, 2);
                    other = q;
                }
            }
            if (!View.Pairing)
            {
                var cursor = r.xMin + (View.Frequency - 88) / 20 * r.width;
                Line(vh, new(cursor, r.yMin), new(cursor, r.yMax), amber, 2);
            }
            return;
        }
        var readings = SignalsModel.PlottedReadings(View.State, View.Level).ToArray();
        var points = readings.Select(x => x.Position).Concat(new[] { View.Position }).ToList();
        if (View.State.HasFix)
        {
            var e = View.State.Estimate;
            var radius = View.State.Radius;
            points.Add(new SignalPoint { X = e.X - radius, Z = e.Z - radius });
            points.Add(new SignalPoint { X = e.X + radius, Z = e.Z + radius });
        }
        var minX = points.Min(p => p.X) - 100;
        var maxX = points.Max(p => p.X) + 100;
        var minZ = points.Min(p => p.Z) - 100;
        var maxZ = points.Max(p => p.Z) + 100;
        var scale = Math.Min(r.width / (maxX - minX), r.height / (maxZ - minZ));
        Vector2 Project(SignalPoint p) =>
            new(
                r.center.x + (p.X - (minX + maxX) / 2) * scale,
                r.center.y + (p.Z - (minZ + maxZ) / 2) * scale
            );
        foreach (var reading in readings)
        {
            var origin = Project(reading.Position);
            foreach (var offset in new[] { -reading.Uncertainty, 0, reading.Uncertainty })
            {
                var angle = (reading.Bearing + offset) * Math.PI / 180;
                var direction = new Vector2((float)Math.Sin(angle), (float)Math.Cos(angle));
                var tx =
                    Math.Abs(direction.x) < .0001f
                        ? float.MaxValue
                        : (direction.x > 0 ? r.xMax - origin.x : r.xMin - origin.x) / direction.x;
                var ty =
                    Math.Abs(direction.y) < .0001f
                        ? float.MaxValue
                        : (direction.y > 0 ? r.yMax - origin.y : r.yMin - origin.y) / direction.y;
                var end = origin + direction * Math.Min(tx, ty);
                Line(
                    vh,
                    origin,
                    end,
                    offset == 0 ? green : new Color(.1f, .36f, .32f),
                    offset == 0 ? 2 : 1
                );
            }
        }
        var player = Project(View.Position);
        Line(vh, player - Vector2.one * 5, player + Vector2.one * 5, Color.white, 3);
        Line(vh, player + new Vector2(-5, 5), player + new Vector2(5, -5), Color.white, 3);
        if (View.State.HasFix)
        {
            var center = Project(View.State.Estimate);
            var radius = View.State.Radius * scale;
            for (var i = 0; i < 64; i++)
            {
                Vector2 Circle(int n) =>
                    center
                    + new Vector2(
                        (float)Math.Sin(n * Math.PI / 32),
                        (float)Math.Cos(n * Math.PI / 32)
                    ) * radius;
                Line(vh, Circle(i), Circle(i + 1), amber, 2);
            }
        }
    }
}
