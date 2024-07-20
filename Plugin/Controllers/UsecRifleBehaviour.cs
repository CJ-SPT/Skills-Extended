using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using System.Collections.Generic;
using UnityEngine;

namespace SkillsExtended.Controllers;

public class UsecRifleBehaviour : MonoBehaviour
{
    private static bool _isSubscribed = false;
    public readonly Dictionary<string, int> WeaponInstanceIds = [];
    public IEnumerable<Item> UsecWeapons = null;
    private static SkillManager SkillManager => Utils.GetActiveSkillManager();
    private static ISession Session => Plugin.Session;
    private static GameWorld GameWorld => Singleton<GameWorld>.Instance;
    private static int UsecARLevel => Session.Profile.Skills.UsecArsystems.Level;
    private static WeaponSkillData UsecSkillData => Plugin.SkillData.UsecRifleSkill;
    
    // Store an object containing the weapons original stats.
    private readonly Dictionary<string, OrigWeaponValues> _originalWeaponValues = [];

    private void Update()
    {
        SetupSkillManager();

        if (SkillManager == null || UsecWeapons == null) { return; }

        UpdateWeapons();
    }

    private static void SetupSkillManager()
    {
        if (_isSubscribed || SkillManager is null) return;
        
        if (GameWorld?.MainPlayer is null || GameWorld?.MainPlayer?.Location == "hideout")
        {
            return;
        }
        
        SkillManager.OnMasteringExperienceChanged += ApplyUsecARXp;
        _isSubscribed = true;
    }

    private static void ApplyUsecARXp(MasterSkillClass action)
    {
        var weaponInHand = Singleton<GameWorld>.Instance.MainPlayer.HandsController.GetItem();

        if (!UsecSkillData.Weapons.Contains(weaponInHand.TemplateId))
        {
            return;
        }

        SkillManager.UsecArsystems.Actions[0].Complete(UsecSkillData.WeaponProfXp);
    }

    private void UpdateWeapons()
    {
        foreach (var item in UsecWeapons)
        {
            if (item is not Weapon weapon) return;

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
                if (WeaponInstanceIds[item.Id] == UsecARLevel)
                {
                    continue;
                }

                WeaponInstanceIds.Remove(item.Id);
            }

            weapon.Template.Ergonomics = _originalWeaponValues[item.TemplateId].ergo * (1 + SkillBuffs.UsecArSystemsErgoBuff);
            weapon.Template.RecoilForceUp = _originalWeaponValues[item.TemplateId].weaponUp * (1 - SkillBuffs.UsecArSystemsRecoilBuff);
            weapon.Template.RecoilForceBack = _originalWeaponValues[item.TemplateId].weaponBack * (1 - SkillBuffs.UsecArSystemsRecoilBuff);

            Plugin.Log.LogDebug($"New {weapon.LocalizedName()} ergo: {weapon.Template.Ergonomics}, up {weapon.Template.RecoilForceUp}, back {weapon.Template.RecoilForceBack}");

            WeaponInstanceIds.Add(item.Id, UsecARLevel);
        }
    }
}
