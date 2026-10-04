// External skill identifiers and fields used by the source-linked regression tests.
namespace EFT;
public enum EBuffId { BearAkSystemsErgo, BearAkSystemsRecoil, BearRawPowerAllTraderCostDec, BearRawPowerPraporTraderCostDec, BearRawPowerQuestRewardExpInc, FieldMedicineChanceBonus, FieldMedicineDurationBonus, FieldMedicineSkillCap, FirstAidHealingSpeed, FirstAidMovementSpeedElite, FirstAidResourceCost, LockpickingForgivenessAngle, LockpickingTimeIncrease, LockpickingUseElite, ScavGenerateAsCultistChance, ShadowConnectionsCultistCircleReturnTimeDec, ShadowConnectionsScavCooldownTimeDec, ShadowConnectionsScavCooldownTimeElite, SilentOpsIncMeleeSpeed, SilentOpsRedVolume, SilentOpsSilencerCostRed, StrengthColliderSpeedBuff, StrengthColliderSpeedBuffElite, UsecArSystemsErgo, UsecArSystemsRecoil, UsecNegotiationRewardMoneyInc, UsecNegotiationsAllTraderCostDec, UsecNegotiationsPeacekeeperTraderCostDec }
public enum ESkillId { Endurance, Health, Immunity, Metabolism, Strength, StressResistance, Vitality, Lockpicking = 43 }
public partial class SkillManager
{
    public object BonusController = new();
    public SkillAction DamageTakenAction = new();
    public FloatBuff EnduranceBreathElite = new();
    public FloatBuff EnduranceBuffBreathTimeInc = new();
    public FloatBuff EnduranceBuffEnduranceInc = new();
    public FloatBuff EnduranceBuffJumpCostRed = new();
    public FloatBuff EnduranceBuffRestoration = new();
    public FloatBuff EnduranceHands = new();
    public SkillAction<float> EnergyChanged = new();
    public SkillAction FistfightAction = new();
    public FloatBuff HealthBreakChanceRed = new();
    public FloatBuff HealthEliteAbsorbDamage = new();
    public FloatBuff HealthEnergy = new();
    public FloatBuff HealthHydration = new();
    public SkillAction<HealthSystem.IHealthEffect> HealthNegativeEffect = new();
    public SkillAction<float> HydrationChanged = new();
    public FloatBuff ImmunityAvoidMiscEffectsChance = new();
    public FloatBuff ImmunityAvoidPoisonChance = new();
    public FloatBuff ImmunityMiscEffects = new();
    public FloatBuff ImmunityPainKiller = new();
    public FloatBuff ImmunityPoisonBuff = new();
    public SkillAction LowHPDuration = new();
    public FloatBuff MetabolismEliteBuffNoDyhydration = new();
    public FloatBuff MetabolismMiscDebuffTime = new();
    public FloatBuff MetabolismRatioPlus = new();
    public SkillAction MovementAction = new();
    public FloatBuff ProneMovementSpeed = new();
    public FloatBuff ProneMovementVolume = new();
    public SkillAction PushUp = new();
    public SkillAction<Skill> SkillProgress = new();
    public SkillAction SprintAction = new();
    public SkillAction StimulatorNegativeBuff = new();
    public FloatBuff StrengthBuffAimFatigue = new();
    public FloatBuff StrengthBuffElite = new();
    public FloatBuff StrengthBuffJumpHeightInc = new();
    public FloatBuff StrengthBuffLiftWeightInc = new();
    public FloatBuff StrengthBuffMeleeCrits = new();
    public FloatBuff StrengthBuffMeleePowerInc = new();
    public FloatBuff StrengthBuffSprintSpeedInc = new();
    public FloatBuff StrengthBuffThrowDistanceInc = new();
    public FloatBuff StressBerserk = new();
    public FloatBuff StressPain = new();
    public FloatBuff StressTremor = new();
    public SkillAction ThrowAction = new();
    public FloatBuff VitalityBuffBleedChanceRed = new();
    public FloatBuff VitalityBuffBleedStop = new();
    public FloatBuff VitalityBuffRegeneration = new();
    public FloatBuff VitalityBuffSurviobilityInc = new();
    public SkillAction ProneAction = new();
}
