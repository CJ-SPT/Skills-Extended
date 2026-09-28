using System.Collections.Generic;
using System.Linq;
using EFT.Interactive;

namespace SkillsExtended.Skills.Hacking;

internal sealed class ElectronicsDoorRegistry
{
    private readonly Dictionary<string, KeycardDoor> _doors = new();

    public static bool Supports(KeycardDoor door) => door && !string.IsNullOrEmpty(door.Id);

    public void Register(KeycardDoor door)
    {
        if (Supports(door))
        {
            _doors[door.Id] = door;
        }
    }

    public void Refresh()
    {
        // LocationScene indexes arrays by their exact registered type. Electronic
        // doors live in WorldInteractiveObject[], not a KeycardDoor[] container.
        foreach (var door in LocationScene
            .GetAllObjectsAndWhenISayAllIActuallyMeanIt<WorldInteractiveObject>()
            .OfType<KeycardDoor>())
        {
            Register(door);
        }
    }

    public KeycardDoor Resolve(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        if (_doors.TryGetValue(id, out var door) && door && door.Id == id)
        {
            return door;
        }

        // Also resolve doors loaded after raid startup on the host and peers.
        _doors.Remove(id);
        Refresh();
        return _doors.TryGetValue(id, out door) && door ? door : null;
    }
}
