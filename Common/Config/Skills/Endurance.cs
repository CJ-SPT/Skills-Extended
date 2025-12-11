using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class EnduranceData
{
    [DataMember]
    public bool Enabled { get; set; }
}