using System.Text.Json.Serialization;

namespace SkillsExtended.Models;

public record SkillsConfig
{
    public required FirstAidSubConfig FirstAid { get; set; }
    public required FieldMedicineSubConfig FieldMedicine { get; set; }
    public required WeaponConfig NatoWeapons { get; set; }
    public required WeaponConfig EasternWeapons { get; set; }
    public required LockPickingConfig LockPicking { get; set; }
    public required ProneMovementConfig ProneMovement { get; set; }
    public required SilentOpsConfig SilentOps { get; set; }
    public required StrengthConfig Strength { get; set; }
}

public record BaseSubConfig
{
    [JsonPropertyName("ENABLED")]
    public required bool Enabled { get; set; }
    
    [JsonPropertyName("XP_PER_ACTION")]
    public required float XpPerAction { get; set; }
}

public record FirstAidSubConfig : BaseSubConfig
{
    [JsonPropertyName("MEDKIT_USAGE_REDUCTION")]
    public required float MedkitUsageReduction { get; set; }
    
    [JsonPropertyName("MEDKIT_USAGE_REDUCTION_ELITE")]
    public required float MedkitUsageReductionElite { get; set; }
    
    [JsonPropertyName("MEDKIT_SPEED_BONUS")]
    public required float MedkitSpeedBonus { get; set; }
    
    [JsonPropertyName("MEDKIT_SPEED_BONUS_ELITE")]
    public required float MedkitSpeedBonusElite { get; set; }
}

public record FieldMedicineSubConfig : BaseSubConfig
{
    [JsonPropertyName("SKILL_BONUS")]
    public required float SkillBonus { get; set; }
    
    [JsonPropertyName("SKILL_BONUS_ELITE")]
    public required float SkillBonusElite { get; set; }
    
    [JsonPropertyName("DURATION_BONUS")]
    public required float DurationBonus { get; set; }
    
    [JsonPropertyName("DURATION_BONUS_ELITE")]
    public required float DurationBonusElite { get; set; }
    
    [JsonPropertyName("POSITIVE_EFFECT_BONUS")]
    public required float PositiveEffectBonus { get; set; }
    
    [JsonPropertyName("POSITIVE_EFFECT_BONUS_ELITE")]
    public required float PositiveEffectBonusElite { get; set; }
}

public record WeaponConfig : BaseSubConfig
{
    [JsonPropertyName("SKILL_SHARE_ENABLED")]
    public required bool SkillShareEnabled { get; set; }

    [JsonPropertyName("SKILL_SHARE_XP_RATIO")]
    public required float SkillShareXpRatio { get; set; }
    
    [JsonPropertyName("ERGO_MOD")]
    public required float ErgoMod { get; set; }

    [JsonPropertyName("ERGO_MOD_ELITE")]
    public required float ErgoModElite { get; set; }

    [JsonPropertyName("RECOIL_REDUCTION")]
    public required float RecoilReduction { get; set; }

    [JsonPropertyName("RECOIL_REDUCTION_ELITE")]
    public required float RecoilReductionElite { get; set; }

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

    [JsonPropertyName("PICK_STRENGTH")]
    public required float PickStrength { get; set; }

    [JsonPropertyName("PICK_STRENGTH_PER_LEVEL")]
    public required float PickStrengthPerLevel { get; set; }

    [JsonPropertyName("SWEET_SPOT_RANGE")]
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
    [JsonPropertyName("MOVEMENT_SPEED_INCREASE_MAX")]
    public required float MovementSpeedIncreaseMax { get; set; }

    [JsonPropertyName("MOVEMENT_SPEED_INCREASE_MAX_ELITE")]
    public required float MovementSpeedIncreaseMaxElite { get; set; }

    [JsonPropertyName("MOVEMENT_VOLUME_DECREASE_MAX")]
    public required float MovementVolumeDecreaseMax { get; set; }

    [JsonPropertyName("MOVEMENT_VOLUME_DECREASE_MAX_ELITE")]
    public required float MovementVolumeDecreaseMaxElite { get; set; }
}

public record SilentOpsConfig : BaseSubConfig
{
    [JsonPropertyName("MELEE_SPEED_INCREASE")]
    public required float MeleeSpeedIncrease { get; set; }

    [JsonPropertyName("VOLUME_REDUCTION")]
    public required float VolumeReduction { get; set; }

    [JsonPropertyName("SILENCER_PRICE_RED")]
    public required float SilencerPriceReduction { get; set; }
}

public record StrengthConfig
{
    [JsonPropertyName("ENABLED")]
    public required bool Enabled { get; set; }
    
    [JsonPropertyName("COLLIDER_SPEED_BUFF")]
    public required float ColliderSpeedBuff { get; set; }
}