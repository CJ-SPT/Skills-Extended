using EFT;
using UnityEngine;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class StrengthSkill(SkillManager skillManager)
    : SkillClass(skillManager, ESkillId.Strength, ESkillClass.Physical, GetActions(skillManager), GetBuffs(skillManager))
{
    private static SkillManager.SkillActionClass[] GetActions(SkillManager skillManager)
    {
        var actions = new StrengthActions(skillManager);
        
        return [
            skillManager.SprintAction.Factor(actions.SprintAction),
            skillManager.MovementAction.Factor(actions.MovementAction),
            skillManager.PushUp.Factor(actions.PushUpAction),
            skillManager.FistfightAction.Factor(0.2f),
            skillManager.ThrowAction.Factor(0.2f)
        ];
    }
    
    private static SkillManager.SkillBuffAbstractClass[] GetBuffs(SkillManager skillManager)
    {
        return [
            skillManager.StrengthBuffJumpHeightInc.Max(0.2f),
            skillManager.StrengthBuffLiftWeightInc.Max(0.3f),
            skillManager.StrengthBuffMeleePowerInc.Max(0.3f),
            skillManager.StrengthBuffSprintSpeedInc.Max(0.2f),
            skillManager.StrengthBuffThrowDistanceInc.Max(0.2f),
            skillManager.StrengthBuffAimFatigue.Max(0.2f),
            skillManager.StrengthBuffElite,
            skillManager.StrengthBuffMeleeCrits.PerLevel(0f).Elite(0.5f)
        ];
    }

    private class StrengthActions(SkillManager skillManager)
    {
        public float SprintAction(MovementParams movement)
        {
            if (movement.Overweight <= 0f)
            {
                return 0f;
            }

            return Mathf.Lerp(
                skillManager.Settings.Strength.SprintActionMin, 
                skillManager.Settings.Strength.SprintActionMax, 
                movement.Overweight
                );
        }
        
        public float MovementAction(MovementParams movement)
        {
            if (movement.Overweight <= 0f)
            {
                return 0f;
            }

            return Mathf.Lerp(
                skillManager.Settings.Strength.MovementActionMin, 
                skillManager.Settings.Strength.MovementActionMax, 
                movement.Overweight
            );
        }

        public float PushUpAction(MovementParams movement)
        {
            if (movement.Overweight <= 0f)
            {
                return 0f;
            }
            
            return Mathf.Lerp(
                skillManager.Settings.Strength.PushUpMin, 
                skillManager.Settings.Strength.PushUpMax, 
                movement.Overweight
            );
        }
    }
}