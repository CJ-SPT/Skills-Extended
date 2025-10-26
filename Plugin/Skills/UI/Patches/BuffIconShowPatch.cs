using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine.UI;

namespace SkillsExtended.Skills.UI.Patches;

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
            
            case EBuffId.FirstAidResourceCost:
                ____icon.sprite = staticIcons.HealEffectSprites.GetValueOrDefault(EHealthFactorType.Health);
                break;
            
            case EBuffId.FirstAidMovementSpeedElite:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.StressBerserk);
                break;
            
            case EBuffId.FieldMedicineSkillCap:
                ____icon.sprite = staticIcons.StimulatorBuffSprites.GetValueOrDefault(EStimulatorBuffType.SkillRate);
                break;
            
            case EBuffId.FieldMedicineDurationBonus:
                ____icon.sprite = staticIcons.StimulatorBuffSprites.GetValueOrDefault(EStimulatorBuffType.StaminaRate);
                break;
            
            case EBuffId.FieldMedicineChanceBonus:
                ____icon.sprite = staticIcons.ItemAttributeSprites.GetValueOrDefault(EItemAttributeId.MoneySum);
                break;
            
            case EBuffId.UsecArSystemsErgo:
            case EBuffId.BearAkSystemsErgo:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.WeaponErgonomicsBuff);
                break;
            
            case EBuffId.UsecArSystemsRecoil:
            case EBuffId.BearAkSystemsRecoil:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.WeaponRecoilBuff);
                break;
            
            case EBuffId.LockpickingTimeIncrease:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.CraftingContinueTimeReduce);
                break;
            
            case EBuffId.LockpickingForgivenessAngle:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.HideoutExtraSlots);
                break;
            
            case EBuffId.LockpickingUseElite:
                ____icon.sprite = staticIcons.ItemAttributeSprites.GetValueOrDefault(EItemAttributeId.KeyUses);
                break;
            
            case EBuffId.SilentOpsIncMeleeSpeed:
                ____icon.sprite = staticIcons.DamageEffectSprites.GetValueOrDefault(EDamageEffectType.Contusion);
                break;
            
            case EBuffId.SilentOpsRedVolume:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.CovertMovementSoundVolume);
                break;
            
            case EBuffId.SilentOpsSilencerCostRed:
                ____icon.sprite = staticIcons.ItemAttributeSprites.GetValueOrDefault(EItemAttributeId.Loudness);
                break;
            
            case EBuffId.StrengthColliderSpeedBuff:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.StrengthBuffSprintSpeedInc);
                break;
            
            case EBuffId.ScavCooldownTimeDec:
            case EBuffId.ScavCooldownTimeElite:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.CharismaFenceRepPenaltyReduction);
                break;
            
            case EBuffId.CultistCircleReturnTimeDec:
                ____icon.sprite = staticIcons.BuffIdSprites.GetValueOrDefault(EBuffId.CovertMovementSpeed);
                break;
        }
        
        __instance.UpdateBuff();
    }
}