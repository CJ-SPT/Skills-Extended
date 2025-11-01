using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.FirstAid.Patches;

public class HealthEffectComponentPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Constructor(typeof(HealthEffectsComponent), [typeof(Item), typeof(IHealthEffect)]);
    }

    private static readonly Dictionary<MongoID, int> InstanceIdsChangedAtLevel = [];
    private static readonly Dictionary<MongoID, OriginalCostsData> OriginalCosts = [];
    
    [PatchPostfix]
    public static void PostFix(Item item, IHealthEffect template)
    {
        var skillData = Plugin.SkillData.FirstAid;
        if (!skillData.Enabled)
        {
            return;
        }
        
        var skillManager = GameUtils.GetSkillManager();
        if (skillManager == null)
        {
            return;
        }
        
        if (template.DamageEffects is null || item is not MedicalItemClass meds)
        {
            return;
        }
            
        // Why? -- I don't know, but leave it for now because something probably broke
        if (meds.TemplateId.LocalizedName().Contains("Name"))
        {
            return;
        }
            
        ResetLevelChangedAt(meds, skillManager);
        
        if (!OriginalCosts.TryGetValue(meds.TemplateId, out var originalCosts))
        {
            originalCosts = new OriginalCostsData(0, 0, 0);
            OriginalCosts.Add(meds.TemplateId, originalCosts);
        }
        
        if (
            !AdjustLightBleedCost(template, originalCosts, skillManager) && 
            !AdjustHeavyBleedCost(template, originalCosts, skillManager) && 
            !AdjustFractureCost(template, originalCosts, skillManager)
        )
        {
            return;
        }
            
        InstanceIdsChangedAtLevel.Add(item.TemplateId, skillManager.FirstAid.Level);
            
#if DEBUG
        Logger.LogDebug($"Updated Template: {meds.TemplateId.LocalizedName()} \n");
#endif
    }

    private static void ResetLevelChangedAt(MedicalItemClass meds, SkillManager skillManager)
    {
        if (!InstanceIdsChangedAtLevel.TryGetValue(meds.TemplateId, out var level) || 
            level == skillManager.FirstAid.Level)
        {
            return;
        }
        
        InstanceIdsChangedAtLevel.Remove(meds.TemplateId);
    }
    
    private static bool AdjustFractureCost(
        IHealthEffect template, 
        OriginalCostsData originalCosts,
        SkillManager skillManager
        )
    {
        if (!template.DamageEffects.TryGetValue(EDamageEffectType.Fracture, out var fracture))
        {
            return false;
        }

        if (fracture is null || fracture.Cost <= 0)
        {
            return false;
        }
        
        originalCosts.Fracture = originalCosts.Fracture == 0 && fracture.Cost > 0
            ? fracture.Cost
            : originalCosts.Fracture;
                
        var originalCost = originalCosts.Fracture;
        
        skillManager.SkillManagerExtended.FirstAidResourceCostBuff.Apply(ref fracture.Cost);
        
#if DEBUG
        Logger.LogDebug($"[FirstAid] Original Fracture Value: {originalCost}");
        Logger.LogDebug($"[FirstAid] New Fracture Value: {fracture.Cost}");
#endif
        return true;

    }
    
    private static bool AdjustLightBleedCost(
        IHealthEffect template, 
        OriginalCostsData originalCosts,
        SkillManager skillManager
        )
    {
        if (!template.DamageEffects.TryGetValue(EDamageEffectType.LightBleeding, out var lightBleed))
        {
            return false;
        }

        if (lightBleed is null || lightBleed.Cost <= 0)
        {
            return false;
        }
        
        originalCosts.LightBleed = originalCosts.LightBleed == 0 && lightBleed.Cost > 0
            ? lightBleed.Cost
            : originalCosts.LightBleed;
                
        var originalCost = originalCosts.LightBleed;
        skillManager.SkillManagerExtended.FirstAidResourceCostBuff.Apply(ref lightBleed.Cost);
        
#if DEBUG
        Logger.LogDebug($"[FirstAid] Original LightBleeding Value: {originalCost}");
        Logger.LogDebug($"[FirstAid] New LightBleeding Value: {lightBleed.Cost}");
#endif
        return true;
    }
    
    private static bool AdjustHeavyBleedCost(
        IHealthEffect healthEffect, 
        OriginalCostsData originalCosts, 
        SkillManager skillManager
        )
    {
        if (!healthEffect.DamageEffects.TryGetValue(EDamageEffectType.HeavyBleeding, out var heavyBleed))
        {
            return false;
        }

        if (heavyBleed is null || heavyBleed.Cost <= 0)
        {
            return false;
        }
        
        originalCosts.HeavyBleed = originalCosts.HeavyBleed == 0 && heavyBleed.Cost > 0
            ? heavyBleed.Cost
            : originalCosts.HeavyBleed;

        var originalCost = originalCosts.HeavyBleed;
        skillManager.SkillManagerExtended.FirstAidResourceCostBuff.Apply(ref heavyBleed.Cost);
        
#if DEBUG
        Logger.LogDebug($"[FirstAid] Original HeavyBleeding Value: {originalCost}");
        Logger.LogDebug($"[FirstAid] New HeavyBleeding Value: {heavyBleed.Cost}");
#endif
        return true;
    }
    
    private class OriginalCostsData(int fracture = 0, int lightBleed = 0, int heavyBleed = 0)
    {
        public int Fracture = fracture;
        public int LightBleed = lightBleed;
        public int HeavyBleed = heavyBleed;
    }
}