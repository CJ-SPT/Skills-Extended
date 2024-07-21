using EFT;
using EFT.Interactive;

namespace SkillsExtended.LockPicking;

public sealed class InspectLockActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;

    public void InspectLockAction(bool actionCompleted)
    {
        var doorLevel = Helpers.GetLevelForDoor(Owner.Player.Location, InteractiveObject.Id);

        // If the player completed the full timer uninterrupted
        if (actionCompleted)
        {
            // Only apply xp once per door per raid
            if (!Helpers.InspectedDoors.Contains(InteractiveObject.Id))
            {
                Helpers.InspectedDoors.Add(InteractiveObject.Id);
                Helpers.ApplyLockPickActionXp(InteractiveObject, Owner);
            }

            Helpers.DisplayInspectInformation(InteractiveObject, Owner);
        }
        else
        {
            Owner.CloseObjectivesPanel();
        }
    }
}