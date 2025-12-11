using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class ProneMovementData
{
    [DataMember]
    public bool Enabled { get; set; }

    [DataMember]
    public float XpPerAction { get; set; }

    [DataMember]
    public float MovementSpeedInc { get; set; }

    [DataMember]
    public float MovementVolumeDec { get; set; }
}