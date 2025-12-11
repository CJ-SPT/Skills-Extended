using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class FieldMedicineData
{
    [DataMember]
    public bool Enabled { get; set; }

    [DataMember]
    public float XpPerAction { get; set; }

    [DataMember]
    public float SkillBonus { get; set; }
    
    [DataMember]
    public float DurationBonus { get; set; }

    [DataMember]
    public float PositiveEffectChanceBonus { get; set; }
}