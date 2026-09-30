using UnityEngine;

namespace SkillsExtended.Skills.Signals;

public sealed partial class SignalsView
{
    public void VerifyNavigation()
    {
        var originalPosition = _position;
        var originalFix = State.HasFix;
        var originalBearing = Bearing;
        var rect = _plot.rectTransform.rect;
        foreach (var hasFix in new[] { false, true })
        foreach (var x in new[] { -3000f, 0, 200, 3000 })
        foreach (var z in new[] { -3000f, 0, 150, 3000 })
        foreach (var bearing in new[] { 0f, 90, 180, 270, 359 })
        {
            State.HasFix = hasFix;
            _position = new() { X = x, Z = z };
            _bearing.value = bearing;
            var vertices = _plot.Mesh().Vertices;
            var player = vertices.Where(v => v.Color == Color.white).ToArray();
            if (
                player.Length != 8
                || Math.Abs(player.Average(v => v.Point.x) - rect.center.x) > .01f
                || Math.Abs(player.Average(v => v.Point.y) - rect.center.y) > .01f
            )
                throw new Exception("Player marker must remain centered throughout travel.");
            if (
                vertices.Any(v =>
                    !float.IsFinite(v.Point.x)
                    || !float.IsFinite(v.Point.y)
                    || v.Point.x < rect.xMin - 2
                    || v.Point.x > rect.xMax + 2
                    || v.Point.y < rect.yMin - 2
                    || v.Point.y > rect.yMax + 2
                )
            )
                throw new Exception(
                    "Bearings and search circle must stay within the plot during travel."
                );
        }
        _position = originalPosition;
        State.HasFix = originalFix;
        _bearing.value = originalBearing;
        if (_recording && !Pairing && !_status.text.Contains("BEARING"))
            throw new Exception(
                "Three stored readings must still show actionable bearing controls."
            );
    }
}
