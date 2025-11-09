using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Extensions;
using SkillsExtended.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace SkillsExtended.Patches;

public class GetTraderAssortPatch : AbstractPatch
{
    private static readonly ConfigController ConfigController = ServiceLocator.ServiceProvider.GetRequiredService<ConfigController>();
    private static readonly SkillUtil SkillUtil = ServiceLocator.ServiceProvider.GetRequiredService<SkillUtil>();
    private static readonly ProfileHelper ProfileHelper = ServiceLocator.ServiceProvider.GetRequiredService<ProfileHelper>();
    private static readonly PaymentHelper PaymentHelper = ServiceLocator.ServiceProvider.GetRequiredService<PaymentHelper>();
    
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(TraderAssortHelper), nameof(TraderAssortHelper.GetAssort));
    }

    [PatchPostfix]
    public static void Postfix(
        MongoId sessionId, 
        MongoId traderId, 
        TraderAssort __result
        )
    {
        var profile = ProfileHelper.GetPmcProfile(sessionId);
        if (profile is null)
        {
            return;
        }
        
        SkillUtil.TryGetSkillLevel(sessionId, SkillTypes.UsecNegotiations, out var usecLevel);
        SkillUtil.TryGetSkillLevel(sessionId, SkillTypes.BearRawpower, out var bearLevel);
        
        foreach (var assort in __result.BarterScheme)
        {
            foreach (var barter in assort.Value.SelectMany(scheme => scheme.Where(b => PaymentHelper.IsMoneyTpl(b.Template))))
            {
                ModifyMoneyPrice(traderId, barter, profile, usecLevel, bearLevel);
            }
        }
    }

    private static void ModifyMoneyPrice(
        MongoId traderId, 
        BarterScheme barter, 
        PmcData profile,
        int usecLevel,
        int bearLevel
        )
    {
        var usecConfig = ConfigController.SkillsConfig.UsecNegotiations;
        var bearConfig = ConfigController.SkillsConfig.BearRawPower;
        
        // Keep track of an additive running discount for all skills
        var discount = 0.0f;
        
        // Peacekeeper discount only
        if (traderId == Traders.PEACEKEEPER)
        {
            if (profile.Info?.Side == "Usec" || !usecConfig.FactionLocked)
            {
                discount += usecConfig.PeacekeeperTradingCostDec.NormalizeToPercentage() * usecLevel;
            }
        }

        // Prapor discount only
        if (traderId == Traders.PRAPOR)
        {
            if (profile.Info?.Side == "Bear" || !usecConfig.FactionLocked)
            {
                discount += bearConfig.PraporTradingCostDec.NormalizeToPercentage() * bearLevel;
            }
        }
        
        // Usec Negotiations elite discount
        if (usecConfig.Enabled && usecLevel == 51)
        {
            if (profile.Info?.Side == "Usec" || !usecConfig.FactionLocked)
            {
                discount += usecConfig.AllTraderCostDecrease.NormalizeToPercentage();
            }
        }

        // Bear raw power elite discount
        if (bearConfig.Enabled && bearLevel == 51)
        {
            if (profile.Info?.Side == "Bear" || !bearConfig.FactionLocked)
            {
                discount += bearConfig.AllTraderCostDecrease.NormalizeToPercentage();
            }
        }
        
        var normalizedDiscount = Math.Clamp(1 - discount, 0.10f, 1.0f);

#if DEBUG
        Console.WriteLine($"discount: {discount}%");
        Console.WriteLine($"normalized discount: {normalizedDiscount}%");
        Console.WriteLine($"Original price: {barter.Count}");
#endif
        
        barter.Count *= normalizedDiscount;
        
#if DEBUG
        Console.WriteLine($"Modified price: {barter.Count}");
#endif
    }
}