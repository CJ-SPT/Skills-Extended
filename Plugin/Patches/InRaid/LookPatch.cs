using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.InRaid;

public class LookPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.Look));
    }

    [PatchPrefix]
    private static bool Prefix()
    {
        return !Plugin.MiniGame.gameObject.activeSelf;
    }
}