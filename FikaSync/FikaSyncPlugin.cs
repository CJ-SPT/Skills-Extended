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
[BepInDependency("com.cj.SkillsExtended")]
[BepInDependency("com.fika.core")]
public class FikaSyncPlugin : BaseUnityPlugin
{
    public static LockPickingController? LockPickingController { get; set; }
    
    internal new static ManualLogSource? Logger;
    private static PatchManager? _patchManager;
    
    private void Awake()
    {
        Logger = base.Logger;
        
        _patchManager = new PatchManager(this, true);
        _patchManager.EnablePatches();
        
        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
    }
    
    private void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent createdEvent)
    {
        switch (createdEvent.Manager)
        {
            case FikaServer server:
                server.RegisterPacket<DoorPickedPacket>(OnDoorPickedPacketReceived);
                break;
            case FikaClient client:
                client.RegisterPacket<DoorPickedPacket>(OnDoorPickedPacketReceived);
                break;
        }
    }

    private static void OnDoorPickedPacketReceived(DoorPickedPacket packet)
    {
        if (packet.Unlocked)
        {
            Logger?.LogError($"Unlocking door: {packet.DoorId}");
            LockPickingController?.UnlockDoor(packet.DoorId);
        }
    }
}