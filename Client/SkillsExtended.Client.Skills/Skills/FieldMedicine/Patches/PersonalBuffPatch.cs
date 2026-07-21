using System.Reflection;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.FieldMedicine.Patches;

internal class PersonalBuffPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(EffectsSettings.StimulatorSettings),
            nameof(EffectsSettings.StimulatorSettings.GetPersonalBuffSettings)
        );
    }

    [PatchPostfix]
    public static void PostFix(
        SkillManager skills,
        EffectsSettings.StimulatorSettings.StimulatorBuffSettings __result
    )
    {
        if (!SkillsExtendedPlugin.SkillData.FieldMedicine.Enabled || !__result.IsBuff)
        {
            return;
        }

        skills.SkillsExtendedManager.AdjustStimulatorBuff(
            (EffectsSettings.StimulatorSettings.StimulatorBuffSettings)__result.Clone()
        );
    }
}
