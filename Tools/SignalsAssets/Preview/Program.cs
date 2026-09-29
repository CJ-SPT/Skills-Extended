using System.Text.Json;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

var output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/signals-ui");
Directory.CreateDirectory(output);
foreach (var scenario in new[] { "receiver", "pairing", "unlocked", "practice" })
{
    var root = new GameObject("Preview");
    root.transform.sizeDelta = new Vector2(1920, 1080);
    var view = root.AddComponent<SignalsView>();
    view.Preview(scenario);
    var nodes = new List<object>();
    void Visit(RectTransform rt)
    {
        if (!rt.gameObject.active)
            return;
        var p = rt.Origin;
        var size = rt.Size;
        foreach (var component in rt.gameObject.Components)
        {
            object payload = null;
            if (component is MaskableGraphic graphic)
            {
                var mesh = graphic.Mesh();
                payload = new
                {
                    kind = "mesh",
                    vertices = mesh.Vertices.Select(v =>
                        new[]
                        {
                            v.Point.x + size.x * rt.pivot.x,
                            size.y * (1 - rt.pivot.y) - v.Point.y,
                            v.Color.r,
                            v.Color.g,
                            v.Color.b,
                            v.Color.a,
                        }
                    ),
                    indices = mesh.Triangles,
                };
            }
            else if (component is TMP_Text text)
                payload = new
                {
                    kind = "text",
                    value = text.text,
                    size = text.fontSize,
                    color = new[] { text.color.r, text.color.g, text.color.b, text.color.a },
                    align = text.alignment.ToString(),
                    wrap = text.enableWordWrapping,
                };
            else if (component is Image image)
                payload = new
                {
                    kind = "image",
                    color = new[] { image.color.r, image.color.g, image.color.b, image.color.a },
                };
            if (payload != null)
                nodes.Add(
                    new
                    {
                        name = rt.gameObject.name,
                        x = p.x,
                        y = p.y,
                        w = size.x,
                        h = size.y,
                        payload,
                    }
                );
        }
        foreach (var child in rt.Children)
            Visit(child);
    }
    Visit(root.transform);
    File.WriteAllText(Path.Combine(output, scenario + ".json"), JsonSerializer.Serialize(nodes));
}
Console.WriteLine("Production PDA layout and graphic meshes exported for four states.");

namespace SkillsExtended.Skills.Signals
{
    public sealed partial class SignalsView : MonoBehaviour
    {
        private TMP_FontAsset _font;
        private Slider _frequency,
            _bearing,
            _phase;
        private TMP_Text _status,
            _values,
            _heading;
        private SignalGraphic _plot,
            _spectrum;
        private bool _recording;
        private SignalPoint _position = new() { X = 200, Z = 150 };
        public SignalManifest Manifest { get; private set; }
        public SignalSnapshot State { get; private set; }
        public SignalPoint Position => _position;
        public bool InRaid { get; private set; } = true;
        public bool Pairing { get; private set; }
        public float Frequency => _frequency.value;
        public float Bearing => _bearing.value;
        public float Phase => _phase.value;
        public int Level => 25;
        public double Clock => 0;

        private void ToggleRecord() { }

        private void Close() { }

        private void Move(float x, float z) { }

        private void Command(string op) { }

        public void Preview(string scenario)
        {
            InRaid = scenario != "practice";
            Pairing = scenario == "pairing";
            Manifest = new()
            {
                Frequency = 96.45f,
                Seed = 42,
                Config = new(),
                Placement = new()
                {
                    Position = new() { X = 280, Z = 270 },
                },
            };
            State = new()
            {
                HasFix = true,
                AccessCode = "312974",
                Radius = 85,
                Estimate = new() { X = 270, Z = 250 },
                Unlocked = scenario == "unlocked",
                Readings = new()
                {
                    new()
                    {
                        Position = new() { X = 0, Z = 0 },
                        Bearing = 47,
                        Uncertainty = 8,
                    },
                    new()
                    {
                        Position = new() { X = 400, Z = 30 },
                        Bearing = 330,
                        Uncertainty = 8,
                    },
                },
            };
            BuildScreen();
            _frequency.value = 96.45f;
            _bearing.value = 47;
            _phase.value = 40;
            RenderScreen(.78f);
        }
    }
}
