using System;
using EFT;

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
        return [
            skillManager.HealthBreakChanceRed.PerLevel(0.012f),
            skillManager.HealthEnergy.PerLevel(0.006f),
            skillManager.HealthHydration.PerLevel(0.006f),
            skillManager.HealthEliteAbsorbDamage
        ];
    }

    private static bool IsSkillRelatedToHealth(SkillClass skill)
    {
        return Array.IndexOf(SkillsRelatedToHealth, skill.Id) >= 0;
    }
}