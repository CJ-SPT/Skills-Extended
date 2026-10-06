using System;
using System.Collections.Generic;
using SkillsExtended.Config.Skills;

namespace SkillsExtended.Hacking;

public class HackRequest
{
    public string Raid { get; set; } = "";
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public string Actor { get; set; } = "";
    public string Door { get; set; } = "";
    public string Attempt { get; set; } = "";
    public string Operation { get; set; } = "sync";
    public int Sequence { get; set; }
    public int Node { get; set; } = -1;
    public int Slot { get; set; } = -1;
}

public class HackReply
{
    public string Raid { get; set; }
    public string RequestId { get; set; }
    public string Actor { get; set; }
    public string Door { get; set; }
    public string Attempt { get; set; }
    public int Sequence { get; set; }
    public string Error { get; set; }
    public long Revision { get; set; }
    public bool Unlock { get; set; }
    public float Xp { get; set; }
    public HackBoard Board { get; set; }
    public Dictionary<string, int> Failures { get; set; } = new();
}

public class HackSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Actor { get; set; }
    public string Door { get; set; }
    public int Sequence { get; set; }
    public HackBoard Board { get; set; }
    public bool Finished { get; set; }
}

/// <summary>One authority per raid. Eligibility and door effects are owned by the game adapter.</summary>
public sealed class HackingAuthority
{
    public string Raid { get; } = Guid.NewGuid().ToString("N");
    public Dictionary<string, HackSession> Active { get; } = new();
    public Dictionary<string, int> Failures { get; } = new();

    private readonly HashSet<string> _successXp = new();
    private readonly HashSet<string> _failureXp = new();
    private readonly HashSet<string> _requests = new();
    private readonly HackingData _config;
    private uint _seed;
    private long _revision;

    public HackingAuthority(HackingData config, uint seed)
    {
        config.Validate();
        _config = config;
        _seed = seed;
    }

    public HackReply Process(
        HackRequest request,
        int level,
        int difficulty,
        string eligibilityError
    )
    {
        var reply = Reply(request);
        if (request.Operation == "sync")
        {
            return reply;
        }

        if (request.Raid != Raid)
        {
            reply.Error = "SkillsExtended.HackingAuthority.RaidChangedReconnectThePda";
            return reply;
        }

        if (
            string.IsNullOrEmpty(request.RequestId)
            || string.IsNullOrEmpty(request.Actor)
            || string.IsNullOrEmpty(request.Door)
        )
        {
            reply.Error = "SkillsExtended.HackingAuthority.InvalidRequest";
            return reply;
        }

        if (!_requests.Add(request.Actor + "/" + request.RequestId))
        {
            reply.Error = "SkillsExtended.HackingAuthority.DuplicateRequest";
            return reply;
        }

        if (request.Operation == "start")
        {
            if (!string.IsNullOrEmpty(eligibilityError))
            {
                reply.Error = eligibilityError;
                return reply;
            }

            if (
                Failures.TryGetValue(request.Door, out var count)
                && count >= _config.AttemptsPerDoor
            )
            {
                reply.Error = "SkillsExtended.HackingAuthority.HackingLockedOutForThisRaidAValidKeycard";
                return reply;
            }

            if (Active.ContainsKey(request.Door))
            {
                reply.Error = "SkillsExtended.HackingAuthority.AnotherHackIsActiveOnThisDoor";
                return reply;
            }

            foreach (var active in Active.Values)
            {
                if (active.Actor == request.Actor)
                {
                    reply.Error = "SkillsExtended.HackingAuthority.FinishTheCurrentHackFirst";
                    return reply;
                }
            }

            var session = new HackSession
            {
                Actor = request.Actor,
                Door = request.Door,
                Board = HackingEngine.StartAttempt(_config, difficulty, level, ++_seed),
            };
            Active.Add(request.Door, session);
            reply.Revision = ++_revision;
            reply.Attempt = session.Id;
            reply.Board = session.Board;
            return reply;
        }

        if (
            !Active.TryGetValue(request.Door, out var current)
            || current.Actor != request.Actor
            || current.Id != request.Attempt
        )
        {
            reply.Error = "SkillsExtended.HackingAuthority.AttemptIsNoLongerActive";
            return reply;
        }

        reply.Attempt = current.Id;
        reply.Sequence = current.Sequence;
        reply.Board = current.Board;
        // Closing the UI may race an already-sent click. Cancellation is terminal for this
        // exact actor/attempt, so accept its last-known sequence; future actions stay strict.
        var validSequence =
            request.Operation == "abort"
                ? request.Sequence > 0 && request.Sequence <= current.Sequence + 1
                : request.Sequence == current.Sequence + 1;
        if (!validSequence)
        {
            reply.Error = "SkillsExtended.HackingAuthority.OutOfOrderAction";
            return reply;
        }

        if (!string.IsNullOrEmpty(eligibilityError) && request.Operation != "abort")
        {
            reply.Error = eligibilityError;
            return reply;
        }

        var changed = request.Operation switch
        {
            "node" => current.Board.SelectNode(request.Node),
            "utility" => current.Board.UseUtility(request.Slot, request.Node),
            "abort" => current.Board.Abort(),
            "ui-error" when current.Board.Turn == 0 => Cancel(current.Board),
            _ => false,
        };
        if (!changed)
        {
            reply.Error = "SkillsExtended.HackingAuthority.ThatActionIsNotAvailable";
            return reply;
        }

        current.Sequence++;
        reply.Sequence = current.Sequence;
        reply.Revision = ++_revision;
        Finish(current, reply);
        reply.Failures = new Dictionary<string, int>(Failures);
        return reply;
    }

    private static bool Cancel(HackBoard board)
    {
        board.Status = HackStatus.Cancelled;
        return true;
    }

    public HackReply End(string door, bool penalize)
    {
        if (!Active.TryGetValue(door, out var session))
        {
            return null;
        }

        session.Board.Status = penalize ? HackStatus.Aborted : HackStatus.Cancelled;
        session.Sequence++;
        var reply = new HackReply
        {
            Raid = Raid,
            Actor = session.Actor,
            Door = door,
            Attempt = session.Id,
            Sequence = session.Sequence,
            Board = session.Board,
            Revision = ++_revision,
        };
        Finish(session, reply);
        reply.Failures = new Dictionary<string, int>(Failures);
        return reply;
    }

    private HackReply Reply(HackRequest request) =>
        new()
        {
            Raid = Raid,
            RequestId = request.RequestId,
            Actor = request.Actor,
            Door = request.Door,
            Attempt = request.Attempt,
            Revision = _revision,
            Failures = new Dictionary<string, int>(Failures),
        };

    private void Finish(HackSession session, HackReply reply)
    {
        if (session.Finished || session.Board.Status == HackStatus.Active)
        {
            return;
        }

        session.Finished = true;
        Active.Remove(session.Door);
        var key = session.Actor + "/" + session.Door;
        var xp = _config.Tier(session.Board.Difficulty).SuccessXp;
        if (session.Board.Status == HackStatus.Won)
        {
            reply.Unlock = true;
            if (_successXp.Add(key))
            {
                reply.Xp = xp;
            }
        }
        else if (
            session.Board.Status == HackStatus.Lost
            || session.Board.Status == HackStatus.Aborted
        )
        {
            Failures[session.Door] =
                (Failures.TryGetValue(session.Door, out var count) ? count : 0) + 1;
            if (
                session.Board.Status == HackStatus.Lost
                && session.Board.Explored >= 3
                && _failureXp.Add(key)
            )
            {
                reply.Xp = xp * _config.FailureXpRatio;
            }
        }
    }
}
