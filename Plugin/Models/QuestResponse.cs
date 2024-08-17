using System.Collections.Generic;

namespace SkillsExtended.Models;

public class QuestResponse
{
    // Quest to search for the condition on
    public string QuestId;
    
    // Quest to search for the condition on
    public string ConditionId;
    
    // Condition type to invoke
    public string ConditionType;

    // Location where the objective must be completed
    public List<string> Locations = [];
}