using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Models;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.ProneMovement.Patches;

public class ProneMoveStatePatch : ModulePatch
{
    private static FieldInfo _playerField;
    private static ProneMovementData ProneData => SkillsPlugin.SkillData.ProneMovement;
    
    protected override MethodBase GetTargetMethod()
    {
        _playerField = AccessTools.Field(typeof(MovementContext), "_player");
        return AccessTools.Method(typeof(MovementContext), nameof(MovementContext.ClampSpeed));
    }

    [PatchPrefix]
    [HarmonyAfter(["RealismMod"])]
    private static bool Prefix(MovementContext __instance, float speed, ref float __result)
    {
        if (!ProneData.Enabled) return true;
        if (__instance.CurrentState is not ProneMoveStateClass) return true;
        
        var player = (Player)_playerField.GetValue(__instance);

        if (!player.IsYourPlayer) return true;

        var buff = player.Skills.ProneMovementSpeed;
        var bonus = 1f + buff;

#if DEBUG
        Logger.LogDebug($"Original Prone Speed: {speed}");
        Logger.LogDebug($"Updated Prone Speed: {speed * bonus}");
#endif
        
        if (!player.Skills.ProneMovement.IsEliteLevel)
        {
            player.ExecuteSkill(() => player.Skills.ProneAction.Complete(ProneData.XpPerAction));
        }
        
        __result = Mathf.Clamp(speed * bonus, 0f, __instance.StateSpeedLimit * bonus);
        
        return false;
    }
}

public class ProneMoveVolumePatch : ModulePatch
{
    private static FieldInfo _playerField;
    private static ProneMovementData proneData => SkillsPlugin.SkillData.ProneMovement;
    
    protected override MethodBase GetTargetMethod()
    {
        _playerField = AccessTools.Field(typeof(MovementContext), "_player");
        return AccessTools.PropertyGetter(typeof(MovementContext), nameof(MovementContext.CovertMovementVolume));
    }

    [PatchPrefix]
    private static bool Prefix(MovementContext __instance, ref float __result)
    {
        if (!proneData.Enabled) return true;
        if (__instance.CurrentState is not ProneMoveStateClass) return true;
        
        var player = (Player)_playerField.GetValue(__instance);

        if (!player.IsYourPlayer) return true;

        var buff = player.Skills.ProneMovementVolume;
        var bonus = 1f - buff;

#if DEBUG
        Logger.LogDebug($"Original Prone volume: {__result}");
        Logger.LogDebug($"Updated Prone volume: {__result * bonus}");
#endif
        
        __result = Mathf.Clamp(__result * bonus, 0f, __result * bonus);
        
        return false;
    }
}