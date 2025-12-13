using System.Collections.Generic;
using EFT;
using EFT.Interactive;

namespace SkillsExtendedFika.Controllers;

public class LockPickingFikaController
{
    private GameWorld _gameWorld;
    private List<WorldInteractiveObject> _doors = [];

    public LockPickingFikaController(GameWorld gameWorld)
    {
        _gameWorld = gameWorld;
        GetDoors();
    }

    public void UnlockDoor(string doorId)
    {
        foreach (var door in _doors)
        {
            if (door.Id != doorId)
            {
                continue;
            }

            door.Unlock();
            break;
        }
    }

    public void BreakLock(string doorId)
    {
        foreach (var door in _doors)
        {
            if (door.Id != doorId)
            {
                continue;
            }
            
            door.KeyId = string.Empty;
            door.Operatable = false;
            door.DoorStateChanged(EDoorState.None);
            break;
        }
    }

    private void GetDoors()
    {
        foreach (var interactableObj in
                 LocationScene.GetAllObjectsAndWhenISayAllIActuallyMeanIt<WorldInteractiveObject>())
        {
            if (interactableObj.KeyId is null or "")
            {
                continue;
            }

            _doors.Add(interactableObj);
        }
    }
}