using Newtonsoft.Json;
using System.Collections.Generic;

namespace SkillsExtended.Models;

public struct SkillDataResponse
{
    [JsonProperty("FirstAid")] 
    public FirstAidData FirstAid { get; set; }

    [JsonProperty("FieldMedicine")] 
    public FieldMedicineData FieldMedicine { get; set; }

    [JsonProperty("NatoWeapons")] 
    public WeaponSkillData NatoWeapons { get; set; }

    [JsonProperty("EasternWeapons")] 
    public WeaponSkillData EasternWeapons { get; set; }

    [JsonProperty("LockPicking")] 
    public LockPickingData LockPicking { get; set; }

    [JsonProperty("ProneMovement")] 
    public ProneMovementData ProneMovement { get; set; }

    [JsonProperty("SilentOps")] 
    public SilentOpsData SilentOps { get; set; }

    [JsonProperty("Strength")] 
    public StrengthData Strength { get; set; }
    
    [JsonProperty("ShadowConnections")]
    public ShadowConnectionsData ShadowConnections { get; set; }
    
    [JsonProperty("BearRawPower")]
    public BearRawPowerData BearRawPower { get; set; }
    
    [JsonProperty("UsecNegotiations")]
    public UsecNegotiationsData UsecNegotiations { get; set; }
}

public struct FirstAidData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled { get; set; }

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction { get; set; }

    [JsonProperty("MEDKIT_USAGE_REDUCTION_PER_LEVEL")]
    public float MedkitUsageReduction { get; set; }
    
    [JsonProperty("MEDKIT_SPEED_BONUS_PER_LEVEL")] 
    public float ItemSpeedBonus { get; set; }
}

public struct FieldMedicineData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled { get; set; }

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction { get; set; }

    [JsonProperty("SKILL_BONUS_PER_LEVEL")] 
    public float SkillBonus { get; set; }
    
    [JsonProperty("DURATION_BONUS_PER_LEVEL")] 
    public float DurationBonus { get; set; }

    [JsonProperty("POSITIVE_EFFECT_BONUS_PER_LEVEL")]
    public float PositiveEffectChanceBonus { get; set; }
}

public struct WeaponSkillData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled { get; set; }

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction { get; set; }
    
    [JsonProperty("SKILL_SHARE_ENABLED")] 
    public bool SkillShareEnabled { get; set; }

    [JsonProperty("SKILL_SHARE_XP_RATIO")] 
    public float SkillShareXpRatio { get; set; }

    [JsonProperty("ERGO_MOD_PER_LEVEL")] 
    public float ErgoMod { get; set; }

    [JsonProperty("RECOIL_REDUCTION_MOD_PER_LEVEL")] 
    public float RecoilReduction { get; set; }
    
    [JsonProperty("WEAPONS")] 
    public HashSet<string> Weapons { get; set; }
}

public struct LockPickingData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled { get; set; }

    [JsonProperty("PICK_STRENGTH_BASE")] 
    public float PickStrengthBase { get; set; }

    [JsonProperty("PICK_STRENGTH_PER_LEVEL")]
    public float PickStrengthPerLevel { get; set; }

    [JsonProperty("SWEET_SPOT_RANGE_BASE")] 
    public float SweetSpotRangeBase { get; set; }

    [JsonProperty("SWEET_SPOT_RANGE_PER_LEVEL")]
    public float SweetSpotRangePerLevel { get; set; }

    [JsonProperty("ATTEMPTS_BEFORE_BREAK")]
    public int AttemptsBeforeBreak { get; set; }

    [JsonProperty("INSPECT_LOCK_XP_RATIO")]
    public float InspectLockXpRatio { get; set; }

    [JsonProperty("FAILURE_LOCK_XP_RATIO")]
    public float FailureLockXpRatio { get; set; }

    [JsonProperty("XP_TABLE")] 
    public Dictionary<string, float> XpTable { get; set; }

    [JsonProperty("DOOR_PICK_LEVELS")] 
    public DoorPickLevels DoorPickLevels { get; set; }
}

// DoorId : level to pick the lock
public struct DoorPickLevels
{
    [JsonProperty("Factory")] 
    public Dictionary<string, int> Factory { get; set; }
    
    [JsonProperty("Woods")] 
    public Dictionary<string, int> Woods { get; set; }
    
    [JsonProperty("Customs")] 
    public Dictionary<string, int> Customs { get; set; }
    
    [JsonProperty("Interchange")] 
    public Dictionary<string, int> Interchange { get; set; }
    
    [JsonProperty("Reserve")] 
    public Dictionary<string, int> Reserve { get; set; }
    
    [JsonProperty("Shoreline")] 
    public Dictionary<string, int> Shoreline { get; set; }
    
    [JsonProperty("Labs")] 
    public Dictionary<string, int> Labs { get; set; }
    
    [JsonProperty("Lighthouse")] 
    public Dictionary<string, int> Lighthouse { get; set; }
    
    [JsonProperty("Streets")] 
    public Dictionary<string, int> Streets { get; set; }
    
    [JsonProperty("GroundZero")] 
    public Dictionary<string, int> GroundZero { get; set; }
    
    [JsonProperty("Labyrinth")] 
    public Dictionary<string, int> Labyrinth { get; set; }
}

public struct ProneMovementData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled { get; set; }

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction { get; set; }

    [JsonProperty("MOVEMENT_SPEED_INCREASE_PER_LEVEL")]
    public float MovementSpeedInc { get; set; }

    [JsonProperty("MOVEMENT_VOLUME_DECREASE_PER_LEVEL")]
    public float MovementVolumeDec { get; set; }
}

public struct SilentOpsData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled { get; set; }

    [JsonProperty("XP_PER_ACTION")] 
    public float XpPerAction { get; set; }

    [JsonProperty("MELEE_SPEED_INCREASE_PER_LEVEL")] 
    public float MeleeSpeedInc { get; set; }

    [JsonProperty("VOLUME_REDUCTION_PER_LEVEL")] 
    public float VolumeReduction { get; set; }

    [JsonProperty("SILENCER_PRICE_RED_PER_LEVEL")] 
    public float SilencerPriceReduction { get; set; }
}

public struct StrengthData
{
    [JsonProperty("ENABLED")] 
    public bool Enabled { get; set; }

    [JsonProperty("COLLIDER_SPEED_BUFF_PER_LEVEL")] 
    public float ColliderSpeedBuff { get; set; }
}

public struct ShadowConnectionsData
{
    [JsonProperty("ENABLED")]
    public bool Enabled { get; set; }
    
    [JsonProperty("XP_PER_ACTION")]
    public float XpPerAction { get; set; }
    
    [JsonProperty("SCAV_COOLDOWN_TIME_PER_LEVEL")] 
    public float ScavCooldownTimeReduction { get; set; }
    
    [JsonProperty("CULTIST_CIRCLE_RETURN_TIME_PER_LEVEL")] 
    public float CultistCircleReturnTimeReduction { get; set; }
}

public struct BearRawPowerData
{
    [JsonProperty("ENABLED")]
    public bool Enabled { get; set; }
    
    [JsonProperty("XP_PER_ACTION")]
    public float XpPerAction { get; set; }

    [JsonProperty("FACTION_LOCKED")] 
    public bool FactionLocked { get; set; }

    [JsonProperty("PRAPOR_TRADING_COST_PER_LEVEL")]
    public float PraporTradingCostDec { get; set; }

    [JsonProperty("QUEST_EXP_REWARD_PER_LEVEL")]
    public float QuestExpRewardInc { get; set; }

    [JsonProperty("ALL_TRADER_COST_DECREASE")]
    public float AllTraderCostDecrease { get; set; }
}

public struct UsecNegotiationsData
{
    [JsonProperty("ENABLED")]
    public bool Enabled { get; set; }
    
    [JsonProperty("XP_PER_ACTION")]
    public float XpPerAction { get; set; }

    [JsonProperty("FACTION_LOCKED")] 
    public bool FactionLocked { get; set; }

    [JsonProperty("PEACEKEEPER_TRADING_COST_PER_LEVEL")]
    public float PeacekeeperTradingCostDec { get; set; }

    [JsonProperty("QUEST_MONEY_REWARD_PER_LEVEL")]
    public float QuestMoneyRewardInc { get; set; }

    [JsonProperty("ALL_TRADER_COST_DECREASE")]
    public float AllTraderCostDecrease { get; set; }
}