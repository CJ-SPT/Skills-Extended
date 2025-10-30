using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;



namespace SkillsExtended.Skills.FieldMedicine.Patches;

internal class PersonalBuffPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BuffSettingsContainer), nameof(BuffSettingsContainer.GetPersonalBuffSettings));
    }

    [PatchPostfix]
    public static void PostFix(SkillManager skills, Buff __result)
    {
        if (!SkillsPlugin.SkillData.FieldMedicine.Enabled || !__result.IsBuff)
        {
            return;
        }
        
        skills.SkillManagerExtended.AdjustStimulatorBuff((Buff)__result.Clone());
    }
}