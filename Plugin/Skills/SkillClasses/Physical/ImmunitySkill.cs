using EFT;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class ImmunitySkill(SkillManager skillManager)
    : SkillClass(skillManager, ESkillId.Immunity, ESkillClass.Physical, GetActions(skillManager), GetBuffs(skillManager))
{
    private static SkillManager.SkillActionClass[] GetActions(SkillManager skillManager)
    {
        return [
            skillManager.HealthNegativeEffect
                .Where(IsIntoxication)
                .Factor(skillManager.Settings.Immunity.HealthNegativeEffect),
            
            skillManager.StimulatorNegativeBuff
                .Factor(skillManager.Settings.Immunity.StimulatorNegativeBuff),
        ];
    }
    
    private static SkillManager.SkillBuffAbstractClass[] GetBuffs(SkillManager skillManager)
    {
        return [
            skillManager.ImmunityMiscEffects.PerLevel(0.01f),
            skillManager.ImmunityPoisonBuff.PerLevel(0.01f),
            skillManager.ImmunityPainKiller.PerLevel(0.006f),
            skillManager.ImmunityAvoidPoisonChance.Default(0f).Elite(0.9f),
            skillManager.ImmunityAvoidMiscEffectsChance.Default(0f).Elite(0.9f)
        ];
    }

    private static bool IsIntoxication(IEffect effect)
    {
        return effect is IIntoxication;
    }
}