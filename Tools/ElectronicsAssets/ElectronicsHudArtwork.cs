// Reproducible vector-style gauge artwork. Original low-resolution EVE inputs remain untouched.
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ElectronicsHudArtwork
{
    private static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        var d = b - a;
        return Vector2.Distance(p, a + d * Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude));
    }

    private static float Path(Vector2 p, params float[] points)
    {
        var d = float.MaxValue;
        for (var i = 0; i < points.Length - 2; i += 2)
        {
            d = Mathf.Min(
                d,
                Segment(
                    p,
                    new Vector2(points[i], points[i + 1]),
                    new Vector2(points[i + 2], points[i + 3])
                )
            );
        }

        return d;
    }

    private static float Coverage(float d, float width) =>
        Mathf.Clamp01((width * .5f - d) * 4 + .5f);

    private static float Triangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
        var x = Cross(b - a, p - a);
        var y = Cross(c - b, p - b);
        var z = Cross(a - c, p - c);
        var inside = x >= 0 && y >= 0 && z >= 0 || x <= 0 && y <= 0 && z <= 0;
        var distance = Mathf.Min(Segment(p, a, b), Mathf.Min(Segment(p, b, c), Segment(p, c, a)));
        return Mathf.Clamp01(.5f + (inside ? distance : -distance) * 4);
    }

    public static void Build(string directory)
    {
        Write(
            directory,
            "sharp-hud-icon",
            88,
            66,
            p =>
            {
                // TerraGroup's divided diamond: three solid quadrants and one outlined.
                // Vector geometry keeps the small emblem sharp at every supported UI scale.
                var topLeft = Triangle(
                    p,
                    new Vector2(14, 32),
                    new Vector2(43, 3),
                    new Vector2(43, 32)
                );
                var topRight = Triangle(
                    p,
                    new Vector2(45, 3),
                    new Vector2(74, 32),
                    new Vector2(45, 32)
                );
                var bottomLeft = Triangle(
                    p,
                    new Vector2(14, 34),
                    new Vector2(43, 63),
                    new Vector2(43, 34)
                );
                var bottomRight = Triangle(
                    p,
                    new Vector2(45, 34),
                    new Vector2(74, 34),
                    new Vector2(45, 63)
                );
                var cutout = Triangle(
                    p,
                    new Vector2(49, 38),
                    new Vector2(64.3f, 38),
                    new Vector2(49, 53.3f)
                );
                var alpha = Mathf.Max(
                    Mathf.Max(topLeft, topRight),
                    Mathf.Max(bottomLeft, bottomRight * (1 - cutout))
                );
                return new Color(.92f, .97f, .97f, alpha);
            }
        );
        Write(
            directory,
            "sharp-hud-background",
            184,
            142,
            p =>
            {
                var center = new Vector2(92, 71);
                var delta = p - center;
                var ellipse = Mathf.Sqrt(
                    delta.x * delta.x / (88 * 88) + delta.y * delta.y / (90 * 90)
                );
                var interior = Mathf.Min(
                    Mathf.Clamp01((1 - ellipse) * 80),
                    Mathf.Clamp01((57 - Mathf.Abs(delta.y)) * 4 + .5f)
                );
                var color =
                    p.y < 71
                        ? Color.Lerp(
                            new Color(.055f, .019f, .008f),
                            new Color(.28f, .13f, .072f),
                            Mathf.InverseLerp(14, 71, p.y)
                        )
                        : Color.Lerp(
                            new Color(.22f, .009f, .002f),
                            new Color(.07f, .002f, 0),
                            Mathf.InverseLerp(71, 128, p.y)
                        );
                color.a = interior;
                if (Mathf.Abs(p.y - 71) < .5f && interior > 0)
                {
                    color = new Color(.32f, .25f, .22f, interior);
                }

                // Follow the same curve as the shader bars, with 4.5 units of clearance.
                var curveY = Mathf.Clamp(delta.y, -60, 60);
                var root = Mathf.Sqrt(1 - curveY * curveY / (67 * 67));
                var frameX = 29.5f - 22 * root;
                var slope = 22 * curveY / (67 * 67 * root);
                var curveDistance =
                    Mathf.Min(Mathf.Abs(p.x - frameX), Mathf.Abs(p.x - (184 - frameX)))
                    / Mathf.Sqrt(1 + slope * slope);
                curveDistance = Mathf.Max(curveDistance, Mathf.Abs(delta.y) - 60);
                var rails = Mathf.Min(
                    curveDistance,
                    Mathf.Min(
                        Path(p, 19.7f, 11, 25, 9, 71, 9),
                        Path(p, 113, 133, 159, 133, 164.3f, 131)
                    )
                );
                float railAlpha = Coverage(rails, 1.2f);
                color = Color.Lerp(color, new Color(.38f, .46f, .48f, 1), railAlpha);
                return color;
            }
        );
    }

    private static void Write(
        string directory,
        string name,
        int width,
        int height,
        Func<Vector2, Color> pixel
    )
    {
        const int scale = 4;
        var texture = new Texture2D(width * scale, height * scale, TextureFormat.RGBA32, false);
        var pixels = new Color[texture.width * texture.height];
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                pixels[y * texture.width + x] = pixel(
                    new Vector2((x + .5f) / scale, height - (y + .5f) / scale)
                );
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        var path = directory + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }
}
