using System.Reflection;
using SkillsExtended.Core.Editing;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils.Json;

public static class RewardCatalogChecks
{
    public const string ModItem = "12345678901234567890abcd";
    public const string QuestItem = "12345678901234567890abce";
    private const string Barter = "5448eb774bdc2d0a728b4567";
    public static GlobalLocaleDictionary Names =>
        new()
        {
            [ModItem + " Name"] = "Modded precision circuit board",
            [ModItem + " ShortName"] = "PCB-X",
            [QuestItem + " Name"] = "Quest circuit board",
            ["57347ca924597744596b4e71 Name"] = "Graphics card",
            ["5c12620d86f7743f8b198b72 Name"] = "Tetriz portable game console",
            ["5c05300686f7746dce784e5d Name"] = "VPX Flash Storage Module",
            ["5c05308086f7746b2101e90b Name"] = "Virtex programmable processor",
            ["5c052f6886f7746b1e3db148 Name"] = "Military COFDM Wireless Signal Transmitter",
            ["5c052fb986f7746b2101e909 Name"] = "UHF RFID Reader",
            ["5d0376a486f7747d8050965c Name"] = "Military circuit board",
            ["5d03775b86f774203e7e0c4b Name"] = "Phased array element",
            ["5d0377ce86f774186372f689 Name"] = "Iridium military thermal vision module",
            ["5d03784a86f774203e7e0c4d Name"] = "Military gyrotachometer",
            ["5d0378d486f77420421a5ff4 Name"] = "Military power filter",
            ["6389c7750ef44505c87f5996 Name"] = "Microcontroller board",
            ["6389c7f115805221fb410466 Name"] = "Far-forward GPS Signal Amplifier Unit",
            ["6389c85357baa773a825b356 Name"] = "Advanced current converter",
            ["5e2aee0a86f774755a234b62 Name"] = "Cyclon rechargeable battery",
            ["5bc9b720d4351e450201234b Name"] = "Golden 1GPhone smartphone",
            ["5c1265fc86f7743f896a21c2 Name"] = "Broken GPhone X smartphone",
            ["66d9f8744827a77e870ecaf1 Name"] = "GARY ZONT portable electronic warfare device",
            ["5d03794386f77420415576f5 Name"] = "6-STEN-140-M military battery",
            ["5e2aedd986f7746d404f3aa4 Name"] = "GreenBat lithium battery",
            ["5734758f24597738025ee253 Name"] = "Golden neck chain",
            ["5bc9bc53d4351e00367fbcee Name"] = "Golden rooster figurine",
            ["59faff1d86f7746c51718c9c Name"] = "Physical Bitcoin",
            ["59faf7ca86f7740dbe19f6c2 Name"] = "Roler Submariner gold wrist watch",
            ["5c1267ee86f77416ec610f72 Name"] = "Chain with Prokill medallion",
            ["5d235a5986f77443f6329bc6 Name"] = "Gold skull ring",
            ["5bc9bdb8d4351e003562b8a1 Name"] = "Silver Badge",
            ["5f745ee30acaeb0d490d8c5b Name"] = "Veritas guitar pick",
            ["62a09cfe4f842e1bd12da3e4 Name"] = "Golden egg",
            ["5bc9c049d4351e44f824d360 Name"] = "Battered antique book",
            ["59e3639286f7741777737013 Name"] = "Bronze lion figurine",
            ["62a091170b9d3c46de5b6cf2 Name"] = "Axel parrot figurine",
            ["655c652d60d0ac437100fed7 Name"] = "BEAR operative figurine",
            ["655c663a6689c676ce57af85 Name"] = "USEC operative figurine",
            ["655c669103999d3c810c025b Name"] = "Cultist figurine",
            ["655c66e40b2de553b618d4b8 Name"] = "Politician Mutkevich figurine",
            ["655c673673a43e23e857aebd Name"] = "Scav figurine",
            ["655c67782a1356436041c9c5 Name"] = "Ryzhy figurine",
            ["66572be36a723f7f005a066e Name"] = "Reshala figurine",
            ["66572c82ad599021091c6118 Name"] = "Killa figurine",
            ["5c0530ee86f774697952d952 Name"] = "LEDX Skin Transilluminator",
            ["5af0534a86f7743b6f354284 Name"] = "Ophthalmoscope",
            ["5c052e6986f7746b207bc3c9 Name"] = "Portable defibrillator",
            ["5c0e530286f7747fa1419862 Name"] = "Propital regenerative stimulant injector",
            ["5c0e531286f7747fa54205c2 Name"] = "SJ1 TGLabs combat stimulant injector",
            ["5c0e531d86f7747fa23f4d42 Name"] = "SJ6 TGLabs combat stimulant injector",
            ["5c0e533786f7747fa23f4d47 Name"] = "Zagustin hemostatic drug injector",
            ["5c0e534186f7747fa1419867 Name"] = "eTG-change regenerative stimulant injector",
            ["5c10c8fd86f7743d7d706df3 Name"] = "Adrenaline injector",
            ["5ed515c8d380ab312177c0fa Name"] = "3-(b-TG) stimulant injector",
            ["5ed515e03a40a50460332579 Name"] = "L1 (Norepinephrine) injector",
            ["5ed515ece452db0eb56fc028 Name"] = "P22 (Product 22) stimulant injector",
            ["5ed515f6915ec335206e4152 Name"] = "AHF1-M stimulant injector",
            ["5ed5160a87bb8443d10680b5 Name"] = "Meldonin injector",
            ["5ed51652f6c34d2cc26336a1 Name"] = "M.U.L.E. stimulant injector",
            ["5fca138c2a7b221b2852a5c6 Name"] = "xTG-12 antidote injector",
            ["5fca13ca637ee0341a484f46 Name"] = "SJ9 TGLabs combat stimulant injector",
            ["637b6251104668754b72f8f9 Name"] = "Perfotoran (Blue Blood) stimulant injector",
            ["60098ad7c2240c0fe85c570a Name"] = "AFAK tactical individual first aid kit",
            ["5751a89d24597722aa0e8db0 Name"] = "Golden Star balm",
        };

    public static TemplateTable Templates()
    {
        var templates = new TemplateTable
        {
            Items = [],
            Handbook = new HandbookBase { Items = [], Categories = [] },
            Character = [],
            CustomisationStorage = [],
            Prestige = null!,
            Quests = [],
            RepeatableQuests = null!,
            Customization = [],
            Dialogue = null!,
            Profiles = [],
            Prices = [],
            DefaultEquipmentPresets = [],
            Achievements = [],
            CustomAchievements = [],
            LocationServices = null!,
        };
        foreach (var name in Names.Where(pair => pair.Key.EndsWith(" Name")))
        {
            var id = new MongoId(name.Key[..24]);
            templates.Items[id] = new TemplateItem
            {
                Id = id,
                Name = "internal_" + name.Value,
                Type = "Item",
                Parent = new MongoId(Barter),
                Properties = new TemplateItemProperties { Width = 2, Height = 1 },
            };
            templates.Handbook.Items.Add(new HandbookItem { Id = id, Price = 50000 });
        }
        templates.Items[new MongoId(QuestItem)].Properties!.QuestItem = true;
        return templates;
    }

    public static LocaleService Locales() =>
        new(
            DispatchProxy.Create<ISptLogger<LocaleService>, SilentLogger>(),
            new LocaleTable
            {
                Global = new() { ["en"] = new LazyLoad<GlobalLocaleDictionary>(() => Names) },
                Menu = [],
                Languages = [],
            },
            new LocaleConfig
            {
                GameLocale = "en",
                ServerLocale = "en",
                ServerSupportedLocales = ["en"],
                Fallbacks = [],
            }
        );

    public static void Run(Action<bool, string> check)
    {
        var templates = Templates();
        var catalog = new RewardItemCatalog(templates, Names);
        check(
            catalog.Search("CIRCUIT precision", true).Single().Id == ModItem,
            "Item search matches localized words in any order, including mod-added items"
        );
        check(
            catalog.Search("pcb-x", true).Single().Id == ModItem,
            "Item search matches localized short names without case sensitivity"
        );
        check(
            catalog.Search(ModItem.ToUpperInvariant(), true).Single().Id == ModItem,
            "Item search resolves an exact template ID"
        );
        check(
            catalog.Search("circuit", true).All(item => item.Id != QuestItem)
                && catalog.Search("circuit", true).Any(item => item.Id == ModItem)
                && catalog.Search("circuit", false).Any(item => item.Id == QuestItem),
            "Eligibility filter hides quest rewards while all-items search explains them"
        );
        check(
            catalog.Search("   ", false).Count == 0 && catalog.Search("a", false, 2).Count == 2,
            "Search ignores blank input and bounds result size"
        );
        check(
            catalog.Find("unknown") == null && catalog.Find(ModItem)?.Price == 50000,
            "Catalog handles unknown IDs and displays server handbook values"
        );
        check(
            new RewardItemCatalog(templates, new Dictionary<string, string>())
                .Find(ModItem)!
                .Name.StartsWith("internal_"),
            "Missing translations fall back to the server item name"
        );
        templates.Items[new MongoId(ModItem)].Parent = new MongoId("000000000000000000000000");
        check(
            new RewardItemCatalog(templates, Names).Search("precision", true).Count == 0,
            "Non-barter and non-medical items are not offered as eligible rewards"
        );
        templates.Items[new MongoId(ModItem)].Parent = new MongoId(ModItem);
        check(
            new RewardItemCatalog(templates, Names).Search("precision", true).Count == 0,
            "Cyclic modded ancestry cannot hang item search"
        );
    }
}
