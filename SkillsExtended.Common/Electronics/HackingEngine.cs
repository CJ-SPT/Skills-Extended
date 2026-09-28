using System;
using System.Collections.Generic;
using System.Linq;
using SkillsExtended.Config.Skills;

namespace SkillsExtended.Hacking;

public static class HackingIds
{
    public const byte Skill = 200;
    public const string Pda = "662400eb756ca8948fe64fe8";
    public const int CoherenceBuff = 1028;
    public const int StrengthBuff = 1029;
    public const int SlotsBuff = 1030;
}

public enum NodeKind
{
    Empty,
    Core,
    Firewall,
    Antivirus,
    Restoration,
    Suppressor,
    SelfRepair,
    KernelRot,
    Shield,
    Vector,
    Cache,
}

public enum HackStatus
{
    Active,
    Won,
    Lost,
    Aborted,
    Cancelled,
}

public enum HackCommand
{
    SelectNode,
    UseUtility,
    Abort,
}

public class HackNode
{
    public int Id { get; set; }
    public int Q { get; set; }
    public int R { get; set; }
    public NodeKind Kind { get; set; }
    public NodeKind CacheContent { get; set; }
    public bool Revealed { get; set; }
    public bool Cleared { get; set; }
    public int Coherence { get; set; }
    public int MaximumCoherence { get; set; }
    public int Strength { get; set; }
    public int Clue { get; set; }
    public int VectorTurns { get; set; }
    public List<int> Neighbors { get; set; } = new();
    public bool IsDefense => Kind >= NodeKind.Firewall && Kind <= NodeKind.Suppressor;
    public bool IsUtility => Kind >= NodeKind.SelfRepair && Kind <= NodeKind.Vector;
}

/// <summary>Serializable deterministic state. Never depends on Unity time or System.Random implementation.</summary>
public class HackBoard
{
    public List<HackNode> Nodes { get; set; } = new();
    public List<NodeKind> Utilities { get; set; } = new();
    public int Slots { get; set; }
    public int Coherence { get; set; }
    public int MaximumCoherence { get; set; }
    public int BaseStrength { get; set; }
    public int RepairTurns { get; set; }
    public int ShieldCharges { get; set; }
    public int Turn { get; set; }
    public int Explored { get; set; }
    public uint RandomState { get; set; }
    public HackStatus Status { get; set; }
    public int Difficulty { get; set; }
    public int Strength =>
        Math.Max(
            5,
            BaseStrength
                - Nodes.Count(n => n.Revealed && !n.Cleared && n.Kind == NodeKind.Suppressor) * 5
        );

    public int Next(int max)
    {
        var x = RandomState == 0 ? 0x9E3779B9u : RandomState;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        RandomState = x;
        return (int)(x % (uint)max);
    }

    public bool CanSelect(int id)
    {
        if (Status != HackStatus.Active || id < 0 || id >= Nodes.Count)
        {
            return false;
        }

        var n = Nodes[id];
        if (n.Cleared || !n.Neighbors.Any(i => Nodes[i].Revealed && Nodes[i].Cleared))
        {
            return false;
        }

        // Defenses remain attackable; otherwise neighboring defenders could deadlock each other.
        if (n.Revealed && n.IsDefense)
        {
            return true;
        }

        return !n.Neighbors.Any(i => Nodes[i].Revealed && Nodes[i].IsDefense && !Nodes[i].Cleared);
    }

    public bool SelectNode(int id)
    {
        if (!CanSelect(id))
        {
            return false;
        }

        var n = Nodes[id];
        if (!n.Revealed)
        {
            n.Revealed = true;
            Explored++;
            if (n.Kind == NodeKind.Empty)
            {
                n.Cleared = true;
                n.Clue = DistanceClue(n);
            }
        }
        else if (n.Kind == NodeKind.Cache)
        {
            n.Kind = n.CacheContent;
            HackingEngine.SetStats(n);
            RefreshDistanceClues();
        }
        else if (n.IsUtility)
        {
            if (Utilities.Count >= Slots)
            {
                return false;
            }

            Utilities.Add(n.Kind);
            n.Cleared = true;
            RefreshDistanceClues();
        }
        else if (n.IsDefense || n.Kind == NodeKind.Core)
        {
            Damage(n, Strength);
            if (Status == HackStatus.Won)
            {
                Turn++;
                return true;
            }

            if (!n.Cleared && n.Strength > 0)
            {
                if (ShieldCharges > 0)
                {
                    ShieldCharges--;
                }
                else
                {
                    Coherence = Math.Max(0, Coherence - n.Strength);
                }

                if (Coherence == 0)
                {
                    Status = HackStatus.Lost;
                    Turn++;
                    return true;
                }
            }
        }
        else
        {
            return false;
        }

        EndTurn();
        return true;
    }

    public bool UseUtility(int slot, int target = -1)
    {
        if (Status != HackStatus.Active || slot < 0 || slot >= Utilities.Count)
        {
            return false;
        }

        var kind = Utilities[slot];
        HackNode n = null;
        if (kind == NodeKind.KernelRot || kind == NodeKind.Vector)
        {
            if (!CanSelect(target))
            {
                return false;
            }

            n = Nodes[target];
            if (
                !n.Revealed
                || (!n.IsDefense && n.Kind != NodeKind.Core)
                || (kind == NodeKind.Vector && n.VectorTurns > 0)
            )
            {
                return false;
            }
        }

        if (kind == NodeKind.SelfRepair && RepairTurns > 0)
        {
            return false;
        }

        if (kind == NodeKind.Shield && ShieldCharges > 0)
        {
            return false;
        }

        Utilities.RemoveAt(slot);
        switch (kind)
        {
            case NodeKind.SelfRepair:
                RepairTurns = 3;
                break;
            case NodeKind.Shield:
                ShieldCharges = 2;
                break;
            case NodeKind.KernelRot:
                Damage(n, (n.Coherence + 1) / 2);
                break;
            case NodeKind.Vector:
                n.VectorTurns = 3;
                break;
        }

        EndTurn();
        return true;
    }

    public bool Abort()
    {
        if (Status != HackStatus.Active)
        {
            return false;
        }

        Status = HackStatus.Aborted;
        return true;
    }

    private void Damage(HackNode n, int value)
    {
        n.Coherence = Math.Max(0, n.Coherence - value);
        if (n.Coherence != 0)
        {
            return;
        }

        n.Cleared = true;
        if (n.Kind == NodeKind.Core)
        {
            Status = HackStatus.Won;
        }
    }

    private void EndTurn()
    {
        Turn++;
        if (Status != HackStatus.Active)
        {
            return;
        }

        foreach (var n in Nodes.Where(n => n.VectorTurns > 0 && !n.Cleared))
        {
            n.VectorTurns--;
            Damage(n, 20);
            if (Status == HackStatus.Won)
            {
                return;
            }
        }

        if (RepairTurns > 0)
        {
            RepairTurns--;
            Coherence = Math.Min(MaximumCoherence, Coherence + 8);
        }

        foreach (
            var healer in Nodes.Where(n =>
                n.Revealed && !n.Cleared && n.Kind == NodeKind.Restoration
            )
        )
        {
            var targets = Nodes
                .Where(n =>
                    n != healer
                    && n.Revealed
                    && n.IsDefense
                    && !n.Cleared
                    && n.Coherence < n.MaximumCoherence
                )
                .ToArray();
            if (targets.Length == 0)
            {
                continue;
            }

            var target = targets[Next(targets.Length)];
            target.Coherence = Math.Min(target.MaximumCoherence, target.Coherence + 10);
        }
    }

    private void RefreshDistanceClues()
    {
        // Persistent labels describe the current board, not targets already collected
        // or caches that have become defenses. Clue zero identifies the entrance.
        foreach (
            var node in Nodes.Where(n =>
                n.Revealed && n.Cleared && n.Kind == NodeKind.Empty && n.Clue > 0
            )
        )
        {
            node.Clue = DistanceClue(node);
        }
    }

    private int DistanceClue(HackNode start)
    {
        var queue = new Queue<(int id, int distance)>();
        var seen = new HashSet<int> { start.Id };
        queue.Enqueue((start.Id, 0));
        while (queue.Count != 0)
        {
            var (id, distance) = queue.Dequeue();
            var n = Nodes[id];
            if (
                id != start.Id
                && !n.Cleared
                && (n.Kind == NodeKind.Core || n.IsUtility || n.Kind == NodeKind.Cache)
            )
            {
                return Math.Min(5, distance);
            }

            foreach (var neighbor in n.Neighbors)
            {
                if (seen.Add(neighbor))
                {
                    queue.Enqueue((neighbor, distance + 1));
                }
            }
        }

        return 5;
    }
}

public static class HackingEngine
{
    private static readonly (int q, int r)[] Directions =
    {
        (1, 0),
        (1, -1),
        (0, -1),
        (-1, 0),
        (-1, 1),
        (0, 1),
    };

    public static HackBoard StartAttempt(
        HackingData config,
        int difficulty,
        int level,
        uint seed
    )
    {
        config.Validate();
        var tier = config.Tier(difficulty);
        level = Math.Max(0, Math.Min(51, level));
        var board = new HackBoard
        {
            RandomState = seed,
            Difficulty = difficulty,
            Coherence = config.BaseCoherence + level * config.CoherencePerLevel,
            MaximumCoherence = config.BaseCoherence + level * config.CoherencePerLevel,
            BaseStrength = config.BaseStrength + level / config.LevelsPerStrength,
            Slots = level == 51 ? config.EliteUtilitySlots : config.UtilitySlots,
        };
        var coords = new List<(int q, int r)> { (0, 0) };
        // A broad network fits the PDA's landscape display without crowding defense labels.
        double Radius((int q, int r) p) => Math.Max(Math.Abs(p.q + p.r * .5), Math.Abs(p.r) * 2.3);
        // Grow a compact connected hex lattice. Stable sorting makes seeds portable across runtimes.
        while (coords.Count < tier.Nodes)
        {
            var available = coords
                .SelectMany(p => Directions.Select(d => (q: p.q + d.q, r: p.r + d.r)))
                .Distinct()
                .Where(p => !coords.Contains(p))
                .OrderBy(Radius)
                .ThenBy(p => p.q)
                .ThenBy(p => p.r)
                .ToArray();
            var radius = Radius(available[0]);
            // Occasionally grow the next ring before filling this one, so even 19-node boards are irregular.
            var nextRing = radius + (board.Next(5) == 0 ? 1.5 : .5);
            var ring = available.Where(p => Radius(p) <= nextRing).ToArray();
            coords.Add(ring[board.Next(ring.Length)]);
        }

        for (var i = 0; i < coords.Count; i++)
        {
            board.Nodes.Add(
                new HackNode
                {
                    Id = i,
                    Q = coords[i].q,
                    R = coords[i].r,
                }
            );
        }

        foreach (var n in board.Nodes)
        {
            n.Neighbors = board
                .Nodes.Where(other =>
                    Directions.Any(d => other.Q == n.Q + d.q && other.R == n.R + d.r)
                )
                .Select(other => other.Id)
                .ToList();
        }

        var entry = board
            .Nodes.Where(n => n.Neighbors.Count >= 2)
            .OrderBy(n => n.Q)
            .ThenBy(n => n.R)
            .First();
        entry.Revealed = entry.Cleared = true;
        var distance = new Dictionary<int, int> { [entry.Id] = 0 };
        var queue = new Queue<int>();
        queue.Enqueue(entry.Id);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            foreach (var neighbor in board.Nodes[n].Neighbors)
            {
                if (!distance.ContainsKey(neighbor))
                {
                    distance[neighbor] = distance[n] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        var farthest = board.Nodes.Where(n => distance[n.Id] == distance.Values.Max()).ToArray();
        var core = farthest[board.Next(farthest.Length)];
        core.Kind = NodeKind.Core;
        core.Coherence = core.MaximumCoherence = tier.CoreCoherence;
        core.Strength = tier.CoreStrength;
        var positions = board
            .Nodes.Where(n => n != entry && n != core && !entry.Neighbors.Contains(n.Id))
            .ToList();
        HackNode Take()
        {
            var index = board.Next(positions.Count);
            var n = positions[index];
            positions.RemoveAt(index);
            return n;
        }

        for (var i = 0; i < tier.Defenses; i++)
        {
            var n = Take();
            n.Kind = Defense(board);
            SetStats(n);
        }

        for (var i = 0; i < tier.Utilities; i++)
        {
            Take().Kind = Utility(board, false);
        }

        for (var i = 0; i < tier.Caches; i++)
        {
            var n = Take();
            n.Kind = NodeKind.Cache;
            n.CacheContent = board.Next(2) == 0 ? Defense(board) : Utility(board, true);
        }

        return board;
    }

    private static NodeKind Defense(HackBoard b) =>
        (NodeKind)((int)NodeKind.Firewall + b.Next(b.Difficulty + 1));

    private static NodeKind Utility(HackBoard b, bool cache) =>
        b.Difficulty == 1
            ? (cache && b.Next(2) == 0 ? NodeKind.KernelRot : NodeKind.SelfRepair)
            : (NodeKind)((int)NodeKind.SelfRepair + b.Next(b.Difficulty == 2 ? 3 : 4));

    public static void SetStats(HackNode n)
    {
        (n.MaximumCoherence, n.Strength) = n.Kind switch
        {
            NodeKind.Firewall => (60, 10),
            NodeKind.Antivirus => (30, 20),
            NodeKind.Restoration => (50, 0),
            NodeKind.Suppressor => (40, 0),
            _ => (0, 0),
        };
        n.Coherence = n.MaximumCoherence;
    }
}
