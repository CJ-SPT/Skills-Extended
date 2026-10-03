using EFT;
using SkillsExtended;
using SkillsExtended.Config;
using SkillsExtended.Skills.Core.Patches;

public static class NativeBonusChecks
{
    public static void Run(Action<bool, string> check)
    {
        var config = SkillsExtendedPlugin.SkillData;
        var original = config.NativeSkills;
        try
        {
            config.NativeSkills = new();
            foreach (var skill in NativeSkillCatalog.All)
            {
                foreach (var field in skill.Fields.Where(f => f.BuffId != 0))
                {
                    var buff = new SkillManager.FloatBuff { Id = (EBuffId)field.BuffId,
                        PerLevelValue = .123f, MaxValue = .456f, EliteValue = .789f };
                    var other = new SkillManager.FloatBuff { Id = (EBuffId)999, PerLevelValue = .111f };
                    SkillManager.Buff[] buffs = [buff, other];
                    NativeSkillBonusesPatch.Prefix((ESkillId)skill.Id, buffs);
                    check(buff.PerLevelValue == .123f && buff.MaxValue == .456f && buff.EliteValue == .789f,
                        skill.Name + " absent override preserves " + field.Label);
                    config.NativeSkills.Overrides[skill.Key] = new() { [field.Key] = 25 };
                    NativeSkillBonusesPatch.Prefix((ESkillId)skill.Id, buffs);
                    var actual = field.Rule switch { "PerLevel" => buff.PerLevelValue, "Max" => buff.MaxValue, _ => buff.EliteValue };
                    check(actual == .25f && other.PerLevelValue == .111f, skill.Name + " applies only the selected bonus rule");
                    config.NativeSkills.Overrides[skill.Key][field.Key] = 0;
                    NativeSkillBonusesPatch.Prefix((ESkillId)skill.Id, buffs);
                    actual = field.Rule switch { "PerLevel" => buff.PerLevelValue, "Max" => buff.MaxValue, _ => buff.EliteValue };
                    check(actual == 0, skill.Name + " supports zero bonus overrides");
                    config.NativeSkills.Overrides.Clear();
                }
            }
        }
        finally { config.NativeSkills = original; }
    }
}
