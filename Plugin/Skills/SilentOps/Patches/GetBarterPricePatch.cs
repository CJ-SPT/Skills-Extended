using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;

namespace SkillsExtended.Skills.SilentOps.Patches;

public class GetBarterPricePatch : ModulePatch
{
    public static Item Selecteditem;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TraderAssortmentControllerClass),
            nameof(TraderAssortmentControllerClass.GetBarterPrice));
    }

    [PatchPostfix]
    private static void Postfix(TraderAssortmentControllerClass __instance, ref TraderClass.GStruct300? __result, Item[] items)
    {
        if (!Plugin.SkillData.SilentOps.Enabled || items.IsNullOrEmpty())
        {
            return;
        }
        
        var scheme = __instance.GetSchemeForItem(items[0]);
        if (scheme is null)
        {
            return;
        }
        
        float price = 0;
        foreach (var item in items)
        {
            var barterScheme = __instance.GetSchemeForItem(item);
            if (barterScheme is null)
            {
                continue;
            }
            
            var num2 = Mathf.Ceil((float)barterScheme.Sum(TraderAssortmentControllerClass.Class2058.class2058_0.method_0));
            var bonus = 1f - GameUtils.GetSkillManager()!.SkillManagerExtended.SilentOpsSilencerCostRedBuff;

            // Silencer Type
            if (item is SilencerItemClass)
            {
                num2 *= bonus;
            }
            
            price += num2;
        }

        Selecteditem = __instance.SelectedItem;
        
        __result = new TraderClass.GStruct300(scheme[0][0]._tpl, (int)Mathf.Ceil(price));
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
        if (!Plugin.SkillData.SilentOps.Enabled || GetBarterPricePatch.Selecteditem is not SilencerItemClass)
        {
            return;
        }
        
        var bonus = 1f - GameUtils.GetSkillManager()!.SkillManagerExtended.SilentOpsSilencerCostRedBuff;

        __result = (int)Mathf.Ceil(__result * bonus);
    }
}
