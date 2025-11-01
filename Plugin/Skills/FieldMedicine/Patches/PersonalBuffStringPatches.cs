using System.Reflection;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.FieldMedicine.Patches;

internal class PersonalBuffFullStringPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InjectorBuff), nameof(InjectorBuff.GetStringValue));
    }

    [PatchPrefix]
    public static void Prefix(InjectorBuff __instance)
    {
        if (!Plugin.SkillData.FieldMedicine.Enabled)
        {
            return;
        }

        var skillManager = GameUtils.GetSkillManager()?.SkillManagerExtended;
        skillManager?.AdjustStimulatorBuff((InjectorBuff)__instance.Clone());
    }
}