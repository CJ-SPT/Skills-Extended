using EFT;
using SkillsExtended.Skills.Core;

internal static class SkillListChecks
{
    public static void Run(Action<bool, string> check)
    {
        var native = Enumerable.Range(0, 43).Select(id => new Skill { Id = (ESkillId)id }).ToArray();
        var silentOps = native[35];
        var additions = Enumerable.Range(100, 6).Select(id => new Skill { Id = (ESkillId)id })
            .Append(silentOps).ToArray();
        var skills = SkillLists.AddMissing(native, additions);
        check(skills.Length == 49, "native SilentOps is not appended twice");
        check(skills.ToDictionary(s => s.Id).Count == 49, "raid skills accept a unique-key dictionary");
        check(native.All(s => ReferenceEquals(skills.Single(x => x.Id == s.Id), s)), "native skill objects retain their progress and subscriptions");
        check(SkillLists.AddMissing(skills, additions).SequenceEqual(skills), "repeated registration preserves references and order");

        var display = SkillLists.Insert(native, 12, additions);
        check(display.Length == 49 && display.Select(s => s.Id).Distinct().Count() == 49, "display moves existing skills instead of duplicating them");
        check(display.Skip(12).Take(7).SequenceEqual(additions), "extended display group keeps its requested order");
        check(SkillLists.Insert(display, 12, additions).SequenceEqual(display), "display insertion is repeatable");
        check(SkillLists.Insert([], 12, additions).SequenceEqual(additions), "short display lists are supported");

        var hacking = new Skill { Id = (ESkillId)200 };
        skills = SkillLists.AddMissing(skills, hacking);
        check(SkillLists.AddMissing(skills, hacking).SequenceEqual(skills), "Hacking registration does not duplicate ID 200");
        var duplicate = new Skill { Id = silentOps.Id };
        var repaired = SkillLists.AddMissing([silentOps, duplicate], silentOps);
        check(repaired.Length == 1 && ReferenceEquals(repaired[0], silentOps), "existing duplicate IDs retain the original skill object");
    }
}
