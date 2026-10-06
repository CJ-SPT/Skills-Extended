using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using Newtonsoft.Json;
using SkillsExtended.Signals;
using SkillsExtended.Skills.Hacking;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SkillsExtended.Skills.Signals;

public class SignalsEnvelope
{
    public SignalManifest Manifest { get; set; }
    public SignalSnapshot State { get; set; }
    public double HostTime { get; set; }
    public string Inventory { get; set; }
}

public sealed partial class SignalsRuntime : MonoBehaviour
{
    public static SignalsRuntime Instance { get; private set; }
    public static Func<bool> IsAuthority = () => !SkillsExtendedInfo.IsFikaPresent;
    public static Action<SignalRequest> Transport;
    public static Func<IEnumerable<string>> ConnectedActors;
    public static event Action<SignalsEnvelope> AuthorityReply;
    public GameWorld World { get; private set; }
    public SignalManifest Manifest { get; private set; }
    public SignalSnapshot State { get; private set; }
    public SignalsAuthority Authority { get; private set; }
    public double HostTime =>
        IsAuthority()
            ? Time.realtimeSinceStartupAsDouble
            : Time.realtimeSinceStartupAsDouble + _clockOffset;
    private double _clockOffset;
    private SignalsCase _case;
    internal Transform CaseTransform => _case?.Transform;
    private float _nextSync;
    private SignalsAudio _proximityAudio;
    private float _appliedXp;
    private int _sequence;
    private bool _lootReady;
    private string _inventory;
    private bool _creating;
    private Task _preload;
    private readonly CancellationTokenSource _lifetime = new();
    internal CancellationToken Lifetime => _lifetime.Token;
    private static readonly Dictionary<string, float> AppliedXp = new();
    private readonly Dictionary<
        string,
        (Player Player, Action<DamageInfo, EBodyPart, float> Handler)
    > _hits = new();

    private Task _finishLoot;
    private Task _caseTask;
    private readonly TaskCompletionSource<bool> _snapshotReady = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public void RefreshRegistration(World world)
    {
        if (_case == null || World.World != world)
            return;
        try
        {
            _case.RegisterInteraction(Manifest);
        }
        catch (Exception e)
        {
            Manifest.Error = "Signal interaction registration failed: " + e.Message;
            if (State != null)
                State.Ready = false;
            SkillsExtendedPlugin.Log.LogError(Manifest.Error);
            _case.Dispose();
            _case = null;
            SignalsView.Current?.Close();
            if (IsAuthority())
                Publish();
        }
    }

    public string Eligibility(Player player)
    {
        if (
            !(
                Manifest?.Config?.Enabled
                ?? SkillsExtendedPlugin.SkillData.SignalsIntelligence.Enabled
            )
        )
            return "SkillsExtended.SignalsRuntime.SignalsIntelligenceIsDisabled";
        if (SkillsExtendedInfo.IsFikaPresent && Transport == null)
            return "SkillsExtended.SignalsRuntime.MatchingSkillsExtendedFikaSupportIsRequired";
        if (!player || player.Side == EPlayerSide.Savage || !player.HealthController.IsAlive)
            return "SkillsExtended.SignalsRuntime.ALivingPmcIsRequired";
        if (!ElectronicsRuntime.HasPda(player))
            return "SkillsExtended.SignalsRuntime.CarryAModifiedPda";
        if (Manifest?.Error != null)
            return Manifest.Error;
        if (!SignalsMaps.IsSupported(World.LocationId))
            return "SkillsExtended.SignalsRuntime.SignalHuntsAreUnavailableOnFactory";
        if (_case == null || State?.Ready != true)
            return _creating
                ? "SkillsExtended.SignalsRuntime.SignalCachePreparationIsStillInProgress"
                : "SkillsExtended.SignalsRuntime.SignalCacheHasNotInitializedCheckTheGameLog";
        if (player.InputDirection.sqrMagnitude > .001f)
            return "SkillsExtended.SignalsRuntime.StopMovingBeforeUsingTheReceiver";
        return null;
    }

    public void Open(bool pairing = false)
    {
        var error = Eligibility(World.MainPlayer);
        if (error != null)
        {
            ElectronicsRuntime.Notify(error);
            return;
        }
        if (HackingView.IsOpen || SignalsView.Current)
            return;
        SignalsView.Open(this, pairing);
    }

    public void Send(string operation, float frequency = 0, float bearing = 0, float phase = 0)
    {
        var actor =
            World.MainPlayer?.ProfileId
            ?? (operation == "sync" ? Utils.GameUtils.GetSession()?.Profile?.Id : null);
        if (string.IsNullOrEmpty(actor))
            return;
        var r = new SignalRequest
        {
            Raid = Manifest?.Raid ?? "",
            Actor = actor,
            Sequence = ++_sequence,
            Operation = operation,
            Frequency = frequency,
            Bearing = bearing,
            Phase = phase,
        };
        if (IsAuthority())
            Handle(r);
        else
            Transport?.Invoke(r);
    }

    public void Handle(SignalRequest request)
    {
        if (!IsAuthority() || request == null)
            return;
        if (request.Operation == "sync")
        {
            Publish();
            return;
        }
        if (Authority == null)
            return;
        var player = World
            .RegisteredPlayers.OfType<Player>()
            .FirstOrDefault(p => p.ProfileId == request.Actor);
        Authority.Process(
            request,
            player ? SignalsCase.Point(player.Position) : null,
            player ? SignalsSkill.Get(player.Skills).Skill.Level : 0,
            HostTime,
            Eligibility(player),
            ConnectedActors?.Invoke()
                ?? new[] { World.MainPlayer?.ProfileId }.Where(id => id != null)
        );
        if (State.Unlocked)
            _case?.Unlock();
        Publish();
        ApplyXp();
    }

    public void Disconnect(string actor)
    {
        Authority?.Cancel(actor);
        Publish();
    }

    private void Publish()
    {
        if (Manifest == null)
            return;
        AuthorityReply?.Invoke(
            new SignalsEnvelope
            {
                Manifest = Manifest.ForPeer(),
                State = State ?? new SignalSnapshot { Raid = Manifest.Raid },
                HostTime = HostTime,
                Inventory = _case?.Snapshot(),
            }
        );
    }

    public void Receive(SignalsEnvelope envelope)
    {
        if (IsAuthority() || envelope?.Manifest == null || envelope.State == null)
            return;
        if (
            envelope.Manifest.Error == null
            && (
                (envelope.State.Ready && !envelope.Manifest.PlacementResolved)
                || (
                    envelope.Manifest.PlacementResolved
                    && envelope.Manifest.Placement?.Position?.IsFinite != true
                )
                || (
                    envelope.Manifest.PlacementResolved
                    && !SignalPoint.Finite(envelope.Manifest.Placement.Yaw)
                )
                || (
                    envelope.Manifest.PlacementResolved
                    && !string.IsNullOrEmpty(World.LocationId)
                    && !SignalsMaps.Same(envelope.Manifest.Placement.Map, World.LocationId)
                )
            )
        )
            return;
        if (
            State != null
            && State.Raid == envelope.State.Raid
            && State.Revision > envelope.State.Revision
        )
            return;
        if (Manifest != null && Manifest.Raid != envelope.Manifest.Raid)
            return;
        // Once resolved, repeated or stale snapshots cannot relocate an existing cache.
        if (
            Manifest?.PlacementResolved == true
            && (
                !envelope.Manifest.PlacementResolved
                || !Manifest.HasSamePlacement(envelope.Manifest)
            )
        )
            return;
        Manifest = envelope.Manifest;
        RefreshSkillRules();
        State = envelope.State;
        _inventory = envelope.Inventory;
        if (Manifest.Error != null)
        {
            _case?.Dispose();
            _case = null;
            SignalsView.Current?.Close();
        }
        if (Manifest.Error != null || (State.Ready && _inventory != null))
            _snapshotReady.TrySetResult(true);
        var actor = World.MainPlayer?.ProfileId;
        if (actor != null && State.LastSequences.TryGetValue(actor, out var sequence))
            _sequence = Math.Max(_sequence, sequence);
        _clockOffset = envelope.HostTime - Time.realtimeSinceStartupAsDouble;
        EnsureCase();
        if (State.Unlocked)
            _case?.Unlock();
        ApplyXp();
    }

    private void ApplyXp()
    {
        var player = World.MainPlayer;
        if (!player || State == null || !State.EarnedXp.TryGetValue(player.ProfileId, out var xp))
            return;
        var key = State.Raid + "/" + player.ProfileId;
        if (AppliedXp.TryGetValue(key, out var applied))
            _appliedXp = Math.Max(_appliedXp, applied);
        if (xp <= _appliedXp)
            return;
        try
        {
            var delta = SignalsXpLedger.Reserve(key, xp);
            _appliedXp = xp;
            AppliedXp[key] = xp;
            if (delta > 0)
                player.ExecuteSkill(() => SignalsSkill.Get(player.Skills).Action.Complete(delta));
        }
        catch (Exception e)
        {
            _appliedXp = xp; // Avoid a log storm; retry through the receipt file on reconnect.
            SkillsExtendedPlugin.Log.LogError("Could not deliver signal skill XP: " + e.Message);
        }
    }

    private void RefreshSkillRules()
    {
        if (Manifest?.Config != null && World.MainPlayer)
            SignalsSkill.Get(World.MainPlayer.Skills).ApplyRaidRules(Manifest.Config.Enabled);
    }

    private void Update()
    {
        UpdateProximityAudio();
        if (!World)
            return;
        if (World.MainPlayer && Config.ConfigManager.SignalsShortcut.Value.IsDown())
            Open();
        if (Time.unscaledTime < _nextSync)
            return;
        _nextSync = Time.unscaledTime + .5f;
        if (IsAuthority())
        {
            Authority?.Expire(HostTime);
        }
        foreach (
            var p in World
                .RegisteredPlayers.OfType<Player>()
                .Where(p => IsAuthority() || p.IsYourPlayer)
        )
        {
            if (_hits.TryGetValue(p.ProfileId, out var previous))
            {
                if (previous.Player == p)
                    continue;
                if (previous.Player)
                    previous.Player.BeingHitAction -= previous.Handler;
                _hits.Remove(p.ProfileId);
            }
            var actor = p.ProfileId;
            Action<DamageInfo, EBodyPart, float> callback = (_, _, _) =>
            {
                Authority?.Cancel(actor);
                if (p.IsYourPlayer)
                    SignalsView.Current?.Close();
                Publish();
            };
            p.BeingHitAction += callback;
            _hits.Add(actor, (p, callback));
        }
        if (!IsAuthority())
            Send("sync");
        if (SignalsView.Current && Eligibility(World.MainPlayer) != null)
            SignalsView.Current.Close();
        ApplyXp();
    }

    private void UpdateProximityAudio()
    {
        var player = World ? World.MainPlayer : null;
        var interval =
            !SkillsExtendedInfo.IsFikaHeadless
            && player
            && player.Side != EPlayerSide.Savage
            && player.HealthController.IsAlive
            && (!SkillsExtendedInfo.IsFikaPresent || Transport != null)
            && _case != null
                ? SignalsModel.ProximityInterval(
                    Manifest,
                    State,
                    SignalsCase.Point(player.Position)
                )
                : 0;
        if (interval <= 0 || !ElectronicsRuntime.HasPda(player))
        {
            _proximityAudio?.Stop();
            return;
        }
        _proximityAudio ??= new SignalsAudio(gameObject);
        _proximityAudio.Tick(
            interval,
            1,
            SignalsModel.ProximityPitch(
                SignalsCase.Point(player.Position),
                Manifest.Placement.Position,
                player.Rotation.x
            )
        );
    }

    private void OnDestroy()
    {
        _proximityAudio?.Dispose();
        _lifetime.Cancel();
        SignalsView.Current?.Close();
        foreach (var entry in _hits.Values)
            if (entry.Player)
                entry.Player.BeingHitAction -= entry.Handler;
        _case?.Dispose();
        if (Instance == this)
            Instance = null;
    }
}

public class SignalsInitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GameWorld), nameof(GameWorld.InitLevel));

    [PatchPostfix]
    private static void Postfix(GameWorld __instance, ref Task __result) =>
        __result = Complete(__result, __instance);

    private static async Task Complete(Task task, GameWorld world)
    {
        await task;
        await SignalsRuntime.Boot(world);
    }
}

public class SignalsLootPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(GameWorld), nameof(GameWorld.SpawnLoot));

    [PatchPostfix]
    private static void Postfix() => SignalsRuntime.Instance?.LootReady();
}

// Fika rebuilds this lookup again after SpawnLoot. Dynamic cases must survive that pass.
public class SignalsInteractionRegistryPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(typeof(World), nameof(World.RegisterNetworkInteractionObjects));

    [PatchPostfix]
    private static void Postfix(World __instance) =>
        SignalsRuntime.Instance?.RefreshRegistration(__instance);
}

public class SignalsCaseInteractionPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(
            typeof(InteractionContextHelper),
            nameof(InteractionContextHelper.GetAvailableActions),
            new[] { typeof(GamePlayerOwner), typeof(LootableContainer) }
        );

    [PatchPostfix]
    private static void Postfix(
        AvailableInteractionState __result,
        GamePlayerOwner owner,
        LootableContainer __1
    )
    {
        var worldInteractiveObject = __1;
        if (
            worldInteractiveObject?.Id?.StartsWith(SignalsIds.Prefix) != true
            || worldInteractiveObject.DoorState != EDoorState.Locked
        )
            return;
        __result.Actions.Clear();
        var runtime = SignalsRuntime.Instance;
        var error = runtime ? runtime.Eligibility(owner.Player) : LocalizedText.Get("SkillsExtended.SignalsRuntime.ReceiverUnavailable");
        if (error == null && runtime.State?.HasFix != true)
            error = LocalizedText.Get("SkillsExtended.SignalsRuntime.TakeTwoCrossingBearingsToRecoverTheAccessCode");
        if (error == null)
            __result.Error = null;
        __result.Actions.Add(
            new InteractionAction
            {
                Name = error == null ? LocalizedText.Get("SkillsExtended.SignalsRuntime.PairPdaSignalCache") : LocalizedText.Get("SkillsExtended.SignalsRuntime.SignalCache", error),
                Disabled = error != null,
                Action = () => runtime.Open(true),
            }
        );
    }
}

public class SignalsCaseLockPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod() =>
        AccessTools.Method(
            typeof(LootableContainer),
            nameof(LootableContainer.Interact),
            new[] { typeof(InteractionResult) }
        );

    [PatchPrefix]
    private static bool Prefix(LootableContainer __instance) =>
        __instance.Id?.StartsWith(SignalsIds.Prefix) != true
        || __instance.DoorState != EDoorState.Locked;
}
