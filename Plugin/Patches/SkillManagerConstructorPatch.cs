using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using SkillsExtended.Models;

namespace SkillsExtended.Patches;

internal class SkillManagerConstructorPatch : ModulePatch
{
    private static SkillDataResponse SkillData => Plugin.SkillData;
    
    protected override MethodBase GetTargetMethod() =>
        typeof(SkillManager).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
            null, 
            [typeof(EPlayerSide)], 
            null);

    [PatchPostfix]
    public static void Postfix(
        SkillManager __instance, 
        ref SkillClass[] ___DisplayList, 
        ref SkillClass[] ___Skills)
    {
        var insertIndex = 12;
        
        // Action and buff lists intentionally empty here, this is for display list purposes only
        
        InstantiateBuffs(__instance);
        
        __instance.UsecArsystems = new SkillClass(
            __instance, 
            ESkillId.UsecArsystems, 
            ESkillClass.Combat, 
            [], 
            []);
        
        __instance.BearAksystems = new SkillClass(
            __instance, 
            ESkillId.BearAksystems, 
            ESkillClass.Combat, 
            [], 
            []);

        __instance.BearRawpower = new SkillClass(
            __instance, 
            ESkillId.BearRawpower, 
            ESkillClass.Special, 
            [], 
            []);
        
        __instance.UsecTactics = new SkillClass(
            __instance, 
            ESkillId.UsecTactics, 
            ESkillClass.Special, 
            [], 
            []);
        
        var newDisplayList = new SkillClass[___DisplayList.Length + 5];

        Array.Copy(___DisplayList, newDisplayList, insertIndex);

        newDisplayList[12] = __instance.UsecArsystems;
        newDisplayList[12 + 1] = __instance.BearAksystems;
        newDisplayList[12 + 2] = __instance.Lockpicking;
        newDisplayList[12 + 3] = __instance.BearRawpower;
        newDisplayList[12 + 4] = __instance.UsecTactics;
        
        Array.Copy(___DisplayList, insertIndex, newDisplayList, insertIndex + 5, ___DisplayList.Length - insertIndex);

        ___DisplayList = newDisplayList;

        Array.Resize(ref ___Skills, ___Skills.Length + 5);

        ___Skills[___Skills.Length - 1] = __instance.UsecArsystems;
        ___Skills[___Skills.Length - 2] = __instance.BearAksystems;
        ___Skills[___Skills.Length - 3] = __instance.Lockpicking;
        ___Skills[___Skills.Length - 4] = __instance.BearRawpower;
        ___Skills[___Skills.Length - 5] = __instance.UsecTactics;

        // If the skill is not enabled, lock it
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.UsecArsystems,
            !Plugin.SkillData.NatoRifleSkill.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.BearAksystems,
            !Plugin.SkillData.EasternRifleSkill.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.Lockpicking,
            !Plugin.SkillData.LockPickingSkill.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.FieldMedicine,
            !Plugin.SkillData.MedicalSkills.EnableFieldMedicine);

        //BonusController is called in SkillClass.OnTrigger and must not be null, otherwise it will trigger System.NullReferenceException.
        __instance.BonusController = new();
	}

    private static void InstantiateBuffs(SkillManager skillManager)
    {
        skillManager.FirstAidSpeedBuff = new()
        {
            Id = EBuffId.FirstAidHealingSpeed
        };
        
        skillManager.FirstAidHpBuff = new()
        {
            Id = EBuffId.FirstAidMaxHp
        };
        
        skillManager.FieldMedicineSpeedBuff = new()
        {
            Id = EBuffId.FieldMedicineSpeed
        };
        
        skillManager.UsecArSystemsErgoBuff = new()
        {
            Id = EBuffId.UsecArSystemsErgo
        };
        
        skillManager.UsecArSystemsRecoilBuff = new()
        {
            Id = EBuffId.UsecArSystemsRecoil
        };
        
        skillManager.BearAkSystemsErgoBuff = new()
        {
            Id = EBuffId.BearAkSystemsErgo
        };
        
        skillManager.BearAkSystemsRecoilBuff = new()
        {
            Id = EBuffId.BearAkSystemsRecoil
        };
        
        skillManager.LockPickingTimeBuff = new()
        {
            Id = EBuffId.LockpickingTimeReduction,
        };
        
        skillManager.LockPickingUseBuffElite = new()
        {
            Id = EBuffId.LockpickingUseElite,
            BuffType = SkillManager.EBuffType.Elite
        };
    }
}