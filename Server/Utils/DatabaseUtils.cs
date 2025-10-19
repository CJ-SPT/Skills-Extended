using SkillsExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Utils;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class DatabaseUtils(
    ISptLogger<DatabaseUtils> logger,
    DatabaseService databaseService,
    LocaleService localeService,
    ItemHelper itemHelper,
    FileUtil  fileUtil,
    JsonUtil  jsonUtil
    ) : IOnLoad
{
    
    public async Task OnLoad()
    {
        await ImportLocales();
    }
    
    public KeysResponse GetKeyLocales()
    {
        var items = databaseService.GetItems().Values;
        var locales = localeService.GetLocaleDb();

        var keysResponse = new KeysResponse()
        {
            KeyLocale = []
        };
        
        var keys = items.Where(
            item => item.Type == "Item" && 
                    itemHelper.IsOfBaseclasses(
                        item.Id, [BaseClasses.KEY, BaseClasses.KEY_MECHANICAL, BaseClasses.KEY_MECHANICAL]
                        )
                    );
        
        foreach (var key in keys)
        {
            keysResponse.KeyLocale[key.Id.ToString()] = locales[$"{key.Id} Name"];    
        }
        
        return keysResponse;
    }

    private async ValueTask ImportLocales()
    {
        var localesPath = Path.Combine(ModMetadata.ResourcesDirectory, "Locales");

        foreach (var file in Directory.GetFiles(localesPath))
        {
            var lang = Path.GetFileNameWithoutExtension(file);
            var text = await fileUtil.ReadFileAsync(file);
            var locales = jsonUtil.Deserialize<Dictionary<string, string>>(text)!;
            
            ImportLocale(lang, locales);
        }
    }
    
    private void ImportLocale(string lang, Dictionary<string, string> locales)
    {
        if (!databaseService.GetLocales().Global.TryGetValue(lang, out var lazyLoad))
        {
            logger.Error($"[Skills Extended] Could not find localization for '{lang}'");
            return;
        }

        foreach (var (key, value) in locales)
        {
            lazyLoad.AddTransformer(transformer =>
            {
                if (!transformer?.TryAdd(key, value) ?? false)
                {
                    transformer[key] = value;
                }
                
                return transformer;
            });
        }
    }
}