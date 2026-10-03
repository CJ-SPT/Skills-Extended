using System.Reflection;
using HarmonyLib;
using SkillsExtended.Config;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;

namespace SkillsExtended.Core;

// Context follows this execution flow, rather than leaking between concurrent players.
public static class GameplayLevelingContext
{
    private static readonly AsyncLocal<int> Depth = new();
    public static bool Active => Depth.Value > 0;
    public static void Enter(out int previous) { previous = Depth.Value; Depth.Value = previous + 1; }
    public static void Exit(int previous) => Depth.Value = previous;
    public static double Scale(LevelingSpeedData settings, int id, double points) =>
        Active ? points * settings.ForSkill(id) : points;
}

[Injectable(InjectionType.Singleton, OnLoadOrder.Preload + 1)]
public class GameplayLevelingPatches(ConfigController config) : IOnLoad
{
    private static ConfigController _config = null!;

    public static IReadOnlyList<MethodBase> GameplayMethods() => new MethodBase[]
    {
        AccessTools.Method(typeof(HideoutController), "UpgradeComplete"),
        AccessTools.Method(typeof(HideoutController), "ScavCaseProductionStart"),
        AccessTools.Method(typeof(HideoutController), "HandleRecipe"),
        AccessTools.Method(typeof(HideoutController), "ApplyWorkoutSkillGain"),
        AccessTools.Method(typeof(HideoutHelper), "UpdateFuel"),
        AccessTools.Method(typeof(HideoutHelper), "UpdateWaterFilters"),
        AccessTools.Method(typeof(HideoutHelper), "UpdateAirFilters"),
        AccessTools.Method(typeof(RepairService), "AddRepairSkillPoints"),
        AccessTools.Method(typeof(InsuranceController), "Insure"),
        AccessTools.Method(typeof(InventoryController), "FlagItemsAsInspectedAndRewardXp"),
    };

    public static MethodBase AwardMethod() => AccessTools.Method(typeof(ProfileHelper), "AddSkillPointsToPlayer",
        new[] { typeof(PmcData), typeof(SkillTypes), typeof(double), typeof(bool), typeof(bool) });

    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        await config.EnsureLoadedAsync();
        _config = config;
        var harmony = new Harmony("com.cj.skillsextended.leveling.gameplay");
        var prefix = new HarmonyMethod(typeof(GameplayLevelingPatches), nameof(Enter));
        var finalizer = new HarmonyMethod(typeof(GameplayLevelingPatches), nameof(Exit));
        foreach (var method in GameplayMethods())
            harmony.Patch(method ?? throw new MissingMethodException("Missing server gameplay XP method."), prefix: prefix, finalizer: finalizer);
        harmony.Patch(AwardMethod() ?? throw new MissingMethodException("Missing terminal skill XP award method."),
            prefix: new HarmonyMethod(typeof(GameplayLevelingPatches), nameof(ScaleAward)));
    }

    public static void Enter(out int __state) => GameplayLevelingContext.Enter(out __state);
    public static void Exit(int __state) => GameplayLevelingContext.Exit(__state);
    public static void ScaleAward(SkillTypes skill, ref double pointsToAddToSkill) =>
        pointsToAddToSkill = GameplayLevelingContext.Scale(_config.SkillsConfig.LevelingSpeed, (int)skill, pointsToAddToSkill);
}
