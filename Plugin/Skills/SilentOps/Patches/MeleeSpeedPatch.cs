using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Skills.Core;
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
        if (!SkillsPlugin.SkillData.SilentOps.Enabled) return;
        
        var bonus = 1 + SkillManagerExt.Instance(EPlayerSide.Usec).SilentOpsIncMeleeSpeedBuff;

        speed *= bonus;
    }
}