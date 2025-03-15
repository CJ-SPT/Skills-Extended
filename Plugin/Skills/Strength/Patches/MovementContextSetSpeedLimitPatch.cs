using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Strength.Patches;

public class MovementContextSetSpeedLimitPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(MovementContext), nameof(MovementContext.method_0), [typeof(float), typeof(string)]);
	}

	[PatchPrefix]
	public static bool Prefix(MovementContext __instance)
	{
		var skillData = SkillsPlugin.SkillData;
		var skillMgrExt = SkillManagerExt.Instance(EPlayerSide.Usec);

		if (!skillData.Strength.Enabled) return true;

		MovementContext.Struct303 gStruct;
		gStruct.movementContext_0 = __instance;
		gStruct.conditions = EPhysicalCondition.None;

		var flag = false;
		foreach (var collider in __instance._enteredObstacles)
		{
			gStruct.conditions |= collider.ConditionsMask;
			flag |= collider.HasSwampSpeedLimit;
		}
		
		__instance.method_28(EPhysicalCondition.ProneDisabled, ref gStruct);
		__instance.method_28(EPhysicalCondition.ProneMovementDisabled, ref gStruct);

		// Allow jumping/sprinting in swamps
		if (skillMgrExt.StrengthBushSpeedIncBuffElite)
		{
			__instance.method_28(EPhysicalCondition.SprintDisabled, ref gStruct);
			__instance.method_28(EPhysicalCondition.JumpDisabled, ref gStruct);
		}
		
		if (__instance.PhysicalConditionIs(EPhysicalCondition.SprintDisabled))
		{
			__instance.EnableSprint(false);
		}

		if (flag)
		{
			__instance.AddStateSpeedLimit(0.2f * (1 + skillMgrExt.StrengthBushSpeedIncBuff), Player.ESpeedLimit.Swamp);
			return false;
		}
		
		__instance.RemoveStateSpeedLimit(Player.ESpeedLimit.Swamp);
		return false;
	}
}