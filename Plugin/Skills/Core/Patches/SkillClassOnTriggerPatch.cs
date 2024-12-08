using System.Reflection;
using HarmonyLib;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Core.Patches;

public class SkillClassOnTriggerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillClass), nameof(SkillClass.OnTrigger));
    }

    [PatchPrefix]
    public static void PatchPrefix(SkillClass __instance)
    {
        if (__instance.SkillManager.BonusController is not null) return;
        
        // BonusController is called in SkillClass.OnTrigger and must not be null,
        // otherwise it will trigger System.NullReferenceException.
        __instance.SkillManager.BonusController = GameUtils.GetProfile(true)?.BonusController;
    }
}