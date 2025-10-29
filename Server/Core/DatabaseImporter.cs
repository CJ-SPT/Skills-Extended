using SkillsExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace SkillsExtended.Core;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class DatabaseImporter(
    ISptLogger<DatabaseImporter> logger,
    CustomItemService customItemService,
    DatabaseService databaseService,
    LocaleService localeService,
    ItemHelper itemHelper,
    FileUtil  fileUtil,
    JsonUtil  jsonUtil
    ) : IOnLoad
{
    public async Task OnLoad()
    {
        await LoadLocales();
        await CreateItems();
        await AddCraftsToDatabase();
        await LoadAchievements();
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

    private async ValueTask LoadLocales()
    {
        var localesPath = Path.Combine(SeModMetadata.ResourcesDirectory, "Locales");

        var importedLocales = new Dictionary<string, Dictionary<string, string>>();
        foreach (var file in Directory.GetFiles(localesPath))
        {
            var lang = Path.GetFileNameWithoutExtension(file);
            var text = await fileUtil.ReadFileAsync(file);
            var locales = jsonUtil.Deserialize<Dictionary<string, string>>(text)!;
            importedLocales[lang] = locales;
        }
        
        ImportLocales(importedLocales);
    }
    
    private void ImportLocales(Dictionary<string, Dictionary<string, string>> allLocales)
    {
        if (!allLocales.TryGetValue("en", out var enLocales))
        {
            logger.Error("[Skills Extended] No default locales found. Mod will not function.");
            return;
        }

        // Set all languages to english first.
        foreach (var (_, lazyLoad) in databaseService.GetLocales().Global)
        {
            lazyLoad.AddTransformer(transformer =>
            {
                foreach (var (key, value) in enLocales)
                {
                    transformer![key] = value;
                }
                
                return transformer;
            });
        }

        // Apply an override for other languages
        foreach (var (lang, locales) in allLocales.Where(kvp => kvp.Key != "en"))
        {
            if (databaseService.GetLocales().Global.TryGetValue(lang, out var lazyLoad))
            {
                lazyLoad.AddTransformer(transformer =>
                {
                    foreach (var (key, value) in locales)
                    {
                        transformer![key] = value;
                    }

                    return transformer;
                });
                
                continue;
            }
            
            logger.Error($"[Skills Extended] Could not find language {lang} in global locales.");
        }
    }

    private async ValueTask CreateItems()
    {
        var itemsPath = Path.Combine(SeModMetadata.ResourcesDirectory, "Items", "Items.json");
        var text = await fileUtil.ReadFileAsync(itemsPath);
        var items = jsonUtil.Deserialize<List<NewItemFromCloneDetails>>(text)!;

        foreach (var item in items)
        {
            // Skip PDA for now
            if (item.NewId == "662400eb756ca8948fe64fe8")
            {
                continue;
            }

            customItemService.CreateItemFromClone(item);
            AddItemToSpecSlots(item.NewId!);
        }
    }

    private void AddItemToSpecSlots(string itemId)
    {
        var dbItems = databaseService.GetItems();

        foreach (var (id, item) in dbItems)
        {
            if (id != "627a4e6b255f7527fb05a0f6" && item.Id != "65e080be269cbd5c5005e529")
            {
                continue;
            }

            foreach (var slot in item.Properties?.Slots ?? [])
            {
                foreach (var filter in slot.Properties?.Filters ?? [])
                {
                    filter.Filter!.Add(itemId);
                }
            }
        }
    }

    private async ValueTask AddCraftsToDatabase()
    {
        var craftsPath = Path.Combine(SeModMetadata.ResourcesDirectory, "Items", "Crafting.json");
        var text = await fileUtil.ReadFileAsync(craftsPath);
        var productions = jsonUtil.Deserialize<List<HideoutProduction>>(text)!;

        foreach (var production in productions)
        {
            databaseService.GetHideout().Production.Recipes!.Add(production);
        }
    }

    private async ValueTask LoadAchievements()
    {
        var achievementsPath = Path.Combine(SeModMetadata.ResourcesDirectory, "Achievements");
        var achievementsDb = databaseService.GetAchievements();
        
        foreach (var file in Directory.GetFiles(achievementsPath))
        {
            var text  = await fileUtil.ReadFileAsync(file);
            var achievements = jsonUtil.Deserialize<List<Achievement>>(text)!;
            
            achievementsDb.AddRange(achievements);
        }
    }
}