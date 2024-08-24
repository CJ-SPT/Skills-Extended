using System;
using System.Linq;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Patches.Skills;

internal class SkillManagerConstructorPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        typeof(SkillManager).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
            null, 
            [typeof(EPlayerSide)], 
            null);

    [PatchPostfix]
    public static void Postfix(SkillManager __instance, ref SkillClass[] ___DisplayList, ref SkillClass[] ___Skills, EPlayerSide faction)
    {
        var insertIndex = 12;
        __instance.Side = faction;
        
        Logger.LogDebug($"Creating SkillManager for side {faction}");
        
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
        
        __instance.ProneMovement = new SkillClass(
            __instance, 
            ESkillId.ProneMovement, 
            ESkillClass.Combat, 
            [], 
            []);
        
        var newDisplayList = new SkillClass[___DisplayList.Length + 4];

        Array.Copy(___DisplayList, newDisplayList, insertIndex);

        newDisplayList[12] = __instance.UsecArsystems;
        newDisplayList[12 + 1] = __instance.BearAksystems;
        newDisplayList[12 + 2] = __instance.Lockpicking;
        newDisplayList[12 + 3] = __instance.ProneMovement;
        
        Array.Copy(___DisplayList, insertIndex, newDisplayList, insertIndex + 4, ___DisplayList.Length - insertIndex);

        ___DisplayList = newDisplayList;

        Array.Resize(ref ___Skills, ___Skills.Length + 4);

        ___Skills[___Skills.Length - 1] = __instance.UsecArsystems;
        ___Skills[___Skills.Length - 2] = __instance.BearAksystems;
        ___Skills[___Skills.Length - 3] = __instance.Lockpicking;
        ___Skills[___Skills.Length - 4] = __instance.ProneMovement;

        // If the skill is not enabled, lock it
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.UsecArsystems,
            !Plugin.SkillData.NatoRifle.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.BearAksystems,
            !Plugin.SkillData.EasternRifle.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.Lockpicking,
            !Plugin.SkillData.LockPicking.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.FieldMedicine,
            !Plugin.SkillData.FieldMedicine.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.FirstAid,
            !Plugin.SkillData.FirstAid.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(__instance.ProneMovement,
            !Plugin.SkillData.ProneMovement.Enabled);
        
        //BonusController is called in SkillClass.OnTrigger and must not be null, otherwise it will trigger System.NullReferenceException.
        __instance.BonusController = new();
	}
}