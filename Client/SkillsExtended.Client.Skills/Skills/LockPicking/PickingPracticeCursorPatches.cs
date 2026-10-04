using System.Reflection;
using System.Collections.Generic;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.LockPicking;

internal static class PickingPracticeCursor
{
    internal static bool Active => LockPickingGame.Current && !LockPickingGame.Current.InRaid;
    private static readonly FieldInfo Decision = typeof(InputManager).GetField(
        "_shouldLockCursor", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly Dictionary<InputManager, ECursorResult> NativeDecisions = new();

    internal static void Override(InputManager manager, ref ECursorResult result)
    {
        if (!Active) return;
        NativeDecisions[manager] = result;
        result = ECursorResult.LockCursor;
    }

    internal static void Release()
    {
        // InputManager.Update uses the previous dispatch's cached decision. Restore
        // the menu's last result before restoring the pointer, avoiding one extra
        // lock/unlock frame when returning from the puzzle to setup.
        foreach (var pair in NativeDecisions)
            if (pair.Key) Decision.SetValue(pair.Key, pair.Value);
        NativeDecisions.Clear();
    }
}

// Practice has no PlayerOwner: menu/inventory nodes otherwise keep requesting a
// visible cursor. Set the final native decision, after all input nodes have run.
internal sealed class PickingPracticeDispatchPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(InputManager), "DispatchInput");

    [PatchPostfix]
    public static void Postfix(InputManager __instance, ref ECursorResult ____shouldLockCursor)
    {
        PickingPracticeCursor.Override(__instance, ref ____shouldLockCursor);
    }
}

// Also cover the opening frame's cached decision and force-show events. Correct
// the request before EFT shows/unlocks/warps the pointer, rather than fighting it
// in LateUpdate after relative mouse input has already been sampled.
internal sealed class PickingPracticeVisibilityPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(ClientApplicationInitOperation), "CursorVisibilityChangedHandler");

    [PatchPrefix]
    public static void Prefix(ref bool __0)
    {
        if (PickingPracticeCursor.Active)
            __0 = false;
    }
}
