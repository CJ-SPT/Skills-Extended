using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.UI;

public class GetCustomInteractions : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.GetItemContextInteractions));
    }

    [PatchPostfix]
    private static void Postfix()
    {
        
    }
}