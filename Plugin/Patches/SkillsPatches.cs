using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using Comfort.Common;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using SkillsExtended.Skills;
using UnityEngine.UI;

namespace SkillsExtended.Patches;

internal class SkillManagerConstructorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(SkillManager).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
            null, 
            [typeof(EPlayerSide)], 
            null);

    [PatchPostfix]
    public static void Postfix(SkillManager __instance, ref SkillClass[] ___DisplayList, ref SkillClass[] ___Skills)
    {
        var insertIndex = 12;
        
        // Action and buff lists intentionally empty here, this is for display list purposes only
        
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
}

internal class SkillClassCtorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(SkillClass).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
            null, 
            [typeof(SkillManager), typeof(ESkillId), typeof(ESkillClass), typeof(SkillManager.SkillActionClass[]), typeof(SkillManager.SkillBuffAbstractClass[])], 
            null);
    
    /// <summary>
    /// We are using values that have been added by the pre-patcher here.
    /// </summary>
    [PatchPrefix]
    public static void Prefix(SkillClass __instance, ESkillId id, ref SkillManager.SkillBuffAbstractClass[] buffs, ref SkillManager.SkillActionClass[] actions)
    {
        // This is where we set all of our buffs and actions, done as a constructor patch, so they always exist when we need them
        
        var skillMgrExt = Singleton<SkillManagerExt>.Instance;
        
        if (id == ESkillId.FirstAid)
        {
            buffs = skillMgrExt.FirstAidBuffs();
            actions = [
                skillMgrExt.FirstAidAction.Factor(0.35f)
            ];
        }
        
        if (id == ESkillId.FieldMedicine)
        {
            buffs = skillMgrExt.FieldMedicineBuffs();
            actions = [
                skillMgrExt.FieldMedicineAction.Factor(0.35f)
            ];
        }
        
        if (id == ESkillId.UsecArsystems)
        {
            buffs = skillMgrExt.UsecArBuffs();
            actions =
            [
                skillMgrExt.UsecRifleAction.Factor(0.5f)
            ];
        }
        
        if (id == ESkillId.BearAksystems)
        {
            buffs = skillMgrExt.BearAkBuffs();
            actions =
            [
                skillMgrExt.BearRifleAction.Factor(0.5f)
            ];
        }

        if (id == ESkillId.Lockpicking)
        {
            buffs = skillMgrExt.LockPickingBuffs();
            actions = 
            [
                skillMgrExt.LockPickAction.Factor(0.25f)
            ];
        }
    }
}

internal class SkillPanelDisablePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(SkillPanel), nameof(SkillPanel.Show));

    [PatchPrefix]
    public static bool Prefix(SkillClass skill)
    {
        return !skill.Locked;
    }
}

internal class BuffIconShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BuffIcon), nameof(BuffIcon.Show));
    }

    [PatchPostfix]
    public static void Postfix(
        BuffIcon __instance, 
        SkillManager.SkillBuffAbstractClass buff,
        Image ____icon)
    {
        var staticIcons = EFTHardSettings.Instance.StaticIcons;

        switch (buff.Id)
        {
            case EBuffId.FirstAidHealingSpeed:
                ____icon.sprite = staticIcons.HealEffectSprites.GetValueOrDefault(EHealthFactorType.Energy);
                break;
            
            case EBuffId.FirstAidMaxHp:
                ____icon.sprite = staticIcons.HealEffectSprites.GetValueOrDefault(EHealthFactorType.Health);
                break;
            
            case EBuffId.FieldMedicineSpeed:
                ____icon.sprite = staticIcons.HealEffectSprites.GetValueOrDefault(EHealthFactorType.Energy);
                break;
            
            case EBuffId.UsecArSystemsErgo:
            case EBuffId.BearAkSystemsErgo:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.WeaponErgonomicsBuff);
                break;
            
            case EBuffId.UsecArSystemsRecoil:
            case EBuffId.BearAkSystemsRecoil:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.WeaponRecoilBuff);
                break;
            
            case EBuffId.LockpickingTimeReduction:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.CraftingContinueTimeReduce);
                break;
            
            case EBuffId.LockpickingUseElite:
                ____icon.sprite = staticIcons.ItemAttributeSprites.GetValueOrDefault(EItemAttributeId.KeyUses);
                break;
        }
        
        __instance.UpdateBuff();
    }
}