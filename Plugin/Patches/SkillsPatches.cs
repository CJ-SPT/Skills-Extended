using EFT;
using EFT.UI;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using System;
using System.Reflection;
using TMPro;

namespace SkillsExtended.Patches
{
    internal class SkillManagerConstructorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SkillManager).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [typeof(EPlayerSide)], null);

        [PatchPostfix]
        public static void Postfix(SkillManager __instance, ref SkillClass[] ___DisplayList, ref SkillClass[] ___Skills)
        {
            int insertIndex = 12;

            __instance.UsecArsystems = new SkillClass(__instance, ESkillId.UsecArsystems, ESkillClass.Special, [], []);
            __instance.BearAksystems = new SkillClass(__instance, ESkillId.BearAksystems, ESkillClass.Special, [], []);

            __instance.UsecTactics = new SkillClass(__instance, ESkillId.UsecTactics, ESkillClass.Special, [], []);
            __instance.BearRawpower = new SkillClass(__instance, ESkillId.BearRawpower, ESkillClass.Special, [], []);

            var newDisplayList = new SkillClass[___DisplayList.Length + 5];

            Array.Copy(___DisplayList, newDisplayList, insertIndex);

            newDisplayList[12] = __instance.UsecArsystems;
            newDisplayList[12 + 1] = __instance.BearAksystems;

            newDisplayList[12 + 2] = __instance.UsecTactics;
            newDisplayList[12 + 3] = __instance.BearRawpower;
            newDisplayList[12 + 4] = __instance.Lockpicking;

            Array.Copy(___DisplayList, insertIndex, newDisplayList, insertIndex + 5, ___DisplayList.Length - insertIndex);

            ___DisplayList = newDisplayList;

            Array.Resize(ref ___Skills, ___Skills.Length + 5);

            ___Skills[___Skills.Length - 1] = __instance.UsecArsystems;
            ___Skills[___Skills.Length - 2] = __instance.BearAksystems;

            ___Skills[___Skills.Length - 3] = __instance.UsecTactics;
            ___Skills[___Skills.Length - 4] = __instance.BearRawpower;
            ___Skills[___Skills.Length - 5] = __instance.Lockpicking;

            // If the skill is not enabled, lock it
            AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.UsecArsystems,
                !Plugin.SkillData.UsecRifleSkill.Enabled);

            AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.BearAksystems,
                !Plugin.SkillData.BearRifleSkill.Enabled);

            AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.UsecTactics,
                !Plugin.SkillData.UsecTacticsSkill.Enabled);

            AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.BearRawpower,
                !Plugin.SkillData.BearRawPowerSkill.Enabled);

            AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.Lockpicking,
                !Plugin.SkillData.LockPickingSkill.Enabled);

            AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.FirstAid,
                !Plugin.SkillData.MedicalSkills.EnableFirstAid);

            AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.FieldMedicine,
                !Plugin.SkillData.MedicalSkills.EnableFieldMedicine);
        }
    }

    internal class SkillToolTipPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SkillTooltip).GetMethods().SingleCustom(x => x.Name == "Show" && x.GetParameters().Length == 1);

        private static SkillDataResponse _skillData => Plugin.SkillData;

        [PatchPostfix]
        public static void Postfix(
            SkillTooltip __instance,
            SkillClass skill,
            ref TextMeshProUGUI ____name,
            ref TextMeshProUGUI ____description)
        {
            if (skill.Id == ESkillId.FirstAid)
            {
                var firstAidSkill = Plugin.Session.Profile.Skills.FirstAid;

                float speedBonus = firstAidSkill.IsEliteLevel
                    ? (firstAidSkill.Level * _skillData.MedicalSkills.MedicalSpeedBonus) - _skillData.MedicalSkills.MedicalSpeedBonusElite
                    : (firstAidSkill.Level * _skillData.MedicalSkills.MedicalSpeedBonus);

                float hpBonus = firstAidSkill.IsEliteLevel
                    ? firstAidSkill.Level * _skillData.MedicalSkills.MedkitHpBonus + _skillData.MedicalSkills.MedkitHpBonusElite
                    : firstAidSkill.Level * _skillData.MedicalSkills.MedkitHpBonus;

                ____description.SetText(SkillDescriptions.FirstAidDescription(speedBonus, hpBonus));
            }

            if (skill.Id == ESkillId.FieldMedicine)
            {
                var fieldMedicineSkill = Plugin.Session.Profile.Skills.FieldMedicine;

                float speedBonus = fieldMedicineSkill.IsEliteLevel
                    ? (fieldMedicineSkill.Level * _skillData.MedicalSkills.MedicalSpeedBonus) - _skillData.MedicalSkills.MedicalSpeedBonusElite
                    : (fieldMedicineSkill.Level * _skillData.MedicalSkills.MedicalSpeedBonus);

                ____description.SetText(SkillDescriptions.FieldMedicineDescription(speedBonus));
            }

            if (skill.Id == ESkillId.UsecArsystems)
            {
                var usecSystems = Plugin.Session.Profile.Skills.UsecArsystems;

                float ergoBonus = usecSystems.IsEliteLevel
                    ? usecSystems.Level * _skillData.UsecRifleSkill.ErgoMod + _skillData.UsecRifleSkill.ErgoModElite
                    : usecSystems.Level * _skillData.UsecRifleSkill.ErgoMod;

                float recoilReduction = usecSystems.IsEliteLevel
                    ? usecSystems.Level * _skillData.UsecRifleSkill.RecoilReduction + _skillData.UsecRifleSkill.RecoilReductionElite
                    : usecSystems.Level * _skillData.UsecRifleSkill.RecoilReduction;

                ____name.SetText("USEC rifle and carbine proficiency");
                ____description.SetText(SkillDescriptions.UsecArSystemsDescription(ergoBonus, recoilReduction));
            }

            if (skill.Id == ESkillId.BearAksystems)
            {
                var bearSystems = Plugin.Session.Profile.Skills.BearAksystems;

                float ergoBonus = bearSystems.IsEliteLevel
                    ? bearSystems.Level * _skillData.BearRifleSkill.ErgoMod + _skillData.BearRifleSkill.ErgoModElite
                    : bearSystems.Level * _skillData.BearRifleSkill.ErgoMod;

                float recoilReduction = bearSystems.IsEliteLevel
                    ? bearSystems.Level * _skillData.BearRifleSkill.RecoilReduction + _skillData.BearRifleSkill.RecoilReductionElite
                    : bearSystems.Level * _skillData.BearRifleSkill.RecoilReduction;

                ____name.SetText("BEAR rifle and carbine proficiency");
                ____description.SetText(SkillDescriptions.BearAkSystemsDescription(ergoBonus, recoilReduction));
            }

            if (skill.Id == ESkillId.Lockpicking)
            {
                var lockPickingSkill = Plugin.Session.Profile.Skills.Lockpicking;

                float timeReduction = lockPickingSkill.IsEliteLevel
                    ? lockPickingSkill.Level * _skillData.LockPickingSkill.TimeReduction + _skillData.LockPickingSkill.TimeReductionElite
                    : lockPickingSkill.Level * _skillData.LockPickingSkill.TimeReduction;

                ____name.SetText("Lockpicking");
                ____description.SetText(SkillDescriptions.LockPickingDescription(timeReduction));
            }

            if (skill.Id == ESkillId.UsecTactics)
            {
                var usecTacticsSkill = Plugin.Session.Profile.Skills.UsecTactics;

                float inertiaReduction = usecTacticsSkill.IsEliteLevel
                    ? usecTacticsSkill.Level * _skillData.UsecTacticsSkill.InertiaRedBonus + _skillData.UsecTacticsSkill.InertiaRedBonusElite
                    : usecTacticsSkill.Level * _skillData.UsecTacticsSkill.InertiaRedBonus;

                ____name.SetText("USEC Tactics");
                ____description.SetText(SkillDescriptions.UsecTacticsDescription(inertiaReduction));
            }

            if (skill.Id == ESkillId.BearRawpower)
            {
                var bearRawpowerSkill = Plugin.Session.Profile.Skills.BearRawpower;

                float hpBonus = bearRawpowerSkill.IsEliteLevel
                    ? bearRawpowerSkill.Level * _skillData.BearRawPowerSkill.HPBonus + _skillData.BearRawPowerSkill.HPBonusElite
                    : bearRawpowerSkill.Level * _skillData.BearRawPowerSkill.HPBonus;

                ____name.SetText("BEAR raw power");
                ____description.SetText(SkillDescriptions.BearRawpowerDescription(hpBonus));
            }
        }
    }

    internal class SkillPanelDisablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SkillPanel).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);

        [PatchPrefix]
        public static bool Prefix(SkillClass skill)
        {
            var skills = Plugin.Session.Profile.Skills;
            var side = Plugin.Session.Profile.Side;

            if (skill.Locked)
            {
                // Skip original method and dont show skill
                return false;
            }

            // Usec AR systems
            if (skill.Id == ESkillId.UsecArsystems && side == EPlayerSide.Bear && !skills.BearAksystems.IsEliteLevel)
            {
                if (Plugin.SkillData.DisableEliteRequirements)
                {
                    return true;
                }

                // Skip original method and dont show skill
                return false;
            }

            // Usec Tactics
            if (skill.Id == ESkillId.UsecTactics && side == EPlayerSide.Bear)
            {
                // Skip original method and dont show skill
                return false;
            }

            // Bear AK systems
            if (skill.Id == ESkillId.BearAksystems && side == EPlayerSide.Usec && !skills.UsecArsystems.IsEliteLevel)
            {
                if (Plugin.SkillData.DisableEliteRequirements)
                {
                    return true;
                }

                // Skip original method and dont show skill
                return false;
            }

            // Bear Raw Power
            if (skill.Id == ESkillId.BearRawpower && side == EPlayerSide.Usec)
            {
                // Skip original method and dont show skill
                return false;
            }

            // Show the skill
            return true;
        }
    }

    internal class SkillPanelNamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SkillPanel).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);

        [PatchPostfix]
        public static void Postfix(SkillPanel __instance, SkillClass skill)
        {
            if (skill.Id == ESkillId.UsecArsystems)
            {
                TextMeshProUGUI name = (TextMeshProUGUI)AccessTools.Field(typeof(SkillPanel), "_name").GetValue(__instance);
                name.text = "USEC rifle and carbine proficiency";
            }

            if (skill.Id == ESkillId.UsecTactics)
            {
                TextMeshProUGUI name = (TextMeshProUGUI)AccessTools.Field(typeof(SkillPanel), "_name").GetValue(__instance);
                name.text = "USEC Tactics";
            }

            if (skill.Id == ESkillId.BearAksystems)
            {
                TextMeshProUGUI name = (TextMeshProUGUI)AccessTools.Field(typeof(SkillPanel), "_name").GetValue(__instance);
                name.text = "BEAR rifle and carbine proficiency";
            }

            if (skill.Id == ESkillId.BearRawpower)
            {
                TextMeshProUGUI name = (TextMeshProUGUI)AccessTools.Field(typeof(SkillPanel), "_name").GetValue(__instance);
                name.text = "BEAR Raw Power";
            }
        }
    }
}