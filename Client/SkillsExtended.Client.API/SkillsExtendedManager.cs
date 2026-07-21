using EFT;
using EFT.HealthSystem;
using SkillsExtended.Config;
using SkillsExtended.Extensions;

namespace SkillsExtended;

public class SkillsExtendedManager(SkillManager skillManager, SkillsConfig skillData)
{
    #region BUFFS

    public readonly SkillManager.FloatBuff FirstAidItemSpeedBuff = new()
    {
        Id = EBuffId.FirstAidHealingSpeed,
    };

    public readonly SkillManager.LossBuff FirstAidResourceCostBuff = new()
    {
        Id = EBuffId.FirstAidResourceCost,
    };

    public readonly SkillManager.BooleanBuff FirstAidMovementSpeedBuffElite = new()
    {
        Id = EBuffId.FirstAidMovementSpeedElite,
        BuffType = SkillManager.EBuffType.Elite,
    };

    public readonly SkillManager.FloatBuff FieldMedicineSkillCap = new()
    {
        Id = EBuffId.FieldMedicineSkillCap,
    };

    public readonly SkillManager.FloatBuff FieldMedicineDurationBonus = new()
    {
        Id = EBuffId.FieldMedicineDurationBonus,
    };

    public readonly SkillManager.FloatBuff FieldMedicineChanceBonus = new()
    {
        Id = EBuffId.FieldMedicineChanceBonus,
    };

    public readonly SkillManager.FloatBuff UsecArSystemsErgoBuff = new()
    {
        Id = EBuffId.UsecArSystemsErgo,
    };

    public readonly SkillManager.FloatBuff UsecArSystemsRecoilBuff = new()
    {
        Id = EBuffId.UsecArSystemsRecoil,
    };

    public readonly SkillManager.FloatBuff BearAkSystemsErgoBuff = new()
    {
        Id = EBuffId.BearAkSystemsErgo,
    };

    public readonly SkillManager.FloatBuff BearAkSystemsRecoilBuff = new()
    {
        Id = EBuffId.BearAkSystemsRecoil,
    };

    public readonly SkillManager.FloatBuff LockPickingTimeBuff = new()
    {
        Id = EBuffId.LockpickingTimeIncrease,
    };

    public readonly SkillManager.FloatBuff LockPickingForgiveness = new()
    {
        Id = EBuffId.LockpickingForgivenessAngle,
    };

    public readonly SkillManager.BooleanBuff LockPickingUseBuffElite = new()
    {
        Id = EBuffId.LockpickingUseElite,
        BuffType = SkillManager.EBuffType.Elite,
    };

    public readonly SkillManager.FloatBuff SilentOpsIncMeleeSpeedBuff = new()
    {
        Id = EBuffId.SilentOpsIncMeleeSpeed,
    };

    public readonly SkillManager.FloatBuff SilentOpsReduceVolumeBuff = new()
    {
        Id = EBuffId.SilentOpsRedVolume,
    };

    public readonly SkillManager.FloatBuff SilentOpsSilencerCostRedBuff = new()
    {
        Id = EBuffId.SilentOpsSilencerCostRed,
    };

    public readonly SkillManager.FloatBuff StrengthBushSpeedIncBuff = new()
    {
        Id = EBuffId.StrengthColliderSpeedBuff,
    };

    public readonly SkillManager.BooleanBuff StrengthBushSpeedIncBuffElite = new()
    {
        Id = EBuffId.StrengthColliderSpeedBuffElite,
        BuffType = SkillManager.EBuffType.Elite,
    };

    public readonly SkillManager.FloatBuff ScavCooldownTimeReductionBuff = new()
    {
        Id = EBuffId.ShadowConnectionsScavCooldownTimeDec,
    };

    public readonly SkillManager.FloatBuff CultistCircleReturnTimeReductionBuff = new()
    {
        Id = EBuffId.ShadowConnectionsCultistCircleReturnTimeDec,
    };

    public readonly SkillManager.BooleanBuff ScavCooldownTimeReductionEliteBuff = new()
    {
        Id = EBuffId.ShadowConnectionsScavCooldownTimeElite,
        BuffType = SkillManager.EBuffType.Elite,
    };

    public readonly SkillManager.FloatBuff ScavGenerateAsCultistChance = new()
    {
        Id = EBuffId.ScavGenerateAsCultistChance,
    };

    public readonly SkillManager.FloatBuff BearRawPowerPraporTraderCostDec = new()
    {
        Id = EBuffId.BearRawPowerPraporTraderCostDec,
    };

    public readonly SkillManager.FloatBuff BearRawPowerQuestRewardExpInc = new()
    {
        Id = EBuffId.BearRawPowerQuestRewardExpInc,
    };

    public readonly SkillManager.FloatBuff BearRawPowerAllTraderCostDec = new()
    {
        Id = EBuffId.BearRawPowerAllTraderCostDec,
        BuffType = SkillManager.EBuffType.Elite,
    };

    public readonly SkillManager.FloatBuff UsecNegotiationsPeacekeeperTraderCostDec = new()
    {
        Id = EBuffId.UsecNegotiationsPeacekeeperTraderCostDec,
    };

    public readonly SkillManager.FloatBuff UsecNegotiationRewardMoneyInc = new()
    {
        Id = EBuffId.UsecNegotiationRewardMoneyInc,
    };

    public readonly SkillManager.FloatBuff UsecNegotiationsAllTraderCostDec = new()
    {
        Id = EBuffId.UsecNegotiationsAllTraderCostDec,
        BuffType = SkillManager.EBuffType.Elite,
    };

    #endregion

    #region ACTIONS

    public readonly SkillManager.SkillAction FirstAidAction = new();
    public readonly SkillManager.SkillAction FieldMedicineAction = new();
    public readonly SkillManager.SkillAction UsecRifleAction = new();
    public readonly SkillManager.SkillAction BearRifleAction = new();
    public readonly SkillManager.SkillAction LockPickAction = new();
    public readonly SkillManager.SkillAction SilentOpsGunAction = new();
    public readonly SkillManager.SkillAction SilentOpsMeleeAction = new();
    public readonly SkillManager.SkillAction ShadowConnectionsKillAction = new();
    public readonly SkillManager.SkillAction BearRawPowerKillAction = new();
    public readonly SkillManager.SkillAction UsecNegotiationsKillAction = new();

    #endregion

    public SkillManager.Buff[] FirstAidBuffs()
    {
        return
        [
            FirstAidItemSpeedBuff.PerLevel(
                skillData.FirstAid.ItemSpeedBonus.NormalizeToPercentage()
            ),
            FirstAidResourceCostBuff.PerLevel(
                skillData.FirstAid.MedkitUsageReduction.NormalizeToPercentage()
            ),
            FirstAidMovementSpeedBuffElite,
        ];
    }

    public SkillManager.Buff[] FieldMedicineBuffs()
    {
        return
        [
            FieldMedicineSkillCap.PerLevel(
                skillData.FieldMedicine.SkillBonus.NormalizeToPercentage()
            ),
            FieldMedicineDurationBonus.PerLevel(
                skillData.FieldMedicine.DurationBonus.NormalizeToPercentage()
            ),
            FieldMedicineChanceBonus.PerLevel(
                skillData.FieldMedicine.PositiveEffectChanceBonus.NormalizeToPercentage()
            ),
        ];
    }

    public SkillManager.Buff[] UsecArBuffs()
    {
        return
        [
            UsecArSystemsErgoBuff.PerLevel(skillData.NatoWeapons.ErgoMod.NormalizeToPercentage()),
            UsecArSystemsRecoilBuff.PerLevel(
                skillData.NatoWeapons.RecoilReduction.NormalizeToPercentage()
            ),
        ];
    }

    public SkillManager.Buff[] BearAkBuffs()
    {
        return
        [
            BearAkSystemsErgoBuff.PerLevel(
                skillData.EasternWeapons.ErgoMod.NormalizeToPercentage()
            ),
            BearAkSystemsRecoilBuff.PerLevel(
                skillData.EasternWeapons.RecoilReduction.NormalizeToPercentage()
            ),
        ];
    }

    public SkillManager.Buff[] LockPickingBuffs()
    {
        return
        [
            LockPickingTimeBuff.PerLevel(
                skillData.LockPicking.PickStrengthPerLevel.NormalizeToPercentage()
            ),
            LockPickingForgiveness.PerLevel(
                skillData.LockPicking.SweetSpotRangePerLevel.NormalizeToPercentage()
            ),
            LockPickingUseBuffElite,
        ];
    }

    public SkillManager.Buff[] ProneMovementBuffs()
    {
        return
        [
            skillManager.ProneMovementSpeed.PerLevel(
                skillData.ProneMovement.MovementSpeedInc.NormalizeToPercentage()
            ),
            skillManager.ProneMovementVolume.PerLevel(
                skillData.ProneMovement.MovementVolumeDec.NormalizeToPercentage()
            ),
        ];
    }

    public SkillManager.Buff[] SilentOpsBuffs()
    {
        return
        [
            SilentOpsIncMeleeSpeedBuff.PerLevel(
                skillData.SilentOps.MeleeSpeedInc.NormalizeToPercentage()
            ),
            SilentOpsReduceVolumeBuff.PerLevel(
                skillData.SilentOps.VolumeReduction.NormalizeToPercentage()
            ),
            SilentOpsSilencerCostRedBuff.PerLevel(
                skillData.SilentOps.SilencerPriceReduction.NormalizeToPercentage()
            ),
        ];
    }

    public SkillManager.Buff[] ShadowConnectionsBuffs()
    {
        return
        [
            ScavCooldownTimeReductionBuff.PerLevel(
                skillData.ShadowConnections.ScavCooldownTimeDec.NormalizeToPercentage()
            ),
            CultistCircleReturnTimeReductionBuff.PerLevel(
                skillData.ShadowConnections.CultistCircleReturnTimeReduction.NormalizeToPercentage()
            ),
            ScavGenerateAsCultistChance.PerLevel(
                skillData.ShadowConnections.ScavGenerateAsCultistChance.NormalizeToPercentage()
            ),
            ScavCooldownTimeReductionEliteBuff,
        ];
    }

    public SkillManager.Buff[] BearRawPowerBuffs()
    {
        return
        [
            BearRawPowerPraporTraderCostDec.PerLevel(
                skillData.BearRawPower.PraporTradingCostDec.NormalizeToPercentage()
            ),
            BearRawPowerQuestRewardExpInc.PerLevel(
                skillData.BearRawPower.QuestExpRewardInc.NormalizeToPercentage()
            ),
            BearRawPowerAllTraderCostDec.Elite(
                skillData.BearRawPower.AllTraderCostDecrease.NormalizeToPercentage()
            ),
        ];
    }

    public SkillManager.Buff[] UsecNegotiationsBuffs()
    {
        return
        [
            UsecNegotiationsPeacekeeperTraderCostDec.PerLevel(
                skillData.UsecNegotiations.PeacekeeperTradingCostDec.NormalizeToPercentage()
            ),
            UsecNegotiationRewardMoneyInc.PerLevel(
                skillData.UsecNegotiations.QuestMoneyRewardInc.NormalizeToPercentage()
            ),
            UsecNegotiationsAllTraderCostDec.Elite(
                skillData.UsecNegotiations.AllTraderCostDecrease.NormalizeToPercentage()
            ),
        ];
    }

    public void AdjustStimulatorBuff(
        EffectsSettings.StimulatorSettings.StimulatorBuffSettings injectorBuff
    )
    {
        injectorBuff.Duration *= 1f + FieldMedicineDurationBonus;
        if (!injectorBuff.Chance.ApproxEquals(1f))
        {
            injectorBuff.Chance *= 1f + FieldMedicineChanceBonus;
        }
    }
}
