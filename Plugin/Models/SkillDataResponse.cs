using Newtonsoft.Json;
using System.Collections.Generic;

namespace SkillsExtended.Models;

public struct SkillDataResponse
{
    [JsonProperty("FirstAid")] 
    public FirstAidData FirstAid;

    [JsonProperty("FieldMedicine")] 
    public FieldMedicineData FieldMedicine;

    [JsonProperty("NatoWeapons")] 
    public WeaponSkillData NatoWeapons;

    [JsonProperty("EasternWeapons")] 
    public WeaponSkillData EasternWeapons;

    [JsonProperty("LockPicking")] 
    public LockPickingData LockPicking;

    [JsonProperty("ProneMovement")] 
    public ProneMovementData ProneMovement;

    [JsonProperty("SilentOps")] 
    public SilentOpsData SilentOps;

    [JsonProperty("Strength")] 
    public StrengthData Strength;
}

public struct FirstAidData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("XP_PER_ACTION")] 
    public float FirstAidXpPerAction;

    [JsonProperty("MEDKIT_USAGE_REDUCTION")]
    public float MedkitUsageReduction;

    [JsonProperty("MEDKIT_USAGE_REDUCTION_ELITE")]
    public float MedkitUsageReductionElite;

    [JsonProperty("MEDKIT_SPEED_BONUS")] 
    public float ItemSpeedBonus;

    [JsonProperty("MEDKIT_SPEED_BONUS_ELITE")]
    public float ItemSpeedBonusElite;
}

public struct FieldMedicineData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("XP_PER_ACTION")] 
    public float FieldMedicineXpPerAction;

    [JsonProperty("SKILL_BONUS")] 
    public float SkillBonus;

    [JsonProperty("SKILL_BONUS_ELITE")] 
    public float SkillBonusElite;

    [JsonProperty("DURATION_BONUS")] 
    public float DurationBonus;

    [JsonProperty("DURATION_BONUS_ELITE")] 
    public float DurationBonusElite;

    [JsonProperty("POSITIVE_EFFECT_BONUS")]
    public float PositiveEffectChanceBonus;

    [JsonProperty("POSITIVE_EFFECT_BONUS_ELITE")]
    public float PositiveEffectChanceBonusElite;
}

public struct WeaponSkillData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction;

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

    [JsonProperty("PICK_STRENGTH")] 
    public float PickStrength;

    [JsonProperty("PICK_STRENGTH_PER_LEVEL")]
    public float PickStrengthPerLevel;

    [JsonProperty("SWEET_SPOT_RANGE")] 
    public float SweetSpotRange;

    [JsonProperty("SWEET_SPOT_RANGE_PER_LEVEL")]
    public float SweetSpotRangePerLevel;

    [JsonProperty("ATTEMPTS_BEFORE_BREAK")]
    public float AttemptsBeforeBreak;

    [JsonProperty("INSPECT_LOCK_XP_RATIO")]
    public float InspectLockXpRatio;

    [JsonProperty("FAILURE_LOCK_XP_RATIO")]
    public float FailureLockXpRatio;

    [JsonProperty("XP_TABLE")] 
    public Dictionary<string, float> XpTable;

    [JsonProperty("DOOR_PICK_LEVELS")] 
    public DoorPickLevels DoorPickLevels;
}

// DoorId : level to pick the lock
public struct DoorPickLevels
{
    [JsonProperty("Factory")] 
    public Dictionary<string, int> Factory;
    
    [JsonProperty("Woods")] 
    public Dictionary<string, int> Woods;
    
    [JsonProperty("Customs")] 
    public Dictionary<string, int> Customs;
    
    [JsonProperty("Interchange")] 
    public Dictionary<string, int> Interchange;
    
    [JsonProperty("Reserve")] 
    public Dictionary<string, int> Reserve;
    
    [JsonProperty("Shoreline")] 
    public Dictionary<string, int> Shoreline;
    
    [JsonProperty("Labs")] 
    public Dictionary<string, int> Labs;
    
    [JsonProperty("Lighthouse")] 
    public Dictionary<string, int> Lighthouse;
    
    [JsonProperty("Streets")] 
    public Dictionary<string, int> Streets;
    
    [JsonProperty("GroundZero")] 
    public Dictionary<string, int> GroundZero;
}

public struct ProneMovementData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction;

    [JsonProperty("MOVEMENT_SPEED_INCREASE_MAX")]
    public float MovementSpeedIncMax;

    [JsonProperty("MOVEMENT_SPEED_INCREASE_MAX_ELITE")]
    public float MovementSpeedIncMaxElite;

    [JsonProperty("MOVEMENT_VOLUME_DECREASE_MAX")]
    public float MovementVolumeDecMax;

    [JsonProperty("MOVEMENT_VOLUME_DECREASE_MAX_ELITE")]
    public float MovementVolumeDecMaxElite;
}

public struct SilentOpsData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction;

    [JsonProperty("MELEE_SPEED_INCREASE")] 
    public float MeleeSpeedInc;

    [JsonProperty("VOLUME_REDUCTION")] 
    public float VolumeReduction;

    [JsonProperty("SILENCER_PRICE_RED")] 
    public float SilencerPriceReduction;
}

public struct StrengthData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("COLLIDER_SPEED_BUFF")] 
    public float ColliderSpeedBuff;
}