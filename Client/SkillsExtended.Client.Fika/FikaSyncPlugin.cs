using System;
using BepInEx;
using BepInEx.Logging;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using SkillsExtended;

namespace SkillsExtendedFika;

[BepInPlugin("com.cj.SkillsExtendedFika", "Skills Extended Fika","1.2.4")]
[BepInDependency(SkillsExtendedInfo.MOD_GUID, SkillsExtendedInfo.VERSION)]
[BepInDependency("com.fika.core", "2.4.3")]
[BepInDependency("com.fika.headless", BepInDependency.DependencyFlags.SoftDependency)]
public class FikaSyncPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource? Logger;

    private void Awake()
    {
        Logger = base.Logger;
        if (!VersionChecker.CheckEftVersion(Logger, Config))
        {
            throw new Exception("Invalid EFT Version");
        }

        new FikaPeerIdentityPatch().Enable();
        SkillsExtendedInfo.SyncPluginPresent = true;
        ElectronicsFikaBridge.Initialize();
        SignalsFikaBridge.Initialize();
        if (SkillsExtendedInfo.IsFikaHeadless)
            new SignalsHeadlessLootPatch().Enable();
        LockPickingFikaBridge.Initialize();
        // Run after the bridges have released sessions using the departing actor.
        FikaEventDispatcher.SubscribeEvent<PeerDisconnectedEvent>(e =>
            FikaPeerIdentity.Peers.Remove(e.Peer)
        );
        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
    }

    private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent createdEvent)
    {
        FikaPeerIdentity.Peers.Clear();
        ElectronicsFikaBridge.Connect(createdEvent.Manager);
        SignalsFikaBridge.Connect(createdEvent.Manager);
        LockPickingFikaBridge.Connect(createdEvent.Manager);
    }
}
