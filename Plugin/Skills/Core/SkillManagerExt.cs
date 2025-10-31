using EFT;
using SkillsExtended.Helpers;
using SkillsExtended.Models;

namespace SkillsExtended.Skills.Core;

public class SkillManagerExt(SkillManager skillManager)
{
    private static SkillDataResponse SkillData => SkillsPlugin.SkillData;

    #region BUFFS

    public readonly SkillManager.SkillBuffClass FirstAidItemSpeedBuff = new()
    {
        Id = EBuffId.FirstAidHealingSpeed,
    };

    public readonly NegativeSkillBuffInt FirstAidResourceCostBuff = new()
    {
        Id = EBuffId.FirstAidResourceCost,
    };

    public readonly SkillManager.GClass2257 FirstAidMovementSpeedBuffElite = new()
    {
        Id = EBuffId.FirstAidMovementSpeedElite,
        BuffType = SkillManager.EBuffType.Elite
    };

    public readonly SkillManager.SkillBuffClass FieldMedicineSkillCap = new()
    {
        Id = EBuffId.FieldMedicineSkillCap,
    };

    public readonly SkillManager.SkillBuffClass FieldMedicineDurationBonus = new()
    {
        Id = EBuffId.FieldMedicineDurationBonus,
    };

    public readonly SkillManager.SkillBuffClass FieldMedicineChanceBonus = new()
    {
        Id = EBuffId.FieldMedicineChanceBonus,
    };

    public readonly SkillManager.SkillBuffClass UsecArSystemsErgoBuff = new()
    {
        Id = EBuffId.UsecArSystemsErgo,
    };

    public readonly SkillManager.SkillBuffClass UsecArSystemsRecoilBuff = new()
    {
        Id = EBuffId.UsecArSystemsRecoil,
    };

    public readonly SkillManager.SkillBuffClass BearAkSystemsErgoBuff = new()
    {
        Id = EBuffId.BearAkSystemsErgo,
    };

    public readonly SkillManager.SkillBuffClass BearAkSystemsRecoilBuff = new()
    {
        Id = EBuffId.BearAkSystemsRecoil,
    };

    public readonly SkillManager.SkillBuffClass LockPickingTimeBuff = new()
    {
        Id = EBuffId.LockpickingTimeIncrease,
    };

    public readonly SkillManager.SkillBuffClass LockPickingForgiveness = new()
    {
        Id = EBuffId.LockpickingForgivenessAngle,
    };

    public readonly SkillManager.GClass2257 LockPickingUseBuffElite = new()
    {
        Id = EBuffId.LockpickingUseElite,
        BuffType = SkillManager.EBuffType.Elite
    };

    public readonly SkillManager.SkillBuffClass SilentOpsIncMeleeSpeedBuff = new()
    {
        Id = EBuffId.SilentOpsIncMeleeSpeed,
    };

    public readonly SkillManager.SkillBuffClass SilentOpsReduceVolumeBuff = new()
    {
        Id = EBuffId.SilentOpsRedVolume
    };

    public readonly SkillManager.SkillBuffClass SilentOpsSilencerCostRedBuff = new()
    {
        Id = EBuffId.SilentOpsSilencerCostRed
    };

    public readonly SkillManager.SkillBuffClass StrengthBushSpeedIncBuff = new()
    {
        Id = EBuffId.StrengthColliderSpeedBuff
    };

    public readonly SkillManager.GClass2257 StrengthBushSpeedIncBuffElite = new()
    {
        Id = EBuffId.StrengthColliderSpeedBuffElite,
        BuffType = SkillManager.EBuffType.Elite
    };

    public readonly SkillManager.SkillBuffClass ScavCooldownTimeReductionBuff = new()
    {
        Id = EBuffId.ShadowConnectionsScavCooldownTimeDec
    };

    public readonly SkillManager.SkillBuffClass CultistCircleReturnTimeReductionBuff = new()
    {
        Id = EBuffId.ShadowConnectionsCultistCircleReturnTimeDec
    };

    public readonly SkillManager.GClass2257 ScavCooldownTimeReductionEliteBuff = new()
    {
        Id = EBuffId.ShadowConnectionsScavCooldownTimeElite,
        BuffType = SkillManager.EBuffType.Elite
    };
    
    public readonly SkillManager.SkillBuffClass BearRawPowerPraporTraderCostDec = new()
    {
        Id = EBuffId.BearRawPowerPraporTraderCostDec
    };

    public readonly SkillManager.SkillBuffClass BearRawPowerQuestRewardExpInc = new()
    {
        Id = EBuffId.BearRawPowerQuestRewardExpInc
    };

    public readonly SkillManager.SkillBuffClass BearRawPowerAllTraderCostDec = new()
    {
        Id = EBuffId.BearRawPowerAllTraderCostDec,
        BuffType = SkillManager.EBuffType.Elite
    };
    
    public readonly SkillManager.SkillBuffClass UsecNegotiationsPeacekeeperTraderCostDec = new()
    {
        Id = EBuffId.UsecNegotiationsPeacekeeperTraderCostDec
    };

    public readonly SkillManager.SkillBuffClass UsecNegotiationRewardMoneyInc = new()
    {
        Id = EBuffId.UsecNegotiationRewardMoneyInc,
    };

    public readonly SkillManager.SkillBuffClass UsecNegotiationsAllTraderCostDec = new()
    {
        Id = EBuffId.UsecNegotiationsAllTraderCostDec,
        BuffType = SkillManager.EBuffType.Elite
    };
    

    #endregion

    #region ACTIONS

    public readonly SkillManager.SkillActionClass FirstAidAction = new();
    public readonly SkillManager.SkillActionClass FieldMedicineAction = new();
    public readonly SkillManager.SkillActionClass UsecRifleAction = new();
    public readonly SkillManager.SkillActionClass BearRifleAction = new();
    public readonly SkillManager.SkillActionClass LockPickAction = new();
    public readonly SkillManager.SkillActionClass SilentOpsGunAction = new();
    public readonly SkillManager.SkillActionClass SilentOpsMeleeAction = new();
    public readonly SkillManager.SkillActionClass ShadowConnectionsKillAction = new();
    public readonly SkillManager.SkillActionClass BearRawPowerKillAction = new();
    public readonly SkillManager.SkillActionClass UsecNegotiationsKillAction = new();

    #endregion

    public SkillManager.SkillBuffAbstractClass[] FirstAidBuffs()
    {
        return
        [
            FirstAidItemSpeedBuff.PerLevel(SkillData.FirstAid.ItemSpeedBonus.NormalizeToPercentage()),
            FirstAidResourceCostBuff.PerLevel(SkillData.FirstAid.MedkitUsageReduction.NormalizeToPercentage()),
            FirstAidMovementSpeedBuffElite
        ];
    }

    public SkillManager.SkillBuffAbstractClass[] FieldMedicineBuffs()
    {
        return
        [
            FieldMedicineSkillCap.PerLevel(SkillData.FieldMedicine.SkillBonus.NormalizeToPercentage()),
            FieldMedicineDurationBonus.PerLevel(SkillData.FieldMedicine.DurationBonus.NormalizeToPercentage()),
            FieldMedicineChanceBonus.PerLevel(SkillData.FieldMedicine.PositiveEffectChanceBonus.NormalizeToPercentage()),
        ];
    }

    public SkillManager.SkillBuffAbstractClass[] UsecArBuffs()
    {
        return
        [
            UsecArSystemsErgoBuff.PerLevel(SkillData.NatoWeapons.ErgoMod.NormalizeToPercentage()),
            UsecArSystemsRecoilBuff.PerLevel(SkillData.NatoWeapons.RecoilReduction.NormalizeToPercentage())
        ];
    }

    public SkillManager.SkillBuffAbstractClass[] BearAkBuffs()
    {
        return
        [
            BearAkSystemsErgoBuff.PerLevel(SkillData.EasternWeapons.ErgoMod.NormalizeToPercentage()),
            BearAkSystemsRecoilBuff.PerLevel(SkillData.EasternWeapons.RecoilReduction.NormalizeToPercentage())
        ];
    }

    public SkillManager.SkillBuffAbstractClass[] LockPickingBuffs()
    {
        return
        [
            LockPickingTimeBuff.PerLevel(SkillData.LockPicking.PickStrengthPerLevel.NormalizeToPercentage()),
            LockPickingForgiveness.PerLevel(SkillData.LockPicking.SweetSpotRangePerLevel.NormalizeToPercentage()),
            LockPickingUseBuffElite
        ];
    }

    public SkillManager.SkillBuffAbstractClass[] ProneMovementBuffs()
    {
        return
        [
            skillManager.ProneMovementSpeed.PerLevel(SkillData.ProneMovement.MovementSpeedInc.NormalizeToPercentage()),
            skillManager.ProneMovementVolume.PerLevel(SkillData.ProneMovement.MovementVolumeDec.NormalizeToPercentage())
        ];
    }
    
    public SkillManager.SkillBuffAbstractClass[] SilentOpsBuffs()
    {
        return
        [
            SilentOpsIncMeleeSpeedBuff.PerLevel(SkillData.SilentOps.MeleeSpeedInc.NormalizeToPercentage()),
            SilentOpsReduceVolumeBuff.PerLevel(SkillData.SilentOps.VolumeReduction.NormalizeToPercentage()),
            SilentOpsSilencerCostRedBuff.PerLevel(SkillData.SilentOps.SilencerPriceReduction.NormalizeToPercentage())
        ];
    }

    public SkillManager.SkillBuffAbstractClass[] ShadowConnectionsBuffs()
    {
        return
        [
            ScavCooldownTimeReductionBuff.PerLevel(SkillData.ShadowConnections.ScavCooldownTimeReduction.NormalizeToPercentage()),
            CultistCircleReturnTimeReductionBuff.PerLevel(SkillData.ShadowConnections.CultistCircleReturnTimeReduction.NormalizeToPercentage()),
            ScavCooldownTimeReductionEliteBuff
        ];
    }
    
    public SkillManager.SkillBuffAbstractClass[] BearRawPowerBuffs()
    {
        return
        [
            BearRawPowerPraporTraderCostDec.PerLevel(SkillData.BearRawPower.PraporTradingCostDec.NormalizeToPercentage()),
            BearRawPowerQuestRewardExpInc.PerLevel(SkillData.BearRawPower.QuestExpRewardInc.NormalizeToPercentage()),
            BearRawPowerAllTraderCostDec.Elite(SkillData.BearRawPower.AllTraderCostDecrease.NormalizeToPercentage())
        ];
    }
    
    public SkillManager.SkillBuffAbstractClass[] UsecNegotiationsBuffs()
    {
        return
        [
            UsecNegotiationsPeacekeeperTraderCostDec.PerLevel(SkillData.UsecNegotiations.PeacekeeperTradingCostDec.NormalizeToPercentage()),
            UsecNegotiationRewardMoneyInc.PerLevel(SkillData.UsecNegotiations.QuestMoneyRewardInc.NormalizeToPercentage()),
            UsecNegotiationsAllTraderCostDec.Elite(SkillData.UsecNegotiations.AllTraderCostDecrease.NormalizeToPercentage())
        ];
    }

    public void AdjustStimulatorBuff(InjectorBuff injectorBuff)
    {
        injectorBuff.Duration *= 1f + FieldMedicineDurationBonus;
        if (!injectorBuff.Chance.ApproxEquals(1f))
        {
            injectorBuff.Chance *= 1f + FieldMedicineChanceBonus;
        }

#if DEBUG
        SkillsPlugin.Log.LogDebug($"Buff {injectorBuff.BuffName} duration adjusted to {injectorBuff.Duration}");
        SkillsPlugin.Log.LogDebug($"Buff {injectorBuff.BuffName} chance adjusted to {injectorBuff.Chance}");
#endif
    }
}