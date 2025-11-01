using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;

namespace SkillsExtended.Utils;

[Injectable]
public class SkillUtils(
    ISptLogger<SkillUtils> logger,
    ProfileHelper profileHelper
    )
{
    
    /// <summary>
    ///     Get the skill level for the provided profile and skill
    /// </summary>
    /// <param name="profileId">Profile to check</param>
    /// <param name="skillType">Skill Type to check</param>
    /// <returns>Skill level</returns>
    public bool TryGetSkillLevel(MongoId profileId, SkillTypes skillType, out int skillLevel)
    {
        skillLevel = 0;
        
        var profile = profileHelper.GetPmcProfile(profileId);
        if (profile is null)
        {
            logger.Error($"[Skills Extended] Profile `{profileId.ToString()}` not found when trying to get the players skill level.");
            return false;
        }
        
        var skill = profile.Skills?.Common.FirstOrDefault(s => s.Id == skillType);
        if (skill is null)
        {
            return false;
        }
        
        skillLevel = (int)Math.Clamp(skill.Progress / 100f, 0, 5100);
        return true;
    }

    /// <summary>
    ///     Is the provided skill elite level?
    /// </summary>
    /// <param name="profileId">Pmc profile to check</param>
    /// <param name="skillType">Skill type to check</param>
    /// <returns>True if elite level</returns>
    public bool IsEliteLevel(MongoId profileId, SkillTypes skillType)
    {
        if (TryGetSkillLevel(profileId, skillType, out var skillLevel))
        {
            return skillLevel == 51;
        }
        
        return false;
    }
}