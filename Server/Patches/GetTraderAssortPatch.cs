using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Extensions;
using SkillsExtended.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Commerce;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Traders;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace SkillsExtended.Patches;

[Injectable]
public class GetTraderAssortPatch(
    ConfigController configController,
    SkillUtil skillUtil,
    ProfileHelper profileHelper,
    PaymentHelper paymentHelper
) : AbstractPatch
{
    private static ConfigController _configController = null!;
    private static SkillUtil _skillUtil = null!;
    private static ProfileHelper _profileHelper = null!;
    private static PaymentHelper _paymentHelper = null!;

    protected override MethodBase? GetTargetMethod()
    {
        _configController = configController;
        _skillUtil = skillUtil;
        _profileHelper = profileHelper;
        _paymentHelper = paymentHelper;

        return AccessTools.Method(typeof(TraderAssortHelper), nameof(TraderAssortHelper.GetAssort));
    }

    [PatchPostfix]
    public static void Postfix(MongoId sessionId, MongoId traderId, TraderAssort __result)
    {
        var profile = _profileHelper.GetPmcProfile(sessionId);
        if (profile is null)
        {
            return;
        }

        _skillUtil.TryGetSkillLevel(sessionId, SkillTypes.UsecNegotiations, out var usecLevel);
        _skillUtil.TryGetSkillLevel(sessionId, SkillTypes.BearRawpower, out var bearLevel);

        foreach (var assort in __result.BarterScheme)
        {
            foreach (
                var barter in assort.Value.SelectMany(scheme =>
                    scheme.Where(b => _paymentHelper.IsMoneyTpl(b.Template))
                )
            )
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
        var usecConfig = _configController.SkillsConfig.UsecNegotiations;
        var bearConfig = _configController.SkillsConfig.BearRawPower;

        // Keep track of an additive running discount for all skills
        var discount = 0.0f;

        // Peacekeeper discount only
        if (traderId == Traders.PEACEKEEPER)
        {
            if (profile.Info?.Side == "Usec" || !usecConfig.FactionLocked)
            {
                discount +=
                    usecConfig.PeacekeeperTradingCostDec.NormalizeToPercentage() * usecLevel;
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
