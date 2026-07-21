using System.Collections.Generic;

namespace SkillsExtended.Config.Skills;

public class LockPickingData
{
    public bool Enabled { get; set; }
    public float PickStrengthBase { get; set; }
    public float PickStrengthPerLevel { get; set; }
    public float SweetSpotRangeBase { get; set; }
    public float SweetSpotRangePerLevel { get; set; }
    public int AttemptsBeforeBreak { get; set; }
    public float InspectLockXpRatio { get; set; }
    public float FailureLockXpRatio { get; set; }
    public Dictionary<string, float> XpTable { get; set; }
    public DoorPickLevels DoorPickLevels { get; set; }
}

// DoorId : level to pick the lock
public class DoorPickLevels
{
    public Dictionary<string, int> Factory { get; set; }
    public Dictionary<string, int> Woods { get; set; }
    public Dictionary<string, int> Customs { get; set; }
    public Dictionary<string, int> Interchange { get; set; }
    public Dictionary<string, int> Reserve { get; set; }
    public Dictionary<string, int> Shoreline { get; set; }
    public Dictionary<string, int> Labs { get; set; }
    public Dictionary<string, int> Lighthouse { get; set; }
    public Dictionary<string, int> Streets { get; set; }
    public Dictionary<string, int> GroundZero { get; set; }
    public Dictionary<string, int> Labyrinth { get; set; }
}