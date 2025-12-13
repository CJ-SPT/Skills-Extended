using System.Collections.Generic;

namespace SkillsExtended.Config.Skills;

public class WeaponSkillData
{
    public bool Enabled { get; set; }
    public float XpPerAction { get; set; }
    public bool SkillShareEnabled { get; set; }
    public float SkillShareXpRatio { get; set; }
    public float ErgoMod { get; set; }
    public float RecoilReduction { get; set; }
    public HashSet<string> Weapons { get; set; }
}