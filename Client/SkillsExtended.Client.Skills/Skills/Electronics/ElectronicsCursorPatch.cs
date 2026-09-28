using System.Reflection;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Hacking;

public class ElectronicsCursorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(PlayerOwner), nameof(PlayerOwner.ShouldLockCursor));

    [PatchPostfix]
    public static void Postfix(PlayerOwner __instance, ref ECursorResult __result)
    {
        var view = HackingView.Current;
        if (view && view.InRaid && __instance.Player?.IsYourPlayer == true)
            __result = ECursorResult.ShowCursor;
    }
}
