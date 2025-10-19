using SkillsExtended.Models;
using SkillsExtended.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.Core;

[Injectable]
public class SkillsStaticRouter : StaticRouter
{
    private static ISptLogger<SkillsStaticRouter>? _logger;
    private static ConfigController? _configController;
    private static JsonUtil? _jsonUtil;
    private static DatabaseUtils? _databaseUtils;
    
    public SkillsStaticRouter(
        ISptLogger<SkillsStaticRouter> logger,
        ConfigController configController,
        JsonUtil jsonUtil,
        DatabaseUtils databaseUtils
    ) : base(jsonUtil, GetRoutes())
    {
        _logger = logger;
        _configController = configController;
        _jsonUtil = jsonUtil;
        _databaseUtils = databaseUtils;
    }
    
    private static List<RouteAction> GetRoutes()
    {
        return [
            new RouteAction(
                "/skillsExtended/GetSkillsConfig",
                async (_, _, _, _) => await GetSerializedConfigResponse()
                ),
            new RouteAction(
                "/skillsExtended/GetKeys",
                async (_, _, _, _) => await GetSerializedKeysResponse()
            )
        ];
    }

    private static ValueTask<string> GetSerializedConfigResponse()
    {
        return new ValueTask<string>(_jsonUtil!.Serialize(_configController!.SkillsConfig)!);
    }

    private static ValueTask<string> GetSerializedKeysResponse()
    {
        return new ValueTask<string>(_jsonUtil!.Serialize(_databaseUtils!.GetKeyLocales())!);
    }
}