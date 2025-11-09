using SkillsExtended.Patches;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;

namespace SkillsExtended.Core;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class PatchRegister : IOnLoad
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