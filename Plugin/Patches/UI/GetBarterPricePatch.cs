using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;
/*
namespace SkillsExtended.Patches.UI;

public class GetBarterPricePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TraderAssortmentControllerClass),
            nameof(TraderAssortmentControllerClass.GetBarterPrice));
    }

    [PatchPostfix]
    private static void Postfix(TraderAssortmentControllerClass __instance, ref TraderClass.GStruct244? __result, Item[] items, TraderClass ___traderClass)
    {
        if (items.IsNullOrEmpty()) return;
        
        var scheme = __instance.GetSchemeForItem(items[0]);

        if (scheme is null) return;

        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        
        var price = 0;
        foreach (var item in items)
        {
            var num2 = (int)Mathf.Ceil((float)__instance.GetSchemeForItem(item)
                .Sum(TraderAssortmentControllerClass.Class1762.class1762_0.method_0));

            price += num2;
        }

        var bonus = 1f;
        
        if (___traderClass.Id == Traders.PRAPOR || ___traderClass.Id == Traders.JAEGER)
        {
            if (Plugin.Session.Profile.Side == EPlayerSide.Usec)
            {
                bonus = 1f - skillMgrExt.UsecNegotiationsTraderPrice;
            }
        }
        
        if (___traderClass.Id == Traders.PEACEKEEPER || ___traderClass.Id == Traders.MECHANIC)
        {
            if (Plugin.Session.Profile.Side == EPlayerSide.Bear)
            {
                bonus = 1f - skillMgrExt.BearAuthorityTraderPrice;
            }
        }
        
        __result = new TraderClass.GStruct244(scheme[0][0]._tpl, (int)Mathf.Ceil(price * bonus));
    }
}

public class RequiredItemsCountPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        var type = PatchConstants.EftTypes.SingleCustom(t => t.GetProperty("RequiredItemsCount") != null);
        
        return AccessTools.PropertyGetter(type, "RequiredItemsCount");
    }

    [PatchPostfix]
    private static void Postfix(GClass2064 __instance, ref int __result)
    {
        var activeTrader = TraderDealScreenPatch.ActiveTrader;
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        
        if (activeTrader == Traders.PRAPOR || activeTrader == Traders.JAEGER)
        {
            if (Plugin.Session.Profile.Side == EPlayerSide.Usec)
            {
                var bonus = 1f - skillMgrExt.UsecNegotiationsTraderPrice;
            
                __result = (int)Mathf.Ceil(__result * bonus);
            };
        }
        
        if (activeTrader == Traders.PEACEKEEPER || activeTrader == Traders.MECHANIC)
        {
            if (Plugin.Session.Profile.Side == EPlayerSide.Bear)
            {
                var bonus = 1f - skillMgrExt.BearAuthorityTraderPrice;
            
                __result = (int)Mathf.Ceil(__result * bonus);
            }
        }
    }
}
*/