using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.LockPicking.Patches;

[IgnoreAutoPatch]
public class KeyCardDoorActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(InteractionContextHelper),
            nameof(InteractionContextHelper.GetAvailableActions),
            [typeof(GamePlayerOwner), typeof(KeycardDoor), typeof(bool)]
        );
    }

    [PatchPostfix]
    private static void Postfix(
        ref InteractionContextHelper __result,
        GamePlayerOwner owner,
        KeycardDoor door
    )
    {
        /*
        if (WorldInteractionUtils.IsBotInteraction(owner)
            || !SkillsExtendedPlugin.SkillData.LockPicking.Enabled
            || Singleton<GameWorld>.Instance.MainPlayer.Side == EPlayerSide.Savage)
        {
            return;
        }

        door.AddInspectInteraction(__result, owner);
        door.AddKeyCardInteraction(__result, owner);
        */
    }
}
