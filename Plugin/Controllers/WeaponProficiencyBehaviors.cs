using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SkillsExtended;
using SkillsExtended.Helpers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Skills_Extended.Controllers
{
    public class WeaponProficiencyBehaviors : MonoBehaviour
    {
        private class OrigWeaponValues
        {
            public float ergo;
            public float weaponUp;
            public float weaponBack;
        }

        private SkillManager _skillManager;

        private ISession _session => Plugin.Session;

        private GameWorld _gameWorld { get => Singleton<GameWorld>.Instance; }

        private Player _player { get => _gameWorld.MainPlayer; }

        private int _usecARLevel => _session.Profile.Skills.UsecArsystems.Level;
        
        private int _bearAKLevel => _session.Profile.Skills.BearAksystems.Level;
        
        private IEnumerable<Item> _usecWeapons => _session.Profile.Inventory.AllPlayerItems.Where(x => Constants.USEC_WEAPON_LIST.Contains(x.TemplateId));
        
        private IEnumerable<Item> _bearWeapons => _session.Profile.Inventory.AllPlayerItems.Where(x => Constants.BEAR_WEAPON_LIST.Contains(x.TemplateId));
        
        private float _ergoBonus => _skillManager.UsecArsystems.IsEliteLevel ? 
            _usecARLevel * Constants.ERGO_MOD + Constants.ERGO_MOD_ELITE : _usecARLevel * Constants.ERGO_MOD;
        
        private float _recoilBonus => _skillManager.UsecArsystems.IsEliteLevel ? 
            _usecARLevel * Constants.RECOIL_REDUCTION + Constants.RECOIL_REDUCTION_ELITE : _usecARLevel * Constants.RECOIL_REDUCTION;

        public Dictionary<string, int> weaponInstanceIds = new Dictionary<string, int>();

        public List<string> weaponUiInstanceIds = new List<string>();

        // Store an object containing the weapons original stats.
        private Dictionary<string, OrigWeaponValues> _originalWeaponValues = new Dictionary<string, OrigWeaponValues>();

        public List<string> customUsecWeapons = new List<string>();
        public List<string> customBearWeapons = new List<string>();

        private void Awake()
        {
            GetCustomWeapons();
        }

        private void Update()
        {
            SetupSkillManager();

            if (_skillManager == null) { return; }

            // Only run this behavior if we are USEC, or the player has completed the BEAR skill
            if (Plugin.Session?.Profile?.Side == EPlayerSide.Usec || _skillManager.BearAksystems.IsEliteLevel)
            { 
                StaticManager.Instance.StartCoroutine(UpdateWeapons(_usecWeapons, _ergoBonus, _recoilBonus, _usecARLevel));
            }

            // Only run this behavior if we are BEAR, or the player has completed the USEC skill
            if (Plugin.Session?.Profile?.Side == EPlayerSide.Bear || _skillManager.UsecArsystems.IsEliteLevel)
            {
                StaticManager.Instance.StartCoroutine(UpdateWeapons(_bearWeapons, _ergoBonus, _recoilBonus, _bearAKLevel));
            }
        }

        private void SetupSkillManager()
        {
            // Set skill manager instance
            if (Plugin.Session?.Profile?.Skills != null && _skillManager == null)
            {
                _skillManager = Plugin.Session.Profile.Skills;
                
                if (Plugin.Session.Profile.Side == EPlayerSide.Usec || _skillManager.BearAksystems.IsEliteLevel)
                {
                    _skillManager.OnMasteringExperienceChanged += ApplyUsecARXp;
                    Plugin.Log.LogDebug("USEC AR XP ENABLED.");
                }

                if (Plugin.Session.Profile.Side == EPlayerSide.Bear || _skillManager.UsecArsystems.IsEliteLevel)
                {
                    _skillManager.OnMasteringExperienceChanged += ApplyBearAKXp;
                    Plugin.Log.LogDebug("BEAR AK XP ENABLED.");
                }

                Plugin.Log.LogDebug($"SkillManager is Session. Side: {Plugin.Session.Profile.Side} IsBearComplete: {_skillManager.BearAksystems.IsEliteLevel} IsUsecComplete {_skillManager.UsecArsystems.IsEliteLevel}");
            }

            if (_gameWorld?.MainPlayer != null && _skillManager == null)
            {
                _skillManager = _player.Skills;

                if (Plugin.Session.Profile.Side == EPlayerSide.Usec || _skillManager.BearAksystems.IsEliteLevel)
                {
                    _skillManager.OnMasteringExperienceChanged += ApplyUsecARXp;
                    Plugin.Log.LogDebug("USEC AR XP ENABLED.");
                }

                if (Plugin.Session.Profile.Side == EPlayerSide.Bear || _skillManager.UsecArsystems.IsEliteLevel)
                {
                    _skillManager.OnMasteringExperienceChanged += ApplyBearAKXp;
                    Plugin.Log.LogDebug("BEAR AK XP ENABLED.");
                }

                Plugin.Log.LogDebug($"SkillManager is Player. Side: {Plugin.Session.Profile.Side} IsBearComplete: {_skillManager.BearAksystems.IsEliteLevel} IsUsecComplete {_skillManager.UsecArsystems.IsEliteLevel}");
            }
        }

        private void GetCustomWeapons()
        {
            customUsecWeapons = Utils.Get<List<string>>("/skillsExtended/GetCustomWeaponsUsec");
            customBearWeapons = Utils.Get<List<string>>("/skillsExtended/GetCustomWeaponsBear");
        }

        private void ApplyUsecARXp(GClass1634 action)
        {
            var items = _session.Profile.InventoryInfo.GetItemsInSlots(new EquipmentSlot[] { EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon });

            // Check if the item exists in the predefined list,
            // if it doesn't check if it exists in the custom list
            items = items.Where(x => Constants.USEC_WEAPON_LIST.Contains(x.TemplateId) || customUsecWeapons.Contains(x.TemplateId));

            if (items.Any())
            {
                _skillManager.UsecArsystems.Current += Constants.WEAPON_PROF_XP;

                Plugin.Log.LogDebug($"USEC AR {Constants.WEAPON_PROF_XP} XP Gained.");
                return;
            }

            Plugin.Log.LogDebug("Invalid weapon for XP");
        }

        private void ApplyBearAKXp(GClass1634 action)
        {
            var items = _session.Profile.InventoryInfo.GetItemsInSlots(new EquipmentSlot[] { EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon });

            // Check if the item exists in the predefined list,
            // if it doesn't check if it exists in the custom list
            items = items.Where(x => Constants.BEAR_WEAPON_LIST.Contains(x.TemplateId) || customBearWeapons.Contains(x.TemplateId));

            if (items.Any())
            {
                _skillManager.BearAksystems.Current += Constants.WEAPON_PROF_XP;

                Plugin.Log.LogDebug($"BEAR AK {Constants.WEAPON_PROF_XP} XP Gained.");
                return;
            }

            Plugin.Log.LogDebug("Invalid weapon for XP");
        }

        private IEnumerator UpdateWeapons(IEnumerable<Item> items, float ergoBonus, float recoilReduction, int level)
        { 
            foreach (var item in items)
            {
                if (item is Weapon weap)
                {
                    // Store the weapons original values
                    if (!_originalWeaponValues.ContainsKey(item.TemplateId))
                    {
                        var origVals = new OrigWeaponValues();

                        origVals.ergo = weap.Template.Ergonomics;
                        origVals.weaponUp = weap.Template.RecoilForceUp;
                        origVals.weaponBack = weap.Template.RecoilForceBack;

                        Plugin.Log.LogDebug($"original {weap.LocalizedName()} ergo: {weap.Template.Ergonomics}, up {weap.Template.RecoilForceUp}, back {weap.Template.RecoilForceBack}");

                        _originalWeaponValues.Add(item.TemplateId, origVals);
                    }

                    //Skip instances of the weapon that are already adjusted at this level.
                    if (weaponInstanceIds.ContainsKey(item.Id))
                    {
                        if (weaponInstanceIds[item.Id] == level)
                        {
                            continue;
                        }
                        else
                        {
                            weaponInstanceIds.Remove(item.Id);
                        }
                    }
       
                    weap.Template.Ergonomics = _originalWeaponValues[item.TemplateId].ergo * (1 + ergoBonus);
                    weap.Template.RecoilForceUp = _originalWeaponValues[item.TemplateId].weaponUp * (1 - recoilReduction); 
                    weap.Template.RecoilForceBack = _originalWeaponValues[item.TemplateId].weaponBack * (1 - recoilReduction);

                    Plugin.Log.LogDebug($"New {weap.LocalizedName()} ergo: {weap.Template.Ergonomics}, up {weap.Template.RecoilForceUp}, back {weap.Template.RecoilForceBack}");

                    weaponInstanceIds.Add(item.Id, level);
                }
            }

            yield break;
        }
    }
}