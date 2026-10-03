using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SkillsExtended.Config;
using SkillsExtended.Core;
using SkillsExtended.Core.Editing;
using SkillsExtended.Web.Pages;
using SPTarkov.Server.Core.Models.Spt.Tables;

public static class NativeSkillChecks
{
    public static async Task Run(ConfigSnapshot snapshot, Action<bool, string> check)
    {
        check(NativeSkillCatalog.All.Count == 27 && SkillCatalog.Navigation.Count == 46,
            "All 27 remaining implemented skills have pages alongside the original 19");
        var routes = typeof(Home).Assembly.GetTypes().SelectMany(t => t.GetCustomAttributes<RouteAttribute>()).Select(r => r.Template).ToArray();
        check(SkillCatalog.Navigation.All(s => routes.Count(r => r == s.Url) == 1), "Every skill navigation entry has one real route");
        check(SkillCatalog.Navigation.All(s => File.Exists(Path.Combine(AppContext.BaseDirectory, "icons", s.Icon + ".png"))),
            "Every skill has its own exported icon file");
        check(!NativeSkillCatalog.ByKey.ContainsKey("Memory") && !NativeSkillCatalog.ByKey.ContainsKey("NightOps")
            && !NativeSkillCatalog.ByKey.ContainsKey("HMG"), "Dormant and placeholder skills have no bonus pages");
        check(JsonSerializer.Deserialize<SkillsConfig>("{}")!.NativeSkills.Overrides.Count == 0,
            "Legacy configs preserve all native bonuses without migration");
        check(NativeSkillCatalog.All.SelectMany(s => s.Fields).All(f => NativeSkillCatalog.Defaults.ContainsKey(f.Key)),
            "Every native bonus field has a captured game default");
        var session = new EditorSession(snapshot);
        session.Skills.NativeSkills.Overrides["Surgery"] = new() { ["Buff.98.Max"] = 25, ["Buff.98.Elite"] = 50 };
        session.Skills.LevelingSpeed.SkillMultipliers["Surgery"] = 2;
        check(session.ChangesFor("Surgery") == 2 && session.PageChangesFor("Surgery") == 3 && session.ChangeCount == 3,
            "Bonus and leveling drafts track pending edits without double counting");
        check(snapshot.Skills.NativeSkills.Overrides.Count == 0, "Native drafts do not mutate saved configuration");
        foreach (var bad in new[] { -1f, 101f, float.NaN, float.PositiveInfinity })
        {
            session.Skills.NativeSkills.Overrides["Surgery"]["Buff.98.Max"] = bad;
            check(ConfigRules.Validate(session.Skills).Count > 0, "Reject invalid native bonus " + bad);
        }
        session.Reset(snapshot);
        check(!session.Dirty && session.Skills.NativeSkills.Overrides.Count == 0, "Discard restores game defaults and leveling speed");
        var settings = JsonSerializer.Deserialize<SkillsSettings>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "NativeGlobals.json")))!;
        settings.Crafting.CraftTimeReductionPerLevel = 1.25;
        var original = JsonSerializer.Serialize(settings);
        var state = new NativeSkillOverrideState(settings);
        check(state.Defaults["Crafting.CraftTimeReductionPerLevel"] == 1.25f
            && state.Defaults["Charisma.BonusSettings.LevelBonusSettings.InsuranceDiscount"] == .1f,
            "Displayed defaults reflect loaded modded globals and convert native fractions to percentage points");
        check(NativeSkillCatalog.All.SelectMany(s => s.Fields).Where(f => f.BuffId == 0).All(f =>
            NativeSkillOverrideState.Resolve(settings, f.Key).Property.CanWrite), "Every server bonus resolves against the real SPT model");
        var changes = new NativeSkillData();
        changes.Overrides["Charisma"] = new() { ["Charisma.BonusSettings.LevelBonusSettings.InsuranceDiscount"] = 2,
            ["Charisma.BonusSettings.EliteBonusSettings.RepeatableQuestExtraCount"] = 3 };
        changes.Overrides["Crafting"] = new() { ["Crafting.CraftTimeReductionPerLevel"] = 0 };
        state.Apply(changes);
        var insurance = NativeSkillOverrideState.Resolve(settings, "Charisma.BonusSettings.LevelBonusSettings.InsuranceDiscount");
        check(Math.Abs(Convert.ToDouble(insurance.Property.GetValue(insurance.Owner)) - .02) < .00001,
            "Percentage point bonuses reach server globals as fractions");
        var count = NativeSkillOverrideState.Resolve(settings, "Charisma.BonusSettings.EliteBonusSettings.RepeatableQuestExtraCount");
        check(Convert.ToInt32(count.Property.GetValue(count.Owner)) == 3 && settings.Crafting.CraftTimeReductionPerLevel == 0,
            "Integer elite bonuses and zero overrides reach server globals");
        state.Apply(changes);
        check(Math.Abs(Convert.ToDouble(insurance.Property.GetValue(insurance.Owner)) - .02) < .00001,
            "Repeated saves do not compound bonus overrides");
        state.Apply(new());
        check(JsonSerializer.Serialize(settings) == original, "Clearing overrides restores native server values exactly");
        var directory = Path.Combine(Path.GetTempPath(), "skills-native-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "SkillsConfig.json"), ConfigStore.Serialize(snapshot.Skills));
            await File.WriteAllTextAsync(Path.Combine(directory, "ServerConfig.json"), ConfigStore.Serialize(snapshot.Server));
            var store = new ConfigStore(directory, new ConfigFiles());
            var baseline = await store.ReadSnapshotAsync();
            baseline.Skills.NativeSkills = changes;
            baseline.Skills.LevelingSpeed.SkillMultipliers["Surgery"] = 2;
            var saved = await store.SaveAsync(baseline.Skills, baseline.Server, baseline.Revision, _ => { });
            var reloaded = await store.ReadSnapshotAsync();
            check(saved.Success && reloaded.Skills.NativeSkills.Overrides["Charisma"].Count == 2
                && reloaded.Skills.LevelingSpeed.Individual("Surgery") == 2, "Native bonus and leveling settings survive saving and reloading");
        }
        finally { Directory.Delete(directory, true); }
    }
}
