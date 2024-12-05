using EFT.UI;
using EFT.UI.Screens;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using EFT;
using SkillsExtended.Helpers;

namespace SkillsExtended.Skills.Shared.Patches;

internal class OnScreenChangePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(MenuTaskBar).GetMethod("OnScreenChanged");

    [PatchPrefix]
    public static void Prefix(EEftScreenType eftScreenType)
    {
        if (Plugin.SkillData.NatoRifle.Enabled)
        {
            Plugin.NatoWeaponScript.WeaponInstanceIds.Clear();

            var usecWeapons = Plugin.SkillData.NatoRifle;

            Plugin.NatoWeaponScript.UsecWeapons = GameUtils.GetProfile().Inventory.AllRealPlayerItems
                .Where(x => usecWeapons.Weapons.Contains(x.TemplateId));

            StaticManager.BeginCoroutine(Plugin.NatoWeaponScript.UpdateWeapons());
        }

        if (Plugin.SkillData.EasternRifle.Enabled)
        {
            Plugin.EasternWeaponScript.WeaponInstanceIds.Clear();

            var bearWeapons = Plugin.SkillData.EasternRifle;

            Plugin.EasternWeaponScript.BearWeapons = GameUtils.GetProfile().Inventory.AllRealPlayerItems
                .Where(x => bearWeapons.Weapons.Contains(x.TemplateId));
                
            StaticManager.BeginCoroutine(Plugin.EasternWeaponScript.UpdateWeapons());
        }
    }
}