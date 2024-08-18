using System.Collections.Generic;
using EFT;

namespace SkillsExtended.Models;

public class BuffsModel
{
    public List<SkillBuffModel> SkillBuffs;
}

public class SkillBuffModel
{
    public string ItemId;
    public string Name;
    public int DurationInSeconds;
    public int Strength;
    public ESkillId SkillType;
}