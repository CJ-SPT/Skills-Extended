using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SkillsExtended.Models;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Patches.InRaid;

public class ProneMoveStatePatch : ModulePatch
{
    private static FieldInfo _playerField;
    private static ProneMovementData proneData => Plugin.SkillData.ProneMovement;
    
    protected override MethodBase GetTargetMethod()
    {
        _playerField = AccessTools.Field(typeof(MovementContext), "_player");
        return AccessTools.Method(typeof(MovementContext), nameof(MovementContext.ClampSpeed));
    }

    [PatchPrefix]
    [HarmonyAfter(["RealismMod"])]
    private static bool Prefix(MovementContext __instance, float speed, ref float __result)
    {
        if (__instance.CurrentState is not GClass1718) return true;
        if (!proneData.Enabled) return true;
        
        var player = (Player)_playerField.GetValue(__instance);

        if (!player.IsYourPlayer) return true;

        var buff = player.Skills.ProneMovementSpeed;
        var bonus = 1f + buff;
        
        Logger.LogDebug($"Original Prone Speed: {speed}");
        Logger.LogDebug($"Updated Prone Speed: {speed * bonus}");

        if (!player.Skills.ProneMovement.IsEliteLevel)
        {
            player.ExecuteSkill(() => player.Skills.ProneAction.Complete(proneData.XpPerAction));
        }
        
        __result = Mathf.Clamp(speed * bonus, 0f, __instance.StateSpeedLimit * bonus);
        
        return false;
    }
}

public class ProneMoveVolumePatch : ModulePatch
{
    private static FieldInfo _playerField;
    private static ProneMovementData proneData => Plugin.SkillData.ProneMovement;
    
    protected override MethodBase GetTargetMethod()
    {
        _playerField = AccessTools.Field(typeof(MovementContext), "_player");
        return AccessTools.PropertyGetter(typeof(MovementContext), nameof(MovementContext.CovertMovementVolume));
    }

    [PatchPrefix]
    private static bool Prefix(MovementContext __instance, ref float __result)
    {
        if (__instance.CurrentState is not GClass1718) return true;
        if (!proneData.Enabled) return true;
        
        var player = (Player)_playerField.GetValue(__instance);

        if (!player.IsYourPlayer) return true;

        var buff = player.Skills.ProneMovementVolume;
        var bonus = 1f - buff;
        
        Logger.LogDebug($"Original Prone volume: {__result}");
        Logger.LogDebug($"Updated Prone volume: {__result * bonus}");
        
        __result = Mathf.Clamp(__result * bonus, 0f, __result * bonus);
        
        return false;
    }
}