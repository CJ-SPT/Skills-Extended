using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using SkillsExtended.Skills;
using UnityEngine;
using Random = System.Random;

namespace SkillsExtended.Controllers;

public class FirstAidBehaviour : MonoBehaviour
{
    private readonly Dictionary<string, MedKitValues> _originalMedKitValues = [];

    private static SkillManager SkillManager => Utils.GetActiveSkillManager();
    private static SkillManagerExt SkillMgrExt => Singleton<SkillManagerExt>.Instance;
    
    private int _lastAppliedLevel = -1;

    private static MedicalSkillData SkillData => Plugin.SkillData.MedicalSkills;

    // Store the instance ID of the item and the level its bonus resource is set to.
    public readonly Dictionary<string, int> FirstAidInstanceIDs = [];
    
    private readonly Dictionary<string, HealthEffectValues> _originalHealthEffectValues = [];

    private static float FaPmcSpeedBonus => 1f - SkillMgrExt.FirstAidSpeedBuff;
    private static float FaHpBonus => 1 + SkillMgrExt.FirstAidHpBuff;
    
    public void ApplyFirstAidExp()
    {
        var xpGain = Plugin.SkillData.MedicalSkills.FirstAidXpPerAction;
        SkillMgrExt.FirstAidAction.Complete(xpGain);
    }
    
    public IEnumerator FirstAidUpdate()
    {
        if (Plugin.Items is null || _lastAppliedLevel == SkillManager.FirstAid.Level)
        {
            yield break;
        }
        
        var items = Plugin.Items.Where(x => x is MedsClass);
        
        foreach (var item in items)
        {
            // Skip if we already set this first aid item.
            if (FirstAidInstanceIDs.ContainsKey(item.Id))
            {
                int previouslySet = FirstAidInstanceIDs[item.Id];

                if (previouslySet == SkillManager.FirstAid.Level)
                {
                    continue;
                }
                
                FirstAidInstanceIDs.Remove(item.Id);
            }

            // Apply first aid speed bonus to items
            if (!SkillData.FaItemList.Contains(item.TemplateId)) continue;

            ApplyFirstAidSpeedBonus(item);
            ApplyFirstAidHpBonus(item);
            FirstAidInstanceIDs.Add(item.Id, SkillManager.FirstAid.Level);

            yield return null;
        }

        _lastAppliedLevel = SkillManager.FirstAid.Level;
    }
    
    private void ApplyFirstAidSpeedBonus(Item item)
    {
        var bonus = FaPmcSpeedBonus;

        if (FirstAidInstanceIDs.ContainsKey(item.Id)) return;
        if (item is not MedsClass meds) return;
        
        if (!_originalHealthEffectValues.ContainsKey(item.TemplateId))
        {
            var origValues = new HealthEffectValues
            {
                UseTime = meds.HealthEffectsComponent.UseTime,
                BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults,
                HealthEffects = meds.HealthEffectsComponent.HealthEffects,
                DamageEffects = meds.HealthEffectsComponent.DamageEffects,
                StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs
            };

            _originalHealthEffectValues.Add(item.TemplateId, origValues);
        }

        IHealthEffect newGInterface = new HealthEffectValues
        {
            UseTime = _originalHealthEffectValues[meds.TemplateId].UseTime * bonus,
            BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults,
            HealthEffects = meds.HealthEffectsComponent.HealthEffects,
            DamageEffects = meds.HealthEffectsComponent.DamageEffects,
            StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs
        };

        var healthEffectComp = AccessTools.Field(typeof(MedsClass), "HealthEffectsComponent").GetValue(meds);
        AccessTools.Field(typeof(HealthEffectsComponent), "iHealthEffect").SetValue(healthEffectComp, newGInterface);

        Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {_originalHealthEffectValues[meds.TemplateId].UseTime * bonus} seconds");
    }

    private void ApplyFirstAidHpBonus(Item item)
    {
        // Don't apply HP bonuses with realism med changes enabled
        if (Plugin.RealismConfig.med_changes) return;
        if (FirstAidInstanceIDs.ContainsKey(item.Id)) return;
        if (item is not MedsClass meds) return;
        
        // Add the original medkit template to the original dictionary
        if (!_originalMedKitValues.ContainsKey(item.TemplateId))
        {
            var origMedValues = new MedKitValues
            {
                MaxHpResource = meds.MedKitComponent.MaxHpResource,
                HpResourceRate = meds.MedKitComponent.HpResourceRate
            };

            _originalMedKitValues.Add(item.TemplateId, origMedValues);
        }

        var maxHpResource = Mathf.FloorToInt(_originalMedKitValues[item.TemplateId].MaxHpResource * FaHpBonus);
        
        if (meds.TemplateId == "590c657e86f77412b013051d")
        {
            maxHpResource = Mathf.Clamp(maxHpResource, 1800, 2750);
        }

        var medKitInterface = new MedKitValues
        {
            MaxHpResource = maxHpResource,
            HpResourceRate = meds.MedKitComponent.HpResourceRate
        };

        Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {medKitInterface.MaxHpResource} HP");

        var currentResource = meds.MedKitComponent.HpResource;
        var currentMaxResource = meds.MedKitComponent.MaxHpResource;

        // Only change the current resource if the item is unused.
        if (Mathf.FloorToInt(currentResource) == currentMaxResource)
        {
            meds.MedKitComponent.HpResource = medKitInterface.MaxHpResource;
        }

        var medComp = AccessTools.Field(typeof(MedsClass), "MedKitComponent").GetValue(meds);
        AccessTools.Field(typeof(MedKitComponent), "iMedkitResource").SetValue(medComp, medKitInterface);
    }
}
