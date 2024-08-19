using System.Linq;
using System.Reflection;
using EFT.UI;
using SPT.Reflection.Utils;

namespace SkillsExtended.Helpers;

public static class CursorSettings
{
    private static readonly MethodInfo setCursorMethod;

    static CursorSettings()
    {
        var cursorType = PatchConstants.EftTypes.Single(x => x.GetMethod("SetCursor") != null);
        setCursorMethod = cursorType.GetMethod("SetCursor");
    }

    public static void SetCursor(ECursorType type)
    {
        setCursorMethod.Invoke(null, new object[] { type });
    }
}