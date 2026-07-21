using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Helpers.Commerce;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace SkillsExtended.Patches;

[Injectable]
public class QuestExperienceRewardPatch(
    ConfigController configController,
    SkillUtil skillUtil,
    ProfileHelper profileHelper
) : AbstractPatch
{
    private static ConfigController _configController = null!;
    private static SkillUtil _skillUtil = null!;
    private static ProfileHelper _profileHelper = null!;

    protected override MethodBase? GetTargetMethod()
    {
        _configController = configController;
        _skillUtil = skillUtil;
        _profileHelper = profileHelper;

        return AccessTools.Method(typeof(RewardHelper), nameof(RewardHelper.ApplyRewards));
    }

    [PatchPostfix]
    public static void Postfix(IEnumerable<Reward> rewards, SptProfile fullProfile)
    {
        var config = _configController.SkillsConfig.BearRawPower;
        if (!config.Enabled)
        {
            return;
        }

        var pmcProfile = fullProfile.CharacterData?.PmcData;
        if (pmcProfile is null)
        {
            return;
        }

        if (pmcProfile.Info?.Side == "Usec" && config.FactionLocked)
        {
            return;
        }

        foreach (var reward in rewards)
        {
            switch (reward.Type)
            {
                case RewardType.Experience:
                    ApplyXpRewardModifier(fullProfile, reward.Value);
                    break;
            }
        }
    }

    private static void ApplyXpRewardModifier(SptProfile fullProfile, double? baseReward)
    {
        if (baseReward is null)
        {
            return;
        }

        var config = _configController.SkillsConfig.BearRawPower;
        var sessionId = fullProfile.ProfileInfo?.ProfileId;

        if (
            !_skillUtil.TryGetSkillLevel(
                sessionId!.Value,
                SkillTypes.BearRawpower,
                out var skillLevel
            )
        )
        {
            return;
        }

        var bonus = config.QuestExpRewardInc * skillLevel;

#if DEBUG
        Console.WriteLine($"Quest experience base reward: {baseReward}");
        Console.WriteLine($"Quest experience bonus: {bonus}");
#endif
        var additionalReward = (int)(baseReward.Value * bonus);

#if DEBUG
        Console.WriteLine($"Additional quest experience reward: {additionalReward}");
#endif
        _profileHelper.AddExperienceToPmc(sessionId.Value, additionalReward);
    }
}
