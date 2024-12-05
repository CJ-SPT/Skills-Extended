using System.Reflection;
using EFT;
using EFT.ObstacleCollision;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.FirstAid.Patches;

internal class CanWalkPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(MovementContext), nameof(MovementContext.CanWalk));
    }

    [PatchPostfix]
    public static void PostFix(
        MovementContext __instance, 
        Player ____player,
        IObstacleCollisionFacade ____obstacleCollisionFacade,
        ref bool __result)
    {
        if (!____player.IsYourPlayer) return;

        if (Plugin.LockPickingGame.activeSelf)
        {
            __result = false;
            return;
        }
        
        var skillMgrExt = Plugin.PlayerSkillManagerExt;
        var skillData = Plugin.SkillData.FirstAid;

        if (!skillData.Enabled) return;
        if (!skillMgrExt.FirstAidMovementSpeedBuffElite) return;

        __result = ____obstacleCollisionFacade.CanMove();
    }
}