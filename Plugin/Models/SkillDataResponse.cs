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

    [JsonProperty("Endurance")]
    public EnduranceData Endurance;

    [JsonProperty("Vitality")]
    public VitalityData Vitality;

    [JsonProperty("Health")]
    public HealthData Health;
}

public struct FirstAidData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction;

    [JsonProperty("MEDKIT_USAGE_REDUCTION_PER_LEVEL")]
    public float MedkitUsageReduction;
    
    [JsonProperty("MEDKIT_SPEED_BONUS_PER_LEVEL")] 
    public float ItemSpeedBonus;
}

public struct FieldMedicineData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction;

    [JsonProperty("SKILL_BONUS_PER_LEVEL")] 
    public float SkillBonus;
    
    [JsonProperty("DURATION_BONUS_PER_LEVEL")] 
    public float DurationBonus;

    [JsonProperty("POSITIVE_EFFECT_BONUS_PER_LEVEL")]
    public float PositiveEffectChanceBonus;
}

public struct WeaponSkillData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction;
    
    [JsonProperty("SKILL_SHARE_ENABLED")] 
    public bool SkillShareEnabled;

    [JsonProperty("SKILL_SHARE_XP_RATIO")] 
    public float SkillShareXpRatio;

    [JsonProperty("ERGO_MOD_PER_LEVEL")] 
    public float ErgoMod;

    [JsonProperty("RECOIL_REDUCTION_MOD_PER_LEVEL")] 
    public float RecoilReduction;
    
    [JsonProperty("WEAPONS")] 
    public HashSet<string> Weapons;
}

public struct LockPickingData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("PICK_STRENGTH_BASE")] 
    public float PickStrengthBase;

    [JsonProperty("PICK_STRENGTH_PER_LEVEL")]
    public float PickStrengthPerLevel;

    [JsonProperty("SWEET_SPOT_RANGE_BASE")] 
    public float SweetSpotRangeBase;

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

    [JsonProperty("MOVEMENT_SPEED_INCREASE_PER_LEVEL")]
    public float MovementSpeedInc;

    [JsonProperty("MOVEMENT_VOLUME_DECREASE_PER_LEVEL")]
    public float MovementVolumeDec;
}

public struct SilentOpsData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction;

    [JsonProperty("MELEE_SPEED_INCREASE_PER_LEVEL")] 
    public float MeleeSpeedInc;

    [JsonProperty("VOLUME_REDUCTION_PER_LEVEL")] 
    public float VolumeReduction;

    [JsonProperty("SILENCER_PRICE_RED_PER_LEVEL")] 
    public float SilencerPriceReduction;
}

public struct StrengthData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("COLLIDER_SPEED_BUFF_PER_LEVEL")] 
    public float ColliderSpeedBuff;

    [JsonProperty("BASE_ARMS_HP")]
    public float BaseArmsHp;

	[JsonProperty("ARMS_HP_PER_LEVEL")]
    public float ArmsHpBuff;

    [JsonProperty("ARMS_HP_ELITE")]
    public float ArmsHpElite;
}

public struct EnduranceData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("BASE_LEGS_HP")]
    public float BaseLegsHp;

	[JsonProperty("LEGS_HP_PER_LEVEL")]
    public float LegsHpBuff;

    [JsonProperty("LEGS_HP_ELITE")]
    public float LegsHpElite;
}

public struct VitalityData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

    [JsonProperty("BASE_THORAX_HP")]
    public float BaseThoraxHp;

    [JsonProperty("BASE_STOMACH_HP")]
    public float BaseStomachHp;

	[JsonProperty("TORSO_HP_PER_LEVEL")]
    public float TorsoHpBuff;

    [JsonProperty("TORSO_HP_ELITE")]
    public float TorsoHpElite;
}

public struct HealthData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled;

	[JsonProperty("BASE_HEAD_HP")]
	public float BaseHeadHp;

	[JsonProperty("HEAD_HP_PER_LEVEL")]
    public float HeadHpBuff;

    [JsonProperty("HEAD_HP_ELITE")]
    public float HeadHpElite;
}