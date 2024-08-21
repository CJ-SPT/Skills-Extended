using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using HarmonyLib;
using JetBrains.Annotations;
using SkillsExtended.Models;
using UnityEngine;

namespace SkillsExtended.Quests;

public class QuestProgressController : MonoBehaviour
{
    private static string UnderlyingQuestControllerClassName;
    private Player _player;
    private AbstractQuestControllerClass _questController;
    
    private Dictionary<string, QuestResponse> CustomConditions => Plugin.Quests;

    private readonly List<string> _questsWithCustomConditions = [];

    private static MedicalQuestController _medController;
    private static LPQuestController _lpController;
    
    void Awake()
    {
        _player = Singleton<GameWorld>.Instance.MainPlayer;
        _questController = _player?.AbstractQuestControllerClass;
        
        _medController = new MedicalQuestController(this);
        _lpController = new LPQuestController(this);
        
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
            _questsWithCustomConditions.Add(condition.Key);
        }
    }

    private void OnDestroy()
    {
        _lpController.Dispose();
        _medController.Dispose();
    }

    private void Update()
    {
    }

    /// <summary>
    /// Get active conditions for a specific type
    /// </summary>
    /// <param name="conditionType"></param>
    public List<ConditionPair> GetActiveConditions(EQuestCondition conditionType)
    {
        var quests = GetActiveQuests();
        
        // No quests, return empty
        if (!quests.Any()) return [];

        List<ConditionPair> pairs = [];
        
        foreach (var quest in quests)
        {
            var questRespCond = GetCustomConditionsByCondition(quest.Id, conditionType);

            if (questRespCond is null)
            {
                Plugin.Log.LogDebug($"Skipping quest {quest.Id.LocalizedName()} : No {conditionType} condition");
                continue;
            }
            
            // Grab all custom conditions for our location
            var customConditions = questRespCond
                .Where(cond => cond.Locations is not null && cond.Locations
                    .Any(loc => loc == _player.Location || loc == "any"));
            
            if (!customConditions.Any())
            {
                Plugin.Log.LogWarning($"Custom Condition is null for `{quest.Id.LocalizedName()}`");
                continue;
            }

            // Make sure if there are conditions both specific to this map and across any map that we increment all of them
            foreach (var condition in customConditions)
            {
                var bsgCondition = GetBsgConditionById(quest.Id, condition.ConditionId);
                
                if (bsgCondition is null) continue;

                ConditionPair pair = new()
                {
                    Quest = quest,
                    Condition = bsgCondition,
                    CustomCondition = condition
                };
                
                pairs.Add(pair);
            }
        }

        return pairs;
    }
    
    /// <summary>
    /// Increments a provided condition counter
    ///
    /// Credits: Terkoiz for this code
    /// </summary>
    /// <param name="quest"></param>
    /// <param name="condition"></param>
    /// /// <param name="value"></param>
    public void IncrementConditionCounter(QuestClass quest, Condition condition, float value)
    {
        // This line will increment the condition counter by 1
        var currentVal = quest.ProgressCheckers[condition].CurrentValue;
        quest.ProgressCheckers[condition].SetCurrentValueGetter(_ => currentVal + value);
                    
        // We call 'SetConditionCurrentValue' to trigger all the code needed to make the condition completion appear visually in-game
        var conditionController = AccessTools.Field(
                _questController.GetType(), 
                $"{UnderlyingQuestControllerClassName.ToLowerInvariant()}_0")
            .GetValue(_questController);
                    
        AccessTools.DeclaredMethod(conditionController.GetType().BaseType, "SetConditionCurrentValue")
            .Invoke(conditionController, new object[] { quest, EQuestStatus.AvailableForFinish, condition, currentVal + value, true });
    }
    
    /// <summary>
    /// Gets all active quests that are started,
    /// and we have custom conditions for
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
    private IEnumerable<CustomCondition> GetCustomConditionsByCondition(string questId, EQuestCondition conditionType)
    {
        if (!CustomConditions.TryGetValue(questId, out var quest))
        {
            return null;
        }
        
        var customConditions = quest.Conditions
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

public class ConditionPair
{
    public QuestClass Quest;
    public Condition Condition;
    public CustomCondition CustomCondition;
}