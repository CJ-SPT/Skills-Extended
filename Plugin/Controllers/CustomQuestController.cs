using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Quests;
using HarmonyLib;
using JetBrains.Annotations;
using SkillsExtended.LockPicking;
using SkillsExtended.Models;
using UnityEngine;

namespace SkillsExtended.Controllers;

public class CustomQuestController : MonoBehaviour
{
    private static string UnderlyingQuestControllerClassName;
    private Player _player;
    private AbstractQuestControllerClass _questController;
    
    private List<QuestResponse> CustomConditions => Plugin.Quests;

    private readonly List<string> _questsWithCustomConditions = [];
    
    void Awake()
    {
        _player = Singleton<GameWorld>.Instance.MainPlayer;
        _questController = _player?.AbstractQuestControllerClass;
        
        if (UnderlyingQuestControllerClassName == null)
        {
            var type = AccessTools.GetTypesFromAssembly(typeof(AbstractGame).Assembly)
                .SingleOrDefault(t => t.GetEvent(
                    "OnConditionQuestTimeExpired", 
                    BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) != null);
                
            if (type == null)
            {
                Plugin.Log.LogError("Failed to locate a specific quest controller type");
                return;
            }

            UnderlyingQuestControllerClassName = type.Name.Split('`')[0];
            Plugin.Log.LogDebug($"Resolved {nameof(UnderlyingQuestControllerClassName)} to be {UnderlyingQuestControllerClassName}");
        }

        foreach (var condition in CustomConditions)
        {
            _questsWithCustomConditions.Add(condition.QuestId);
        }
        
        InspectLockActionHandler.OnLockInspected += InspectLockHandler;
        LockPickActionHandler.OnLockPicked += PickLockHandler;
        LockPickActionHandler.OnLockPickFailed += PickLockFailedHandler;
    }

    private void OnDestroy()
    {
        InspectLockActionHandler.OnLockInspected -= InspectLockHandler;
        LockPickActionHandler.OnLockPicked -= PickLockHandler;
        LockPickActionHandler.OnLockPickFailed -= PickLockFailedHandler;
    }
    
    private void InspectLockHandler(object sender, EventArgs e)
    {
        CheckActiveConditionForEvent("InspectLock");
    }
    
    private void PickLockHandler(object sender, EventArgs e)
    {
        CheckActiveConditionForEvent("PickLock");
    }
    
    private void PickLockFailedHandler(object sender, EventArgs e)
    {
        CheckActiveConditionForEvent("PickLockFailed");
    }
    
    private void BreakLockHandler(object sender, EventArgs e)
    {
        CheckActiveConditionForEvent("BreakLock");
    }
    
    private void HackDoorHandler(object sender, EventArgs e)
    {
        CheckActiveConditionForEvent("HackDoor");
    }
    
    private void HackDoorFailedHandler(object sender, EventArgs e)
    {
        CheckActiveConditionForEvent("HackDoorFailed");
    }

    /// <summary>
    /// Check for and increment an active condition by type on a map
    /// </summary>
    /// <param name="conditionType"></param>
    private void CheckActiveConditionForEvent(string conditionType)
    {
        var quests = GetActiveQuests();
        
        // No quests, return
        if (!quests.Any()) return;

        foreach (var quest in quests)
        {
            var questRespCond = GetCustomConditionsById(quest.Id, conditionType);

            if (questRespCond is null)
            {
                Plugin.Log.LogDebug($"Skipping quest {quest.Id.LocalizedName()} : No {conditionType} condition");
                continue;
            }
            
            var customCondition = questRespCond
                .FirstOrDefault(cond => cond.Locations
                    .Any(loc => loc == _player.Location || loc == "any"));

            if (customCondition is null)
            {
                Plugin.Log.LogWarning($"Custom Condition is null for `{quest.Id.LocalizedName()}`");
                continue;
            }
            
            var condition = GetBsgConditionById(quest.Id, customCondition.ConditionId);

            if (condition is null)
            {
                Plugin.Log.LogWarning($"Condition is null for `{quest.Id.LocalizedName()}`");
                continue;
            }
            
            IncrementConditionCounter(quest, condition);
            Plugin.Log.LogDebug($"Incremented condition {conditionType} on quest {quest.Id.LocalizedName()}");
        }
    }
    
    private void HandleIncrementByLocation(QuestClass quest, IEnumerable<QuestResponse> questResponses)
    {
        
    }
    
    /// <summary>
    /// Increments a provided condition counter
    /// </summary>
    /// <param name="quest"></param>
    /// <param name="condition"></param>
    private void IncrementConditionCounter(QuestClass quest, Condition condition)
    {
        // This line will increment the condition counter by 1
        var currentVal = quest.ProgressCheckers[condition].CurrentValue;
        quest.ProgressCheckers[condition].SetCurrentValueGetter(_ => currentVal + 1);
                    
        // We call 'SetConditionCurrentValue' to trigger all the code needed to make the condition completion appear visually in-game
        var conditionController = AccessTools.Field(
                _questController.GetType(), 
                $"{UnderlyingQuestControllerClassName.ToLowerInvariant()}_0")
            .GetValue(_questController);
                    
        AccessTools.DeclaredMethod(conditionController.GetType().BaseType, "SetConditionCurrentValue")
            .Invoke(conditionController, new object[] { quest, EQuestStatus.AvailableForFinish, condition, currentVal + 1, true });
    }

    /// <summary>
    /// Gets all active quests that are started, and we have custom conditions for
    /// </summary>
    /// <returns></returns>
    private IEnumerable<QuestClass> GetActiveQuests()
    {
        var activeQuests = _questController.Quests
            .Where(q => q.QuestStatus == EQuestStatus.Started)
            .Where(q => _questsWithCustomConditions.Contains(q.Id));
        
        Plugin.Log.LogDebug($"Custom conditions active: {activeQuests.Any()}");
        
        return activeQuests;
    }
    
    /// <summary>
    /// Gets BSG conditions by questId and conditionId
    /// </summary>
    /// <param name="questId"></param>
    /// <param name="conditionId"></param>
    /// <returns></returns>
    [CanBeNull]
    private Condition GetBsgConditionById(string questId, string conditionId)
    {
        var quest = GetQuestById(questId);
        
        if (quest is null) return null;
        if (quest.QuestStatus != EQuestStatus.Started) return null;

        foreach (var gclass in quest.Conditions)
        {
            var conditions = gclass.Value.IEnumerable_0;
            var condition = conditions.FirstOrDefault(cond => cond.id == conditionId);

            if (condition is not null)
            {
                return condition;
            }
        }

        Plugin.Log.LogWarning($"Could not find condition `{conditionId}` on quest `{questId.LocalizedName()}`");
        return null;
    }

    /// <summary>
    /// Gets custom conditions by quest id and condition type
    /// </summary>
    /// <param name="questId"></param>
    /// <param name="conditionType"></param>
    /// <returns></returns>
    [CanBeNull]
    private IEnumerable<QuestResponse> GetCustomConditionsById(string questId, string conditionType)
    {
        var customConditions = CustomConditions
            .Where(cond => cond.QuestId == questId)
            .Where(cond => cond.ConditionType == conditionType);
            
        return customConditions;
    }
    
    [CanBeNull]
    private QuestClass GetQuestById(string questId)
    {
        return _questController?.Quests?
            .FirstOrDefault(x => x is not null && x.Id == questId);
    }
}