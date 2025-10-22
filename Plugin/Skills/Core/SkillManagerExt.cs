using EFT;
using SkillsExtended.Models;

namespace SkillsExtended.Skills.Core;

public class SkillManagerExt
{
    private static SkillDataResponse SkillData => SkillsPlugin.SkillData;
    
    #region BUFFS

    public readonly SkillManager.SkillBuffClass FirstAidItemSpeedBuff = new()
    {
        Id = EBuffId.FirstAidHealingSpeed,
    };
    
    public readonly SkillManager.SkillBuffClass FirstAidResourceCostBuff = new()
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

    #endregion
    
    #region ACTIONS

    public readonly SkillManager.SkillActionClass FirstAidAction = new();
    public readonly SkillManager.SkillActionClass FieldMedicineAction = new();
    public readonly SkillManager.SkillActionClass UsecRifleAction = new();
    public readonly SkillManager.SkillActionClass BearRifleAction = new();
    public readonly SkillManager.SkillActionClass LockPickAction = new();
    public readonly SkillManager.SkillActionClass SilentOpsGunAction = new();
    public readonly SkillManager.SkillActionClass SilentOpsMeleeAction = new();

    #endregion
    
    public SkillManager.SkillBuffAbstractClass[] FirstAidBuffs()
    {
        return
        [
            FirstAidItemSpeedBuff
                .Max(SkillData.FirstAid.ItemSpeedBonus)
                .Elite(SkillData.FirstAid.ItemSpeedBonusElite),

            FirstAidResourceCostBuff
                .Max(SkillData.FirstAid.MedkitUsageReduction)
                .Elite(SkillData.FirstAid.MedkitUsageReductionElite),
            
            FirstAidMovementSpeedBuffElite
        ];
    }
    
    public SkillManager.SkillBuffAbstractClass[] FieldMedicineBuffs()
    {
        return
        [
            FieldMedicineSkillCap
                .Max(SkillData.FieldMedicine.SkillBonus)
                .Elite(SkillData.FieldMedicine.SkillBonusElite),
            
            FieldMedicineDurationBonus
                .Max(SkillData.FieldMedicine.DurationBonus)
                .Elite(SkillData.FieldMedicine.DurationBonusElite),
            
            FieldMedicineChanceBonus
                .Max(SkillData.FieldMedicine.PositiveEffectChanceBonus)
                .Elite(SkillData.FieldMedicine.PositiveEffectChanceBonusElite)
        ];
    }
    
    public SkillManager.SkillBuffAbstractClass[] UsecArBuffs()
    {
        return
        [
            UsecArSystemsErgoBuff
                .Max(SkillData.NatoWeapons.ErgoMod)
                .Elite(SkillData.NatoWeapons.ErgoModElite),
            
            UsecArSystemsRecoilBuff
                .Max(SkillData.NatoWeapons.RecoilReduction)
                .Elite(SkillData.NatoWeapons.RecoilReductionElite)
        ];
    }
    
    public SkillManager.SkillBuffAbstractClass[] BearAkBuffs()
    {
        return
        [
            BearAkSystemsErgoBuff
                .Max(SkillData.EasternWeapons.ErgoMod)
                .Elite(SkillData.EasternWeapons.ErgoModElite),
            
            BearAkSystemsRecoilBuff
                .Max(SkillData.EasternWeapons.RecoilReduction)
                .Elite(SkillData.EasternWeapons.RecoilReductionElite)
        ];
    }
    
    public SkillManager.SkillBuffAbstractClass[] LockPickingBuffs()
    {
        return
        [
            LockPickingTimeBuff
                .PerLevel(SkillData.LockPicking.PickStrengthPerLevel),
            
            LockPickingForgiveness
                .PerLevel(SkillData.LockPicking.SweetSpotRangePerLevel),
            
            LockPickingUseBuffElite
        ];
    }
    
    public SkillManager.SkillBuffAbstractClass[] SilentOpsBuffs()
    {
        return
        [
            SilentOpsIncMeleeSpeedBuff
                .Max(SkillData.SilentOps.MeleeSpeedInc),
            
            SilentOpsReduceVolumeBuff
                .Max(SkillData.SilentOps.VolumeReduction),
            
            SilentOpsSilencerCostRedBuff
                .Max(SkillData.SilentOps.SilencerPriceReduction)
        ];
    }
}