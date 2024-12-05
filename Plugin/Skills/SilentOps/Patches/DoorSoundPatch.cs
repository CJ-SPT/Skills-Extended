using System.Reflection;
using Comfort.Common;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.SilentOps.Patches;

public class DoorSoundPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(WorldInteractiveObject), nameof(WorldInteractiveObject.PlaySound));
    }

    [PatchPrefix]
    private static bool Prefix(WorldInteractiveObject __instance, EDoorState state)
    {
        if (!Plugin.SkillData.SilentOps.Enabled) return true;
        
        if (__instance.OpenSound.Length != 0 && state == EDoorState.Open)
        {
            PlayDoorOpenSound(__instance);
        }

        if (__instance.SqueakSound.Length != 0)
        {
            PlayDoorSqueakSound(__instance);
        }

        return false;
    }

    private static void PlayDoorOpenSound(WorldInteractiveObject door)
    {
        var openSound = door.OpenSound[Random.Range(0, door.OpenSound.Length)];
        var bonus = 1f - Plugin.PlayerSkillManagerExt.SilentOpsReduceVolumeBuff;
        
        if (openSound)
        {
            Singleton<BetterAudio>.Instance.PlayAtPoint(
                door.transform.position, 
                openSound, 
                CameraClass.Instance.Distance(door.transform.position), 
                BetterAudio.AudioSourceGroupType.Collisions, 
                35, 
                Random.Range(0.8f * bonus, 1f * bonus), 
                EOcclusionTest.Fast);
        }
    }
    
    private static void PlayDoorSqueakSound(WorldInteractiveObject door)
    {
        var squeakSound = door.SqueakSound[Random.Range(0, door.SqueakSound.Length)];
        var bonus = 1f - Plugin.PlayerSkillManagerExt.SilentOpsReduceVolumeBuff;    
        
        if (squeakSound)
        {
            Singleton<BetterAudio>.Instance.PlayAtPoint(
                door.transform.position, 
                squeakSound, 
                CameraClass.Instance.Distance(door.transform.position), 
                BetterAudio.AudioSourceGroupType.Collisions, 
                35, 
                Random.Range(0.8f * bonus, 1f * bonus), 
                EOcclusionTest.Fast);
        }
    }
}