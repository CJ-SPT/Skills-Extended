using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using EFT.HealthSystem;
using HarmonyLib;

namespace SkillsExtended.Patches;

internal class DoMedEffectPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.DoMedEffect));
    }

    [PatchPrefix]
    public static void Prefix(ActiveHealthController __instance, Item item, EBodyPart bodyPart)
    {
        // Don't give xp for surgery
        if (item.TemplateId == "5d02778e86f774203e7dedbe" || item.TemplateId == "5d02797c86f774203f38e30a")
        {
            return;
        }

        if (!__instance.Player.IsYourPlayer)
        {
            return;
        }

        if (Plugin.SkillData.MedicalSkills.FmItemList.Contains(item.TemplateId) && Plugin.SkillData.MedicalSkills.EnableFieldMedicine)
        {
            Plugin.FieldMedicineScript.ApplyFieldMedicineExp(bodyPart);
            return;
        }

        if (Plugin.SkillData.MedicalSkills.FaItemList.Contains(item.TemplateId) && Plugin.SkillData.MedicalSkills.EnableFirstAid)
        {
            Plugin.FirstAidScript.ApplyFirstAidExp(bodyPart);
        }
    }
}
