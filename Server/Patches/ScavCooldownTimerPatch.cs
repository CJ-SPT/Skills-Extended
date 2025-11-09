using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Extensions;
using SkillsExtended.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Patches;

public class ScavCooldownTimerPatch : AbstractPatch
{
    private static readonly ConfigController ConfigController = ServiceLocator.ServiceProvider.GetRequiredService<ConfigController>();
    private static readonly FenceService FenceService = ServiceLocator.ServiceProvider.GetRequiredService<FenceService>();
    private static readonly DatabaseService DatabaseService =  ServiceLocator.ServiceProvider.GetRequiredService<DatabaseService>();
    private static readonly SkillUtil SkillUtil = ServiceLocator.ServiceProvider.GetRequiredService<SkillUtil>();
    private static readonly TimeUtil TimeUtil = ServiceLocator.ServiceProvider.GetRequiredService<TimeUtil>();
    
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(PlayerScavGenerator), "SetScavCooldownTimer");
    }

    [PatchPrefix]
    public static bool Prefix(PmcData scavData, PmcData pmcData)
    {
        if (SkillUtil.IsEliteLevel(pmcData.Id!.Value, SkillTypes.Shadowconnections))
        {
#if DEBUG
            Console.WriteLine("Elite Shadow Connections, no scav cooldown.");
#endif
            // Give it 5 seconds so nothing weird happens
            scavData.Info!.SavageLockTime = TimeUtil.GetTimeStamp() + 5;
            return false;
        }
        
        if (!SkillUtil.TryGetSkillLevel(pmcData.Id.Value, SkillTypes.Shadowconnections, out var skillLevel))
        {
            return true;
        }
        
        var modifier = 1d + pmcData.Bonuses?.Where(x => x.Type == BonusType.ScavCooldownTimer)
            .Sum(bonus => (bonus.Value ?? 1) / 100);

        modifier *= FenceService.GetFenceInfo(pmcData)!.SavageCooldownModifier;
        
        var timeBonusPerLevel = ConfigController.SkillsConfig.ShadowConnections.ScavCooldownTimeDec.NormalizeToPercentage();
        var buff = Math.Clamp(1f - timeBonusPerLevel * skillLevel, 0.05f, 1f);
        
        modifier *= buff;
        
        scavData.Info!.SavageLockTime = TimeUtil.GetTimeStamp() + DatabaseService.GetGlobals().Configuration.SavagePlayCooldown * modifier;
        
#if DEBUG
        Console.WriteLine($"SE Timer Buff: {buff}");
        Console.WriteLine($"Total Modifier {modifier}");
        Console.WriteLine($"Next Scav Time modified time: `{DateTimeOffset.FromUnixTimeSeconds((long)scavData.Info?.SavageLockTime)}`");
#endif

        return false;
    }
}