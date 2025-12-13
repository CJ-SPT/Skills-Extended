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

[BepInPlugin("com.cj.SkillsExtendedFika", "Skills Extended Fika", SkillsExtendedInfo.VERSION)]
[BepInDependency("com.cj.SkillsExtended", SkillsExtendedInfo.VERSION)]
[BepInDependency("com.fika.core")]
public class FikaSyncPlugin : BaseUnityPlugin
{
    internal new static ManualLogSource? Logger;
    private static PatchManager? _patchManager;
    
    private void Awake()
    {
        Logger = base.Logger;
        
        _patchManager = new PatchManager(this, true);
        _patchManager.EnablePatches();
        
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