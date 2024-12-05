using System.Reflection;
using HarmonyLib;
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
        if (!Plugin.SkillData.SilentOps.Enabled) return;
        
        var bonus = 1 + Plugin.PlayerSkillManagerExt.SilentOpsIncMeleeSpeedBuff;

        speed *= bonus;
    }
}