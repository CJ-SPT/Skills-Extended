using EFT;
using EFT.UI;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using Comfort.Common;
using EFT.InventoryLogic;
using System.Collections;
using System.Collections.Generic;
using static SkillsExtended.Patches.MedicalPatches;
using EFT.HealthSystem;

namespace SkillsExtended.Controllers
{
    internal class GInterface243Impl : GInterface243
    {
        public float UseTime { set; get; }

        public KeyValuePair<EBodyPart, float>[] BodyPartTimeMults { set; get; }

        public Dictionary<EHealthFactorType, GClass1146> HealthEffects { set; get; }

        public Dictionary<EDamageEffectType, GClass1145> DamageEffects { set; get; }

        public string StimulatorBuffs { set; get; }
    }

    internal class GInterface249Impl : GInterface249
    {
        public int MaxHpResource { set; get; }
        public float HpResourceRate { set; get; }
    }

    public class MedicalBehavior : MonoBehaviour
    {
        public static Dictionary<string, float> originalFieldMedicineUseTimes = new Dictionary<string, float>
        {
            { "544fb25a4bdc2dfb738b4567", 2f},  //bandage
            { "5751a25924597722c463c472", 2f},  //army bandage
            { "5e831507ea0a7c419c2f9bd9", 5f},  //esmarch
            { "60098af40accd37ef2175f27", 3f},  //CAT
            { "5e8488fa988a8701445df1e4", 3f},  //calok-b
            { "544fb3364bdc2d34748b456a", 5f},  //splint
            { "5af0454c86f7746bf20992e8", 3f},  //alu splint
        };

        private static Dictionary<string, int> _originalFirstAidHPValues = new Dictionary<string, int>
        {
            { "544fb45d4bdc2dee738b4568", 400 },   // Salewa
            { "5755356824597772cb798962", 100 },   // AI-2
            { "590c657e86f77412b013051d", 1800 },  // Grizzly
            { "590c661e86f7741e566b646a", 220 },   // Car
            { "590c678286f77426c9660122", 300 },   // Ifak
            { "5e99711486f7744bfc4af328", 3000 },  // Sanitars
            { "60098ad7c2240c0fe85c570a", 400 }    // AFAK
        };

        private static Dictionary<string, float> _originalFirstAidUseTimes = new Dictionary<string, float>
        {
            { "544fb45d4bdc2dee738b4568", 3f }, // Salewa
            { "5755356824597772cb798962", 2f }, // AI-2
            { "590c657e86f77412b013051d", 5f }, // Grizzly
            { "590c661e86f7741e566b646a", 3f }, // Car
            { "590c678286f77426c9660122", 3f }, // Ifak
            { "5e99711486f7744bfc4af328", 2f }, // Sanitars
            { "60098ad7c2240c0fe85c570a", 3f }  // AFAK
        };

        private GameWorld gameWorld { get => Singleton<GameWorld>.Instance; }

        private Player player { get => gameWorld.MainPlayer; }

        private static SkillManager _playerSkillManager;
        private static SkillManager _ScavSkillManager;

        private int bonusHpPmc { get => _playerSkillManager.FirstAid.Level * 5; }

        // Store the instance ID of the item and the level its bonus resource is set to.
        private static Dictionary<string, int> _firstAidInstanceIDs = new Dictionary<string, int>();
        private static Dictionary<string, int> _fieldMedicineInstanceIDs = new Dictionary<string, int>();

        private bool _setOnMenu = false;

        private void Awake()
        {
            new EnableSkillsPatch().Enable();
            new DoMedEffectPatch().Enable();
        }

        private void Update()
        {
            // Set skill manager instance
            if (_playerSkillManager == null && Plugin.Session?.Profile?.Skills != null)
            {
                _playerSkillManager = Plugin.Session.Profile.Skills;
                _ScavSkillManager = Plugin.Session.ProfileOfPet.Skills;
         
                Plugin.Log.LogDebug("Medical Component Initialized.");
            }

            // Dont continue if session is null
            if (_playerSkillManager == null) { return; }

            if (Singleton<PreloaderUI>.Instantiated && _setOnMenu == false) 
            {
                _firstAidInstanceIDs.Clear();
                _fieldMedicineInstanceIDs.Clear();
                _setOnMenu = true;
            }
            else if (!Singleton<PreloaderUI>.Instantiated)
            {
                _setOnMenu = false;
            }

            StaticManager.Instance.StartCoroutine(FirstAidUpdate());
        }

        public void ApplyFirstAidExp()
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
                Plugin.Log.LogDebug($"No XP gain occured. Something went horribly wrong: Invalid Player side on first aid.");
            }
        }

        public void ApplyFieldMedicineExp()
        {
            float xpGain = 1.5f;

            if (player.Side == EPlayerSide.Usec || player.Side == EPlayerSide.Bear)
            {
                _playerSkillManager.FieldMedicine.SetCurrent(_playerSkillManager.FieldMedicine.Current + xpGain, true);

                if (_playerSkillManager.FieldMedicine.LevelProgress >= _playerSkillManager.FieldMedicine.LevelExp)
                {
                    _playerSkillManager.FieldMedicine.SetLevel(_playerSkillManager.FieldMedicine.Level + 1);
                }

                Plugin.Log.LogDebug($"Skill: {_playerSkillManager.FieldMedicine.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else if (player.Side == EPlayerSide.Savage)
            {
                _ScavSkillManager.FieldMedicine.SetCurrent(_ScavSkillManager.FieldMedicine.Current + xpGain, true);

                if (_ScavSkillManager.FieldMedicine.LevelProgress >= _ScavSkillManager.FieldMedicine.LevelExp)
                {
                    _ScavSkillManager.FieldMedicine.SetLevel(_ScavSkillManager.FieldMedicine.Level + 1);
                }

                Plugin.Log.LogDebug($"Skill: {_ScavSkillManager.FieldMedicine.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else
            {
                Plugin.Log.LogDebug($"No XP gain occured. Something went horribly wrong: Invalid Player side on field medicine.");
            }
        }

        private void ApplyFirstAidSpeedBonus(Item item)
        {
            float bonus = 1f;
            
            if (_firstAidInstanceIDs.ContainsKey(item.Id)) { return; }

            if (gameWorld == null)
            {
                bonus = 1f - (_playerSkillManager.FirstAid.Level * 0.007f);
                
                if (_playerSkillManager.FirstAid.IsEliteLevel) { bonus -= 0.15f; }
            }
            else
            {
                if (player.Side == EPlayerSide.Bear || player.Side == EPlayerSide.Usec)
                {
                    bonus = 1f - (_playerSkillManager.FirstAid.Level * 0.007f);
                    
                    if (_playerSkillManager.FirstAid.IsEliteLevel) { bonus -= 0.15f; }
                }
                else if (player.Side == EPlayerSide.Savage)
                {
                    bonus = 1f - (_ScavSkillManager.FirstAid.Level * 0.007f);
                    
                    if (_ScavSkillManager.FirstAid.IsEliteLevel) { bonus -= 0.15f; }
                }
            }
        
            if (item is MedsClass meds && _playerSkillManager.FirstAid.Level > 0)
            {
                GInterface243 newGInterface = new GInterface243Impl
                {
                    UseTime = _originalFirstAidUseTimes[meds.TemplateId] * bonus,
                    BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults,
                    HealthEffects = meds.HealthEffectsComponent.HealthEffects,
                    DamageEffects = meds.HealthEffectsComponent.DamageEffects,
                    StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs
                };

                var healthEffectComp = AccessTools.Field(typeof(MedsClass), "HealthEffectsComponent").GetValue(meds);
                AccessTools.Field(typeof(HealthEffectsComponent), "ginterface243_0").SetValue(healthEffectComp, newGInterface);

                Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {_originalFirstAidUseTimes[meds.TemplateId] * bonus} seconds");
            }        
        }

        private void ApplyFirstAidHPBonus(Item item)
        {
            if (_firstAidInstanceIDs.ContainsKey(item.Id)) { return; }

            if (item is MedsClass meds &&
                    _playerSkillManager.FirstAid.Level > 0 &&
                    meds.MedKitComponent.MaxHpResource != _originalFirstAidHPValues[meds.TemplateId] + bonusHpPmc)
            {

                GInterface249 newGInterface = new GInterface249Impl
                {
                    MaxHpResource = _originalFirstAidHPValues[meds.TemplateId] + bonusHpPmc,
                    HpResourceRate = meds.MedKitComponent.HpResourceRate
                };

                var currentResouce = meds.MedKitComponent.HpResource;
                var currentMaxResouce = meds.MedKitComponent.MaxHpResource;

                var medComp = AccessTools.Field(typeof(MedsClass), "MedKitComponent").GetValue(meds);
                AccessTools.Field(typeof(MedKitComponent), "ginterface249_0").SetValue(medComp, newGInterface);

                // Only change the current resource if the item is unused.
                if (currentResouce == currentMaxResouce)
                {
                    meds.MedKitComponent.HpResource = _originalFirstAidHPValues[meds.TemplateId] + bonusHpPmc;
                }

                Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {_originalFirstAidHPValues[meds.TemplateId] + bonusHpPmc} HP");
            }
        }

        private void ApplyFieldMedicineSpeedBonus(Item item)
        {
            float bonus = 1f;

            if (gameWorld == null)
            {
                bonus = 1f - (_playerSkillManager.FieldMedicine.Level * 0.007f);
                if (_playerSkillManager.FieldMedicine.IsEliteLevel) { bonus -= 0.15f; }         
            }
            else
            {
                if (player.Side == EPlayerSide.Bear || player.Side == EPlayerSide.Usec)
                {
                    bonus = 1f - (_playerSkillManager.FieldMedicine.Level * 0.007f);
                    if (_playerSkillManager.FieldMedicine.IsEliteLevel) { bonus -= 0.15f; }
                }
                else if (player.Side == EPlayerSide.Savage)
                {
                    bonus = 1f - (_ScavSkillManager.FieldMedicine.Level * 0.007f);
                    if (_ScavSkillManager.FieldMedicine.IsEliteLevel) { bonus -= 0.15f; }
                }
                else
                {
                    Plugin.Log.LogDebug($"No Field Medicine Bonus Applied: Invalid playerside.");
                }
            }


            if (item is MedsClass meds && _playerSkillManager.FieldMedicine.Level > 0)
            {
                GInterface243 newGInterface = new GInterface243Impl
                {
                    UseTime = originalFieldMedicineUseTimes[meds.TemplateId] * bonus,
                    BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults,
                    HealthEffects = meds.HealthEffectsComponent.HealthEffects,
                    DamageEffects = meds.HealthEffectsComponent.DamageEffects,
                    StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs
                };

                var healthEffectComp = AccessTools.Field(typeof(MedsClass), "HealthEffectsComponent").GetValue(meds);
                AccessTools.Field(typeof(HealthEffectsComponent), "ginterface243_0").SetValue(healthEffectComp, newGInterface);

                Plugin.Log.LogDebug($"Field Medicine: Set instance {item.Id} of type {item.TemplateId} to {originalFieldMedicineUseTimes[meds.TemplateId] * bonus} seconds");
            }    
        }

        private IEnumerator FirstAidUpdate()
        {
            var items = Plugin.Session.Profile.Inventory.AllPlayerItems.Where(x => x is MedsClass);

            foreach (var item in items)
            {
                // Skip if we already set this first aid item.
                if (_firstAidInstanceIDs.ContainsKey(item.Id))
                {
                    int previouslySet = _firstAidInstanceIDs[item.Id];

                    if (previouslySet == _playerSkillManager.FirstAid.Level) 
                    { 
                        continue; 
                    }
                    else 
                    {
                        _firstAidInstanceIDs.Remove(item.Id); 
                    }
                }

                // Skip if we already set this field medicine item.
                if (_fieldMedicineInstanceIDs.ContainsKey(item.Id))
                {
                    int previouslySet = _fieldMedicineInstanceIDs[item.Id];

                    if (previouslySet == _playerSkillManager.FieldMedicine.Level)
                    {
                        continue;
                    }
                    else
                    {
                        _fieldMedicineInstanceIDs.Remove(item.Id);
                    }
                }

                // Apply first aid speed bonus to items
                if (_originalFirstAidUseTimes.ContainsKey(item.TemplateId))
                {
                    ApplyFirstAidSpeedBonus(item);
                    ApplyFirstAidHPBonus(item);
                    _firstAidInstanceIDs.Add(item.Id, _playerSkillManager.FirstAid.Level);
                }
                
                // Apply Field medicine speed bonus to items
                if (originalFieldMedicineUseTimes.ContainsKey(item.TemplateId))
                {
                    ApplyFieldMedicineSpeedBonus(item);
                    _fieldMedicineInstanceIDs.Add(item.Id, _playerSkillManager.FieldMedicine.Level);
                }
                
            }

            yield break;
        }
    }
}