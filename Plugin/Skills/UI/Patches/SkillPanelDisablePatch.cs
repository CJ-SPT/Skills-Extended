using System.Reflection;
using EFT;
using EFT.UI;
using HarmonyLib;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.UI.Patches;

internal class SkillPanelDisablePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SkillPanel), nameof(SkillPanel.Show));

    [PatchPrefix]
    public static bool Prefix(SkillClass skill)
    {
        return skill.Id switch
        {
            ESkillId.UsecNegotiations => SkillUtils.IsUsecNegotiationsAvailable(),
            ESkillId.BearRawpower => SkillUtils.IsBearRawPowerAvailable(),
            _ => !skill.Locked
        };
    }
}