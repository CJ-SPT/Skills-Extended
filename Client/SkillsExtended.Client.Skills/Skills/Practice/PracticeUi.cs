using System;
using System.Linq;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SkillsExtended.Skills.Practice;

internal static class PracticeUi
{
    internal static readonly Color Ink = new(.77f, .76f, .68f);
    internal static readonly Color Muted = new(.55f, .55f, .51f);
    internal static readonly Color Surface = new(0, 0, 0, .98f);
    private static MessageWindow _nativeWindow;
    private static Image _nativeCheckbox;

    // Read graphics from the game's existing dialog; never clone its behaviour,
    // localization subscriptions or confirmation callbacks into a practice window.
    private static Transform NativeWindow
    {
        get
        {
            if (!_nativeWindow)
                _nativeWindow = Resources.FindObjectsOfTypeAll<MessageWindow>()
                    .FirstOrDefault(w => w.name == "ConfirmationWindow")
                    ?? Resources.FindObjectsOfTypeAll<MessageWindow>()
                        .FirstOrDefault(w => w.transform.Find("Window/Background"));
            return _nativeWindow ? _nativeWindow.transform.Find("Window") : null;
        }
    }

    internal static TMP_FontAsset DialogFont(TMP_FontAsset fallback)
    {
        var caption = NativeWindow ? NativeWindow.Find("Caption Panel/Caption") : null;
        var label = caption ? caption.GetComponent<TMP_Text>() : null;
        return label && label.font ? label.font : fallback;
    }

    private static Image NativeImage(string path)
    {
        var item = NativeWindow ? NativeWindow.Find(path) : null;
        return item ? item.GetComponent<Image>() : null;
    }

    internal static Image Skin(RectTransform rect, string path, Color fallback, bool raycast = false)
    {
        var source = NativeImage(path);
        var image = Box(rect, source ? source.color : fallback);
        CopyGraphic(image, source);
        image.raycastTarget = raycast;
        return image;
    }

    private static void CopyGraphic(Image image, Image source)
    {
        if (source)
        {
            image.color = source.color;
            image.sprite = source.sprite;
            image.type = source.type;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            image.fillCenter = source.fillCenter;
        }
    }

    internal static void Frame(RectTransform panel)
    {
        Skin(panel, "Background", Surface, true);
        var border = Rect("Window border", panel, Vector2.zero, Vector2.zero);
        Stretch(border);
        border.sizeDelta = new Vector2(10, 10);
        var image = Skin(border, "Border", new Color(.35f, .35f, .32f));
        image.fillCenter = false;
        if (!image.sprite)
        {
            // A missing template still produces an outlined window, never an opaque
            // fallback rectangle covering its contents.
            image.color = Color.clear;
            Outline(panel);
        }
    }

    internal static void Outline(RectTransform parent)
    {
        var color = new Color(.36f, .35f, .3f, .75f);
        foreach (var top in new[] { false, true })
        {
            var edge = Rect("Horizontal border", parent, Vector2.zero, Vector2.zero);
            edge.anchorMin = new Vector2(0, top ? 1 : 0);
            edge.anchorMax = new Vector2(1, top ? 1 : 0);
            edge.sizeDelta = new Vector2(0, 1);
            Box(edge, color).raycastTarget = false;
        }
        foreach (var right in new[] { false, true })
        {
            var edge = Rect("Vertical border", parent, Vector2.zero, Vector2.zero);
            edge.anchorMin = new Vector2(right ? 1 : 0, 0);
            edge.anchorMax = new Vector2(right ? 1 : 0, 1);
            edge.sizeDelta = new Vector2(1, 0);
            Box(edge, color).raycastTarget = false;
        }
    }

    internal static void Rule(Transform parent, float y) =>
        Box(Rect("Divider", parent, new Vector2(512, 1), new Vector2(0, y)),
            new Color(.29f, .29f, .26f, .6f)).raycastTarget = false;

    internal static void CloseButton(Transform parent, TMP_FontAsset font, Vector2 position, Action close)
    {
        var button = Button(parent, "", font, new Vector2(26, 24), position, close);
        var icon = Rect("Close icon", button.transform, new Vector2(9, 11), Vector2.zero);
        var image = Skin(icon, "Caption Panel/Close Button/X", Ink);
        if (!image.sprite)
        {
            image.enabled = false;
            Label(button.transform, "×", font, new Vector2(24, 24), Vector2.zero, 20);
        }
    }

    internal static Toggle Checkbox(Transform parent, string text, TMP_FontAsset font,
        Vector2 position, Action<bool> change)
    {
        var rect = Rect("Customize skill level", parent, new Vector2(512, 32), position);
        Box(rect, Color.clear);
        var box = Rect("Checkbox", rect, new Vector2(20, 20), new Vector2(-246, 0));
        var background = Box(box, new Color(.09f, .09f, .08f));
        var mark = Rect("Check", box, new Vector2(12, 12), Vector2.zero);
        var graphic = Box(mark, Ink);
        if (!_nativeCheckbox)
            _nativeCheckbox = Resources.FindObjectsOfTypeAll<Image>()
                .FirstOrDefault(i => i.name == "Checkbox" && i.transform.Find("Mark"));
        if (_nativeCheckbox)
        {
            CopyGraphic(background, _nativeCheckbox);
            CopyGraphic(graphic, _nativeCheckbox.transform.Find("Mark").GetComponent<Image>());
        }
        if (!background.sprite) Outline(box);
        graphic.raycastTarget = false;
        var label = Label(rect, text, font, new Vector2(478, 30), new Vector2(17, 0), 17);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        var toggle = rect.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = graphic;
        toggle.navigation = new Navigation { mode = Navigation.Mode.None };
        // Initialize the newly assigned graphic even if Toggle already defaults off.
        toggle.SetIsOnWithoutNotify(true);
        toggle.SetIsOnWithoutNotify(false);
        toggle.onValueChanged.AddListener(value => change(value));
        return toggle;
    }

    internal static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    internal static Image Box(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    internal static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    internal static TMP_Text Label(Transform parent, string text, TMP_FontAsset font,
        Vector2 size, Vector2 position, float fontSize = 20)
    {
        var rect = Rect(text, parent, size, position);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = Ink;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.enableWordWrapping = true;
        return label;
    }

    internal static Button Button(Transform parent, string text, TMP_FontAsset font,
        Vector2 size, Vector2 position, Action click, float fontSize = 18)
    {
        var rect = Rect(text, parent, size, position);
        var image = Skin(rect, "ButtonsPanel/YesButton/Background", Color.white, true);
        image.color = Color.white;
        Outline(rect);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = button.colors;
        colors.normalColor = new Color(.15f, .15f, .13f, .85f);
        colors.highlightedColor = new Color(.39f, .38f, .31f);
        colors.pressedColor = new Color(.27f, .26f, .22f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.09f, .09f, .08f, .65f);
        colors.fadeDuration = .08f;
        button.colors = colors;
        var label = Label(rect, text, font, size, Vector2.zero, fontSize);
        Stretch(label.rectTransform);
        button.onClick.AddListener(() => click());
        return button;
    }
}
