using System.Reflection;
using EFT;
using HarmonyLib;
using SkillsExtended.Skills.SkillClasses.Physical;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Core.Patches;

public class CreatePhysicalSkillsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SkillManager), nameof(SkillManager.method_3));
    }

    [PatchPrefix]
    public static bool Prefix(SkillManager __instance)
    {
        __instance.Endurance = new EnduranceSkill(__instance);
        __instance.Strength = new StrengthSkill(__instance);
        __instance.Vitality = new VitalitySkill(__instance);
        __instance.Health = new HealthSkill(__instance);
        __instance.Metabolism = new MetabolismSkill(__instance);
        __instance.StressResistance = new StressResistanceSkill(__instance);
        __instance.Immunity = new ImmunitySkill(__instance);
        
        return false;
    }
}