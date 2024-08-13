using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT.HealthSystem;
using HarmonyLib;
using SkillsExtended.Skills;
using UnityEngine;

namespace SkillsExtended.Patches;

internal class DoMedEffectPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.DoMedEffect));
    }

    [PatchPrefix]
    public static void Prefix(ActiveHealthController __instance, Item item)
    {
        // Don't give xp for surgery
        if (item.TemplateId == "5d02778e86f774203e7dedbe" || item.TemplateId == "5d02797c86f774203f38e30a")
        {
            return;
        }

        if (!__instance.Player.IsYourPlayer || item is not MedsClass)
        {
            return;
        }

        if (Plugin.SkillData.MedicalSkills.FmItemList.Contains(item.TemplateId) && Plugin.SkillData.MedicalSkills.EnableFieldMedicine)
        {
            __instance.Player.ExecuteSkill(Plugin.FieldMedicineScript.ApplyFieldMedicineExp);
            return;
        }

        if (Plugin.SkillData.MedicalSkills.FaItemList.Contains(item.TemplateId) && Plugin.SkillData.MedicalSkills.EnableFirstAid)
        {
            __instance.Player.ExecuteSkill(ApplyFirstAidExp);
        }
    }
    
    private static void ApplyFirstAidExp()
    {
        var skillMgrExt = Singleton<SkillManagerExt>.Instance;
        var xpGain = Plugin.SkillData.MedicalSkills.FirstAidXpPerAction;
        skillMgrExt.FirstAidAction.Complete(xpGain);
    }
}

internal class HealthEffectUseTimePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(HealthEffectsComponent), nameof(HealthEffectsComponent.UseTime));
    }

    [PatchPostfix]
    public static void PostFix(ref float __result, HealthEffectsComponent __instance)
    {
        var skillMgrExt = Singleton<SkillManagerExt>.Instance;

        var skillData = Plugin.SkillData.MedicalSkills;
        
        if (skillData.FaItemList.Contains(__instance.Item.TemplateId))
        {
            __result *= (1f - skillMgrExt.FirstAidItemSpeedBuff);
            return;
        }
        
        if (skillData.FmItemList.Contains(__instance.Item.TemplateId))
        {
            __result *= (1f - skillMgrExt.FieldMedicineSpeedBuff);
        }
    }
}

internal class HealthEffectDamageEffectPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(HealthEffectsComponent), nameof(HealthEffectsComponent.DamageEffects));
    }

    private static Dictionary<string, int> _instanceIdsChangedAtLevel = [];
    
    [PatchPostfix]
    public static void PostFix(Dictionary<EDamageEffectType, GClass1244> __result, HealthEffectsComponent __instance)
    {
        var skillMgrExt = Singleton<SkillManagerExt>.Instance;
        var skillData = Plugin.SkillData.MedicalSkills;

        if (!skillData.EnableFirstAid) return;
        
        if (_instanceIdsChangedAtLevel.TryGetValue(__instance.Item.TemplateId, out var level))
        {
            // We've changed this item at this level
            if (level == Plugin.Session.Profile.Skills.FirstAid.Level) return;

            _instanceIdsChangedAtLevel.Remove(__instance.Item.TemplateId);
        }
        
        if (skillData.FaItemList.Contains(__instance.Item.TemplateId))
        {
            if (__result.TryGetValue(EDamageEffectType.Fracture, out var fracture))
            {
                Logger.LogDebug($"Original Fracture Value: {fracture.Cost}");
                var originalCost = fracture.Cost;
                fracture.Cost = Mathf.FloorToInt(originalCost * (1f - skillMgrExt.FirstAidItemSpeedBuff));
                Logger.LogDebug($"New Fracture Value: {fracture.Cost}");
            }
            
            if (__result.TryGetValue(EDamageEffectType.LightBleeding, out var lightBleed))
            {
                Logger.LogDebug($"Original LightBleeding Value: {lightBleed.Cost}");
                lightBleed.Cost = Mathf.FloorToInt(lightBleed.Cost * (1f - skillMgrExt.FirstAidResourceCostBuff));
                Logger.LogDebug($"New LightBleeding Value: {lightBleed.Cost}");
            }
            
            if (__result.TryGetValue(EDamageEffectType.HeavyBleeding, out var heavyBleed))
            {
                Logger.LogDebug($"Original HeavyBleeding Value: {heavyBleed.Cost}");
                heavyBleed.Cost = Mathf.FloorToInt(heavyBleed.Cost * (1f - skillMgrExt.FirstAidResourceCostBuff));
                Logger.LogDebug($"New HeavyBleeding Value: {heavyBleed.Cost}");
            }
            
            _instanceIdsChangedAtLevel.Add(__instance.Item.TemplateId, Plugin.Session.Profile.Skills.FirstAid.Level);
        }
    }
}