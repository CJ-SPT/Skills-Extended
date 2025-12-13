using EFT;
using SkillsExtended.Extensions;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class StressResistanceSkill(SkillManager skillManager)
    : SkillClass(skillManager, ESkillId.StressResistance, ESkillClass.Physical, GetActions(skillManager), GetBuffs(skillManager))
{
    private static SkillManager.SkillActionClass[] GetActions(SkillManager skillManager)
    {
        return [
            skillManager.HealthNegativeEffect
                .Where(IsPain)
                .Factor(skillManager.Settings.StressResistance.HealthNegativeEffect),
            
            skillManager.LowHPDuration
                .Factor(skillManager.Settings.StressResistance.LowHPDuration),
        ];
    }
    
    private static SkillManager.SkillBuffAbstractClass[] GetBuffs(SkillManager skillManager)
    {
        var data = SkillsExtendedPlugin.SkillData.StressResistance;
        
        return [
            skillManager.StressPain
                .PerLevel(data.StressPainPerLevel.NormalizeToPercentage()),
            
            skillManager.StressTremor
                .PerLevel(data.StressTremorPerLevel.NormalizeToPercentage()),
            
            skillManager.StressBerserk
        ];
    }

    private static bool IsPain(IEffect effect)
    {
        return effect is IPain;
    }
}