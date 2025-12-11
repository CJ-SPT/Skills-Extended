using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class StrengthData
{
    [DataMember]
    public bool Enabled { get; set; }

    [DataMember]
    public float ColliderSpeedBuff { get; set; }
}