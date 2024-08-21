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
    [CanBeNull] public List<string> Locations;
    [CanBeNull] public List<string> AllowedItems;
    [CanBeNull] public List<string> ForbiddenItems;
    [CanBeNull] public List<string> Zones;
    [CanBeNull] public List<EBodyPart> IncludeBodyParts;
    [CanBeNull] public List<EBodyPart> ExcludeBodyParts;
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
    HealthGain,
    DestroyBodyPart,
    RestoreBodyPart
}