using System;

namespace SkillsExtended.DeveloperTools;

public static class DeveloperEditorLayout
{
    public const float BrowserWidth = 286, InspectorWidth = 326, ToolTop = 82, StatusHeight = 56, PanelBottom = 60;
    public static float Scale(int width, int height) =>
        Math.Min(Math.Max(1, width) / 1920f, Math.Max(1, height) / 1080f);
    public static (float X, float Y, float Width, float Height) Popup(float x, float top, float bottom,
        float width, float height, float viewportWidth, float viewportHeight)
    {
        var w = Math.Min(Math.Max(160, width), Math.Max(1, viewportWidth - 8));
        var h = Math.Min(height, Math.Max(1, viewportHeight - 8));
        var px = Math.Max(4, Math.Min(x, viewportWidth - w - 4));
        var py = bottom + h + 4 <= viewportHeight ? bottom : Math.Max(4, top - h);
        return (px, Math.Min(py, viewportHeight - h - 4), w, h);
    }
}
