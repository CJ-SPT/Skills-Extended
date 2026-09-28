using System.Reflection;
using HarmonyLib;
using SkillsExtended.Electronics;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace SkillsExtended.Patches;

public static class ElectronicsProfile
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
            if (list.Any(s => (int)s.Id == ElectronicsIds.Skill))
            {
                return;
            }

            list.Add(new CommonSkill { Id = (SkillTypes)ElectronicsIds.Skill, Progress = 0 });
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
    public static void Postfix(PmcData? __result) => ElectronicsProfile.Ensure(__result);
}

[Injectable]
public class ElectronicsFullProfilePatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(ProfileHelper), nameof(ProfileHelper.GetFullProfile));

    [PatchPostfix]
    public static void Postfix(SptProfile? __result) =>
        ElectronicsProfile.Ensure(__result?.CharacterData?.PmcData);
}
