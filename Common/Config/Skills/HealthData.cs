using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class HealthData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public float BreakChanceRedPerLevel { get; set; }
    
    [DataMember]
    public float EnergyPerLevel { get; set; }
    
    [DataMember]
    public float HydrationPerLevel { get; set; }
}