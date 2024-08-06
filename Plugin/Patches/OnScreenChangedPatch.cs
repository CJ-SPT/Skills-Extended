using EFT.UI;
using EFT.UI.Screens;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using EFT;

namespace SkillsExtended.Patches
{
    internal class OnScreenChangePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(MenuTaskBar).GetMethod("OnScreenChanged");

        [PatchPrefix]
        public static void Prefix(EEftScreenType eftScreenType)
        {
            if (Plugin.SkillData.MedicalSkills.EnableFieldMedicine)
            {
                Plugin.FieldMedicineScript.FieldMedicineInstanceIDs.Clear();

                StaticManager.BeginCoroutine(Plugin.FieldMedicineScript.FieldMedicineUpdate());
            }

            if (Plugin.SkillData.MedicalSkills.EnableFirstAid)
            {
                Plugin.FirstAidScript.FirstAidInstanceIDs.Clear();

                StaticManager.BeginCoroutine(Plugin.FirstAidScript.FirstAidUpdate());
            }

            if (Plugin.SkillData.NatoRifleSkill.Enabled)
            {
                Plugin.NatoWeaponScript.WeaponInstanceIds.Clear();

                var usecWeapons = Plugin.SkillData.NatoRifleSkill;

                Plugin.NatoWeaponScript.UsecWeapons = Plugin.Session.Profile.Inventory.AllRealPlayerItems
                    .Where(x => usecWeapons.Weapons.Contains(x.TemplateId));

                StaticManager.BeginCoroutine(Plugin.NatoWeaponScript.UpdateWeapons());
            }

            if (Plugin.SkillData.EasternRifleSkill.Enabled)
            {
                Plugin.EasternWeaponScript.WeaponInstanceIds.Clear();

                var bearWeapons = Plugin.SkillData.EasternRifleSkill;

                Plugin.EasternWeaponScript.BearWeapons = Plugin.Session.Profile.Inventory.AllRealPlayerItems
                    .Where(x => bearWeapons.Weapons.Contains(x.TemplateId));
                    
                StaticManager.BeginCoroutine(Plugin.EasternWeaponScript.UpdateWeapons());
            }
        }
    }
}