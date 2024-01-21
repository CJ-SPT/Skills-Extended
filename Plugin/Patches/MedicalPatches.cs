using EFT;
using HarmonyLib;
using EFT.HealthSystem;
using System.Reflection;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;
using Aki.Reflection.Patching;
using SkillsExtended.Controllers;
using EFT.UI;
using EFT.UI.Screens;
using System;
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
            public static void Prefix(ref Item item)
            {
                // We dont want to alter surgery with the first aid skill
                if (item is MedsClass meds)
                {
                    var healthEffectComp = meds.HealthEffectsComponent;

                    // Surgery item, dont adjust time
                    if (healthEffectComp.AffectsAny(EDamageEffectType.DestroyedPart))
                    {
                        Plugin.Log.LogDebug("Surgery effect, skipping time modification");
                    }       
                }

                if (MedicalBehavior.originalFieldMedicineUseTimes.ContainsKey(item.TemplateId))
                {
                    Plugin.MedicalScript.ApplyFieldMedicineExp();
                    Plugin.Log.LogDebug("Field Medicine Effect");
                    return;
                }

                Plugin.MedicalScript.ApplyFirstAidExp();
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
                string firstAid = @"\bfirst aid\b";
                string fieldMedicine = @"\bField Medicine\b";

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