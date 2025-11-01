using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Core.Patches;

public class SkillClassOnTriggerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillClass), nameof(SkillClass.OnTrigger));
    }

    // BonusController is called in SkillClass.OnTrigger and must not be null,
    // otherwise it will trigger System.NullReferenceException.
    
    [PatchPrefix]
    public static void PatchPrefix(SkillClass __instance)
    {
        if (__instance.SkillManager.BonusController is not null)
        {
            return;
        }
        
        __instance.SkillManager.BonusController = GameUtils.IsScav() 
            ? GameUtils.GetProfile(EPlayerSide.Savage)?.BonusController 
            // Usec and bear retrieve the same profile
            : GameUtils.GetProfile(EPlayerSide.Usec)?.BonusController;
    }
}