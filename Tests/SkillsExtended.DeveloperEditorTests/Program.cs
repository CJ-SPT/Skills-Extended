using System.Numerics;
using System.Text.Json;
using Mono.Cecil;
using SkillsExtended.Core.Editing;
using SkillsExtended.DeveloperTools;
using SkillsExtended.ServerDeveloperTools;
using SkillsExtended.Signals;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    checks++;
}
var point = new SignalPlacement { Id = "a", Name = "A", Map = "woods", SearchRadius = 0 };
var draft = new SignalEditorDraft();
draft.Load(new() { Status = "success", Map = "woods", Raid = "raid", Revision = "first", Placements = [point] });
point.Position.X = 99;
Check(draft.Points[0].Position.X == 0 && !draft.Dirty, "Detached load protects draft from caller mutations");
var report = new SignalPlacementReport(); report.Add(new() { Location = "a", Placement = SignalPlacementSearch.Copy(draft.Points[0]) });
Check(draft.Record("a", draft.Version, report) && draft.Valid, "Current validation is accepted");
var version = draft.Version;
draft.Edit(p => p[0].Position.X = 7);
Check(draft.Dirty && !draft.Valid && !draft.Record("a", version, report), "Moving invalidates checks and rejects stale async results");
draft.Undo(); Check(!draft.Dirty && !draft.Valid && draft.CanRedo, "Undo returns to baseline but requires fresh checks");
draft.Undo(true); Check(draft.Points[0].Position.X == 7 && draft.Dirty, "Redo restores movement");
draft.Undo(); draft.Edit(p => p[0].Enabled = false);
Check(!draft.CanRedo && draft.Valid, "A new edit discards redo and permits disabled drafts");
var emitted = draft.Request(); emitted.Placements[0].Position.X = 400;
Check(draft.Points[0].Position.X == 0, "Transport request cannot mutate draft");
draft.Edit(p => { var clone = SignalPlacementSearch.Copy(p[0]); clone.Id = "b"; p.Add(clone); });
draft.Edit(p => p.RemoveAll(x => x.Id == "a")); draft.Undo();
Check(draft.Points.Count == 2 && draft.Points.Any(p => p.Id == "a"), "Deleted cache is restored by undo");

var geometry = new SignalCaseGeometry
{
    Body = new(new(.1f, .3f, 0), new(.4f, .2f, .15f)),
    Opening = new(new(.1f, .6f, 0), new(.4f, .5f, .2f)),
    Interaction = new(new(.1f, .35f, 0), new(.4f, .15f, .2f)),
    Approaches = [new(0, 0, -.85f)], ViewTarget = new(0, .4f, 0),
};
var authored = new SignalPlacement { Id = "exact", Map = "woods", SearchRadius = 0, Yaw = 0 };
var scene = new Scene();
var exact = SignalPlacementSearch.Search([authored], 0, geometry, scene, exactYaw: true).ToArray();
Check(exact.Length == 1 && exact[0].Placement == null, "Authoring exact yaw cannot silently rotate around a collision");
var gameplay = SignalPlacementSearch.Search([authored], 0, geometry, scene).ToArray();
Check(gameplay.Last().Placement?.Yaw == 90, "Existing runtime search still uses its yaw fallback");
authored.Yaw = 90;
authored.Position.Y = .8f;
Check(SignalPlacementSearch.Search([authored], 0, geometry, scene, exactYaw: true).Single().Failure == SignalPlacementFailure.Support,
    "Floating exact preview is rejected instead of silently moved to the ground");
authored.Position.Y = 0;
for (var i = 0; i < 100; i++)
{
    var resolved = SignalPlacementSearch.Search([authored], 0, geometry, scene, exactYaw: true).Single().Placement;
    authored.Position = SignalPlacementSearch.GroundAnchor(resolved.Position, geometry);
    Check(Math.Abs(authored.Position.Y) < .00001f && resolved.Yaw == 90, "Repeated exact anchor resolution does not drift");
    var preview = SignalPlacementSearch.PreviewRoot(authored.Position, geometry);
    Check(Math.Abs(preview.Y - resolved.Position.Y) < .00001f, "Preview and resolved prefab root agree");
}
var cancellation = new CancellationTokenSource(); cancellation.Cancel();
try { SignalPlacementSearch.Search([authored], 0, geometry, scene, cancellation.Token, true).ToArray(); Check(false, "Cancelled validation accepted"); }
catch (OperationCanceledException) { checks++; }
foreach (var enabled in new[] { false, true })
foreach (var loaded in new[] { false, true })
foreach (var alive in new[] { false, true })
foreach (var headless in new[] { false, true })
foreach (var fika in new[] { false, true })
foreach (var soloHost in new[] { false, true })
    Check(DeveloperEditorPolicy.Eligible(enabled, loaded, alive, headless, fika, soloHost)
        == (enabled && loaded && alive && !headless && (!fika || soloHost)), "Solo admission matrix");
Check(!DeveloperEditorPolicy.Eligible(true, true, true, false, true, false), "Peer join revokes editor admission");
foreach (var size in new[] { (1920, 1080), (2560, 1440), (3440, 1440), (5120, 1440), (1280, 720) })
{
    var scale = DeveloperEditorLayout.Scale(size.Item1, size.Item2);
    var width = size.Item1 / scale; var height = size.Item2 / scale;
    Check(width - DeveloperEditorLayout.BrowserWidth - DeveloperEditorLayout.InspectorWidth >= 1300
        && height - DeveloperEditorLayout.ToolTop - DeveloperEditorLayout.PanelBottom >= 900,
        "Production layout preserves usable scene and inspector space at supported display sizes");
    // Inspector edge and bottom-of-screen menus were clipped in the live acceptance screenshots.
    foreach (var anchor in new[] { (width - 310, 960f, 990f, 300f), (width - 50, 1040f, 1070f, 300f), (8f, 82f, 112f, 260f) })
    {
        var popup = DeveloperEditorLayout.Popup(anchor.Item1, anchor.Item2, anchor.Item3, anchor.Item4, 192, width, height);
        Check(popup.X >= 0 && popup.Y >= 0 && popup.X + popup.Width <= width && popup.Y + popup.Height <= height,
            "Choice menu stays inside scaled panel at inspector and display edges");
        Check(popup.Y >= anchor.Item3 || popup.Y + popup.Height <= anchor.Item2,
            "Choice menu flips above its field when space below is insufficient");
    }
}

var directory = Path.Combine(Path.GetTempPath(), "SkillsExtended-DeveloperEditor-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    foreach (var name in new[] { "SkillsConfig.json", "ServerConfig.json" })
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Resources", "Configs", name), Path.Combine(directory, name));
    var files = new FaultFiles(); var store = new ConfigStore(directory, files);
    var legacy = await store.ReadSnapshotAsync();
    legacy.Skills.LockPicking.DoorPickLevels.Woods["legacy-zero"] = 0;
    legacy.Skills.LockPicking.DoorPickLevels.Woods["legacy-high"] = 8;
    Check((await store.SaveAsync(legacy.Skills, legacy.Server, legacy.Revision, _ => { })).Success,
        "Existing configuration permits legacy lock levels");
    ConfigSnapshot? published = null;
    var now = DateTime.UtcNow;
    var transactions = new DeveloperEditorTransactions(store.ReadSnapshotAsync,
        s => store.SaveAsync(s.Skills, s.Server, s.Revision, p => published = p), () => now);
    transactions.Start("owner", "raid", "Woods", true);
    var open = await transactions.Open("owner", new() { Map = "WOODS" });
    Check(open.Success && open.Map == "woods" && open.Raid == "raid", "Open binds authenticated profile to the active normalized map");
    var loaded = await transactions.Execute("owner", new() { Map = "Woods", Raid = "raid" }, false);
    var baseline = await store.ReadSnapshotAsync();
    var otherMaps = ConfigStore.Serialize(baseline.Skills.SignalsIntelligence.Placements.Where(p => p.Map != "woods"));
    var loot = ConfigStore.Serialize(baseline.Skills.SignalsIntelligence.Loot);
    var scalar = baseline.Skills.Hacking.AttemptsPerDoor;
    var request = new SignalAuthoringRequest { Map = "woods", Raid = "raid", Revision = loaded.Revision,
        Placements = [new() { Id = "saved", Map = "woods", Name = "Authored", Enabled = true, SearchRadius = 0 }] };
    // Round-trip exactly the shared client request into the native router adapter.
    var native = JsonSerializer.Deserialize<NativeSignalAuthoringRequest>(Newtonsoft.Json.JsonConvert.SerializeObject(request))!;
    Check(native.Map == request.Map && native.Placements.Single().Id == "saved", "Client payload matches the real server router request type");
    var saved = await transactions.Execute("owner", native, true);
    var after = await store.ReadSnapshotAsync();
    Check(saved.Success && published != null && saved.Revision != loaded.Revision, "Native request commits and publishes a new revision");
    Check(after.Skills.SignalsIntelligence.Placements.Single(p => p.Map == "woods").Id == "saved"
        && ConfigStore.Serialize(after.Skills.SignalsIntelligence.Placements.Where(p => p.Map != "woods")) == otherMaps
        && ConfigStore.Serialize(after.Skills.SignalsIntelligence.Loot) == loot
        && after.Skills.Hacking.AttemptsPerDoor == scalar, "Cache save preserves other maps, loot and unrelated settings");
    Check((await transactions.Execute("owner", request, true)).Status == "conflict", "Stale web/editor revision cannot overwrite a committed change");
    request.Revision = saved.Revision; request.Placements[0].Map = "bigmap";
    Check((await transactions.Execute("owner", request, true)).Status == "validation", "Cross-map placement injection rejected");
    request.Placements[0].Map = "woods"; request.Placements[0].Position.X = float.NaN;
    Check((await transactions.Execute("owner", request, true)).Status == "validation", "Nonfinite author coordinates rejected server-side");
    request.Placements[0].Position.X = 0;
    foreach (var owner in new[] { "different-profile", "" })
        Check((await transactions.Execute(owner, request, true)).Status == "session", "Transport owner is authoritative");
    request.Raid = "old-raid";
    Check((await transactions.Execute("owner", request, true)).Status == "session", "Old raid token rejected"); request.Raid = "raid";
    request.Map = "bigmap";
    Check((await transactions.Execute("owner", request, true)).Status == "session", "Wrong active map rejected"); request.Map = "woods";
    var doors = await transactions.ExecuteDoors("owner", new() { Map = "Woods", Raid = "raid" }, false);
    var doorDraft = new DoorEditorDraft(); doorDraft.Load(doors);
    var keyRules = ConfigStore.Serialize(after.Skills.Hacking.KeycardDifficulties);
    var excludedKeys = ConfigStore.Serialize(after.Skills.Hacking.ExcludedKeycards);
    var otherLocks = ConfigStore.Serialize(after.Skills.LockPicking.DoorPickLevels.Customs);
    doorDraft.Edit(r => { r.HackingDifficulties["reader"] = 3; r.ExcludedHackingDoors.Add("excluded-reader"); r.LockLevels["mechanical"] = 5; });
    doorDraft.Undo(false); Check(!doorDraft.Dirty && doorDraft.CanRedo, "Door undo restores inherited settings");
    doorDraft.Undo(true); Check(doorDraft.Dirty && doorDraft.Rules.LockLevels["mechanical"] == 5, "Door redo restores both skill rules");
    var doorNative = JsonSerializer.Deserialize<NativeDoorAuthoringRequest>(Newtonsoft.Json.JsonConvert.SerializeObject(doorDraft.Rules))!;
    var doorSaved = await transactions.ExecuteDoors("owner", doorNative, true);
    Check(doorSaved.Success, "Door editor writes both skill rules together");
    after = await store.ReadSnapshotAsync();
    Check(after.Skills.Hacking.Difficulty("Woods", "reader", null) == 3
        && after.Skills.Hacking.Excluded("WOODS", "excluded-reader", null)
        && after.Skills.LockPicking.DoorPickLevels.Woods["mechanical"] == 5
        && after.Skills.LockPicking.DoorPickLevels.Woods["legacy-zero"] == 0
        && after.Skills.LockPicking.DoorPickLevels.Woods["legacy-high"] == 8
        && ConfigStore.Serialize(after.Skills.LockPicking.DoorPickLevels.Customs) == otherLocks
        && ConfigStore.Serialize(after.Skills.Hacking.KeycardDifficulties) == keyRules
        && ConfigStore.Serialize(after.Skills.Hacking.ExcludedKeycards) == excludedKeys,
        "Door rules preserve other maps and global key rules; runtime lookup accepts native map casing");
    Check((await transactions.Execute("owner", request, true)).Status == "conflict", "Door saves invalidate stale cache revisions safely");
    doorDraft.Load(doorSaved); var bad = DoorEditorDraft.Copy(doorDraft.Rules); bad.LockLevels["new"] = 6;
    Check((await transactions.ExecuteDoors("owner", bad, true)).Status == "validation", "New door tiers outside the allowed range are rejected");
    var beforeFailure = await File.ReadAllBytesAsync(Path.Combine(directory, "SkillsConfig.json"));
    var beforeServer = await File.ReadAllBytesAsync(Path.Combine(directory, "ServerConfig.json"));
    files.FailSecondReplace = true;
    doorDraft.Edit(r => r.LockLevels["mechanical"] = 4);
    var failed = await transactions.ExecuteDoors("owner", doorDraft.Rules, true);
    var failedSkills = await File.ReadAllBytesAsync(Path.Combine(directory, "SkillsConfig.json"));
    var failedServer = await File.ReadAllBytesAsync(Path.Combine(directory, "ServerConfig.json"));
    Check(failed.Status == "persistence" && doorDraft.Dirty
        && beforeFailure.SequenceEqual(failedSkills)
        && beforeServer.SequenceEqual(failedServer),
        "Second-file failure rolls back atomically and leaves the client draft intact");
    files.FailSecondReplace = false;
    transactions.End("owner");
    Check((await transactions.ExecuteDoors("owner", doorDraft.Rules, true)).Status == "session", "Raid teardown revokes editing session");
    transactions.Start("owner", "scav", "woods", false);
    Check(!(await transactions.Open("owner", new() { Map = "woods" })).Success, "Scav raid cannot open authoring");
    transactions.Start("owner", "labs", "laboratory", true);
    Check((await transactions.ExecuteDoors("owner", new() { Map = "laboratory", Raid = "labs" }, false)).Success,
        "Generic door tool supports maps outside Signals");
    var labs = await transactions.Execute("owner", new() { Map = "laboratory", Raid = "labs" }, false);
    Check(labs.Success && labs.Placements.Count == 0, "Newly supported map opens an empty draft without generating locations");
    var previousMaps = ConfigStore.Serialize((await store.ReadSnapshotAsync()).Skills.SignalsIntelligence.Placements);
    var labSave = await transactions.Execute("owner", new()
    {
        Map = "laboratory", Raid = "labs", Revision = labs.Revision,
        Placements = [new() { Id = "manual-labs", Map = "laboratory", Name = "Manual location", Enabled = true, SearchRadius = 0 }],
    }, true);
    Check(labSave.Success && labSave.Placements.Single().Id == "manual-labs", "Manually authored new-map location saves through configuration validation");
    Check(ConfigStore.Serialize((await store.ReadSnapshotAsync()).Skills.SignalsIntelligence.Placements.Where(p => p.Map != "laboratory")) == previousMaps,
        "Enabling a new map never generates or modifies other maps' locations");
    foreach (var factory in new[] { "factory4_day", "factory4_night", "FACTORY4_DAY" })
    {
        transactions.Start("factory-owner", "factory-raid", factory, true);
        var factoryRequest = new SignalAuthoringRequest { Map = factory, Raid = "factory-raid", Revision = labSave.Revision };
        Check((await transactions.Execute("factory-owner", factoryRequest, false)).Status == "validation"
            && (await transactions.Execute("factory-owner", factoryRequest, true)).Status == "validation",
            "Factory variants reject cache authoring reads and writes");
    }
    now = now.AddHours(5);
    Check(!(await transactions.Open("owner", new() { Map = "laboratory" })).Success, "Expired raid session fails closed");
    Check(ReferenceEquals(DoorRuleMaps.Locks(after.Skills.LockPicking.DoorPickLevels, "factory4_day"),
        DoorRuleMaps.Locks(after.Skills.LockPicking.DoorPickLevels, "factory4_night")), "Factory variants share existing lock table");
}
finally
{
    if (!Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
        || !Path.GetFileName(directory).StartsWith("SkillsExtended-DeveloperEditor-", StringComparison.Ordinal))
        throw new InvalidOperationException("Unexpected fixture directory.");
    Directory.Delete(directory, true);
}

var repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
using (var client = AssemblyDefinition.ReadAssembly(Path.Combine(repo, "Client/SkillsExtended.Client.Skills/bin/Release/netstandard2.1/SkillsExtended.Client.Skills.dll")))
{
    var host = client.MainModule.GetType("SkillsExtended.DeveloperTools.SkillsDeveloperEditor");
    string Calls(string method)
    {
        var methods = host.Methods.Where(m => m.Name == method).ToList();
        foreach (var m in methods.ToArray())
        {
            var state = m.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "AsyncStateMachineAttribute");
            if (state != null) methods.AddRange(((TypeReference)state.ConstructorArguments[0].Value).Resolve().Methods.Where(x => x.HasBody));
        }
        return string.Join("\n", methods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Select(i => i.Operand?.ToString()));
    }
    var close = Calls("Close");
    Check(close.Contains("HackingInputState::Restore") && close.Contains("HackingUiInputState::Restore")
        && close.Contains("onPreCull") && close.Contains("System.Delegate::Remove") && close.Contains("SetPositionAndRotation"), "Close restores native input, cursor, camera and render callback ownership");
    Check(Calls("Update").Contains("Eligible") && Calls("Update").Contains("Close"), "Active editor continuously enforces solo admission");
    Check(!client.MainModule.AssemblyReferences.Any(r => r.Name.StartsWith("WTT")), "Standalone client has no Campaigns assembly dependency");
    Check(client.MainModule.GetType("SkillsExtended.DeveloperTools.IDeveloperEditorTool") != null
        && client.MainModule.GetType("SkillsExtended.DeveloperTools.DoorEditorTool") != null, "Generic module boundary and door implementation are compiled");
    var view = client.MainModule.GetType("SkillsExtended.DeveloperTools.DeveloperEditorView");
    var doors = client.MainModule.GetType("SkillsExtended.DeveloperTools.DoorEditorTool");
    var controls = string.Join("\n", view.Methods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Select(i => i.Operand?.ToString()));
    var inspector = string.Join("\n", doors.Methods.Where(m => m.HasBody).SelectMany(m => m.Body.Instructions).Select(i => i.Operand?.ToString()));
    Check(!inspector.Contains("DropdownField::.ctor") && inspector.Contains("DeveloperEditorView::Choice"),
        "Door fields use the bundled themed choices instead of native unskinned dropdowns");
    Check(Calls("Update").Contains("get_MenuOpen") && Calls("Update").Contains("get_MenuDismissed"),
        "Open menus and their dismissing click suppress scene placement and camera shortcuts");
    Check(Calls("SwitchTool").Contains("DismissMenu") && controls.Contains("DismissMenu"),
        "Menu ownership ends with tool changes and panel hiding");
}
Console.WriteLine($"Developer editor: {checks} draft, geometry, session, persistence and compiled-contract checks passed.");

sealed class Scene : ISignalPlacementScene
{
    public bool Navigation(Vector3 near, out Vector3 p) { p = near; return true; }
    public bool Ground(Vector3 near, out Vector3 p, out float slope) { p = new(near.X, 0, near.Z); slope = 0; return true; }
    public SignalPlacementFailure Clearance(SignalVolume v) => v.Yaw == 0 && v.Extents.X > .35f ? SignalPlacementFailure.Solid : SignalPlacementFailure.None;
    public bool Visible(Vector3 eye, Vector3 target) => true;
    public bool Route(Vector3 p) => true;
}
sealed class FaultFiles : IConfigFiles
{
    private readonly ConfigFiles _files = new();
    private int _replaces;
    public bool FailSecondReplace;
    public Task<string> Read(string path) => _files.Read(path);
    public Task Write(string path, string text) => _files.Write(path, text);
    public void Replace(string staged, string destination, string backup)
    {
        _replaces++;
        if (FailSecondReplace && _replaces % 2 == 0) throw new IOException("Injected second-file failure");
        _files.Replace(staged, destination, backup);
    }
    public void Restore(string backup, string destination) => _files.Restore(backup, destination);
    public void Delete(string path) => _files.Delete(path);
}
