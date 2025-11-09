using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Utils;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;

namespace SkillsExtended.Patches;

public class QuestExperienceRewardPatch : AbstractPatch
{
    private static readonly ConfigController ConfigController = ServiceLocator.ServiceProvider.GetRequiredService<ConfigController>();
    private static readonly SkillUtil SkillUtil = ServiceLocator.ServiceProvider.GetRequiredService<SkillUtil>();
    private static readonly ProfileHelper ProfileHelper = ServiceLocator.ServiceProvider.GetRequiredService<ProfileHelper>();
    
    protected override MethodBase? GetTargetMethod()
    {
        return AccessTools.Method(typeof(RewardHelper), nameof(RewardHelper.ApplyRewards));
    }

    [PatchPostfix]
    public static void Postfix(IEnumerable<Reward> rewards, SptProfile fullProfile)
    {
        var config = ConfigController.SkillsConfig.BearRawPower;
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
        
        var config = ConfigController.SkillsConfig.BearRawPower;
        var sessionId = fullProfile.ProfileInfo?.ProfileId;

        if (!SkillUtil.TryGetSkillLevel(sessionId!.Value, SkillTypes.BearRawpower, out var skillLevel))
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
        
        ProfileHelper.AddExperienceToPmc(sessionId.Value, additionalReward);
    }
}