using Comfort.Common;
using EFT;
using EFT.Interactive;
using SPT.Reflection.Patching;
using System.Reflection;

namespace SkillsExtended.Skills.LockPicking.Patches;

internal class DoorActionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(GetActionsClass).GetMethod("smethod_5", BindingFlags.Public | BindingFlags.Static);

    [PatchPostfix]
    private static void Postfix(ref ActionsReturnClass __result, GamePlayerOwner owner, WorldInteractiveObject worldInteractiveObject)
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
        
        if (!SkillsExtendedPlugin.SkillData.LockPicking.Enabled 
            || WorldInteractionUtils.IsBotInteraction(owner)
            || owner.Player.Side == EPlayerSide.Savage)
        {
            return;
        }

        worldInteractiveObject.AddLockpickingInteraction(__result, owner);
        worldInteractiveObject.AddInspectInteraction(__result, owner);
    }
}
