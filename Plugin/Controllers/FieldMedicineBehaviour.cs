using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using SkillsExtended.Skills;
using UnityEngine;

namespace SkillsExtended.Controllers;

internal class FieldMedicineBehaviour : MonoBehaviour
{
    private static SkillManager SkillManager => Utils.GetActiveSkillManager();

    private int _lastAppliedLevel = -1;

    private static MedicalSkillData SkillData => Plugin.SkillData.MedicalSkills;

    private static float FmPmcSpeedBonus => 1f - SkillBuffs.FieldMedicineSpeedBuff;

    private readonly Dictionary<string, HealthEffectValues> _originalHealthEffectValues = [];

    private readonly Dictionary<EBodyPart, DateTime> _fieldMedicineBodyPartCache = [];

    public readonly Dictionary<string, int> FieldMedicineInstanceIDs = [];

    private void Update()
    {
        if (Plugin.Items is null || _lastAppliedLevel == SkillManager.FieldMedicine.Level)
        {
            return;
        }

        if (Plugin.GameWorld?.MainPlayer is null)
        {
            _fieldMedicineBodyPartCache.Clear();
        }

        FieldMedicineUpdate();
    }

    public void ApplyFieldMedicineExp(EBodyPart bodypart)
    {
        var xpGain = Plugin.SkillData.MedicalSkills.FieldMedicineXpPerAction;
        
        SkillManager.FieldMedicine.Actions[0].Complete(xpGain);
    }

    private void ApplyFieldMedicineSpeedBonus(Item item)
    {
        var bonus = FmPmcSpeedBonus;

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

        IHealthEffect healthEffects = new HealthEffectValues
        {
            UseTime = _originalHealthEffectValues[meds.TemplateId].UseTime * bonus,
            BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults,
            HealthEffects = meds.HealthEffectsComponent.HealthEffects,
            DamageEffects = meds.HealthEffectsComponent.DamageEffects,
            StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs
        };

        var healthEffectComp = AccessTools.Field(typeof(MedsClass), "HealthEffectsComponent").GetValue(meds);
        AccessTools.Field(typeof(HealthEffectsComponent), "iHealthEffect").SetValue(healthEffectComp, healthEffects);

        Plugin.Log.LogDebug($"Field Medicine: Set instance {item.Id} of type {item.TemplateId} to {_originalHealthEffectValues[meds.TemplateId].UseTime * bonus} seconds");
    }

    public void FieldMedicineUpdate()
    {
        var items = Plugin.Items.Where(x => x is MedsClass);
        
        foreach (var item in items)
        {
            // Skip if we already set this field medicine item.
            if (FieldMedicineInstanceIDs.ContainsKey(item.Id))
            {
                var previouslySet = FieldMedicineInstanceIDs[item.Id];

                if (previouslySet == SkillManager.FieldMedicine.Level)
                {
                    continue;
                }
                
                FieldMedicineInstanceIDs.Remove(item.Id);
            }

            // Apply Field medicine speed bonus to items not in the list
            if (!SkillData.FmItemList.Contains(item.TemplateId)) continue;
            
            ApplyFieldMedicineSpeedBonus(item);
            FieldMedicineInstanceIDs.Add(item.Id, SkillManager.FieldMedicine.Level);
        }

        _lastAppliedLevel = SkillManager.FieldMedicine.Level;
    }
}
