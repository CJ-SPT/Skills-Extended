using Comfort.Common;
using EFT;
using EFT.Interactive;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;
using System.Reflection;

namespace SkillsExtended.Patches;

internal class DoorActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(GetActionsClass).GetMethod("smethod_3", BindingFlags.Public | BindingFlags.Static);

    [PatchPostfix]
    private static void Postfix(ref ActionsReturnClass __result, GamePlayerOwner owner, WorldInteractiveObject worldInteractiveObject)
    {
        if (WorldInteractionUtils.IsBotInteraction(owner)
            || !Plugin.SkillData.LockPicking.Enabled
            || Singleton<GameWorld>.Instance.MainPlayer.Side == EPlayerSide.Savage)
        {
            return;
        }

        worldInteractiveObject.AddLockpickingInteraction(__result, owner);
        worldInteractiveObject.AddInspectInteraction(__result, owner);
    }
}

internal class KeycardDoorActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(GetActionsClass).GetMethod("smethod_9", BindingFlags.Public | BindingFlags.Static);

    [PatchPostfix]
    private static void Postfix(ref ActionsReturnClass __result, GamePlayerOwner owner, KeycardDoor door)
    {
        if (WorldInteractionUtils.IsBotInteraction(owner)
            || !Plugin.SkillData.LockPicking.Enabled
            || Singleton<GameWorld>.Instance.MainPlayer.Side == EPlayerSide.Savage)
        {
            return;
        }

        door.AddInspectInteraction(__result, owner);
        door.AddKeyCardInteraction(__result, owner);
    }
}
