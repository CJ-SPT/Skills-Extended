using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Quest;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;

namespace SkillsExtended.Patches;

public class QuestMoneyRewardPatch(ConfigController configController, SkillUtil skillUtil)
    : AbstractPatch
{
    private static ConfigController _configController = null!;
    private static SkillUtil _skillUtil = null!;

    protected override MethodBase? GetTargetMethod()
    {
        _configController = configController;
        _skillUtil = skillUtil;

        return AccessTools.Method(typeof(QuestRewardHelper), "GetQuestMoneyRewardBonusMultiplier");
    }

    [PatchPostfix]
    public static void Postfix(PmcData pmcData, ref double __result)
    {
        var config = _configController.SkillsConfig.UsecNegotiations;
        if (!config.Enabled)
        {
            return;
        }

        if (pmcData.Info?.Side == "Bear" && config.FactionLocked)
        {
            return;
        }

        if (
            !_skillUtil.TryGetSkillLevel(
                pmcData.Id!.Value,
                SkillTypes.UsecNegotiations,
                out var level
            )
        )
        {
            return;
        }

        // Convert reward percentage into whole number before application
        var bonus = config.QuestMoneyRewardInc * level;

#if DEBUG
        Console.WriteLine($"[Skills Extended] Base cash reward bonus `{__result}`");
#endif
        __result += bonus;

#if DEBUG
        Console.WriteLine(
            $"[Skills Extended] Applying `{bonus}` to quest cash reward: new bonus {__result}"
        );
#endif
    }
}
