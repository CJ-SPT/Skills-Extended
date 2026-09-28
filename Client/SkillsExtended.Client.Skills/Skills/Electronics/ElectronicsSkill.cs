using System;
using System.Runtime.CompilerServices;
using EFT;
using HarmonyLib;
using SkillsExtended.Electronics;

namespace SkillsExtended.Skills.Electronics;

public sealed class ElectronicsSkill
{
    private static readonly ConditionalWeakTable<SkillManager, ElectronicsSkill> Registrations =
        new();
    public Skill Skill { get; }
    public SkillManager.SkillAction Action { get; } = new();

    private ElectronicsSkill(SkillManager manager)
    {
        var config =
            SkillsExtendedPlugin.SkillData?.Electronics ?? new Config.Skills.ElectronicsData();
        Skill = new Skill(
            manager,
            (ESkillId)ElectronicsIds.Skill,
            ESkillClass.Practical,
            config.Enabled ? new[] { Action.Factor(1f) } : Array.Empty<SkillManager.SkillAction>(),
            config.Enabled
                ? new SkillManager.Buff[]
                {
                    new SkillManager.FloatBuff
                    {
                        Id = (EBuffId)ElectronicsIds.CoherenceBuff,
                    }.Custom(level =>
                        (float)(level * config.CoherencePerLevel) / config.BaseCoherence
                    ),
                    new SkillManager.FloatBuff { Id = (EBuffId)ElectronicsIds.StrengthBuff }.Custom(
                        level => (float)(level / config.LevelsPerStrength) / config.BaseStrength
                    ),
                    new SkillManager.BooleanBuff
                    {
                        Id = (EBuffId)ElectronicsIds.SlotsBuff,
                        BuffType = SkillManager.EBuffType.Elite,
                    },
                }
                : Array.Empty<SkillManager.Buff>()
        );
    }

    public static ElectronicsSkill Get(SkillManager manager) =>
        Registrations.GetValue(manager, m => new ElectronicsSkill(m));

    public static void Attach(SkillManager manager, ref Skill[] skills, ref Skill[] display)
    {
        var skill = Get(manager).Skill;
        Array.Resize(ref skills, skills.Length + 1);
        skills[skills.Length - 1] = skill;
        Array.Resize(ref display, display.Length + 1);
        display[display.Length - 1] = skill;
    }
}
