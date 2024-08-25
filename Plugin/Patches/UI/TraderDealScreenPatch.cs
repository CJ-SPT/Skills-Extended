using System.Reflection;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.UI;

public class TraderDealScreenPatch : ModulePatch
{
    public static string ActiveTrader;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.method_11));
    }

    [PatchPostfix]
    private static void Postfix(Profile.TraderInfo traderInfo)
    {
        ActiveTrader = traderInfo.Id;
    }
}