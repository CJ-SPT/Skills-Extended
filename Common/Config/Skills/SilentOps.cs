using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class SilentOpsData
{
    [DataMember]
    public bool Enabled { get; set; }

    [DataMember]
    public float XpPerAction { get; set; }

    [DataMember]
    public float MeleeSpeedInc { get; set; }

    [DataMember]
    public float VolumeReduction { get; set; }

    [DataMember] 
    public float SilencerPriceReduction { get; set; }
}