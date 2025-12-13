using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtendedFika.Controllers;
using SPT.Reflection.Patching;

namespace SkillsExtendedFika.Patches;

public class OnGameStartedPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    public static void PatchPostfix(GameWorld __instance)
    {
        LockPickingFikaController.GetDoors();
    }
}