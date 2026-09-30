using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace SkillsExtended.Core.Editing;

// A per-editor snapshot of the loaded server database, including mod-added items.
public sealed class RewardItemCatalog
{
    private readonly Dictionary<string, RewardItem> _items;

    public RewardItemCatalog(TemplateTable templates, IReadOnlyDictionary<string, string> locales)
    {
        var prices = (templates.Handbook?.Items ?? [])
            .GroupBy(item => item.Id.ToString())
            .ToDictionary(group => group.Key, group => group.Last().Price ?? 0);
        var templatesById = (templates.Items ?? []).ToDictionary(
            pair => pair.Key.ToString(),
            pair => pair.Value
        );
        var grid = templatesById
            .GetValueOrDefault("5909d50c86f774659e6aaebe")
            ?.Properties?.Grids?.FirstOrDefault()
            ?.Properties;
        _items = templatesById
            .Where(pair => pair.Value.Type == "Item")
            .ToDictionary(
                pair => pair.Key,
                pair =>
                {
                    var item = pair.Value;
                    var name = locales.GetValueOrDefault(pair.Key + " Name");
                    var shortName = locales.GetValueOrDefault(pair.Key + " ShortName", "");
                    var price = prices.GetValueOrDefault(pair.Key);
                    return new RewardItem(
                        pair.Key,
                        string.IsNullOrWhiteSpace(name) ? item.Name ?? pair.Key : name,
                        shortName,
                        price,
                        item.Properties?.Width ?? 0,
                        item.Properties?.Height ?? 0,
                        UnavailableReason(item, price, templatesById, grid)
                    );
                }
            );
    }

    public RewardItem? Find(string id) => _items.GetValueOrDefault(id);

    public IReadOnlyList<RewardItem> Search(string query, bool eligibleOnly, int limit = 30)
    {
        var words = query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return [];
        }

        return _items
            .Values.Where(item =>
                (!eligibleOnly || item.UnavailableReason.Length == 0)
                && words.All(word =>
                    item.Id.Contains(word, StringComparison.OrdinalIgnoreCase)
                    || item.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
                    || item.ShortName.Contains(word, StringComparison.OrdinalIgnoreCase)
                )
            )
            .OrderByDescending(item =>
                item.Id.Equals(query.Trim(), StringComparison.OrdinalIgnoreCase)
            )
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArray();
    }

    private static string UnavailableReason(
        TemplateItem item,
        double price,
        IReadOnlyDictionary<string, TemplateItem> templates,
        GridProperties? grid
    )
    {
        // Mirror SignalsRaidService's candidate rules without changing the loot generator.
        if (price <= 0)
        {
            return "No positive handbook value; excluded from cache rewards.";
        }
        if (item.Properties == null || item.Properties.QuestItem == true)
        {
            return "Quest item or missing item data; excluded from cache rewards.";
        }
        if (item.Properties.Grids?.Any() == true || item.Properties.Slots?.Any() == true)
        {
            return "Items with storage or attachment slots are excluded from cache rewards.";
        }
        if (
            item.Properties.Width is not > 0
            || item.Properties.Height is not > 0
            || (
                grid != null
                && (item.Properties.Width > grid.CellsH || item.Properties.Height > grid.CellsV)
            )
        )
        {
            return "Item dimensions do not fit the signal cache.";
        }
        var cursor = item;
        for (var depth = 0; depth < 12; depth++)
        {
            var parent = cursor.Parent.ToString();
            if (parent is "5448eb774bdc2d0a728b4567" or "543be5664bdc2dd4348b4569")
            {
                return "";
            }
            if (!templates.TryGetValue(parent, out cursor))
            {
                break;
            }
        }
        return "Only barter valuables and medical supplies can be cache rewards.";
    }
}

public sealed record RewardItem(
    string Id,
    string Name,
    string ShortName,
    double Price,
    int Width,
    int Height,
    string UnavailableReason
);
