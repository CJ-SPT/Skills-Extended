using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFT;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Signals;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SkillsExtended.DeveloperTools;

internal sealed class SignalCacheEditorTool : IDeveloperEditorTool
{
    public string Id => "signals";
    public string Title => "Signal caches";
    public bool Supports(string map) => SignalsMaps.IsSupported(map);
    public bool Busy { get; private set; }
    public bool Dirty => _draft.Dirty;
    private readonly SignalEditorDraft _draft = new();
    private readonly Dictionary<string, (GameObject Case, Button Label)> _previews = new();
    private DeveloperEditorContext _context;
    private GameObject _root, _resolved;
    private Button _resolvedLabel;
    private Label _validationLabel;
    private EditorSceneHandles _handles;
    private string _selected = "", _mode = "select";
    private int _presented = -1, _axis = -1;
    private SignalPlacement _drag;
    private float _dragStart;
    private Vector2 _dragMouse;
    private Vector2 _dragPivot;
    private Vector3 _dragOrigin, _dragAxis;
    private SignalPlacement Selected
    {
        get
        {
            for (var i = 0; i < _draft.Points.Count; i++)
                if (_draft.Points[i].Id == _selected) return _draft.Points[i];
            return null;
        }
    }
    private DeveloperEditorView View => _context.View;

    public async Task Activate(DeveloperEditorContext context)
    {
        _context = context;
        _handles = new EditorSceneHandles(); View.Markers.Add(_handles);
        if (string.IsNullOrEmpty(_draft.Raid)) await Reload();
        if (!context.IsOpen()) return;
        _presented = -1; Refresh();
    }
    public void Deactivate()
    {
        Cancel(); if (_root) _root.SetActive(false);
        _previews.Clear(); if (_root) Object.Destroy(_root); _root = null;
        _resolved = null; _resolvedLabel = null; _handles = null; _presented = -1;
    }
    public void BuildActions(VisualElement parent)
    {
        foreach (var mode in new[] { "add", "select", "move", "rotate" })
        {
            var captured = mode;
            View.Button(parent, char.ToUpper(mode[0]) + mode.Substring(1), () =>
            { if (Busy) return; Cancel(); _mode = captured; _context.Status("Cache tool: " + captured + ". Click the scene; changes are drafts until Save."); });
        }
        View.Separator(parent);
        View.Button(parent, "Duplicate", Duplicate);
        View.Button(parent, "Delete", Delete);
        View.Separator(parent);
        View.Button(parent, "Undo", () => Undo(false));
        View.Button(parent, "Redo", () => Undo(true));
        View.Separator(parent);
        View.Button(parent, "Validate", () => Run(Validate));
        View.Button(parent, "Save", () => Run(Save));
        View.Button(parent, "Reload", RequestReload);
    }
    private async void Run(Func<Task> operation)
    {
        try { if (!Busy) await operation(); }
        catch (OperationCanceledException) { }
        catch (Exception e) { SkillsExtendedPlugin.Log.LogError(e); _context.Status("Cache operation failed; draft retained. " + e.Message); }
    }
    private void RequestReload()
    {
        if (Busy) return;
        if (Dirty) View.Confirm("Discard this cache draft and reload the server configuration?", () => Run(Reload));
        else Run(Reload);
    }
    public async Task Reload()
    {
        Busy = true;
        try
        {
            var response = await SkillsDeveloperEditor.Post<SignalAuthoringReply>("/skills-extended/signals/editor/read",
                new SignalAuthoringRequest { Map = _context.World.LocationId, Raid = _context.Raid });
            if (!_context.World) return;
            if (!response.Success) { _context.Status(response.Message); return; }
            _draft.Load(response); _selected = _draft.Points.FirstOrDefault()?.Id ?? "";
            if (_context.IsOpen()) Refresh(); _context.Status(response.Message);
        }
        finally { Busy = false; }
    }
    private async Task<bool> CheckAll()
    {
        var version = _draft.Version;
        var token = _context.Cancellation();
        foreach (var p in _draft.Points.Where(p => p.Enabled).ToArray())
        {
            token.ThrowIfCancellationRequested();
            _context.Status("Checking " + p.Name + "…");
            var report = await SignalsPlacement.Resolve(_context.Runner, new[] { SignalPlacementSearch.Copy(p) }, 0, token);
            if (!_draft.Record(p.Id, version, report)) return false;
        }
        if (_context.IsOpen()) { Reconcile(); Inspector(); }
        return version == _draft.Version && _draft.Valid;
    }
    public async Task Validate()
    {
        Busy = true;
        try
        {
            var valid = await CheckAll();
            _context.Status(valid ? "Placements accepted. Radius zero uses the exact position and yaw; areas passed placement checks." : "Some areas failed. Select a marker to see the rejection reasons.");
        }
        finally { Busy = false; }
    }
    public async Task Save()
    {
        if (View.HasInvalid) { _context.Status("Correct the highlighted numeric fields before saving."); return; }
        if (string.IsNullOrEmpty(_draft.Raid)) { _context.Status("Load placements before saving."); return; }
        Busy = true;
        try
        {
            if (!await CheckAll()) { _context.Status("Save blocked: enabled caches must pass placement checks."); return; }
            _context.Cancellation().ThrowIfCancellationRequested();
            var request = _draft.Request();
            var response = await SkillsDeveloperEditor.Post<SignalAuthoringReply>("/skills-extended/signals/editor/save", request);
            if (!_context.World) return;
            if (response.Success) { _draft.Load(response); if (_context.IsOpen()) Refresh(); }
            _context.Status(response.Message);
        }
        finally { Busy = false; }
    }
    public void Undo(bool redo) { if (Busy || _drag != null) return; _draft.Undo(redo); Refresh(); }
    private void Change(Action<SignalPlacement> change)
    {
        if (Busy || Selected == null) return;
        _draft.Edit(points => change(points.First(p => p.Id == _selected))); Refresh(false);
    }
    private void Select(string id) { if (Busy) return; Cancel(); _selected = id; Inspector(); RefreshList(); }
    private void Add(Vector3 point)
    {
        if (_draft.Points.Count >= 500) { _context.Status("The placement limit is 500 across all maps."); return; }
        var p = new SignalPlacement
        {
            Id = _draft.Map + "-" + Guid.NewGuid().ToString("N").Substring(0, 8), Map = _draft.Map,
            Name = "Cache " + (_draft.Points.Count + 1),
            Position = SignalPlacementSearch.PreviewRoot(SignalsCase.Point(point), SignalsPlacement.Geometry()),
            Yaw = _context.Camera().transform.eulerAngles.y, Enabled = true, SearchRadius = 0,
        };
        _draft.Edit(points => points.Add(p)); _selected = p.Id; Refresh();
    }
    private void Duplicate()
    {
        if (Busy || Selected == null) return;
        var p = SignalPlacementSearch.Copy(Selected);
        p.Id = _draft.Map + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        p.Name += " copy"; p.Position.X += 1;
        _draft.Edit(points => points.Add(p)); _selected = p.Id; Refresh();
    }
    private void Delete()
    {
        if (Busy || Selected == null) return;
        _draft.Edit(points => points.RemoveAll(p => p.Id == _selected)); _selected = ""; Refresh();
    }
    private void Refresh(bool inspector = true)
    {
        if (!_context.IsOpen()) return;
        Reconcile(); RefreshList(); if (inspector) Inspector();
        else if (_validationLabel != null) _validationLabel.text = "Not validated since the last edit.";
    }
    public void RefreshList()
    {
        if (_context == null) return;
        View.List.Clear(); var search = View.Search.value ?? "";
        foreach (var p in _draft.Points.Where(p => (p.Name + " " + p.Id).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0))
        {
            var id = p.Id;
            View.Row(p.Name, (p.Enabled ? "Enabled" : "Disabled") + " · " + p.Id, () => Select(id), id == _selected, p.Id);
        }
        View.BrowserTitle("Signal caches", View.List.childCount, _draft.Points.Count);
        if (View.List.childCount == 0) View.List.Add(new Label("No caches match this search."));
    }
    private void Inspector()
    {
        View.Inspector.Clear(); _validationLabel = null; var p = Selected;
        View.Inspector.Add(new Label("CACHE PROPERTIES"));
        if (p == null) { View.Inspector.Add(new Label("Select a cache, or choose Add and click a surface.")); return; }
        var scroll = View.Scroll(View.Inspector);
        View.Field(scroll, "Name", p.Name, value => Change(x => x.Name = value));
        var toggle = new Toggle("Enabled") { value = p.Enabled };
        toggle.AddToClassList("editor-setting");
        toggle.RegisterValueChangedCallback(e => Change(x => x.Enabled = e.newValue)); scroll.Add(toggle);
        View.Number(scroll, "X", p.Position.X, -100000, 100000, n => Change(x => x.Position.X = n));
        View.Number(scroll, "Y", p.Position.Y, -100000, 100000, n => Change(x => x.Position.Y = n));
        View.Number(scroll, "Z", p.Position.Z, -100000, 100000, n => Change(x => x.Position.Z = n));
        View.Number(scroll, "Yaw", p.Yaw, -36000, 36000, n => Change(x => x.Yaw = SignalsModel.Wrap(n)));
        View.Number(scroll, "Search radius", p.SearchRadius, 0, 25, n => Change(x => x.SearchRadius = n));
        var help = new Label("0 m fixes the position and yaw. A larger radius allows nearby runtime placement.");
        help.style.whiteSpace = WhiteSpace.Normal; scroll.Add(help);
        var report = _draft.Check(p.Id);
        var result = new Label(report == null ? "Not validated since the last edit." : report.ToString());
        _validationLabel = result;
        result.style.whiteSpace = WhiteSpace.Normal; scroll.Add(result);
        var details = new Foldout { text = "Details", value = false };
        details.AddToClassList("editor-inspector-section");
        details.Add(new Label("Map: " + p.Map)); details.Add(new Label("ID: " + p.Id)); scroll.Add(details);
    }
    private GameObject Visual(string name)
    {
        var root = new GameObject(name); root.SetActive(false); root.transform.SetParent(_root.transform, false);
        root.AddComponent<SignalsPlacementPreview>();
        var clone = SignalsCase.InstantiateVisual(root.transform);
        foreach (var script in clone.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
        foreach (var collider in clone.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        clone.SetActive(true); root.SetActive(true); return root;
    }
    private void Reconcile()
    {
        if (!_context.IsOpen() || _handles == null) return;
        if (!_root) _root = new GameObject("Developer cache previews");
        _root.SetActive(true);
        foreach (var id in _previews.Keys.Where(id => !_draft.Points.Any(p => p.Id == id)).ToArray())
        { Object.Destroy(_previews[id].Case); _previews[id].Label.RemoveFromHierarchy(); _previews.Remove(id); }
        foreach (var p in _draft.Points)
        {
            if (!_previews.TryGetValue(p.Id, out var preview))
            {
                var id = p.Id; var label = View.Button(View.Markers, "", () => Select(id));
                label.style.position = Position.Absolute;
                preview = (Visual("Cache preview " + id), label); _previews.Add(id, preview);
            }
            preview.Case.transform.SetPositionAndRotation(SignalsCase.Vector(p.SearchRadius == 0
                ? p.Position : SignalPlacementSearch.PreviewRoot(p.Position, SignalsPlacement.Geometry())),
                Quaternion.Euler(0, p.Yaw, 0));
            preview.Label.text = p.Name + (p.Enabled ? "" : " [disabled]");
        }
        _presented = _draft.Version;
    }
    public void Tick()
    {
        if (!_context.IsOpen() || _handles == null || View.Root.panel == null) return;
        if (_presented != _draft.Version) Refresh();
        var camera = _context.Camera();
        for (var i = 0; i < _draft.Points.Count; i++)
        {
            var p = _draft.Points[i];
            var preview = _previews[p.Id];
            var pos = EditorSceneHandles.Project(camera, View.Root, preview.Case.transform.position + Vector3.up * .5f, out var visible);
            preview.Label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            preview.Label.style.left = pos.x; preview.Label.style.top = pos.y;
        }
        var selected = _drag ?? Selected;
        _handles.Active = selected != null && (_mode == "move" || _mode == "rotate");
        if (selected != null)
        {
            var origin = _previews[selected.Id].Case.transform.position;
            _handles.Center = EditorSceneHandles.Project(camera, View.Root, origin, out var visible);
            _handles.Active &= visible; _handles.Rotate = _mode == "rotate";
            var length = Mathf.Max(.4f, Vector3.Distance(camera.transform.position, origin) * .08f);
            for (var i = 0; i < 3; i++)
                _handles.Ends[i] = EditorSceneHandles.Project(camera, View.Root,
                    origin + EditorSceneHandles.Axis(i, selected.Yaw) * length, out _);
        }
        _handles.RepaintIfChanged();
        var resolved = selected == null ? null : _draft.Check(selected.Id)?.Placement;
        if (selected != null && selected.SearchRadius > 0 && resolved != null)
        {
            if (!_resolved) _resolved = Visual("Resolved area preview");
            if (_resolvedLabel == null)
            {
                _resolvedLabel = View.Button(View.Markers, "Resolved area preview", () => _context.Frame(_resolved.transform.position));
                _resolvedLabel.style.position = Position.Absolute;
            }
            _resolved.SetActive(true); _resolved.transform.SetPositionAndRotation(SignalsCase.Vector(resolved.Position), Quaternion.Euler(0, resolved.Yaw, 0));
            var pos = EditorSceneHandles.Project(camera, View.Root, _resolved.transform.position + Vector3.up * .8f, out var visible);
            _resolvedLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _resolvedLabel.style.left = pos.x; _resolvedLabel.style.top = pos.y;
        }
        else
        {
            if (_resolved) _resolved.SetActive(false);
            if (_resolvedLabel != null) _resolvedLabel.style.display = DisplayStyle.None;
        }
    }
    public void WorldInput(Ray ray)
    {
        if (Busy || string.IsNullOrEmpty(_draft.Raid)) return;
        if (_drag != null)
        {
            if (Input.GetMouseButtonUp(0))
            {
                var p = _drag; _drag = null; _axis = -1;
                _draft.Edit(points => { var index = points.FindIndex(x => x.Id == p.Id); points[index] = p; }); Refresh(); return;
            }
            if (_axis == 3)
            {
                _drag.Yaw = SignalsModel.Wrap(_drag.Yaw + EditorSceneHandles.RotationDelta(_dragPivot, _dragMouse, View.Pointer));
                _dragMouse = View.Pointer;
            }
            else
            {
                if (!EditorSceneHandles.TryAxisDistance(ray, _dragOrigin, _dragAxis, out var distance)) return;
                _drag.Position = SignalsCase.Point(SignalsCase.Vector(Selected.Position) + _dragAxis * (distance - _dragStart));
            }
            _previews[_drag.Id].Case.transform.SetPositionAndRotation(SignalsCase.Vector(_drag.SearchRadius == 0
                ? _drag.Position : SignalPlacementSearch.PreviewRoot(_drag.Position, SignalsPlacement.Geometry())),
                Quaternion.Euler(0, _drag.Yaw, 0));
            return;
        }
        if (!Input.GetMouseButtonDown(0) || View.PointerOver || View.Confirming) return;
        _axis = _handles.Pick(View.Pointer);
        if (_axis >= 0 && Selected != null)
        {
            _dragOrigin = _previews[Selected.Id].Case.transform.position;
            _dragPivot = EditorSceneHandles.Project(_context.Camera(), View.Root, _dragOrigin, out _);
            if (_axis != 3)
            {
                _dragAxis = EditorSceneHandles.Axis(_axis, Selected.Yaw);
                if (!EditorSceneHandles.TryAxisDistance(ray, _dragOrigin, _dragAxis, out _dragStart))
                { _axis = -1; return; }
            }
            _drag = SignalPlacementSearch.Copy(Selected); _dragMouse = View.Pointer;
            return;
        }
        if (_mode == "add")
        {
            var hit = Physics.RaycastAll(ray, 3000, LayerMask.GetMask("Terrain", "LowPolyCollider", "HighPolyCollider"), QueryTriggerInteraction.Ignore)
                .OrderBy(h => h.distance).FirstOrDefault(h => !h.collider.GetComponentInParent<Player>()
                    && !h.collider.GetComponentInParent<SignalsPlacementPreview>());
            if (hit.collider) Add(hit.point);
            else _context.Status("No terrain or static surface under the pointer.");
        }
        else
        {
            var distance = float.PositiveInfinity; string picked = null;
            var body = SignalsPlacement.Geometry().Body;
            foreach (var p in _draft.Points)
            {
                var preview = _previews[p.Id].Case.transform;
                var inverse = Quaternion.Inverse(preview.rotation);
                var local = new Ray(inverse * (ray.origin - preview.position), inverse * ray.direction);
                var bounds = new Bounds(new Vector3(body.Center.X, body.Center.Y, body.Center.Z),
                    new Vector3(body.Extents.X, body.Extents.Y, body.Extents.Z) * 2);
                if (bounds.IntersectRay(local, out var hitDistance) && hitDistance < distance)
                { picked = p.Id; distance = hitDistance; }
            }
            if (picked != null) Select(picked);
        }
    }
    public bool Cancel()
    {
        if (_drag != null) { _drag = null; _axis = -1; Reconcile(); return true; }
        if (_mode == "add") { _mode = "select"; return true; }
        return false;
    }
    public void Frame() { if (Selected != null) _context.Frame(SignalsCase.Vector(Selected.Position)); }
    public void Dispose() { Deactivate(); }
}
