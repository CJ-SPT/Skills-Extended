using HarmonyLib;

using BepInEx.Logging;

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
	public class ActiveHealthControllerCTORPatch : ModulePatch
	{
		protected override MethodBase GetTargetMethod() =>
			typeof(ActiveHealthController).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0];

		[PatchPostfix]
		public static void Postfix(
			ActiveHealthController __instance,
			Player player,
			Profile.ProfileHealthClass profileHealth,
			InventoryController inventory,
			SkillManager skills)
		{
#if DEBUG
			Logger.LogInfo("[ActiveHealthControllerCTORPatch] ActiveHealthController constructor applied postfix.");
#endif

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