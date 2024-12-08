using Comfort.Common;
using EFT;
using EFT.Interactive;
using SPT.Reflection.Patching;
using System.Reflection;

namespace SkillsExtended.Skills.LockPicking.Patches;

internal class DoorActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(GetActionsClass).GetMethod("smethod_4", BindingFlags.Public | BindingFlags.Static);

    [PatchPostfix]
    private static void Postfix(ref ActionsReturnClass __result, GamePlayerOwner owner, WorldInteractiveObject worldInteractiveObject)
    {
        if (WorldInteractionUtils.IsBotInteraction(owner)
            || !SkillsPlugin.SkillData.LockPicking.Enabled
            || Singleton<GameWorld>.Instance.MainPlayer.Side == EPlayerSide.Savage)
        {
            return;
        }

        worldInteractiveObject.AddLockpickingInteraction(__result, owner);
        worldInteractiveObject.AddInspectInteraction(__result, owner);
    }
}
