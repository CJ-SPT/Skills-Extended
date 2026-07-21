using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;

namespace SkillsExtended;

[Injectable(InjectionType.Singleton, OnLoadOrder.Preload)]
public class SkillsExtendedServer(
    ISptLogger<SkillsExtendedServer> logger,
    IEnumerable<IRuntimePatch> patches
) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        foreach (var patch in patches)
        {
            patch.Enable();
        }

        return Task.CompletedTask;
    }
}
