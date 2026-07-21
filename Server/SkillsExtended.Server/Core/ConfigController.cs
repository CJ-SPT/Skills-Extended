using SkillsExtended.Config;
using SkillsExtended.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Core;

[Injectable(InjectionType.Singleton, OnLoadOrder.Preload)]
public class ConfigController(
    ISptLogger<ConfigController> logger,
    FileUtil fileUtil,
    JsonUtil jsonUtil,
    IReadOnlyList<SptMod> loadedMods
) : IOnLoad
{
    public ServerConfig ServerConfig { get; private set; } = null!;
    public SkillsConfig SkillsConfig { get; private set; } = null!;
    public IReadOnlyList<EnumEntryDefinition> EnumEntries { get; private set; } = [];

    public bool IsFikaPresent { get; private set; }

    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        await LoadEnumEntries();
        await LoadSkillsConfig();
        await LoadServerConfig();

        IsFikaPresent = loadedMods.Any(m => m.ModMetadata.ModGuid == "Fika");
    }

    private async Task LoadEnumEntries()
    {
        var path = Path.Combine(ModMetadata.ResourcesDirectory, "Configs", "EnumEntries.json");

        var text = await fileUtil.ReadFileAsync(path);
        EnumEntries = jsonUtil.Deserialize<List<EnumEntryDefinition>>(text)!;
    }

    public async Task SaveSkillsConfig()
    {
        var path = Path.Combine(ModMetadata.ResourcesDirectory, "Configs", "SkillsConfig.json");

        var text = jsonUtil.Serialize(SkillsConfig, true);
        await fileUtil.WriteFileAsync(path, text!);
    }

    private async Task LoadSkillsConfig()
    {
        var path = Path.Combine(ModMetadata.ResourcesDirectory, "Configs", "SkillsConfig.json");

        var text = await fileUtil.ReadFileAsync(path);
        SkillsConfig = jsonUtil.Deserialize<SkillsConfig>(text)!;
    }

    public async Task SaveServerConfig()
    {
        var path = Path.Combine(ModMetadata.ResourcesDirectory, "Configs", "ServerConfig.json");

        var text = jsonUtil.Serialize(ServerConfig, true);
        await fileUtil.WriteFileAsync(path, text!);
    }

    private async Task LoadServerConfig()
    {
        var path = Path.Combine(ModMetadata.ResourcesDirectory, "Configs", "ServerConfig.json");

        var text = await fileUtil.ReadFileAsync(path);
        ServerConfig = jsonUtil.Deserialize<ServerConfig>(text)!;
    }
}
