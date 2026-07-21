using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Hideout;
using SPTarkov.Server.Core.Services.Hideout;

namespace SkillsExtended.Patches;

/// <summary>
///     This patch is needed so we can get the sessionId of the profile starting the sacrifice
/// </summary>
[Injectable]
public class StartSacrificePatch : AbstractPatch
{
    internal static MongoId PmcProfileId { get; private set; }

    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(CircleOfCultistService),
            nameof(CircleOfCultistService.StartSacrifice)
        );
    }

    [PatchPrefix]
    public static void Prefix(MongoId sessionId)
    {
        PmcProfileId = sessionId;
    }

    [PatchPostfix]
    public static void Postfix()
    {
        PmcProfileId = MongoId.Empty();
    }
}

/// <summary>
///     This patch modifies the time required for a cultist circle return
/// </summary>
[Injectable]
public class CultistProductionPatch(ConfigController configController, SkillUtil skillUtil)
    : AbstractPatch
{
    private static ConfigController _configController = null!;
    private static SkillUtil _skillUtil = null!;

    protected override MethodBase? GetTargetMethod()
    {
        _configController = configController;
        _skillUtil = skillUtil;

        return AccessTools.Method(typeof(CircleOfCultistService), "GetCircleCraftingInfo");
    }

    [PatchPostfix]
    public static void Postfix(CircleCraftDetails __result)
    {
        if (StartSacrificePatch.PmcProfileId.IsEmpty)
        {
            throw new InvalidOperationException(
                "[Skills Extended] Pmc ProfileId is empty when starting a cultist circle sacrifice."
            );
        }

        if (
            !_skillUtil.TryGetSkillLevel(
                StartSacrificePatch.PmcProfileId,
                SkillTypes.Shadowconnections,
                out var skillLevel
            )
        )
        {
            return;
        }

#if DEBUG
        Console.WriteLine($"Cultist circle original time: `{__result.Time}` seconds");
#endif
        var timeBonusPerLevel = _configController
            .SkillsConfig
            .ShadowConnections
            .CultistCircleReturnTimeReduction;

        var buff = Math.Clamp(1f - timeBonusPerLevel * skillLevel, 0f, 1f);

#if DEBUG
        Console.WriteLine($"Cultist Circle Buff: {buff}");
#endif
        __result.Time = (long)(__result.Time * buff);

#if DEBUG
        Console.WriteLine($"Cultist circle modified time: `{__result.Time}` seconds");
#endif
    }
}
