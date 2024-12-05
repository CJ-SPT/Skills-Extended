﻿using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Patches.Skills;

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
            ? Mathf.FloorToInt(60 * (1 + Plugin.PlayerSkillManagerExt.FieldMedicineSkillCap))
            : 51;
        
        __result = Mathf.Min(buffLevel, __instance.Level + __instance.Buff);
    }
}