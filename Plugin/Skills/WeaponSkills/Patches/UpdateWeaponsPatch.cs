using System.Collections;
using System.Collections.Generic;
using EFT.UI;
using EFT.UI.Screens;
using SPT.Reflection.Patching;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SkillsExtended.Skills.Core;

namespace SkillsExtended.Skills.WeaponSkills.Patches;

internal class UpdateWeaponsPatch : ModulePatch
{
    // Store an object containing the weapons original stats.
    private static readonly Dictionary<string, OrigWeaponValues> UsecOriginalWeaponValues = [];
    private static readonly Dictionary<string, int> UsecWeaponInstanceIds = [];
    
    private static readonly Dictionary<string, OrigWeaponValues> EasternOriginalWeaponValues = [];
    private static readonly Dictionary<string, int> EasternWeaponInstanceIds = [];
    
    private static SkillManager SkillManager => GameUtils.GetSkillManager();
    private static SkillManagerExt SkillMgrExt => SkillManagerExt.Instance(EPlayerSide.Usec);
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(MenuTaskBar), nameof(MenuTaskBar.OnScreenChanged));
    }
    
    [PatchPrefix]
    public static void Prefix(EEftScreenType eftScreenType)
    {
        if (SkillsPlugin.SkillData.NatoWeapons.Enabled)
        {
            UsecWeaponInstanceIds.Clear();
            
            StaticManager.BeginCoroutine(UpdateUsecWeapons());
        }

        if (SkillsPlugin.SkillData.EasternWeapons.Enabled)
        {
            EasternWeaponInstanceIds.Clear();
            
            StaticManager.BeginCoroutine(UpdateEasternWeapons());
        }
    }
    
    // TODO: These methods are redundant together, reduce it down to a single routine.
    
    private static IEnumerator UpdateUsecWeapons()
    {
        if (SkillManager is null) yield break; 
        
        var natoWeapons = SkillsPlugin.SkillData.NatoWeapons;
        
        var weapons = GameUtils.GetProfile()!.Inventory.AllRealPlayerItems
            .Where(x => natoWeapons.Weapons.Contains(x.TemplateId));
        
        foreach (var item in weapons)
        {
            if (item is not Weapon weapon) continue;

            // Store the weapons original values
            if (!UsecOriginalWeaponValues.ContainsKey(weapon.TemplateId))
            {
                var origVals = new OrigWeaponValues
                {
                    ergo = weapon.Template.Ergonomics,
                    weaponUp = weapon.Template.RecoilForceUp,
                    weaponBack = weapon.Template.RecoilForceBack
                };

#if DEBUG
                SkillsPlugin.Log.LogDebug($"original {weapon.LocalizedName()} ergo: {weapon.Template.Ergonomics}, up {weapon.Template.RecoilForceUp}, back {weapon.Template.RecoilForceBack}");
#endif
                UsecOriginalWeaponValues.Add(item.TemplateId, origVals);
            }

            //Skip instances of the weapon that are already adjusted at this level.
            if (UsecWeaponInstanceIds.ContainsKey(item.Id))
            {
                if (UsecWeaponInstanceIds[item.Id] == GameUtils.GetSkillManager()!.UsecArsystems.Level)
                {
                    continue;
                }

                UsecWeaponInstanceIds.Remove(item.Id);
            }

            weapon.Template.Ergonomics = UsecOriginalWeaponValues[item.TemplateId].ergo * (1 + SkillMgrExt.UsecArSystemsErgoBuff);
            weapon.Template.RecoilForceUp = UsecOriginalWeaponValues[item.TemplateId].weaponUp * (1 - SkillMgrExt.UsecArSystemsRecoilBuff);
            weapon.Template.RecoilForceBack = UsecOriginalWeaponValues[item.TemplateId].weaponBack * (1 - SkillMgrExt.UsecArSystemsRecoilBuff);

#if DEBUG
            SkillsPlugin.Log.LogDebug($"New {weapon.LocalizedName()} ergo: {weapon.Template.Ergonomics}, up {weapon.Template.RecoilForceUp}, back {weapon.Template.RecoilForceBack}");
#endif
            
            UsecWeaponInstanceIds.Add(item.Id, GameUtils.GetSkillManager()!.UsecArsystems.Level);

            yield return null;
        }
    }

    private static IEnumerator UpdateEasternWeapons()
    {
        if (SkillManager == null) yield break;

        var easternWeapons = SkillsPlugin.SkillData.EasternWeapons;

        var weapons = GameUtils.GetProfile()!.Inventory.AllRealPlayerItems
            .Where(x => easternWeapons.Weapons.Contains(x.TemplateId));

        foreach (var item in weapons)
        {
            if (item is not Weapon weapon) continue;

            // Store the weapons original values
            if (!EasternOriginalWeaponValues.ContainsKey(item.TemplateId))
            {
                var origVals = new OrigWeaponValues
                {
                    ergo = weapon.Template.Ergonomics,
                    weaponUp = weapon.Template.RecoilForceUp,
                    weaponBack = weapon.Template.RecoilForceBack
                };

#if DEBUG
                SkillsPlugin.Log.LogDebug(
                    $"original {weapon.LocalizedName()} ergo: {weapon.Template.Ergonomics}, up {weapon.Template.RecoilForceUp}, back {weapon.Template.RecoilForceBack}");
#endif
                
                EasternOriginalWeaponValues.Add(item.TemplateId, origVals);
            }

            //Skip instances of the weapon that are already adjusted at this level.
            if (EasternWeaponInstanceIds.ContainsKey(item.Id))
            {
                if (EasternWeaponInstanceIds[item.Id] == SkillManager.BearAksystems.Level)
                {
                    continue;
                }

                EasternWeaponInstanceIds.Remove(item.Id);
            }

            weapon.Template.Ergonomics =
                EasternOriginalWeaponValues[item.TemplateId].ergo * (1 + SkillMgrExt.BearAkSystemsErgoBuff);
            weapon.Template.RecoilForceUp = EasternOriginalWeaponValues[item.TemplateId].weaponUp *
                                            (1 - SkillMgrExt.BearAkSystemsRecoilBuff);
            weapon.Template.RecoilForceBack = EasternOriginalWeaponValues[item.TemplateId].weaponBack *
                                              (1 - SkillMgrExt.BearAkSystemsRecoilBuff);

#if DEBUG
            SkillsPlugin.Log.LogDebug(
                $"New {weapon.LocalizedName()} ergo: {weapon.Template.Ergonomics}, up {weapon.Template.RecoilForceUp}, back {weapon.Template.RecoilForceBack}");
#endif
            
            EasternWeaponInstanceIds.Add(item.Id, SkillManager.BearAksystems.Level);

            yield return null;
        }
    }
}