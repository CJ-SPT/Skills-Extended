using System;
using System.Runtime.CompilerServices;
using EFT;
using HarmonyLib;
using SkillsExtended.Hacking;

namespace SkillsExtended.Skills.Hacking;

public sealed class HackingSkill
{
    private static readonly ConditionalWeakTable<SkillManager, HackingSkill> Registrations =
        new();
    public Skill Skill { get; }
    public SkillManager.SkillAction Action { get; } = new();

    private HackingSkill(SkillManager manager)
    {
        var config =
            SkillsExtendedPlugin.SkillData?.Hacking ?? new Config.Skills.HackingData();
        Skill = new Skill(
            manager,
            (ESkillId)HackingIds.Skill,
            ESkillClass.Practical,
            config.Enabled ? new[] { Action.Factor(1f) } : Array.Empty<SkillManager.SkillAction>(),
            config.Enabled
                ? new SkillManager.Buff[]
                {
                    new SkillManager.FloatBuff
                    {
                        Id = (EBuffId)HackingIds.CoherenceBuff,
                    }.Custom(level =>
                        (float)(level * config.CoherencePerLevel) / config.BaseCoherence
                    ),
                    new SkillManager.FloatBuff { Id = (EBuffId)HackingIds.StrengthBuff }.Custom(
                        level => (float)(level / config.LevelsPerStrength) / config.BaseStrength
                    ),
                    new SkillManager.BooleanBuff
                    {
                        Id = (EBuffId)HackingIds.SlotsBuff,
                        BuffType = SkillManager.EBuffType.Elite,
                    },
                }
                : Array.Empty<SkillManager.Buff>()
        );
    }

    public static HackingSkill Get(SkillManager manager) =>
        Registrations.GetValue(manager, m => new HackingSkill(m));

    public static void Attach(SkillManager manager, ref Skill[] skills, ref Skill[] display)
    {
        var skill = Get(manager).Skill;
        skills = Core.SkillLists.AddMissing(skills, skill);
        display = Core.SkillLists.AddMissing(display, skill);
    }
}
