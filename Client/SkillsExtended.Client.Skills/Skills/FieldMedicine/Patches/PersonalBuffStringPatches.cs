using System.Reflection;
using EFT.HealthSystem;
using HarmonyLib;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.FieldMedicine.Patches;

internal class PersonalBuffFullStringPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(EffectsSettings.StimulatorSettings.StimulatorBuffSettings),
            nameof(EffectsSettings.StimulatorSettings.StimulatorBuffSettings.GetStringValue)
        );
    }

    [PatchPrefix]
    public static void Prefix(EffectsSettings.StimulatorSettings.StimulatorBuffSettings __instance)
    {
        if (!SkillsExtendedPlugin.SkillData.FieldMedicine.Enabled)
        {
            return;
        }

        var skillManager = GameUtils.GetSkillManager()?.SkillsExtendedManager;
        skillManager?.AdjustStimulatorBuff(
            (EffectsSettings.StimulatorSettings.StimulatorBuffSettings)__instance.Clone()
        );
    }
}
