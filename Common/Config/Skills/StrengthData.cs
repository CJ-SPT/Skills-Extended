using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class StrengthData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public bool AlwaysLevelStrength { get; set; }
    
    [DataMember]
    public float BuffJumpHeightIncMax { get; set; }
    
    [DataMember]
    public float BuffLiftWeightIncMax { get; set; }
    
    [DataMember]
    public float BuffMeleePowerIncMax { get; set; }
    
    [DataMember]
    public float BuffSprintSpeedIncMax { get; set; }
    
    [DataMember]
    public float BuffThrowDistanceIncMax { get; set; }
    
    [DataMember]
    public float BuffAimFatigueMax { get; set; }
    
    [DataMember]
    public float BuffMeleeCritsPerLevel { get; set; }
    
    [DataMember]
    public float BuffMeleeCritsEliteBonus { get; set; }
    
    [DataMember]
    public float ColliderSpeedBuffMax { get; set; }
}