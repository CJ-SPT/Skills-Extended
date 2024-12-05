using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.FieldMedicine.Patches;

internal class SummaryLevelPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(AbstractSkillClass), nameof(AbstractSkillClass.SummaryLevel));
    }

    [PatchPostfix]
    public static void PostFix(
        AbstractSkillClass __instance, 
        ref int __result)
    {
        if (!Plugin.SkillData.FieldMedicine.Enabled) return;

        var buffLevel = __instance.Buff > 0 
            ? Mathf.FloorToInt(60 * (1 + SkillManagerExt.Instance(EPlayerSide.Usec).FieldMedicineSkillCap))
            : 51;
        
        __result = Mathf.Min(buffLevel, __instance.Level + __instance.Buff);
    }
}