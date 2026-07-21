using EFT;
using EFT.HealthSystem;
using SkillsExtended.Extensions;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class VitalitySkill(SkillManager skillManager)
    : Skill(
        skillManager,
        ESkillId.Vitality,
        ESkillClass.Physical,
        GetActions(skillManager),
        GetBuffs(skillManager)
    )
{
    private static SkillManager.SkillAction[] GetActions(SkillManager skillManager)
    {
        return
        [
            skillManager.DamageTakenAction.Factor(skillManager.Settings.Vitality.DamageTakenAction),
            skillManager
                .HealthNegativeEffect.Where(effect => effect is IBleeding)
                .Factor(skillManager.Settings.Vitality.HealthNegativeEffect),
        ];
    }

    private static SkillManager.Buff[] GetBuffs(SkillManager skillManager)
    {
        var data = SkillsExtendedPlugin.SkillData.Vitality;

        return
        [
            skillManager.VitalityBuffBleedChanceRed.PerLevel(
                data.BuffBleedChanceRedPerLevel.NormalizeToPercentage()
            ),
            skillManager.VitalityBuffSurviobilityInc.PerLevel(
                data.BuffSurviobilityIncPerLevel.NormalizeToPercentage()
            ),
            skillManager.VitalityBuffRegeneration,
            skillManager.VitalityBuffBleedStop,
        ];
    }
}
