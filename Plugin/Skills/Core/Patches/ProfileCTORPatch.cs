using SPT.Reflection.Patching;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using static EFT.Profile.ProfileHealthClass;

namespace SkillsExtended.Skills.Core.Patches
{
	public class ProfileCTORPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() =>
			typeof(EFT.Profile).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0];

		[PatchPostfix]
		public static void Postfix(EFT.Profile __instance)
		{
#if DEBUG
			Logger.LogInfo("[ProfileCTORPatch] Profile constructor applied postfix.");
#endif
			var skillData = SkillsPlugin.SkillData;
			var skillMgrExt = __instance.Skills.SkillManagerExtended;

#if DEBUG
			Logger.LogInfo("[ProfileCTORPatch] " + skillData.Strength.BaseArmsHp);
			Logger.LogInfo("[ProfileCTORPatch] " + skillData.Strength.ColliderSpeedBuff);
#endif

			foreach (var kvp in __instance.Health.BodyParts)
			{
				switch (kvp.Key)
				{
					case EBodyPart.LeftArm:
						LimbsHPBuff.LimbsHPBuff.ArmsHPBuff(kvp.Value, skillData, skillMgrExt);
						break;
					case EBodyPart.RightArm:
						LimbsHPBuff.LimbsHPBuff.ArmsHPBuff(kvp.Value, skillData, skillMgrExt);
						break;
					case EBodyPart.LeftLeg:
						LimbsHPBuff.LimbsHPBuff.LegsHPBuff(kvp.Value, skillData, skillMgrExt);
						break;
					case EBodyPart.RightLeg:
						LimbsHPBuff.LimbsHPBuff.LegsHPBuff(kvp.Value, skillData, skillMgrExt);
						break;
					case EBodyPart.Chest:
						LimbsHPBuff.LimbsHPBuff.ThoraxHPBuff(kvp.Value, skillData, skillMgrExt);
						break;
					case EBodyPart.Stomach:
						LimbsHPBuff.LimbsHPBuff.StomachHPBuff(kvp.Value, skillData, skillMgrExt);
						break;
					case EBodyPart.Head:
						LimbsHPBuff.LimbsHPBuff.HeadHPBuff(kvp.Value, skillData, skillMgrExt);
						break;
				}
			}
		}
	}
}
