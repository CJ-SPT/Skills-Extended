using System.Collections;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using System.Collections.Generic;
using SkillsExtended.Skills.Core;
using UnityEngine;

namespace SkillsExtended.Skills.EasternRifle;

internal class EasternRifleBehaviour : MonoBehaviour
{
    private static bool _isSubscribed = false;

    public readonly Dictionary<string, int> WeaponInstanceIds = [];
    public IEnumerable<Item> BearWeapons = null;

    private static SkillManager SkillManager => Utils.GetActiveSkillManager();
    private static ISession Session => Plugin.Session;

    private static SkillManagerExt SkillMgrExt => Plugin.PlayerSkillManagerExt;
    
    private static GameWorld GameWorld => Singleton<GameWorld>.Instance;

    private static int BearAkLevel => Session.Profile.Skills.BearAksystems.Level;
    private static WeaponSkillData EasternData => Plugin.SkillData.EasternRifle;
    
    // Store an object containing the weapons original stats.
    private readonly Dictionary<string, OrigWeaponValues> _originalWeaponValues = [];
    
    private void Update()
    {
        SetupSkillManager();
    }

    public IEnumerator UpdateWeapons()
    {
        if (SkillManager == null || BearWeapons == null) yield break;
        
        foreach (var item in BearWeapons)
        {
            if (item is not Weapon weapon) continue;

            // Store the weapons original values
            if (!_originalWeaponValues.ContainsKey(item.TemplateId))
            {
                var origVals = new OrigWeaponValues
                {
                    ergo = weapon.Template.Ergonomics,
                    weaponUp = weapon.Template.RecoilForceUp,
                    weaponBack = weapon.Template.RecoilForceBack
                };

                Plugin.Log.LogDebug($"original {weapon.LocalizedName()} ergo: {weapon.Template.Ergonomics}, up {weapon.Template.RecoilForceUp}, back {weapon.Template.RecoilForceBack}");

                _originalWeaponValues.Add(item.TemplateId, origVals);
            }

            //Skip instances of the weapon that are already adjusted at this level.
            if (WeaponInstanceIds.ContainsKey(item.Id))
            {
                if (WeaponInstanceIds[item.Id] == BearAkLevel)
                {
                    continue;
                }

                WeaponInstanceIds.Remove(item.Id);
            }
            
            weapon.Template.Ergonomics = _originalWeaponValues[item.TemplateId].ergo * (1 + SkillMgrExt.BearAkSystemsErgoBuff);
            weapon.Template.RecoilForceUp = _originalWeaponValues[item.TemplateId].weaponUp * (1 - SkillMgrExt.BearAkSystemsRecoilBuff);
            weapon.Template.RecoilForceBack = _originalWeaponValues[item.TemplateId].weaponBack * (1 - SkillMgrExt.BearAkSystemsRecoilBuff);

            Plugin.Log.LogDebug($"New {weapon.LocalizedName()} ergo: {weapon.Template.Ergonomics}, up {weapon.Template.RecoilForceUp}, back {weapon.Template.RecoilForceBack}");

            WeaponInstanceIds.Add(item.Id, BearAkLevel);

            yield return null;
        }
    }
    
    private static void SetupSkillManager()
    {
        if (_isSubscribed || SkillManager is null) return;
        
        if (GameWorld?.MainPlayer is null || GameWorld?.MainPlayer?.Location == "hideout")
        {
            return;
        }

        SkillManager.OnMasteringExperienceChanged += ApplyBearAkXp;
        _isSubscribed = true;
    }

    private static void ApplyBearAkXp(MasterSkillClass action)
    {
        var weaponInHand = Singleton<GameWorld>.Instance.MainPlayer.HandsController.GetItem();

        if (!EasternData.Weapons.Contains(weaponInHand.TemplateId)) return;
        
        GameWorld.MainPlayer.ExecuteSkill(CompleteSkill);
    }

    private static void CompleteSkill()
    {
        if (SkillManager.BearAksystems.IsEliteLevel) return;
        
        if (EasternData.SkillShareEnabled)
        {
	        SkillMgrExt.UsecRifleAction.Complete(EasternData.WeaponProfXp * EasternData.SkillShareXpRatio);
            return;
        }
        
        SkillMgrExt.BearRifleAction.Complete(EasternData.WeaponProfXp);
	}
}