using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Skills.Core;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.SilentOps.Patches;

public class MeleeSpeedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ObjectInHandsAnimator), nameof(ObjectInHandsAnimator.SetMeleeSpeed));
    }

    [PatchPrefix]
    private static void Prefix(ref float speed)
    {
        if (!Plugin.SkillData.SilentOps.Enabled)
        {
            return;
        }

        speed *= 1 + GameUtils.GetSkillManager()!.SkillManagerExtended.SilentOpsIncMeleeSpeedBuff;
    }
}