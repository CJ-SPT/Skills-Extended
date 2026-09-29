using System.Globalization;
using System.Text.Json.Nodes;
using SkillsExtended.Core.Editing;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition)
    {
        throw new InvalidOperationException(name);
    }

    checks++;
    Console.WriteLine($"PASS {name}");
}

var source = Path.Combine(AppContext.BaseDirectory, "Resources", "Configs");
var store = new ConfigStore(source, new ConfigFiles());
var shipped = await store.ReadSnapshotAsync();
Check(
    ConfigRules.Validate(shipped.Skills).Count == 0,
    "Every shipped field passes shared validation"
);
ConfigRules.RequireStructure(shipped.Skills);
Check(SkillCatalog.All.Count == 19, "All 19 skill pages are catalogued");
Check(
    SkillCatalog.All.All(s => s.Fields.All(f => f.Label != f.Key)),
    "Every scalar has a readable label"
);
Check(!ConfigRules.Warnings(shipped.Skills).Any(), "Every shipped door level has an XP entry");
Check(
    SkillCatalog.Fields["LockPicking"].Single(f => f.Key == "SweetSpotRangeBase").Maximum is null,
    "Lock sweet spot permits shipped 3.75 value"
);
var editor = new EditorSession(shipped);
editor.Skills.FirstAid.XpPerAction = 12.5f;
editor.Skills.NatoWeapons.Weapons.Add("offline-test-weapon");
editor.Skills.LockPicking.DoorPickLevels.Labyrinth.Add("test-door", 4);
editor.Skills.LockPicking.XpTable["5"] = 4.25f;
editor.Server.CheckForUpdates = !editor.Server.CheckForUpdates;
Check(
    editor.ChangeCount == 5,
    "Dirty count includes scalars, sets, dictionaries, and server settings"
);
Check(
    shipped.Skills.FirstAid.XpPerAction != 12.5f
        && !shipped.Skills.NatoWeapons.Weapons.Contains("offline-test-weapon")
        && !shipped.Skills.LockPicking.DoorPickLevels.Labyrinth.ContainsKey("test-door"),
    "Draft deeply isolates runtime and original snapshot"
);
editor.InputErrors["Strength.BuffJumpHeightIncMax"] = "Invalid";
Check(editor.ChangesFor("Strength") == 1, "Invalid text remains dirty across navigation");
editor.Reset(shipped);
Check(
    !editor.Dirty && editor.InputErrors.Count == 0 && editor.Inputs.Count == 0,
    "Discard restores all draft data and input state"
);
editor.Skills.NatoWeapons.Weapons = editor.Skills.NatoWeapons.Weapons.Reverse().ToHashSet();
Check(!editor.Dirty, "Weapon set order does not create changes");
foreach (var text in new[] { "", "NaN", "Infinity", "-1", "1e99", "invalid" })
{
    var field = SkillCatalog.Fields["FirstAid"].Single(f => f.Key == "XpPerAction");
    Check(field.Parse(text, out _) is not null, $"Reject invalid XP '{text}'");
}

Check(
    SkillCatalog
        .Fields["NatoWeapons"]
        .Single(f => f.Key == "SkillShareXpRatio")
        .Parse("1.01", out _)
        is not null,
    "Ratios stop at one"
);
Check(
    SkillCatalog
        .Fields["Immunity"]
        .Single(f => f.Key == "AvoidPoisonChanceElite")
        .Parse("100.01", out _)
        is not null,
    "Direct chance stops at 100 percent"
);
var culture = CultureInfo.CurrentCulture;
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
Check(
    SkillCatalog
        .Fields["FirstAid"]
        .Single(f => f.Key == "XpPerAction")
        .Parse("0,0025", out var small)
        is null
        && (float)small! == .0025f,
    "Locale decimal input preserves small XP values"
);
CultureInfo.CurrentCulture = culture;
var fake = new MemoryFiles(
    await File.ReadAllTextAsync(Path.Combine(source, "SkillsConfig.json")),
    await File.ReadAllTextAsync(Path.Combine(source, "ServerConfig.json"))
);
var transactional = new ConfigStore("fixture", fake);
var first = await transactional.ReadSnapshotAsync();
var second = await transactional.ReadSnapshotAsync();
ConfigSnapshot? published = null;
var result = await transactional.SaveAsync(
    first.Skills,
    first.Server,
    first.Revision,
    s => published = s
);
Check(
    result.Success && published is not null,
    "Both configuration files commit before runtime publication"
);
Check(
    JsonNode.DeepEquals(
        JsonNode.Parse(ConfigStore.Serialize(first.Skills)),
        JsonNode.Parse(ConfigStore.Serialize(result.Snapshot!.Skills))
    ),
    "Shipped config round-trip preserves every field and collection"
);
Check(
    result.Snapshot.Skills.ProneMovement.XpPerAction == .0025f
        && result.Snapshot.Skills.NatoWeapons.Weapons.Any(x => x.StartsWith("__")),
    "Round-trip preserves fractional XP and weapon category markers"
);
first.Skills.FirstAid.Enabled = !first.Skills.FirstAid.Enabled;
Check(
    published!.Skills.FirstAid.Enabled != first.Skills.FirstAid.Enabled,
    "Published runtime is detached from the save caller"
);
Check(
    (
        await transactional.SaveAsync(
            second.Skills,
            second.Server,
            second.Revision,
            _ => throw new Exception("Unexpected publish")
        )
    ).Status == EditStatus.Conflict,
    "Stale editor cannot overwrite a save"
);
var fresh = await transactional.ReadSnapshotAsync();
fake.ExternalEdit();
Check(
    (await transactional.SaveAsync(fresh.Skills, fresh.Server, fresh.Revision, _ => { })).Status
        == EditStatus.Conflict,
    "External file edits are detected"
);
fresh = await transactional.ReadSnapshotAsync();
fresh.Skills.LockPicking.SweetSpotRangeBase = -1;
Check(
    (await transactional.SaveAsync(fresh.Skills, fresh.Server, fresh.Revision, _ => { })).Status
        == EditStatus.Validation,
    "Backend independently rejects invalid configuration"
);
foreach (var failure in new[] { "stage1", "stage2", "replace1", "replace2" })
{
    var fixture = new MemoryFiles(
        await File.ReadAllTextAsync(Path.Combine(source, "SkillsConfig.json")),
        await File.ReadAllTextAsync(Path.Combine(source, "ServerConfig.json"))
    )
    {
        FailAt = failure,
    };
    var failingStore = new ConfigStore("fixture", fixture);
    var original = await failingStore.ReadSnapshotAsync();
    var draft = ConfigStore.Clone(original);
    draft.Skills.FirstAid.Enabled = !draft.Skills.FirstAid.Enabled;
    var didPublish = false;
    var failed = await failingStore.SaveAsync(
        draft.Skills,
        draft.Server,
        draft.Revision,
        _ => didPublish = true
    );
    Check(
        failed.Status == EditStatus.Persistence
            && !didPublish
            && (await failingStore.ReadSnapshotAsync()).Revision == original.Revision,
        $"{failure}: rollback preserves both originals and runtime"
    );
    Check(
        fixture.Paths.All(p => p.EndsWith(".json")),
        $"{failure}: staging and backup files cleaned"
    );
    fixture.FailAt = "";
    Check(
        (
            await failingStore.SaveAsync(draft.Skills, draft.Server, draft.Revision, _ => { })
        ).Success,
        $"{failure}: preserved draft can retry"
    );
}

var parallelFixture = new MemoryFiles(
    await File.ReadAllTextAsync(Path.Combine(source, "SkillsConfig.json")),
    await File.ReadAllTextAsync(Path.Combine(source, "ServerConfig.json"))
);
var parallelStore = new ConfigStore("fixture", parallelFixture);
var parallelBaseline = await parallelStore.ReadSnapshotAsync();
var concurrent = await Task.WhenAll(
    Enumerable
        .Range(0, 2)
        .Select(_ =>
            parallelStore.SaveAsync(
                parallelBaseline.Skills,
                parallelBaseline.Server,
                parallelBaseline.Revision,
                _ => { }
            )
        )
);
Check(
    concurrent.Count(r => r.Success) == 1
        && concurrent.Count(r => r.Status == EditStatus.Conflict) == 1,
    "Concurrent editors serialize and reject the stale writer"
);

// Exercise actual File.Replace/rollback semantics only in a disposable test directory.
var temp = Path.Combine(
    Path.GetTempPath(),
    "SkillsExtended-WebTests-" + Guid.NewGuid().ToString("N")
);
Directory.CreateDirectory(temp);
try
{
    File.Copy(Path.Combine(source, "SkillsConfig.json"), Path.Combine(temp, "SkillsConfig.json"));
    File.Copy(Path.Combine(source, "ServerConfig.json"), Path.Combine(temp, "ServerConfig.json"));
    var diskStore = new ConfigStore(temp, new ConfigFiles());
    var diskDraft = await diskStore.ReadSnapshotAsync();
    diskDraft.Skills.LockPicking.SweetSpotRangeBase = 3.8f;
    Check(
        (
            await diskStore.SaveAsync(
                diskDraft.Skills,
                diskDraft.Server,
                diskDraft.Revision,
                _ => { }
            )
        ).Success
            && (await diskStore.ReadSnapshotAsync()).Skills.LockPicking.SweetSpotRangeBase == 3.8f,
        "Real filesystem staged replacement round-trip"
    );
}
finally
{
    Directory.Delete(temp, true);
}

var profiles = new MemoryProfiles();
var profileEditor = new ProfileEditor(profiles);
var snapshot = profileEditor.Snapshot("one", "Pmc")!;
var profileDraft = new ProfileDraft(snapshot);
profileDraft.Edit("Endurance", "51");
Check(
    profiles.Pmc["Endurance"] == 125.25 && profileDraft.Changes["Endurance"] == 5100,
    "Profile edits remain detached until apply"
);
profileDraft.Edit("Endurance", "1");
Check(
    !profileDraft.Dirty && profileDraft.Baseline.Progress["Endurance"] == 125.25,
    "Returning to original level preserves fractional progress"
);
foreach (var value in new[] { "-1", "52", "1.5", "NaN", "" })
{
    profileDraft.Edit("Endurance", value);
    Check(
        profileDraft.Errors.ContainsKey("Endurance") && profileDraft.Dirty,
        $"Reject invalid profile level '{value}'"
    );
}

profileDraft.Edit("Endurance", "0");
Check(
    (await profileEditor.ApplyAsync(profileDraft.Baseline, profileDraft.Changes)).Success
        && profiles.Pmc["Endurance"] == 0
        && profiles.Pmc["Strength"] == 321.75
        && profiles.Scav["Endurance"] == 875.5,
    "Apply level zero changes only selected PMC skill"
);
var scav = profileEditor.Snapshot("one", "Scav")!;
Check(
    (
        await profileEditor.ApplyAsync(
            scav,
            new Dictionary<string, double> { ["Endurance"] = 5100 }
        )
    ).Success
        && profiles.Scav["Endurance"] == 5100
        && profiles.Pmc["Endurance"] == 0,
    "Apply elite level changes only selected Scav"
);
Check(
    profileEditor.Snapshot("missing", "Pmc") is null
        && profileEditor.Snapshot("one", "invalid") is null,
    "Missing profile and invalid side are handled"
);
Check(
    (
        await profileEditor.ApplyAsync(
            snapshot,
            new Dictionary<string, double> { ["Endurance"] = 200 }
        )
    ).Status == EditStatus.Conflict,
    "Changed baseline prevents profile overwrite"
);
Check(
    (
        await profileEditor.ApplyAsync(
            snapshot,
            new Dictionary<string, double> { ["Missing"] = 200 }
        )
    ).Status == EditStatus.Validation,
    "Unknown skill cannot be inserted"
);
Check(
    (
        await profileEditor.ApplyAsync(
            snapshot,
            new Dictionary<string, double> { ["Endurance"] = double.NaN }
        )
    ).Status == EditStatus.Validation,
    "Backend rejects nonfinite profile progress"
);
var failingBaseline = profileEditor.Snapshot("one", "Pmc")!;
profiles.FailNextSave = true;
Check(
    (
        await profileEditor.ApplyAsync(
            failingBaseline,
            new Dictionary<string, double> { ["Endurance"] = 400 }
        )
    ).Status == EditStatus.Persistence
        && profiles.Pmc["Endurance"] == 0
        && profiles.PersistedPmc["Endurance"] == 0,
    "Failed profile save restores and persists affected values"
);
Check(
    (
        await profileEditor.ApplyAsync(
            failingBaseline,
            new Dictionary<string, double> { ["Endurance"] = 400 }
        )
    ).Success,
    "Failed profile draft can retry successfully"
);
profiles.Exists = false;
Check(
    (
        await profileEditor.ApplyAsync(
            failingBaseline,
            new Dictionary<string, double> { ["Endurance"] = 400 }
        )
    ).Status == EditStatus.Conflict,
    "Deleted profile is rejected at apply time"
);
await AuthorizationChecks.Run(Check);
await RenderingChecks.Run(Check);
await ComponentChecks.Run(shipped, Check);
Console.WriteLine($"{checks} web editor regression checks passed.");

sealed class MemoryFiles(string skills, string server) : IConfigFiles
{
    private readonly Dictionary<string, string> _data = new()
    {
        [Path.Combine("fixture", "SkillsConfig.json")] = skills,
        [Path.Combine("fixture", "ServerConfig.json")] = server,
    };
    private int _writes;
    private int _replaces;
    public string FailAt { get; set; } = "";
    public IEnumerable<string> Paths => _data.Keys;

    public Task<string> Read(string path) => Task.FromResult(_data[path]);

    public async Task Write(string path, string text)
    {
        await Task.Yield();
        if (FailAt == "stage" + ++_writes)
        {
            throw new IOException("Simulated staging failure");
        }

        _data[path] = text;
    }

    public void Replace(string staged, string destination, string backup)
    {
        if (FailAt == "replace" + ++_replaces)
        {
            throw new IOException("Simulated replacement failure");
        }

        _data[backup] = _data[destination];
        _data[destination] = _data[staged];
        _data.Remove(staged);
    }

    public void Restore(string backup, string destination)
    {
        _data[destination] = _data[backup];
        _data.Remove(backup);
    }

    public void Delete(string path) => _data.Remove(path);

    public void ExternalEdit() => _data[Path.Combine("fixture", "SkillsConfig.json")] += "\n";
}

sealed class MemoryProfiles : IProfileSkillStore
{
    public Dictionary<string, double> Pmc { get; } =
        new() { ["Endurance"] = 125.25, ["Strength"] = 321.75 };
    public Dictionary<string, double> Scav { get; } = new() { ["Endurance"] = 875.5 };
    public Dictionary<string, double> PersistedPmc { get; private set; } = [];
    public bool FailNextSave { get; set; }
    public bool Exists { get; set; } = true;

    public IReadOnlyList<ProfileChoice> Profiles() => [new("one", "Offline fixture")];

    public IReadOnlyList<LiveSkill>? Skills(string profileId, string side)
    {
        if (!Exists || profileId != "one")
        {
            return null;
        }

        var values = side == "Pmc" ? Pmc : Scav;
        return values
            .Keys.Select(id => new LiveSkill(id, () => values[id], v => values[id] = v))
            .ToArray();
    }

    public Task Save(string profileId, string side, IReadOnlyDictionary<string, double> expected)
    {
        if (FailNextSave)
        {
            FailNextSave = false;
            throw new IOException("Simulated profile write failure");
        }

        PersistedPmc = new(Pmc);
        return Task.CompletedTask;
    }
}
