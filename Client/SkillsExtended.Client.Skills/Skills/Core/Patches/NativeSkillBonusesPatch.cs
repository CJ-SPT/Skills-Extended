using System.Linq;
using System.Reflection;
using EFT;
using SkillsExtended.Config;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Core.Patches;

public class NativeSkillBonusesPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => typeof(Skill).GetConstructor(
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
        [typeof(SkillManager), typeof(ESkillId), typeof(ESkillClass),
            typeof(SkillManager.SkillAction[]), typeof(SkillManager.Buff[])], null);

    [PatchPrefix]
    public static void Prefix(ESkillId id, SkillManager.Buff[] buffs)
    {
        var data = SkillsExtendedPlugin.SkillData?.NativeSkills;
        if (data == null || !LevelingSkillCatalog.ById.TryGetValue((int)id, out var skill)
            || !NativeSkillCatalog.ByKey.TryGetValue(skill.Key, out var definition)
            || !data.Overrides.TryGetValue(skill.Key, out var values)) return;
        foreach (var field in definition.Fields.Where(f => f.BuffId != 0))
        {
            if (!values.TryGetValue(field.Key, out var value) || !field.IsValid(value)) continue;
            var buff = buffs.OfType<SkillManager.FloatBuff>().FirstOrDefault(b => (int)b.Id == field.BuffId);
            if (buff == null) continue;
            switch (field.Rule)
            {
                case "PerLevel": buff.PerLevel(value * field.Scale); break;
                case "Max": buff.Max(value * field.Scale); break;
                case "Elite": buff.Elite(value * field.Scale); break;
            }
        }
    }
}
