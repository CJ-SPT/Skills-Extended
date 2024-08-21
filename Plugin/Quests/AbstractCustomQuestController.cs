using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using SkillsExtended.Models;

namespace SkillsExtended.Quests;

public abstract class AbstractCustomQuestController
{
    protected QuestProgressController _questController;
    protected Player _player;

    protected AbstractCustomQuestController(QuestProgressController questProgressController)
    {
        _questController = questProgressController;
        _player = Singleton<GameWorld>.Instance.MainPlayer;
    }

    /// <summary>
    /// Loop over and increment all provided condition pairs
    /// </summary>
    /// <param name="conditions"></param>
    /// /// <param name="value"></param>
    protected void IncrementConditions(List<ConditionPair> conditions, float value = 0f)
    {
        foreach (var condition in conditions)
        {
            _questController.IncrementConditionCounter(condition.Quest, condition.Condition, value);
        }
    }

    /// <summary>
    /// Increment a single provided condition pair
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="value"></param>
    protected void IncrementCondition(ConditionPair condition, float value = 0f)
    {
        _questController.IncrementConditionCounter(condition.Quest, condition.Condition, value);
    }

    /// <summary>
    /// Is the player in an applicable zone for the condition
    /// </summary>
    /// <param name="condition"></param>
    /// <returns>True if we are in a zone, or no zones apply</returns>
    protected bool IsInZone(ConditionPair condition)
    {
        if (condition.CustomCondition.Zones is null) return true;
        
        var condZones = condition.CustomCondition.Zones.ToHashSet();
        var playerZones = _player.TriggerZones;

        foreach (var zone in playerZones)
        {
            if (condZones.Add(zone)) continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks a provided condition for body parts the condition should be triggered for
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="partToCheckFor"></param>
    /// <returns>True if the condition passes</returns>
    protected bool BodyPartIncludeCheck(ConditionPair condition, EBodyPart partToCheckFor)
    {
        // Condition passes because it doesn't exist
        if (condition.CustomCondition.IncludeBodyParts is null) 
            return true;
        
        Plugin.Log.LogWarning(condition.CustomCondition.IncludeBodyParts.Contains(partToCheckFor));
        Plugin.Log.LogWarning(partToCheckFor);
        
        return condition.CustomCondition.IncludeBodyParts.Contains(partToCheckFor);
    }

    /// <summary>
    /// Should this body part be ignored for this condition
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="partToCheckFor"></param>
    /// <returns>True if we should ignore</returns>
    protected bool BodyPartExcludeCheck(ConditionPair condition, EBodyPart partToCheckFor)
    {
        // Condition passes because it doesn't exist
        if (condition.CustomCondition.ExcludeBodyParts is null) 
            return true;
        
        return condition.CustomCondition.ExcludeBodyParts.Contains(partToCheckFor);
    }
}