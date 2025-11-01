using System.Reflection;
using EFT.HealthSystem;
using SkillsExtended.Helpers;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.FieldMedicine.Patches;

internal class StimulatorApplyBuffPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        var healthController = typeof(ActiveHealthController);
        var nestedTypes = healthController.GetNestedType("Stimulator", BindingFlags.NonPublic);
        return nestedTypes.GetMethod("smethod_0", BindingFlags.Static | BindingFlags.Public);
    }

    [PatchPrefix]
    public static bool Prefix(InjectorBuff buffSettings, float refValue, Vector2? limits, ref float __result)
    {
        if (!Plugin.SkillData.FieldMedicine.Enabled || limits is null)
        {
            return true;
        }
        
        var value = buffSettings.AbsoluteValue ? buffSettings.Value : (buffSettings.Value + 1f) * refValue;
        var skillManager =  GameUtils.GetSkillManager();
        var newSkillCap = 60 * (1 + skillManager?.SkillManagerExtended.FieldMedicineSkillCap);
        
        __result = Mathf.CeilToInt(Mathf.Clamp(value, limits.Value.x, newSkillCap));

#if DEBUG
        Logger.LogDebug("==================================================================");
        Logger.LogDebug($"Skill Name:                   `{buffSettings.SkillName}`");
        Logger.LogDebug($"IsAbsolute:                   `{buffSettings.AbsoluteValue}`");
        Logger.LogDebug($"Buff Value:                   `{value}`");
        Logger.LogDebug($"Adjusted max skill cap:       `{__result}`");
        Logger.LogDebug("==================================================================");
#endif
        
        return false;
    }
}