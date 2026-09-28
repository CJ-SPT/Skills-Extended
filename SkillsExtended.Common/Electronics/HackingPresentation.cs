using System.Collections.Generic;
using System.Linq;

namespace SkillsExtended.Electronics;

public enum NodeMotion
{
    Reveal,
    Destroy,
    Damage,
    Repair,
    Available,
    Blocked,
}

public readonly struct NodeTransition
{
    public readonly int Node;
    public readonly NodeMotion Motion;

    public NodeTransition(int node, NodeMotion motion)
    {
        Node = node;
        Motion = motion;
    }
}

public readonly struct PathTransition
{
    public readonly int From;
    public readonly int To;
    public readonly bool Claimed;

    public PathTransition(int from, int to, bool claimed)
    {
        From = from;
        To = to;
        Claimed = claimed;
    }
}

public sealed class HackingVisualChanges
{
    public List<NodeTransition> Nodes { get; } = new();
    public List<PathTransition> Paths { get; } = new();
    public bool Won { get; set; }
    public bool Lost { get; set; }
}

/// <summary>Copies presentation state so local authority mutation and duplicate network replies
/// cannot erase or replay transitions. This observer never changes the simulation.</summary>
public sealed class HackingPresentation
{
    private sealed class NodeState
    {
        public bool Revealed;
        public bool Cleared;
        public bool Available;
        public int Coherence;
        public NodeKind Kind;
    }

    private NodeState[] _previous;
    private int _turn;
    private HackStatus _status;

    public void Reset() => _previous = null;

    public HackingVisualChanges Observe(HackBoard board)
    {
        var changes = new HackingVisualChanges();
        if (_previous != null && board.Turn <= _turn && board.Status == _status)
        {
            return changes;
        }

        var next = board
            .Nodes.Select(n => new NodeState
            {
                Revealed = n.Revealed,
                Cleared = n.Cleared,
                Available = board.CanSelect(n.Id),
                Coherence = n.Coherence,
                Kind = n.Kind,
            })
            .ToArray();
        if (_previous != null && _previous.Length == next.Length)
        {
            foreach (var n in board.Nodes)
            {
                var old = _previous[n.Id];
                var now = next[n.Id];
                if (!old.Revealed && now.Revealed || old.Kind != now.Kind)
                {
                    changes.Nodes.Add(new(n.Id, NodeMotion.Reveal));
                }
                else if (!old.Cleared && now.Cleared)
                {
                    changes.Nodes.Add(new(n.Id, NodeMotion.Destroy));
                }
                else if (old.Revealed && now.Coherence < old.Coherence)
                {
                    changes.Nodes.Add(new(n.Id, NodeMotion.Damage));
                }
                else if (old.Revealed && now.Coherence > old.Coherence)
                {
                    changes.Nodes.Add(new(n.Id, NodeMotion.Repair));
                }

                if (board.Status == HackStatus.Active)
                {
                    if (!old.Available && now.Available && !now.Revealed)
                    {
                        changes.Nodes.Add(new(n.Id, NodeMotion.Available));
                    }
                    else if (old.Available && !now.Available && !now.Cleared)
                    {
                        changes.Nodes.Add(new(n.Id, NodeMotion.Blocked));
                    }
                }

                foreach (var neighbor in n.Neighbors.Where(id => id > n.Id))
                {
                    var oldOther = _previous[neighbor];
                    var other = next[neighbor];
                    var wasClaimed = old.Cleared && oldOther.Cleared;
                    var claimed = now.Cleared && other.Cleared;
                    bool Open(NodeState a, NodeState b) =>
                        a.Cleared && (b.Cleared || b.Available) || b.Cleared && a.Available;
                    if (
                        (claimed && !wasClaimed)
                        || (
                            board.Status == HackStatus.Active
                            && Open(now, other)
                            && !Open(old, oldOther)
                        )
                    )
                    {
                        // An incoming claimed path starts at the previously cleared node;
                        // a new frontier path travels out of the newly cleared node.
                        var from = claimed
                            ? (old.Cleared ? n.Id : neighbor)
                            : (now.Cleared ? n.Id : neighbor);
                        changes.Paths.Add(new(from, from == n.Id ? neighbor : n.Id, claimed));
                    }
                }
            }

            changes.Won = _status == HackStatus.Active && board.Status == HackStatus.Won;
            changes.Lost = _status == HackStatus.Active && board.Status == HackStatus.Lost;
        }

        _previous = next;
        _turn = board.Turn;
        _status = board.Status;
        return changes;
    }
}
