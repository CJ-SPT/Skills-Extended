using SkillsExtended.Config;
using SkillsExtended.Core.Editing;
using SkillsExtended.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace SkillsExtended.Core;

[Injectable(InjectionType.Singleton, OnLoadOrder.Preload)]
public class ConfigController(ISptLogger<ConfigController> logger, IReadOnlyList<SptMod> loadedMods)
    : IOnLoad
{
    private readonly ConfigStore _store = new(
        Path.Combine(ModMetadata.ResourcesDirectory, "Configs"),
        new ConfigFiles()
    );
    private ConfigSnapshot _runtime = null!;
    public ServerConfig ServerConfig => _runtime.Server;
    public SkillsConfig SkillsConfig => _runtime.Skills;
    public bool IsFikaPresent { get; private set; }

    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        _runtime = await _store.ReadSnapshotAsync();
        IsFikaPresent = loadedMods.Any(m => m.ModMetadata.ModGuid == "Fika");
    }

    public Task<ConfigSnapshot> GetSnapshotAsync() => _store.ReadSnapshotAsync();

    public async Task<EditResult> SaveAsync(ConfigSnapshot draft)
    {
        var result = await _store.SaveAsync(
            draft.Skills,
            draft.Server,
            draft.Revision,
            snapshot => _runtime = snapshot
        );
        if (!result.Success)
            logger.Warning($"[Skills Extended] {result.Message}");
        return result;
    }
}
