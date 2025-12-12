using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class VitalityData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public float BuffBleedChanceRedPerLevel { get; set; }
    
    [DataMember]
    public float BuffSurviobilityIncPerLevel { get; set; }
}