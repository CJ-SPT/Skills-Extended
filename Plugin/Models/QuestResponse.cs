using System.Collections.Generic;
using AnimationEventSystem;
using JetBrains.Annotations;

namespace SkillsExtended.Models;

public struct QuestResponse
{
    // Quest to search for the condition on
    public string QuestId;

    public List<CustomCondition> Conditions;
}

public struct CustomCondition
{
    // Quest to search for the condition on
    public string ConditionId;
    public EQuestCondition ConditionType;
    [CanBeNull] public HashSet<string> Locations;
    [CanBeNull] public HashSet<string> AllowedItems;
    [CanBeNull] public HashSet<string> ForbiddenItems;
    [CanBeNull] public HashSet<string> Zones;
    [CanBeNull] public HashSet<EBodyPart> IncludeBodyParts;
    [CanBeNull] public HashSet<EBodyPart> ExcludeBodyParts;
}

public enum EQuestCondition
{
    InspectLock,
    PickLock,
    PickLockFailed,
    RepairLock,
    RepairLockFailed,
    BreakLock,
    HackDoor,
    HackDoorFailed,
    FixLightBleed,
    FixHeavyBleed,
    FixFracture,
    HealthLoss,
    HealthGain
}