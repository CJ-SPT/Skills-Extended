using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Communications;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SkillsExtended.Config.Skills;
using SkillsExtended.Hacking;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.Hacking;

public sealed class ElectronicsRuntime : MonoBehaviour
{
    public static ElectronicsRuntime Instance { get; private set; }

    // The optional Fika assembly supplies transport; the core plugin never depends on Fika.
    public static Func<bool> IsAuthority = () => !SkillsExtendedInfo.IsFikaPresent;
    public static Action<HackRequest> Transport;
    public static event Action<HackReply> AuthorityReply;
    public static HackingData Config => SkillsExtendedPlugin.SkillData.Hacking;
    public GameWorld World { get; private set; }
    public HackingAuthority Authority { get; private set; }

    private readonly ElectronicsDoorRegistry _doors = new();
    private readonly Dictionary<string, Vector3> _origins = new();
    private readonly HashSet<string> _appliedUnlocks = new();
    private readonly HashSet<string> _appliedXp = new();
    private readonly Dictionary<string, int> _lastSequences = new();
    private readonly HashSet<string> _endedAttempts = new();
    private readonly HashSet<string> _dismissedAttempts = new();
    private long _revision = -1;
    private readonly Dictionary<string, Player> _subscribed = new();
    private readonly Dictionary<string, Action<DamageInfo, EBodyPart, float>> _hitHandlers = new();
    private Dictionary<string, int> _failures = new();
    private string _raid;
    private string _pendingStart;
    private float _nextSync;

    public static void Boot(GameWorld world)
    {
        if (Instance)
        {
            Destroy(Instance);
        }

        var runtime = world.gameObject.AddComponent<ElectronicsRuntime>();
        runtime.World = world;
        Instance = runtime;
        if (!Config.Enabled)
        {
            return;
        }

        runtime._doors.Refresh();

        if (IsAuthority())
        {
            runtime.Authority = new HackingAuthority(Config, (uint)Guid.NewGuid().GetHashCode());
            runtime._raid = runtime.Authority.Raid;
        }
    }

    public static bool HasPda(Player player) =>
        player
            ?.Inventory?.GetPlayerItems(EPlayerItems.Equipment)
            .Any(i => i.TemplateId == HackingIds.Pda) == true;

    public int Remaining(string door) =>
        Math.Max(0, Config.AttemptsPerDoor - (_failures.TryGetValue(door, out var n) ? n : 0));

    public Player FindPlayer(string actor) =>
        World?.RegisteredPlayers.OfType<Player>().FirstOrDefault(p => p.ProfileId == actor);

    public string Eligibility(Player player, KeycardDoor door, bool starting)
    {
        if (!Config.Enabled)
        {
            return "Hacking is disabled.";
        }

        if (!player || player.Side == EPlayerSide.Savage || !player.HealthController.IsAlive)
        {
            return "Only a living PMC can hack.";
        }

        if (!ElectronicsDoorRegistry.Supports(door))
        {
            return "Electronic lock is unavailable. Try the reader again.";
        }

        if (Config.Excluded(World.LocationId, door.Id, door.KeyId))
        {
            return "This access system cannot be bypassed.";
        }

        if (door.DoorState != EDoorState.Locked)
        {
            return "Door is already unlocked.";
        }

        if (!door.Operatable || door.NoInteractionsAllowed || !door.gameObject.activeInHierarchy)
        {
            return "Reader is unavailable; check power and access conditions.";
        }

        if (player.MovementContext.CanInteract != null)
        {
            return "Cannot interact in the current player state.";
        }

        var grip = door.GetClosestGrip(player.Position);
        if (
            door.Proxies == null
            || !door.Proxies.Any(p => p && p.gameObject.activeInHierarchy && p.Grips.Contains(grip))
        )
        {
            return "Approach the active reader side.";
        }

        if (
            door.HasSkillRequirement
            && (
                !player.Skills.TryGetSkill(door.SkillRequirement, out var required)
                || required.Level < door.SkillMinLevelRequirement
            )
        )
        {
            return "Native door requirements are not met.";
        }

        if (!HasPda(player))
        {
            return "Carry a Modified PDA to hack this door.";
        }

        var position = door.GetInteractionParameters(player.Position).InteractionPosition;
        if (Vector3.Distance(player.Position, position) > 3f)
        {
            return "Move closer to the reader.";
        }

        if (starting && player.InputDirection.sqrMagnitude > 0.001f)
        {
            return "Stop moving before hacking.";
        }

        if (SkillsExtendedInfo.IsFikaPresent && Transport == null)
        {
            return "Matching Skills Extended Fika support is required.";
        }

        return null;
    }

    public void Begin(GamePlayerOwner owner, KeycardDoor door)
    {
        var error = Eligibility(owner.Player, door, true);
        if (error != null)
        {
            Notify(error);
            return;
        }

        if (_pendingStart != null || HackingView.IsOpen)
        {
            return;
        }

        if (!HackingView.Prepare())
        {
            Notify("Hacking UI unavailable. Check electronics_ui.bundle.");
            return;
        }

        if (string.IsNullOrEmpty(_raid))
        {
            Send(new HackRequest { Actor = owner.Player.ProfileId });
            Notify("Connecting PDA. Try again in a moment.");
            return;
        }

        _doors.Register(door);
        _pendingStart = door.Id;
        Send(
            new HackRequest
            {
                Raid = _raid,
                Actor = owner.Player.ProfileId,
                Door = door.Id,
                Operation = "start",
            }
        );
    }

    public void Send(HackRequest request)
    {
        if (IsAuthority())
        {
            Handle(request);
        }
        else
        {
            Transport?.Invoke(request);
        }
    }

    public void Handle(HackRequest request)
    {
        if (Authority == null)
        {
            return;
        }

        var player = FindPlayer(request.Actor);
        var door = _doors.Resolve(request.Door);
        var error =
            request.Operation == "sync"
                ? null
                : Eligibility(player, door, request.Operation == "start");
        // A legitimate external unlock cancels an existing attempt rather than penalizing its owner.
        if (door && door.DoorState != EDoorState.Locked && Authority.Active.ContainsKey(door.Id))
        {
            Publish(Authority.End(door.Id, false));
            return;
        }

        var skill = player ? HackingSkill.Get(player.Skills).Skill : null;
        var level = skill == null ? 0 : Math.Max(0, Math.Min(51, skill.Level + skill.Buff));
        var difficulty = door ? Config.Difficulty(World.LocationId, door.Id, door.KeyId) : 2;
        var reply = Authority.Process(request, level, difficulty, error);
        if (
            reply.Board != null
            && reply.Board.Status == HackStatus.Active
            && player
            && !_origins.ContainsKey(reply.Attempt)
        )
        {
            _origins[reply.Attempt] = player.Position;
        }

        Publish(reply);
    }

    public void Disconnect(string actor)
    {
        if (Authority == null)
        {
            return;
        }

        foreach (var session in Authority.Active.Values.Where(s => s.Actor == actor).ToArray())
        {
            Publish(Authority.End(session.Door, true));
        }
    }

    public void ConnectionLost()
    {
        _raid = null;
        _revision = -1;
        _pendingStart = null;
        _nextSync = 0;
        HackingView.Current?.Close();
    }

    public void DismissAttempt(string attempt) => _dismissedAttempts.Add(attempt);

    private void Publish(HackReply reply)
    {
        if (reply == null)
        {
            return;
        }

        Receive(reply);
        AuthorityReply?.Invoke(reply);
    }

    public void Receive(HackReply reply)
    {
        if (reply == null)
        {
            return;
        }

        if (_raid != null && _raid != reply.Raid)
        {
            return;
        }

        _raid = reply.Raid;
        if (reply.Revision >= _revision)
        {
            _revision = reply.Revision;
            _failures = reply.Failures ?? new();
        }

        if (reply.Board != null && !string.IsNullOrEmpty(reply.Attempt))
        {
            if (
                _endedAttempts.Contains(reply.Attempt)
                || (
                    _lastSequences.TryGetValue(reply.Attempt, out var sequence)
                    && reply.Sequence < sequence
                )
            )
            {
                return;
            }

            _lastSequences[reply.Attempt] = reply.Sequence;
            if (reply.Board.Status != HackStatus.Active)
            {
                _endedAttempts.Add(reply.Attempt);
            }
        }

        if (
            reply.Unlock
            && !string.IsNullOrEmpty(reply.Attempt)
            && _appliedUnlocks.Add(reply.Attempt)
            && _doors.Resolve(reply.Door) is { } door
            && door.DoorState == EDoorState.Locked
        )
        {
            door.Unlock();
        }

        var player = World?.MainPlayer;
        if (!player || player.ProfileId != reply.Actor || SkillsExtendedInfo.IsFikaHeadless)
        {
            return;
        }

        if (_pendingStart == reply.Door)
        {
            _pendingStart = null;
        }

        if (reply.Xp > 0 && _appliedXp.Add(reply.Attempt))
        {
            player.ExecuteSkill(() =>
                HackingSkill.Get(player.Skills).Action.Complete(reply.Xp)
            );
        }

        if (reply.Board == null)
        {
            HackingView.Current?.Receive(reply);
            if (!string.IsNullOrEmpty(reply.Error))
            {
                Notify(reply.Error);
            }

            return;
        }

        if (reply.Board.Status == HackStatus.Active && !HackingView.IsOpen)
        {
            if (_dismissedAttempts.Contains(reply.Attempt))
            {
                return;
            }

            if (!HackingView.Prepare())
            {
                // An accepted session whose UI cannot initialize is cancelled by the authority adapter.
                Send(
                    new HackRequest
                    {
                        Raid = _raid,
                        Actor = player.ProfileId,
                        Door = reply.Door,
                        Attempt = reply.Attempt,
                        Operation = "ui-error",
                        Sequence = reply.Sequence + 1,
                    }
                );
                return;
            }

            HackingView.ShowRaid(this, player, reply);
        }
        else
        {
            HackingView.Current?.Receive(reply);
        }
    }

    private void Update()
    {
        if (!Config.Enabled || !World)
        {
            return;
        }

        if (!IsAuthority())
        {
            if (_raid == null && Time.unscaledTime >= _nextSync && World.MainPlayer)
            {
                _nextSync = Time.unscaledTime + 3;
                Send(new HackRequest { Actor = World.MainPlayer.ProfileId });
            }

            return;
        }

        foreach (var player in World.RegisteredPlayers.OfType<Player>())
        {
            if (!_subscribed.ContainsKey(player.ProfileId))
            {
                _subscribed.Add(player.ProfileId, player);
                Action<DamageInfo, EBodyPart, float> handler = (_, _, damage) =>
                {
                    if (damage > 0)
                    {
                        Disconnect(player.ProfileId);
                    }
                };
                _hitHandlers[player.ProfileId] = handler;
                player.BeingHitAction += handler;
            }
        }

        foreach (var session in Authority.Active.Values.ToArray())
        {
            var player = FindPlayer(session.Actor);
            var door = _doors.Resolve(session.Door);
            if (!door || door.DoorState != EDoorState.Locked)
            {
                Publish(Authority.End(session.Door, false));
                continue;
            }

            if (
                !player
                || !player.HealthController.IsAlive
                || Eligibility(player, door, false) != null
                || (
                    _origins.TryGetValue(session.Id, out var origin)
                    && Vector3.Distance(origin, player.Position) > 0.3f
                )
            )
            {
                Publish(Authority.End(session.Door, true));
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var player in _subscribed.Values)
        {
            if (player)
            {
                player.BeingHitAction -= _hitHandlers[player.ProfileId];
            }
        }

        if (Instance == this)
        {
            HackingView.Current?.Close();
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void Notify(string text) => NotificationManager.DisplayMessageNotification(text);
}

public class ElectronicsRaidStartPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));

    [PatchPostfix]
    public static void Postfix(GameWorld __instance) => ElectronicsRuntime.Boot(__instance);
}

public class ElectronicsKeycardPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(
            typeof(InteractionContextHelper),
            nameof(InteractionContextHelper.GetAvailableActions),
            new[] { typeof(GamePlayerOwner), typeof(KeycardDoor), typeof(bool) }
        );

    [PatchPostfix]
    public static void Postfix(
        AvailableInteractionState __result,
        GamePlayerOwner owner,
        KeycardDoor door,
        bool __2
    )
    {
        var runtime = ElectronicsRuntime.Instance;
        if (
            !runtime
            || !ElectronicsRuntime.Config.Enabled
            || owner?.Player?.IsYourPlayer != true
            || !__2
            || door.DoorState != EDoorState.Locked
            || owner.Player.Side == EPlayerSide.Savage
        )
        {
            return;
        }

        if (ElectronicsRuntime.Config.Excluded(runtime.World.LocationId, door.Id, door.KeyId))
        {
            return;
        }

        var remaining = runtime.Remaining(door.Id);
        var tier = ElectronicsRuntime.Config.Tier(
            ElectronicsRuntime.Config.Difficulty(runtime.World.LocationId, door.Id, door.KeyId)
        );
        var error = runtime.Eligibility(owner.Player, door, true);
        if (remaining == 0)
        {
            error = "Hacking locked out; use a keycard";
        }

        __result.Actions.Add(
            new InteractionAction
            {
                Name =
                    error == null
                        ? $"Hack with PDA · {tier.Name} · {remaining} attempts"
                        : $"Hack with PDA · {error}",
                Disabled = error != null,
                Action = () => runtime.Begin(owner, door),
            }
        );
        if (error == null)
        {
            __result.Error = null;
        }
    }
}
