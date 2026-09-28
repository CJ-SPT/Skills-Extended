using System;
using System.Linq;
using EFT;

namespace SkillsExtended.Skills.Core;

internal static class SkillLists
{
    public static Skill[] AddMissing(Skill[] existing, params Skill[] additions) =>
        existing.Concat(additions).GroupBy(skill => skill.Id).Select(group => group.First()).ToArray();

    public static Skill[] Insert(Skill[] existing, int index, params Skill[] additions)
    {
        var inserted = AddMissing(Array.Empty<Skill>(), additions);
        var ids = inserted.Select(skill => skill.Id).ToHashSet();
        var remaining = AddMissing(existing).Where(skill => !ids.Contains(skill.Id)).ToArray();
        index = Math.Min(index, remaining.Length);
        return remaining.Take(index).Concat(inserted).Concat(remaining.Skip(index)).ToArray();
    }
}
