using HarmonyLib;

using SPT.Reflection.Patching;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;

namespace SkillsExtended.Skills.Core.Patches
{
	public class ActiveHealthControllerCTORPatch
	{
		protected virtual MethodBase GetTargetMethod() =>
			typeof(ActiveHealthController).GetConstructor(
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null,
				new Type[] { typeof(Player), typeof(Profile.ProfileHealthClass), typeof(InventoryController), typeof(SkillManager) },
				null);

		[PatchPostfix]
		public static void Postfix(
			ActiveHealthController __instance,
			Player player,
			Profile.ProfileHealthClass profileHealth,
			InventoryController inventory,
			SkillManager skills)
		{
			if (!player.IsYourPlayer) return;
			var skillData = SkillsPlugin.SkillData;
			var skillMgrExt = skills.SkillManagerExtended;
			if (skillData.Endurance.Enabled)
			{
				LimbsHPBuff.LimbsHPBuff.LegsHPBuff(player, skillData);
			}
			if (skillData.Strength.Enabled)
			{
				LimbsHPBuff.LimbsHPBuff.ArmsHPBuff(player, skillData);
			}
			if (skillData.Vitality.Enabled)
			{
				LimbsHPBuff.LimbsHPBuff.TorsoHPBuff(player, skillData);
			}
			if (skillData.Health.Enabled)
			{
				LimbsHPBuff.LimbsHPBuff.HeadHPBuff(player, skillData);
			}
		}
	}
}

//ActiveHealthController(Player player, Profile.ProfileHealthClass profileHealth, InventoryController inventory, SkillManager skills)