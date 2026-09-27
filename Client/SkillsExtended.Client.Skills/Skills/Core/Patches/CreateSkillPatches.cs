using System.Reflection;
using EFT;
using SkillsExtended.Skills.SkillClasses.Physical;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Core.Patches;

public class CreatePhysicalSkillsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(Skill).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof(SkillManager), typeof(ESkillId), typeof(ESkillClass),
                typeof(SkillManager.SkillAction[]), typeof(SkillManager.Buff[])],
            null
        );

    [PatchPrefix]
    public static void Prefix(
        SkillManager skillManager,
        ESkillId id,
        ref SkillManager.SkillAction[] actions,
        ref SkillManager.Buff[] buffs
    )
    {
        var data = SkillsExtendedPlugin.SkillData;
        if (data == null)
        {
            return;
        }

        // Leave the game's arrays intact for disabled skills. Configure before the
        // constructor subscribes actions, rather than constructing each skill twice.
        switch (id)
        {
            case ESkillId.Endurance when data.Endurance.Enabled:
                actions = EnduranceSkill.GetActions(skillManager);
                buffs = EnduranceSkill.GetBuffs(skillManager);
                break;
            case ESkillId.Strength when data.Strength.Enabled:
                actions = StrengthSkill.GetActions(skillManager);
                buffs = StrengthSkill.GetBuffs(skillManager);
                break;
            case ESkillId.Vitality when data.Vitality.Enabled:
                actions = VitalitySkill.GetActions(skillManager);
                buffs = VitalitySkill.GetBuffs(skillManager);
                break;
            case ESkillId.Health when data.Health.Enabled:
                actions = HealthSkill.GetActions(skillManager);
                buffs = HealthSkill.GetBuffs(skillManager);
                break;
            case ESkillId.Metabolism when data.Metabolism.Enabled:
                actions = MetabolismSkill.GetActions(skillManager);
                buffs = MetabolismSkill.GetBuffs(skillManager);
                break;
            case ESkillId.StressResistance when data.StressResistance.Enabled:
                actions = StressResistanceSkill.GetActions(skillManager);
                buffs = StressResistanceSkill.GetBuffs(skillManager);
                break;
            case ESkillId.Immunity when data.Immunity.Enabled:
                actions = ImmunitySkill.GetActions(skillManager);
                buffs = ImmunitySkill.GetBuffs(skillManager);
                break;
        }
    }
}
