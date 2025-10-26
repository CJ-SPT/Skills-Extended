using BepInEx.Logging;

using EFT;
using EFT.HealthSystem;

using SkillsExtended.Skills.Core;

using System;
using System.Collections.Generic;
using System.Text;

using static EFT.Profile.ProfileHealthClass;
using static EFT.SkillManager;

namespace SkillsExtended.Skills.LimbsHPBuff
{
	internal static class LimbsHPBuff
	{
		internal static void ArmsHPBuff(Player instance, Models.SkillDataResponse skillData)
		{
			var skillMgrExt = instance.Skills.SkillManagerExtended;
			ApplyLimbsHPBuff(
				instance,
				EBodyPart.LeftArm,
				skillData.Strength.BaseArmsHp,
				(!skillMgrExt.StrengthArmsHPBuffElite ? skillMgrExt.StrengthArmsHPBuff : skillMgrExt.StrengthArmsHPBuff + skillData.Strength.ArmsHpElite)
			);
			ApplyLimbsHPBuff(
				instance,
				EBodyPart.RightArm,
				skillData.Strength.BaseArmsHp,
				(!skillMgrExt.StrengthArmsHPBuffElite ? skillMgrExt.StrengthArmsHPBuff : skillMgrExt.StrengthArmsHPBuff + skillData.Strength.ArmsHpElite)
			);
		}

		internal static void ArmsHPBuff(ProfileBodyPartHealthClass limbHealthClass, Models.SkillDataResponse skillData, SkillManagerExt skillMgrExt)
		{
			ApplyLimbsHPBuff(
				limbHealthClass,
				skillData.Strength.BaseArmsHp,
				(!skillMgrExt.StrengthArmsHPBuffElite ? skillMgrExt.StrengthArmsHPBuff : skillMgrExt.StrengthArmsHPBuff + skillData.Strength.ArmsHpElite)
			);
		}

		internal static void LegsHPBuff(Player instance, Models.SkillDataResponse skillData)
		{
			var skillMgrExt = instance.Skills.SkillManagerExtended;
			ApplyLimbsHPBuff(
				instance,
				EBodyPart.LeftLeg,
				skillData.Endurance.BaseLegsHp,
				(!skillMgrExt.EnduranceLegsHPBuffElite ? skillMgrExt.EnduranceLegsHPBuff : skillMgrExt.EnduranceLegsHPBuff + skillData.Endurance.LegsHpElite)
			);
			ApplyLimbsHPBuff(
				instance,
				EBodyPart.RightLeg,
				skillData.Endurance.BaseLegsHp,
				(!skillMgrExt.EnduranceLegsHPBuffElite ? skillMgrExt.EnduranceLegsHPBuff : skillMgrExt.EnduranceLegsHPBuff + skillData.Endurance.LegsHpElite)
			);
		}

		internal static void LegsHPBuff(ProfileBodyPartHealthClass limbHealthClass, Models.SkillDataResponse skillData, SkillManagerExt skillMgrExt)
		{
			ApplyLimbsHPBuff(
				limbHealthClass,
				skillData.Endurance.BaseLegsHp,
				(!skillMgrExt.EnduranceLegsHPBuffElite ? skillMgrExt.EnduranceLegsHPBuff : skillMgrExt.EnduranceLegsHPBuff + skillData.Endurance.LegsHpElite)
			);
		}

		internal static void TorsoHPBuff(Player instance, Models.SkillDataResponse skillData)
		{
			var skillMgrExt = instance.Skills.SkillManagerExtended;
			ApplyLimbsHPBuff(
				instance,
				EBodyPart.Chest,
				skillData.Vitality.BaseThoraxHp,
				(!skillMgrExt.VitalityTorsoHPBuffElite ? skillMgrExt.VitalityTorsoHPBuff : skillMgrExt.VitalityTorsoHPBuff + skillData.Vitality.TorsoHpElite)
			);
			ApplyLimbsHPBuff(
				instance,
				EBodyPart.Stomach,
				skillData.Vitality.BaseStomachHp,
				(!skillMgrExt.VitalityTorsoHPBuffElite ? skillMgrExt.VitalityTorsoHPBuff : skillMgrExt.VitalityTorsoHPBuff + skillData.Vitality.TorsoHpElite)
			);
		}

		internal static void ThoraxHPBuff(ProfileBodyPartHealthClass limbHealthClass, Models.SkillDataResponse skillData, SkillManagerExt skillMgrExt)
		{
			ApplyLimbsHPBuff(
				limbHealthClass,
				skillData.Vitality.BaseThoraxHp,
				(!skillMgrExt.VitalityTorsoHPBuffElite ? skillMgrExt.VitalityTorsoHPBuff : skillMgrExt.VitalityTorsoHPBuff + skillData.Vitality.TorsoHpElite)
			);
		}

		internal static void StomachHPBuff(ProfileBodyPartHealthClass limbHealthClass, Models.SkillDataResponse skillData, SkillManagerExt skillMgrExt)
		{
			ApplyLimbsHPBuff(
				limbHealthClass,
				skillData.Vitality.BaseStomachHp,
				(!skillMgrExt.VitalityTorsoHPBuffElite ? skillMgrExt.VitalityTorsoHPBuff : skillMgrExt.VitalityTorsoHPBuff + skillData.Vitality.TorsoHpElite)
			);
		}

		internal static void HeadHPBuff(Player instance, Models.SkillDataResponse skillData)
		{
			var skillMgrExt = instance.Skills.SkillManagerExtended;
			ApplyLimbsHPBuff(
				instance,
				EBodyPart.Head,
				skillData.Health.BaseHeadHp,
				(!skillMgrExt.HealthHeadHPBuffElite ? skillMgrExt.HealthHeadHPBuff : skillMgrExt.HealthHeadHPBuff + skillData.Health.HeadHpElite)
			);
		}

		internal static void HeadHPBuff(ProfileBodyPartHealthClass limbHealthClass, Models.SkillDataResponse skillData, SkillManagerExt skillMgrExt)
		{
			ApplyLimbsHPBuff(
				limbHealthClass,
				skillData.Health.BaseHeadHp,
				(!skillMgrExt.HealthHeadHPBuffElite ? skillMgrExt.HealthHeadHPBuff : skillMgrExt.HealthHeadHPBuff + skillData.Health.HeadHpElite)
			);
		}

		internal static void ApplyLimbsHPBuff(Player player, EBodyPart limb, float limbBaseHP, float limbHPBuffFactor)
		{
			GClass3009<ActiveHealthController.GClass3008>.BodyPartState bodyPartState = player.ActiveHealthController.Dictionary_0[limb];

			int newLimbMaxHP = ((int)Math.Ceiling(limbBaseHP * (1 + limbHPBuffFactor)));

			bodyPartState.Health = new HealthValue(Math.Min(bodyPartState.Health.Current, newLimbMaxHP), newLimbMaxHP);

#if DEBUG
			Logger.CreateLogSource("SkillsExtended").LogInfo($"[LimbsHPBuff] Applied {limb.ToString()} HPBuff with {limbHPBuffFactor} factor. New Max HP: {newLimbMaxHP}, Current HP: {bodyPartState.Health.Current}/{bodyPartState.Health.Maximum}");
#endif
		}

		internal static void ApplyLimbsHPBuff(ProfileBodyPartHealthClass limbHealthClass, float limbBaseHP, float limbHPBuffFactor)
		{
			int newLimbMaxHP = ((int)Math.Ceiling(limbBaseHP * (1 + limbHPBuffFactor)));

			limbHealthClass.Health.Maximum = newLimbMaxHP;

			Logger.CreateLogSource("SkillsExtended").LogInfo($"[LimbsHPBuff] Applied to LimbProfile HPBuff with {limbHPBuffFactor} factor. New Max HP: {newLimbMaxHP}, Current HP: {limbHealthClass.Health.Current}/{limbHealthClass.Health.Maximum}");
		}
	}
}
