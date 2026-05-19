using System;
using BepInEx;
using BepInEx.Logging;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using SkillsExtended;
using SkillsExtendedFika.Controllers;
using SkillsExtendedFika.Packets;
using SPT.Reflection.Patching;

namespace SkillsExtendedFika;

[BepInPlugin("com.cj.SkillsExtendedFika", "Skills Extended Fika", SkillsExtendedInfo.SYNC_VERSION)]
[BepInDependency("com.cj.SkillsExtended", SkillsExtendedInfo.MIN_MOD_VERSION_FOR_SYNC)]
[BepInDependency("com.fika.core", SkillsExtendedInfo.MIN_FIKA_VERSION)]
public class FikaSyncPlugin : BaseUnityPlugin
{
    internal new static ManualLogSource? Logger;
    private static PatchManager? _patchManager;
    
    private void Awake()
    {
        Logger = base.Logger;

        if (!VersionChecker.CheckEftVersion(Logger, Config))
        {
            throw new Exception("Invalid EFT Version");
        }
        
        _patchManager = new PatchManager(this, true);
        _patchManager.EnablePatches();

        SkillsExtendedInfo.SyncPluginPresent = true;
        
        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
    }
    
    private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent createdEvent)
    {
        switch (createdEvent.Manager)
        {
            case FikaServer server:
                server.RegisterPacket<LockPickingSyncPacket>(OnLockPickingSyncPacketReceived);
                break;
            case FikaClient client:
                client.RegisterPacket<LockPickingSyncPacket>(OnLockPickingSyncPacketReceived);
                break;
        }
    }

    private static void OnLockPickingSyncPacketReceived(LockPickingSyncPacket packet)
    {
#if DEBUG
        Logger?.LogDebug("Received LockPickingSyncPacket");
#endif
        
        LockPickingFikaController.HandlePacket(packet);
    }
}