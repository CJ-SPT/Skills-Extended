using System;
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
            null
        );

    [PatchPrefix]
    public static void Prefix(SkillManager __instance)
    {
        __instance.SkillsExtendedManager = new SkillsExtendedManager(
            __instance,
            SkillsExtendedPlugin.SkillData
        );
    }

    [PatchPostfix]
    public static void Postfix(
        SkillManager __instance,
        ref Skill[] ___DisplayList,
        ref Skill[] ___Skills
    )
    {
        InitializeNewSkills(__instance, ref ___Skills);
        ModifyDisplayList(__instance, ref ___DisplayList);
        LockSkills(__instance);
        Hacking.HackingSkill.Attach(__instance, ref ___Skills, ref ___DisplayList);
    }

    /// <summary>
    ///     Initializes new skills
    /// </summary>
    /// <param name = "skillManager">skill manager</param>
    /// <param name = "skills">skills</param>
    private static void InitializeNewSkills(SkillManager skillManager, ref Skill[] skills)
    {
        skillManager.UsecArsystems ??= new Skill(
            skillManager,
            ESkillId.UsecArsystems,
            ESkillClass.Combat,
            [],
            []
        );
        skillManager.BearAksystems ??= new Skill(
            skillManager,
            ESkillId.BearAksystems,
            ESkillClass.Combat,
            [],
            []
        );
        skillManager.UsecNegotiations ??= new Skill(
            skillManager,
            ESkillId.UsecNegotiations,
            ESkillClass.Special,
            [],
            []
        );
        skillManager.BearRawpower ??= new Skill(
            skillManager,
            ESkillId.BearRawpower,
            ESkillClass.Special,
            [],
            []
        );
        // SilentOps already belongs to the native list in this EFT version.
        skills = SkillLists.AddMissing(skills,
            skillManager.BearRawpower, skillManager.UsecNegotiations, skillManager.SilentOps,
            skillManager.ProneMovement, skillManager.Lockpicking,
            skillManager.BearAksystems, skillManager.UsecArsystems);
    }

    /// <summary>
    ///     Modifies the display list so we can add new skills
    /// </summary>
    /// <param name = "skillManager">skill manager</param>
    /// <param name = "displayList">display list</param>
    private static void ModifyDisplayList(SkillManager skillManager, ref Skill[] displayList)
    {
        displayList = SkillLists.Insert(displayList, 12,
            skillManager.UsecArsystems, skillManager.BearAksystems, skillManager.Lockpicking,
            skillManager.ProneMovement, skillManager.SilentOps,
            skillManager.UsecNegotiations, skillManager.BearRawpower);
    }

    /// <summary>
    ///     Locks skills if they are not enabled
    /// </summary>
    /// <param name = "skillManager">skill manager</param>
    private static void LockSkills(SkillManager skillManager)
    {
        AccessTools
            .Field(typeof(Skill), "Locked")
            .SetValue(
                skillManager.UsecArsystems,
                !SkillsExtendedPlugin.SkillData.NatoWeapons.Enabled
            );
        AccessTools
            .Field(typeof(Skill), "Locked")
            .SetValue(
                skillManager.BearAksystems,
                !SkillsExtendedPlugin.SkillData.EasternWeapons.Enabled
            );
        AccessTools
            .Field(typeof(Skill), "Locked")
            .SetValue(
                skillManager.Lockpicking,
                !SkillsExtendedPlugin.SkillData.LockPicking.Enabled
            );
        AccessTools
            .Field(typeof(Skill), "Locked")
            .SetValue(
                skillManager.FieldMedicine,
                !SkillsExtendedPlugin.SkillData.FieldMedicine.Enabled
            );
        AccessTools
            .Field(typeof(Skill), "Locked")
            .SetValue(skillManager.FirstAid, !SkillsExtendedPlugin.SkillData.FirstAid.Enabled);
        AccessTools
            .Field(typeof(Skill), "Locked")
            .SetValue(
                skillManager.ProneMovement,
                !SkillsExtendedPlugin.SkillData.ProneMovement.Enabled
            );
        AccessTools
            .Field(typeof(Skill), "Locked")
            .SetValue(skillManager.SilentOps, !SkillsExtendedPlugin.SkillData.SilentOps.Enabled);
        AccessTools
            .Field(typeof(Skill), "Locked")
            .SetValue(
                skillManager.Shadowconnections,
                !SkillsExtendedPlugin.SkillData.ShadowConnections.Enabled
            );
    }
}
