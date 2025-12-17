using System.Collections.Generic;
using Comfort.Common;
using EFT.Interactive;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using SkillsExtended.LockPicking;
using SkillsExtended.Skills.LockPicking;
using SkillsExtendedFika.Packets;

namespace SkillsExtendedFika.Controllers;

internal static class LockPickingFikaController
{
    private static readonly List<WorldInteractiveObject> Doors = [];

    static LockPickingFikaController()
    {
        LockPickingEvents.OnLockPicked += SendLockpickingPacket;
    }
    
    public static void GetDoors()
    {
        Doors.Clear();
        
        foreach (var interactableObj in
                 LocationScene.GetAllObjectsAndWhenISayAllIActuallyMeanIt<WorldInteractiveObject>())
        {
            if (interactableObj.KeyId is null or "")
            {
                continue;
            }

            Doors.Add(interactableObj);
        }
    }

    public static void HandlePacket(LockPickingSyncPacket packet)
    {
        if (packet.Unlocked)
        {
            UnlockDoor(packet);
            return;
        }
        
        if (packet.Broken)
        {
            BreakLock(packet);
            return;
        }
        
        LockPickingHelpers.DoorAttempts[packet.DoorId] = packet.Attempts;
    }
    
    private static void SendLockpickingPacket(LockPickingEventData data)
    {
        var packet = new LockPickingSyncPacket(data);

        if (FikaBackendUtils.IsServer)
        {
            Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }
        else
        {
            Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
        }
    }
    
    private static void UnlockDoor(LockPickingSyncPacket packet)
    {
        foreach (var door in Doors)
        {
            if (door.Id != packet.DoorId)
            {
                continue;
            }

            door.Unlock();
            break;
        }
    }

    private static void BreakLock(LockPickingSyncPacket packet)
    {
        foreach (var door in Doors)
        {
            if (door.Id != packet.DoorId)
            {
                continue;
            }
            
            door.KeyId = string.Empty;
            door.Operatable = false;
            door.DoorStateChanged(EDoorState.None);
            break;
        }
    }
}