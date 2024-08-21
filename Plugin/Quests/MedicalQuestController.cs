using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using UnityEngine;

namespace SkillsExtended.Quests;

public class MedicalQuestController 
    : AbstractCustomQuestController
{
    public MedicalQuestController(QuestProgressController questProgressController)
        : base(questProgressController)
    {
        _player.ActiveHealthController.EffectRemovedEvent += RemoveHealthConditionTest;
        _player.ActiveHealthController.HealthChangedEvent += HealthChangeTest;
        _player.ActiveHealthController.BodyPartDestroyedEvent += HandleBodyPartDestroyed;
        _player.ActiveHealthController.BodyPartRestoredEvent += HandleBodyPartRestored;
    }
    
    public void Dispose()
    {
        _player.ActiveHealthController.EffectRemovedEvent -= RemoveHealthConditionTest;
        _player.ActiveHealthController.HealthChangedEvent -= HealthChangeTest;
        _player.ActiveHealthController.BodyPartDestroyedEvent -= HandleBodyPartDestroyed;
        _player.ActiveHealthController.BodyPartRestoredEvent += HandleBodyPartRestored;
    }
    
    private void RemoveHealthConditionTest(IEffect effect)
    {
        if (RE.FractureType.IsInstanceOfType(effect))
        {
            HandleRemoveFracture(effect);
            return;
        }
        
        if (RE.LightBleedType.IsInstanceOfType(effect))
        {
            HandleRemoveLightBleed(effect);
            return;
        }
        
        if (RE.HeavyBleedType.IsInstanceOfType(effect))
        {
            HandleRemoveHeavyBleed(effect);
            return;
        }
    }

    private void HealthChangeTest(EBodyPart bodyPart, float change, DamageInfo damage)
    {
        if (change.Positive())
        {
            HandleHealthGain(bodyPart, change, damage);
        }
        
        if (change.Negative())
        {
            HandleHealthLoss(bodyPart, change, damage);
        }
    }

    private void HandleBodyPartDestroyed(EBodyPart bodyPart, EDamageType damageType)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.DestroyBodyPart);
        
        foreach (var condition in conditions)
        {
            if (!IsInZone(condition)) continue;
            
            if (BodyPartIncludeCheck(condition, bodyPart))
                IncrementCondition(condition, 1f);
            
            if (!BodyPartExcludeCheck(condition, bodyPart))
                IncrementCondition(condition, 1f);
        }
    }

    private void HandleBodyPartRestored(EBodyPart bodyPart, ValueStruct value)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.RestoreBodyPart);
        
        foreach (var condition in conditions)
        {
            if (!CheckBaseMedicalConditions(condition, bodyPart)) 
                continue;
            
            IncrementCondition(condition, 1f);
        }
    }
    
    private void HandleRemoveFracture(IEffect effect)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.FixFracture);
        
        foreach (var condition in conditions)
        {
            if (!CheckBaseMedicalConditions(condition, effect.BodyPart)) 
                continue;
            
            IncrementCondition(condition, 1f);
        }
    }
    
    private void HandleRemoveLightBleed(IEffect effect)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.FixLightBleed);
        
        foreach (var condition in conditions)
        {
            if (!CheckBaseMedicalConditions(condition, effect.BodyPart)) 
                continue;
            
            IncrementCondition(condition, 1f);
        }
    }
    
    private void HandleRemoveHeavyBleed(IEffect effect)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.FixHeavyBleed);
        
        foreach (var condition in conditions)
        {
            if (!CheckBaseMedicalConditions(condition, effect.BodyPart)) 
                continue;
            
            IncrementCondition(condition, 1f);
        }
    }

    private void HandleHealthLoss(EBodyPart bodyPart, float change, DamageInfo damage)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.HealthLoss);
        
        foreach (var condition in conditions)
        {
            if (!CheckBaseMedicalConditions(condition, bodyPart)) 
                continue;
            
            IncrementCondition(condition, Mathf.Abs(change));
        }
    }

    private void HandleHealthGain(EBodyPart bodyPart, float change, DamageInfo damage)
    {
        var conditions = _questController.GetActiveConditions(EQuestCondition.HealthGain);

        foreach (var condition in conditions)
        {
            if (!CheckBaseMedicalConditions(condition, bodyPart)) 
                continue;
            
            IncrementCondition(condition, change);
        }
    }
}