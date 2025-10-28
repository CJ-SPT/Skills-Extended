using SkillsExtended.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace SkillsExtended.Core;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class PatchRegister : IOnLoad
{
    public Task OnLoad()
    {
        new CultistProductionPatch().Enable();
        new StartSacrificePatch().Enable();
        new ScavCooldownTimerPatch().Enable();
        new QuestMoneyRewardPatch().Enable();
        new QuestExperienceRewardPatch().Enable();
        
        return Task.CompletedTask;
    }
}