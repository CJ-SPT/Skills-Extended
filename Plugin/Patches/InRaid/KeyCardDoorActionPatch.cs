using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.InRaid;

public class KeyCardDoorActionPatch : ModulePatch
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