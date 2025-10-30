using System.Text.Json.Serialization;

namespace SkillsExtended.Models;

public record SkillsConfig
{
    public required FirstAidConfig FirstAid { get; set; }
    public required FieldMedicineConfig FieldMedicine { get; set; }
    public required WeaponConfig NatoWeapons { get; set; }
    public required WeaponConfig EasternWeapons { get; set; }
    public required LockPickingConfig LockPicking { get; set; }
    public required ProneMovementConfig ProneMovement { get; set; }
    public required SilentOpsConfig SilentOps { get; set; }
    
    public required StrengthConfig Strength { get; set; }
    public required ShadowConnectionsConfig ShadowConnections { get; set; }
    
    public required BearRawPowerConfig BearRawPower { get; set; }
    
    public required UsecNegotiationsConfig UsecNegotiations { get; set; }
}

public record BaseSubConfig
{
    [JsonPropertyName("ENABLED")]
    public required bool Enabled { get; set; }
    
    [JsonPropertyName("XP_PER_ACTION")]
    public required float XpPerAction { get; set; }
}

public record FirstAidConfig : BaseSubConfig
{
    [JsonPropertyName("MEDKIT_USAGE_REDUCTION_PER_LEVEL")]
    public required float MedkitUsageReduction { get; set; }
    
    [JsonPropertyName("MEDKIT_SPEED_BONUS_PER_LEVEL")]
    public required float MedkitSpeedBonus { get; set; }
}

public record FieldMedicineConfig : BaseSubConfig
{
    [JsonPropertyName("SKILL_BONUS_PER_LEVEL")]
    public required float SkillBonus { get; set; }
    
    [JsonPropertyName("DURATION_BONUS_PER_LEVEL")]
    public required float DurationBonus { get; set; }
    
    [JsonPropertyName("POSITIVE_EFFECT_BONUS_PER_LEVEL")]
    public required float PositiveEffectBonus { get; set; }
}

public record WeaponConfig : BaseSubConfig
{
    [JsonPropertyName("SKILL_SHARE_ENABLED")]
    public required bool SkillShareEnabled { get; set; }

    [JsonPropertyName("SKILL_SHARE_XP_RATIO")]
    public required float SkillShareXpRatio { get; set; }
    
    [JsonPropertyName("ERGO_MOD_PER_LEVEL")]
    public required float ErgoMod { get; set; }

    [JsonPropertyName("RECOIL_REDUCTION_MOD_PER_LEVEL")]
    public required float RecoilReduction { get; set; }

    [JsonPropertyName("WEAPONS")]
    public required HashSet<string> Weapons { get; set; }
}

public record AdditionalWeaponsConfig
{
    [JsonPropertyName("AdditionalNatoWeapons")]
    public required HashSet<string> AdditionalNatoWeapons { get; set; }

    [JsonPropertyName("AdditionalEasternWeapons")]
    public required HashSet<string> AdditionalEasternWeapons { get; set; }
}

public record LockPickingConfig
{
    [JsonPropertyName("ENABLED")]
    public required bool Enabled { get; set; }

    [JsonPropertyName("PICK_STRENGTH_BASE")]
    public required float PickStrengthBase { get; set; }

    [JsonPropertyName("PICK_STRENGTH_PER_LEVEL")]
    public required float PickStrengthPerLevel { get; set; }

    [JsonPropertyName("SWEET_SPOT_RANGE_BASE")]
    public required float SweetSpotRange { get; set; }

    [JsonPropertyName("SWEET_SPOT_RANGE_PER_LEVEL")]
    public required float SweetSpotRangePerLevel { get; set; }

    [JsonPropertyName("ATTEMPTS_BEFORE_BREAK")]
    public required int AttemptsBeforeBreak { get; set; }

    [JsonPropertyName("INSPECT_LOCK_XP_RATIO")]
    public required float InspectLockXpRatio { get; set; }

    [JsonPropertyName("FAILURE_LOCK_XP_RATIO")]
    public required float FailureLockXpRatio { get; set; }

    [JsonPropertyName("XP_TABLE")]
    public required Dictionary<string, float> XpTable { get; set; }

    [JsonPropertyName("DOOR_PICK_LEVELS")]
    public required Dictionary<string, Dictionary<string, float>> DoorPickLevels { get; set; }
}

public record ProneMovementConfig : BaseSubConfig
{
    [JsonPropertyName("MOVEMENT_SPEED_INCREASE_PER_LEVEL")]
    public required float MovementSpeedInc { get; set; }

    [JsonPropertyName("MOVEMENT_VOLUME_DECREASE_PER_LEVEL")]
    public required float MovementVolumeDec { get; set; }
}

public record SilentOpsConfig : BaseSubConfig
{
    [JsonPropertyName("MELEE_SPEED_INCREASE_PER_LEVEL")]
    public required float MeleeSpeedIncrease { get; set; }

    [JsonPropertyName("VOLUME_REDUCTION_PER_LEVEL")]
    public required float VolumeReduction { get; set; }

    [JsonPropertyName("SILENCER_PRICE_RED_PER_LEVEL")]
    public required float SilencerPriceReduction { get; set; }
}

public record StrengthConfig
{
    [JsonPropertyName("ENABLED")]
    public required bool Enabled { get; set; }
    
    [JsonPropertyName("COLLIDER_SPEED_BUFF_PER_LEVEL")]
    public required float ColliderSpeedBuff { get; set; }
}

public record ShadowConnectionsConfig : BaseSubConfig
{
    [JsonPropertyName("SCAV_COOLDOWN_TIME_PER_LEVEL")]
    public required float ScavCooldownTimeDec { get; set; }
    
    [JsonPropertyName("CULTIST_CIRCLE_RETURN_TIME_PER_LEVEL")]
    public required float CultistCircleReturnTimeDec { get; set; }
}

public record BearRawPowerConfig : BaseSubConfig
{
    [JsonPropertyName("FACTION_LOCKED")]
    public required bool FactionLocked { get; set; }
    
    [JsonPropertyName("PRAPOR_TRADING_COST_PER_LEVEL")]
    public required float PraporTradingCostDec { get; set; }
    
    [JsonPropertyName("QUEST_EXP_REWARD_PER_LEVEL")]
    public required float QuestExpRewardInc { get; set; }
    
    [JsonPropertyName("ALL_TRADER_COST_DECREASE")]
    public required float AllTraderCostDecrease { get; set; }
}

public record UsecNegotiationsConfig : BaseSubConfig
{
    [JsonPropertyName("FACTION_LOCKED")]
    public required bool FactionLocked { get; set; }
    
    [JsonPropertyName("PEACEKEEPER_TRADING_COST_PER_LEVEL")]
    public required float PeacekeeperTradingCostDec { get; set; }
    
    [JsonPropertyName("QUEST_MONEY_REWARD_PER_LEVEL")]
    public required float QuestMoneyRewardInc { get; set; }
    
    [JsonPropertyName("ALL_TRADER_COST_DECREASE")]
    public required float AllTraderCostDecrease { get; set; }
}