using System;
using EFT;
using EFT.Interactive;

namespace SkillsExtended.LockPicking;

public sealed class InspectLockActionHandler
{
    public GamePlayerOwner Owner;
    public WorldInteractiveObject InteractiveObject;
    public static event InspectEventHandler OnLockInspected;
    public delegate void InspectEventHandler(object sender, EventArgs e); 
    
    public void InspectLockAction(bool actionCompleted)
    {
        // If the player completed the full-timer uninterrupted
        if (actionCompleted)
        {
            // Only apply xp once per door per raid
            if (!LpHelpers.InspectedDoors.Contains(InteractiveObject.Id))
            {
                LpHelpers.InspectedDoors.Add(InteractiveObject.Id);
                LpHelpers.ApplyLockPickActionXp(InteractiveObject, Owner, true);

                if (OnLockInspected is not null)
                {
                    OnLockInspected(this, EventArgs.Empty);
                }
            }

            LpHelpers.DisplayInspectInformation(InteractiveObject, Owner);
        }
        else
        {
            Owner.CloseObjectivesPanel();
        }
    }
}