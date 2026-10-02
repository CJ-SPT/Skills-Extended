using System;
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
    public string Title => "Doors";
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
        View.Button(parent, "Select door", () => _context.Status("Click a door or its reader in the scene, or search the loaded-door list."));
        View.Button(parent, "Rescan", () => { if (!Busy) { Scan(); RefreshList(); Inspector(); } });
        View.Separator(parent);
        View.Button(parent, "Clear overrides", () => Edit(r =>
        { r.HackingDifficulties.Remove(_selected); r.ExcludedHackingDoors.RemoveAll(id => id == _selected); r.LockLevels.Remove(_selected); }));
        View.Separator(parent);
        View.Button(parent, "Undo", () => Undo(false)); View.Button(parent, "Redo", () => Undo(true));
        View.Separator(parent);
        View.Button(parent, "Save", () => Run(Save));
        View.Button(parent, "Reload", () =>
        {
            if (Busy) return;
            if (Dirty) View.Confirm("Discard this door draft and reload the server configuration?", () => Run(Reload));
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
        View.Choice(View.BrowserControls, "Show", new[] { "All doors", "Editable doors", "Configured doors" }, _filter,
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
            var kind = door is KeycardDoor ? "Electronic" : LockPickingHelpers.Supported(door) ? "Mechanical"
                : string.IsNullOrEmpty(door.KeyId) ? "Keyless" : "Unknown key";
            var label = DisplayName(door);
            var identity = "#" + numbers[pair.Key];
            if ((label + " " + door.name + " " + pair.Key + " " + keyName + " " + kind + " " + identity).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            var id = pair.Key; var position = door.transform.position;
            var location = string.Format(CultureInfo.InvariantCulture, "X {0:0} · Z {1:0}", position.x, position.z);
            View.Row(label, identity + " · " + kind + " · " + location + (Configured(id) ? " · Configured" : ""), () => Select(id), id == _selected,
                "Door ID: " + id + "\nScene: " + door.name + "\nKey: " + keyName + "\nPosition: " + position);
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
                View.Row(id, "Saved rule · Door not loaded", () => Select(id), id == _selected,
                    "This saved rule is preserved until explicitly cleared."); shown++;
            }
        }
        View.BrowserTitle("Doors", shown, _doors.Count + unloaded);
        if (shown == 0) Text(View.List, "No doors match this search and filter.");
    }
    private bool Configured(string id) => _draft.Loaded && (_draft.Rules.LockLevels.ContainsKey(id)
        || _draft.Rules.HackingDifficulties.ContainsKey(id) || _draft.Rules.ExcludedHackingDoors.Contains(id));
    private static string KeyName(WorldInteractiveObject door) => string.IsNullOrEmpty(door.KeyId) ? "No key"
        : SkillsExtendedPlugin.Keys?.KeyLocale?.TryGetValue(door.KeyId, out var name) == true ? name : door.KeyId;
    private static string DisplayName(WorldInteractiveObject door)
    {
        if (!door) return "Door";
        var key = KeyName(door);
        if (key != "No key" && key != door.KeyId) return key;
        var name = Regex.Replace(door.name.Replace('_', ' '), @"\s+", " ").Trim();
        return name.Length == 0 ? "Door" : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
    private void Text(VisualElement parent, string text)
    { var label = new Label(text) { enableRichText = false }; label.AddToClassList("editor-label"); parent.Add(label); }
    private void Inspector()
    {
        View.Inspector.Clear(); Text(View.Inspector, "DOOR PROPERTIES");
        if (!_draft.Loaded) { Text(View.Inspector, "Door rules are not loaded. Use Reload to retry."); return; }
        if (string.IsNullOrEmpty(_selected)) { Text(View.Inspector, "Select a loaded door in the scene or the list."); return; }
        var content = View.Scroll(View.Inspector);
        var door = Selected; var metadata = _draft.Metadata; var r = _draft.Rules;
        Text(content, door ? DisplayName(door) : "Saved rule for an unloaded door");
        if (door) Text(content, "State: " + door.DoorState + " · " + (door.Operatable ? "Operatable" : "Not operatable"));
        var electronic = door && door is KeycardDoor && ElectronicsDoorRegistry.Supports((KeycardDoor)door);
        var mechanical = door && LockPickingHelpers.Supported(door);
        if (electronic || !door || r.HackingDifficulties.ContainsKey(_selected) || r.ExcludedHackingDoors.Contains(_selected))
        {
            var hacking = new Foldout { text = "Hacking", value = true }; hacking.AddToClassList("editor-inspector-section"); content.Add(hacking);
            if (!metadata.Hacking.Enabled) Text(hacking, "Hacking is disabled globally.");
            if (door && !electronic) Text(hacking, "This door does not support Hacking. Clear overrides to remove its saved rules.");
            var exclude = new Toggle("Exclude this door") { value = r.ExcludedHackingDoors.Contains(_selected) };
            exclude.AddToClassList("editor-setting");
            exclude.SetEnabled(!door || electronic);
            exclude.RegisterValueChangedCallback(evt => Edit(x =>
            { x.ExcludedHackingDoors.RemoveAll(id => id == _selected); if (evt.newValue) x.ExcludedHackingDoors.Add(_selected); })); hacking.Add(exclude);
            var difficulty = View.Choice(hacking, "Difficulty", new[] { "Inherit", "1 · Standard", "2 · Secure", "3 · Hardened" },
                r.HackingDifficulties.TryGetValue(_selected, out var d) ? d : 0,
                index => Edit(x => { if (index == 0) x.HackingDifficulties.Remove(_selected); else x.HackingDifficulties[_selected] = index; }));
            difficulty.tooltip = "Inherit uses the keycard difficulty, or the global default when the key has no rule.";
            difficulty.SetEnabled(!door || electronic);
            if (electronic)
            {
                if (metadata.Hacking.ExcludedKeycards.Contains(door.KeyId ?? ""))
                    Text(hacking, "This key is globally excluded. Door overrides cannot enable it.");
                else if (r.ExcludedHackingDoors.Contains(_selected)) Text(hacking, "Excluded from Hacking.");
                else Text(hacking, "Effective difficulty: " + (r.HackingDifficulties.TryGetValue(_selected, out d) ? d
                    : metadata.Hacking.KeycardDifficulties.TryGetValue(door.KeyId ?? "", out d) ? d : metadata.Hacking.DefaultDifficulty));
            }
        }
        if (mechanical || !door || r.LockLevels.ContainsKey(_selected))
        {
            var picking = new Foldout { text = "Lock Picking", value = true }; picking.AddToClassList("editor-inspector-section"); content.Add(picking);
            if (!metadata.LockPickingEnabled) Text(picking, "Lock Picking is disabled globally.");
            if (!metadata.LockMapSupported) Text(picking, "This map has no Lock Picking table.");
            else if (door && !mechanical) Text(picking, "This door has no recognized mechanical key lock. Clear overrides to remove its saved tier.");
            var levels = new List<string> { "Disabled", "Tier 1", "Tier 2", "Tier 3", "Tier 4", "Tier 5" };
            var hasLevel = r.LockLevels.TryGetValue(_selected, out var level);
            var index = hasLevel ? level : 0;
            if (hasLevel && (level < 1 || level > 5))
            {
                index = levels.Count; levels.Add(level + " · Legacy");
                Text(picking, "Legacy level " + level + " uses effective tier " + Math.Max(1, Math.Min(5, level)) + ".");
            }
            var tier = View.Choice(picking, "Lock tier", levels, index, chosen => Edit(x =>
            { if (chosen == 0) x.LockLevels.Remove(_selected); else if (chosen <= 5) x.LockLevels[_selected] = chosen; }));
            tier.tooltip = "Disabled removes this door from the current map's lock table.";
            tier.SetEnabled(metadata.LockMapSupported && (!door || mechanical));
            if (hasLevel && metadata.MissingXpLevels.Contains(level)) Text(picking, "No XP entry exists for this tier. Add it in the web editor.");
        }
        if (door && !electronic && !mechanical && !Configured(_selected))
            Text(content, "No supported Hacking reader or mechanical key lock. Select another door, or use the Editable doors filter.");
        Text(content, "Door rule changes require a game client restart after saving.");
        var details = new Foldout { text = "Details", value = false }; details.AddToClassList("editor-inspector-section"); content.Add(details);
        Text(details, "Map: " + r.Map); Text(details, "Door ID: " + _selected);
        if (door) { Text(details, "Key: " + KeyName(door)); Text(details, "Key ID: " + door.KeyId); Text(details, "Scene name: " + door.name); Text(details, "Position: " + door.transform.position); }
        if (SignalsMaps.Normalize(_context.World.LocationId).StartsWith("factory")) Text(details, "Factory day and night share this lock table.");
        if (SignalsMaps.Normalize(_context.World.LocationId).StartsWith("sandbox")) Text(details, "Both Ground Zero variants share this lock table.");
        Text(details, "This tool does not unlock, move, or replace doors.");
    }
    private void Edit(Action<DoorAuthoringRequest> action)
    {
        if (Busy || !_draft.Loaded || string.IsNullOrEmpty(_selected)) return;
        _draft.Edit(action); RefreshList();
        // Delay inspector rebuilding until callbacks have finished dispatching.
        View.Inspector.schedule.Execute(() => { if (_context.IsOpen() && _label != null) Inspector(); });
        _context.Status("Door draft changed. Save writes the current map's rules; no doors are changed in this raid.");
    }
    public void Undo(bool redo) { if (Busy) return; _draft.Undo(redo); RefreshList(); Inspector(); }
    private async void Run(Func<Task> operation)
    {
        if (Busy) return;
        try { await operation(); }
        catch (Exception e) { SkillsExtendedPlugin.Log.LogError(e); _context.Status("Door operation failed; draft retained. " + e.Message); }
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
        if (!_draft.Loaded) { _context.Status("Load door rules before saving."); return; }
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
            _label.text = DisplayName(door) + " [selected]";
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
        else _context.Status("No loaded door under the pointer. Use the searchable door list if its collider is hidden.");
    }
    public bool Cancel() => false;
    public void Frame() { if (Selected) _context.Frame(Selected.transform.position + Vector3.up); }
    public void Dispose() { _doors.Clear(); Deactivate(); }
}
