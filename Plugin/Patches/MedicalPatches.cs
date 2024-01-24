using Aki.Reflection.Patching;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using SkillsExtended.Controllers;
using System.Reflection;

namespace SkillsExtended.Patches
{
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
}