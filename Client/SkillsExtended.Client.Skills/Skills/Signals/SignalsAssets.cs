using System.IO;
using System.Reflection;
using UnityEngine;

namespace SkillsExtended.Skills.Signals;

public static class SignalsAssets
{
    private static Sprite _icon;

    public static Sprite Icon()
    {
        if (_icon)
            return _icon;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var path = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "Images",
            "SignalsSkillIcon.png"
        );
        if (!File.Exists(path) || !texture.LoadImage(File.ReadAllBytes(path), true))
        {
            Object.Destroy(texture);
            return null;
        }
        texture.filterMode = FilterMode.Bilinear;
        return _icon = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f)
        );
    }
}
