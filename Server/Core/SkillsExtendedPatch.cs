using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;

namespace SkillsExtended.Core;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class SkillsExtendedPatch(ISptLogger<SkillsExtendedPatch> logger) : IOnLoad
{
    public Task OnLoad()
    {
        var manager = new PatchManager
        {
            PatcherName = "SkillsExtended",
            AutoPatch = true
        };
        manager.EnablePatches();
        
        return Task.CompletedTask;
    }
}