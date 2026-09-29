using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.Signals;

// Resolution-independent chamfered housing and control faces, rendered by Unity UI.
public sealed class PdaSurface : MaskableGraphic
{
    public Color Top = new(.16f, .19f, .2f);
    public Color Bottom = new(.06f, .08f, .09f);
    public Color Edge = new(.29f, .34f, .35f);
    public float Corner = 8;
    public float Bevel = 2;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        Polygon(vh, r, Corner, Edge, Bottom * .65f);
        r = new Rect(r.x + Bevel, r.y + Bevel, r.width - Bevel * 2, r.height - Bevel * 2);
        Polygon(vh, r, Mathf.Max(0, Corner - Bevel), Top, Bottom);
    }

    private void Polygon(VertexHelper vh, Rect r, float corner, Color top, Color bottom)
    {
        var cut = Mathf.Min(corner, Mathf.Min(r.width, r.height) * .5f);
        var points = new[]
        {
            new Vector2(r.xMin + cut, r.yMin),
            new Vector2(r.xMax - cut, r.yMin),
            new Vector2(r.xMax, r.yMin + cut),
            new Vector2(r.xMax, r.yMax - cut),
            new Vector2(r.xMax - cut, r.yMax),
            new Vector2(r.xMin + cut, r.yMax),
            new Vector2(r.xMin, r.yMax - cut),
            new Vector2(r.xMin, r.yMin + cut),
        };
        var start = vh.currentVertCount;
        vh.AddVert(r.center, Color.Lerp(bottom, top, .5f) * color, Vector2.zero);
        foreach (var p in points)
            vh.AddVert(p, Color.Lerp(bottom, top, (p.y - r.yMin) / r.height) * color, Vector2.zero);
        for (var i = 0; i < points.Length; i++)
            vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Length);
    }
}
