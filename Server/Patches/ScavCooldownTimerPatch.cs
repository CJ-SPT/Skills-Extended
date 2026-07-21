using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Extensions;
using SkillsExtended.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Generators.Bot;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Patches;

[Injectable]
public class ScavCooldownTimerPatch(
    ConfigController configController,
    FenceService fenceService,
    SkillUtil skillUtil,
    TimeUtil timeUtil,
    GlobalTable globalTable
) : AbstractPatch
{
    private static ConfigController _configController = null!;
    private static FenceService _fenceService = null!;
    private static SkillUtil _skillUtil = null!;
    private static TimeUtil _timeUtil = null!;
    private static GlobalTable _globalTable = null!;

    protected override MethodBase? GetTargetMethod()
    {
        _configController = configController;
        _fenceService = fenceService;
        _skillUtil = skillUtil;
        _timeUtil = timeUtil;
        _globalTable = globalTable;

        return AccessTools.Method(typeof(PlayerScavGenerator), "SetScavCooldownTimer");
    }

    [PatchPrefix]
    public static bool Prefix(PmcData scavData, PmcData pmcData)
    {
        if (_skillUtil.IsEliteLevel(pmcData.Id!.Value, SkillTypes.Shadowconnections))
        {
#if DEBUG
            Console.WriteLine("Elite Shadow Connections, no scav cooldown.");
#endif
            // Give it 5 seconds so nothing weird happens
            scavData.Info!.SavageLockTime = _timeUtil.GetTimeStamp() + 5;
            return false;
        }

        if (
            !_skillUtil.TryGetSkillLevel(
                pmcData.Id.Value,
                SkillTypes.Shadowconnections,
                out var skillLevel
            )
        )
        {
            return true;
        }

        var modifier =
            1d
            + pmcData
                .Bonuses?.Where(x => x.Type == BonusType.ScavCooldownTimer)
                .Sum(bonus => (bonus.Value ?? 1) / 100);

        modifier *= _fenceService.GetFenceInfo(pmcData)!.SavageCooldownModifier;

        var timeBonusPerLevel =
            _configController.SkillsConfig.ShadowConnections.ScavCooldownTimeDec.NormalizeToPercentage();
        var buff = Math.Clamp(1f - timeBonusPerLevel * skillLevel, 0.05f, 1f);

        modifier *= buff;

        scavData.Info!.SavageLockTime =
            _timeUtil.GetTimeStamp() + _globalTable.Configuration.SavagePlayCooldown * modifier;

#if DEBUG
        Console.WriteLine($"SE Timer Buff: {buff}");
        Console.WriteLine($"Total Modifier {modifier}");
        Console.WriteLine(
            $"Next Scav Time modified time: `{DateTimeOffset.FromUnixTimeSeconds((long)scavData.Info?.SavageLockTime)}`"
        );
#endif

        return false;
    }
}
