using EFT;
using EFT.UI;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using Comfort.Common;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using static SkillsExtended.Patches.FirstAidSkillPatches;

namespace SkillsExtended.Controllers
{
    internal class GInterface249Impl : GInterface249
    {
        public int MaxHpResource { set; get; }
        public float HpResourceRate { set; get; }
    }


    public class FirstAid : MonoBehaviour
    {
        private GameWorld gameWorld { get => Singleton<GameWorld>.Instance; }

        private Player player { get => gameWorld.MainPlayer; }

        private static SkillManager _playerSkillManager;
        private static SkillManager _ScavSkillManager;

        private int bonusHpPmc { get => _playerSkillManager.FirstAid.Level * 5; }

        private static Dictionary<string, int> _originalHPValues = new Dictionary<string, int>
        {
            { "544fb45d4bdc2dee738b4568", 400 },   // Salewa
            { "5755356824597772cb798962", 100 },   // AI-2
            { "590c657e86f77412b013051d", 1800 },  // Grizzly
            { "590c661e86f7741e566b646a", 220 },   // Car
            { "590c678286f77426c9660122", 300 },   // Ifak
            { "5e99711486f7744bfc4af328", 3000 },  // Sanitars
            { "60098ad7c2240c0fe85c570a", 400 }    // AFAK
        };
        
        // Store the instance ID of the item and the level its bonus resource is set to.
        private static Dictionary<string, int> instanceIDs = new Dictionary<string, int>();

        private void Awake()
        {
            new HealthControllerMedEffectPatch().Enable();
            new HealthEffectComponentPatch().Enable();
            new FirstAidEnablePatch().Enable();
            new FirstAidImageBuffPatch().Enable();
        }

        private void Update()
        {
            // Set skill manager instance
            if (_playerSkillManager == null && Plugin.Session?.Profile?.Skills != null)
            {
                _playerSkillManager = Plugin.Session.Profile.Skills;
                _ScavSkillManager = Plugin.Session.ProfileOfPet.Skills;
                
                StaticManager.Instance.StartCoroutine(FirstAidUpdate());
                
                Plugin.Log.LogDebug("FirstAid Skill Initialized.");
            }

            // Dont continue if skill manager is null
            if (_playerSkillManager == null) { return; }

            if (Singleton<PreloaderUI>.Instantiated) { instanceIDs.Clear(); }

            StaticManager.Instance.StartCoroutine(FirstAidUpdate());
        }

        public void ApplyExp()
        {
            float xpGain = 1.5f;

            if (player.Side == EPlayerSide.Usec || player.Side == EPlayerSide.Bear)
            {
                _playerSkillManager.FirstAid.SetCurrent(_playerSkillManager.FirstAid.Current + xpGain, true);

                if (_playerSkillManager.FirstAid.LevelProgress >= _playerSkillManager.FirstAid.LevelExp)
                {
                    _playerSkillManager.FirstAid.SetLevel(_playerSkillManager.FirstAid.Level + 1);
                }

                Plugin.Log.LogDebug($"Skill: {_playerSkillManager.FirstAid.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else if (player.Side == EPlayerSide.Savage)
            {
                _ScavSkillManager.FirstAid.SetCurrent(_ScavSkillManager.FirstAid.Current + xpGain, true);

                if (_ScavSkillManager.FirstAid.LevelProgress >= _ScavSkillManager.FirstAid.LevelExp)
                {
                    _ScavSkillManager.FirstAid.SetLevel(_ScavSkillManager.FirstAid.Level + 1);
                }

                Plugin.Log.LogDebug($"Skill: {_ScavSkillManager.FirstAid.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else
            {
                Plugin.Log.LogDebug($"No XP gain occured. Something went horribly wrong: Invalid Player side.");
            }
        }

        public float CalculateSpeedBonus()
        {
            float bonus;

            if (player.Side != EPlayerSide.Savage)
            {
                // 0.07% per level, Max 35%
                bonus = 1f - (_playerSkillManager.FirstAid.Level * 0.007f);

                if (_playerSkillManager.FirstAid.IsEliteLevel)
                {
                    // 15% Elite bonus
                    bonus = bonus - 0.15f;
                }

                Plugin.Log.LogDebug($"{_playerSkillManager.FirstAid.Id} Bonus: {(1 - bonus) * 100}%, Is elite: {_playerSkillManager.FirstAid.IsEliteLevel}");

                return bonus;
            }
            else
            {
                // 0.07% per level, Max 35%
                bonus = 1f - (_ScavSkillManager.FirstAid.Level * 0.007f);

                if (_ScavSkillManager.FirstAid.IsEliteLevel)
                {
                    // 15% Elite bonus
                    bonus = bonus - 0.15f;
                }

                Plugin.Log.LogDebug($"{_ScavSkillManager.FirstAid.Id} Bonus: {(1 - bonus) * 100}%, Is elite: {_ScavSkillManager.FirstAid.IsEliteLevel}");

                return bonus;
            }
        }

        private IEnumerator FirstAidUpdate()
        {
            var items = Plugin.Session.Profile.Inventory.AllPlayerItems.Where(x => x is MedsClass);

            foreach (var item in items)
            {
                // Skip if not a med kit
                if (!_originalHPValues.ContainsKey(item.TemplateId)) { continue; }

                // Skip if we already set this item at the current level.
                if (instanceIDs.ContainsKey(item.Id))
                {
                    int previouslySet = instanceIDs[item.Id];

                    if (previouslySet == _playerSkillManager.FirstAid.Level) 
                    { 
                        continue; 
                    }
                    else 
                    { 
                        instanceIDs.Remove(item.Id); 
                    }
                }

                if (item is MedsClass meds &&
                    _playerSkillManager.FirstAid.Level > 0 &&
                    meds.MedKitComponent.MaxHpResource != _originalHPValues[meds.TemplateId] + bonusHpPmc)
                {
                    
                    GInterface249 newGInterface = new GInterface249Impl { 
                        MaxHpResource = _originalHPValues[meds.TemplateId] + bonusHpPmc, 
                        HpResourceRate = meds.MedKitComponent.HpResourceRate 
                    };

                    var currentResouce = meds.MedKitComponent.HpResource;
                    var currentMaxResouce = meds.MedKitComponent.MaxHpResource;

                    var medComp = AccessTools.Field(typeof(MedsClass), "MedKitComponent").GetValue(meds);
                    AccessTools.Field(typeof(MedKitComponent), "ginterface249_0").SetValue(medComp, newGInterface);

                    // Only change the current resource if the item is unused.
                    if (currentResouce == currentMaxResouce)
                    {
                        meds.MedKitComponent.HpResource = _originalHPValues[meds.TemplateId] + bonusHpPmc;
                    }
                    
                    // Add the instance ID of the item to a list, so we dont change already changed items.
                    instanceIDs.Add(item.Id, _playerSkillManager.FirstAid.Level);

                    Plugin.Log.LogDebug($"Set instance {item.Id} of type {item.TemplateId} to {_originalHPValues[meds.TemplateId] + bonusHpPmc}");
                }
            }

            yield break;
        }
    }
}