using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SkillsExtended.Config.Skills;

[DataContract]
public class WeaponSkillData
{
    [DataMember]
    public bool Enabled { get; set; }

    [DataMember]
    public float XpPerAction { get; set; }
    
    [DataMember]
    public bool SkillShareEnabled { get; set; }

    [DataMember]
    public float SkillShareXpRatio { get; set; }

    [DataMember]
    public float ErgoMod { get; set; }

    [DataMember]
    public float RecoilReduction { get; set; }
    
    [DataMember]
    public HashSet<string> Weapons { get; set; }
}