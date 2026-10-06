using System;
using System.Collections.Generic;
using System.Linq;
using SkillsExtended.Config.Skills;

namespace SkillsExtended.LockPicking;

public static class PickingProtocol
{
    public const int Version = 7;
    public const string UpdateMessage = "SkillsExtended.PickingAuthority.LockPickingVersionsDifferUpdateSkillsExtendedCoreAnd";
}

public sealed class PickRequest
{
    public int ProtocolVersion { get; set; }
    public string Raid { get; set; }
    public string Actor { get; set; }
    public string Door { get; set; }
    public string Tool { get; set; }
    public string Attempt { get; set; }
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public string Operation { get; set; } = "sync";
    public int Sequence { get; set; }
    public float Depth { get; set; }
    public float Lift { get; set; }
    public bool Tension { get; set; }
    public float TensionStrength { get; set; } = .35f;
}

public sealed class PickReply
{
    public int ProtocolVersion { get; set; }
    public string Raid { get; set; }
    public string Actor { get; set; }
    public string Door { get; set; }
    public string Tool { get; set; }
    public string Attempt { get; set; }
    public string RequestId { get; set; }
    public string Error { get; set; }
    public int Sequence { get; set; }
    public int Difficulty { get; set; }
    public int ToolUses { get; set; }
    public long Revision { get; set; }
    public float Xp { get; set; }
    public PickSnapshot State { get; set; }
    public PickCoaching Coaching { get; set; }
}

public sealed class PickSession
{
    public string Actor,
        Door,
        Tool,
        Id = Guid.NewGuid().ToString("N");
    public int Sequence,
        Difficulty,
        Uses;
    public float Depth,
        Lift,
        IdleSeconds;
    public bool Tension;
    public bool ShowCoaching;
    public float TensionStrength = .35f;
    public PinLockEngine Engine;
}

public sealed class PickingAuthority
{
    public string Raid { get; } = Guid.NewGuid().ToString("N");
    public Dictionary<string, PickSession> Active { get; } = new();
    private readonly Dictionary<string, PinLockDefinition> _locks = new();
    private readonly Dictionary<string, float> _wear = new();
    private readonly Dictionary<string, int> _uses = new();
    private readonly HashSet<string> _xp = new();
    private readonly HashSet<string> _requests = new();
    private readonly LockPickingData _config;
    private uint _seed;
    private long _revision;

    public PickingAuthority(LockPickingData config, uint seed)
    {
        config.Validate();
        _config = config;
        _seed = seed;
    }

    public PickReply Process(
        PickRequest r,
        int skill,
        int difficulty,
        int uses,
        int maximumUses,
        string error,
        int? coachingSkill = null
    )
    {
        var reply = new PickReply
        {
            ProtocolVersion = PickingProtocol.Version,
            Raid = Raid,
            Actor = r.Actor,
            Door = r.Door,
            RequestId = r.RequestId,
            Revision = ++_revision,
        };
        if (r.ProtocolVersion != PickingProtocol.Version)
        {
            reply.Error = PickingProtocol.UpdateMessage;
            return reply;
        }
        if (r.Operation == "sync")
            return reply;
        if (r.Raid != Raid || string.IsNullOrEmpty(r.Actor) || string.IsNullOrEmpty(r.Door))
        {
            reply.Error = "SkillsExtended.PickingAuthority.PickingSessionIsNoLongerAvailable";
            return reply;
        }
        if (r.Operation == "start" || r.Operation == "inspect")
        {
            if (string.IsNullOrEmpty(r.RequestId) || !_requests.Add(r.Actor + "/" + r.RequestId))
                return reply;
            if (error != null)
            {
                reply.Error = error;
                return reply;
            }
            reply.Difficulty = difficulty;
            if (r.Operation == "inspect")
            {
                reply.Xp = Reward(
                    r.Actor,
                    r.Door,
                    "inspect",
                    difficulty,
                    _config.InspectLockXpRatio
                );
                reply.Attempt = r.RequestId;
                return reply;
            }
            if (Active.ContainsKey(r.Door) || Active.Values.Any(s => s.Actor == r.Actor))
            {
                reply.Error = "SkillsExtended.PickingAuthority.APickingSessionIsAlreadyActive";
                return reply;
            }
            if (string.IsNullOrEmpty(r.Tool))
            {
                reply.Error = "SkillsExtended.PickingAuthority.CarryAUsableLockpickSet";
                return reply;
            }
            uses = Math.Max(uses, _uses.TryGetValue(r.Tool, out var recorded) ? recorded : 0);
            if (maximumUses > 0 && uses >= maximumUses)
            {
                reply.Error = "SkillsExtended.PickingAuthority.ThisLockpickSetIsDepleted";
                return reply;
            }
            if (!_locks.TryGetValue(r.Door, out var definition))
            {
                definition = PinLockDefinition.Create(_config.Tier(difficulty), ++_seed);
                _locks.Add(r.Door, definition);
            }
            var session = new PickSession
            {
                Actor = r.Actor,
                Door = r.Door,
                Tool = r.Tool,
                Difficulty = difficulty,
                ShowCoaching = CanCoach(difficulty, coachingSkill ?? skill),
                Uses = uses,
                Engine = new PinLockEngine(
                    definition,
                    _config,
                    difficulty,
                    skill,
                    _wear.TryGetValue(r.Tool, out var w) ? w : 0
                ),
            };
            Active.Add(r.Door, session);
            return Reply(session);
        }
        if (!Active.TryGetValue(r.Door, out var s) || s.Actor != r.Actor || s.Id != r.Attempt)
            return reply;
        if (r.Operation == "cancel")
            return End(s.Door, false);
        if (error != null)
            return End(s.Door, true);
        if (
            r.Operation != "input"
            || r.Sequence <= s.Sequence
            || !PinLockEngine.Finite(r.Depth)
            || !PinLockEngine.Finite(r.Lift)
            || !PinLockEngine.Finite(r.TensionStrength)
            || r.TensionStrength < 0
            || r.TensionStrength > 1
            || r.Depth < 0
            || r.Depth > 1
            || r.Lift < 0
            || r.Lift > 1
        )
            return reply;
        s.Sequence = r.Sequence;
        s.ShowCoaching = CanCoach(s.Difficulty, coachingSkill ?? skill);
        s.Depth = r.Depth;
        s.Lift = r.Lift;
        s.Tension = r.Tension;
        s.TensionStrength = r.TensionStrength;
        s.IdleSeconds = 0;
        return null;
    }

    public List<PickReply> Advance(float seconds)
    {
        var replies = new List<PickReply>();
        foreach (var s in Active.Values.ToArray())
        {
            s.IdleSeconds += seconds;
            if (s.IdleSeconds > 3)
            {
                replies.Add(End(s.Door, true));
                continue;
            }
            s.Engine.Advance(seconds, s.Depth, s.Lift, s.Tension, s.TensionStrength);
            var reply = Reply(s);
            if (s.Engine.Outcome != PickOutcome.Active)
                Finish(s, reply);
            replies.Add(reply);
        }
        return replies;
    }

    public PickReply End(string door, bool interrupted)
    {
        if (!Active.TryGetValue(door, out var s))
            return null;
        s.Engine.End(interrupted);
        var reply = Reply(s);
        Finish(s, reply);
        return reply;
    }

    private void Finish(PickSession s, PickReply reply)
    {
        _wear[s.Tool] = s.Engine.Wear;
        if (s.Engine.Outcome == PickOutcome.PickBroken)
        {
            _wear[s.Tool] = 0;
            _uses[s.Tool] = ++s.Uses;
            reply.ToolUses = s.Uses;
            if (s.Engine.MadeProgress)
                reply.Xp = Reward(
                    s.Actor,
                    s.Door,
                    "failure",
                    s.Difficulty,
                    _config.FailureLockXpRatio
                );
        }
        if (s.Engine.Outcome == PickOutcome.Unlocked)
            reply.Xp = Reward(s.Actor, s.Door, "success", s.Difficulty, 1);
        Active.Remove(s.Door);
    }

    private float Reward(string actor, string door, string kind, int difficulty, float ratio) =>
        _xp.Add(actor + "/" + door + "/" + kind)
        && _config.XpTable != null
        && _config.XpTable.TryGetValue(difficulty.ToString(), out var xp)
            ? xp * ratio
            : 0;

    private bool CanCoach(int difficulty, int skill) =>
        _config.EnableRaidCoaching && difficulty >= 1 && difficulty <= 3 && skill >= 0 && skill <= 10;

    private PickReply Reply(PickSession s) =>
        new()
        {
            ProtocolVersion = PickingProtocol.Version,
            Raid = Raid,
            Actor = s.Actor,
            Door = s.Door,
            Tool = s.Tool,
            Attempt = s.Id,
            Sequence = s.Sequence,
            Difficulty = s.Difficulty,
            ToolUses = s.Uses,
            Revision = ++_revision,
            State = s.Engine.Snapshot(),
            Coaching = _config.EnableRaidCoaching && s.ShowCoaching && s.Engine.Outcome == PickOutcome.Active
                ? s.Engine.Coaching() : null,
        };
}
