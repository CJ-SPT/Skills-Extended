using EFT;

using HarmonyLib;

using SPT.Reflection.Patching;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SkillsExtended.Skills.Core.Patches
{
	public class MainMenuControllerClassMethod5Patch : ModulePatch
	{
		private static bool isPatched = false;
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(MainMenuControllerClass), nameof(MainMenuControllerClass.method_5));
		}

		[PatchPrefix]
		public static void Prefix(MainMenuControllerClass __instance)
		{
			Profile profile = __instance.ISession.Profile;

			if (profile == null)
			{
				UnityEngine.Debug.LogError("[MainMenuControllerClass] Profile is null");
			}

			if (isPatched)
				return;

			var skillData = SkillsPlugin.SkillData;
			var skillMgrExt = __instance.ISession.Profile.Skills.SkillManagerExtended;

			if (skillData.Strength.Enabled)
			{
				LimbsHPBuff.LimbsHPBuff.ArmsHPBuff(profile.Health.BodyParts[EBodyPart.RightArm], skillData, skillMgrExt);
				LimbsHPBuff.LimbsHPBuff.ArmsHPBuff(profile.Health.BodyParts[EBodyPart.LeftArm], skillData, skillMgrExt);
			}
			if (skillData.Endurance.Enabled)
			{
				LimbsHPBuff.LimbsHPBuff.LegsHPBuff(profile.Health.BodyParts[EBodyPart.RightLeg], skillData, skillMgrExt);
				LimbsHPBuff.LimbsHPBuff.LegsHPBuff(profile.Health.BodyParts[EBodyPart.LeftLeg], skillData, skillMgrExt);
			}
			if (skillData.Vitality.Enabled)
			{
				LimbsHPBuff.LimbsHPBuff.ThoraxHPBuff(profile.Health.BodyParts[EBodyPart.Chest], skillData, skillMgrExt);
				LimbsHPBuff.LimbsHPBuff.StomachHPBuff(profile.Health.BodyParts[EBodyPart.Stomach], skillData, skillMgrExt);
			}
			if (skillData.Health.Enabled)
			{
				LimbsHPBuff.LimbsHPBuff.HeadHPBuff(profile.Health.BodyParts[EBodyPart.Head], skillData, skillMgrExt);
			}

			isPatched = true;

			//SkillsPlugin.Log.LogInfo($"{__instance.HealthControllerClass.ToString()}");


			//profile.Health = __instance.HealthControllerClass.Store(profile.Health);

			//SkillsPlugin.Log.LogInfo($"[MainMenuControllerClassMethod5] Updated player health profile on main menu load.");
		}
	}
}
