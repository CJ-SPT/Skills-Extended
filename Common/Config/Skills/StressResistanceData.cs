using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class StressResistanceData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public float StressPainPerLevel { get; set; }
    
    [DataMember]
    public float StressTremorPerLevel { get; set; }
}