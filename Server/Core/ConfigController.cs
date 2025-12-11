using SkillsExtended.Config;
using SkillsExtended.Helpers;
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
    public SeModMetadata SeModMetadata { get; } = new();

    public SkillsConfig SkillsConfig { get; private set; } = null!;
    
    public bool IsFikaPresent { get; private set; }
    
    public async Task OnLoad()
    {
        await LoadSkillsConfig();
        IsFikaPresent = loadedMods.Any(m => m.ModMetadata.ModGuid == "Fika");

        if (IsFikaPresent)
        {
            logger.Warning("[Skills Extended] Fika has been detected -- Disabling Lock-picking");
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
}