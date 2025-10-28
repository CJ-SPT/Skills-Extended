using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Enums;

namespace SkillsExtended.Patches;

public class QuestMoneyRewardPatch : AbstractPatch
{
    private static readonly ConfigController ConfigController = ServiceLocator.ServiceProvider.GetRequiredService<ConfigController>();
    private static readonly SkillUtils SkillUtils = ServiceLocator.ServiceProvider.GetRequiredService<SkillUtils>();
    
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(QuestRewardHelper), "GetQuestMoneyRewardBonusMultiplier");
    }

    [PatchPostfix]
    public static void Postfix(PmcData pmcData, ref double __result)
    {
        var config = ConfigController.SkillsConfig.UsecNegotiations;
        if (!config.Enabled)
        {
            return;
        }
        
        if (pmcData.Info?.Side == "Bear" && config.FactionLocked)
        {
            return;
        }

        if (!SkillUtils.TryGetSkillLevel(pmcData.Id!.Value, SkillTypes.UsecNegotiations, out var level))
        {
            return;
        }

        // Convert reward percentage into whole number before application
        var bonus = config.QuestMoneyRewardInc * 100 * level;
        
#if DEBUG
        Console.WriteLine($"[Skills Extended] Base cash reward bonus `{__result}`");        
#endif
        
        __result += bonus;
        
#if DEBUG
        Console.WriteLine($"[Skills Extended] Applying `{bonus}` to quest cash reward: new bonus {__result}");        
#endif
    }
}