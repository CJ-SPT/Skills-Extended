using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Patches;

public class ScavCooldownTimerPatch : AbstractPatch
{
    private static readonly ConfigController ConfigController = ServiceLocator.ServiceProvider.GetRequiredService<ConfigController>();
    private static readonly SkillUtil SkillUtil = ServiceLocator.ServiceProvider.GetRequiredService<SkillUtil>();
    private static readonly TimeUtil TimeUtil = ServiceLocator.ServiceProvider.GetRequiredService<TimeUtil>();
    
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(PlayerScavGenerator), "SetScavCooldownTimer");
    }

    [PatchPostfix]
    public static void Postfix(PmcData scavData, PmcData pmcData)
    {
        if (SkillUtil.IsEliteLevel(pmcData.Id!.Value, SkillTypes.Shadowconnections))
        {
#if DEBUG
            Console.WriteLine("Elite Shadow Connections, no scav cooldown.");
#endif
            // Give it 5 seconds so nothing weird happens
            scavData.Info!.SavageLockTime = TimeUtil.GetTimeStamp() + 5;
            return;
        }
        
        if (!SkillUtil.TryGetSkillLevel(pmcData.Id.Value, SkillTypes.Shadowconnections, out var skillLevel))
        {
            return;
        }

#if DEBUG
        Console.WriteLine($"Next Scav Time Original: `{scavData.Info?.SavageLockTime}`");
#endif
        
        var timeBonusPerLevel = ConfigController.SkillsConfig.ShadowConnections.ScavCooldownTimeDec;
        var buff = Math.Clamp(1f - timeBonusPerLevel * skillLevel, 0f, 1f);
        var sptScavTime = scavData.Info!.SavageLockTime - TimeUtil.GetTimeStamp();
        
        scavData.Info!.SavageLockTime = sptScavTime * buff;
        
#if DEBUG
        Console.WriteLine($"Spt scav cooldown: `{sptScavTime}` seconds");
        Console.WriteLine($"Scav cooldown timer Buff: {buff}");
        Console.WriteLine($"Next Scav Time modified time: `{scavData.Info?.SavageLockTime}`");
#endif
    }
}