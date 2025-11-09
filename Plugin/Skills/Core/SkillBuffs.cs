using EFT;
using UnityEngine;

namespace SkillsExtended.Skills.Core;

public class PositiveSkillBuffInt : SkillManager.SkillBuffClass
{
    public void Apply(ref int val)
    {
        val = Mathf.CeilToInt(val * (1 + Value));
    }
    
    public int Apply(int val)
    {
        return Mathf.CeilToInt(val * (1 + Value));
    }
}

public class NegativeSkillBuffInt : SkillManager.SkillBuffClass
{
    public void Apply(ref int val)
    {
        val = Mathf.CeilToInt(val * (1 - Value));
    }
    
    public int Apply(int val)
    {
        return Mathf.CeilToInt(val * (1 - Value));
    }
}