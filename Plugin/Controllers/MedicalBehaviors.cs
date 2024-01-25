using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkillsExtended.Controllers
{
    public class MedicalBehavior : MonoBehaviour
    {
        public sealed class MedKitValues : GInterface249
        {
            public int MaxHpResource { set; get; }
            public float HpResourceRate { set; get; }
        }

        public sealed class HealthEffectValues : GInterface243
        {
            public float UseTime { set; get; }

            public KeyValuePair<EBodyPart, float>[] BodyPartTimeMults { set; get; }

            public Dictionary<EHealthFactorType, GClass1146> HealthEffects { set; get; }

            public Dictionary<EDamageEffectType, GClass1145> DamageEffects { set; get; }

            public string StimulatorBuffs { set; get; }
        }

        public List<string> firstAidItemList = new List<string>
        {
            "544fb45d4bdc2dee738b4568", // Salewa
            "5755356824597772cb798962", // AI-2
            "590c657e86f77412b013051d", // Grizzly
            "590c661e86f7741e566b646a", // Car
            "590c678286f77426c9660122", // Ifak
            "5e99711486f7744bfc4af328", // Sanitars
            "60098ad7c2240c0fe85c570a"  // AFAK
        };

        public List<string> fieldMedicineItemList = new List<string>
        {
            "544fb25a4bdc2dfb738b4567", // bandage
            "5751a25924597722c463c472", // army bandage
            "5e831507ea0a7c419c2f9bd9", // esmarch
            "60098af40accd37ef2175f27", // CAT
            "5e8488fa988a8701445df1e4", // calok-b
            "544fb3364bdc2d34748b456a", // splint
            "5af0454c86f7746bf20992e8"  // alu splint
        };

        private Dictionary<string, MedKitValues> _originalMedKitValues = new Dictionary<string, MedKitValues>();
        private Dictionary<string, HealthEffectValues> _originalHealthEffectValues = new Dictionary<string, HealthEffectValues>();

        private GameWorld gameWorld { get => Singleton<GameWorld>.Instance; }

        private Player player { get => gameWorld.MainPlayer; }

        public SkillManager _playerSkillManager;
        public SkillManager _scavSkillManager;

        // Store the instance ID of the item and the level its bonus resource is set to.
        public Dictionary<string, int> firstAidInstanceIDs = new Dictionary<string, int>();

        public Dictionary<string, int> fieldMedicineInstanceIDs = new Dictionary<string, int>();

        // Store a dictionary of bodyparts to prevent the user from spam exploiting the leveling system.
        // Bodypart, Last time healed
        private Dictionary<EBodyPart, DateTime> _firstAidBodypartCahce = new Dictionary<EBodyPart, DateTime>();

        private Dictionary<EBodyPart, DateTime> _fieldMedicineBodyPartCache = new Dictionary<EBodyPart, DateTime>();

        private float FaPmcSpeedBonus => _playerSkillManager.FirstAid.IsEliteLevel ? 1f - (_playerSkillManager.FirstAid.Level * 0.007f) - 0.15f : 1f - (_playerSkillManager.FirstAid.Level * 0.007f);
        private float FaScavSpeedBonus => _scavSkillManager.FirstAid.IsEliteLevel ? 1f - (_scavSkillManager.FirstAid.Level * 0.007f) - 0.15f : 1f - (_scavSkillManager.FirstAid.Level * 0.007f);
        private int FaHpBonus => _playerSkillManager.FirstAid.IsEliteLevel ? _playerSkillManager.FirstAid.Level * 10 : _playerSkillManager.FirstAid.Level * 5;

        private float FmPmcSpeedBonus => _playerSkillManager.FirstAid.IsEliteLevel ? 1f - (_playerSkillManager.FirstAid.Level * 0.007f) - 0.15f : 1f - (_playerSkillManager.FirstAid.Level * 0.007f);
        private float FmScavSpeedBonus => _scavSkillManager.FirstAid.IsEliteLevel ? 1f - (_scavSkillManager.FirstAid.Level * 0.007f) - 0.15f : 1f - (_scavSkillManager.FirstAid.Level * 0.007f);

        private void Awake()
        {
            new DoMedEffectPatch().Enable();
        }

        private void Update()
        {
            // Set skill manager instance
            if (_playerSkillManager == null && Plugin.Session?.Profile?.Skills != null)
            {
                _playerSkillManager = Plugin.Session.Profile.Skills;
                _scavSkillManager = Plugin.Session.ProfileOfPet.Skills;

                Plugin.Log.LogDebug("Medical Component Initialized.");
            }

            if (gameWorld?.MainPlayer == null)
            {
                _fieldMedicineBodyPartCache.Clear();
                _firstAidBodypartCahce.Clear();
            }

            if (gameWorld?.MainPlayer != null)
            {
                if (player.Side == EPlayerSide.Bear || player.Side == EPlayerSide.Usec)
                {
                    _playerSkillManager = player.Skills;
                }
                else if (player.Side == EPlayerSide.Savage)
                {
                    _scavSkillManager = player.Skills;
                }
            }

            // Dont continue if session is null
            if (_playerSkillManager == null) { return; }

            StaticManager.Instance.StartCoroutine(FirstAidUpdate());
        }

        public void ApplyFirstAidExp(EBodyPart bodypart)
        {
            float xpGain = 1.5f;

            if (!CanGainXPForLimb(_firstAidBodypartCahce, bodypart)) { return; }

            if (player.Side == EPlayerSide.Usec || player.Side == EPlayerSide.Bear)
            {
                _playerSkillManager.FirstAid.SetCurrent(_playerSkillManager.FirstAid.Current + xpGain, true);

                Plugin.Log.LogDebug($"Skill: {_playerSkillManager.FirstAid.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else if (player.Side == EPlayerSide.Savage)
            {
                _scavSkillManager.FirstAid.SetCurrent(_scavSkillManager.FirstAid.Current + xpGain, true);

                Plugin.Log.LogDebug($"Skill: {_scavSkillManager.FirstAid.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else
            {
                Plugin.Log.LogDebug($"No XP gain occured. Something went horribly wrong: Invalid Player side on first aid.");
            }
        }

        public void ApplyFieldMedicineExp(EBodyPart bodypart)
        {
            float xpGain = 2.5f;

            // If we recently healed this limb, return
            if (!CanGainXPForLimb(_fieldMedicineBodyPartCache, bodypart)) { return; }

            if (player.Side == EPlayerSide.Usec || player.Side == EPlayerSide.Bear)
            {
                _playerSkillManager.FieldMedicine.SetCurrent(_playerSkillManager.FieldMedicine.Current + xpGain, true);

                Plugin.Log.LogDebug($"Skill: {_playerSkillManager.FieldMedicine.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else if (player.Side == EPlayerSide.Savage)
            {
                _scavSkillManager.FieldMedicine.SetCurrent(_scavSkillManager.FieldMedicine.Current + xpGain, true);

                Plugin.Log.LogDebug($"Skill: {_scavSkillManager.FieldMedicine.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
        }

        private void ApplyFirstAidSpeedBonus(Item item)
        {
            float bonus = 0f;

            if (firstAidInstanceIDs.ContainsKey(item.Id)) { return; }

            // If we're in the menu always use the PMC bonus
            // Otherwise check the player side for the applicable bonus
            if (gameWorld == null)
            {
                bonus = FaPmcSpeedBonus;
            }
            else
            {
                if (player.Side == EPlayerSide.Bear || player.Side == EPlayerSide.Usec)
                {
                    bonus = FaPmcSpeedBonus;
                }
                else if (player.Side == EPlayerSide.Savage)
                {
                    bonus = FaScavSpeedBonus;
                }
            }

            if (item is MedsClass meds)
            {
                if (!_originalHealthEffectValues.ContainsKey(item.TemplateId))
                {
                    var origValues = new HealthEffectValues();

                    origValues.UseTime = meds.HealthEffectsComponent.UseTime;
                    origValues.BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults;
                    origValues.HealthEffects = meds.HealthEffectsComponent.HealthEffects;
                    origValues.DamageEffects = meds.HealthEffectsComponent.DamageEffects;
                    origValues.StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs;

                    _originalHealthEffectValues.Add(item.TemplateId, origValues);
                }

                GInterface243 newGInterface = new HealthEffectValues
                {
                    UseTime = _originalHealthEffectValues[meds.TemplateId].UseTime * bonus,
                    BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults,
                    HealthEffects = meds.HealthEffectsComponent.HealthEffects,
                    DamageEffects = meds.HealthEffectsComponent.DamageEffects,
                    StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs
                };

                var healthEffectComp = AccessTools.Field(typeof(MedsClass), "HealthEffectsComponent").GetValue(meds);
                AccessTools.Field(typeof(HealthEffectsComponent), "ginterface243_0").SetValue(healthEffectComp, newGInterface);

                Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {_originalHealthEffectValues[meds.TemplateId].UseTime * bonus} seconds");
            }
        }

        private void ApplyFirstAidHPBonus(Item item)
        {
            if (firstAidInstanceIDs.ContainsKey(item.Id)) { return; }

            if (item is MedsClass meds)
            {
                GInterface249 newInterface;

                // Add the original medkit template to the original dictionary
                if (!_originalMedKitValues.ContainsKey(item.TemplateId))
                {
                    var origValues = new MedKitValues();

                    origValues.MaxHpResource = meds.MedKitComponent.MaxHpResource;
                    origValues.HpResourceRate = meds.MedKitComponent.HpResourceRate;

                    _originalMedKitValues.Add(item.TemplateId, origValues);
                }

                newInterface = new MedKitValues
                {
                    MaxHpResource = _originalMedKitValues[item.TemplateId].MaxHpResource + FaHpBonus,
                    HpResourceRate = meds.MedKitComponent.HpResourceRate
                };

                Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {_originalMedKitValues[meds.TemplateId].MaxHpResource + FaHpBonus} HP");

                var currentResouce = meds.MedKitComponent.HpResource;
                var currentMaxResouce = meds.MedKitComponent.MaxHpResource;

                // Only change the current resource if the item is unused.
                if (currentResouce == currentMaxResouce)
                {
                    meds.MedKitComponent.HpResource = newInterface.MaxHpResource;
                }

                var medComp = AccessTools.Field(typeof(MedsClass), "MedKitComponent").GetValue(meds);
                AccessTools.Field(typeof(MedKitComponent), "ginterface249_0").SetValue(medComp, newInterface);
                AccessTools.Field(typeof(MedKitComponent), "ginterface249_0").SetValue(medComp, newInterface);
            }
        }

        private void ApplyFieldMedicineSpeedBonus(Item item)
        {
            float bonus = 1f;

            if (gameWorld == null)
            {
                bonus = FmPmcSpeedBonus;
            }
            else
            {
                if (player.Side == EPlayerSide.Bear || player.Side == EPlayerSide.Usec)
                {
                    bonus = FmPmcSpeedBonus;
                }
                else if (player.Side == EPlayerSide.Savage)
                {
                    bonus = FmScavSpeedBonus;
                }
            }

            if (item is MedsClass meds)
            {
                if (!_originalHealthEffectValues.ContainsKey(item.TemplateId))
                {
                    var origValues = new HealthEffectValues();

                    origValues.UseTime = meds.HealthEffectsComponent.UseTime;
                    origValues.BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults;
                    origValues.HealthEffects = meds.HealthEffectsComponent.HealthEffects;
                    origValues.DamageEffects = meds.HealthEffectsComponent.DamageEffects;
                    origValues.StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs;

                    _originalHealthEffectValues.Add(item.TemplateId, origValues);
                }

                GInterface243 newGInterface = new HealthEffectValues
                {
                    UseTime = _originalHealthEffectValues[meds.TemplateId].UseTime * bonus,
                    BodyPartTimeMults = meds.HealthEffectsComponent.BodyPartTimeMults,
                    HealthEffects = meds.HealthEffectsComponent.HealthEffects,
                    DamageEffects = meds.HealthEffectsComponent.DamageEffects,
                    StimulatorBuffs = meds.HealthEffectsComponent.StimulatorBuffs
                };

                var healthEffectComp = AccessTools.Field(typeof(MedsClass), "HealthEffectsComponent").GetValue(meds);
                AccessTools.Field(typeof(HealthEffectsComponent), "ginterface243_0").SetValue(healthEffectComp, newGInterface);

                Plugin.Log.LogDebug($"Field Medicine: Set instance {item.Id} of type {item.TemplateId} to {_originalHealthEffectValues[meds.TemplateId].UseTime * bonus} seconds");
            }
        }

        private IEnumerator FirstAidUpdate()
        {
            var items = Plugin.Session.Profile.Inventory.AllPlayerItems.Where(x => x is MedsClass);

            foreach (var item in items)
            {
                // Skip if we already set this first aid item.
                if (firstAidInstanceIDs.ContainsKey(item.Id))
                {
                    int previouslySet = firstAidInstanceIDs[item.Id];

                    if (previouslySet == _playerSkillManager.FirstAid.Level)
                    {
                        continue;
                    }
                    else
                    {
                        firstAidInstanceIDs.Remove(item.Id);
                    }
                }

                // Skip if we already set this field medicine item.
                if (fieldMedicineInstanceIDs.ContainsKey(item.Id))
                {
                    int previouslySet = fieldMedicineInstanceIDs[item.Id];

                    if (previouslySet == _playerSkillManager.FieldMedicine.Level)
                    {
                        continue;
                    }
                    else
                    {
                        fieldMedicineInstanceIDs.Remove(item.Id);
                    }
                }

                // Apply first aid speed bonus to items
                if (firstAidItemList.Contains(item.TemplateId))
                {
                    ApplyFirstAidSpeedBonus(item);
                    ApplyFirstAidHPBonus(item);
                    firstAidInstanceIDs.Add(item.Id, _playerSkillManager.FirstAid.Level);
                }

                // Apply Field medicine speed bonus to items
                if (fieldMedicineItemList.Contains(item.TemplateId))
                {
                    ApplyFieldMedicineSpeedBonus(item);
                    fieldMedicineInstanceIDs.Add(item.Id, _playerSkillManager.FieldMedicine.Level);
                }
            }

            yield break;
        }

        private bool CanGainXPForLimb(Dictionary<EBodyPart, DateTime> dict, EBodyPart bodypart)
        {
            if (!dict.ContainsKey(bodypart))
            {
                dict.Add(bodypart, DateTime.Now);
                return true;
            }
            else
            {
                TimeSpan elapsed = DateTime.Now - dict[bodypart];

                if (elapsed.TotalSeconds >= 60)
                {
                    dict.Remove(bodypart);
                    return true;
                }

                Plugin.Log.LogDebug($"Time until next available xp: {60 - elapsed.TotalSeconds} seconds");
                return false;
            }
        }
    }
}