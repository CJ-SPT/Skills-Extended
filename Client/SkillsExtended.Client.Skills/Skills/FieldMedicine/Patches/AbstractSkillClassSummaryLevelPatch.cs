using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.FieldMedicine.Patches;

public class AbstractSkillClassSummaryLevelPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(Skill), nameof(Skill.SummaryLevel));
    }

    [PatchPrefix]
    public static bool Prefix(Skill __instance, ref int __result)
    {
        if (!SkillsExtendedPlugin.SkillData.FieldMedicine.Enabled)
        {
            return true;
        }

        var skillManager = GameUtils.GetSkillManager();
        if (skillManager == null)
        {
            return true;
        }

        var newSkillCap = 60 * (1 + skillManager.SkillsExtendedManager.FieldMedicineSkillCap);

        var level = __instance.Level;
        var buff = __instance.Buff;
        __result = Mathf.CeilToInt(Mathf.Min(buff > 0 ? newSkillCap : 51, level + buff));

        return false;
    }
}
