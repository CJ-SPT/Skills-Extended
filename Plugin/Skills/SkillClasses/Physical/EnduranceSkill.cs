using EFT;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class EnduranceSkill(SkillManager skillManager)
    : SkillClass(skillManager, ESkillId.Endurance, ESkillClass.Physical, GetActions(skillManager), GetBuffs(skillManager))
{
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
            skillManager.EnduranceBuffEnduranceInc.Max(0.5f).Elite(0.7f),
            skillManager.EnduranceHands.PerLevel(0f).Elite(0.5f),
            skillManager.EnduranceBuffJumpCostRed.Max(0.3f),
            skillManager.EnduranceBuffBreathTimeInc.Max(1f),
            skillManager.EnduranceBuffRestoration.Max(0.5f).Elite(0.75f),
            skillManager.EnduranceBreathElite.PerLevel(0f).Elite(1f)
        ];
    }

    private class EnduranceActions(SkillManager skillManager)
    {
        public float SprintAction(MovementParams movement)
        {
            if (movement.Overweight <= 0f)
            {
                return skillManager.Settings.Endurance.SprintAction * 
                       (1f + skillManager.Settings.Endurance.GainPerFatigueStack * movement.Fatigue);
            }

            return 0f;
        }
        
        public float MovementAction(MovementParams movement)
        {
            if (movement.Overweight <= 0f)
            {
                return skillManager.Settings.Endurance.MovementAction * 
                       (1f + skillManager.Settings.Endurance.GainPerFatigueStack * movement.Fatigue);
            }

            return 0f;
        }
    }
}