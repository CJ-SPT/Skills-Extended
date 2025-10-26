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
        var skillMgrExt = skillManager.SkillManagerExtended;
        
        switch (id)
        {
            case ESkillId.FirstAid:
                buffs = skillMgrExt.FirstAidBuffs();
                actions = [
                    skillMgrExt.FirstAidAction.Factor(0.35f)
                ];
                break;
            
            case ESkillId.FieldMedicine:
                buffs = skillMgrExt.FieldMedicineBuffs();
                actions = [
                    skillMgrExt.FieldMedicineAction.Factor(0.35f)
                ];
                break;
            
            case ESkillId.UsecArsystems:
                buffs = skillMgrExt.UsecArBuffs();
                actions = [
                    skillMgrExt.UsecRifleAction.Factor(0.5f)
                ];
                break;
            
            case ESkillId.BearAksystems:
                buffs = skillMgrExt.BearAkBuffs();
                actions = [
                    skillMgrExt.BearRifleAction.Factor(0.5f)
                ];
                break;
            
            case ESkillId.Lockpicking:
                buffs = skillMgrExt.LockPickingBuffs();
                actions = [
                    skillMgrExt.LockPickAction.Factor(0.25f)
                ];
                break;
            
            case ESkillId.ProneMovement:
                buffs = [
                    skillManager.ProneMovementSpeed.PerLevel(skillData.ProneMovement.MovementSpeedInc),
                    skillManager.ProneMovementVolume.PerLevel(skillData.ProneMovement.MovementVolumeDec)
                ];
                actions = [
                    skillManager.ProneAction.Factor(0.25f)
                ];
                break;
            
            case ESkillId.SilentOps:
                buffs = skillMgrExt.SilentOpsBuffs();
                actions = [
                    skillMgrExt.SilentOpsMeleeAction.Factor(0.75f),
                    skillMgrExt.SilentOpsGunAction.Factor(0.50f)
                ];
                break;
            
            case ESkillId.Shadowconnections:
                buffs = skillMgrExt.ShadowConnectionsBuffs();
                actions = [
                    skillMgrExt.ShadowConnectionsKillAction.Factor(1.0f)
                ];
                break;
                
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
        var skillMgrExt = skillManager.SkillManagerExtended;
        
        if (id == ESkillId.Strength)
        {
            buffList.Add(skillMgrExt.StrengthBushSpeedIncBuff.PerLevel(skillData.Strength.ColliderSpeedBuff));
            buffList.Add(skillMgrExt.StrengthBushSpeedIncBuffElite);
        }
        
        buffs = buffList.ToArray();
        actions = actionList.ToArray();
    }
}