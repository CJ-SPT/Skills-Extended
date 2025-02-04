using System.Text.Json.Serialization;

namespace ConfigEditor.Core.Models;

public class SkillsConfigModel
{
	[JsonPropertyName("FirstAid")]
	public FirstAidData FirstAid;

	[JsonPropertyName("FieldMedicine")]
	public FieldMedicineData FieldMedicine;

	[JsonPropertyName("NatoRifle")]
	public WeaponSkillData NatoRifle;

	[JsonPropertyName("EasternRifle")]
	public WeaponSkillData EasternRifle;

	[JsonPropertyName("LockPicking")]
	public LockPickingData LockPicking;

	[JsonPropertyName("ProneMovement")]
	public ProneMovementData ProneMovement;

	[JsonPropertyName("SilentOps")]
	public SilentOpsData SilentOps;

	[JsonPropertyName("Strength")]
	public StrengthData Strength;
}

public class FirstAidData
{
	[JsonPropertyName("ENABLED")] 
	public bool Enabled;

	[JsonPropertyName("XP_PER_ACTION")] 
	public float FirstAidXpPerAction;

	[JsonPropertyName("MEDKIT_USAGE_REDUCTION")]
	public float MedkitUsageReduction;

	[JsonPropertyName("MEDKIT_USAGE_REDUCTION_ELITE")]
	public float MedkitUsageReductionElite;

	[JsonPropertyName("MEDKIT_SPEED_BONUS")]
	public float ItemSpeedBonus;

	[JsonPropertyName("MEDKIT_SPEED_BONUS_ELITE")]
	public float ItemSpeedBonusElite;
}

public class FieldMedicineData
{
	[JsonPropertyName("ENABLED")] 
	public bool Enabled;

	[JsonPropertyName("XP_PER_ACTION")] 
	public float FieldMedicineXpPerAction;

	[JsonPropertyName("SKILL_BONUS")] 
	public float SkillBonus;

	[JsonPropertyName("SKILL_BONUS_ELITE")]
	public float SkillBonusElite;

	[JsonPropertyName("DURATION_BONUS")] 
	public float DurationBonus;

	[JsonPropertyName("DURATION_BONUS_ELITE")]
	public float DurationBonusElite;

	[JsonPropertyName("POSITIVE_EFFECT_BONUS")]
	public float PositiveEffectChanceBonus;

	[JsonPropertyName("POSITIVE_EFFECT_BONUS_ELITE")]
	public float PositiveEffectChanceBonusElite;
}

public class WeaponSkillData
{
	[JsonPropertyName("ENABLED")] 
	public bool Enabled;

	[JsonPropertyName("WEAPON_PROF_XP")] 
	public float WeaponProfXp;

	[JsonPropertyName("ERGO_MOD")] 
	public float ErgoMod;

	[JsonPropertyName("ERGO_MOD_ELITE")] 
	public float ErgoModElite;

	[JsonPropertyName("RECOIL_REDUCTION")] 
	public float RecoilReduction;

	[JsonPropertyName("RECOIL_REDUCTION_ELITE")]
	public float RecoilReductionElite;

	[JsonPropertyName("SKILL_SHARE_ENABLED")]
	public bool SkillShareEnabled;

	[JsonPropertyName("SKILL_SHARE_XP_RATIO")]
	public float SkillShareXpRatio;

	[JsonPropertyName("WEAPONS")] 
	public List<string> Weapons;
}

public class LockPickingData
{
	[JsonPropertyName("ENABLED")] 
	public bool Enabled;

	[JsonPropertyName("PICK_STRENGTH")] 
	public float PickStrength;

	[JsonPropertyName("PICK_STRENGTH_PER_LEVEL")]
	public float PickStrengthPerLevel;

	[JsonPropertyName("SWEET_SPOT_RANGE")] 
	public float SweetSpotRange;

	[JsonPropertyName("SWEET_SPOT_RANGE_PER_LEVEL")]
	public float SweetSpotRangePerLevel;

	[JsonPropertyName("ATTEMPTS_BEFORE_BREAK")]
	public float AttemptsBeforeBreak;

	[JsonPropertyName("INSPECT_LOCK_XP_RATIO")]
	public float InspectLockXpRatio;

	[JsonPropertyName("FAILURE_LOCK_XP_RATIO")]
	public float FailureLockXpRatio;

	[JsonPropertyName("XP_TABLE")] 
	public Dictionary<string, float> XpTable;

	[JsonPropertyName("DOOR_PICK_LEVELS")] 
	public DoorPickLevels DoorPickLevels;
}

// DoorId : level to pick the lock
public class DoorPickLevels
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

public class ProneMovementData
{
	[JsonPropertyName("ENABLED")] public bool Enabled;

	[JsonPropertyName("XP_PER_ACTION")] public float XpPerAction;

	[JsonPropertyName("MOVEMENT_SPEED_INCREASE_MAX")]
	public float MovementSpeedIncMax;

	[JsonPropertyName("MOVEMENT_SPEED_INCREASE_MAX_ELITE")]
	public float MovementSpeedIncMaxElite;

	[JsonPropertyName("MOVEMENT_VOLUME_DECREASE_MAX")]
	public float MovementVolumeDecMax;

	[JsonPropertyName("MOVEMENT_VOLUME_DECREASE_MAX_ELITE")]
	public float MovementVolumeDecMaxElite;
}

public class SilentOpsData
{
	[JsonPropertyName("ENABLED")] public bool Enabled;

	[JsonPropertyName("XP_PER_ACTION")] 
	public float XpPerAction;

	[JsonPropertyName("MELEE_SPEED_INCREASE")]
	public float MeleeSpeedInc;

	[JsonPropertyName("VOLUME_REDUCTION")] 
	public float VolumeReduction;

	[JsonPropertyName("SILENCER_PRICE_RED")]
	public float SilencerPriceReduction;
}

public class StrengthData
{
	[JsonPropertyName("ENABLED")] 
	public bool Enabled;

	[JsonPropertyName("COLLIDER_SPEED_BUFF")]
	public float ColliderSpeedBuff;
}