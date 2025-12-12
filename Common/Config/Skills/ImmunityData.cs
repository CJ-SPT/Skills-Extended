using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class ImmunityData
{
    [DataMember]
    public bool Enabled { get; set; }
    
    [DataMember]
    public float MiscEffectsPerLevel { get; set; }
    
    [DataMember]
    public float PoisonBuffPerLevel { get; set; }
    
    [DataMember]
    public float PainKillerPerLevel { get; set; }
    
    [DataMember]
    public float AvoidPoisonChanceElite { get; set; }
    
    [DataMember]
    public float AvoidMiscEffectsChanceElite { get; set; }
}