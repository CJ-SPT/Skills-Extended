using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;

namespace SkillsExtended.Patches.UI;

public class GetBarterPricePatch : ModulePatch
{
    public static Item Selecteditem;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TraderAssortmentControllerClass),
            nameof(TraderAssortmentControllerClass.GetBarterPrice));
    }

    [PatchPostfix]
    private static void Postfix(TraderAssortmentControllerClass __instance, ref TraderClass.GStruct244? __result, Item[] items, TraderClass ___traderClass)
    {
        if (items.IsNullOrEmpty()) return;
        if (!Plugin.SkillData.SilentOps.Enabled) return;
        
        var scheme = __instance.GetSchemeForItem(items[0]);

        if (scheme is null) return;

        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        
        float price = 0;
        foreach (var item in items)
        {
            var num2 = Mathf.Ceil((float)__instance.GetSchemeForItem(item)
                .Sum(TraderAssortmentControllerClass.Class1762.class1762_0.method_0));

            var bonus = 1f - skillMgrExt.SilentOpsSilencerCostRedBuff;

            // Silencer Type
            if (item is GClass2671)
            {
                num2 *= bonus;
            }
            
            price += num2;
        }

        Selecteditem = __instance.SelectedItem;
        
        __result = new TraderClass.GStruct244(scheme[0][0]._tpl, (int)Mathf.Ceil(price));
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
        // Suppressor type
        if (GetBarterPricePatch.Selecteditem is not GClass2671) return;
        if (!Plugin.SkillData.SilentOps.Enabled) return;
        
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        var bonus = 1f - skillMgrExt.SilentOpsSilencerCostRedBuff;

        __result = (int)Mathf.Ceil(__result * bonus);
    }
}
