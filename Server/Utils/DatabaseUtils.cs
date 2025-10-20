using SkillsExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Utils;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class DatabaseUtils(
    ISptLogger<DatabaseUtils> logger,
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
        await ImportLocales();
        await CreateItems();
        await AddCraftsToDatabase();
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
        var localesPath = Path.Combine(SeModMetadata.ResourcesDirectory, "Locales");

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
}