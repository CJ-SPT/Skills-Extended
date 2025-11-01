using System.Reflection;
using HarmonyLib;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.FieldMedicine.Patches;

public class AbstractSkillClassSummaryLevelPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(AbstractSkillClass), nameof(AbstractSkillClass.SummaryLevel));
    }

    [PatchPrefix]
    public static bool Prefix(AbstractSkillClass __instance, ref int __result)
    {
        if (!Plugin.SkillData.FieldMedicine.Enabled)
        {
            return true;
        }
        
        var skillManager =  GameUtils.GetSkillManager();
        if (skillManager == null)
        {
            return true;
        }
        
        var newSkillCap = 60 * (1 + skillManager?.SkillManagerExtended.FieldMedicineSkillCap);

        var level = __instance.Level;
        var buff = __instance.Buff;
        __result = Mathf.CeilToInt(Mathf.Min(buff > 0 ? newSkillCap : 51, level + buff));

#if DEBUG
        Logger.LogDebug($"New Skill Cap: {__result}");
#endif
        return false;
    }
}