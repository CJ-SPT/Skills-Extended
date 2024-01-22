using Aki.Reflection.Patching;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Screens;
using HarmonyLib;
using SkillsExtended.Controllers;
using SkillsExtended.Helpers;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SkillsExtended.Patches
{
    internal class MedicalPatches
    {
        internal class EnableSkillsPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(SkillManager).GetMethod("method_3", BindingFlags.NonPublic | BindingFlags.Instance);

            [PatchPostfix]
            public static void Postfix(SkillManager __instance)
            {
                AccessTools.Field(Utils.GetSkillType(), "Locked").SetValue(__instance.FirstAid, false);
                AccessTools.Field(Utils.GetSkillType(), "Locked").SetValue(__instance.FieldMedicine, false);
            }
        }

        internal class DoMedEffectPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(ActiveHealthController).GetMethod("DoMedEffect");

            [PatchPrefix]
            public static void Prefix(ref Item item, EBodyPart bodyPart)
            {
                // Dont give xp for surgery
                if (item.TemplateId == "5d02778e86f774203e7dedbe" || item.TemplateId == "5d02797c86f774203f38e30a")
                {
                    return;
                }

                if (MedicalBehavior.originalFieldMedicineUseTimes.ContainsKey(item.TemplateId))
                {
                    Plugin.MedicalScript.ApplyFieldMedicineExp(bodyPart);
                    Plugin.Log.LogDebug("Field Medicine Effect");
                    return;
                }

                Plugin.MedicalScript.ApplyFirstAidExp(bodyPart);
            }
        }

        internal class OnScreenChangePatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(MenuTaskBar).GetMethod("OnScreenChanged");

            [PatchPrefix]
            public static void Prefix(EEftScreenType eftScreenType)
            {
                if (eftScreenType == EEftScreenType.Inventory)
                {
                    Plugin.MedicalScript.fieldMedicineInstanceIDs.Clear();
                    Plugin.MedicalScript.firstAidInstanceIDs.Clear();
                    Utils.GetServerConfig();
                }
            }
        }

        internal class SimpleToolTipPatch : ModulePatch
        {
            protected override MethodBase GetTargetMethod() =>
                typeof(SimpleTooltip).GetMethod("Show");

            [PatchPostfix]
            public static void Postfix(SimpleTooltip __instance, ref string text)
            {
                string firstAid = @"\bFirstAidDescriptionPattern\b";
                string fieldMedicine = @"\bFieldMedicineDescriptionPattern\b";

                if (Regex.IsMatch(text, firstAid))
                {
                    var speedBonus = MedicalBehavior.playerSkillManager.FirstAid.Level * 0.007f;
                    var hpBonus = MedicalBehavior.playerSkillManager.FirstAid.Level * 5f;

                    if (MedicalBehavior.playerSkillManager.FirstAid.IsEliteLevel)
                    {
                        speedBonus += 0.15f;
                        hpBonus = MedicalBehavior.playerSkillManager.FirstAid.Level * 10f;
                    }

                    __instance.SetText($"First aid skills make use of first aid kits quicker and more effective." +
                        $"\n\n Increases the speed of healing items by 0.7% per level. \n\n Elite bonus: 15% \n\n Increases the HP resource of medical items by 5 per level. \n\n Elite bonus: 10 per level." +
                        $"\n\n Current speed bonus: <color=red>{speedBonus * 100}%</color> \n\n Current bonus HP: <color=red>{hpBonus}</color>");
                }

                if (Regex.IsMatch(text, fieldMedicine))
                {
                    var speedBonus = MedicalBehavior.playerSkillManager.FieldMedicine.Level * 0.007f;

                    if (MedicalBehavior.playerSkillManager.FirstAid.IsEliteLevel)
                    {
                        speedBonus += 0.15f;
                    }

                    __instance.SetText($"Field Medicine increases your skill at applying wound dressings. \n\n Increases the speed of splints, bandages, and heavy bleed items 0.7% per level. \n\n Elite bonus: 15% " +
                        $"\n\n Current speed bonus: <color=red>{speedBonus * 100}%</color>");
                }
            }
        }
    }
}