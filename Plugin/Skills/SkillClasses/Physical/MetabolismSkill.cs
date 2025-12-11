using EFT;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class MetabolismSkill(SkillManager skillManager)
    : SkillClass(skillManager, ESkillId.Metabolism, ESkillClass.Physical, GetActions(skillManager), GetBuffs(skillManager))
{
    private static SkillManager.SkillActionClass[] GetActions(SkillManager skillManager)
    {
        return [
            skillManager.HydrationChanged
                .Where(GreaterThanZero)
                .Factor(skillManager.Settings.Metabolism.HydrationRecoveryRate),
            
            skillManager.EnergyChanged
                .Where(GreaterThanZero)
                .Factor(skillManager.Settings.Metabolism.EnergyRecoveryRate),
        ];
    }
    
    private static SkillManager.SkillBuffAbstractClass[] GetBuffs(SkillManager skillManager)
    {
        return [
            skillManager.MetabolismEliteBuffNoDyhydration,
            skillManager.MetabolismRatioPlus.PerLevel(0.01f),
            skillManager.MetabolismMiscDebuffTime.PerLevel(0.01f)
        ];
    }

    private static bool GreaterThanZero(float x)
    {
        return x > 0;
    }
}