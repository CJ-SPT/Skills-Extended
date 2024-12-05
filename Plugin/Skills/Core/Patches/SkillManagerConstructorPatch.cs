using System;
using System.Linq;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Core.Patches;

internal class SkillManagerConstructorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(SkillManager).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
            null, 
            [], 
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
        
        var newDisplayList = new SkillClass[___DisplayList.Length + 5];

        Array.Copy(___DisplayList, newDisplayList, insertIndex);

        newDisplayList[12] = __instance.UsecArsystems;
        newDisplayList[12 + 1] = __instance.BearAksystems;
        newDisplayList[12 + 2] = __instance.Lockpicking;
        newDisplayList[12 + 3] = __instance.ProneMovement;
        newDisplayList[12 + 4] = __instance.SilentOps;
        
        Array.Copy(___DisplayList, insertIndex, newDisplayList, insertIndex + 5, ___DisplayList.Length - insertIndex);

        ___DisplayList = newDisplayList;

        Array.Resize(ref ___Skills, ___Skills.Length + 5);

        ___Skills[___Skills.Length - 1] = __instance.UsecArsystems;
        ___Skills[___Skills.Length - 2] = __instance.BearAksystems;
        ___Skills[___Skills.Length - 3] = __instance.Lockpicking;
        ___Skills[___Skills.Length - 4] = __instance.ProneMovement;
        ___Skills[___Skills.Length - 5] = __instance.SilentOps;

        // If the skill is not enabled, lock it
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.UsecArsystems,
            !SkillsPlugin.SkillData.NatoRifle.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.BearAksystems,
            !SkillsPlugin.SkillData.EasternRifle.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.Lockpicking,
            !SkillsPlugin.SkillData.LockPicking.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.FieldMedicine,
            !SkillsPlugin.SkillData.FieldMedicine.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.FirstAid,
            !SkillsPlugin.SkillData.FirstAid.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.ProneMovement,
            !SkillsPlugin.SkillData.ProneMovement.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.SilentOps,
            !SkillsPlugin.SkillData.SilentOps.Enabled);
        
        
        // BonusController is called in SkillClass.OnTrigger and must not be null, otherwise it will trigger System.NullReferenceException.
        
        // TODO: Is this still needed?
        // __instance.BonusController = new();
	}

    /// <summary>
    /// Return true if we should disable the skill for the provided side
    /// </summary>
    /// <returns></returns>
    private static bool ShouldDisable(EPlayerSide faction, EPlayerSide skillSide)
    {
        var disable = true;
        
        if (disable)
        {
            return false;
        }
        
        return faction != skillSide;
    }
}