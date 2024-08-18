using System;
using System.Linq;
using EFT;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Controllers;
using SkillsExtended.LockPicking;
using SPT.Reflection.Utils;

namespace SkillsExtended.Patches.InRaid;

internal class LocationSceneAwakePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
        => typeof(LocationScene).GetMethod(nameof(LocationScene.Awake));

    [PatchPostfix]
    private static void Postfix(LocationScene __instance)
    {
        foreach (var interactableObj in __instance.WorldInteractiveObjects)
        {
            if (interactableObj.KeyId != null && interactableObj.KeyId != string.Empty)
            {
                if (Plugin.Keys.KeyLocale.ContainsKey(interactableObj.KeyId))
                {
                    Plugin.Log.LogDebug($"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId} Key Name: {Plugin.Keys.KeyLocale[interactableObj.KeyId]}");
                }
                else
                {
                    Plugin.Log.LogError($"Door ID: {interactableObj.Id} KeyID: {interactableObj.KeyId} Key locale missing...");
                }
            }
        }
    }
}
