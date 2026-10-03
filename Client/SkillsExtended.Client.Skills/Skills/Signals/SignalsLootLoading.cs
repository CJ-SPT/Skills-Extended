using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using Diz.Jobs;
using EFT;
using HarmonyLib;
using SkillsExtended.Signals;
using SPT.Common.Http;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Signals;

public sealed partial class SignalsRuntime
{
    private string _caseFailure;
    private string _failedInventory;

    // Hideout worlds can retain a raid location ID. Check the world type before
    // creating a runtime; a Fika peer must never wait for a raid host here.
    private static bool IsRaidWorld(GameWorld world) => world
        && world is not HideoutGameWorld
        && SignalsMaps.Normalize(world.LocationId) != "hideout";

    public static Task Boot(GameWorld world)
    {
        if (!world || !Singleton<GameWorld>.Instantiated || Singleton<GameWorld>.Instance != world)
            return Task.CompletedTask;
        if (!IsRaidWorld(world))
        {
            if (Instance)
            {
                Instance._lifetime.Cancel();
                Destroy(Instance);
                Instance = null;
            }
            return Task.CompletedTask;
        }
        if (Instance && Instance.World == world)
            return Task.CompletedTask;
        if (Instance)
        {
            Instance._lifetime.Cancel();
            Destroy(Instance);
        }
        var runtime = world.gameObject.AddComponent<SignalsRuntime>();
        runtime.World = world;
        Instance = runtime;
        return Task.CompletedTask;
    }

    // Capture the owning world before awaiting native work. Never initialize a newer raid
    // from an older task's completion, or hide a native loading failure/cancellation.
    public static async Task CompleteLoot(Task nativeLoot, GameWorld world)
    {
        await nativeLoot;
        if (!world || !Singleton<GameWorld>.Instantiated || Singleton<GameWorld>.Instance != world)
            return;
        await Boot(world);
        if (!IsRaidWorld(world)) return;
        await Instance.FinishLoot();
    }

    public Task FinishLoot() => !IsRaidWorld(World)
        ? Task.CompletedTask : _finishLoot ??= FinishLootInternal();

    private async Task FinishLootInternal()
    {
        if (
            !SignalsMaps.IsSupported(World.LocationId)
            || (SkillsExtendedInfo.IsFikaPresent && Transport == null)
        )
            return;
        using var total = new SignalsLoadTiming("initialization", World.LocationId);
        try
        {
            _lifetime.Token.ThrowIfCancellationRequested();
            SkillsExtendedPlugin.Log.LogInfo(
                $"Signals initializing on {World.LocationId} ({SignalsMaps.Normalize(World.LocationId)}); authority={IsAuthority()}."
            );
            if (IsAuthority())
            {
                string json;
                using (new SignalsLoadTiming("manifest retrieval", World.LocationId))
                    json = await RequestHandler.GetJsonAsync("/skills-extended/signals/raid");
                _lifetime.Token.ThrowIfCancellationRequested();
                Manifest = Helpers.ConfigurationJson.Deserialize<SignalManifest>(json);
                if (Manifest == null)
                    throw new InvalidOperationException("No signal manifest returned.");
                RefreshSkillRules();
                if (Manifest.Error != null)
                {
                    Publish();
                    return;
                }
                if (
                    Manifest.PlacementCandidates.Count == 0
                    || Manifest.PlacementCandidates.Any(p =>
                        !SignalsMaps.Same(p.Map, World.LocationId)
                    )
                )
                    throw new InvalidOperationException("Signal manifest belongs to another map.");
                Authority = new SignalsAuthority(Manifest);
                State = Authority.State;
            }
            else
            {
                using var snapshotTiming = new SignalsLoadTiming("peer snapshot wait", World.LocationId);
                Send("sync");
                var wait = await Task.WhenAny(
                    _snapshotReady.Task,
                    Task.Delay(120000, _lifetime.Token)
                );
                _lifetime.Token.ThrowIfCancellationRequested();
                if (wait != _snapshotReady.Task)
                    throw new InvalidOperationException(
                        "Signal host did not supply a cache snapshot during loot loading."
                    );
                if (Manifest?.Error != null)
                    return;
            }
            _lifetime.Token.ThrowIfCancellationRequested();
            _lootReady = true;
            EnsureCase();
            if (_caseTask != null)
                await _caseTask;
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Manifest ??= new SignalManifest();
            Manifest.Error = "Signal raid initialization: " + e.Message;
            SkillsExtendedPlugin.Log.LogError(Manifest.Error);
            if (IsAuthority())
                Publish();
        }
    }

    public void LootReady()
    {
        _lootReady = true;
        EnsureCase();
    }

    private void EnsureCase()
    {
        if (!IsRaidWorld(World) || !_lootReady || _case != null || Manifest == null || Manifest.Error != null)
            return;
        // Peers poll the host every half-second. A fresh envelope must not trigger
        // repeated object creation/logging for the same already-failed inventory.
        if (_caseFailure != null && string.Equals(_failedInventory, _inventory, StringComparison.Ordinal))
        {
            Manifest.Error = _caseFailure;
            return;
        }
        if (
            !IsAuthority()
            && (State?.Ready != true || !Manifest.PlacementResolved || _inventory == null)
        )
            return;
        if (_creating)
            return;
        _creating = true;
        _caseTask = CreateCase();
    }

    private async Task CreateCase()
    {
        var inventory = _inventory;
        try
        {
            using (new SignalsLoadTiming("asset preparation", World.LocationId))
                await Preload();
            if (_lifetime.IsCancellationRequested || !this || !World)
                return;
            if (IsAuthority() && !Manifest.PlacementResolved)
            {
                SignalPlacementReport report;
                using (new SignalsLoadTiming("placement", World.LocationId))
                    report = await SignalsPlacement.Resolve(
                        this,
                        Manifest.PlacementCandidates,
                        Manifest.Seed,
                        _lifetime.Token
                    );
                _lifetime.Token.ThrowIfCancellationRequested();
                if (report.Placement == null)
                    throw new InvalidOperationException(report.ToString());
                Manifest.Placement = report.Placement;
                Manifest.PlacementResolved = true;
            }
            _lifetime.Token.ThrowIfCancellationRequested();
            using (new SignalsLoadTiming("cache creation", World.LocationId))
                _case = SignalsCase.Create(World, Manifest, inventory);
            if (IsAuthority())
            {
                Authority.Ready();
                Publish();
            }
            if (State?.Unlocked == true)
                _case.Unlock();
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            _failedInventory = inventory;
            Manifest.Error = _caseFailure = "Signal case unavailable: " + e.Message;
            SkillsExtendedPlugin.Log.LogError(Manifest.Error);
            if (IsAuthority())
                Publish();
        }
        finally
        {
            _creating = false;
        }
    }

    private Task Preload()
    {
        if (_preload != null)
            return _preload;
        var factory = Singleton<ItemFactory>.Instance;
        var resources = Newtonsoft
            .Json.Linq.JArray.Parse(Manifest.ItemsJson)
            .Skip(1)
            .Select(r => factory.ItemTemplates[(string)r["_tpl"]])
            .SelectMany(t => new[] { t.Prefab, t.UsePrefab })
            .Where(r => r != null && !string.IsNullOrEmpty(r.path))
            .Distinct()
            .ToArray();
        return _preload = Singleton<ObjectsFactory>.Instance.LoadBundlesAndCreatePools(
            ObjectsFactory.PoolsCategory.Raid,
            ObjectsFactory.AssemblyType.Local,
            resources,
            JobYieldPriority.Immediate,
            null,
            _lifetime.Token
        );
    }
}

// Both LocalGame and Fika CoopGame inherit this same closed generic base.
// Await initialization here so native world/loot synchronization sees the case.
public class SignalsLootCompletionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(LocalGame).BaseType, "SpawnLoot");

    [PatchPostfix]
    private static void Postfix(BaseLocalGame<EftGamePlayerOwner> __instance, ref Task __result) =>
        __result = SignalsRuntime.CompleteLoot(__result, __instance.GameWorld);
}

internal sealed class SignalsLoadTiming : IDisposable
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();
    private readonly string _stage;
    private readonly string _map;

    public SignalsLoadTiming(string stage, string map)
    {
        _stage = stage;
        _map = map;
    }

    public void Dispose() => SkillsExtendedPlugin.Log.LogInfo(
        $"Signals loading: {_stage} on {_map} took {_watch.Elapsed.TotalMilliseconds:0} ms.");
}
