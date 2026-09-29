using TMPro;
using UnityEngine;

namespace SkillsExtended.Skills.Signals;

public sealed partial class SignalsView
{
    private RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private TMP_Text Text(
        Transform parent,
        string text,
        float x,
        float y,
        float w,
        float h,
        int size = 20
    )
    {
        var t = Rect("Label", parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        t.font = _font;
        t.text = text;
        t.fontSize = size;
        t.color = Ink;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }
}
