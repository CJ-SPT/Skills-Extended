using SkillsExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Core;

[Injectable(InjectionType.Singleton, null, OnLoadOrder.PreSptModLoader)]
public class ConfigController(
    ISptLogger<ConfigController> logger,
    FileUtil fileUtil,
    JsonUtil jsonUtil
    ) : IOnLoad
{
    public ModMetadata ModMetadata { get; } = new();

    public SkillsConfig SkillsConfig { get; private set; } = null!;
    
    public async Task OnLoad()
    {
        await LoadSkillsConfig();
    }

    private async Task LoadSkillsConfig()
    {
        var path = Path.Combine(ModMetadata.ResourcesDirectory, "configs", "SkillsConfig.json");
        
        var text = await fileUtil.ReadFileAsync(path);
        SkillsConfig = jsonUtil.Deserialize<SkillsConfig>(text)!;
    }
}