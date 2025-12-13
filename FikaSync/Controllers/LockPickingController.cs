using System.Collections.Generic;
using EFT;
using EFT.Interactive;

namespace SkillsExtendedFika.Controllers;

public class LockPickingController
{
    private GameWorld _gameWorld;
    private List<WorldInteractiveObject> _doors = [];

    public LockPickingController(GameWorld gameWorld)
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