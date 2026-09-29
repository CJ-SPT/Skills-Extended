using System.Reflection;
using HarmonyLib;
using SkillsExtended.Hacking;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace SkillsExtended.Patches;

public static class HackingProfile
{
    public static void Ensure(PmcData? profile)
    {
        if (profile?.Skills?.Common == null)
        {
            return;
        }

        lock (profile.Skills)
        {
            var list = profile.Skills.Common.ToList();
            if (!list.Any(s => (int)s.Id == Signals.SignalsIds.Skill))
                list.Add(
                    new CommonSkill { Id = (SkillTypes)Signals.SignalsIds.Skill, Progress = 0 }
                );
            if (list.Any(s => (int)s.Id == HackingIds.Skill))
            {
                profile.Skills.Common = list;
                return;
            }

            list.Add(new CommonSkill { Id = (SkillTypes)HackingIds.Skill, Progress = 0 });
            profile.Skills.Common = list;
        }
    }
}

[Injectable]
public class ElectronicsPmcProfilePatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(ProfileHelper), nameof(ProfileHelper.GetPmcProfile));

    [PatchPostfix]
    public static void Postfix(PmcData? __result) => HackingProfile.Ensure(__result);
}

[Injectable]
public class ElectronicsFullProfilePatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(ProfileHelper), nameof(ProfileHelper.GetFullProfile));

    [PatchPostfix]
    public static void Postfix(SptProfile? __result) =>
        HackingProfile.Ensure(__result?.CharacterData?.PmcData);
}
