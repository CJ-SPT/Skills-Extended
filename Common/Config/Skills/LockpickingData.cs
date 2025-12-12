using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class LockPickingData
{
    [DataMember]
    public bool Enabled { get; set; }

    [DataMember]
    public float PickStrengthBase { get; set; }

    [DataMember]
    public float PickStrengthPerLevel { get; set; }

    [DataMember]
    public float SweetSpotRangeBase { get; set; }

    [DataMember]
    public float SweetSpotRangePerLevel { get; set; }

    [DataMember]
    public int AttemptsBeforeBreak { get; set; }

    [DataMember]
    public float InspectLockXpRatio { get; set; }

    [DataMember]
    public float FailureLockXpRatio { get; set; }

    [DataMember]
    public Dictionary<string, float> XpTable { get; set; }

    [DataMember]
    public DoorPickLevels DoorPickLevels { get; set; }
}

// DoorId : level to pick the lock
[DataContract]
public class DoorPickLevels
{
    [DataMember]
    public Dictionary<string, int> Factory { get; set; }
    
    [DataMember]
    public Dictionary<string, int> Woods { get; set; }
    
    [DataMember]
    public Dictionary<string, int> Customs { get; set; }
    
    [DataMember]
    public Dictionary<string, int> Interchange { get; set; }
    
    [DataMember]
    public Dictionary<string, int> Reserve { get; set; }
    
    [DataMember]
    public Dictionary<string, int> Shoreline { get; set; }
    
    [DataMember]
    public Dictionary<string, int> Labs { get; set; }
    
    [DataMember]
    public Dictionary<string, int> Lighthouse { get; set; }
    
    [DataMember]
    public Dictionary<string, int> Streets { get; set; }
    
    [DataMember]
    public Dictionary<string, int> GroundZero { get; set; }
    
    [DataMember]
    public Dictionary<string, int> Labyrinth { get; set; }
}