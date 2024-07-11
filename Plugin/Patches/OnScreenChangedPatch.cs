using EFT.UI;
using EFT.UI.Screens;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;

namespace SkillsExtended.Patches
{
    internal class OnScreenChangePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(MenuTaskBar).GetMethod("OnScreenChanged");

        [PatchPrefix]
        public static void Prefix(EEftScreenType eftScreenType)
        {
            if (eftScreenType == EEftScreenType.Inventory)
            {
                if (Plugin.SkillData.MedicalSkills.EnableFieldMedicine)
                {
                    Plugin.FieldMedicineScript.fieldMedicineInstanceIDs.Clear();

                    Plugin.FieldMedicineScript.FieldMedicineUpdate();
                }

                if (Plugin.SkillData.MedicalSkills.EnableFirstAid)
                {
                    Plugin.FirstAidScript.firstAidInstanceIDs.Clear();

                    Plugin.FirstAidScript.FirstAidUpdate();
                }

                if (Plugin.SkillData.UsecRifleSkill.Enabled)
                {
                    Plugin.UsecRifleScript.weaponInstanceIds.Clear();

                    var usecWeapons = Plugin.SkillData.UsecRifleSkill;

                    Plugin.UsecRifleScript.usecWeapons = Plugin.Session.Profile.Inventory.AllRealPlayerItems
                        .Where(x => usecWeapons.Weapons.Contains(x.TemplateId));
                }

                if (Plugin.SkillData.BearRifleSkill.Enabled)
                {
                    Plugin.BearRifleScript.weaponInstanceIds.Clear();

                    var bearWeapons = Plugin.SkillData.BearRifleSkill;

                    Plugin.BearRifleScript.bearWeapons = Plugin.Session.Profile.Inventory.AllRealPlayerItems
                        .Where(x => bearWeapons.Weapons.Contains(x.TemplateId));
                }

                Utils.CheckServerModExists();
            }
        }
    }
}