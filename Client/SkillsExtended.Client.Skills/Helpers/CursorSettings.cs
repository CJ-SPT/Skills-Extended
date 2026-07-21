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
    
    static CursorSettings()
    {
        var cursorType = PatchConstants.EftTypes.Single(x => x.GetMethod("SetCursor") != null);
        
        SetCursorMethod = cursorType.GetMethod("SetCursor");
        SetCursorLockMethod = cursorType.GetMethod("SetCursorLockMode");
    }

    public static void SetCursor(ECursorType type)
    {
        SetCursorMethod.Invoke(null, new object[] { type });
    }
    
    public static void SetCursorLockMode(bool visible, FullScreenMode fullscreenMode)
    {
        SetCursorLockMethod.Invoke(null, new object[] { visible, fullscreenMode });
    }
}