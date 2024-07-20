using EFT;
using SkillsExtended.Models;

namespace SkillsExtended.Helpers;

public static class SkillBuffs
{
    private static SkillDataResponse SkillData => Plugin.SkillData;
    
    public static readonly SkillManager.SkillBuffClass FirstAidSpeedBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.FirstAidHealingSpeed,
    };
    
    public static readonly SkillManager.SkillBuffClass FirstAidHpBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.FirstAidMaxHp,
    };
    
    public static readonly SkillManager.SkillBuffClass FieldMedicineSpeedBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.FieldMedicineSpeed,
    };
    
    public static readonly SkillManager.SkillBuffClass UsecArSystemsErgoBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.UsecArSystemsErgo,
    };
    
    public static readonly SkillManager.SkillBuffClass UsecArSystemsRecoilBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.UsecArSystemsRecoil,
    };
    
    public static readonly SkillManager.SkillBuffClass BearAkSystemsErgoBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.BearAkSystemsErgo,
    };
    
    public static readonly SkillManager.SkillBuffClass BearAkSystemsRecoilBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.BearAkSystemsRecoil,
    };
    
    public static readonly SkillManager.SkillBuffClass LockpickingTimeBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.LockpickingTimeReduction,
    };
    
    public static readonly SkillManager.GClass1790 LockpickingUseBuffElite = new SkillManager.GClass1790()
    {
        Id = EBuffId.LockpickingUseElite,
        BuffType = SkillManager.EBuffType.Elite
    };

    public static readonly SkillManager.SkillActionClass FirstAidAction = new();
    public static readonly SkillManager.SkillActionClass FieldMedicineAction = new();
    public static readonly SkillManager.SkillActionClass UsecRifleAction = new();
    public static readonly SkillManager.SkillActionClass BearRifleAction = new();
    public static readonly SkillManager.SkillActionClass LockPickAction = new();
    public static readonly SkillManager.SkillActionClass UsecTacticsAction = new();
    public static readonly SkillManager.SkillActionClass BearRawPowerAction = new();
    
    public static SkillManager.SkillBuffAbstractClass[] FirstAidBuffs()
    {
        return !Plugin.RealismConfig.med_changes
            ? new SkillManager.SkillBuffAbstractClass[]
            {
                FirstAidSpeedBuff
                    .Max(SkillData.MedicalSkills.MedicalSpeedBonus)
                    .Elite(SkillData.MedicalSkills.MedicalSpeedBonusElite),
                
                FirstAidHpBuff
                    .Max(SkillData.MedicalSkills.MedkitHpBonus)
                    .Elite(SkillData.MedicalSkills.MedkitHpBonusElite),
            }
            : new SkillManager.SkillBuffAbstractClass[]
            {
                FirstAidSpeedBuff
                    .Max(SkillData.MedicalSkills.MedkitHpBonus)
                    .Elite(SkillData.MedicalSkills.MedkitHpBonusElite),
            };
    }
    
    public static SkillManager.SkillBuffAbstractClass[] FieldMedicineBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            FieldMedicineSpeedBuff
                .Max(SkillData.MedicalSkills.MedicalSpeedBonus)
                .Elite(SkillData.MedicalSkills.MedicalSpeedBonusElite),
        };
    }
    
    public static SkillManager.SkillBuffAbstractClass[] UsecArBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            UsecArSystemsErgoBuff
                .Max(SkillData.UsecRifleSkill.ErgoMod)
                .Elite(SkillData.UsecRifleSkill.ErgoModElite),
            
            UsecArSystemsRecoilBuff
                .Max(SkillData.UsecRifleSkill.RecoilReduction)
                .Elite(SkillData.UsecRifleSkill.RecoilReductionElite),
        };
    }
    
    public static SkillManager.SkillBuffAbstractClass[] BearAkBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            BearAkSystemsErgoBuff
                .Max(SkillData.BearRifleSkill.ErgoMod)
                .Elite(SkillData.BearRifleSkill.ErgoModElite),
            
            BearAkSystemsRecoilBuff
                .Max(SkillData.BearRifleSkill.RecoilReduction)
                .Elite(SkillData.BearRifleSkill.RecoilReductionElite),
        };
    }
    
    public static SkillManager.SkillBuffAbstractClass[] LockPickingBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            LockpickingTimeBuff
                .Max(SkillData.LockPickingSkill.TimeReduction)
                .Elite(SkillData.LockPickingSkill.TimeReductionElite),
            
            LockpickingUseBuffElite
        };
    }
}