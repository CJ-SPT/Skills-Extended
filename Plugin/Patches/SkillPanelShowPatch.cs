using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches;

internal class SkillPanelShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillPanel), nameof(SkillPanel.Show));
    }

    [PatchPrefix]
    public static bool Prefix(SkillClass skill)
    {
        return !skill.Locked;
    }
}