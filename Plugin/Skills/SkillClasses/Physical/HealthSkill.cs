using System;
using EFT;
using SkillsExtended.Extensions;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class HealthSkill(SkillManager skillManager)
    : SkillClass(skillManager, ESkillId.Health, ESkillClass.Physical, GetActions(skillManager), GetBuffs(skillManager))
{
    private static readonly ESkillId[] SkillsRelatedToHealth = [
        ESkillId.Endurance,
        ESkillId.Strength,
        ESkillId.Vitality
    ];
    
    private static SkillManager.SkillActionClass[] GetActions(SkillManager skillManager)
    {
        return [
            skillManager.SkillProgress.Where(IsSkillRelatedToHealth)
                .Factor(skillManager.Settings.Health.SkillProgress),
        ];
    }
    
    private static SkillManager.SkillBuffAbstractClass[] GetBuffs(SkillManager skillManager)
    {
        var data = SkillsExtendedPlugin.SkillData.Health;
        
        return [
            skillManager.HealthBreakChanceRed
                .PerLevel(data.BreakChanceRedPerLevel.NormalizeToPercentage()),
            
            skillManager.HealthEnergy
                .PerLevel(data.EnergyPerLevel.NormalizeToPercentage()),
            
            skillManager.HealthHydration
                .PerLevel(data.HydrationPerLevel.NormalizeToPercentage()),
            
            skillManager.HealthEliteAbsorbDamage
        ];
    }

    private static bool IsSkillRelatedToHealth(SkillClass skill)
    {
        return Array.IndexOf(SkillsRelatedToHealth, skill.Id) >= 0;
    }
}