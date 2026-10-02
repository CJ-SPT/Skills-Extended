using System.Reflection;
using HarmonyLib;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.SilentOps.Patches;

public class MeleeSpeedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(ObjectInHandsAnimator),
            nameof(ObjectInHandsAnimator.SetMeleeSpeed)
        );
    }

    [PatchPrefix]
    private static void Prefix(ref float speed)
    {
        if (!SkillsExtendedPlugin.SkillData.SilentOps.Enabled)
        {
            return;
        }

        var skills = GameUtils.GetSkillManager()?.SkillsExtendedManager;
        if (skills == null)
        {
            return;
        }

        speed *= 1 + skills.SilentOpsIncMeleeSpeedBuff;
    }
}
