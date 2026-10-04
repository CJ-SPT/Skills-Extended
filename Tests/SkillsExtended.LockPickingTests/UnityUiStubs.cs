// Geometry-only harness: compiles the production cutaway without starting Unity.
namespace UnityEngine
{
    public struct Vector2(float x, float y)
    {
        public float x = x,
            y = y;
        public static Vector2 zero => new(0, 0);
        public Vector2 normalized
        {
            get
            {
                var length = MathF.Sqrt(x * x + y * y);
                return length > 0 ? new(x / length, y / length) : zero;
            }
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);

        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);

        public static Vector2 operator *(Vector2 a, float s) => new(a.x * s, a.y * s);
    }

    public struct Color(float r, float g, float b, float a = 1)
    {
        public float r = r,
            g = g,
            b = b,
            a = a;

        public static Color Lerp(Color a, Color b, float t) =>
            new(
                a.r + (b.r - a.r) * t,
                a.g + (b.g - a.g) * t,
                a.b + (b.b - a.b) * t,
                a.a + (b.a - a.a) * t
            );
    }

    public static class Mathf
    {
        public static float Abs(float value) => MathF.Abs(value);
        public const float PI = MathF.PI;

        public static float Sin(float x) => MathF.Sin(x);

        public static float Cos(float x) => MathF.Cos(x);

        public static float Pow(float x, float y) => MathF.Pow(x, y);

        public static float Sqrt(float x) => MathF.Sqrt(x);

        public static float Min(float x, float y) => MathF.Min(x, y);

        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);

        public static float Clamp01(float value) => Math.Clamp(value, 0, 1);

        public static float Max(float a, float b) => Math.Max(a, b);

        public static float Exp(float value) => MathF.Exp(value);

        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
    }
}

namespace UnityEngine.UI
{
    public abstract class MaskableGraphic
    {
        protected void SetVerticesDirty() { }

        protected abstract void OnPopulateMesh(VertexHelper helper);

        public VertexHelper CaptureMesh()
        {
            var mesh = new VertexHelper();
            OnPopulateMesh(mesh);
            return mesh;
        }
    }

    public class VertexHelper
    {
        public List<(UnityEngine.Vector2 Point, UnityEngine.Color Tint)> Vertices = new();
        public List<int[]> Triangles = new();
        public int currentVertCount => Vertices.Count;

        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
        }

        public void AddVert(UnityEngine.Vector2 p, UnityEngine.Color c, UnityEngine.Vector2 uv) =>
            Vertices.Add((p, c));

        public void AddTriangle(int a, int b, int c) => Triangles.Add([a, b, c]);
    }
}
