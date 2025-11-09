using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Utils;

namespace SkillsExtended.Core;

[Injectable(TypePriority = OnLoadOrder.SaveCallbacks + 1)]
public class SkillLevelAdjuster(
    ISptLogger<SkillLevelAdjuster> logger,
    ProfileHelper profileHelper
    ) : IOnLoad
{
    public Task OnLoad()
    {
        return Task.CompletedTask;
    }
    
    public Dictionary<MongoId, SptProfile> GetAllProfiles()
    {
        return profileHelper.GetProfiles();
    }

    public IEnumerable<CommonSkill> GetPmcSkillsForProfile(SptProfile profile)
    {
        return profile.CharacterData?.PmcData?.Skills?.Common ?? [];
    }
    
    public IEnumerable<CommonSkill> GetScavSkillsForProfile(SptProfile profile)
    {
        return profile.CharacterData?.ScavData?.Skills?.Common ?? [];
    }
}