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
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    public ServerConfig ServerConfig => _runtime.Server;
    public SkillsConfig SkillsConfig => _runtime.Skills;
    public bool IsFikaPresent { get; private set; }
    public event Action<SkillsConfig>? Saved;
    public IReadOnlyDictionary<string, float> NativeBonusDefaults { get; set; } = NativeSkillCatalog.Defaults;

    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        await EnsureLoadedAsync();
        IsFikaPresent = loadedMods.Any(m => m.ModMetadata.ModGuid == "Fika");
    }

    public async Task EnsureLoadedAsync()
    {
        await _loadLock.WaitAsync();
        try
        {
            if (_runtime == null)
            {
                var snapshot = await _store.ReadSnapshotAsync();
                (snapshot.Skills.LevelingSpeed ?? throw new InvalidDataException("Missing leveling speed configuration.")).Validate();
                snapshot.Skills.NativeSkills.Validate();
                _runtime = snapshot;
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<ConfigSnapshot> GetSnapshotAsync() =>
        (await _store.ReadSnapshotAsync()) with { NativeDefaults = NativeBonusDefaults };

    public async Task<EditResult> SaveAsync(ConfigSnapshot draft)
    {
        var result = await _store.SaveAsync(
            draft.Skills,
            draft.Server,
            draft.Revision,
            snapshot => { _runtime = snapshot; Saved?.Invoke(snapshot.Skills); }
        );
        if (!result.Success)
        {
            logger.Warning($"[Skills Extended] {result.Message}");
        }

        return result.Snapshot is null ? result : result with {
            Snapshot = result.Snapshot with { NativeDefaults = NativeBonusDefaults }
        };
    }
}
