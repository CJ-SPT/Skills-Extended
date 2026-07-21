using EFT;
using SkillsExtended.Config.Skills;
using SkillsExtended.Extensions;
using UnityEngine;

namespace SkillsExtended.Skills.SkillClasses.Physical;

public class StrengthSkill(SkillManager skillManager)
    : Skill(
        skillManager,
        ESkillId.Strength,
        ESkillClass.Physical,
        GetActions(skillManager),
        GetBuffs(skillManager)
    )
{
    private static readonly StrengthData Data = SkillsExtendedPlugin.SkillData.Strength;

    private static SkillManager.SkillAction[] GetActions(SkillManager skillManager)
    {
        var actions = new StrengthActions(skillManager);

        return
        [
            skillManager.SprintAction.Factor(actions.SprintAction),
            skillManager.MovementAction.Factor(actions.MovementAction),
            skillManager.PushUp.Factor(actions.PushUpAction),
            skillManager.FistfightAction.Factor(0.2f),
            skillManager.ThrowAction.Factor(0.2f),
        ];
    }

    private static SkillManager.Buff[] GetBuffs(SkillManager skillManager)
    {
        return
        [
            skillManager.StrengthBuffJumpHeightInc.Max(
                Data.BuffJumpHeightIncMax.NormalizeToPercentage()
            ),
            skillManager.StrengthBuffLiftWeightInc.Max(
                Data.BuffLiftWeightIncMax.NormalizeToPercentage()
            ),
            skillManager.StrengthBuffMeleePowerInc.Max(
                Data.BuffMeleePowerIncMax.NormalizeToPercentage()
            ),
            skillManager.StrengthBuffSprintSpeedInc.Max(
                Data.BuffSprintSpeedIncMax.NormalizeToPercentage()
            ),
            skillManager.StrengthBuffThrowDistanceInc.Max(
                Data.BuffThrowDistanceIncMax.NormalizeToPercentage()
            ),
            skillManager.StrengthBuffAimFatigue.Max(Data.BuffAimFatigueMax.NormalizeToPercentage()),
            skillManager.StrengthBuffElite,
            skillManager
                .StrengthBuffMeleeCrits.PerLevel(
                    Data.BuffMeleeCritsPerLevel.NormalizeToPercentage()
                )
                .Elite(Data.BuffMeleeCritsEliteBonus.NormalizeToPercentage()),
            // Added by SE below this point
            skillManager.SkillsExtendedManager.StrengthBushSpeedIncBuff.Max(
                Data.ColliderSpeedBuffMax.NormalizeToPercentage()
            ),
            skillManager.SkillsExtendedManager.StrengthBushSpeedIncBuffElite,
        ];
    }

    private class StrengthActions(SkillManager skillManager)
    {
        public float SprintAction(SkillManager.MovementParams movement)
        {
            if (movement.Overweight <= 0f && !Data.AlwaysLevelStrength)
            {
                return 0f;
            }

            return Mathf.Lerp(
                skillManager.Settings.Strength.SprintActionMin,
                skillManager.Settings.Strength.SprintActionMax,
                movement.Overweight
            );
        }

        public float MovementAction(SkillManager.MovementParams movement)
        {
            if (movement.Overweight <= 0f && !Data.AlwaysLevelStrength)
            {
                return 0f;
            }

            return Mathf.Lerp(
                skillManager.Settings.Strength.MovementActionMin,
                skillManager.Settings.Strength.MovementActionMax,
                movement.Overweight
            );
        }

        public float PushUpAction(SkillManager.MovementParams movement)
        {
            if (movement.Overweight <= 0f && !Data.AlwaysLevelStrength)
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
