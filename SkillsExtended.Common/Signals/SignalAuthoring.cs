using System;
using System.Collections.Generic;
using System.Linq;
using SkillsExtended.DeveloperTools;

namespace SkillsExtended.Signals;

public class SignalAuthoringRequest : DeveloperEditorRequest
{
    public List<SignalPlacement> Placements { get; set; } = new();
}

public sealed class SignalAuthoringReply : DeveloperEditorReply
{
    public List<SignalPlacement> Placements { get; set; } = new();
}

// No Unity or transport state: undo and validation always refer to detached drafts.
public sealed class SignalEditorDraft
{
    private List<SignalPlacement> _points = new(), _baseline = new();
    private readonly List<List<SignalPlacement>> _undo = new(), _redo = new();
    private readonly Dictionary<string, SignalPlacementReport> _checks = new();
    public IReadOnlyList<SignalPlacement> Points => _points;
    public string Map { get; private set; } = "";
    public string Raid { get; private set; } = "";
    public string Revision { get; private set; } = "";
    public int Version { get; private set; }
    public bool CanUndo => _undo.Count != 0;
    public bool CanRedo => _redo.Count != 0;
    public bool Dirty => !Equal(_points, _baseline);
    private static List<SignalPlacement> Copy(IEnumerable<SignalPlacement> points) =>
        points.Select(SignalPlacementSearch.Copy).ToList();
    private static bool Equal(List<SignalPlacement> a, List<SignalPlacement> b) =>
        a.Count == b.Count && a.OrderBy(p => p.Id, StringComparer.Ordinal).Zip(
            b.OrderBy(p => p.Id, StringComparer.Ordinal), (p, q) =>
                p.Id == q.Id && p.Map == q.Map && p.Name == q.Name && p.Enabled == q.Enabled
                && p.Position.X == q.Position.X && p.Position.Y == q.Position.Y
                && p.Position.Z == q.Position.Z && p.Yaw == q.Yaw && p.SearchRadius == q.SearchRadius
        ).All(same => same);
    public void Load(SignalAuthoringReply reply)
    {
        if (!reply.Success) throw new ArgumentException(reply.Message);
        Map = reply.Map; Raid = reply.Raid; Revision = reply.Revision;
        _points = Copy(reply.Placements); _baseline = Copy(_points);
        _undo.Clear(); _redo.Clear(); Invalidate();
    }
    public void Edit(Action<List<SignalPlacement>> operation)
    {
        var next = Copy(_points); operation(next);
        if (Equal(_points, next)) return;
        _undo.Add(Copy(_points)); if (_undo.Count > 100) _undo.RemoveAt(0);
        _points = next; _redo.Clear(); Invalidate();
    }
    public void Undo(bool redo = false)
    {
        var source = redo ? _redo : _undo; var target = redo ? _undo : _redo;
        if (source.Count == 0) return;
        target.Add(Copy(_points)); _points = source[source.Count - 1];
        source.RemoveAt(source.Count - 1); Invalidate();
    }
    private void Invalidate() { Version++; _checks.Clear(); }
    public bool Record(string id, int version, SignalPlacementReport report)
    {
        if (version != Version || !_points.Any(p => p.Id == id)) return false;
        _checks[id] = report; return true;
    }
    public SignalPlacementReport Check(string id) => _checks.TryGetValue(id, out var r) ? r : null;
    public bool Valid => _points.Where(p => p.Enabled).All(p => Check(p.Id)?.Placement != null);
    public SignalAuthoringRequest Request() => new()
    {
        Map = Map, Raid = Raid, Revision = Revision, Placements = Copy(_points),
    };
}
