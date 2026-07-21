using System.Reflection;
using EFT.HealthSystem;
using HarmonyLib;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.FieldMedicine.Patches;

internal class StimulatorApplyBuffPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(ActiveHealthController.Stimulator),
            nameof(ActiveHealthController.Stimulator.EvaluateValue)
        );
    }

    [PatchPrefix]
    public static bool Prefix(
        EffectsSettings.StimulatorSettings.StimulatorBuffSettings buffSettings,
        float refValue,
        Vector2? limits,
        ref float __result
    )
    {
        if (!SkillsExtendedPlugin.SkillData.FieldMedicine.Enabled || limits is null)
        {
            return true;
        }

        var value = buffSettings.AbsoluteValue
            ? buffSettings.Value
            : (buffSettings.Value + 1f) * refValue;
        var skillManager = GameUtils.GetSkillManager();
        var newSkillCap = 60 * (1 + skillManager?.SkillsExtendedManager.FieldMedicineSkillCap);

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
