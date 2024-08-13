using EFT;
using SkillsExtended.Models;

namespace SkillsExtended.Skills;

public class SkillManagerExt
{
    private static SkillDataResponse SkillData => Plugin.SkillData;
    
    public readonly SkillManager.SkillBuffClass FirstAidItemSpeedBuff = new()
    {
        Id = EBuffId.FirstAidHealingSpeed,
    };
    
    public readonly SkillManager.SkillBuffClass FirstAidResourceCostBuff = new()
    {
        Id = EBuffId.FirstAidResourceCost,
    };
    
    public readonly SkillManager.GClass1790 FirstAidMovementSpeedBuffElite = new()
    {
        Id = EBuffId.FirstAidMovementSpeedElite,
        BuffType = SkillManager.EBuffType.Elite
    };
    
    public readonly SkillManager.SkillBuffClass FieldMedicineSpeedBuff = new()
    {
        Id = EBuffId.FieldMedicineSpeed,
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
        Id = EBuffId.LockpickingTimeReduction,
    };
    
    public readonly SkillManager.GClass1790 LockPickingUseBuffElite = new()
    {
        Id = EBuffId.LockpickingUseElite,
        BuffType = SkillManager.EBuffType.Elite
    };

    public readonly SkillManager.SkillActionClass FirstAidAction = new();
    public readonly SkillManager.SkillActionClass FieldMedicineAction = new();
    public readonly SkillManager.SkillActionClass UsecRifleAction = new();
    public readonly SkillManager.SkillActionClass BearRifleAction = new();
    public readonly SkillManager.SkillActionClass LockPickAction = new();
    
    public SkillManager.SkillBuffAbstractClass[] FirstAidBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            FirstAidItemSpeedBuff
                .Max(SkillData.MedicalSkills.ItemSpeedBonus)
                .Elite(SkillData.MedicalSkills.ItemSpeedBonusElite),

            FirstAidResourceCostBuff
                .Max(SkillData.MedicalSkills.MedkitUsageReduction)
                .Elite(SkillData.MedicalSkills.MedkitUsageReductionElite),
            
            FirstAidMovementSpeedBuffElite
        };
    }
    
    public SkillManager.SkillBuffAbstractClass[] FieldMedicineBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            FieldMedicineSpeedBuff
                .Max(SkillData.MedicalSkills.ItemSpeedBonus)
                .Elite(SkillData.MedicalSkills.ItemSpeedBonusElite),
        };
    }
    
    public SkillManager.SkillBuffAbstractClass[] UsecArBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            UsecArSystemsErgoBuff
                .Max(SkillData.NatoRifleSkill.ErgoMod)
                .Elite(SkillData.NatoRifleSkill.ErgoModElite),
            
            UsecArSystemsRecoilBuff
                .Max(SkillData.NatoRifleSkill.RecoilReduction)
                .Elite(SkillData.NatoRifleSkill.RecoilReductionElite),
        };
    }
    
    public SkillManager.SkillBuffAbstractClass[] BearAkBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            BearAkSystemsErgoBuff
                .Max(SkillData.EasternRifleSkill.ErgoMod)
                .Elite(SkillData.EasternRifleSkill.ErgoModElite),
            
            BearAkSystemsRecoilBuff
                .Max(SkillData.EasternRifleSkill.RecoilReduction)
                .Elite(SkillData.EasternRifleSkill.RecoilReductionElite),
        };
    }
    
    public SkillManager.SkillBuffAbstractClass[] LockPickingBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            LockPickingTimeBuff
                .Max(SkillData.LockPickingSkill.TimeReduction)
                .Elite(SkillData.LockPickingSkill.TimeReductionElite),
            
            LockPickingUseBuffElite
        };
    }
}