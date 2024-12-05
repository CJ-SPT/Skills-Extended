using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.Medical;

internal class PersonalBuffPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GClass2756.GClass2781), nameof(GClass2756.GClass2781.GetPersonalBuffSettings));
    }

    [PatchPostfix]
    public static void PostFix(GClass2756.GClass2781.GClass2782 __result)
    {
        if (!Plugin.SkillData.FieldMedicine.Enabled) return;
        if (__result.BuffName == "Pain") return;
        
        var durationBuff = Plugin.PlayerSkillManagerExt.FieldMedicineDurationBonus;
        var chanceBuff = Plugin.PlayerSkillManagerExt.FieldMedicineChanceBonus;
        
        __result.Duration *= 1f + durationBuff;
        __result.Delay *= __result.Delay == 1f 
            ? __result.Delay
            : 1f + durationBuff;
        
        if (__result.Chance < 1f && !__result.Value.Negative())
        {
            __result.Chance *= 1f + chanceBuff;
        }
    }
}