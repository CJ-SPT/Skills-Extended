using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class FirstAidData
{
    [DataMember]
    public bool Enabled { get; set; }

    [DataMember]
    public float XpPerAction { get; set; }

    [DataMember]
    public float MedkitUsageReduction { get; set; }
    
    [DataMember]
    public float ItemSpeedBonus { get; set; }
}