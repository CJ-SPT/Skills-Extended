using EFT;
using EFT.UI;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using Comfort.Common;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using SkillsExtended.Helpers;
using System.Collections;
using System.Collections.Generic;
using System;
using SkillsExtended.Patches;

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

        public static SkillManager playerSkillManager;
        public static SkillManager ScavSkillManager;

        // Store the instance ID of the item and the level its bonus resource is set to.
        public Dictionary<string, int> firstAidInstanceIDs = new Dictionary<string, int>();
        public Dictionary<string, int> fieldMedicineInstanceIDs = new Dictionary<string, int>();

        // Store a dictionary of bodyparts to prevent the user from spam exploiting the leveling system.
        // Bodypart, Last time healed
        private Dictionary<EBodyPart, DateTime> _firstAidBodypartCahce = new Dictionary<EBodyPart, DateTime>();
        private Dictionary<EBodyPart, DateTime> _fieldMedicineBodyPartCache = new Dictionary<EBodyPart, DateTime>();

        private float FaPmcSpeedBonus => playerSkillManager.FirstAid.IsEliteLevel ? 1f - (playerSkillManager.FirstAid.Level * 0.007f) - 0.15f : 1f - (playerSkillManager.FirstAid.Level * 0.007f);
        private float FaScavSpeedBonus => ScavSkillManager.FirstAid.IsEliteLevel ? 1f - (ScavSkillManager.FirstAid.Level * 0.007f) - 0.15f : 1f - (ScavSkillManager.FirstAid.Level * 0.007f);
        private int FaHpBonus => playerSkillManager.FirstAid.IsEliteLevel ? playerSkillManager.FirstAid.Level * 10 : playerSkillManager.FirstAid.Level * 5;

        private float FmPmcSpeedBonus => playerSkillManager.FirstAid.IsEliteLevel ? 1f - (playerSkillManager.FirstAid.Level * 0.007f) - 0.15f : 1f - (playerSkillManager.FirstAid.Level * 0.007f);
        private float FmScavSpeedBonus => ScavSkillManager.FirstAid.IsEliteLevel ? 1f - (ScavSkillManager.FirstAid.Level * 0.007f) - 0.15f : 1f - (ScavSkillManager.FirstAid.Level * 0.007f);

        private void Awake()
        {
            new DoMedEffectPatch().Enable();
        }

        private void Update()
        {
            // Set skill manager instance
            if (playerSkillManager == null && Plugin.Session?.Profile?.Skills != null)
            {
                playerSkillManager = Plugin.Session.Profile.Skills;
                ScavSkillManager = Plugin.Session.ProfileOfPet.Skills;
         
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
                    playerSkillManager = player.Skills;
                } 
                else if (player.Side == EPlayerSide.Savage)
                {
                    ScavSkillManager = player.Skills;
                }
            }

            // Dont continue if session is null
            if (playerSkillManager == null) { return; }

            StaticManager.Instance.StartCoroutine(FirstAidUpdate());
        }

        public void ApplyFirstAidExp(EBodyPart bodypart)
        {
            float xpGain = 1.5f;
            
            if (!CanGainXPForLimb(_firstAidBodypartCahce, bodypart)) { return; }

            if (player.Side == EPlayerSide.Usec || player.Side == EPlayerSide.Bear)
            {
                playerSkillManager.FirstAid.SetCurrent(playerSkillManager.FirstAid.Current + xpGain, true);

                if (playerSkillManager.FirstAid.LevelProgress >= playerSkillManager.FirstAid.LevelExp)
                {
                    playerSkillManager.FirstAid.SetLevel(playerSkillManager.FirstAid.Level + 1);
                }

                Plugin.Log.LogDebug($"Skill: {playerSkillManager.FirstAid.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else if (player.Side == EPlayerSide.Savage)
            {
                ScavSkillManager.FirstAid.SetCurrent(ScavSkillManager.FirstAid.Current + xpGain, true);

                if (ScavSkillManager.FirstAid.LevelProgress >= ScavSkillManager.FirstAid.LevelExp)
                {
                    ScavSkillManager.FirstAid.SetLevel(ScavSkillManager.FirstAid.Level + 1);
                }

                Plugin.Log.LogDebug($"Skill: {ScavSkillManager.FirstAid.Id} Side: {player.Side} Gained: {xpGain} exp.");
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
                playerSkillManager.FieldMedicine.SetCurrent(playerSkillManager.FieldMedicine.Current + xpGain, true);

                if (playerSkillManager.FieldMedicine.LevelProgress >= playerSkillManager.FieldMedicine.LevelExp)
                {
                    playerSkillManager.FieldMedicine.SetLevel(playerSkillManager.FieldMedicine.Level + 1);
                }

                //_fieldMedicineBodyPartCache.Add(bodypart, );

                Plugin.Log.LogDebug($"Skill: {playerSkillManager.FieldMedicine.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else if (player.Side == EPlayerSide.Savage)
            {
                ScavSkillManager.FieldMedicine.SetCurrent(ScavSkillManager.FieldMedicine.Current + xpGain, true);

                if (ScavSkillManager.FieldMedicine.LevelProgress >= ScavSkillManager.FieldMedicine.LevelExp)
                {
                    ScavSkillManager.FieldMedicine.SetLevel(ScavSkillManager.FieldMedicine.Level + 1);
                }

                Plugin.Log.LogDebug($"Skill: {ScavSkillManager.FieldMedicine.Id} Side: {player.Side} Gained: {xpGain} exp.");
            }
            else
            {
                Plugin.Log.LogDebug($"No XP gain occured. Something went horribly wrong: Invalid Player side on field medicine.");
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
            if (firstAidInstanceIDs.ContainsKey(item.Id)) { return; }

            if (item is MedsClass meds && meds.MedKitComponent.MaxHpResource != _originalFirstAidHPValues[meds.TemplateId] + FaHpBonus)
            {
                GInterface249 newInterface;

                newInterface = new GInterface249Impl
                {
                    MaxHpResource = _originalFirstAidHPValues[meds.TemplateId] + FaHpBonus,
                    HpResourceRate = meds.MedKitComponent.HpResourceRate
                };

                Plugin.Log.LogDebug($"First Aid: Set instance {item.Id} of type {item.TemplateId} to {_originalFirstAidHPValues[meds.TemplateId] + FaHpBonus} HP");

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
                if (firstAidInstanceIDs.ContainsKey(item.Id))
                {
                    int previouslySet = firstAidInstanceIDs[item.Id];

                    if (previouslySet == playerSkillManager.FirstAid.Level) 
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

                    if (previouslySet == playerSkillManager.FieldMedicine.Level)
                    {
                        continue;
                    }
                    else
                    {
                        fieldMedicineInstanceIDs.Remove(item.Id);
                    }
                }

                // Apply first aid speed bonus to items
                if (_originalFirstAidUseTimes.ContainsKey(item.TemplateId))
                {
                    ApplyFirstAidSpeedBonus(item);
                    ApplyFirstAidHPBonus(item);
                    firstAidInstanceIDs.Add(item.Id, playerSkillManager.FirstAid.Level);
                }
                
                // Apply Field medicine speed bonus to items
                if (originalFieldMedicineUseTimes.ContainsKey(item.TemplateId))
                {
                    ApplyFieldMedicineSpeedBonus(item);
                    fieldMedicineInstanceIDs.Add(item.Id, playerSkillManager.FieldMedicine.Level);
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