using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Models;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches;

/// <summary>
/// Handles all the mental skills
/// </summary>
public class SkillManagerMethod1Patch : ModulePatch
{
    private static SkillDataResponse SkillData => Plugin.SkillData;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillManager), nameof(SkillManager.method_4));
    }

    [PatchPostfix]
    public static void Postfix(SkillManager __instance)
    {
        
    }
}

/// <summary>
/// Handles all the physical skills
/// </summary>
public class SkillManagerMethod2Patch : ModulePatch
{
    private static SkillDataResponse SkillData => Plugin.SkillData;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillManager), nameof(SkillManager.method_4));
    }

    [PatchPostfix]
    public static void Postfix(SkillManager __instance)
    {
        
    }
}

/// <summary>
/// Handles all the practical skills
/// </summary>
public class SkillManagerMethod3Patch : ModulePatch
{
    private static SkillDataResponse SkillData => Plugin.SkillData;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillManager), nameof(SkillManager.method_4));
    }

    [PatchPostfix]
    public static void Postfix(SkillManager __instance)
    {
        __instance.FieldMedicine = new SkillClass(
            __instance,
            ESkillId.FieldMedicine,
            ESkillClass.Practical,
            [
                __instance.FieldMedicineAction.Factor(0.35f)
            ],
            [
                __instance.FieldMedicineSpeedBuff
                    .Max(SkillData.MedicalSkills.MedicalSpeedBonus)
                    .Elite(SkillData.MedicalSkills.MedicalSpeedBonusElite),
            ]);
        
        __instance.FirstAid = new SkillClass(
            __instance,
            ESkillId.FirstAid,
            ESkillClass.Practical,
            [
                __instance.FirstAidAction.Factor(0.35f)
            ],
            [
                __instance.FirstAidSpeedBuff
                    .Max(SkillData.MedicalSkills.MedicalSpeedBonus)
                    .Elite(SkillData.MedicalSkills.MedicalSpeedBonusElite),
                
                __instance.FirstAidHpBuff
                    .Max(SkillData.MedicalSkills.MedkitHpBonus)
                    .Elite(SkillData.MedicalSkills.MedkitHpBonusElite),
            ]);
        
        __instance.Lockpicking = new SkillClass(
            __instance,
            ESkillId.Lockpicking,
            ESkillClass.Practical,
            [
                __instance.LockPickAction.Factor(0.25f)
            ],
            [
                __instance.LockPickingTimeBuff
                    .Max(SkillData.LockPickingSkill.TimeReduction)
                    .Elite(SkillData.LockPickingSkill.TimeReductionElite),
                
                __instance.LockPickingUseBuffElite
            ]);
    }
}

/// <summary>
/// Handles all the bear faction specific skills
/// </summary>
public class SkillManagerMethod4Patch : ModulePatch
{
    private static SkillDataResponse SkillData => Plugin.SkillData;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillManager), nameof(SkillManager.method_4));
    }

    [PatchPostfix]
    public static void Postfix(SkillManager __instance)
    {
        __instance.BearAksystems = new SkillClass(
            __instance,
            ESkillId.BearAksystems,
            ESkillClass.Combat,
            [
                __instance.BearRifleAction.Factor(0.5f)
            ],
            [
                __instance.BearAkSystemsErgoBuff
                    .Max(SkillData.EasternRifleSkill.ErgoMod)
                    .Elite(SkillData.EasternRifleSkill.ErgoModElite),
                
                __instance.BearAkSystemsRecoilBuff
                    .Max(SkillData.EasternRifleSkill.RecoilReduction)
                    .Elite(SkillData.EasternRifleSkill.RecoilReductionElite)
            ]);
    }
}

/// <summary>
/// Handles all the Usec faction specific skills
/// </summary>
public class SkillManagerMethod5Patch : ModulePatch
{
    private static SkillDataResponse SkillData => Plugin.SkillData;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillManager), nameof(SkillManager.method_5));
    }

    [PatchPostfix]
    public static void Postfix(SkillManager __instance)
    {
        __instance.UsecArsystems = new SkillClass(
            __instance,
            ESkillId.UsecArsystems,
            ESkillClass.Combat,
            [
                __instance.UsecRifleAction.Factor(0.5f)
            ],
            [
                __instance.UsecArSystemsErgoBuff
                    .Max(SkillData.NatoRifleSkill.ErgoMod)
                    .Elite(SkillData.NatoRifleSkill.ErgoModElite),
                
                __instance.UsecArSystemsRecoilBuff
                    .Max(SkillData.NatoRifleSkill.RecoilReduction)
                    .Elite(SkillData.NatoRifleSkill.RecoilReductionElite),
            ]);
    }
}