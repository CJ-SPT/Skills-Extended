using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.Skills;

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
    public static void Prefix(SkillManager skillManager, SkillClass __instance, ESkillId id, ref SkillManager.SkillBuffAbstractClass[] buffs, ref SkillManager.SkillActionClass[] actions)
    {
        // This is where we set all of our buffs and actions, done as a constructor patch, so they always exist when we need them

        var skillData = Plugin.SkillData;
        
        var skillMgrExt = skillManager.Side == EPlayerSide.Savage
            ? Plugin.ScavSkillManagerExt
            : Plugin.PlayerSkillManagerExt;
        
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
        
        if (id == ESkillId.ProneMovement)
        {
            buffs = [
                skillManager.ProneMovementSpeed
                    .Max(skillData.ProneMovement.MovementSpeedIncMax)
                    .Elite(skillData.ProneMovement.MovementSpeedIncMaxElite),
                
                skillManager.ProneMovementVolume
                    .Max(skillData.ProneMovement.MovementVolumeDecMax)
                    .Elite(skillData.ProneMovement.MovementVolumeDecMaxElite)
            ];
            actions = 
            [
                skillManager.ProneAction.Factor(0.25f)
            ];
        }
        
        if (id == ESkillId.BearAuthority)
        {
            buffs = skillMgrExt.BearAuthorityBuffs();
            actions = [
            ];
        }
        
        if (id == ESkillId.UsecNegotiations)
        {
            buffs = skillMgrExt.UsecNegotiationsBuffs();
            actions = [
                
            ];
        }
    }
}