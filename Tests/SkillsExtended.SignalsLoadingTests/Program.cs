using System.Reflection;
using Comfort.Common;
using EFT;
using Newtonsoft.Json;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Signals;
using SkillsExtendedFika;

var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new Exception(message);
}
MethodBase Target(object patch) => (MethodBase)patch.GetType()
    .GetMethod("GetTargetMethod", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(patch, null);
Task Hook(object game, Task native)
{
    var headless = game is Fika.Headless.Classes.GameMode.HeadlessGame;
    var patch = headless ? typeof(SignalsHeadlessLootPatch) : typeof(SignalsLootCompletionPatch);
    var args = new object[] { game, native };
    patch.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    return (Task)args[1];
}
async Task Throws<T>(Task task, string name) where T : Exception
{
    try { await task.WaitAsync(TimeSpan.FromSeconds(5)); }
    catch (T) { Check(true, name); return; }
    throw new Exception(name);
}

Check(Target(new SignalsLootCompletionPatch()).DeclaringType == typeof(BaseLocalGame<EftGamePlayerOwner>),
    "SPT and regular Fika patch the shared native loot boundary");
var headlessTarget = (MethodInfo)Target(new SignalsHeadlessLootPatch());
Check(headlessTarget.Name == "LoadLoot" && headlessTarget.ReturnType == typeof(Task)
    && headlessTarget.GetParameters()[0].ParameterType == typeof(JsonType.LocationSettings.Location),
    "Headless hook selects the exact native overload without a headless assembly reference");
HarmonyLib.AccessTools.HideHeadless = true;
try { Target(new SignalsHeadlessLootPatch()); throw new Exception("Missing type was accepted"); }
catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException)
{ Check(true, "Missing optional headless API is diagnosed"); }
HarmonyLib.AccessTools.HideHeadless = false;
Target(new SignalsHeadlessLootPatch());

foreach (var mode in new[] { "SPT", "Fika host", "Headless host" })
{
    var world = Fixture.Reset(fika: mode != "SPT", headless: mode == "Headless host");
    object game = mode switch
    {
        "SPT" => new LocalGame { GameWorld = world },
        "Fika host" => new Fika.Core.Main.GameMode.CoopGame { GameWorld = world },
        _ => new Fika.Headless.Classes.GameMode.HeadlessGame { GameWorld = world },
    };
    var native = new TaskCompletionSource();
    var assets = new TaskCompletionSource();
    Fixture.Assets = assets.Task;
    var loading = Hook(game, native.Task);
    Check(Fixture.Requests == 0 && SignalsRuntime.Instance == null, mode + " waits for native loot first");
    native.SetResult();
    Check(Fixture.Requests == 1 && Fixture.Preloads == 1 && !loading.IsCompleted,
        mode + " waits for Signals assets before returning");
    var runtime = SignalsRuntime.Instance;
    var duplicate = Hook(game, Task.CompletedTask);
    Check(ReferenceEquals(runtime.FinishLoot(), runtime.FinishLoot()) && Fixture.Requests == 1,
        mode + " shares repeated initialization calls");
    assets.SetResult();
    await Task.WhenAll(loading, duplicate).WaitAsync(TimeSpan.FromSeconds(5));
    await Hook(game, Task.CompletedTask);
    runtime.LootReady();
    Check(Fixture.Requests == 1 && Fixture.Preloads == 1 && Fixture.Placements == 1
        && Fixture.Cases == 1 && Fixture.Replies == 1 && runtime.State.Ready,
        mode + " creates and publishes exactly one ready cache");
    Check(runtime.World.MainPlayer == null && runtime.Manifest.PlacementResolved
        && runtime.Manifest.InteractionNetId == 456, mode + " needs no local player to prepare a shared identity");
    foreach (var stage in new[] { "manifest retrieval", "asset preparation", "placement", "cache creation", "initialization" })
        Check(Fixture.Logs.Any(l => l.Contains(stage) && l.Contains(" ms.")), mode + " logs " + stage);
}

foreach (var headless in new[] { false, true })
{
    var world = Fixture.Reset(fika: headless, headless: headless);
    object game = headless ? new Fika.Headless.Classes.GameMode.HeadlessGame { GameWorld = world }
        : new LocalGame { GameWorld = world };
    var error = new InvalidOperationException("Native loot failure");
    var failed = Hook(game, Task.FromException(error));
    try { await failed; throw new Exception("Native exception swallowed"); }
    catch (InvalidOperationException e) { Check(ReferenceEquals(e, error), "Original native failure preserved"); }
    Check(Fixture.Requests == 0 && Fixture.Cases == 0, "Native failure never initializes Signals");
    await Throws<OperationCanceledException>(Hook(game, Task.FromCanceled(new CancellationToken(true))),
        "Native loading cancellation preserved");
}

var unsupported = Fixture.Reset(); unsupported.LocationId = "factory4_day";
await SignalsRuntime.CompleteLoot(Task.CompletedTask, unsupported);
Check(Fixture.Requests == 0 && Fixture.Cases == 0, "Unsupported map skips Signals work");
foreach (var authority in new[] { false, true })
foreach (var typedHideout in new[] { false, true })
{
    Fixture.Reset(fika: true, authority: authority);
    // The type guard also covers a hideout retaining its previous raid map ID.
    GameWorld hideout = typedHideout ? new HideoutGameWorld { LocationId = "woods" }
        : new GameWorld { LocationId = " HideOut " };
    Singleton<GameWorld>.Instance = hideout;
    await SignalsRuntime.Boot(hideout);
    Check(SignalsRuntime.Instance == null, "Hideout InitLevel never attaches a Signals runtime");
    var native = new TaskCompletionSource();
    var loading = Hook(new LocalGame { GameWorld = hideout }, native.Task);
    Check(!loading.IsCompleted, "Hideout still waits for its own native loading");
    native.SetResult();
    await loading.WaitAsync(TimeSpan.FromSeconds(1));
    Check(SignalsRuntime.Instance == null && Fixture.Requests == 0 && Fixture.Syncs == 0
        && Fixture.Preloads == 0 && Fixture.Placements == 0 && Fixture.Cases == 0 && Fixture.Logs.Count == 0,
        "Hideout host and peer skip all Signals requests, snapshot waits, assets, placement and logs");
    await Throws<OperationCanceledException>(Hook(new LocalGame { GameWorld = hideout },
        Task.FromCanceled(new CancellationToken(true))), "Hideout native cancellation is preserved");
}
var leavingRaid = Fixture.Reset();
await SignalsRuntime.Boot(leavingRaid);
var leavingRuntime = SignalsRuntime.Instance;
var returningHideout = new HideoutGameWorld { LocationId = "woods" };
Singleton<GameWorld>.Instance = returningHideout;
await SignalsRuntime.CompleteLoot(Task.CompletedTask, returningHideout);
Check(leavingRuntime.Canceled && SignalsRuntime.Instance == null && Fixture.Requests == 0,
    "Entering the hideout cancels and clears any remaining raid runtime");
var disabled = Fixture.Reset(fika: true, headless: true);
Fixture.Manifest = () =>
{
    var manifest = Fixture.Hunt(); manifest.Config.Enabled = false; manifest.Error = "No signal hunt on this raid.";
    return Task.FromResult(JsonConvert.SerializeObject(manifest));
};
await SignalsRuntime.CompleteLoot(Task.CompletedTask, disabled);
Check(Fixture.Preloads == 0 && Fixture.Cases == 0 && Fixture.Replies == 1,
    "Disabled authoritative manifest publishes a terminal snapshot without creating a cache");
var missingBridge = Fixture.Reset(fika: true); SignalsRuntime.Transport = null;
await SignalsRuntime.CompleteLoot(Task.CompletedTask, missingBridge);
Check(Fixture.Requests == 0 && Fixture.Cases == 0, "Missing optional integration skips initialization");

var oldWorld = Fixture.Reset();
var pendingNative = new TaskCompletionSource();
var staleLoad = SignalsRuntime.CompleteLoot(pendingNative.Task, oldWorld);
Singleton<GameWorld>.Instance = new() { LocationId = "woods" };
pendingNative.SetResult(); await staleLoad;
Check(SignalsRuntime.Instance == null && Fixture.Requests == 0, "Old native completion cannot initialize a new raid");
var destroyed = Fixture.Reset(); destroyed.Destroyed = true;
await SignalsRuntime.CompleteLoot(Task.CompletedTask, destroyed);
Check(SignalsRuntime.Instance == null, "Destroyed world never gets a runtime");

var cancelWorld = Fixture.Reset(fika: true, headless: true);
var pendingManifest = new TaskCompletionSource<string>(); Fixture.Manifest = () => pendingManifest.Task;
var cancelLoad = SignalsRuntime.CompleteLoot(Task.CompletedTask, cancelWorld);
SignalsRuntime.Instance.Cancel(); pendingManifest.SetResult(JsonConvert.SerializeObject(Fixture.Hunt()));
await cancelLoad;
Check(Fixture.Cases == 0 && Fixture.Preloads == 0 && Fixture.Replies == 0,
    "Cancellation during HTTP retrieval never creates or publishes a partial cache");
var assetWorld = Fixture.Reset(); var pendingAssets = new TaskCompletionSource(); Fixture.Assets = pendingAssets.Task;
var assetLoad = SignalsRuntime.CompleteLoot(Task.CompletedTask, assetWorld);
SignalsRuntime.Instance.Cancel(); pendingAssets.SetResult(); await assetLoad;
Check(Fixture.Placements == 0 && Fixture.Cases == 0 && Fixture.Replies == 0,
    "Cancellation during asset loading never places or publishes a cache");
var placingWorld = Fixture.Reset(); var pendingPlacement = new TaskCompletionSource<SignalPlacementReport>();
Fixture.Placement = () => pendingPlacement.Task;
var placingLoad = SignalsRuntime.CompleteLoot(Task.CompletedTask, placingWorld);
SignalsRuntime.Instance.Cancel(); pendingPlacement.SetResult(new()); await placingLoad;
Check(Fixture.Cases == 0 && Fixture.Replies == 0, "Cancelled placement never publishes readiness");

var failureWorld = Fixture.Reset(fika: true, headless: true); Fixture.CaseFailure = true;
await SignalsRuntime.CompleteLoot(Task.CompletedTask, failureWorld);
Check(SignalsRuntime.Instance.Manifest.Error.Contains("Missing cache asset") && Fixture.Replies == 1
    && !SignalsRuntime.Instance.State.Ready, "Cache failure publishes an error so peers can leave the wait");
var wrongMap = Fixture.Reset(); Fixture.Manifest = () => Task.FromResult(JsonConvert.SerializeObject(Fixture.Hunt("bigmap")));
await SignalsRuntime.CompleteLoot(Task.CompletedTask, wrongMap);
Check(SignalsRuntime.Instance.Manifest.Error.Contains("another map") && Fixture.Cases == 0,
    "Wrong-map manifests never place a cache");

var peerWorld = Fixture.Reset(fika: true, authority: false);
var peerLoad = SignalsRuntime.CompleteLoot(Task.CompletedTask, peerWorld);
Check(!peerLoad.IsCompleted && Fixture.Requests == 0 && Fixture.Syncs == 1, "Peer awaits host snapshot without fetching a manifest");
var resolved = Fixture.Hunt(); resolved.Placement = resolved.PlacementCandidates[0]; resolved.PlacementResolved = true;
SignalsRuntime.Instance.Deliver(resolved, new() { Ready = true }, "host-inventory");
await peerLoad.WaitAsync(TimeSpan.FromSeconds(5));
Check(Fixture.Placements == 0 && Fixture.Cases == 1 && Fixture.Replies == 0
    && ReferenceEquals(SignalsRuntime.Instance.Manifest.Placement, resolved.Placement),
    "Peer uses the host placement and inventory without searching or broadcasting");
Check(Fixture.Logs.Any(l => l.Contains("peer snapshot wait")), "Peer snapshot wait logs elapsed time");
var cancelPeer = Fixture.Reset(fika: true, authority: false);
var cancelledPeerLoad = SignalsRuntime.CompleteLoot(Task.CompletedTask, cancelPeer);
SignalsRuntime.Instance.Cancel(); await cancelledPeerLoad.WaitAsync(TimeSpan.FromSeconds(5));
Check(Fixture.Cases == 0, "Leaving a raid cancels the peer wait promptly");
var errorPeer = Fixture.Reset(fika: true, authority: false);
var errorPeerLoad = SignalsRuntime.CompleteLoot(Task.CompletedTask, errorPeer);
SignalsRuntime.Instance.Deliver(new() { Error = "No signal hunt on this raid." }, new(), null);
await errorPeerLoad.WaitAsync(TimeSpan.FromSeconds(5));
Check(Fixture.Cases == 0, "Terminal host errors release a peer without waiting for timeout");

var failedPeer = Fixture.Reset(fika: true, authority: false); Fixture.CaseFailure = true;
var failedPeerLoad = SignalsRuntime.CompleteLoot(Task.CompletedTask, failedPeer);
SignalManifest PeerManifest()
{
    var manifest = Fixture.Hunt(); manifest.Placement = manifest.PlacementCandidates[0];
    manifest.PlacementResolved = true; return manifest;
}
SignalsRuntime.Instance.Deliver(PeerManifest(), new() { Ready = true }, "failed-inventory");
await failedPeerLoad.WaitAsync(TimeSpan.FromSeconds(5));
Check(Fixture.CaseAttempts == 1, "Peer inventory failure is diagnosed once");
for (var i = 0; i < 5; i++)
{
    SignalsRuntime.Instance.Deliver(PeerManifest(), new() { Ready = true }, "failed-inventory");
    SignalsRuntime.Instance.LootReady();
}
Check(Fixture.CaseAttempts == 1 && Fixture.Logs.Count(l => l.StartsWith("Signal case unavailable:")) == 1
    && SignalsRuntime.Instance.Manifest.Error.Contains("Missing cache asset"),
    "Repeated host snapshots cannot recreate or relog the same failed inventory");
Fixture.CaseFailure = false;
SignalsRuntime.Instance.Deliver(PeerManifest(), new() { Ready = true }, "updated-inventory");
SignalsRuntime.Instance.LootReady();
Check(Fixture.CaseAttempts == 2 && Fixture.Cases == 1, "A changed host inventory can recover from an earlier failure");

var replaced = Fixture.Reset(); await SignalsRuntime.Boot(replaced); var previous = SignalsRuntime.Instance;
var replacement = new GameWorld { LocationId = "woods" }; Singleton<GameWorld>.Instance = replacement;
await SignalsRuntime.Boot(replacement);
Check(previous.Canceled && SignalsRuntime.Instance.World == replacement, "Replacing a runtime immediately cancels its old raid work");
await SignalsRuntime.Boot(replaced);
Check(SignalsRuntime.Instance.World == replacement, "Late InitLevel completion cannot replace the current raid runtime");
Console.WriteLine($"Signals loading: {checks} lifecycle, host, headless and peer checks passed.");
