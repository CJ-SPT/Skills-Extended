using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Signals;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.ServerSignals;

[Injectable(InjectionType.Singleton)]
public class SignalsRaidService(ConfigController config, TemplateTable templates, JsonUtil json)
{
    private readonly Dictionary<string, SignalManifest> _sessions = new();
    private readonly object _gate = new();

    public SignalManifest Get(string session)
    {
        lock (_gate)
            return _sessions.GetValueOrDefault(session)
                ?? new SignalManifest { Error = "No supported PMC raid." };
    }

    public void Start(string session, string raid, string map, bool pmc)
    {
        map = SignalsMaps.Normalize(map);
        lock (_gate)
        {
            if (_sessions.TryGetValue(session, out var old) && old.Raid == raid)
                return;
            var data = json.Deserialize<Config.Skills.SignalsIntelligenceData>(
                json.Serialize(config.SkillsConfig.SignalsIntelligence)!
            )!;
            var manifest = new SignalManifest { Raid = raid, Config = data };
            _sessions[session] = manifest;
            if (!pmc || !data.Enabled || !SignalsMaps.IsSupported(map))
            {
                manifest.Error = "No signal hunt on this raid.";
                return;
            }
            try
            {
                data.Validate();
                var points = data.Placements.Where(p => p.Enabled && p.Map == map).ToArray();
                if (points.Length == 0)
                    throw new InvalidDataException("No signal placements configured for this map.");
                manifest.Seed = BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(4));
                manifest.PlacementCandidates = SignalPlacementSearch.Order(points, manifest.Seed);
                manifest.Frequency = 88 + RandomNumberGenerator.GetInt32(2001) / 100f;
                manifest.ContainerId = SignalsIds.Prefix + raid;
                manifest.RootId = new MongoId().ToString();
                manifest.ContainerTemplate = "5909d50c86f774659e6aaebe";
                manifest.ItemsJson = Generate(manifest);
                // Peers need the frozen rules, not the complete location and reward catalogs.
                manifest.Config.Placements.Clear();
                manifest.Config.Loot.Clear();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                manifest.Error = "Signal cache unavailable: " + ex.Message;
            }
        }
    }

    private string Generate(SignalManifest manifest)
    {
        JsonObject Template(string id) =>
            JsonNode.Parse(json.Serialize(templates.Items[new MongoId(id)])!)!.AsObject();
        var handbook = JsonNode.Parse(json.Serialize(templates.Handbook)!)!;
        var prices = handbook["Items"]!
            .AsArray()
            .ToDictionary(n => n!["Id"]!.GetValue<string>(), n => n!["Price"]!.GetValue<double>());
        var container = Template(manifest.ContainerTemplate);
        var grid = container["_props"]!["Grids"]![0]!;
        var width = grid["_props"]!["cellsH"]!.GetValue<int>();
        var height = grid["_props"]!["cellsV"]!.GetValue<int>();
        var occupied = new bool[width, height];
        var candidates = new List<(SignalLootEntry Entry, int Width, int Height, double Value)>();
        foreach (var entry in manifest.Config.Loot)
        {
            if (
                !templates.Items.ContainsKey(new MongoId(entry.Template))
                || !prices.TryGetValue(entry.Template, out var price)
                || price <= 0
            )
                continue;
            var item = Template(entry.Template);
            var props = item["_props"]!;
            if (
                props["QuestItem"]?.GetValue<bool>() == true
                || props["Grids"] is JsonArray { Count: > 0 }
                || props["Slots"] is JsonArray { Count: > 0 }
            )
                continue;
            // Explicit candidates may only be barter valuables or medical supplies, never keys/access cards.
            var cursor = item;
            var allowed = false;
            for (var depth = 0; depth < 12 && cursor["_parent"] is JsonNode parent; depth++)
            {
                var id = parent.GetValue<string>();
                if (id is "5448eb774bdc2d0a728b4567" or "543be5664bdc2dd4348b4569")
                {
                    allowed = true;
                    break;
                }
                if (!templates.Items.ContainsKey(new MongoId(id)))
                    break;
                cursor = Template(id);
            }
            if (!allowed)
                continue;
            candidates.Add(
                (entry, props["Width"]!.GetValue<int>(), props["Height"]!.GetValue<int>(), price)
            );
        }
        var themes = candidates.Select(c => c.Entry.Theme).Distinct().ToArray();
        if (themes.Length == 0)
            throw new InvalidDataException("No eligible signal loot templates.");
        var theme = themes[RandomNumberGenerator.GetInt32(themes.Length)];
        candidates = candidates
            .Where(c => c.Entry.Theme == theme && c.Value <= manifest.Config.MaximumLootValue)
            .ToList();
        var result = new JsonArray(
            new JsonObject
            {
                ["_id"] = manifest.RootId,
                ["_tpl"] = manifest.ContainerTemplate,
                ["upd"] = new JsonObject { ["StackObjectsCount"] = 1, ["SpawnedInSession"] = true },
            }
        );
        double total = 0;
        for (
            var attempt = 0;
            attempt < 100 && total < manifest.Config.MinimumLootValue && candidates.Count > 0;
            attempt++
        )
        {
            var roll = RandomNumberGenerator.GetInt32(candidates.Sum(c => c.Entry.Weight));
            var selected = candidates[0];
            foreach (var candidate in candidates)
            {
                selected = candidate;
                roll -= candidate.Entry.Weight;
                if (roll < 0)
                    break;
            }
            if (total + selected.Value > manifest.Config.MaximumLootValue)
            {
                candidates.Remove(selected);
                continue;
            }
            var fit = false;
            for (var y = 0; y <= height - selected.Height && !fit; y++)
            for (var x = 0; x <= width - selected.Width && !fit; x++)
            {
                var clear = true;
                for (var yy = y; yy < y + selected.Height; yy++)
                for (var xx = x; xx < x + selected.Width; xx++)
                    clear &= !occupied[xx, yy];
                if (!clear)
                    continue;
                for (var yy = y; yy < y + selected.Height; yy++)
                for (var xx = x; xx < x + selected.Width; xx++)
                    occupied[xx, yy] = true;
                result.Add(
                    new JsonObject
                    {
                        ["_id"] = new MongoId().ToString(),
                        ["_tpl"] = selected.Entry.Template,
                        ["parentId"] = manifest.RootId,
                        ["slotId"] = grid["_name"]!.GetValue<string>(),
                        ["location"] = new JsonObject
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["r"] = 0,
                            ["isSearched"] = false,
                        },
                        ["upd"] = new JsonObject
                        {
                            ["StackObjectsCount"] = 1,
                            ["SpawnedInSession"] = true,
                        },
                    }
                );
                total += selected.Value;
                fit = true;
            }
            if (!fit)
                candidates.Remove(selected);
        }
        if (total < manifest.Config.MinimumLootValue)
            throw new InvalidDataException(
                "Configured reward table cannot fill the case within its value limits."
            );
        return result.ToJsonString();
    }
}

[Injectable]
public class SignalsRaidStartPatch(SignalsRaidService service) : AbstractPatch
{
    private static SignalsRaidService _service = null!;

    protected override MethodBase GetTargetMethod()
    {
        _service = service;
        return AccessTools.Method(
            typeof(MatchController),
            nameof(MatchController.StartLocalRaidAsync)
        );
    }

    [PatchPostfix]
    private static void Postfix(
        MongoId sessionId,
        StartLocalRaidRequestData request,
        ref Task<StartLocalRaidResponseData> __result
    ) => __result = Complete(__result, sessionId.ToString(), request);

    private static async Task<StartLocalRaidResponseData> Complete(
        Task<StartLocalRaidResponseData> pending,
        string session,
        StartLocalRaidRequestData request
    )
    {
        var response = await pending;
        _service.Start(
            session,
            response.ServerId!,
            request.Location!,
            string.Equals(request.PlayerSide, "pmc", StringComparison.OrdinalIgnoreCase)
        );
        return response;
    }
}

[Injectable]
public class SignalsRouter : StaticRouter
{
    public SignalsRouter(JsonUtil json, SignalsRaidService service)
        : base(
            json,
            [
                new RouteAction(
                    "/skills-extended/signals/raid",
                    (_, _, session, _, _) =>
                        new ValueTask<object>(json.Serialize(service.Get(session.ToString()))!)
                ),
            ]
        ) { }
}
