using EFT;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class VitalitySkill(SkillManager skillManager)
    : SkillClass(skillManager, ESkillId.Vitality, ESkillClass.Physical, GetActions(skillManager), GetBuffs(skillManager))
{
    private static SkillManager.SkillActionClass[] GetActions(SkillManager skillManager)
    {
        return [
            skillManager.DamageTakenAction
                .Factor(skillManager.Settings.Vitality.DamageTakenAction),
            
            skillManager.HealthNegativeEffect.Where(effect => effect is GInterface341)
                .Factor(skillManager.Settings.Vitality.HealthNegativeEffect)
        ];
    }
    
    private static SkillManager.SkillBuffAbstractClass[] GetBuffs(SkillManager skillManager)
    {
        return [
            skillManager.VitalityBuffBleedChanceRed.PerLevel(0.012f),
            skillManager.VitalityBuffSurviobilityInc.PerLevel(0.004f),
            skillManager.VitalityBuffRegeneration,
            skillManager.VitalityBuffBleedStop
        ];
    }
}