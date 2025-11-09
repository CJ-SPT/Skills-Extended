using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Hideout;
using SPTarkov.Server.Core.Services;

namespace SkillsExtended.Patches;

/// <summary>
///     This patch is needed so we can get the sessionId of the profile starting the sacrifice
/// </summary>
public class StartSacrificePatch : AbstractPatch
{
    internal static MongoId PmcProfileId { get; private set; }
    
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(CircleOfCultistService), nameof(CircleOfCultistService.StartSacrifice));
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
public class CultistProductionPatch : AbstractPatch
{
    private static readonly ConfigController ConfigController = ServiceLocator.ServiceProvider.GetRequiredService<ConfigController>();
    private static readonly SkillUtil SkillUtil = ServiceLocator.ServiceProvider.GetRequiredService<SkillUtil>();
    
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(CircleOfCultistService), "GetCircleCraftingInfo");
    }

    [PatchPostfix]
    public static void Postfix(CircleCraftDetails __result)
    {
        if (StartSacrificePatch.PmcProfileId.IsEmpty)
        {
            throw new InvalidOperationException("[Skills Extended] Pmc ProfileId is empty when starting a cultist circle sacrifice.");
        }

        if (!SkillUtil.TryGetSkillLevel(StartSacrificePatch.PmcProfileId, SkillTypes.Shadowconnections, out var skillLevel))
        {
            return;
        }

#if DEBUG
        Console.WriteLine($"Cultist circle original time: `{__result.Time}` seconds");
#endif
        
        var timeBonusPerLevel = ConfigController.SkillsConfig.ShadowConnections.CultistCircleReturnTimeDec;
        
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