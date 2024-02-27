using Aki.Reflection.Patching;
using EFT;
using System.Linq;
using System.Reflection;

namespace SkillsExtended.Patches
{
    internal class DoMedEffectPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethods().First(m =>
                m.Name == "SetInHands" && m.GetParameters()[0].Name == "meds");
        }

        [PatchPrefix]
        public static void Prefix(Player __instance, MedsClass meds, EBodyPart bodyPart)
        {
            // Dont give xp for surgery
            if (meds.TemplateId == "5d02778e86f774203e7dedbe" || meds.TemplateId == "5d02797c86f774203f38e30a")
            {
                return;
            }

            if (!__instance.IsYourPlayer)
            {
                return;
            }

            if (Plugin.MedicalScript.fieldMedicineItemList.Contains(meds.TemplateId))
            {
                Plugin.MedicalScript.ApplyFieldMedicineExp(bodyPart);
                Plugin.Log.LogDebug("Field Medicine Effect");
                return;
            }

            Plugin.MedicalScript.ApplyFirstAidExp(bodyPart);
        }
    }
}