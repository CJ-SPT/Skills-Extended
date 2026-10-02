using System;
using System.Collections.Generic;
using System.Linq;

namespace SkillsExtended.DeveloperTools;

public sealed class DoorEditorDraft
{
    private DoorAuthoringRequest _rules = new(), _baseline = new();
    private readonly List<DoorAuthoringRequest> _undo = new(), _redo = new();
    public DoorAuthoringReply Metadata { get; private set; }
    public DoorAuthoringRequest Rules => _rules;
    public bool Dirty { get; private set; }
    public bool Loaded => Metadata != null;
    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;
    public static DoorAuthoringRequest Copy(DoorAuthoringRequest r) => new()
    {
        Map = r.Map, Raid = r.Raid, Revision = r.Revision,
        HackingDifficulties = new(r.HackingDifficulties), ExcludedHackingDoors = new(r.ExcludedHackingDoors),
        LockLevels = new(r.LockLevels),
    };
    private static bool Equal(DoorAuthoringRequest a, DoorAuthoringRequest b) =>
        Same(a.HackingDifficulties, b.HackingDifficulties) && Same(a.LockLevels, b.LockLevels)
        && new HashSet<string>(a.ExcludedHackingDoors).SetEquals(b.ExcludedHackingDoors);
    private static bool Same(Dictionary<string, int> a, Dictionary<string, int> b) =>
        a.Count == b.Count && a.All(p => b.TryGetValue(p.Key, out var value) && p.Value == value);
    public void Load(DoorAuthoringReply reply)
    {
        if (!reply.Success) throw new ArgumentException(reply.Message);
        Metadata = reply; _rules = Copy(reply.Rules); _baseline = Copy(_rules); _undo.Clear(); _redo.Clear();
        Dirty = false;
    }
    public void Edit(Action<DoorAuthoringRequest> action)
    {
        var next = Copy(_rules); action(next); if (Equal(_rules, next)) return;
        _undo.Add(Copy(_rules)); if (_undo.Count > 100) _undo.RemoveAt(0);
        _rules = next; _redo.Clear(); Dirty = !Equal(_rules, _baseline);
    }
    public void Undo(bool redo)
    {
        var source = redo ? _redo : _undo; var target = redo ? _undo : _redo;
        if (source.Count == 0) return;
        target.Add(Copy(_rules)); _rules = source[source.Count - 1]; source.RemoveAt(source.Count - 1);
        Dirty = !Equal(_rules, _baseline);
    }
}
