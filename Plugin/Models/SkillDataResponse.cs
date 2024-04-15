using Newtonsoft.Json;

namespace SkillsExtended.Models
{
    public struct SkillDataResponse
    {
        public MedicalSkillData MedicalSkills;

        public WeaponSkillData UsecRifleSkill;

        public WeaponSkillData BearRifleSkill;

        public LockPickingData LockPickingSkill;
    }

    public struct MedicalSkillData
    {
        [JsonProperty("MEDKIT_HP_BONUS")]
        public float MedkitHpBonus;

        [JsonProperty("MEDKIT_HP_BONUS_ELITE")]
        public float MedkitHpBonusElite;

        [JsonProperty("MEDICAL_SPEED_BONUS")]
        public float MedicalSpeedBonus;

        [JsonProperty("MEDICAL_SPEED_BONUS_ELITE")]
        public float MedicalSpeedBonusElite;
    }

    public struct WeaponSkillData
    {
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
    }

    public struct LockPickingData
    {
        [JsonProperty("BASE_PICK_TIME")]
        public float BasePickTime;

        [JsonProperty("PICK_TIME_REDUCTION")]
        public float PickTimeReduction;

        [JsonProperty("PICK_TIME_REDUCTION_ELITE")]
        public float PickTimeReductionElite;

        [JsonProperty("BASE_PICK_CHANCE")]
        public float BasePickChance;

        [JsonProperty("BONUS_CHANCE_PER_LEVEL")]
        public float BonusChancePerLevel;

        [JsonProperty("BONUS_CHANCE_PER_LEVEL_ELITE")]
        public float BonusChancePerLevelElite;
    }
}