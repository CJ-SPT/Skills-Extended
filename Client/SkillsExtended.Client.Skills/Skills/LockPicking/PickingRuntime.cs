using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.Ballistics;
using EFT.Communications;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Config.Skills;
using SkillsExtended.LockPicking;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.LockPicking;

public sealed class PickingRuntime : MonoBehaviour
{
    public static PickingRuntime Instance { get; private set; }
    public static Func<bool> IsAuthority = () => !SkillsExtendedInfo.IsFikaPresent;
    public static Action<PickRequest> Transport;
    public static event Action<PickReply> AuthorityReply;
    public static LockPickingData Config => SkillsExtendedPlugin.SkillData.LockPicking;
    public GameWorld World { get; private set; }
    public PickingAuthority Authority { get; private set; }
    private readonly Dictionary<string, WorldInteractiveObject> _doors = new();
    private readonly Dictionary<string, Vector3> _origins = new();
    private readonly HashSet<string> _effects = new(),
        _dismissed = new(),
        _finished = new();
    private readonly Dictionary<string, long> _revisions = new();
    private readonly Dictionary<Player, Action<DamageInfo, EBodyPart, float>> _hits = new();
    private string _raid,
        _pending;
    private float _tick,
        _nextSync,
        _pendingAt;

    public static void Boot(GameWorld world)
    {
        if (Instance)
            Destroy(Instance);
        var runtime = world.gameObject.AddComponent<PickingRuntime>();
        runtime.World = world;
        Instance = runtime;
        if (!Config.Enabled)
            return;
        foreach (
            var door in LocationScene.GetAllObjectsAndWhenISayAllIActuallyMeanIt<WorldInteractiveObject>()
        )
            if (LockPickingHelpers.Supported(door))
                runtime._doors[door.Id] = door;
        if (IsAuthority())
        {
            runtime.Authority = new PickingAuthority(Config, (uint)Guid.NewGuid().GetHashCode());
            runtime._raid = runtime.Authority.Raid;
        }
    }

    private Player Player(string id) =>
        World?.RegisteredPlayers.OfType<Player>().FirstOrDefault(p => p.ProfileId == id);

    private WorldInteractiveObject Door(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        if (_doors.TryGetValue(id, out var cached) && cached)
            return cached;
        // Modded doors can register after OnGameStarted, including on a remote peer.
        var door = LocationScene
            .GetAllObjectsAndWhenISayAllIActuallyMeanIt<WorldInteractiveObject>()
            .FirstOrDefault(d => d && d.Id == id && LockPickingHelpers.Supported(d));
        if (door)
            _doors[id] = door;
        return door;
    }

    private static Key Tool(Player p, string id) =>
        LockPickingHelpers.Picks(p).FirstOrDefault(k => k.Id == id);

    private string Eligibility(
        Player p,
        WorldInteractiveObject d,
        string tool,
        bool inspect,
        bool start
    )
    {
        if (!Config.Enabled || !p || !p.HealthController.IsAlive || p.Side == EPlayerSide.Savage)
            return "Only a living PMC can pick locks.";
        if (
            !LockPickingHelpers.Supported(d)
            || LockPickingHelpers.GetLevelForDoor(World.LocationId, d.Id) < 0
        )
            return "This lock is not supported.";
        if (
            d.DoorState != EDoorState.Locked
            || !d.Operatable
            || d.NoInteractionsAllowed
            || !d.gameObject.activeInHierarchy
        )
            return "This door is unavailable.";
        if (p.MovementContext.CanInteract != null)
            return "Cannot interact in the current player state.";
        if (
            d.HasSkillRequirement
            && (
                !p.Skills.TryGetSkill(d.SkillRequirement, out var required)
                || required.Level < d.SkillMinLevelRequirement
            )
        )
            return "Native door requirements are not met.";
        if (
            Vector3.Distance(p.Position, d.GetInteractionParameters(p.Position).InteractionPosition)
            > 3
        )
            return "Move closer to the lock.";
        if (start && p.InputDirection.sqrMagnitude > .001f)
            return "Stop moving before picking.";
        if (!inspect && Tool(p, tool) == null)
            return "Carry a usable lockpick set.";
        return null;
    }

    public void Begin(GamePlayerOwner owner, WorldInteractiveObject door, bool inspect)
    {
        if (
            _pending != null
            || LockPickingGame.Current
            || Hacking.HackingView.IsOpen
            || Signals.SignalsView.Current
        )
            return;
        var tool = LockPickingHelpers.Picks(owner.Player).FirstOrDefault();
        var error = Eligibility(owner.Player, door, tool?.Id, inspect, true);
        if (error != null)
        {
            Notify(error);
            return;
        }
        if (!inspect && !LockPickingGame.Prepare())
        {
            Notify("Lock-picking artwork could not be loaded.");
            return;
        }
        if (_raid == null)
        {
            Send(new PickRequest { Actor = owner.Player.ProfileId });
            Notify("Connecting lock-picking session. Try again shortly.");
            return;
        }
        _doors[door.Id] = door;
        _pending = door.Id;
        _pendingAt = Time.unscaledTime;
        Send(
            new PickRequest
            {
                Raid = _raid,
                Actor = owner.Player.ProfileId,
                Door = door.Id,
                Tool = tool?.Id,
                Operation = inspect ? "inspect" : "start",
            }
        );
    }

    public void Send(PickRequest request)
    {
        if (IsAuthority())
            Handle(request);
        else
            Transport?.Invoke(request);
    }

    public void Handle(PickRequest request)
    {
        if (Authority == null || request == null)
            return;
        var player = Player(request.Actor);
        var door = Door(request.Door);
        var active =
            request.Door != null && Authority.Active.TryGetValue(request.Door, out var session)
                ? session
                : null;
        var tool = Tool(player, active?.Tool ?? request.Tool);
        var error =
            request.Operation == "sync"
                ? null
                : Eligibility(
                    player,
                    door,
                    active?.Tool ?? request.Tool,
                    request.Operation == "inspect",
                    request.Operation == "start"
                );
        var level = player
            ? Math.Max(
                0,
                Math.Min(51, player.Skills.Lockpicking.Level + player.Skills.Lockpicking.Buff)
            )
            : 0;
        var reply = Authority.Process(
            request,
            level,
            door ? LockPickingHelpers.GetLevelForDoor(World.LocationId, door.Id) : 1,
            tool?.KeyComponent.NumberOfUsages ?? 0,
            tool?.KeyComponent.Template.MaximumNumberOfUsage ?? 0,
            error
        );
        if (reply?.State?.Outcome == PickOutcome.Active && player)
            _origins[reply.Attempt] = player.Position;
        Publish(reply);
    }

    private void Publish(PickReply reply)
    {
        if (reply == null)
            return;
        if (reply.State != null && reply.State.Outcome != PickOutcome.Active)
            _origins.Remove(reply.Attempt);
        Receive(reply);
        AuthorityReply?.Invoke(reply);
    }

    public void Receive(PickReply reply)
    {
        if (reply == null || (_raid != null && reply.Raid != _raid))
            return;
        _raid = reply.Raid;
        if (reply.Attempt != null)
        {
            if (
                _finished.Contains(reply.Attempt)
                || (_revisions.TryGetValue(reply.Attempt, out var rev) && rev >= reply.Revision)
            )
                return;
            _revisions[reply.Attempt] = reply.Revision;
            if (reply.State != null && reply.State.Outcome != PickOutcome.Active)
                _finished.Add(reply.Attempt);
        }
        if (
            reply.State?.Outcome == PickOutcome.Unlocked
            && Door(reply.Door) is { } door
            && door.DoorState == EDoorState.Locked
        )
            door.Unlock();
        var p = World?.MainPlayer;
        if (!p || p.ProfileId != reply.Actor || SkillsExtendedInfo.IsFikaHeadless)
            return;
        var pending = _pending == reply.Door;
        if (pending)
            _pending = null;
        if (reply.Error != null)
        {
            Notify(reply.Error);
            return;
        }
        if (
            reply.Attempt != null
            && (reply.Xp > 0 || reply.State?.Outcome == PickOutcome.PickBroken)
            && _effects.Add(reply.Attempt)
        )
        {
            if (reply.State?.Outcome == PickOutcome.PickBroken)
            {
                var pick = Tool(p, reply.Tool);
                if (pick != null)
                {
                    pick.KeyComponent.NumberOfUsages = Math.Max(
                        pick.KeyComponent.NumberOfUsages,
                        reply.ToolUses
                    );
                    if (
                        pick.KeyComponent.Template.MaximumNumberOfUsage > 0
                        && pick.KeyComponent.NumberOfUsages
                            >= pick.KeyComponent.Template.MaximumNumberOfUsage
                    )
                    {
                        var result = ItemManipulator.Discard(
                            pick,
                            (ItemController)pick.Parent.GetOwner()
                        );
                        if (result.Failed)
                            SkillsExtendedPlugin.Log.LogWarning(
                                "Depleted lockpick could not be discarded; it remains unusable."
                            );
                    }
                }
            }
            if (reply.Xp > 0 && !p.Skills.Lockpicking.IsEliteLevel)
            {
                var xp = reply.Xp;
                p.ExecuteSkill(() => p.Skills.SkillsExtendedManager.LockPickAction.Complete(xp));
            }
        }
        if (reply.State == null)
        {
            if (reply.Difficulty > 0 && Door(reply.Door) is { } inspected)
            {
                var key = SkillsExtendedPlugin.Keys.KeyLocale.TryGetValue(
                    inspected.KeyId,
                    out var name
                )
                    ? name
                    : "Unknown";
                Notify(
                    $"Lock tier {reply.Difficulty} · {Config.Tier(reply.Difficulty).Pins} pins · Key: {key}"
                );
            }
            return;
        }
        if (LockPickingGame.Current)
            LockPickingGame.Current.Receive(reply);
        else if (reply.State.Outcome == PickOutcome.Active && !_dismissed.Contains(reply.Attempt))
        {
            if (!pending || Hacking.HackingView.IsOpen || Signals.SignalsView.Current)
            {
                Cancel(reply);
                return;
            }
            try
            {
                LockPickingGame.Show(this, p, reply);
            }
            catch (Exception e)
            {
                SkillsExtendedPlugin.Log.LogError(e);
                Cancel(reply);
            }
        }
    }

    public void Cancel(PickReply reply)
    {
        _dismissed.Add(reply.Attempt);
        Send(
            new PickRequest
            {
                Raid = _raid,
                Actor = reply.Actor,
                Door = reply.Door,
                Attempt = reply.Attempt,
                Operation = "cancel",
            }
        );
    }

    public void Disconnect(string actor)
    {
        if (Authority != null)
            foreach (var session in Authority.Active.Values.Where(s => s.Actor == actor).ToArray())
                Publish(Authority.End(session.Door, true));
    }

    public void ConnectionLost()
    {
        _raid = null;
        _pending = null;
        _nextSync = 0;
        LockPickingGame.Current?.Close();
    }

    private void Update()
    {
        if (!World || !Config.Enabled)
            return;
        if (_pending != null && Time.unscaledTime - _pendingAt > 5)
            _pending = null;
        if (!IsAuthority())
        {
            if (_raid == null && World.MainPlayer && Time.unscaledTime >= _nextSync)
            {
                _nextSync = Time.unscaledTime + 3;
                Send(new PickRequest { Actor = World.MainPlayer.ProfileId });
            }
            return;
        }
        foreach (var p in World.RegisteredPlayers.OfType<Player>())
        {
            if (_hits.ContainsKey(p))
                continue;
            Action<DamageInfo, EBodyPart, float> hit = (_, _, amount) =>
            {
                if (amount > 0)
                    Disconnect(p.ProfileId);
            };
            _hits.Add(p, hit);
            p.BeingHitAction += hit;
        }
        foreach (var s in Authority.Active.Values.ToArray())
        {
            var p = Player(s.Actor);
            var d = Door(s.Door);
            if (
                Eligibility(p, d, s.Tool, false, false) != null
                || (
                    _origins.TryGetValue(s.Id, out var origin)
                    && Vector3.Distance(origin, p.Position) > .3f
                )
            )
                Publish(Authority.End(s.Door, true));
        }
        _tick += Time.unscaledDeltaTime;
        if (_tick >= .05f)
        {
            var elapsed = Math.Min(.25f, _tick);
            _tick = 0;
            foreach (var reply in Authority.Advance(elapsed))
                Publish(reply);
        }
    }

    private void OnDestroy()
    {
        foreach (var hit in _hits)
            if (hit.Key)
                hit.Key.BeingHitAction -= hit.Value;
        if (Instance == this)
        {
            LockPickingGame.Current?.Close();
            Instance = null;
        }
    }

    public static void Notify(string text) => NotificationManager.DisplayMessageNotification(text);
}

public class PickingRaidStartPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));

    [PatchPostfix]
    public static void Postfix(GameWorld __instance) => PickingRuntime.Boot(__instance);
}
