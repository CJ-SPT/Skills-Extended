using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.FirstAid.Patches;

internal class HealthEffectUseTimePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(HealthEffectsComponent), nameof(HealthEffectsComponent.UseTime));
    }

    [PatchPostfix]
    public static void PostFix(ref float __result, HealthEffectsComponent __instance)
    {
        var firstAid = SkillsPlugin.SkillData.FirstAid;

        if (!firstAid.Enabled)
        {
            return;
        }

        var skillManager = GameUtils.GetSkillManager();
        if (skillManager == null)
        {
            Logger.LogError("Skill Manager is null");
            return;
        }
        
        __result *= 1f - skillManager.SkillManagerExtended.FirstAidItemSpeedBuff;
    }
}

internal class SpawnPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player.MedsController), nameof(Player.MedsController.Spawn));
    }

    [PatchPrefix]
    public static void PreFix(ref float animationSpeed)
    {
        var firstAid = SkillsPlugin.SkillData.FirstAid;

        if (!firstAid.Enabled)
        {
            return;
        }
        
        var skillManager = GameUtils.GetSkillManager();
        if (skillManager == null)
        {
            Logger.LogError("Skill Manager is null");
            return;
        }
            
        animationSpeed *= 1f + skillManager.SkillManagerExtended.FirstAidItemSpeedBuff;
    }
}