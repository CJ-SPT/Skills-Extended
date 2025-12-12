using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class EnduranceData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public bool AlwaysLevelEndurance { get; set; }
    
    [DataMember]
    public float BuffEnduranceIncMax { get; set; }

    [DataMember] 
    public float BuffEnduranceIncElite { get; set; }
    
    [DataMember]
    public float HandsPerLevel { get; set; }
    
    [DataMember]
    public float HandsElite { get; set; }
    
    [DataMember]
    public float BuffJumpCostRedMax { get; set; }
    
    [DataMember]
    public float BuffBreathTimeIncMax { get; set; }
    
    [DataMember]
    public float BuffRestorationMax { get; set; }

    [DataMember] 
    public float BuffRestorationElite { get; set; }
}