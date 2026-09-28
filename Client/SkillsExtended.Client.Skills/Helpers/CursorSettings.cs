using System.Linq;
using System.Reflection;
using EFT.UI;
using SPT.Reflection.Utils;
using UnityEngine;

namespace SkillsExtended.Helpers;

public static class CursorSettings
{
    private static readonly MethodInfo SetCursorMethod;
    private static readonly MethodInfo SetCursorLockMethod;
    private static readonly PropertyInfo PreviousCursorProperty;
    
    static CursorSettings()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
        var cursorParameters = new[] { typeof(ECursorType) };
        var lockParameters = new[] { typeof(bool), typeof(FullScreenMode) };

        // SetCursor also has a CursorData overload; resolve the signatures we invoke.
        var cursorType = PatchConstants.EftTypes.Single(x =>
            x.GetMethod("SetCursor", flags, null, cursorParameters, null) != null &&
            x.GetMethod("SetCursorLockMode", flags, null, lockParameters, null) != null);
        
        SetCursorMethod = cursorType.GetMethod("SetCursor", flags, null, cursorParameters, null);
        SetCursorLockMethod = cursorType.GetMethod("SetCursorLockMode", flags, null, lockParameters, null);
        PreviousCursorProperty = cursorType.GetProperty("PreviousType", flags);
    }

    public static void SetCursor(ECursorType type)
    {
        SetCursorMethod.Invoke(null, new object[] { type });
    }

    public static ECursorType CurrentCursor => (ECursorType)PreviousCursorProperty.GetValue(null);
    
    public static void SetCursorLockMode(bool visible, FullScreenMode fullscreenMode)
    {
        SetCursorLockMethod.Invoke(null, new object[] { visible, fullscreenMode });
    }
}
