using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Core.Patches;

public class MasteringOnTriggerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() => AccessTools.Method(typeof(BaseSkill), nameof(BaseSkill.OnTrigger));

    [PatchPrefix]
    public static void Prefix(BaseSkill __instance, ref float val)
    {
        // Skill.OnTrigger also calls this base method; only mastery is scaled here.
        if (__instance is Mastering)
            val *= SkillsExtendedPlugin.SkillData?.LevelingSpeed?.WeaponMasteryMultiplier ?? 1;
    }
}
