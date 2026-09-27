using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using JsonType;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.FirstAid.Patches;

public class HealthEffectComponentPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.PropertyGetter(typeof(HealthEffectsComponent), nameof(HealthEffectsComponent.DamageEffects));

    [PatchPostfix]
    public static void Postfix(
        HealthEffectsComponent __instance,
        ref Dictionary<EDamageEffectType, DamageEffectSpecification> __result
    )
    {
        if (!SkillsExtendedPlugin.SkillData.FirstAid.Enabled || __result == null
            || __instance.Item.GetItemComponent<MedKitComponent>() == null
            || __instance.Item.Owner is not InventoryController inventory
            || inventory.Profile?.SkillsInfo is not SkillManager skills
            || skills.SkillsExtendedManager == null)
        {
            return;
        }

        var reduction = Mathf.Clamp01(skills.SkillsExtendedManager.FirstAidResourceCostBuff.Value);
        if (reduction <= 0f)
        {
            return;
        }

        // Templates are shared by every instance. Return personal costs so treatment
        // eligibility and resource consumption agree without changing anyone else's kit.
        var adjusted = new Dictionary<EDamageEffectType, DamageEffectSpecification>(__result);
        foreach (var type in new[] { EDamageEffectType.LightBleeding, EDamageEffectType.HeavyBleeding, EDamageEffectType.Fracture })
        {
            if (!__result.TryGetValue(type, out var effect) || effect == null || effect.Cost <= 0)
            {
                continue;
            }

            adjusted[type] = new DamageEffectSpecification
            {
                Cost = Mathf.CeilToInt(effect.Cost * (1f - reduction)),
                Delay = effect.Delay,
                Duration = effect.Duration,
                FadeOut = effect.FadeOut,
                HealthPenaltyMin = effect.HealthPenaltyMin,
                HealthPenaltyMax = effect.HealthPenaltyMax,
            };
        }
        __result = adjusted;
    }
}
