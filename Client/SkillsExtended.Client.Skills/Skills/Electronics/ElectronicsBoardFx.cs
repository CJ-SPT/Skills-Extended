using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Hacking.UI
{
    /// <summary>Cosmetic only. Uses unscaled time; it never calls the model or blocks input.
    /// The same renderer is sampled by the offline Unity animation preview.</summary>
    public sealed class ElectronicsBoardFx : MonoBehaviour
    {
        public struct Connection
        {
            public int A;
            public int B;
            public Image Image;

            public Connection(int a, int b, Image image)
            {
                A = a;
                B = b;
                Image = image;
            }
        }

        private sealed class Node
        {
            public RectTransform Root;
            public RectTransform Ring;
            public RectTransform Icon;
            public Image Halo;
            public bool Available;
            public bool Hover;
            public float Reveal = -100;
            public float Hit = -100;
        }

        private sealed class Burst
        {
            public Image Image;
            public float Start;
            public float Duration;
            public float Size;
            public float Expansion;
            public float Spin;
            public Color Color;
        }

        private sealed class PathPulse
        {
            public Image Edge;
            public Image Beam;
            public Image Head;
            public Image Trace;
            public Vector2 StartPoint;
            public Vector2 EndPoint;
            public float Start;
            public float Length;
            public float Angle;
            public Color Color;
            public Color Resting;
        }

        private readonly List<Burst> _bursts = new List<Burst>();
        private readonly List<PathPulse> _paths = new List<PathPulse>();
        private readonly Queue<Image> _pool = new Queue<Image>();
        private readonly Dictionary<long, Image> _edges = new Dictionary<long, Image>();
        private readonly Dictionary<long, Color> _colors = new Dictionary<long, Color>();
        private RectTransform _layer;
        private Node[] _nodes;
        private Func<string, Sprite> _sprite;
        private int _imageCount;
        private float _clock;
        public int ActiveEffectCount => _bursts.Count + _paths.Count;

        private static long Key(int a, int b) =>
            ((long)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);

        public void Initialize(
            RectTransform board,
            GameObject[] nodes,
            Connection[] edges,
            Func<string, Sprite> sprite
        )
        {
            _sprite = sprite;
            _layer = new GameObject(
                "Network effects",
                typeof(RectTransform)
            ).GetComponent<RectTransform>();
            _layer.SetParent(board, false);
            _nodes = new Node[nodes.Length];
            for (var i = 0; i < nodes.Length; i++)
            {
                var root = (RectTransform)nodes[i].transform;
                _nodes[i] = new Node
                {
                    Root = root,
                    Ring = (RectTransform)root.Find("Ring"),
                    Icon = (RectTransform)root.Find("Icon"),
                    Halo = NewImage("Node focus", sprite("fx-ring")),
                };
                _nodes[i].Halo.rectTransform.anchoredPosition = root.anchoredPosition;
                _nodes[i].Halo.color = Color.clear;
            }

            foreach (var edge in edges)
            {
                _edges[Key(edge.A, edge.B)] = edge.Image;
                _colors[Key(edge.A, edge.B)] = edge.Image.color;
            }

            _clock = Time.unscaledTime;
        }

        private Image NewImage(string name, Sprite sprite)
        {
            var image = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image)
            ).GetComponent<Image>();
            image.transform.SetParent(_layer, false);
            image.sprite = sprite;
            image.raycastTarget = false;
            image.canvasRenderer.cullTransparentMesh = true;
            return image;
        }

        private Image Rent(string sprite)
        {
            if (_pool.Count == 0 && _imageCount >= 192)
            {
                return null;
            }

            Image image;
            if (_pool.Count > 0)
            {
                image = _pool.Dequeue();
            }
            else
            {
                image = NewImage("Transient network effect", null);
                _imageCount++;
            }

            image.gameObject.SetActive(true);
            image.sprite = string.IsNullOrEmpty(sprite) ? null : _sprite(sprite);
            image.color = Color.clear;
            image.type = Image.Type.Simple;
            image.material = null;
            image.rectTransform.pivot = Vector2.one * .5f;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.localRotation = Quaternion.identity;
            return image;
        }

        private void Return(Image image)
        {
            if (image)
            {
                image.gameObject.SetActive(false);
                _pool.Enqueue(image);
            }
        }

        public void SetAvailable(int node, bool available)
        {
            _nodes[node].Available = available;
        }

        public void SetConnectionColor(int a, int b, Color color)
        {
            var key = Key(a, b);
            _colors[key] = color;
            if (!_edges.TryGetValue(key, out var image))
            {
                return;
            }

            image.color = color;
            foreach (var path in _paths)
            {
                if (path.Edge == image)
                {
                    path.Resting = color;
                }
            }
        }

        public void Hover(int node, bool active)
        {
            _nodes[node].Hover = active;
        }

        public void Click(int node) =>
            Ring(node, "fx-ring", ElectronicsUiVisuals.Cyan, 30, 36, .22f, 0);

        public void Reveal(int node)
        {
            _nodes[node].Reveal = _clock;
            Ring(node, "fx-infected", Color.white, 36, 60, .5f, 160);
            Ring(node, "fx-glow", ElectronicsUiVisuals.Amber, 26, 34, .25f, 0);
        }

        public void DestroyNode(int node, Sprite oldIcon)
        {
            Ring(node, "fx-ring", ElectronicsUiVisuals.Amber, 38, 84, .6f, 0);
            if (oldIcon)
            {
                var image = Rent(null);
                if (image)
                {
                    image.sprite = oldIcon;
                    image.rectTransform.anchoredPosition = _nodes[node].Root.anchoredPosition;
                    _bursts.Add(
                        new Burst
                        {
                            Image = image,
                            Start = _clock,
                            Duration = .32f,
                            Size = 64,
                            Expansion = 28,
                            Color = ElectronicsUiVisuals.Cyan,
                            Spin = 35,
                        }
                    );
                }
            }
        }

        public void Damage(int node)
        {
            _nodes[node].Hit = _clock;
            Ring(node, "fx-glow", new Color(1, .3f, .12f), 30, 28, .28f, 0);
        }

        public void Repair(int node) =>
            Ring(node, "fx-ring", ElectronicsUiVisuals.Cyan, 80, -40, .5f, -80);

        public void Available(int node) =>
            Ring(node, "fx-ring", ElectronicsUiVisuals.Cyan, 24, 48, .4f, 0, .15f);

        public void Blocked(int node) =>
            Ring(node, "fx-ring", new Color(.8f, .28f, .12f), 60, -30, .3f, 0);

        private void Ring(
            int node,
            string sprite,
            Color color,
            float size,
            float expansion,
            float duration,
            float spin,
            float delay = 0
        )
        {
            var image = Rent(sprite);
            if (!image)
            {
                return;
            }

            image.rectTransform.anchoredPosition = _nodes[node].Root.anchoredPosition;
            _bursts.Add(
                new Burst
                {
                    Image = image,
                    Start = _clock + delay,
                    Duration = duration,
                    Size = size,
                    Expansion = expansion,
                    Color = color,
                    Spin = spin,
                }
            );
        }

        public void OpenPath(int from, int to, bool claimed)
        {
            if (!_edges.TryGetValue(Key(from, to), out var edge))
            {
                return;
            }

            // Replace an older pulse on this connection during rapid consecutive actions.
            for (var i = _paths.Count - 1; i >= 0; i--)
            {
                if (_paths[i].Edge == edge)
                {
                    Release(_paths[i]);
                    _paths.RemoveAt(i);
                }
            }

            var beam = Rent(null);
            var head = Rent("fx-glow");
            var trace = Rent("fx-trace-" + (1 + (from + to) % 4));
            if (!beam || !head || !trace)
            {
                Return(beam);
                Return(head);
                Return(trace);
                return;
            }

            beam.material = edge.material;
            var a = _nodes[from].Root.anchoredPosition;
            var b = _nodes[to].Root.anchoredPosition;
            var direction = (b - a).normalized;
            a += direction * 21;
            b -= direction * 21;
            var pulse = new PathPulse
            {
                Edge = edge,
                Beam = beam,
                Head = head,
                Trace = trace,
                StartPoint = a,
                EndPoint = b,
                Start = _clock + (claimed ? 0 : .1f),
                Length = Vector2.Distance(a, b),
                Angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
                Color = claimed ? ElectronicsUiVisuals.Amber : ElectronicsUiVisuals.Cyan,
                Resting = _colors[Key(from, to)],
            };
            beam.rectTransform.pivot = new Vector2(0, .5f);
            beam.rectTransform.anchoredPosition = a;
            beam.rectTransform.localRotation = Quaternion.Euler(0, 0, pulse.Angle);
            trace.rectTransform.anchoredPosition = (a + b) * .5f;
            trace.rectTransform.sizeDelta = new Vector2(pulse.Length + 42, 42);
            trace.rectTransform.localRotation = Quaternion.Euler(0, 0, pulse.Angle);
            _paths.Add(pulse);
        }

        public void Finish(bool won, int core)
        {
            var color = won ? ElectronicsUiVisuals.Amber : new Color(1, .18f, .1f);
            for (var i = 0; i < _nodes.Length; i++)
            {
                var distance = Vector2.Distance(
                    _nodes[core].Root.anchoredPosition,
                    _nodes[i].Root.anchoredPosition
                );
                Ring(i, "fx-ring", color, 24, 68, .42f, 0, distance / 2200);
            }
        }

        private void Update() => Sample(Time.unscaledTime);

        public void Sample(float now)
        {
            _clock = now;
            if (_nodes == null)
            {
                return;
            }

            for (var i = 0; i < _nodes.Length; i++)
            {
                var node = _nodes[i];
                var reveal = Mathf.Clamp01((now - node.Reveal) / .2f);
                var ease = 1 - Mathf.Pow(1 - reveal, 3);
                var flip = new Vector3(
                    Mathf.Lerp(.12f, 1, ease),
                    1 + .12f * Mathf.Sin(reveal * Mathf.PI),
                    1
                );
                node.Ring.localScale = node.Icon.localScale = flip;
                var hit = Mathf.Clamp01((now - node.Hit) / .25f);
                node.Icon.localRotation = Quaternion.Euler(
                    0,
                    0,
                    Mathf.Sin(hit * Mathf.PI * 6) * (1 - hit) * 12
                );
                var focus = node.Available
                    ? (node.Hover ? .5f : .08f + .035f * Mathf.Sin(now * 3 + i))
                    : 0;
                node.Halo.color = Alpha(ElectronicsUiVisuals.Cyan, focus);
                node.Halo.rectTransform.sizeDelta =
                    Vector2.one * (node.Hover ? 76 : 62 + 3 * Mathf.Sin(now * 3 + i));
            }

            for (var i = _bursts.Count - 1; i >= 0; i--)
            {
                var effect = _bursts[i];
                var t = (now - effect.Start) / effect.Duration;
                if (t >= 1)
                {
                    Return(effect.Image);
                    _bursts.RemoveAt(i);
                    continue;
                }

                if (t < 0)
                {
                    effect.Image.color = Color.clear;
                    continue;
                }

                var ease = 1 - (1 - t) * (1 - t);
                effect.Image.rectTransform.sizeDelta =
                    Vector2.one * (effect.Size + effect.Expansion * ease);
                effect.Image.rectTransform.localRotation = Quaternion.Euler(
                    0,
                    0,
                    effect.Spin * ease
                );
                effect.Image.color = Alpha(
                    effect.Color,
                    Mathf.Min(1, t / .08f) * Mathf.Pow(1 - t, 1.5f)
                );
            }

            for (var i = _paths.Count - 1; i >= 0; i--)
            {
                var path = _paths[i];
                var age = now - path.Start;
                if (age >= .65f)
                {
                    path.Edge.color = path.Resting;
                    Release(path);
                    _paths.RemoveAt(i);
                    continue;
                }

                var travel = Mathf.Clamp01(age / .3f);
                var fade = age < .3f ? 1 : Mathf.Clamp01((.65f - age) / .35f);
                path.Edge.color = Color.Lerp(new Color(.1f, .18f, .18f), path.Resting, travel);
                path.Beam.rectTransform.sizeDelta = new Vector2(path.Length * travel, 7.5f);
                path.Beam.color = Alpha(path.Color, age >= 0 ? fade * .9f : 0);
                path.Head.rectTransform.anchoredPosition = Vector2.Lerp(
                    path.StartPoint,
                    path.EndPoint,
                    travel
                );
                path.Head.rectTransform.sizeDelta = Vector2.one * 30;
                path.Head.color = Alpha(path.Color, age >= 0 ? fade : 0);
                path.Trace.color = Alpha(
                    path.Color,
                    age >= 0 ? Mathf.Sin(travel * Mathf.PI) * .6f : 0
                );
            }
        }

        private static Color Alpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void Release(PathPulse path)
        {
            Return(path.Beam);
            Return(path.Head);
            Return(path.Trace);
        }
    }
}
