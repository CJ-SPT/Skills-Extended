using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Hacking.UI
{
    // Shared by the runtime renderer and the offline Unity layout validator. No game dependencies.
    public static class ElectronicsUiVisuals
    {
        public static readonly Color Amber = new Color(1f, .43f, .12f);
        public static readonly Color Cyan = new Color(.48f, .94f, .91f);
        public static readonly Color Dormant = new Color(.24f, .28f, .29f);

        // Pixel intersections in EVE's boardbg.png are 86 px apart horizontally and 53 px per staggered row.
        public const float RowSpacing = 53f / 86f;

        public static Material[] CreateGaugeMaterials(Transform panel)
        {
            var materials = new Material[2];
            for (var i = 0; i < 2; i++)
            {
                var image = panel
                    .Find(i == 0 ? "IntegrityRing" : "IntegrityRight")
                    .GetComponent<Image>();
                materials[i] = new Material(image.material);
                image.material = materials[i];
            }

            return materials;
        }

        public static void SetCoherence(Material[] materials, float fraction)
        {
            foreach (var material in materials)
            {
                material.SetFloat("_Fill", Mathf.Clamp01(fraction));
            }
        }

        public static void SmoothConnection(Image image, Material material)
        {
            image.material = material;
            var size = image.rectTransform.sizeDelta;
            image.rectTransform.sizeDelta = new Vector2(size.x, 4.5f);
        }

        public static void AlignBackground(
            RectTransform image,
            float scale,
            float centerX,
            float centerY,
            int anchorQ,
            int anchorR
        )
        {
            image.sizeDelta = new Vector2(896, 629) * (scale / 86f);
            // Texture lattice origin (469,268), measured from its top-left. Image pivots are centered.
            image.anchoredPosition =
                Position(anchorQ, anchorR, centerX, centerY, scale)
                + new Vector2(448 - 469, 268 - 314.5f) * (scale / 86f);
        }

        public static readonly string[] Names =
        {
            "",
            "System Core",
            "Firewall",
            "Antivirus",
            "Restoration",
            "Suppressor",
            "Self Repair",
            "Kernel Rot",
            "Shield",
            "Vector",
            "Data Cache",
        };
        public static readonly string[] Icons =
        {
            "",
            "cpu",
            "shield",
            "bug",
            "wrench",
            "radio",
            "heart-pulse",
            "scissors",
            "shield-check",
            "zap",
            "database",
        };

        public static void DrawNode(
            GameObject node,
            int kind,
            bool revealed,
            bool cleared,
            bool actionable,
            int hp,
            int strength,
            int clue,
            Func<string, Sprite> sprite
        )
        {
            var ring = node.transform.Find("Ring").GetComponent<Image>();
            var icon = node.transform.Find("Icon").GetComponent<Image>();
            var label = node.transform.Find("Label").GetComponent<TMP_Text>();
            var health = node.transform.Find("Health").GetComponent<TMP_Text>();
            var center = node.transform.Find("Center").GetComponent<TMP_Text>();
            var power = node.transform.Find("Power").GetComponent<TMP_Text>();
            var occupied = revealed && !cleared && kind != 0;
            var defense = occupied && kind <= 5;
            ring.sprite = sprite(
                defense ? "defense-ring"
                : !revealed ? (actionable ? "actionable-node" : "hidden-node")
                : "node-ring"
            );
            ring.rectTransform.sizeDelta =
                Vector2.one
                * (
                    defense ? 72
                    : !revealed ? 54
                    : 42
                );
            ring.color =
                cleared || occupied && kind >= 6 ? Amber
                : actionable || occupied ? Cyan
                : Color.white;
            label.text = occupied ? Names[kind] : "";
            label.color = ring.color;
            health.text = defense ? hp.ToString() : "";
            health.color = Cyan;
            power.text = defense && strength > 0 ? strength.ToString() : "";
            power.color = Cyan;
            center.text =
                revealed && cleared && kind == 0
                    ? (clue == 0 ? "IN" : clue + (clue == 5 ? "+" : ""))
                    : "";
            center.color = cleared ? new Color(1, .65f, .36f) : Cyan;
            icon.enabled = occupied;
            if (occupied)
            {
                icon.sprite = sprite(Icons[kind]);
                icon.color =
                    kind == 1 || kind == 10 ? Color.white
                    : kind >= 6 ? Amber
                    : Cyan;
            }

            node.GetComponent<Button>().interactable = actionable;
        }

        public static Vector2 Position(
            float q,
            float r,
            float centerX,
            float centerY,
            float scale
        ) => new Vector2((q + r * .5f - centerX) * scale, -(r * RowSpacing - centerY) * scale);

        public static Texture2D RingTexture(bool defense, bool dot)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    float dx = x - 63.5f,
                        dy = y - 63.5f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float sector = Mathf.Repeat(angle + Mathf.PI / 6, Mathf.PI / 3) - Mathf.PI / 6;
                    float distance =
                        dot ? Mathf.Max(0, radius - 24)
                        : defense ? Mathf.Abs(radius * Mathf.Cos(sector) - 45)
                        : Mathf.Abs(radius - 40);
                    float core = Mathf.Clamp01(2.2f - distance);
                    float glow = Mathf.Exp(-distance * .22f) * .24f;
                    if (defense && Mathf.Abs(sector) > .43f)
                    {
                        core = 0;
                        glow *= .2f;
                    }

                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(core + glow));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
