using System.Reflection;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.Medical;

internal class HealthEffectUseTimePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(HealthEffectsComponent), nameof(HealthEffectsComponent.UseTime));
    }

    [PatchPostfix]
    public static void PostFix(ref float __result, HealthEffectsComponent __instance)
    {
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        var firstAid = Plugin.SkillData.FirstAid;
        
        if (!firstAid.Enabled) return;
            
        __result *= (1f - skillMgrExt.FirstAidItemSpeedBuff);
    }
}