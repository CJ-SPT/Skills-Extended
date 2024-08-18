using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.Skills;

internal class SkillPanelDisablePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SkillPanel), nameof(SkillPanel.Show));

    [PatchPrefix]
    public static bool Prefix(SkillClass skill)
    {
        return !skill.Locked;
    }
}