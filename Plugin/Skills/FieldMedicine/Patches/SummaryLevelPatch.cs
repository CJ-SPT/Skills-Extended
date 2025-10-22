using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Helpers;
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
        if (!SkillsPlugin.SkillData.FieldMedicine.Enabled)
        {
            return;
        }

        var skillManager =  GameUtils.IsInRaid() 
            ? GameUtils.GetPlayer()?.Skills 
            : GameUtils.GetProfile(EPlayerSide.Usec)?.Skills;
        
        var buffLevel = __instance.Buff > 0 
            ? Mathf.FloorToInt(60 * (1 + skillManager?.SkillManagerExtended.FieldMedicineSkillCap))
            : 51;
        
        __result = Mathf.Min(buffLevel, __instance.Level + __instance.Buff);
    }
}