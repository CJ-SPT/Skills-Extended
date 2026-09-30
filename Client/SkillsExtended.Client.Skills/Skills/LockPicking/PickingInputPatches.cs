using System;
using System.Reflection;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using SkillsExtended.Config;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.LockPicking;

public class PickingAxesPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GamePlayerOwner), "TranslateAxes");

    [PatchPrefix]
    public static bool Prefix(GamePlayerOwner __instance, ref float[] __0)
    {
        var view = LockPickingGame.Current;
        if (!view || !view.InRaid || !__instance.Player.IsYourPlayer)
            return true;
        var moving = __0.Length >= 2 && (Math.Abs(__0[0]) > .01f || Math.Abs(__0[1]) > .01f);
        Array.Clear(__0, 0, __0.Length);
        // The legacy default tension binding is A, also a movement binding. Do not let
        // translated gameplay axes cancel the puzzle while its tension key is held.
        if (moving && !Input.GetKey(ConfigManager.LpMiniGameTurnKey.Value))
            view.Abort();
        return false;
    }
}

public class PickingCommandsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GamePlayerOwner), "TranslateCommand");

    [PatchPrefix]
    public static bool Prefix(
        GamePlayerOwner __instance,
        ECommand __0,
        ref InputNode.ETranslateResult __result
    )
    {
        var view = LockPickingGame.Current;
        if (!view || !view.InRaid || !__instance.Player.IsYourPlayer)
            return true;
        if (
            __0 == ECommand.Escape
            || (
                !Input.GetKey(ConfigManager.LpMiniGameTurnKey.Value)
                && (
                    __0 == ECommand.Jump
                    || __0 == ECommand.ToggleSprinting
                    || __0 == ECommand.ToggleDuck
                    || __0 == ECommand.ToggleProne
                )
            )
        )
            view.Abort();
        __result = InputNode.ETranslateResult.BlockAll;
        return false;
    }
}

public class PickingCursorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(PlayerOwner), nameof(PlayerOwner.ShouldLockCursor));

    [PatchPostfix]
    public static void Postfix(PlayerOwner __instance, ref ECursorResult __result)
    {
        if (
            LockPickingGame.Current
            && LockPickingGame.Current.InRaid
            && __instance.Player?.IsYourPlayer == true
        )
            __result = ECursorResult.LockCursor;
    }
}
