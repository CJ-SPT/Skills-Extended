using System;
using System.Runtime.CompilerServices;
using EFT;
using HarmonyLib;
using SkillsExtended.Signals;

namespace SkillsExtended.Skills.Signals;

public sealed class SignalsSkill
{
    private static readonly ConditionalWeakTable<SkillManager, SignalsSkill> Instances = new();
    public Skill Skill { get; }
    public SkillManager.SkillAction Action { get; } = new();

    private SignalsSkill(SkillManager manager)
    {
        Config.Skills.SignalsIntelligenceData Rules() =>
            SignalsRuntime.Instance?.Manifest?.Config
            ?? SkillsExtendedPlugin.SkillData?.SignalsIntelligence
            ?? new Config.Skills.SignalsIntelligenceData();
        Skill = new Skill(
            manager,
            (ESkillId)SignalsIds.Skill,
            ESkillClass.Practical,
            new[] { Action.Factor(1f) },
            new SkillManager.Buff[]
            {
                new SkillManager.FloatBuff { Id = (EBuffId)SignalsIds.Accuracy }.Custom(level =>
                    1 - SignalsModel.Uncertainty(Rules(), level) / Rules().BaseUncertainty
                ),
                new SkillManager.FloatBuff { Id = (EBuffId)SignalsIds.Tuning }.Custom(level =>
                    SignalsModel.Tolerance(Rules(), level) / Rules().TuningTolerance - 1
                ),
                new SkillManager.BooleanBuff
                {
                    Id = (EBuffId)SignalsIds.Memory,
                    BuffType = SkillManager.EBuffType.Elite,
                },
            }
        );
        ApplyRaidRules(Rules().Enabled);
    }

    public void ApplyRaidRules(bool enabled) =>
        AccessTools.Field(typeof(Skill), "Locked").SetValue(Skill, !enabled);

    public static SignalsSkill Get(SkillManager manager) =>
        Instances.GetValue(manager, m => new SignalsSkill(m));

    public static void Attach(SkillManager manager, ref Skill[] skills, ref Skill[] display)
    {
        var skill = Get(manager).Skill;
        skills = Core.SkillLists.AddMissing(skills, skill);
        display = Core.SkillLists.AddMissing(display, skill);
    }
}
