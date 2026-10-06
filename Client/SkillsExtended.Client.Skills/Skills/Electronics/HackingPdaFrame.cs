using System;
using SkillsExtended.Skills.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.Hacking;

// Uses the receiver's resolution-independent housing surfaces around the existing
// hacking prefab. The gameplay panel stays at its original size and hierarchy.
internal static class HackingPdaFrame
{
    public static readonly Vector2 ReferenceResolution = new(1920, 1200);

    public static RectTransform Build(
        Transform parent,
        Vector2 screenSize,
        TMP_FontAsset font,
        Action exit
    )
    {
        var width = screenSize.x + 244;
        var height = screenSize.y + 184;
        var shell = Surface(
            parent,
            "Hacking PDA chassis",
            0,
            0,
            width,
            height,
            new Color(.19f, .21f, .19f),
            new Color(.07f, .08f, .075f),
            42,
            7
        );
        var body = shell.rectTransform;
        body.anchorMin = body.anchorMax = body.pivot = new Vector2(.5f, .5f);
        body.anchoredPosition = Vector2.zero;
        Surface(
            body,
            "Inner housing",
            18,
            18,
            width - 36,
            height - 36,
            new Color(.095f, .11f, .10f),
            new Color(.035f, .045f, .042f),
            30,
            3
        );
        foreach (var x in new[] { 28f, width - 90 })
        {
            Surface(
                body,
                "Rubber grip",
                x,
                108,
                62,
                height - 250,
                new Color(.036f, .042f, .038f),
                new Color(.016f, .02f, .018f),
                18,
                3
            );
            for (var y = 143f; y < height - 150; y += 28)
                Surface(
                    body,
                    "Grip rib",
                    x + 9,
                    y,
                    44,
                    9,
                    new Color(.11f, .13f, .12f),
                    new Color(.025f, .03f, .026f),
                    3
                );
        }
        foreach (var x in new[] { 56f, width - 76 })
        foreach (var y in new[] { 43f, height - 73 })
        {
            Surface(
                body,
                "Steel fastener",
                x,
                y,
                20,
                20,
                new Color(.39f, .42f, .38f),
                new Color(.09f, .11f, .10f),
                8,
                2
            );
            Bar(body, "Screw slot", x + 5, y + 9, 10, 2, Color.black);
        }
        Label(body, LocalizedText.Get("SkillsExtended.HackingPdaFrame.TerragroupFieldSystems"), 128, 32, 640, 28, 17, font);
        Label(body, LocalizedText.Get("SkillsExtended.HackingPdaFrame.ModifiedPdaHk200"), width - 536, 32, 416, 28, 15, font);
        Bar(body, "Power indicator", width - 150, 43, 12, 5, new Color(.36f, .88f, .74f));
        Surface(
            body,
            "Screen gasket",
            108,
            74,
            screenSize.x + 28,
            screenSize.y + 28,
            Color.black,
            new Color(.24f, .27f, .24f),
            13,
            5
        );
        Label(body, LocalizedText.Get("SkillsExtended.HackingPdaFrame.HackingInterface"), 130, height - 69, 500, 28, 19, font);
        Label(body, LocalizedText.Get("SkillsExtended.HackingPdaFrame.ReusableFieldTerminal"), width - 490, height - 69, 364, 28, 13, font);
        for (var i = 0; i < 7; i++)
            Bar(body, "Speaker grille", width / 2 - 66 + i * 13, height - 63, 5, 23, Color.black);

        var face = Surface(
            body,
            "PDA exit",
            width - 90,
            height / 2 - 56,
            62,
            82,
            new Color(.16f, .19f, .17f),
            new Color(.05f, .07f, .06f),
            8,
            2
        );
        face.raycastTarget = true;
        var button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() => exit());
        Label(face.transform, LocalizedText.Get("SkillsExtended.HackingPdaFrame.ExitEsc"), 0, 0, 62, 82, 13, font).alignment =
            TextAlignmentOptions.Center;
        return body;
    }

    // The screen aperture is 122 px from the left and 88 px from the top.
    public static Vector2 ScreenPosition => new(0, 4);

    private static RectTransform Rect(
        Transform parent,
        string name,
        float x,
        float y,
        float w,
        float h
    )
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    private static PdaSurface Surface(
        Transform parent,
        string name,
        float x,
        float y,
        float w,
        float h,
        Color top,
        Color bottom,
        float corner,
        float bevel = 1
    )
    {
        var face = Rect(parent, name, x, y, w, h).gameObject.AddComponent<PdaSurface>();
        face.Top = top;
        face.Bottom = bottom;
        face.Edge = new Color(.25f, .31f, .31f);
        face.Corner = corner;
        face.Bevel = bevel;
        face.raycastTarget = false;
        return face;
    }

    private static void Bar(
        Transform parent,
        string name,
        float x,
        float y,
        float w,
        float h,
        Color color
    )
    {
        var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private static TMP_Text Label(
        Transform parent,
        string text,
        float x,
        float y,
        float w,
        float h,
        float size,
        TMP_FontAsset font
    )
    {
        var label = Rect(parent, text, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = new Color(.82f, .88f, .86f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        return label;
    }
}
