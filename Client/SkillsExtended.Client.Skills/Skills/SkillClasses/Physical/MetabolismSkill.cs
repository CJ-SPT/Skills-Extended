using EFT;
using SkillsExtended.Extensions;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class MetabolismSkill(SkillManager skillManager)
    : Skill(
        skillManager,
        ESkillId.Metabolism,
        ESkillClass.Physical,
        GetActions(skillManager),
        GetBuffs(skillManager)
    )
{
    private static SkillManager.SkillAction[] GetActions(SkillManager skillManager)
    {
        return
        [
            skillManager
                .HydrationChanged.Where(GreaterThanZero)
                .Factor(skillManager.Settings.Metabolism.HydrationRecoveryRate),
            skillManager
                .EnergyChanged.Where(GreaterThanZero)
                .Factor(skillManager.Settings.Metabolism.EnergyRecoveryRate),
        ];
    }

    private static SkillManager.Buff[] GetBuffs(SkillManager skillManager)
    {
        var data = SkillsExtendedPlugin.SkillData.Metabolism;

        return
        [
            skillManager.MetabolismEliteBuffNoDyhydration,
            skillManager.MetabolismRatioPlus.PerLevel(
                data.RatioPlusPerLevel.NormalizeToPercentage()
            ),
            skillManager.MetabolismMiscDebuffTime.PerLevel(
                data.MiscDebuffTimePerLevel.NormalizeToPercentage()
            ),
        ];
    }

    private static bool GreaterThanZero(float x)
    {
        return x > 0;
    }
}
