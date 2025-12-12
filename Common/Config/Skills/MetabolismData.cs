using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class MetabolismData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public float RatioPlusPerLevel { get; set; }
    
    [DataMember]
    public float MiscDebuffTimePerLevel { get; set; }
}