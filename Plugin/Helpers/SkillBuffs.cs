using EFT;
using SkillsExtended.Models;

namespace SkillsExtended.Helpers;

public static class SkillBuffs
{
    private static SkillDataResponse skillData => Plugin.SkillData;
    
    public static SkillManager.SkillBuffClass FirstAidSpeedBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.FirstAidHealingSpeed,
    };
    
    public static SkillManager.SkillBuffClass FirstAidHpBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.FirstAidMaxHp,
    };
    
    public static SkillManager.SkillBuffClass FieldMedicineSpeedBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.FieldMedicineSpeed,
    };
    
    public static SkillManager.SkillBuffClass UsecArSystemsErgoBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.UsecArSystemsErgo,
    };
    
    public static SkillManager.SkillBuffClass UsecArSystemsRecoilBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.UsecArSystemsRecoil,
    };
    
    public static SkillManager.SkillBuffClass BearAkSystemsErgoBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.BearAkSystemsErgo,
    };
    
    public static SkillManager.SkillBuffClass BearAkSystemsRecoilBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.BearAkSystemsRecoil,
    };
    
    public static SkillManager.SkillBuffClass LockpickingTimeBuff = new SkillManager.SkillBuffClass
    {
        Id = EBuffId.LockpickingTimeReduction,
    };
    
    public static SkillManager.GClass1790 LockpickingUseBuffElite = new SkillManager.GClass1790()
    {
        Id = EBuffId.LockpickingUseElite,
        BuffType = SkillManager.EBuffType.Elite
    };
    
    public static SkillManager.SkillBuffAbstractClass[] FirstAidBuffs()
    {
        return !Plugin.RealismConfig.med_changes
            ? new SkillManager.SkillBuffAbstractClass[]
            {
                FirstAidSpeedBuff
                    .Max(skillData.MedicalSkills.MedicalSpeedBonus)
                    .Elite(skillData.MedicalSkills.MedicalSpeedBonusElite),
                
                FirstAidHpBuff
                    .Max(skillData.MedicalSkills.MedkitHpBonus)
                    .Elite(skillData.MedicalSkills.MedkitHpBonusElite),
            }
            : new SkillManager.SkillBuffAbstractClass[]
            {
                FirstAidSpeedBuff
                    .Max(skillData.MedicalSkills.MedkitHpBonus)
                    .Elite(skillData.MedicalSkills.MedkitHpBonusElite),
            };
    }
    
    public static SkillManager.SkillBuffAbstractClass[] FieldMedicineBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            FieldMedicineSpeedBuff
                .Max(skillData.MedicalSkills.MedicalSpeedBonus)
                .Elite(skillData.MedicalSkills.MedicalSpeedBonusElite),
        };
    }
    
    public static SkillManager.SkillBuffAbstractClass[] UsecArBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            UsecArSystemsErgoBuff
                .Max(skillData.UsecRifleSkill.ErgoMod)
                .Elite(skillData.UsecRifleSkill.ErgoModElite),
            
            UsecArSystemsRecoilBuff
                .Max(skillData.UsecRifleSkill.RecoilReduction)
                .Elite(skillData.UsecRifleSkill.RecoilReductionElite),
        };
    }
    
    public static SkillManager.SkillBuffAbstractClass[] BearAkBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            BearAkSystemsErgoBuff
                .Max(skillData.BearRifleSkill.ErgoMod)
                .Elite(skillData.BearRifleSkill.ErgoModElite),
            
            BearAkSystemsRecoilBuff
                .Max(skillData.BearRifleSkill.RecoilReduction)
                .Elite(skillData.BearRifleSkill.RecoilReductionElite),
        };
    }
    
    public static SkillManager.SkillBuffAbstractClass[] LockPickingBuffs()
    {
        return new SkillManager.SkillBuffAbstractClass[]
        {
            LockpickingTimeBuff
                .Max(skillData.LockPickingSkill.TimeReduction)
                .Elite(skillData.LockPickingSkill.TimeReductionElite),
            
            LockpickingUseBuffElite
        };
    }
}