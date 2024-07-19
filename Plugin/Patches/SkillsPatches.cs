using EFT;
using EFT.UI;
using HarmonyLib;
using SkillsExtended.Helpers;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using UnityEngine.UI;

namespace SkillsExtended.Patches;

internal class SkillManagerConstructorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(SkillManager).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [typeof(EPlayerSide)], null);

    [PatchPostfix]
    public static void Postfix(SkillManager __instance, ref SkillClass[] ___DisplayList, ref SkillClass[] ___Skills)
    {
        int insertIndex = 12;
        
        __instance.UsecArsystems = new SkillClass(__instance, ESkillId.UsecArsystems, ESkillClass.Special, [], SkillBuffs.UsecArBuffs());
        __instance.BearAksystems = new SkillClass(__instance, ESkillId.BearAksystems, ESkillClass.Special, [], SkillBuffs.BearAkBuffs());

        __instance.UsecTactics = new SkillClass(__instance, ESkillId.UsecTactics, ESkillClass.Special, [], []);
        __instance.BearRawpower = new SkillClass(__instance, ESkillId.BearRawpower, ESkillClass.Special, [], []);

        var newDisplayList = new SkillClass[___DisplayList.Length + 5];

        Array.Copy(___DisplayList, newDisplayList, insertIndex);

        newDisplayList[12] = __instance.UsecArsystems;
        newDisplayList[12 + 1] = __instance.BearAksystems;

        newDisplayList[12 + 2] = __instance.UsecTactics;
        newDisplayList[12 + 3] = __instance.BearRawpower;
        newDisplayList[12 + 4] = __instance.Lockpicking;

        Array.Copy(___DisplayList, insertIndex, newDisplayList, insertIndex + 5, ___DisplayList.Length - insertIndex);

        ___DisplayList = newDisplayList;

        Array.Resize(ref ___Skills, ___Skills.Length + 5);

        ___Skills[___Skills.Length - 1] = __instance.UsecArsystems;
        ___Skills[___Skills.Length - 2] = __instance.BearAksystems;

        ___Skills[___Skills.Length - 3] = __instance.UsecTactics;
        ___Skills[___Skills.Length - 4] = __instance.BearRawpower;
        ___Skills[___Skills.Length - 5] = __instance.Lockpicking;

        // If the skill is not enabled, lock it
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.UsecArsystems,
            !Plugin.SkillData.UsecRifleSkill.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.BearAksystems,
            !Plugin.SkillData.BearRifleSkill.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.UsecTactics,
            !Plugin.SkillData.UsecTacticsSkill.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.BearRawpower,
            !Plugin.SkillData.BearRawPowerSkill.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.Lockpicking,
            !Plugin.SkillData.LockPickingSkill.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.FieldMedicine,
            !Plugin.SkillData.MedicalSkills.EnableFieldMedicine);
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

    [PatchPrefix]
    public static void Prefix(SkillClass __instance, ESkillId id, ref SkillManager.SkillBuffAbstractClass[] buffs)
    {
        if (id == ESkillId.FirstAid)
        {
            buffs = SkillBuffs.FirstAidBuffs();
        }
        
        if (id == ESkillId.FieldMedicine)
        {
            buffs = SkillBuffs.FieldMedicineBuffs();
        }

        if (id == ESkillId.Lockpicking)
        {
            buffs = SkillBuffs.LockPickingBuffs();
        }
    }
}

internal class SkillPanelDisablePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(SkillPanel).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance);

    [PatchPrefix]
    public static bool Prefix(SkillClass skill)
    {
        var skills = Plugin.Session.Profile.Skills;
        var side = Plugin.Session.Profile.Side;
        
        if (skill.Locked)
        {
            // Skip original method and dont show skill
            return false;
        }

        // Usec AR systems
        if (skill.Id == ESkillId.UsecArsystems && side == EPlayerSide.Bear && !skills.BearAksystems.IsEliteLevel)
        {
            if (Plugin.SkillData.DisableEliteRequirements)
            {
                return true;
            }

            // Skip original method and dont show skill
            return false;
        }

        // Usec Tactics
        if (skill.Id == ESkillId.UsecTactics && side == EPlayerSide.Bear)
        {
            // Skip original method and dont show skill
            return false;
        }

        // Bear AK systems
        if (skill.Id == ESkillId.BearAksystems && side == EPlayerSide.Usec && !skills.UsecArsystems.IsEliteLevel)
        {
            if (Plugin.SkillData.DisableEliteRequirements)
            {
                return true;
            }

            // Skip original method and dont show skill
            return false;
        }

        // Bear Raw Power
        if (skill.Id == ESkillId.BearRawpower && side == EPlayerSide.Usec)
        {
            // Skip original method and dont show skill
            return false;
        }

        // Show the skill
        return true;
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