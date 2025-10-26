using EFT;
using EFT.HealthSystem;

using HarmonyLib;

using SPT.Reflection.Patching;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SkillsExtended.Skills.Core.Patches
{
	public class PlayerOnSkillLevelChangedPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(Player), nameof(Player.OnSkillLevelChanged));
		}

		[PatchPrefix]
		public static void Prefix(Player __instance, AbstractSkillClass skill, IHealthController ____healthController)
		{
			if (!__instance.IsYourPlayer) return;

			var skillData = SkillsPlugin.SkillData;
			var skillId = skill.Id;

			switch (skillId)
			{
				case ESkillId.Strength:
					if (skillData.Strength.Enabled)
						LimbsHPBuff.LimbsHPBuff.ArmsHPBuff(__instance, skillData);
					break;
				case ESkillId.Endurance:
					if (skillData.Endurance.Enabled)
						LimbsHPBuff.LimbsHPBuff.LegsHPBuff(__instance, skillData);
					break;
				case ESkillId.Vitality:
					if (skillData.Vitality.Enabled)
						LimbsHPBuff.LimbsHPBuff.TorsoHPBuff(__instance, skillData);
					break;
				case ESkillId.Health:
					if (skillData.Health.Enabled)
						LimbsHPBuff.LimbsHPBuff.HeadHPBuff(__instance, skillData);
					break;
			}

			if (____healthController is HealthControllerClass)
			{
				Profile profile = __instance.Profile;

				profile.Health = (____healthController as HealthControllerClass).Store();

				SkillsPlugin.Log.LogInfo($"[PlayerOnSkillLevelChangedPatch] Updated player health profile after {skillId} skill level changed.");
			}
		}
	}
}
