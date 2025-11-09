using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;



namespace SkillsExtended.Skills.FieldMedicine.Patches;

internal class PersonalBuffPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BuffSettings), nameof(BuffSettings.GetPersonalBuffSettings));
    }

    [PatchPostfix]
    public static void PostFix(SkillManager skills, InjectorBuff __result)
    {
        if (!Plugin.SkillData.FieldMedicine.Enabled || !__result.IsBuff)
        {
            return;
        }
        
        skills.SkillManagerExtended.AdjustStimulatorBuff((InjectorBuff)__result.Clone());
    }
}