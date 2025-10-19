using SkillsExtended.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Services;

namespace SkillsExtended.Utils;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class DatabaseUtils(
    DatabaseService databaseService,
    LocaleService localeService,
    ItemHelper itemHelper
    )
{
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
}