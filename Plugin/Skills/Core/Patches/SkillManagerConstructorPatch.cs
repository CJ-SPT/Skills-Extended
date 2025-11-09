using System;
using System.Linq;
using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Helpers;
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

    [PatchPrefix]
    public static void Prefix(SkillManager __instance)
    {
        __instance.SkillManagerExtended = new SkillManagerExt(__instance);
    }
    
    [PatchPostfix]
    public static void Postfix(SkillManager __instance, ref SkillClass[] ___DisplayList, ref SkillClass[] ___Skills)
    {
        InitializeNewSkills(__instance, ref ___Skills);
        ModifyDisplayList(__instance, ref ___DisplayList);
        LockSkills(__instance);
	}

    /// <summary>
    ///     Initializes new skills
    /// </summary>
    /// <param name="skillManager">skill manager</param>
    /// <param name="skills">skills</param>
    private static void InitializeNewSkills(SkillManager skillManager, ref SkillClass[] skills)
    {
        skillManager.UsecArsystems = new SkillClass(
            skillManager, 
            ESkillId.UsecArsystems, 
            ESkillClass.Combat, 
            [], 
            []);
        
        skillManager.BearAksystems = new SkillClass(
            skillManager, 
            ESkillId.BearAksystems, 
            ESkillClass.Combat, 
            [], 
            []);

        skillManager.UsecNegotiations = new SkillClass(
            skillManager,
            ESkillId.UsecNegotiations,
            ESkillClass.Special,
            [],
            []);
        
        skillManager.BearRawpower = new SkillClass(
            skillManager,
            ESkillId.BearRawpower,
            ESkillClass.Special,
            [],
            []);
        
        Array.Resize(ref skills, skills.Length + 7);

        skills[^1] = skillManager.UsecArsystems;
        skills[^2] = skillManager.BearAksystems;
        skills[^3] = skillManager.Lockpicking;
        skills[^4] = skillManager.ProneMovement;
        skills[^5] = skillManager.SilentOps;
        skills[^6] = skillManager.UsecNegotiations;
        skills[^7] = skillManager.BearRawpower;
        
    }

    /// <summary>
    ///     Modifies the display list so we can add new skills
    /// </summary>
    /// <param name="skillManager">skill manager</param>
    /// <param name="displayList">display list</param>
    private static void ModifyDisplayList(SkillManager skillManager, ref SkillClass[] displayList)
    {
        const int insertIndex = 12;
        
        var newDisplayList = new SkillClass[displayList.Length + 7];

        Array.Copy(displayList, newDisplayList, insertIndex);

        newDisplayList[12] = skillManager.UsecArsystems;
        newDisplayList[12 + 1] = skillManager.BearAksystems;
        newDisplayList[12 + 2] = skillManager.Lockpicking;
        newDisplayList[12 + 3] = skillManager.ProneMovement;
        newDisplayList[12 + 4] = skillManager.SilentOps;
        newDisplayList[12 + 5] = skillManager.UsecNegotiations;
        newDisplayList[12 + 6] = skillManager.BearRawpower;
        
        Array.Copy(
            displayList, insertIndex, 
            newDisplayList, 
            insertIndex + 7, 
            displayList.Length - insertIndex
            );

        displayList = newDisplayList;
    }

    /// <summary>
    ///     Locks skills if they are not enabled
    /// </summary>
    /// <param name="skillManager">skill manager</param>
    private static void LockSkills(SkillManager skillManager)
    {
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(skillManager.UsecArsystems,
            !Plugin.SkillData.NatoWeapons.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(skillManager.BearAksystems,
            !Plugin.SkillData.EasternWeapons.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(skillManager.Lockpicking,
            !Plugin.SkillData.LockPicking.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(skillManager.FieldMedicine,
            !Plugin.SkillData.FieldMedicine.Enabled);

        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(skillManager.FirstAid,
            !Plugin.SkillData.FirstAid.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(skillManager.ProneMovement,
            !Plugin.SkillData.ProneMovement.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(skillManager.SilentOps,
            !Plugin.SkillData.SilentOps.Enabled);
        
        AccessTools.Field(typeof(SkillClass), "Locked").SetValue(skillManager.Shadowconnections,
            !Plugin.SkillData.ShadowConnections.Enabled);
    }
}