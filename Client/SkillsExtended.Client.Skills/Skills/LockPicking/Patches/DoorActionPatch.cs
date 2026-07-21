using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.LockPicking.Patches;

internal class DoorActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(InteractionContextHelper),
            nameof(InteractionContextHelper.GetAvailableActions),
            [typeof(GamePlayerOwner), typeof(WorldInteractiveObject)]
        );
    }

    [PatchPostfix]
    private static void Postfix(
        ref AvailableInteractionState __result,
        GamePlayerOwner owner,
        WorldInteractiveObject worldInteractiveObject
    )
    {
        // We're the headless, don't do anything
        if (!Singleton<GameWorld>.Instance?.MainPlayer)
        {
            return;
        }

        // Disable without the sync plugin
        if (SkillsExtendedInfo.IsFikaPresent && !SkillsExtendedInfo.SyncPluginPresent)
        {
            return;
        }

        if (
            !SkillsExtendedPlugin.SkillData.LockPicking.Enabled
            || WorldInteractionUtils.IsBotInteraction(owner)
            || owner.Player.Side == EPlayerSide.Savage
        )
        {
            return;
        }

        worldInteractiveObject.AddLockpickingInteraction(__result, owner);
        worldInteractiveObject.AddInspectInteraction(__result, owner);
    }
}
