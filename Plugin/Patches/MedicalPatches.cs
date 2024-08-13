using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Reflection;
using System.Text;
using EFT.HealthSystem;
using EFT.ObstacleCollision;
using HarmonyLib;
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

        if (Plugin.SkillData.FieldMedicine.ItemList.Contains(item.TemplateId) && Plugin.SkillData.FieldMedicine.Enabled)
        {
            __instance.Player.ExecuteSkill(ApplyFieldMedicineExp);
            return;
        }

        if (Plugin.SkillData.FirstAid.ItemList.Contains(item.TemplateId) && Plugin.SkillData.FirstAid.Enabled)
        {
            __instance.Player.ExecuteSkill(ApplyFirstAidExp);
        }
    }
    
    private static void ApplyFirstAidExp()
    {
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        var xpGain = Plugin.SkillData.FirstAid.FirstAidXpPerAction;
        skillMgrExt.FirstAidAction.Complete(xpGain);
    }
    
    private static void ApplyFieldMedicineExp()
    {
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        var xpGain = Plugin.SkillData.FieldMedicine.FieldMedicineXpPerAction;
        skillMgrExt.FieldMedicineAction.Complete(xpGain);
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
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        var firstAid = Plugin.SkillData.FirstAid;
        
        if (!firstAid.Enabled) return;
            
        __result *= (1f - skillMgrExt.FirstAidItemSpeedBuff);
    }
}

internal class HealthEffectDamageEffectPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(HealthEffectsComponent), nameof(HealthEffectsComponent.DamageEffects));
    }

    private static Dictionary<string, int> _instanceIdsChangedAtLevel = [];
    private static Dictionary<string, OriginalCosts> _originalCosts = [];
    
    [PatchPostfix]
    public static void PostFix(Dictionary<EDamageEffectType, GClass1244> __result, HealthEffectsComponent __instance)
    {
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        var skillData = Plugin.SkillData.FirstAid;
        
        if (!skillData.Enabled) return;
        if (!skillData.ItemList.Contains(__instance.Item.TemplateId)) return;
        if (__instance.Item is not MedsClass meds) return;
        
        if (_instanceIdsChangedAtLevel.TryGetValue(meds.TemplateId, out var level))
        {
            // We've changed this item at this level
            if (level == Plugin.Session.Profile.Skills.FirstAid.Level) return;
            
            _instanceIdsChangedAtLevel.Remove(meds.TemplateId);
        }
        
        Logger.LogDebug($"Updating Template: {meds.TemplateId.LocalizedName()}");

        if (_originalCosts.TryGetValue(meds.TemplateId, out var originalCosts))
        { }
        
        if (__result.TryGetValue(EDamageEffectType.Fracture, out var fracture))
        {
            var originalCost = originalCosts.Fracture ?? fracture.Cost;
            Logger.LogDebug($"Original Fracture Value: {originalCost}");
            fracture.Cost = Mathf.FloorToInt(originalCost * (1f - skillMgrExt.FirstAidItemSpeedBuff));
            Logger.LogDebug($"New Fracture Value: {fracture.Cost}");
        }
            
        if (__result.TryGetValue(EDamageEffectType.LightBleeding, out var lightBleed))
        {
            var originalCost = originalCosts.LightBleed ?? lightBleed.Cost;
            Logger.LogDebug($"Original LightBleeding Value: {originalCost}");
            lightBleed.Cost = Mathf.FloorToInt(originalCost * (1f - skillMgrExt.FirstAidResourceCostBuff));
            Logger.LogDebug($"New LightBleeding Value: {lightBleed.Cost}");
        }
            
        if (__result.TryGetValue(EDamageEffectType.HeavyBleeding, out var heavyBleed))
        {
            var originalCost = originalCosts.HeavyBleed ?? heavyBleed.Cost;
            Logger.LogDebug($"Original HeavyBleeding Value: {originalCost}");
            heavyBleed.Cost = Mathf.FloorToInt(originalCost * (1f - skillMgrExt.FirstAidResourceCostBuff));
            Logger.LogDebug($"New HeavyBleeding Value: {heavyBleed.Cost}");
        }
        
        if (!_originalCosts.ContainsKey(__instance.Item.TemplateId))
        {
            var newOriginalCosts = new OriginalCosts(
                fracture?.Cost ?? 0, 
                lightBleed?.Cost ?? 0,
                heavyBleed?.Cost ?? 0);
            
            _originalCosts.Add(meds.TemplateId, newOriginalCosts);
        }
        
        _instanceIdsChangedAtLevel.Add(__instance.Item.TemplateId, Plugin.Session.Profile.Skills.FirstAid.Level);
    }
    
    private struct OriginalCosts(int? fracture, int? lightBleed, int? heavyBleed)
    {
        public int? Fracture = fracture;
        public int? LightBleed = lightBleed;
        public int? HeavyBleed = heavyBleed;
    }
}

internal class CanWalkPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(MovementContext), nameof(MovementContext.CanWalk));
    }

    [PatchPostfix]
    public static void PostFix(
        MovementContext __instance, 
        Player ____player,
        IObstacleCollisionFacade ____obstacleCollisionFacade,
        ref bool __result)
    {
        if (!____player.IsYourPlayer) return;
        
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        var skillData = Plugin.SkillData.FirstAid;

        if (!skillData.Enabled) return;
        if (!skillMgrExt.FirstAidMovementSpeedBuffElite) return;

        __result = ____obstacleCollisionFacade.CanMove();
    }
}

internal class SummaryLevelPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(GClass1778), nameof(GClass1778.SummaryLevel));
    }

    [PatchPostfix]
    public static void PostFix(
        GClass1778 __instance, 
        ref int __result)
    {
        var buffLevel = __instance.Buff > 0 
            ? Mathf.FloorToInt(60 * (1 + Plugin.PlayerSkillManagerExt.FieldMedicineSkillCap))
            : 51;
        
        __result = Mathf.Min(buffLevel, __instance.Level + __instance.Buff);
    }
}

internal class PersonalBuffPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GClass2438.GClass2463), nameof(GClass2438.GClass2463.GetPersonalBuffSettings));
    }

    [PatchPostfix]
    public static void PostFix(GClass2438.GClass2463.GClass2464 __result)
    {
        var durationBuff = Plugin.PlayerSkillManagerExt.FieldMedicineDurationBonus;
        var chanceBuff = Plugin.PlayerSkillManagerExt.FieldMedicineChanceBonus;
        
        __result.Duration *= 1f + durationBuff;
        __result.Delay *= __result.Delay == 1f 
            ? __result.Delay
            : 1f + durationBuff;
        
        if (__result.Chance < 1f && !__result.Value.Negative())
        {
            __result.Chance *= 1f + chanceBuff;
        }
    }
}

internal class PersonalBuffFullStringPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GClass2438.GClass2463.GClass2464), nameof(GClass2438.GClass2463.GClass2464.GetStringValue));
    }

    [PatchPostfix]
    public static void PostFix(
        GClass2438.GClass2463.GClass2464 __instance,
        HashSet<string> ___hashSet_0,
        ref string __result)
    {
        var durationBuff = Plugin.PlayerSkillManagerExt.FieldMedicineDurationBonus;
        var chanceBuff = Plugin.PlayerSkillManagerExt.FieldMedicineChanceBonus;
        
        var flag = __instance.Value.IsZero();
        if (__instance.Delay.IsZero() && (__instance.Duration.IsZero() || __instance.Duration >= 7200f) && __instance.Value.IsZero())
        {
            __result = string.Empty;
            return;
        }
        var stringBuilder = new StringBuilder();
        var text = __instance.BuffName.Localized();
        if (flag && !___hashSet_0.Contains(__instance.BuffName))
        {
            stringBuilder.Append("Applies".Localized() + " ");
            text = text.ToLower();
        }
        stringBuilder.Append(text);
        if (!flag)
        {
            stringBuilder.Append("\n");
            stringBuilder.Append((__instance.Value.Positive() ? "Increase".Localized() : "Decrease".Localized()) + " ");
            stringBuilder.Append(__instance.BuffAbsoluteStringValue());
        }
        if (__instance.Chance < 1f)
        {
            stringBuilder.Append(string.Format("\n{0} {1}%", "UI/ItemAttribute/Chance".Localized(), Math.Round(__instance.Chance * (1f + chanceBuff) * 100f)));
        }
        if (__instance.Delay > 1f)
        {
            stringBuilder.Append(string.Format("\n{0} {1}{2}", "Delay".Localized(), __instance.Delay * (1f + durationBuff), "sec".Localized()));
        }
        if (__instance.Duration > 0f && __instance.Duration < 7200f)
        {
            stringBuilder.AppendFormat("\n{0} {1}{2}", "Duration".Localized(), __instance.Duration * (1f + durationBuff), "sec".Localized());
        }

        __result = stringBuilder.ToString();
    }
}

internal class PersonalBuffStringPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GClass2438.GClass2463.GClass2464), nameof(GClass2438.GClass2463.GClass2464.GetStringValue));
    }

    [PatchPostfix]
    public static void PostFix(
        GClass2438.GClass2463.GClass2464 __instance, 
        HashSet<string> ___hashSet_0,
        ref string __result)
    {
        var durationBuff = Plugin.PlayerSkillManagerExt.FieldMedicineDurationBonus;
        var chanceBuff = Plugin.PlayerSkillManagerExt.FieldMedicineChanceBonus;
        
        var text = __instance.BuffColoredStringValue();
        var flag = __instance.Value.IsZero();
        var stringBuilder = new StringBuilder();
        if (__instance.Chance < 1f)
        {
            stringBuilder.Append(string.Format("{0} {1}%", "UI/ItemAttribute/Chance".Localized(), Math.Round((__instance.Chance * (1f + chanceBuff) * 100f))));
        }
        if (__instance.Delay > 1f)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(" / ");
            }
            stringBuilder.Append(string.Format("{0} {1}{2}", "Del.".Localized(), __instance.Delay * (1f + durationBuff), "sec".Localized()));
        }
        if (__instance.Duration > 0f && __instance.Duration < 7200f)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(" / ");
            }
            stringBuilder.Append(string.Format("{0} {1}{2}", "Dur.".Localized(), __instance.Duration * (1f + durationBuff), "sec".Localized()));
            if (!flag)
            {
                stringBuilder.Append(" (" + text + ")");
            }
        }
        else if (!flag)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(" / ");
            }
            stringBuilder.Append(" " + text);
        }

        __result = stringBuilder.ToString();
    }
}