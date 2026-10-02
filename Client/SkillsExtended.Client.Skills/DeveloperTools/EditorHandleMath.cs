using System;
using System.Numerics;

namespace SkillsExtended.DeveloperTools;

internal static class EditorHandleMath
{
    public static Vector3 Axis(int axis, float yaw)
    {
        var local = axis == 0 ? Vector3.UnitX : axis == 1 ? Vector3.UnitY : Vector3.UnitZ;
        return Vector3.Transform(local, Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw * (float)Math.PI / 180));
    }

    public static bool TryAxisDistance(Vector3 rayOrigin, Vector3 rayDirection, Vector3 pivot,
        Vector3 axis, out float distance)
    {
        var b = Vector3.Dot(axis, rayDirection);
        var offset = pivot - rayOrigin;
        var denominator = 1 - b * b;
        distance = 0;
        if (denominator < .0001f) return false;
        distance = (b * Vector3.Dot(rayDirection, offset) - Vector3.Dot(axis, offset)) / denominator;
        return true;
    }

    public static float RotationDelta(Vector2 pivot, Vector2 previous, Vector2 current)
    {
        var from = previous - pivot;
        var to = current - pivot;
        if (from.LengthSquared() < .01f || to.LengthSquared() < .01f) return 0;
        return (float)(Math.Atan2(from.X * to.Y - from.Y * to.X, Vector2.Dot(from, to)) * 180 / Math.PI);
    }
}
