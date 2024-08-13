﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace SkillsExtended.Models
{
    public struct SkillDataResponse
    {
        public MedicalSkillData MedicalSkills;

        public WeaponSkillData NatoRifleSkill;

        public WeaponSkillData EasternRifleSkill;

        public LockPickingData LockPickingSkill;
    }

    public struct MedicalSkillData
    {
        [JsonProperty("ENABLE_FIELD_MEDICINE")]
        public bool EnableFieldMedicine;

        [JsonProperty("ENABLE_FIRST_AID")]
        public bool EnableFirstAid;

        [JsonProperty("FIRSTAID_XP_PER_ACTION")]
        public float FirstAidXpPerAction;

        [JsonProperty("FIELDMEDICINE_XP_PER_ACTION")]
        public float FieldMedicineXpPerAction;
        
        [JsonProperty("MEDKIT_USAGE_REDUCTION")]
        public float MedkitUsageReduction;

        [JsonProperty("MEDKIT_USAGE_REDUCTION_ELITE")]
        public float MedkitUsageReductionElite;

        [JsonProperty("MEDKIT_SPEED_BONUS")]
        public float ItemSpeedBonus;

        [JsonProperty("MEDKIT_SPEED_BONUS_ELITE")]
        public float ItemSpeedBonusElite;
        
        [JsonProperty("MEDKIT_RESOURCE_RATE")]
        public float MedkitResourceRateReduction;

        [JsonProperty("MEDKIT_RESOURCE_RATE_ELITE")]
        public float MedkitResourceRateReductionElite;

        [JsonProperty("FA_ITEM_LIST")]
        public List<string> FaItemList;

        [JsonProperty("FM_ITEM_LIST")]
        public List<string> FmItemList;
    }

    public struct WeaponSkillData
    {
        [JsonProperty("ENABLED")]
        public bool Enabled;

        [JsonProperty("WEAPON_PROF_XP")]
        public float WeaponProfXp;

        [JsonProperty("ERGO_MOD")]
        public float ErgoMod;

        [JsonProperty("ERGO_MOD_ELITE")]
        public float ErgoModElite;

        [JsonProperty("RECOIL_REDUCTION")]
        public float RecoilReduction;

        [JsonProperty("RECOIL_REDUCTION_ELITE")]
        public float RecoilReductionElite;

        [JsonProperty("SKILL_SHARE_ENABLED")]
        public bool SkillShareEnabled;

        [JsonProperty("SKILL_SHARE_XP_RATIO")]
        public float SkillShareXpRatio;

		[JsonProperty("WEAPONS")]
        public List<string> Weapons;
    }

    public struct LockPickingData
    {
        [JsonProperty("ENABLED")]
        public bool Enabled;

        [JsonProperty("INSPECT_BASE_TIME")]
        public float InspectBaseTime;

        [JsonProperty("INSPECT_CHANCE_BONUS")]
        public float InspectChanceBonus;

        [JsonProperty("PICK_BASE_TIME")]
        public float PickBaseTime;

        [JsonProperty("ATTEMPTS_BEFORE_BREAK")]
        public float AttemptsBeforeBreak;

        [JsonProperty("TIME_REDUCTION_BONUS_PER_LEVEL")]
        public float TimeReduction;

        [JsonProperty("TIME_REDUCTION_BONUS_PER_LEVEL_ELITE")]
        public float TimeReductionElite;

        [JsonProperty("PICK_BASE_SUCCESS_CHANCE")]
        public float PickBaseSuccessChance;

        [JsonProperty("PICK_BASE_DIFFICULTY_MOD")]
        public float PickBaseDifficultyMod;

        [JsonProperty("INSPECT_LOCK_XP_RATIO")]
        public float InspectLockXpRatio;

        [JsonProperty("XP_TABLE")]
        public Dictionary<string, float> XpTable;

        [JsonProperty("DOOR_PICK_LEVELS")]
        public DoorPickLevels DoorPickLevels;
    }

    // DoorId : level to pick the lock
    public struct DoorPickLevels
    {
        public Dictionary<string, int> Factory;
        public Dictionary<string, int> Woods;
        public Dictionary<string, int> Customs;
        public Dictionary<string, int> Interchange;
        public Dictionary<string, int> Reserve;
        public Dictionary<string, int> Shoreline;
        public Dictionary<string, int> Labs;
        public Dictionary<string, int> Lighthouse;
        public Dictionary<string, int> Streets;
        public Dictionary<string, int> GroundZero;
    }
}