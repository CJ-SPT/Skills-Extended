// External Unity/EFT adapters only. The linked production loading code controls
// ordering, task reuse, cancellation, role selection, and failure handling.
using System.Reflection;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using SkillsExtended;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Signals;

static class Fixture
{
    public static int Requests, Preloads, Placements, Cases, CaseAttempts, Replies, Syncs;
    public static Func<Task<string>> Manifest;
    public static Task Assets;
    public static Func<Task<SignalPlacementReport>> Placement;
    public static bool CaseFailure;
    public static readonly List<string> Logs = new();

    public static SignalManifest Hunt(string map = "woods") => new()
    {
        Raid = "fixture-raid", Frequency = 99, Config = new(),
        PlacementCandidates = new() { new() { Id = "cache", Map = map, Position = new() } },
    };

    public static GameWorld Reset(bool fika = false, bool headless = false, bool authority = true)
    {
        SignalsRuntime.Instance?.Cancel();
        SignalsRuntime.Instance = null;
        Requests = Preloads = Placements = Cases = CaseAttempts = Replies = Syncs = 0;
        Logs.Clear(); CaseFailure = false;
        SkillsExtendedInfo.IsFikaPresent = fika;
        SkillsExtendedInfo.IsFikaHeadless = headless;
        SignalsRuntime.IsAuthority = () => authority;
        SignalsRuntime.Transport = fika ? _ => { } : null;
        Manifest = () => Task.FromResult(JsonConvert.SerializeObject(Hunt()));
        Assets = Task.CompletedTask;
        Placement = () =>
        {
            var report = new SignalPlacementReport();
            report.Add(new() { Location = "cache", Placement = Hunt().PlacementCandidates[0] });
            return Task.FromResult(report);
        };
        Singleton<ItemFactory>.Instance = new();
        Singleton<ObjectsFactory>.Instance = new();
        return Singleton<GameWorld>.Instance = new() { LocationId = "woods" };
    }
}

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object value) => value != null && !value.Destroyed;
        public static void Destroy(Object value) => value.Destroyed = true;
    }
    public class MonoBehaviour : Object { }
    public class GameObject { public T AddComponent<T>() where T : new() => new(); }
}
namespace Comfort.Common
{
    public static class Singleton<T> where T : class
    {
        public static T Instance;
        public static bool Instantiated => Instance != null;
    }
}
namespace JsonType { public class LocationSettings { public class Location { } } }
namespace EFT
{
    public class GameWorld : UnityEngine.Object
    {
        public string LocationId;
        public object MainPlayer;
        public UnityEngine.GameObject gameObject = new();
    }
    public class AbstractGame { }
    public class HideoutGameWorld : GameWorld { }
    public class EftGamePlayerOwner { }
    public class BaseLocalGame<T> : AbstractGame
    {
        public GameWorld GameWorld { get; set; }
        public Task SpawnLoot(JsonType.LocationSettings.Location location) => Task.CompletedTask;
    }
    public class LocalGame : BaseLocalGame<EftGamePlayerOwner> { }
    public class ResourceKey { public string path; }
    public class ItemTemplate { public ResourceKey Prefab, UsePrefab; }
    public class ItemFactory { public Dictionary<string, ItemTemplate> ItemTemplates = new(); }
    public class ObjectsFactory
    {
        public enum PoolsCategory { Raid }
        public enum AssemblyType { Local }
        public Task LoadBundlesAndCreatePools(PoolsCategory category, AssemblyType type,
            ResourceKey[] resources, object priority, object progress, CancellationToken cancellation)
        { Fixture.Preloads++; return Fixture.Assets; }
    }
}
namespace Diz.Jobs { public static class JobYieldPriority { public static object Immediate = new(); } }
namespace SPT.Common.Http
{
    public static class RequestHandler
    {
        public static Task<string> GetJsonAsync(string route) { Fixture.Requests++; return Fixture.Manifest(); }
    }
}
namespace HarmonyLib
{
    public static class AccessTools
    {
        public static bool HideHeadless;
        public static Type TypeByName(string name) => HideHeadless ? null : typeof(Fika.Headless.Classes.GameMode.HeadlessGame);
        public static PropertyInfo Property(Type type, string name) => type.GetProperty(name);
        public static MethodInfo Method(Type type, string name) => type.GetMethod(name);
        public static MethodInfo DeclaredMethod(Type type, string name, Type[] parameters) =>
            type.GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null, parameters, null);
    }
}
namespace SPT.Reflection.Patching
{
    public abstract class ModulePatch { protected abstract MethodBase GetTargetMethod(); }
    public class PatchPostfixAttribute : Attribute { }
}
namespace SkillsExtended
{
    public static class SkillsExtendedPlugin { public static TestLog Log = new(); }
    public class TestLog
    {
        public void LogInfo(string message) => Fixture.Logs.Add(message);
        public void LogError(string message) => Fixture.Logs.Add(message);
    }
}
namespace SkillsExtended.Skills.Signals
{
    public sealed partial class SignalsRuntime : UnityEngine.MonoBehaviour
    {
        public static SignalsRuntime Instance;
        public static Func<bool> IsAuthority;
        public static Action<SignalRequest> Transport;
        public GameWorld World;
        public SignalManifest Manifest;
        public SignalSnapshot State;
        public SignalsAuthority Authority;
        private SignalsCase _case;
        private bool _lootReady, _creating;
        private string _inventory;
        private Task _finishLoot, _caseTask, _preload;
        private readonly CancellationTokenSource _lifetime = new();
        private readonly TaskCompletionSource<bool> _snapshotReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Cancel() => _lifetime.Cancel();
        public bool Canceled => _lifetime.IsCancellationRequested;
        private void RefreshSkillRules() { }
        private void Send(string operation) => Fixture.Syncs++;
        private void Publish() => Fixture.Replies++;
        public void Deliver(SignalManifest manifest, SignalSnapshot state, string inventory)
        {
            Manifest = manifest; State = state; _inventory = inventory;
            _snapshotReady.TrySetResult(true);
        }
    }
    public class SignalsCase
    {
        public static SignalsCase Create(GameWorld world, SignalManifest manifest, string inventory)
        {
            Fixture.CaseAttempts++;
            if (Fixture.CaseFailure) throw new InvalidOperationException("Missing cache asset");
            Fixture.Cases++; manifest.InteractionNetId = 456; return new();
        }
        public void Unlock() { }
    }
    public static class SignalsPlacement
    {
        public static Task<SignalPlacementReport> Resolve(UnityEngine.MonoBehaviour runner,
            IEnumerable<SignalPlacement> placements, uint seed, CancellationToken cancellation)
        { Fixture.Placements++; return Fixture.Placement(); }
    }
}
namespace Fika.Core.Main.GameMode { public class CoopGame : BaseLocalGame<EftGamePlayerOwner> { } }
namespace Fika.Headless.Classes.GameMode
{
    public class HeadlessGame : AbstractGame
    {
        public GameWorld GameWorld { get; set; }
        private Task LoadLoot(JsonType.LocationSettings.Location location) => Task.CompletedTask;
        private Task LoadLoot(string unrelatedOverload) => Task.CompletedTask;
    }
}
