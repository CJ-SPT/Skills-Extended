using EFT;
using HarmonyLib;
using EFT.HealthSystem;
using System.Reflection;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;
using Aki.Reflection.Patching;
using System.Collections.Generic;
using SkillsExtended.Controllers;
using System.Linq;
using EFT.UI;
using EFT.UI.Screens;

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
    }
}