using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.SilentOps.Patches;

public class DoorSoundPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(WorldInteractiveObject), nameof(WorldInteractiveObject.PlaySound));

    [PatchPrefix]
    private static void Prefix(WorldInteractiveObject __instance, ref float volume)
    {
        if (!SkillsExtendedPlugin.SkillData.SilentOps.Enabled || SkillsExtendedInfo.IsFikaHeadless)
        {
            return;
        }

        if (__instance.InteractingPlayer is not Player player || !player.IsYourPlayer)
        {
            return;
        }

        var skills = player.Skills?.SkillsExtendedManager;
        if (skills != null)
        {
            volume *= Mathf.Clamp01(1f - skills.SilentOpsReduceVolumeBuff);
        }
        // Preserve the game's clips, positions, rolloff and occlusion handling.
    }
}
