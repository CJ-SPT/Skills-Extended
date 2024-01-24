using Aki.Reflection.Patching;
using EFT;
using System.Reflection;
using SkillsExtended.Helpers;
using EFT.UI;
using EFT.InventoryLogic;

namespace SkillsExtended.Patches
{   
    internal class ItemAttributeDisplayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(ItemSpecificationPanel).GetMethod("method_16", BindingFlags.NonPublic | BindingFlags.Instance);

        [PatchPrefix]
        public static void Prefix(ref Item compareItem)
        {
            if (compareItem is Weapon weap)
            {
                // Change display params for USEC weapons only.
                if (Constants.USEC_WEAPON_LIST.Contains(compareItem.TemplateId))
                {
                    var skills = Plugin.Session.Profile.Skills;

                    var level = skills.UsecArsystems.Level;

                    var ergoBonus = skills.UsecArsystems.IsEliteLevel ? level * Constants.ERGO_MOD + Constants.ERGO_MOD_ELITE : level * Constants.ERGO_MOD;
                    var recoilReduction = skills.UsecArsystems.IsEliteLevel ? level * Constants.RECOIL_REDUCTION + Constants.RECOIL_REDUCTION_ELITE : level * Constants.RECOIL_REDUCTION;

                    weap.Template.Ergonomics *= (1 + ergoBonus);
                    weap.Template.RecoilForceUp *= (1 - recoilReduction);
                    weap.Template.RecoilForceBack *= (1 - recoilReduction);
                }

                // Change display params for BEAR weapons only.
                if (Constants.BEAR_WEAPON_LIST.Contains(compareItem.TemplateId))
                {
                    var skills = Plugin.Session.Profile.Skills;

                    var level = skills.BearAksystems.Level;

                    var ergoBonus = skills.BearAksystems.IsEliteLevel ? level * Constants.ERGO_MOD + Constants.ERGO_MOD_ELITE : level * Constants.ERGO_MOD;
                    var recoilReduction = skills.BearAksystems.IsEliteLevel ? level * Constants.RECOIL_REDUCTION + Constants.RECOIL_REDUCTION_ELITE : level * Constants.RECOIL_REDUCTION;

                    weap.Template.Ergonomics *= (1 + ergoBonus);
                    weap.Template.RecoilForceUp *= (1 - recoilReduction);
                    weap.Template.RecoilForceBack *= (1 - recoilReduction);
                }
            }            
        }
    }
}
