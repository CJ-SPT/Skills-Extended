using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Skills.Core;
using SkillsExtended.Utils;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Strength.Patches;

public class MovementContextSetSpeedLimitPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(MovementContext), nameof(MovementContext.method_0));
	}

	[PatchPrefix]
	public static bool Prefix(MovementContext __instance)
	{
		var skillData = Plugin.SkillData;
		if (!skillData.Strength.Enabled)
		{
			return true;
		}
		
		var skillMgrExt = GameUtils.GetSkillManager()!.SkillManagerExtended;

		MovementContext.Struct333 gStruct;
		gStruct.movementContext_0 = __instance;
		gStruct.conditions = EPhysicalCondition.None;

		var flag = false;
		foreach (var collider in __instance.EnteredObstacles)
		{
			gStruct.conditions |= collider.ConditionsMask;
			flag |= collider.HasSwampSpeedLimit;
		}
		
		__instance.method_28(EPhysicalCondition.ProneDisabled, ref gStruct);
		__instance.method_28(EPhysicalCondition.ProneMovementDisabled, ref gStruct);

		var bushSpeedElite = skillMgrExt.StrengthBushSpeedIncBuffElite;
		if (!bushSpeedElite)
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
			var speedLimit = bushSpeedElite.Value 
				? 1f
				: 0.2f * (1 + skillMgrExt.StrengthBushSpeedIncBuff);

#if DEBUG
			Logger.LogDebug($"Collider speed limit: {speedLimit} :: IsElite {bushSpeedElite.Value}");
#endif
			
			__instance.AddStateSpeedLimit(speedLimit, Player.ESpeedLimit.Swamp);
			return false;
		}
		
		__instance.RemoveStateSpeedLimit(Player.ESpeedLimit.Swamp);
		return false;
	}
}