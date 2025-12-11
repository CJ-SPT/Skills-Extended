using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class ShadowConnectionsData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public float XpPerAction { get; set; }
    
    [DataMember]
    public float ScavCooldownTimeDec { get; set; }
    
    [DataMember]
    public float CultistCircleReturnTimeReduction { get; set; }
    
    [DataMember]
    public float ScavGenerateAsCultistChance { get; set; }
}