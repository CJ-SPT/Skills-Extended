using EFT;
using SkillsExtended.Config.Skills;
using SkillsExtended.Extensions;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class EnduranceSkill(SkillManager skillManager)
    : SkillClass(skillManager, ESkillId.Endurance, ESkillClass.Physical, GetActions(skillManager), GetBuffs(skillManager))
{
    private static readonly EnduranceData Data = SkillsExtendedPlugin.SkillData.Endurance;
    
    private static SkillManager.SkillActionClass[] GetActions(SkillManager skillManager)
    {
        var actions = new EnduranceActions(skillManager);
        
        return [
            skillManager.SprintAction.Factor(actions.SprintAction),
            skillManager.MovementAction.Factor(actions.MovementAction)
        ];
    }
    
    private static SkillManager.SkillBuffAbstractClass[] GetBuffs(SkillManager skillManager)
    {
        return [
            skillManager.EnduranceBuffEnduranceInc
                .Max(Data.BuffBreathTimeIncMax.NormalizeToPercentage())
                .Elite(Data.BuffEnduranceIncElite.NormalizeToPercentage()),
            
            skillManager.EnduranceHands
                .PerLevel(Data.HandsPerLevel.NormalizeToPercentage())
                .Elite(Data.HandsElite.NormalizeToPercentage()),
            
            skillManager.EnduranceBuffJumpCostRed
                .Max(Data.BuffJumpCostRedMax.NormalizeToPercentage()),
            
            skillManager.EnduranceBuffBreathTimeInc
                .Max(Data.BuffBreathTimeIncMax.NormalizeToPercentage()),
            
            skillManager.EnduranceBuffRestoration
                .Max(Data.BuffRestorationMax.NormalizeToPercentage())
                .Elite(Data.BuffRestorationElite.NormalizeToPercentage()),
            
            skillManager.EnduranceBreathElite
                .PerLevel(0f)
                .Elite(1f)
        ];
    }

    private class EnduranceActions(SkillManager skillManager)
    {
        public float SprintAction(MovementParams movement)
        {
            if (movement.Overweight <= 0f || Data.AlwaysLevelEndurance)
            {
                return skillManager.Settings.Endurance.SprintAction * 
                       (1f + skillManager.Settings.Endurance.GainPerFatigueStack * movement.Fatigue);
            }

            return 0f;
        }
        
        public float MovementAction(MovementParams movement)
        {
            if (movement.Overweight <= 0f || Data.AlwaysLevelEndurance)
            {
                return skillManager.Settings.Endurance.MovementAction * 
                       (1f + skillManager.Settings.Endurance.GainPerFatigueStack * movement.Fatigue);
            }

            return 0f;
        }
    }
}