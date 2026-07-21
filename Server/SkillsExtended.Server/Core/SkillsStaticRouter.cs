using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Core;

[Injectable]
public class SkillsStaticRouter : StaticRouter
{
    private static ISptLogger<SkillsStaticRouter>? _logger;
    private static ConfigController? _configController;
    private static JsonUtil? _jsonUtil;
    private static DatabaseImporter? _databaseImporter;

    public SkillsStaticRouter(
        ISptLogger<SkillsStaticRouter> logger,
        ConfigController configController,
        JsonUtil jsonUtil,
        DatabaseImporter databaseImporter
    )
        : base(jsonUtil, GetRoutes())
    {
        _logger = logger;
        _configController = configController;
        _jsonUtil = jsonUtil;
        _databaseImporter = databaseImporter;
    }

    private static List<RouteAction> GetRoutes()
    {
        return
        [
            new RouteAction(
                "/skills-extended/early-init",
                async (_, _, _, _, _) => await GetSerializedEnumEntriesResponse()
            ),
            new RouteAction(
                "/skills-extended/get-skills-config",
                async (_, _, _, _, _) => await GetSerializedConfigResponse()
            ),
            new RouteAction(
                "/skills-extended/get-keys",
                async (_, _, _, _, _) => await GetSerializedKeysResponse()
            ),
        ];
    }

    private static ValueTask<string> GetSerializedConfigResponse()
    {
        return new ValueTask<string>(_jsonUtil!.Serialize(_configController!.SkillsConfig)!);
    }

    private static ValueTask<string> GetSerializedKeysResponse()
    {
        return new ValueTask<string>(_jsonUtil!.Serialize(_databaseImporter!.GetKeyLocales())!);
    }

    private static ValueTask<string> GetSerializedEnumEntriesResponse()
    {
        return new ValueTask<string>(_jsonUtil!.Serialize(_configController!.EnumEntries)!);
    }
}
