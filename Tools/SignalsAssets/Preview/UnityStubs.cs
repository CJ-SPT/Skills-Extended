// Offline layout adapter only. Executes the production skin/mesh builders without Unity.
namespace UnityEngine
{
    public class Object
    {
        public static implicit operator bool(Object o) => o != null;
    }

    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;

        public T GetComponent<T>()
            where T : Component => gameObject.Components.OfType<T>().FirstOrDefault();
    }

    public class MonoBehaviour : Component { }

    public class GameObject : Object
    {
        public string name;
        public bool active = true;
        public RectTransform transform;
        public List<Component> Components = new();

        public GameObject(string name = "", params Type[] types)
        {
            this.name = name;
            transform = new RectTransform { gameObject = this };
            Components.Add(transform);
            foreach (var t in types.Where(t => t != typeof(RectTransform)))
                AddComponent(t);
        }

        public Component AddComponent(Type t)
        {
            var c = (Component)Activator.CreateInstance(t);
            c.gameObject = this;
            Components.Add(c);
            return c;
        }

        public T AddComponent<T>()
            where T : Component, new() => (T)AddComponent(typeof(T));

        public T GetComponent<T>()
            where T : Component => Components.OfType<T>().FirstOrDefault();

        public T[] GetComponentsInChildren<T>(bool inactive)
            where T : Component =>
            Components
                .OfType<T>()
                .Concat(
                    transform.Children.SelectMany(c =>
                        c.gameObject.GetComponentsInChildren<T>(inactive)
                    )
                )
                .ToArray();

        public void SetActive(bool active) => this.active = active;
    }

    public class Transform : Component
    {
        public RectTransform Parent;
        public List<RectTransform> Children = new();

        public void SetParent(Transform parent, bool _)
        {
            Parent = (RectTransform)parent;
            Parent.Children.Add((RectTransform)this);
        }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin,
            anchorMax,
            pivot = new(.5f, .5f),
            anchoredPosition,
            sizeDelta,
            offsetMin,
            offsetMax;
        public Vector2 Size =>
            Parent == null
                ? sizeDelta
                : new(
                    Parent.Size.x * (anchorMax.x - anchorMin.x) + sizeDelta.x,
                    Parent.Size.y * (anchorMax.y - anchorMin.y) + sizeDelta.y
                );
        public Vector2 Origin =>
            Parent == null
                ? Vector2.zero
                : Parent.Origin
                    + new Vector2(
                        Parent.Size.x * (anchorMin.x + (anchorMax.x - anchorMin.x) * pivot.x)
                            + anchoredPosition.x
                            - Size.x * pivot.x,
                        Parent.Size.y * (1 - anchorMin.y - (anchorMax.y - anchorMin.y) * pivot.y)
                            - anchoredPosition.y
                            - Size.y * (1 - pivot.y)
                    );
        public Rect rect => new(-pivot.x * Size.x, -pivot.y * Size.y, Size.x, Size.y);
    }

    public struct Vector2(float x, float y)
    {
        public float x = x,
            y = y;
        public static Vector2 zero => new(0, 0);
        public static Vector2 one => new(1, 1);
        public Vector2 normalized
        {
            get
            {
                var n = MathF.Sqrt(x * x + y * y);
                return n == 0 ? zero : this / n;
            }
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);

        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);

        public static Vector2 operator *(Vector2 a, float b) => new(a.x * b, a.y * b);

        public static Vector2 operator /(Vector2 a, float b) => new(a.x / b, a.y / b);
    }

    public struct Color(float r, float g, float b, float a = 1)
    {
        public float r = r,
            g = g,
            b = b,
            a = a;
        public static Color white => new(1, 1, 1);
        public static Color black => new(0, 0, 0);
        public static Color clear => new(0, 0, 0, 0);

        public static Color operator *(Color c, float f) => new(c.r * f, c.g * f, c.b * f, c.a * f);

        public static Color operator *(Color c, Color d) =>
            new(c.r * d.r, c.g * d.g, c.b * d.b, c.a * d.a);

        public static Color Lerp(Color c, Color d, float t) =>
            new(
                c.r + (d.r - c.r) * t,
                c.g + (d.g - c.g) * t,
                c.b + (d.b - c.b) * t,
                c.a + (d.a - c.a) * t
            );

        public static bool operator ==(Color c, Color d) => c.Equals(d);

        public static bool operator !=(Color c, Color d) => !c.Equals(d);

        public override bool Equals(object o) =>
            o is Color d && r == d.r && g == d.g && b == d.b && a == d.a;

        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
    }

    public struct Rect(float x, float y, float width, float height)
    {
        public float x = x,
            y = y,
            width = width,
            height = height;
        public float xMin => x;
        public float yMin => y;
        public float xMax => x + width;
        public float yMax => y + height;
        public Vector2 center => new(x + width / 2, y + height / 2);
    }

    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);

        public static float Max(float a, float b) => Math.Max(a, b);

        public static float Clamp01(float a) => Math.Clamp(a, 0, 1);
    }

    public enum RenderMode
    {
        ScreenSpaceOverlay,
    }

    public class Canvas : Component
    {
        public RenderMode renderMode;
        public int sortingOrder;
    }
}

namespace UnityEngine.UI
{
    using UnityEngine;

    public class Graphic : Component
    {
        public Color color = Color.white;
        public bool raycastTarget;
        public RectTransform rectTransform => (RectTransform)transform;

        public void SetVerticesDirty() { }
    }

    public class MaskableGraphic : Graphic
    {
        protected virtual void OnPopulateMesh(VertexHelper vh) { }

        public VertexHelper Mesh()
        {
            var v = new VertexHelper();
            OnPopulateMesh(v);
            return v;
        }
    }

    public class Image : Graphic { }

    public class RectMask2D : Component { }

    public class GraphicRaycaster : Component { }

    public class CanvasScaler : Component
    {
        public enum ScaleMode
        {
            ScaleWithScreenSize,
        }

        public enum ScreenMatchMode
        {
            Expand,
        }

        public ScaleMode uiScaleMode;
        public ScreenMatchMode screenMatchMode;
        public Vector2 referenceResolution;
    }

    public struct Navigation
    {
        public enum Mode
        {
            None,
        }

        public Mode mode;
    }

    public struct ColorBlock
    {
        public Color normalColor,
            highlightedColor,
            pressedColor,
            selectedColor,
            disabledColor;
        public float fadeDuration;
    }

    public class Button : Component
    {
        public Graphic targetGraphic;
        public Navigation navigation;
        public ColorBlock colors;
        public bool interactable = true;
        public Click onClick = new();

        public class Click
        {
            public void AddListener(Action _) { }
        }
    }

    public class Slider : Component
    {
        public Navigation navigation;
        public bool wholeNumbers,
            interactable = true;
        public RectTransform fillRect,
            handleRect;
        public Graphic targetGraphic;
        public float minValue,
            maxValue;
        private float _value;
        public float value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, minValue, maxValue);
                var n = maxValue == minValue ? 0 : (_value - minValue) / (maxValue - minValue);
                if (fillRect != null)
                {
                    fillRect.sizeDelta = Vector2.zero;
                    fillRect.anchorMin = Vector2.zero;
                    fillRect.anchorMax = new(n, 1);
                }
                if (handleRect != null)
                {
                    handleRect.anchorMin = handleRect.anchorMax = new(n, 0);
                }
            }
        }
    }

    public class VertexHelper
    {
        public List<(Vector2 Point, Color Color)> Vertices = new();
        public List<int> Triangles = new();
        public int currentVertCount => Vertices.Count;

        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
        }

        public void AddVert(Vector2 p, Color c, Vector2 uv) => Vertices.Add((p, c));

        public void AddTriangle(int a, int b, int c) => Triangles.AddRange([a, b, c]);
    }
}

namespace TMPro
{
    using UnityEngine;

    public class TMP_FontAsset : Object
    {
        public Object atlasTexture = new();
    }

    public enum TextAlignmentOptions
    {
        MidlineLeft,
        MidlineRight,
        Center,
    }

    public enum TextOverflowModes
    {
        Ellipsis,
    }

    public class TMP_Text : UnityEngine.UI.Graphic
    {
        public TMP_FontAsset font;
        public string text;
        public float fontSize;
        public bool enableWordWrapping;
        public TextOverflowModes overflowMode;
        public TextAlignmentOptions alignment;
    }

    public class TextMeshProUGUI : TMP_Text { }
}

namespace SkillsExtended.Skills.Hacking
{
    public static class HackingView
    {
        public static T PdaAsset<T>(string name)
            where T : UnityEngine.Object
        {
            var go = new UnityEngine.GameObject(name);
            go.AddComponent<TMPro.TMP_Text>().font = new();
            return go as T;
        }
    }
}
