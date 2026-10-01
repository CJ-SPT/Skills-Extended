using System.Reflection;
using HarmonyLib;
using SkillsExtended.Core;
using SkillsExtended.Core.Editing;
using SkillsExtended.Signals;
using SkillsExtended.DeveloperTools;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace SkillsExtended.ServerDeveloperTools;

public sealed class NativeDeveloperEditorRequest : DeveloperEditorRequest, IRequestData { }
public sealed class NativeSignalAuthoringRequest : SignalAuthoringRequest, IRequestData { }
public sealed class NativeDoorAuthoringRequest : DoorAuthoringRequest, IRequestData { }

// The same transaction boundary is exercised with temporary config files offline.
public sealed class DeveloperEditorTransactions(
    Func<Task<ConfigSnapshot>> read, Func<ConfigSnapshot, Task<EditResult>> save, Func<DateTime>? utcNow = null)
{
    private readonly Func<DateTime> _now = utcNow ?? (() => DateTime.UtcNow);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, (string Raid, string Map, DateTime Expires)> _sessions = new();
    public void Start(string owner, string raid, string map, bool pmc)
    {
        _gate.Wait();
        try
        {
            _sessions.Remove(owner);
            if (pmc && !string.IsNullOrWhiteSpace(map))
                _sessions[owner] = (raid, SignalsMaps.Normalize(map), _now().AddHours(4));
        }
        finally { _gate.Release(); }
    }
    public void End(string owner)
    {
        _gate.Wait();
        try { _sessions.Remove(owner); }
        finally { _gate.Release(); }
    }
    public async Task<DeveloperEditorReply> Open(string owner, DeveloperEditorRequest request)
    {
        await _gate.WaitAsync();
        try
        {
            if (!_sessions.TryGetValue(owner, out var session) || session.Expires < _now()
                || request == null || !SignalsMaps.Same(session.Map, request.Map))
                return new() { Status = "session", Message = "Load a PMC raid on this map before opening the developer editor." };
            return new() { Status = "success", Map = session.Map, Raid = session.Raid };
        }
        finally { _gate.Release(); }
    }
    public async Task<DoorAuthoringReply> ExecuteDoors(string owner, DoorAuthoringRequest request, bool write)
    {
        await _gate.WaitAsync();
        try
        {
            if (!_sessions.TryGetValue(owner, out var session) || session.Expires < _now()
                || !DeveloperEditorPolicy.Accepts(session.Raid, session.Map, request))
                return new() { Status = "session", Message = "No matching active PMC raid on this map." };
            var snapshot = await read();
            var locks = DoorRuleMaps.Locks(snapshot.Skills.LockPicking.DoorPickLevels, session.Map);
            var hacking = snapshot.Skills.Hacking;
            if (write)
            {
                if (snapshot.Revision != request.Revision)
                    return new() { Status = "conflict", Message = "Configuration changed in another tool. Your draft is intact; reload explicitly." };
                if (request.HackingDifficulties == null || request.ExcludedHackingDoors == null || request.LockLevels == null
                    || request.HackingDifficulties.Count > 5000 || request.ExcludedHackingDoors.Count > 5000 || request.LockLevels.Count > 5000
                    || request.HackingDifficulties.Any(p => string.IsNullOrWhiteSpace(p.Key) || p.Key.Contains('/') || p.Value < 1 || p.Value > 3)
                    || request.ExcludedHackingDoors.Any(p => string.IsNullOrWhiteSpace(p) || p.Contains('/'))
                    || request.LockLevels.Any(p => string.IsNullOrWhiteSpace(p.Key) || p.Key != p.Key.Trim() || p.Key.Contains('/')
                        || ((p.Value < 1 || p.Value > 5) && (locks == null || !locks.TryGetValue(p.Key, out var old) || old != p.Value)))
                    || locks == null && request.LockLevels.Count > 0)
                    return new() { Status = "validation", Message = "Invalid door rules: Hacking tiers are 1–3, lock tiers are 1–5, and IDs must be current-map door IDs." };
                // Retain legacy keys on other maps; canonicalize only the edited map.
                hacking.DoorDifficulties = hacking.DoorDifficulties.Where(p => !DoorRuleMaps.OnMap(p.Key, session.Map))
                    .Concat(request.HackingDifficulties.Select(p => new KeyValuePair<string, int>(session.Map + "/" + p.Key, p.Value)))
                    .ToDictionary(p => p.Key, p => p.Value);
                hacking.ExcludedDoors = hacking.ExcludedDoors.Where(p => !DoorRuleMaps.OnMap(p, session.Map))
                    .Concat(request.ExcludedHackingDoors.Distinct().Select(p => session.Map + "/" + p)).ToList();
                if (locks != null)
                {
                    locks.Clear();
                    foreach (var pair in request.LockLevels) locks.Add(pair.Key, pair.Value);
                }
                var result = await save(snapshot);
                if (!result.Success) return new() { Status = result.Status.ToString().ToLowerInvariant(), Message = result.Message };
                snapshot = result.Snapshot!; hacking = snapshot.Skills.Hacking;
                locks = DoorRuleMaps.Locks(snapshot.Skills.LockPicking.DoorPickLevels, session.Map);
            }
            return new()
            {
                Status = "success", Map = session.Map, Raid = session.Raid, Revision = snapshot.Revision,
                Message = write ? "Door rules saved. Restart the game client to load them; this raid is unchanged." : "Door rules loaded.",
                Hacking = hacking, LockPickingEnabled = snapshot.Skills.LockPicking.Enabled, LockMapSupported = locks != null,
                MissingXpLevels = Enumerable.Range(1, 5).Concat(locks?.Values ?? Enumerable.Empty<int>()).Distinct()
                    .Where(n => !snapshot.Skills.LockPicking.XpTable.ContainsKey(n.ToString())).ToList(),
                Rules = new()
                {
                    Map = session.Map, Raid = session.Raid, Revision = snapshot.Revision,
                    HackingDifficulties = hacking.DoorDifficulties.Where(p => DoorRuleMaps.OnMap(p.Key, session.Map))
                        .GroupBy(p => DoorRuleMaps.Door(p.Key)).ToDictionary(g => g.Key, g => g.First().Value),
                    ExcludedHackingDoors = hacking.ExcludedDoors.Where(p => DoorRuleMaps.OnMap(p, session.Map)).Select(DoorRuleMaps.Door).Distinct().ToList(),
                    LockLevels = locks == null ? new() : new(locks),
                },
            };
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        { return new() { Status = "persistence", Message = "Door operation failed; your draft is intact. " + e.Message }; }
        finally { _gate.Release(); }
    }
    public async Task<SignalAuthoringReply> Execute(string owner, SignalAuthoringRequest request, bool write)
    {
        await _gate.WaitAsync();
        try
        {
            if (!_sessions.TryGetValue(owner, out var session) || session.Expires < _now()
                || !DeveloperEditorPolicy.Accepts(session.Raid, session.Map, request))
                return new() { Status = "session", Message = "No matching active PMC raid on this map. Reopen the editor in a loaded raid." };
            if (!SignalsMaps.IsSupported(session.Map))
                return new() { Status = "validation", Message = "Signal caches are unavailable on Factory." };
            var snapshot = await read();
            if (write)
            {
                if (snapshot.Revision != request.Revision)
                    return new() { Status = "conflict", Message = "Configuration changed in another editor. Your draft is intact; reload explicitly to use the saved configuration." };
                if (request.Placements == null || request.Placements.Count > 500
                    || request.Placements.Any(p => p == null || p.Map != session.Map || p.Position?.IsFinite != true))
                    return new() { Status = "validation", Message = "Placements must belong to the current map; at most 500 are allowed." };
                snapshot.Skills.SignalsIntelligence.Placements = snapshot.Skills.SignalsIntelligence.Placements
                    .Where(p => p.Map != session.Map).Select(SignalPlacementSearch.Copy)
                    .Concat(request.Placements.Select(SignalPlacementSearch.Copy)).ToList();
                var result = await save(snapshot);
                if (!result.Success)
                    return new() { Status = result.Status.ToString().ToLowerInvariant(), Message = result.Message };
                snapshot = result.Snapshot!;
            }
            return new()
            {
                Status = "success", Map = session.Map, Raid = session.Raid, Revision = snapshot.Revision,
                Placements = snapshot.Skills.SignalsIntelligence.Placements.Where(p => p.Map == session.Map)
                    .Select(SignalPlacementSearch.Copy).ToList(),
                Message = write ? "Placements saved for future raids. The current raid cache is unchanged." : "Placements loaded.",
            };
        }
        catch (Exception e) when (e is not OutOfMemoryException)
        { return new() { Status = "persistence", Message = "Configuration operation failed; your draft is intact. " + e.Message }; }
        finally { _gate.Release(); }
    }
}

[Injectable(InjectionType.Singleton)]
public sealed class DeveloperEditorService(ConfigController config)
{
    public DeveloperEditorTransactions Transactions { get; } = new(config.GetSnapshotAsync, config.SaveAsync);
}

[Injectable]
public sealed class DeveloperEditorRouter : StaticRouter
{
    public DeveloperEditorRouter(JsonUtil json, DeveloperEditorService service) : base(json,
    [
        new RouteAction<NativeDeveloperEditorRequest>("/skills-extended/editor/session",
            async (_, r, id, _, _) => json.Serialize(await service.Transactions.Open(id.ToString(), r))!),
        new RouteAction<NativeDoorAuthoringRequest>("/skills-extended/editor/doors/read",
            async (_, r, id, _, _) => json.Serialize(await service.Transactions.ExecuteDoors(id.ToString(), r, false))!),
        new RouteAction<NativeDoorAuthoringRequest>("/skills-extended/editor/doors/save",
            async (_, r, id, _, _) => json.Serialize(await service.Transactions.ExecuteDoors(id.ToString(), r, true))!),
        new RouteAction<NativeSignalAuthoringRequest>("/skills-extended/signals/editor/read",
            async (_, r, id, _, _) => json.Serialize(await service.Transactions.Execute(id.ToString(), r, false))!),
        new RouteAction<NativeSignalAuthoringRequest>("/skills-extended/signals/editor/save",
            async (_, r, id, _, _) => json.Serialize(await service.Transactions.Execute(id.ToString(), r, true))!),
    ]) { }
}

[Injectable]
public sealed class DeveloperEditorRaidEndPatch(DeveloperEditorService service) : AbstractPatch
{
    private static DeveloperEditorService _service = null!;
    protected override MethodBase GetTargetMethod()
    {
        _service = service;
        return AccessTools.Method(typeof(MatchController), nameof(MatchController.EndLocalRaidAsync));
    }
    [PatchPrefix]
    private static void Prefix(MongoId sessionId) => _service.Transactions.End(sessionId.ToString());
}

[Injectable]
public sealed class DeveloperEditorRaidStartPatch(DeveloperEditorService service) : AbstractPatch
{
    private static DeveloperEditorService _service = null!;
    protected override MethodBase GetTargetMethod()
    {
        _service = service;
        return AccessTools.Method(typeof(MatchController), nameof(MatchController.StartLocalRaidAsync));
    }
    [PatchPostfix]
    private static void Postfix(MongoId sessionId, StartLocalRaidRequestData request, ref Task<StartLocalRaidResponseData> __result)
        => __result = Complete(__result, sessionId.ToString(), request);
    private static async Task<StartLocalRaidResponseData> Complete(Task<StartLocalRaidResponseData> pending, string owner, StartLocalRaidRequestData request)
    {
        var reply = await pending;
        _service.Transactions.Start(owner, reply.ServerId!, request.Location!,
            string.Equals(request.PlayerSide, "pmc", StringComparison.OrdinalIgnoreCase));
        return reply;
    }
}
