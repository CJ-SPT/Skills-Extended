using EFT;
using SkillsExtended;
using SkillsExtended.Config;
using SkillsExtended.Skills.Core.Patches;

public static class LevelingClientChecks
{
    public static void Run(Action<bool, string> check)
    {
        var saved = SkillsExtendedPlugin.SkillData;
        try
        {
            SkillsExtendedPlugin.SkillData = new SkillsConfig();
            var skill = new Skill { Id = ESkillId.Endurance, SkillManager = new() };
            float xp = 2.5f;
            SkillClassOnTriggerPatch.PatchPrefix(skill, ref xp);
            check(xp == 2.5f, "Neutral leveling leaves incoming action XP unchanged");
            var data = SkillsExtendedPlugin.SkillData.LevelingSpeed;
            data.GlobalMultiplier = 2;
            data.SkillMultipliers["Endurance"] = 1.5f;
            data.WeaponMasteryMultiplier = 4;
            SkillClassOnTriggerPatch.PatchPrefix(skill, ref xp);
            check(xp == 7.5f, "Action prefix combines global and individual speed before native calculations");
            MasteringOnTriggerPatch.Prefix(skill, ref xp);
            check(xp == 7.5f, "Base trigger does not multiply ordinary skills twice");
            MasteringOnTriggerPatch.Prefix(new Mastering(), ref xp);
            check(xp == 30, "Mastery uses only its independent multiplier");
            foreach (var id in new[] { 34, 43, 54, 57, 200, 201 })
            {
                skill.Id = (ESkillId)id;
                data.SkillMultipliers[LevelingSkillCatalog.ById[id].Key] = .25f;
                xp = 10;
                SkillClassOnTriggerPatch.PatchPrefix(skill, ref xp);
                check(xp == 5, "Shared action prefix covers extended skill " + id);
            }
            data.GlobalMultiplier = 0;
            xp = 10;
            SkillClassOnTriggerPatch.PatchPrefix(skill, ref xp);
            check(xp == 0, "Zero global multiplier disables action gains");
            data.WeaponMasteryMultiplier = 0;
            xp = 10;
            MasteringOnTriggerPatch.Prefix(new Mastering(), ref xp);
            check(xp == 0, "Zero mastery multiplier disables mastery gains");
            SkillsExtendedPlugin.SkillData = null;
            xp = 10;
            SkillClassOnTriggerPatch.PatchPrefix(skill, ref xp);
            check(xp == 10, "Skills constructed before config loading retain native action XP");
        }
        finally { SkillsExtendedPlugin.SkillData = saved; }
    }
}
