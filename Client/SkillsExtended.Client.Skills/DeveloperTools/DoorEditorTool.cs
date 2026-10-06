using System;
using EFT;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EFT.Interactive;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Hacking;
using SkillsExtended.Skills.LockPicking;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkillsExtended.DeveloperTools;

internal sealed class DoorEditorTool : IDeveloperEditorTool
{
    public string Id => "doors";
    public string Title => LocalizedText.Get("SkillsExtended.DoorEditorTool.Doors");
    public bool Supports(string map) => !string.IsNullOrWhiteSpace(map);
    public bool Busy { get; private set; }
    public bool Dirty => _draft.Dirty;
    private readonly DoorEditorDraft _draft = new();
    private readonly Dictionary<string, WorldInteractiveObject> _doors = new();
    private DeveloperEditorContext _context;
    private string _selected = "";
    private int _filter;
    private Button _label;
    private WorldInteractiveObject _labelDoor;
    private string _labelSceneName, _labelKey;
    private DeveloperEditorView View => _context.View;
    private WorldInteractiveObject Selected => _doors.TryGetValue(_selected, out var door) && door ? door : null;
    public async Task Activate(DeveloperEditorContext context)
    {
        _context = context; Scan();
        _label = View.Button(View.Markers, "", Frame); _label.style.position = Position.Absolute;
        if (!_draft.Loaded) await Reload();
        if (context.IsOpen()) { RefreshList(); Inspector(); }
    }
    public void Deactivate() { _label = null; _labelDoor = null; }
    public void BuildActions(VisualElement parent)
    {
        View.Button(parent, LocalizedText.Get("SkillsExtended.DoorEditorTool.SelectDoor"), () => _context.Status(LocalizedText.Get("SkillsExtended.DoorEditorTool.ClickADoorOrItsReaderInTheScene")));
        View.Button(parent, LocalizedText.Get("SkillsExtended.DoorEditorTool.Rescan"), () => { if (!Busy) { Scan(); RefreshList(); Inspector(); } });
        View.Separator(parent);
        View.Button(parent, LocalizedText.Get("SkillsExtended.DoorEditorTool.ClearOverrides"), () => Edit(r =>
        { r.HackingDifficulties.Remove(_selected); r.ExcludedHackingDoors.RemoveAll(id => id == _selected); r.LockLevels.Remove(_selected); }));
        View.Separator(parent);
        View.Button(parent, LocalizedText.Get("SkillsExtended.DoorEditorTool.Undo"), () => Undo(false)); View.Button(parent, LocalizedText.Get("SkillsExtended.DoorEditorTool.Redo"), () => Undo(true));
        View.Separator(parent);
        View.Button(parent, LocalizedText.Get("SkillsExtended.DoorEditorTool.Save"), () => Run(Save));
        View.Button(parent, LocalizedText.Get("SkillsExtended.DoorEditorTool.Reload"), () =>
        {
            if (Busy) return;
            if (Dirty) View.Confirm(LocalizedText.Get("SkillsExtended.DoorEditorTool.DiscardThisDoorDraftAndReloadTheServerConfiguration"), () => Run(Reload));
            else Run(Reload);
        });
    }
    private void Scan()
    {
        _doors.Clear();
        foreach (var door in LocationScene.GetAllObjectsAndWhenISayAllIActuallyMeanIt<WorldInteractiveObject>())
            if (door && door is Door && !string.IsNullOrEmpty(door.Id) && !door.Id.StartsWith(SignalsIds.Prefix))
                _doors[door.Id] = door;
    }
    private void Select(string id) { if (Busy) return; _selected = id; Inspector(); RefreshList(); }
    public void RefreshList()
    {
        if (_context == null) return;
        View.List.Clear(); View.BrowserControls.Clear(); var search = View.Search.value ?? "";
        View.Choice(View.BrowserControls, LocalizedText.Get("SkillsExtended.DoorEditorTool.Show"), new[] { LocalizedText.Get("SkillsExtended.DoorEditorTool.AllDoors"), LocalizedText.Get("SkillsExtended.DoorEditorTool.EditableDoors"), LocalizedText.Get("SkillsExtended.DoorEditorTool.ConfiguredDoors") }, _filter,
            index => { _filter = index; RefreshList(); });
        // Stable within the loaded scene, even when filtering or sorting by display name.
        var numbers = _doors.Keys.OrderBy(id => id, StringComparer.Ordinal).Select((id, i) => (id, number: i + 1))
            .ToDictionary(p => p.id, p => p.number);
        var shown = 0;
        foreach (var pair in _doors.OrderBy(p => DisplayName(p.Value), StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Key))
        {
            var door = pair.Value; if (!door) continue;
            var keyName = KeyName(door);
            var editable = door is KeycardDoor reader ? ElectronicsDoorRegistry.Supports(reader)
                : LockPickingHelpers.Supported(door) && _draft.Metadata?.LockMapSupported == true;
            if (_filter == 1 && !editable || _filter == 2 && !Configured(pair.Key)) continue;
            var kind = door is KeycardDoor ? LocalizedText.Get("SkillsExtended.DoorEditorTool.Electronic") : LockPickingHelpers.Supported(door) ? LocalizedText.Get("SkillsExtended.DoorEditorTool.Mechanical")
                : string.IsNullOrEmpty(door.KeyId) ? LocalizedText.Get("SkillsExtended.DoorEditorTool.Keyless") : LocalizedText.Get("SkillsExtended.DoorEditorTool.UnknownKey");
            var label = DisplayName(door);
            var identity = "#" + numbers[pair.Key];
            if ((label + " " + door.name + " " + pair.Key + " " + keyName + " " + kind + " " + identity).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            var id = pair.Key; var position = door.transform.position;
            var location = LocalizedText.Get("SkillsExtended.DoorEditorTool.XZ", position.x, position.z);
            View.Row(label, identity + " · " + kind + " · " + location + (Configured(id) ? LocalizedText.Get("SkillsExtended.DoorEditorTool.Configured") : ""), () => Select(id), id == _selected,
                LocalizedText.Get("SkillsExtended.DoorEditorTool.DoorIdSceneKeyPosition", id, door.name, keyName, position));
            shown++;
        }
        var unloaded = 0;
        if (_draft.Loaded)
        {
            var missing = _draft.Rules.LockLevels.Keys.Concat(_draft.Rules.HackingDifficulties.Keys)
                .Concat(_draft.Rules.ExcludedHackingDoors).Distinct().Where(id => !_doors.ContainsKey(id));
            foreach (var id in missing.OrderBy(id => id))
            {
                unloaded++;
                if (_filter == 1 || id.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                View.Row(id, LocalizedText.Get("SkillsExtended.DoorEditorTool.SavedRuleDoorNotLoaded"), () => Select(id), id == _selected,
                    LocalizedText.Get("SkillsExtended.DoorEditorTool.ThisSavedRuleIsPreservedUntilExplicitlyCleared")); shown++;
            }
        }
        View.BrowserTitle(LocalizedText.Get("SkillsExtended.DoorEditorTool.Doors"), shown, _doors.Count + unloaded);
        if (shown == 0) Text(View.List, LocalizedText.Get("SkillsExtended.DoorEditorTool.NoDoorsMatchThisSearchAndFilter"));
    }
    private bool Configured(string id) => _draft.Loaded && (_draft.Rules.LockLevels.ContainsKey(id)
        || _draft.Rules.HackingDifficulties.ContainsKey(id) || _draft.Rules.ExcludedHackingDoors.Contains(id));
    private static string KeyName(WorldInteractiveObject door)
    {
        if (string.IsNullOrEmpty(door.KeyId)) return LocalizedText.Get("SkillsExtended.DoorEditorTool.NoKey");
        var key = door.KeyId + " Name";
        var name = key.Localized();
        return name == key ? door.KeyId : name;
    }
    private static string DisplayName(WorldInteractiveObject door)
    {
        if (!door) return LocalizedText.Get("SkillsExtended.DoorEditorTool.Door");
        var key = KeyName(door);
        if (!string.IsNullOrEmpty(door.KeyId) && key != door.KeyId) return key;
        var name = Regex.Replace(door.name.Replace('_', ' '), @"\s+", " ").Trim();
        return name.Length == 0 ? LocalizedText.Get("SkillsExtended.DoorEditorTool.Door") : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
    private void Text(VisualElement parent, string text)
    { var label = new Label(text) { enableRichText = false }; label.AddToClassList("editor-label"); parent.Add(label); }
    private void Inspector()
    {
        View.Inspector.Clear(); Text(View.Inspector, LocalizedText.Get("SkillsExtended.DoorEditorTool.DoorProperties"));
        if (!_draft.Loaded) { Text(View.Inspector, LocalizedText.Get("SkillsExtended.DoorEditorTool.DoorRulesAreNotLoadedUseReloadToRetry")); return; }
        if (string.IsNullOrEmpty(_selected)) { Text(View.Inspector, LocalizedText.Get("SkillsExtended.DoorEditorTool.SelectALoadedDoorInTheSceneOrThe")); return; }
        var content = View.Scroll(View.Inspector);
        var door = Selected; var metadata = _draft.Metadata; var r = _draft.Rules;
        Text(content, door ? DisplayName(door) : LocalizedText.Get("SkillsExtended.DoorEditorTool.SavedRuleForAnUnloadedDoor"));
        if (door) Text(content, LocalizedText.Get("SkillsExtended.DoorEditorTool.State", LocalizedText.Get("SkillsExtended.DoorState." + door.DoorState), (door.Operatable ? LocalizedText.Get("SkillsExtended.DoorEditorTool.Operatable") : LocalizedText.Get("SkillsExtended.DoorEditorTool.NotOperatable"))));
        var electronic = door && door is KeycardDoor && ElectronicsDoorRegistry.Supports((KeycardDoor)door);
        var mechanical = door && LockPickingHelpers.Supported(door);
        if (electronic || !door || r.HackingDifficulties.ContainsKey(_selected) || r.ExcludedHackingDoors.Contains(_selected))
        {
            var hacking = new Foldout { text = LocalizedText.Get("SkillsExtended.DoorEditorTool.Hacking"), value = true }; hacking.AddToClassList("editor-inspector-section"); content.Add(hacking);
            if (!metadata.Hacking.Enabled) Text(hacking, LocalizedText.Get("SkillsExtended.DoorEditorTool.HackingIsDisabledGlobally"));
            if (door && !electronic) Text(hacking, LocalizedText.Get("SkillsExtended.DoorEditorTool.ThisDoorDoesNotSupportHackingClearOverridesTo"));
            var exclude = new Toggle(LocalizedText.Get("SkillsExtended.DoorEditorTool.ExcludeThisDoor")) { value = r.ExcludedHackingDoors.Contains(_selected) };
            exclude.AddToClassList("editor-setting");
            exclude.SetEnabled(!door || electronic);
            exclude.RegisterValueChangedCallback(evt => Edit(x =>
            { x.ExcludedHackingDoors.RemoveAll(id => id == _selected); if (evt.newValue) x.ExcludedHackingDoors.Add(_selected); })); hacking.Add(exclude);
            var difficulty = View.Choice(hacking, LocalizedText.Get("SkillsExtended.DoorEditorTool.Difficulty"), new[] { LocalizedText.Get("SkillsExtended.DoorEditorTool.Inherit"), LocalizedText.Get("SkillsExtended.DoorEditorTool.1Standard"), LocalizedText.Get("SkillsExtended.DoorEditorTool.2Secure"), LocalizedText.Get("SkillsExtended.DoorEditorTool.3Hardened") },
                r.HackingDifficulties.TryGetValue(_selected, out var d) ? d : 0,
                index => Edit(x => { if (index == 0) x.HackingDifficulties.Remove(_selected); else x.HackingDifficulties[_selected] = index; }));
            difficulty.tooltip = LocalizedText.Get("SkillsExtended.DoorEditorTool.InheritUsesTheKeycardDifficultyOrTheGlobalDefault");
            difficulty.SetEnabled(!door || electronic);
            if (electronic)
            {
                if (metadata.Hacking.ExcludedKeycards.Contains(door.KeyId ?? ""))
                    Text(hacking, LocalizedText.Get("SkillsExtended.DoorEditorTool.ThisKeyIsGloballyExcludedDoorOverridesCannotEnable"));
                else if (r.ExcludedHackingDoors.Contains(_selected)) Text(hacking, LocalizedText.Get("SkillsExtended.DoorEditorTool.ExcludedFromHacking"));
                else Text(hacking, LocalizedText.Get("SkillsExtended.DoorEditorTool.EffectiveDifficulty", (r.HackingDifficulties.TryGetValue(_selected, out d) ? d
                    : metadata.Hacking.KeycardDifficulties.TryGetValue(door.KeyId ?? "", out d) ? d : metadata.Hacking.DefaultDifficulty)));
            }
        }
        if (mechanical || !door || r.LockLevels.ContainsKey(_selected))
        {
            var picking = new Foldout { text = LocalizedText.Get("SkillsExtended.DoorEditorTool.LockPicking"), value = true }; picking.AddToClassList("editor-inspector-section"); content.Add(picking);
            if (!metadata.LockPickingEnabled) Text(picking, LocalizedText.Get("SkillsExtended.DoorEditorTool.LockPickingIsDisabledGlobally"));
            if (!metadata.LockMapSupported) Text(picking, LocalizedText.Get("SkillsExtended.DoorEditorTool.ThisMapHasNoLockPickingTable"));
            else if (door && !mechanical) Text(picking, LocalizedText.Get("SkillsExtended.DoorEditorTool.ThisDoorHasNoRecognizedMechanicalKeyLockClear"));
            var levels = new List<string> { LocalizedText.Get("SkillsExtended.DoorEditorTool.Disabled"), LocalizedText.Get("SkillsExtended.DoorEditorTool.Tier1"), LocalizedText.Get("SkillsExtended.DoorEditorTool.Tier2"), LocalizedText.Get("SkillsExtended.DoorEditorTool.Tier3"), LocalizedText.Get("SkillsExtended.DoorEditorTool.Tier4"), LocalizedText.Get("SkillsExtended.DoorEditorTool.Tier5") };
            var hasLevel = r.LockLevels.TryGetValue(_selected, out var level);
            var index = hasLevel ? level : 0;
            if (hasLevel && (level < 1 || level > 5))
            {
                index = levels.Count; levels.Add(LocalizedText.Get("SkillsExtended.DoorEditorTool.Legacy", level));
                Text(picking, LocalizedText.Get("SkillsExtended.DoorEditorTool.LegacyLevelUsesEffectiveTier", level, Math.Max(1, Math.Min(5, level))));
            }
            var tier = View.Choice(picking, LocalizedText.Get("SkillsExtended.DoorEditorTool.LockTier"), levels, index, chosen => Edit(x =>
            { if (chosen == 0) x.LockLevels.Remove(_selected); else if (chosen <= 5) x.LockLevels[_selected] = chosen; }));
            tier.tooltip = LocalizedText.Get("SkillsExtended.DoorEditorTool.DisabledRemovesThisDoorFromTheCurrentMapS");
            tier.SetEnabled(metadata.LockMapSupported && (!door || mechanical));
            if (hasLevel && metadata.MissingXpLevels.Contains(level)) Text(picking, LocalizedText.Get("SkillsExtended.DoorEditorTool.NoXpEntryExistsForThisTierAddIt"));
        }
        if (door && !electronic && !mechanical && !Configured(_selected))
            Text(content, LocalizedText.Get("SkillsExtended.DoorEditorTool.NoSupportedHackingReaderOrMechanicalKeyLockSelect"));
        Text(content, LocalizedText.Get("SkillsExtended.DoorEditorTool.DoorRuleChangesRequireAGameClientRestartAfter"));
        var details = new Foldout { text = LocalizedText.Get("SkillsExtended.DoorEditorTool.Details"), value = false }; details.AddToClassList("editor-inspector-section"); content.Add(details);
        Text(details, LocalizedText.Get("SkillsExtended.DoorEditorTool.Map", r.Map)); Text(details, LocalizedText.Get("SkillsExtended.DoorEditorTool.DoorId", _selected));
        if (door) { Text(details, LocalizedText.Get("SkillsExtended.DoorEditorTool.Key", KeyName(door))); Text(details, LocalizedText.Get("SkillsExtended.DoorEditorTool.KeyId", door.KeyId)); Text(details, LocalizedText.Get("SkillsExtended.DoorEditorTool.SceneName", door.name)); Text(details, LocalizedText.Get("SkillsExtended.DoorEditorTool.Position", door.transform.position)); }
        if (SignalsMaps.Normalize(_context.World.LocationId).StartsWith("factory")) Text(details, LocalizedText.Get("SkillsExtended.DoorEditorTool.FactoryDayAndNightShareThisLockTable"));
        if (SignalsMaps.Normalize(_context.World.LocationId).StartsWith("sandbox")) Text(details, LocalizedText.Get("SkillsExtended.DoorEditorTool.BothGroundZeroVariantsShareThisLockTable"));
        Text(details, LocalizedText.Get("SkillsExtended.DoorEditorTool.ThisToolDoesNotUnlockMoveOrReplaceDoors"));
    }
    private void Edit(Action<DoorAuthoringRequest> action)
    {
        if (Busy || !_draft.Loaded || string.IsNullOrEmpty(_selected)) return;
        _draft.Edit(action); RefreshList();
        // Delay inspector rebuilding until callbacks have finished dispatching.
        View.Inspector.schedule.Execute(() => { if (_context.IsOpen() && _label != null) Inspector(); });
        _context.Status(LocalizedText.Get("SkillsExtended.DoorEditorTool.DoorDraftChangedSaveWritesTheCurrentMapS"));
    }
    public void Undo(bool redo) { if (Busy) return; _draft.Undo(redo); RefreshList(); Inspector(); }
    private async void Run(Func<Task> operation)
    {
        if (Busy) return;
        try { await operation(); }
        catch (Exception e) { SkillsExtendedPlugin.Log.LogError(e); _context.Status(LocalizedText.Get("SkillsExtended.DoorEditorTool.DoorOperationFailedDraftRetained", e.Message)); }
    }
    public async Task Reload()
    {
        Busy = true;
        try
        {
            var reply = await SkillsDeveloperEditor.Post<DoorAuthoringReply>("/skills-extended/editor/doors/read",
                new DoorAuthoringRequest { Map = _context.World.LocationId, Raid = _context.Raid });
            if (!_context.World) return;
            if (reply.Success) { _draft.Load(reply); if (_context.IsOpen()) { RefreshList(); Inspector(); } }
            _context.Status(reply.Message);
        }
        finally { Busy = false; }
    }
    public async Task Save()
    {
        if (!_draft.Loaded) { _context.Status(LocalizedText.Get("SkillsExtended.DoorEditorTool.LoadDoorRulesBeforeSaving")); return; }
        Busy = true;
        try
        {
            _context.Cancellation().ThrowIfCancellationRequested();
            var reply = await SkillsDeveloperEditor.Post<DoorAuthoringReply>("/skills-extended/editor/doors/save", DoorEditorDraft.Copy(_draft.Rules));
            if (!_context.World) return;
            if (reply.Success) { _draft.Load(reply); if (_context.IsOpen()) { RefreshList(); Inspector(); } }
            _context.Status(reply.Message);
        }
        finally { Busy = false; }
    }
    public void Tick()
    {
        if (_label == null || View.Root.panel == null) return;
        var door = Selected;
        if (!door) { _label.style.display = DisplayStyle.None; _labelDoor = null; return; }
        var position = EditorSceneHandles.Project(_context.Camera(), View.Root, door.transform.position + Vector3.up, out var visible);
        if (_labelDoor != door || _labelSceneName != door.name || _labelKey != door.KeyId)
        {
            _labelDoor = door; _labelSceneName = door.name; _labelKey = door.KeyId;
            _label.text = LocalizedText.Get("SkillsExtended.DoorEditorTool.Selected", DisplayName(door));
        }
        _label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        _label.style.left = position.x; _label.style.top = position.y;
    }
    public void WorldInput(Ray ray)
    {
        if (Busy || !Input.GetMouseButtonDown(0) || View.PointerOver) return;
        var hit = Physics.RaycastAll(ray, 3000, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).FirstOrDefault();
        if (!hit.collider) return;
        var door = hit.collider.GetComponentInParent<WorldInteractiveObject>();
        if (door && _doors.ContainsKey(door.Id)) Select(door.Id);
        else _context.Status(LocalizedText.Get("SkillsExtended.DoorEditorTool.NoLoadedDoorUnderThePointerUseTheSearchable"));
    }
    public bool Cancel() => false;
    public void Frame() { if (Selected) _context.Frame(Selected.transform.position + Vector3.up); }
    public void Dispose() { _doors.Clear(); Deactivate(); }
}
