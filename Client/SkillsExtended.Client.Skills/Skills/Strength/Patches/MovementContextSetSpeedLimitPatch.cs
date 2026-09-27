using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.Strength.Patches;

public class MovementContextSetSpeedLimitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(MovementContext), nameof(MovementContext.RefreshObstacleRestrictions));

    [PatchPrefix]
    public static bool Prefix(MovementContext __instance, Player ____player)
    {
        if (!SkillsExtendedPlugin.SkillData.Strength.Enabled || !____player.IsYourPlayer)
        {
            return true;
        }

        var skills = ____player.Skills.SkillsExtendedManager;
        var conditions = EPhysicalCondition.None;
        var hasSwampSpeedLimit = false;
        foreach (var obstacle in __instance._enteredObstacles)
        {
            conditions |= obstacle.ConditionsMask;
            hasSwampSpeedLimit |= obstacle.HasSwampSpeedLimit;
        }

        if (skills.StrengthBushSpeedIncBuffElite.Value)
        {
            conditions &= ~(EPhysicalCondition.SprintDisabled | EPhysicalCondition.JumpDisabled);
        }

        __instance.SetPhysicalCondition(EPhysicalCondition.ProneDisabled,
            (conditions & EPhysicalCondition.ProneDisabled) != 0);
        __instance.SetPhysicalCondition(EPhysicalCondition.ProneMovementDisabled,
            (conditions & EPhysicalCondition.ProneMovementDisabled) != 0);
        __instance.SetPhysicalCondition(EPhysicalCondition.SprintDisabled,
            (conditions & EPhysicalCondition.SprintDisabled) != 0);
        __instance.SetPhysicalCondition(EPhysicalCondition.JumpDisabled,
            (conditions & EPhysicalCondition.JumpDisabled) != 0);

        if (__instance.PhysicalConditionIs(EPhysicalCondition.SprintDisabled))
        {
            __instance.EnableSprint(false);
        }

        if (hasSwampSpeedLimit && !skills.StrengthBushSpeedIncBuffElite.Value)
        {
            __instance.AddStateSpeedLimit(
                Mathf.Clamp01(0.2f * (1f + skills.StrengthBushSpeedIncBuff)),
                Player.ESpeedLimit.Swamp);
        }
        else
        {
            __instance.RemoveStateSpeedLimit(Player.ESpeedLimit.Swamp);
        }

        return false;
    }
}
