using EFT;
using SkillsExtended.Extensions;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class ImmunitySkill(SkillManager skillManager)
    : SkillClass(skillManager, ESkillId.Immunity, ESkillClass.Physical, GetActions(skillManager), GetBuffs(skillManager))
{
    private static SkillManager.SkillActionClass[] GetActions(SkillManager skillManager)
    {
        return [
            skillManager.HealthNegativeEffect
                .Where(effect  => effect is IIntoxication)
                .Factor(skillManager.Settings.Immunity.HealthNegativeEffect),
            
            skillManager.StimulatorNegativeBuff
                .Factor(skillManager.Settings.Immunity.StimulatorNegativeBuff),
        ];
    }
    
    private static SkillManager.SkillBuffAbstractClass[] GetBuffs(SkillManager skillManager)
    {
        var data = SkillsExtendedPlugin.SkillData.Immunity;
        
        return [
            skillManager.ImmunityMiscEffects
                .PerLevel(data.MiscEffectsPerLevel.NormalizeToPercentage()),
            
            skillManager.ImmunityPoisonBuff
                .PerLevel(data.PoisonBuffPerLevel.NormalizeToPercentage()),
            
            skillManager.ImmunityPainKiller
                .PerLevel(data.PainKillerPerLevel.NormalizeToPercentage()),
            
            skillManager.ImmunityAvoidPoisonChance
                .Default(0f)
                .Elite(data.AvoidPoisonChanceElite.NormalizeToPercentage()),
            
            skillManager.ImmunityAvoidMiscEffectsChance
                .Default(0f)
                .Elite(data.AvoidMiscEffectsChanceElite.NormalizeToPercentage())
        ];
    }
}