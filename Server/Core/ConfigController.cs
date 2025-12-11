using SkillsExtended.Config;
using SkillsExtended.Helpers;
using SkillsExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Core;

[Injectable(InjectionType.Singleton, null, OnLoadOrder.PreSptModLoader)]
public class ConfigController(
    ISptLogger<ConfigController> logger,
    FileUtil fileUtil,
    IReadOnlyList<SptMod> loadedMods
    ) : IOnLoad
{
    public ServerConfig ServerConfig { get; private set; } = null!;
    public SkillsConfig SkillsConfig { get; private set; } = null!;
    
    public bool IsFikaPresent { get; private set; }
    
    public async Task OnLoad()
    {
        await LoadSkillsConfig();
        await LoadServerConfig();
        
        IsFikaPresent = loadedMods.Any(m => m.ModMetadata.ModGuid == "Fika");

        if (IsFikaPresent)
        {
            logger.Warning("[Skills Extended] Fika has been detected -- Compatibility is experimental. Use at your own risk.");
            logger.Warning("[Skills Extended] Lockpicking has been disabled.");
        }
    }

    public async Task SaveSkillsConfig()
    {
        var path = Path.Combine(SeModMetadata.ResourcesDirectory, "Configs", "SkillsConfig.json");
        
        var text = DataContractSerializer.Serialize(SkillsConfig);
        await fileUtil.WriteFileAsync(path, text!);
    }
    
    private async Task LoadSkillsConfig()
    {
        var path = Path.Combine(SeModMetadata.ResourcesDirectory, "Configs", "SkillsConfig.json");
        
        var text = await fileUtil.ReadFileAsync(path);
        SkillsConfig = DataContractSerializer.Deserialize<SkillsConfig>(text)!;
    }
    
    public async Task SaveServerConfig()
    {
        var path = Path.Combine(SeModMetadata.ResourcesDirectory, "Configs", "ServerConfig.json");
        
        var text = DataContractSerializer.Serialize(ServerConfig);
        await fileUtil.WriteFileAsync(path, text!);
    }
    
    private async Task LoadServerConfig()
    {
        var path = Path.Combine(SeModMetadata.ResourcesDirectory, "Configs", "ServerConfig.json");
        
        var text = await fileUtil.ReadFileAsync(path);
        ServerConfig = DataContractSerializer.Deserialize<ServerConfig>(text)!;
    }
}