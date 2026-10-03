using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using SkillsExtended.Config;
using SkillsExtended.Core;
using SkillsExtended.Core.Editing;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;

public static class LevelingChecks
{
    private static readonly List<double> Awards = [];
    public static async Task Run(ConfigSnapshot snapshot, Action<bool, string> check)
    {
        var legacy = JsonSerializer.Deserialize<SkillsConfig>("{}")!.LevelingSpeed;
        check(legacy.ForSkill(0) == 1 && legacy.WeaponMasteryMultiplier == 1, "Old skills configuration defaults to neutral leveling");
        check(LevelingSkillCatalog.All.Count == 67 && !LevelingSkillCatalog.ById.ContainsKey(62)
            && !LevelingSkillCatalog.ById.ContainsKey(63), "Catalog includes every player skill and both custom skills, excluding bots");
        check(LevelingSkillCatalog.All.Where(skill => skill.Id < 200).All(skill =>
                Enum.TryParse<SkillTypes>(skill.Key, out var id) && (int)id == skill.Id),
            "Shared player skill IDs agree with the actual SPT server enum");
        var rules = new LevelingSpeedData { GlobalMultiplier = 2, WeaponMasteryMultiplier = 4 };
        rules.SkillMultipliers["Endurance"] = 1.5f;
        check(rules.ForSkill(0) == 3 && rules.ForSkill(1) == 2 && rules.ForSkill(62) == 1, "Effective multipliers compose and excluded skills retain native speed");
        foreach (var value in new[] { -1f, 100.01f, float.NaN, float.PositiveInfinity })
        {
            var invalid = ConfigStore.Clone(snapshot.Skills);
            invalid.LevelingSpeed.GlobalMultiplier = value;
            check(ConfigRules.Validate(invalid).Count > 0, "Reject invalid global multiplier " + value);
            invalid.LevelingSpeed.GlobalMultiplier = 1;
            invalid.LevelingSpeed.WeaponMasteryMultiplier = value;
            check(ConfigRules.Validate(invalid).Count > 0, "Reject invalid mastery multiplier " + value);
            invalid.LevelingSpeed.WeaponMasteryMultiplier = 1;
            invalid.LevelingSpeed.SkillMultipliers["Endurance"] = value;
            check(ConfigRules.Validate(invalid).Count > 0, "Reject invalid individual multiplier " + value);
        }
        foreach (var value in new[] { 0f, .0025f, 100f })
        {
            var valid = ConfigStore.Clone(snapshot.Skills);
            valid.LevelingSpeed.GlobalMultiplier = value;
            valid.LevelingSpeed.WeaponMasteryMultiplier = value;
            valid.LevelingSpeed.SkillMultipliers["Endurance"] = value;
            check(ConfigRules.Validate(valid).Count == 0, "Accept supported multiplier " + value);
        }
        var malformed = ConfigStore.Clone(snapshot.Skills);
        malformed.LevelingSpeed.SkillMultipliers["BotReload"] = 1;
        check(ConfigRules.Validate(malformed).Count > 0, "Reject internal bot configuration keys");
        malformed.LevelingSpeed.SkillMultipliers = null!;
        check(ConfigRules.Validate(malformed).Count > 0, "Reject null multiplier dictionaries");
        malformed.LevelingSpeed = null!;
        check(ConfigRules.Validate(malformed).Count > 0, "Reject explicitly null leveling sections");
        var editor = new EditorSession(snapshot);
        editor.Skills.LevelingSpeed = rules;
        check(editor.ChangesFor("LevelingSpeed") == 3 && editor.ChangeCount == 3 && snapshot.Skills.LevelingSpeed.ForSkill(0) == 1,
            "Leveling settings participate in detached drafts and pending counts");
        editor.InputErrors["LevelingSpeed.SkillMultipliers.Endurance"] = "invalid";
        check(editor.ChangeCount == 3, "Invalid input on an edited multiplier is counted once");
        editor.Reset(snapshot);
        check(!editor.Dirty && editor.Skills.LevelingSpeed.ForSkill(0) == 1, "Discard restores leveling defaults and clears errors");

        var directory = Path.Combine(Path.GetTempPath(), "skills-leveling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "SkillsConfig.json"), ConfigStore.Serialize(snapshot.Skills));
            await File.WriteAllTextAsync(Path.Combine(directory, "ServerConfig.json"), ConfigStore.Serialize(snapshot.Server));
            var store = new ConfigStore(directory, new ConfigFiles());
            var baseline = await store.ReadSnapshotAsync();
            var draft = ConfigStore.Clone(baseline);
            draft.Skills.LevelingSpeed = rules;
            var result = await store.SaveAsync(draft.Skills, draft.Server, draft.Revision, _ => { });
            var saved = await store.ReadSnapshotAsync();
            check(result.Success && saved.Skills.LevelingSpeed.ForSkill(0) == 3
                && ConfigStore.Serialize(saved.Skills.LockPicking.XpTable) == ConfigStore.Serialize(snapshot.Skills.LockPicking.XpTable),
                "Leveling save persists multipliers and preserves existing XP tables");
            check((await store.SaveAsync(baseline.Skills, baseline.Server, baseline.Revision, _ => { })).Status == EditStatus.Conflict,
                "Leveling edits use existing revision conflict protection");
        }
        finally
        {
            if (!Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(directory).StartsWith("skills-leveling-", StringComparison.Ordinal))
                throw new InvalidOperationException("Unexpected fixture cleanup path.");
            Directory.Delete(directory, true);
        }

        // Patch real SPT award overloads, skipping their bodies to avoid any profile or server dependencies.
        var controller = new ConfigController(DispatchProxy.Create<ISptLogger<ConfigController>, SilentLogger>(), []);
        var production = new GameplayLevelingPatches(controller);
        var testHarmony = new Harmony("skills-extended.tests.leveling");
        try
        {
            await production.OnLoadAsync(CancellationToken.None);
            controller.SkillsConfig.LevelingSpeed = rules;
            check(GameplayLevelingPatches.GameplayMethods().All(method => method != null
                    && Harmony.GetPatchInfo(method).Prefixes.Any(p => p.owner == "com.cj.skillsextended.leveling.gameplay")),
                "Every targeted real SPT gameplay award method has its scope hook");
            testHarmony.Patch(GameplayLevelingPatches.AwardMethod(),
                prefix: new HarmonyMethod(typeof(LevelingChecks), nameof(Capture)) { priority = Priority.Last });
            testHarmony.Patch(AccessTools.Method(typeof(LevelingChecks), nameof(Gameplay)),
                prefix: new HarmonyMethod(typeof(GameplayLevelingPatches), nameof(GameplayLevelingPatches.Enter)),
                finalizer: new HarmonyMethod(typeof(GameplayLevelingPatches), nameof(GameplayLevelingPatches.Exit)));
            var helper = (ProfileHelper)RuntimeHelpers.GetUninitializedObject(typeof(ProfileHelper));
            Award(helper);
            check(Awards.Last() == 2, "Direct reward call outside gameplay context remains exact");
            Gameplay(helper, true, false);
            check(Awards.TakeLast(3).SequenceEqual(new[] { 6d, 6d, 6d }) && !GameplayLevelingContext.Active,
                "Four-argument awards scale once through terminal overload, including nested gameplay contexts");
            try { Gameplay(helper, false, true); } catch (InvalidOperationException) { }
            Award(helper);
            check(Awards.Last() == 2 && !GameplayLevelingContext.Active, "Harmony finalizers clear gameplay context after exceptions");
            rules.GlobalMultiplier = 0;
            Gameplay(helper, false, false);
            check(Awards.Last() == 0, "Zero disables server gameplay skill gains");
            rules.GlobalMultiplier = 2;
        }
        finally
        {
            testHarmony.UnpatchSelf();
            Harmony.UnpatchID("com.cj.skillsextended.leveling.gameplay");
        }
        GameplayLevelingContext.Enter(out var previous);
        try
        {
            var independent = await Task.Run(() =>
            {
                GameplayLevelingContext.Exit(0);
                return GameplayLevelingContext.Scale(rules, 0, 2);
            });
            check(independent == 2 && GameplayLevelingContext.Scale(rules, 0, 2) == 6,
                "Separate execution flows cannot alter another player's award context");
        }
        finally { GameplayLevelingContext.Exit(previous); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Gameplay(ProfileHelper helper, bool nested, bool fail)
    {
        Award(helper);
        if (nested) { Gameplay(helper, false, false); Award(helper); }
        if (fail) throw new InvalidOperationException("Fixture failure");
    }
    public static bool Capture(double pointsToAddToSkill) { Awards.Add(pointsToAddToSkill); return false; }
    private static void Award(ProfileHelper helper) => AccessTools.Method(typeof(ProfileHelper), "AddSkillPointsToPlayer",
        new[] { typeof(PmcData), typeof(SkillTypes), typeof(double), typeof(bool) })
        .Invoke(helper, new object?[] { null, SkillTypes.Endurance, 2d, false });
}
