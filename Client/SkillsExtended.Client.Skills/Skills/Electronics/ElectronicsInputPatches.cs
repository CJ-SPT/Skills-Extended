using System;
using System.Reflection;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Electronics;

// Observe translated movement axes before the game's input suppression. This respects remapped keys.
public class ElectronicsAxesPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GamePlayerOwner), "TranslateAxes");

    [PatchPrefix]
    public static bool Prefix(GamePlayerOwner __instance, ref float[] __0)
    {
        var view = HackingView.Current;
        if (!view || !view.InRaid || !__instance.Player.IsYourPlayer)
        {
            return true;
        }

        var movement = __0.Length >= 2 && (Math.Abs(__0[0]) > .01f || Math.Abs(__0[1]) > .01f);
        Array.Clear(__0, 0, __0.Length);
        if (movement)
        {
            view.Abort();
        }

        return false;
    }
}

public class ElectronicsCommandsPatch : ModulePatch
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
        var view = HackingView.Current;
        if (!view || !view.InRaid || !__instance.Player.IsYourPlayer)
        {
            return true;
        }

        if (
            __0 == ECommand.Jump
            || __0 == ECommand.ToggleSprinting
            || __0 == ECommand.ToggleDuck
            || __0 == ECommand.ToggleProne
            || __0 == ECommand.Escape
        )
        {
            view.Abort();
        }

        __result = InputNode.ETranslateResult.BlockAll;
        return false;
    }
}
