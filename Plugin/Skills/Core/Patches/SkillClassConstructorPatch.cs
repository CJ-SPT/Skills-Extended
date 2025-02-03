using System.Linq;
using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Core.Patches;

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
    public static void Prefix(
        SkillManager skillManager,
        ESkillId id, 
        ref SkillManager.SkillBuffAbstractClass[] buffs, 
        ref SkillManager.SkillActionClass[] actions)
    {
        // This is where we set all of our buffs and actions,
        // done as a constructor patch, so they always exist when we need them
        InitializeNewSkills(skillManager, id, ref buffs, ref actions);
        ModifyExistingSkills(skillManager, id, ref buffs, ref actions);
    }

    private static void InitializeNewSkills(
        SkillManager skillManager, 
        ESkillId id, 
        ref SkillManager.SkillBuffAbstractClass[] buffs, 
        ref SkillManager.SkillActionClass[] actions)
    {
        var skillData = SkillsPlugin.SkillData;
        var skillMgrExt = SkillManagerExt.Instance(EPlayerSide.Usec);
        
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
        
        if (id == ESkillId.SilentOps)
        {
            buffs = skillMgrExt.SilentOpsBuffs();
            actions = [
                skillMgrExt.SilentOpsMeleeAction.Factor(0.75f),
                skillMgrExt.SilentOpsGunAction.Factor(0.50f)
            ];
        }
    }

    private static void ModifyExistingSkills(
        SkillManager skillManager, 
        ESkillId id, 
        ref SkillManager.SkillBuffAbstractClass[] buffs, 
        ref SkillManager.SkillActionClass[] actions)
    {
        var buffList = buffs.ToList();
        var actionList = actions.ToList();

        var skillData = SkillsPlugin.SkillData;
        var skillMgrExt = SkillManagerExt.Instance(EPlayerSide.Usec);
        
        if (id == ESkillId.Strength)
        {
            buffList.Add(
                skillMgrExt.StrengthBushSpeedIncBuff
                    .Max(skillData.Strength.ColliderSpeedBuff)
                );
            
            buffList.Add(
                skillMgrExt.StrengthBushSpeedIncBuffElite
            );
        }
        
        buffs = buffList.ToArray();
        actions = actionList.ToArray();
    }
}